using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace DiaBlackJack.Content
{
    [CreateAssetMenu(
        fileName = "HoverDescription",
        menuName = "DiaBlackJack/Hover/Hover Description")]
    public sealed class HoverDescriptionSO : ScriptableObject
    {
        [Serializable]
        private sealed class StateDescription
        {
            [SerializeField] private string stateKey;
            [SerializeField, TextArea(2, 5)] private string descriptionTemplate;

            internal string StateKey => stateKey;
            internal string DescriptionTemplate => descriptionTemplate;
        }

        private static readonly HashSet<string> SupportedTokens =
            new HashSet<string>(StringComparer.Ordinal)
            {
                "price",
                "amount",
                "gold",
                "availableDemons"
            };

        private static readonly Regex TokenPattern = new Regex(
            "\\{([A-Za-z][A-Za-z0-9_-]*)\\}",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        [SerializeField] private string title;
        [SerializeField, TextArea(2, 5)] private string descriptionTemplate;
        [SerializeField] private List<StateDescription> stateDescriptions =
            new List<StateDescription>();

        public string Title => title == null ? string.Empty : title.Trim();

        public string ResolveDescription(
            string stateKey = null,
            IReadOnlyDictionary<string, string> tokens = null)
        {
            ValidateOrThrow();

            string template = ResolveTemplate(stateKey);
            if (tokens != null)
            {
                foreach (KeyValuePair<string, string> token in tokens)
                {
                    if (!SupportedTokens.Contains(token.Key))
                    {
                        throw new ArgumentException(
                            $"Unsupported hover-description token: {token.Key}",
                            nameof(tokens));
                    }

                    template = template.Replace(
                        "{" + token.Key + "}",
                        token.Value ?? string.Empty);
                }
            }

            Match unresolved = TokenPattern.Match(template);
            if (unresolved.Success)
            {
                throw new InvalidOperationException(
                    $"Hover description '{name}' has an unresolved token: " +
                    unresolved.Value);
            }

            return template;
        }

        public void ValidateOrThrow()
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                throw new InvalidOperationException(
                    $"Hover description '{name}' requires a title.");
            }

            if (string.IsNullOrWhiteSpace(descriptionTemplate))
            {
                throw new InvalidOperationException(
                    $"Hover description '{name}' requires a default description.");
            }

            ValidateTemplate(descriptionTemplate, "default");

            if (stateDescriptions == null)
            {
                throw new InvalidOperationException(
                    $"Hover description '{name}' requires a state-description list.");
            }

            var keys = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < stateDescriptions.Count; i++)
            {
                StateDescription state = stateDescriptions[i];
                if (state == null || string.IsNullOrWhiteSpace(state.StateKey))
                {
                    throw new InvalidOperationException(
                        $"Hover description '{name}' has an empty state key.");
                }

                string key = state.StateKey.Trim();
                if (!keys.Add(key))
                {
                    throw new InvalidOperationException(
                        $"Hover description '{name}' has a duplicate state key: {key}");
                }

                if (string.IsNullOrWhiteSpace(state.DescriptionTemplate))
                {
                    throw new InvalidOperationException(
                        $"Hover description '{name}' has an empty '{key}' description.");
                }

                ValidateTemplate(state.DescriptionTemplate, key);
            }
        }

        private string ResolveTemplate(string stateKey)
        {
            if (string.IsNullOrWhiteSpace(stateKey))
            {
                return descriptionTemplate;
            }

            string normalizedKey = stateKey.Trim();
            foreach (StateDescription state in stateDescriptions)
            {
                if (StringComparer.Ordinal.Equals(state.StateKey.Trim(), normalizedKey))
                {
                    return state.DescriptionTemplate;
                }
            }

            throw new KeyNotFoundException(
                $"Hover description '{name}' has no state named '{normalizedKey}'.");
        }

        private void ValidateTemplate(string template, string stateKey)
        {
            MatchCollection matches = TokenPattern.Matches(template);
            foreach (Match match in matches)
            {
                string token = match.Groups[1].Value;
                if (!SupportedTokens.Contains(token))
                {
                    throw new InvalidOperationException(
                        $"Hover description '{name}' state '{stateKey}' uses " +
                        $"unsupported token: {match.Value}");
                }
            }
        }

        private void OnValidate()
        {
            if ((hideFlags & HideFlags.DontSave) != 0)
            {
                return;
            }

            try
            {
                ValidateOrThrow();
            }
            catch (InvalidOperationException exception)
            {
                Debug.LogError(exception.Message, this);
            }
        }
    }
}
