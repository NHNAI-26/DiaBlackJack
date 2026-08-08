using System;
using System.Collections.Generic;
using UnityEngine;

namespace DiaBlackJack.Content
{
    [Serializable]
    public sealed class RunResultOpponentDialogue
    {
        [SerializeField] private string profileKey;
        [TextArea(1, 4)]
        [SerializeField] private List<string> lines = new List<string>();

        public string ProfileKey => profileKey;

        public IReadOnlyList<string> Lines => lines;

        internal void ValidateOrThrow(string assetName)
        {
            if (string.IsNullOrWhiteSpace(profileKey))
            {
                throw new InvalidOperationException(
                    $"Run result dialogue '{assetName}' contains an empty opponent profile key.");
            }

            ValidateLines(assetName, $"opponent '{profileKey}'", lines);
        }

        internal static void ValidateLines(
            string assetName,
            string sectionName,
            IReadOnlyList<string> values)
        {
            if (values == null || values.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Run result dialogue '{assetName}' section '{sectionName}' has no lines.");
            }

            for (int index = 0; index < values.Count; index++)
            {
                if (string.IsNullOrWhiteSpace(values[index]))
                {
                    throw new InvalidOperationException(
                        $"Run result dialogue '{assetName}' section '{sectionName}' contains an empty line.");
                }
            }
        }
    }

    [CreateAssetMenu(
        fileName = "RunResultDialogue",
        menuName = "DiaBlackJack/Dialogue/Run Result Dialogue")]
    public sealed class RunResultDialogueSO : ScriptableObject
    {
        [SerializeField, Min(1f)] private float charactersPerSecond = 40f;
        [TextArea(1, 4)]
        [SerializeField] private List<string> victoryLines = new List<string>();
        [TextArea(1, 4)]
        [SerializeField] private List<string> contractedVictoryLines =
            new List<string>();
        [TextArea(1, 4)]
        [SerializeField] private List<string> defeatOpeningLines =
            new List<string>();
        [SerializeField] private List<RunResultOpponentDialogue> opponentDefeatLines =
            new List<RunResultOpponentDialogue>();
        [TextArea(1, 4)]
        [SerializeField] private List<string> defeatClosingLines =
            new List<string>();

        public float CharactersPerSecond => Mathf.Max(1f, charactersPerSecond);

        public IReadOnlyList<string> VictoryLines => victoryLines;

        public IReadOnlyList<string> ContractedVictoryLines =>
            contractedVictoryLines;

        public IReadOnlyList<string> DefeatOpeningLines => defeatOpeningLines;

        public IReadOnlyList<string> DefeatClosingLines => defeatClosingLines;

        public bool TryGetOpponentDefeatLines(
            string profileKey,
            out IReadOnlyList<string> lines)
        {
            if (!string.IsNullOrWhiteSpace(profileKey) &&
                opponentDefeatLines != null)
            {
                for (int index = 0; index < opponentDefeatLines.Count; index++)
                {
                    RunResultOpponentDialogue entry = opponentDefeatLines[index];
                    if (entry != null && string.Equals(
                        entry.ProfileKey,
                        profileKey,
                        StringComparison.Ordinal))
                    {
                        lines = entry.Lines;
                        return true;
                    }
                }
            }

            lines = Array.Empty<string>();
            return false;
        }

        public void ValidateOrThrow()
        {
            RunResultOpponentDialogue.ValidateLines(
                name,
                nameof(victoryLines),
                victoryLines);
            RunResultOpponentDialogue.ValidateLines(
                name,
                nameof(contractedVictoryLines),
                contractedVictoryLines);
            RunResultOpponentDialogue.ValidateLines(
                name,
                nameof(defeatOpeningLines),
                defeatOpeningLines);
            RunResultOpponentDialogue.ValidateLines(
                name,
                nameof(defeatClosingLines),
                defeatClosingLines);

            if (opponentDefeatLines == null || opponentDefeatLines.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Run result dialogue '{name}' requires opponent defeat lines.");
            }

            var keys = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < opponentDefeatLines.Count; index++)
            {
                RunResultOpponentDialogue entry = opponentDefeatLines[index];
                if (entry == null)
                {
                    throw new InvalidOperationException(
                        $"Run result dialogue '{name}' contains a null opponent entry.");
                }

                entry.ValidateOrThrow(name);
                if (!keys.Add(entry.ProfileKey))
                {
                    throw new InvalidOperationException(
                        $"Run result dialogue '{name}' contains duplicate opponent '{entry.ProfileKey}'.");
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
            catch (Exception exception)
            {
                Debug.LogError(exception.Message, this);
            }
        }
    }
}
