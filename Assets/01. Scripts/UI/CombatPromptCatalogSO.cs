using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using DiaBlackJack.CoreLoop;
using UnityEngine;

namespace DiaBlackJack.GameScene
{
    [CreateAssetMenu(
        fileName = "CombatPromptCatalog",
        menuName = "DiaBlackJack/UI/Combat Prompt Catalog")]
    public sealed class CombatPromptCatalogSO : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public Entry(CombatPromptId id, string template)
            {
                this.id = id;
                this.template = template;
            }

            [SerializeField] private CombatPromptId id;
            [SerializeField, TextArea(1, 3)] private string template;

            public CombatPromptId Id => id;

            public string Template => template;
        }

        [Serializable]
        public sealed class AutomaticResultEntry
        {
            public AutomaticResultEntry(
                AutomaticCardResultPromptId id,
                string template,
                string comparisonTemplate = "")
            {
                this.id = id;
                this.template = template;
                this.comparisonTemplate = comparisonTemplate;
            }

            [SerializeField] private AutomaticCardResultPromptId id;
            [SerializeField, TextArea(1, 4)] private string template;
            [SerializeField, TextArea(1, 2)] private string comparisonTemplate;
            [SerializeField] private string playerOwnerLabel = "나";
            [SerializeField] private string playerPossessiveLabel = "내";
            [SerializeField] private string enemyOwnerLabel = "적";
            [SerializeField] private string discardDispositionLabel =
                "사용 후 버려졌습니다.";
            [SerializeField] private string retainDispositionLabel =
                "공개 상태로 남았습니다.";
            [SerializeField] private string acceptedDecisionLabel =
                "효과를 사용했습니다";
            [SerializeField] private string declinedDecisionLabel =
                "효과를 거절했습니다";
            [SerializeField] private string atLeastComparisonLabel =
                "이상입니다.";
            [SerializeField] private string belowComparisonLabel =
                "미만입니다.";
            [SerializeField] private string reservedOutcomeLabel =
                "승리하면 영혼을 회복합니다.";
            [SerializeField] private string resolvedOutcomeLabel =
                "영혼 회복 없이 효과가 끝났습니다.";

            public AutomaticCardResultPromptId Id => id;
            public string Template => template;
            public string ComparisonTemplate => comparisonTemplate;
            public string PlayerOwnerLabel => playerOwnerLabel;
            public string PlayerPossessiveLabel => playerPossessiveLabel;
            public string EnemyOwnerLabel => enemyOwnerLabel;
            public string DiscardDispositionLabel => discardDispositionLabel;
            public string RetainDispositionLabel => retainDispositionLabel;
            public string AcceptedDecisionLabel => acceptedDecisionLabel;
            public string DeclinedDecisionLabel => declinedDecisionLabel;
            public string AtLeastComparisonLabel => atLeastComparisonLabel;
            public string BelowComparisonLabel => belowComparisonLabel;
            public string ReservedOutcomeLabel => reservedOutcomeLabel;
            public string ResolvedOutcomeLabel => resolvedOutcomeLabel;
        }

        private static readonly Regex TokenPattern =
            new Regex("\\{[^{}]+\\}", RegexOptions.Compiled);
        private static readonly HashSet<string> SelectionTokens =
            new HashSet<string>
            {
                "{source}",
                "{context}",
                "{current}",
                "{required}"
            };
        private static readonly HashSet<string> AutomaticResultTokens =
            new HashSet<string>
            {
                "{source}",
                "{owner}",
                "{ownerPossessive}",
                "{enemy}",
                "{disposition}",
                "{playerDecision}",
                "{enemyDecision}",
                "{declared}",
                "{comparison}",
                "{outcome}"
            };

        [SerializeField] private List<Entry> entries = new List<Entry>();
        [SerializeField] private List<AutomaticResultEntry>
            automaticResultEntries = new List<AutomaticResultEntry>();

        private readonly HashSet<CombatPromptId> _loggedSelectionFailures =
            new HashSet<CombatPromptId>();
        private readonly HashSet<AutomaticCardResultPromptId>
            _loggedAutomaticResultFailures =
                new HashSet<AutomaticCardResultPromptId>();
        private Dictionary<CombatPromptId, string> _selectionTemplates;
        private Dictionary<AutomaticCardResultPromptId, AutomaticResultEntry>
            _automaticResultLookup;

        public IReadOnlyList<Entry> Entries => entries;

        public IReadOnlyList<AutomaticResultEntry> AutomaticResultEntries =>
            automaticResultEntries;

        public bool TryResolve(CombatPromptRequest request, out string text)
        {
            EnsureLookup();
            if (!_selectionTemplates.TryGetValue(
                    request.Id,
                    out string template) ||
                string.IsNullOrWhiteSpace(template) ||
                HasInvalidToken(template, SelectionTokens))
            {
                LogFailureOnce(request.Id);
                text = string.Empty;
                return false;
            }

            text = template
                .Replace("{source}", request.SourceDisplayName)
                .Replace("{context}", request.ContextText)
                .Replace("{current}", request.CurrentCount.ToString())
                .Replace("{required}", request.RequiredCount.ToString());
            return true;
        }

        public bool TryResolve(
            AutomaticCardResultPromptRequest request,
            out string text)
        {
            return TryResolve(request, string.Empty, out text);
        }

        public bool TryResolve(
            AutomaticCardResultPromptRequest request,
            string enemyDisplayName,
            out string text)
        {
            EnsureLookup();
            if (!_automaticResultLookup.TryGetValue(
                    request.Id,
                    out AutomaticResultEntry entry) ||
                !CanResolve(entry, request))
            {
                LogFailureOnce(request.Id);
                text = string.Empty;
                return false;
            }

            string resolvedEnemyName = ResolveEnemyDisplayName(
                entry,
                enemyDisplayName);
            text = ReplaceAutomaticResultTokens(
                entry.Template,
                entry,
                request,
                resolvedEnemyName);
            if (request.Comparison != AutomaticCardHiddenComparison.None &&
                !string.IsNullOrWhiteSpace(entry.ComparisonTemplate))
            {
                text += ReplaceAutomaticResultTokens(
                    entry.ComparisonTemplate,
                    entry,
                    request,
                    resolvedEnemyName);
            }

            return true;
        }

        public IReadOnlyList<string> GetValidationErrors()
        {
            var errors = new List<string>();
            ValidateSelectionEntries(errors);
            ValidateAutomaticResultEntries(errors);
            return errors.AsReadOnly();
        }

        internal void ReplaceEntriesForEditor(IEnumerable<Entry> replacement)
        {
            entries = replacement == null
                ? new List<Entry>()
                : new List<Entry>(replacement);
            InvalidateLookup();
        }

        internal void ReplaceAutomaticResultEntriesForEditor(
            IEnumerable<AutomaticResultEntry> replacement)
        {
            automaticResultEntries = replacement == null
                ? new List<AutomaticResultEntry>()
                : new List<AutomaticResultEntry>(replacement);
            InvalidateLookup();
        }

        private void OnEnable()
        {
            InvalidateLookup();
        }

        private void OnValidate()
        {
            InvalidateLookup();
            foreach (string error in GetValidationErrors())
            {
                Debug.LogError($"[CombatPromptCatalog] {error}", this);
            }
        }

        private void ValidateSelectionEntries(List<string> errors)
        {
            var seen = new HashSet<CombatPromptId>();
            foreach (Entry entry in entries)
            {
                if (entry == null)
                {
                    errors.Add("Prompt entry is null.");
                    continue;
                }

                if (!Enum.IsDefined(typeof(CombatPromptId), entry.Id) ||
                    entry.Id == CombatPromptId.None)
                {
                    errors.Add($"Unknown prompt id: {entry.Id}.");
                    continue;
                }

                if (!seen.Add(entry.Id))
                {
                    errors.Add($"Duplicate prompt id: {entry.Id}.");
                }

                ValidateTemplate(
                    entry.Template,
                    entry.Id.ToString(),
                    SelectionTokens,
                    errors,
                    required: true);
            }

            foreach (CombatPromptId id in Enum.GetValues(typeof(CombatPromptId)))
            {
                if (id != CombatPromptId.None && !seen.Contains(id))
                {
                    errors.Add($"Missing prompt id: {id}.");
                }
            }
        }

        private void ValidateAutomaticResultEntries(List<string> errors)
        {
            var seen = new HashSet<AutomaticCardResultPromptId>();
            foreach (AutomaticResultEntry entry in automaticResultEntries)
            {
                if (entry == null)
                {
                    errors.Add("Automatic result entry is null.");
                    continue;
                }

                if (!Enum.IsDefined(
                        typeof(AutomaticCardResultPromptId),
                        entry.Id) ||
                    entry.Id == AutomaticCardResultPromptId.None)
                {
                    errors.Add($"Unknown automatic result id: {entry.Id}.");
                    continue;
                }

                if (!seen.Add(entry.Id))
                {
                    errors.Add($"Duplicate automatic result id: {entry.Id}.");
                }

                ValidateTemplate(
                    entry.Template,
                    entry.Id.ToString(),
                    AutomaticResultTokens,
                    errors,
                    required: true);
                ValidateTemplate(
                    entry.ComparisonTemplate,
                    entry.Id + " comparison",
                    AutomaticResultTokens,
                    errors,
                    required: false);
                ValidateRequiredLabels(entry, errors);
            }

            foreach (AutomaticCardResultPromptId id in
                Enum.GetValues(typeof(AutomaticCardResultPromptId)))
            {
                if (id != AutomaticCardResultPromptId.None && !seen.Contains(id))
                {
                    errors.Add($"Missing automatic result id: {id}.");
                }
            }
        }

        private static void ValidateTemplate(
            string template,
            string id,
            HashSet<string> allowedTokens,
            List<string> errors,
            bool required)
        {
            if (string.IsNullOrWhiteSpace(template))
            {
                if (required)
                {
                    errors.Add($"Prompt text is empty: {id}.");
                }

                return;
            }

            foreach (Match match in TokenPattern.Matches(template))
            {
                if (!allowedTokens.Contains(match.Value))
                {
                    errors.Add($"Unknown token {match.Value}: {id}.");
                }
            }

            string withoutTokens = TokenPattern.Replace(template, string.Empty);
            if (withoutTokens.Contains("{") || withoutTokens.Contains("}"))
            {
                errors.Add($"Malformed token: {id}.");
            }
        }

        private static void ValidateRequiredLabels(
            AutomaticResultEntry entry,
            List<string> errors)
        {
            string allTemplates = entry.Template + entry.ComparisonTemplate;
            RequireLabels(
                allTemplates.Contains("{owner}") ||
                    allTemplates.Contains("{enemy}"),
                entry.Id,
                errors,
                entry.PlayerOwnerLabel,
                entry.EnemyOwnerLabel);
            RequireLabels(
                allTemplates.Contains("{ownerPossessive}"),
                entry.Id,
                errors,
                entry.PlayerPossessiveLabel,
                entry.EnemyOwnerLabel);
            RequireLabels(
                allTemplates.Contains("{disposition}"),
                entry.Id,
                errors,
                entry.DiscardDispositionLabel,
                entry.RetainDispositionLabel);
            RequireLabels(
                allTemplates.Contains("{playerDecision}") ||
                    allTemplates.Contains("{enemyDecision}"),
                entry.Id,
                errors,
                entry.AcceptedDecisionLabel,
                entry.DeclinedDecisionLabel);
            RequireLabels(
                allTemplates.Contains("{comparison}"),
                entry.Id,
                errors,
                entry.AtLeastComparisonLabel,
                entry.BelowComparisonLabel);
            RequireLabels(
                allTemplates.Contains("{outcome}"),
                entry.Id,
                errors,
                entry.ReservedOutcomeLabel,
                entry.ResolvedOutcomeLabel);
        }

        private static void RequireLabels(
            bool required,
            AutomaticCardResultPromptId id,
            List<string> errors,
            params string[] labels)
        {
            if (!required)
            {
                return;
            }

            foreach (string label in labels)
            {
                if (string.IsNullOrWhiteSpace(label))
                {
                    errors.Add($"Automatic result label is empty: {id}.");
                    return;
                }
            }
        }

        private void EnsureLookup()
        {
            if (_selectionTemplates != null && _automaticResultLookup != null)
            {
                return;
            }

            _selectionTemplates = new Dictionary<CombatPromptId, string>();
            foreach (Entry entry in entries)
            {
                if (entry != null &&
                    entry.Id != CombatPromptId.None &&
                    !_selectionTemplates.ContainsKey(entry.Id))
                {
                    _selectionTemplates.Add(entry.Id, entry.Template);
                }
            }

            _automaticResultLookup =
                new Dictionary<
                    AutomaticCardResultPromptId,
                    AutomaticResultEntry>();
            foreach (AutomaticResultEntry entry in automaticResultEntries)
            {
                if (entry != null &&
                    entry.Id != AutomaticCardResultPromptId.None &&
                    !_automaticResultLookup.ContainsKey(entry.Id))
                {
                    _automaticResultLookup.Add(entry.Id, entry);
                }
            }
        }

        private void InvalidateLookup()
        {
            _selectionTemplates = null;
            _automaticResultLookup = null;
            _loggedSelectionFailures.Clear();
            _loggedAutomaticResultFailures.Clear();
        }

        private static bool CanResolve(
            AutomaticResultEntry entry,
            AutomaticCardResultPromptRequest request)
        {
            string comparisonTemplate = entry?.ComparisonTemplate ?? string.Empty;
            if (entry == null ||
                string.IsNullOrWhiteSpace(entry.Template) ||
                HasInvalidToken(entry.Template, AutomaticResultTokens) ||
                HasInvalidToken(
                    comparisonTemplate,
                    AutomaticResultTokens))
            {
                return false;
            }

            string allTemplates = entry.Template + comparisonTemplate;
            return HasRequiredLabels(entry, allTemplates) &&
                (!allTemplates.Contains("{declared}") ||
                    request.DeclaredNumber.HasValue) &&
                (!allTemplates.Contains("{playerDecision}") ||
                    request.PlayerDecision != AutomaticCardDecisionOutcome.None) &&
                (!allTemplates.Contains("{enemyDecision}") ||
                    request.EnemyDecision != AutomaticCardDecisionOutcome.None) &&
                (!comparisonTemplate.Contains("{comparison}") ||
                    request.Comparison == AutomaticCardHiddenComparison.None ||
                    !string.IsNullOrWhiteSpace(ResolveComparison(entry, request))) &&
                (!allTemplates.Contains("{outcome}") ||
                    request.Outcome != AutomaticCardResultOutcome.None);
        }

        private static bool HasRequiredLabels(
            AutomaticResultEntry entry,
            string allTemplates)
        {
            return (!(allTemplates.Contains("{owner}") ||
                      allTemplates.Contains("{enemy}")) ||
                    AllLabelsExist(
                        entry.PlayerOwnerLabel,
                        entry.EnemyOwnerLabel)) &&
                (!allTemplates.Contains("{ownerPossessive}") ||
                    AllLabelsExist(
                        entry.PlayerPossessiveLabel,
                        entry.EnemyOwnerLabel)) &&
                (!allTemplates.Contains("{disposition}") ||
                    AllLabelsExist(
                        entry.DiscardDispositionLabel,
                        entry.RetainDispositionLabel)) &&
                (!(allTemplates.Contains("{playerDecision}") ||
                   allTemplates.Contains("{enemyDecision}")) ||
                    AllLabelsExist(
                        entry.AcceptedDecisionLabel,
                        entry.DeclinedDecisionLabel)) &&
                (!allTemplates.Contains("{comparison}") ||
                    AllLabelsExist(
                        entry.AtLeastComparisonLabel,
                        entry.BelowComparisonLabel)) &&
                (!allTemplates.Contains("{outcome}") ||
                    AllLabelsExist(
                        entry.ReservedOutcomeLabel,
                        entry.ResolvedOutcomeLabel));
        }

        private static bool AllLabelsExist(params string[] labels)
        {
            foreach (string label in labels)
            {
                if (string.IsNullOrWhiteSpace(label))
                {
                    return false;
                }
            }

            return true;
        }

        private static string ReplaceAutomaticResultTokens(
            string template,
            AutomaticResultEntry entry,
            AutomaticCardResultPromptRequest request,
            string enemyDisplayName)
        {
            return template
                .Replace("{source}", request.SourceDisplayName)
                .Replace(
                    "{owner}",
                    request.OwnerSide == CombatantSide.Player
                        ? entry.PlayerOwnerLabel
                        : enemyDisplayName)
                .Replace(
                    "{ownerPossessive}",
                    request.OwnerSide == CombatantSide.Player
                        ? entry.PlayerPossessiveLabel
                        : enemyDisplayName + "의")
                .Replace("{enemy}", enemyDisplayName)
                .Replace(
                    "{disposition}",
                    request.SourceDisposition ==
                        AutomaticCardSourceDisposition.Discard
                            ? entry.DiscardDispositionLabel
                            : entry.RetainDispositionLabel)
                .Replace(
                    "{playerDecision}",
                    ResolveDecision(entry, request.PlayerDecision))
                .Replace(
                    "{enemyDecision}",
                    ResolveDecision(entry, request.EnemyDecision))
                .Replace(
                    "{declared}",
                    request.DeclaredNumber?.ToString() ?? string.Empty)
                .Replace("{comparison}", ResolveComparison(entry, request))
                .Replace("{outcome}", ResolveOutcome(entry, request));
        }

        private static string ResolveEnemyDisplayName(
            AutomaticResultEntry entry,
            string enemyDisplayName)
        {
            return string.IsNullOrWhiteSpace(enemyDisplayName) ||
                string.Equals(
                    enemyDisplayName,
                    "UNPROFILED ENEMY",
                    StringComparison.Ordinal)
                ? entry.EnemyOwnerLabel
                : enemyDisplayName.Trim();
        }

        private static string ResolveDecision(
            AutomaticResultEntry entry,
            AutomaticCardDecisionOutcome decision)
        {
            return decision == AutomaticCardDecisionOutcome.Accepted
                ? entry.AcceptedDecisionLabel
                : decision == AutomaticCardDecisionOutcome.Declined
                    ? entry.DeclinedDecisionLabel
                    : string.Empty;
        }

        private static string ResolveComparison(
            AutomaticResultEntry entry,
            AutomaticCardResultPromptRequest request)
        {
            return request.Comparison ==
                AutomaticCardHiddenComparison.AtLeastDeclared
                    ? entry.AtLeastComparisonLabel
                    : request.Comparison ==
                        AutomaticCardHiddenComparison.BelowDeclared
                        ? entry.BelowComparisonLabel
                        : string.Empty;
        }

        private static string ResolveOutcome(
            AutomaticResultEntry entry,
            AutomaticCardResultPromptRequest request)
        {
            return request.Outcome ==
                AutomaticCardResultOutcome.WinHealReserved
                    ? entry.ReservedOutcomeLabel
                    : request.Outcome ==
                        AutomaticCardResultOutcome.ReservationResolved
                        ? entry.ResolvedOutcomeLabel
                        : string.Empty;
        }

        private static bool HasInvalidToken(
            string template,
            HashSet<string> allowedTokens)
        {
            if (string.IsNullOrEmpty(template))
            {
                return false;
            }

            foreach (Match match in TokenPattern.Matches(template))
            {
                if (!allowedTokens.Contains(match.Value))
                {
                    return true;
                }
            }

            string withoutTokens = TokenPattern.Replace(template, string.Empty);
            return withoutTokens.Contains("{") || withoutTokens.Contains("}");
        }

        private void LogFailureOnce(CombatPromptId id)
        {
            if (_loggedSelectionFailures.Add(id))
            {
                Debug.LogError(
                    $"[CombatPromptCatalog] Prompt cannot be resolved: {id}.",
                    this);
            }
        }

        private void LogFailureOnce(AutomaticCardResultPromptId id)
        {
            if (_loggedAutomaticResultFailures.Add(id))
            {
                Debug.LogError(
                    $"[CombatPromptCatalog] Automatic result cannot be resolved: {id}.",
                    this);
            }
        }
    }
}
