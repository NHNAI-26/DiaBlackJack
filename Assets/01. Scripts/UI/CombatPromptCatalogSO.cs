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

        private static readonly Regex TokenPattern =
            new Regex("\\{[^{}]+\\}", RegexOptions.Compiled);
        private static readonly HashSet<string> AllowedTokens =
            new HashSet<string>
            {
                "{source}",
                "{context}",
                "{current}",
                "{required}"
            };

        [SerializeField] private List<Entry> entries = new List<Entry>();

        private readonly HashSet<CombatPromptId> _loggedFailures =
            new HashSet<CombatPromptId>();
        private Dictionary<CombatPromptId, string> _templates;

        public IReadOnlyList<Entry> Entries => entries;

        public bool TryResolve(CombatPromptRequest request, out string text)
        {
            EnsureLookup();
            if (!_templates.TryGetValue(request.Id, out string template) ||
                string.IsNullOrWhiteSpace(template) ||
                HasUnknownToken(template))
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

        public IReadOnlyList<string> GetValidationErrors()
        {
            var errors = new List<string>();
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

                if (string.IsNullOrWhiteSpace(entry.Template))
                {
                    errors.Add($"Prompt text is empty: {entry.Id}.");
                }
                else
                {
                    foreach (Match match in TokenPattern.Matches(entry.Template))
                    {
                        if (!AllowedTokens.Contains(match.Value))
                        {
                            errors.Add(
                                $"Unknown token {match.Value}: {entry.Id}.");
                        }
                    }

                    string withoutTokens = TokenPattern.Replace(entry.Template, string.Empty);
                    if (withoutTokens.Contains("{") || withoutTokens.Contains("}"))
                    {
                        errors.Add($"Malformed token: {entry.Id}.");
                    }
                }
            }

            foreach (CombatPromptId id in Enum.GetValues(typeof(CombatPromptId)))
            {
                if (id != CombatPromptId.None && !seen.Contains(id))
                {
                    errors.Add($"Missing prompt id: {id}.");
                }
            }

            return errors.AsReadOnly();
        }

        internal void ReplaceEntriesForEditor(IEnumerable<Entry> replacement)
        {
            entries = replacement == null
                ? new List<Entry>()
                : new List<Entry>(replacement);
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

        private void EnsureLookup()
        {
            if (_templates != null)
            {
                return;
            }

            _templates = new Dictionary<CombatPromptId, string>();
            foreach (Entry entry in entries)
            {
                if (entry == null ||
                    entry.Id == CombatPromptId.None ||
                    _templates.ContainsKey(entry.Id))
                {
                    continue;
                }

                _templates.Add(entry.Id, entry.Template);
            }
        }

        private void InvalidateLookup()
        {
            _templates = null;
            _loggedFailures.Clear();
        }

        private static bool HasUnknownToken(string template)
        {
            foreach (Match match in TokenPattern.Matches(template))
            {
                if (!AllowedTokens.Contains(match.Value))
                {
                    return true;
                }
            }

            string withoutTokens = TokenPattern.Replace(template, string.Empty);
            return withoutTokens.Contains("{") || withoutTokens.Contains("}");
        }

        private void LogFailureOnce(CombatPromptId id)
        {
            if (_loggedFailures.Add(id))
            {
                Debug.LogError(
                    $"[CombatPromptCatalog] Prompt cannot be resolved: {id}.",
                    this);
            }
        }
    }
}
