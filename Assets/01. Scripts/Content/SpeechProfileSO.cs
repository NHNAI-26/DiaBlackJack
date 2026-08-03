using System;
using System.Collections.Generic;
using UnityEngine;

namespace DiaBlackJack.Content
{
    [CreateAssetMenu(
        fileName = "SpeechProfile",
        menuName = "DiaBlackJack/Speech/Speech Profile")]
    public sealed class SpeechProfileSO : ScriptableObject
    {
        [Serializable]
        private sealed class SpeechEntry
        {
            [SerializeField] private string cueKey;
            [TextArea(1, 3)]
            [SerializeField] private List<string> lines = new List<string>();

            public string CueKey => cueKey;

            public IReadOnlyList<string> Lines => lines;

            public void ValidateOrThrow(string assetName)
            {
                if (string.IsNullOrWhiteSpace(cueKey))
                {
                    throw new InvalidOperationException(
                        $"Speech profile '{assetName}' contains an empty cue key.");
                }

                if (lines == null || lines.Count == 0)
                {
                    throw new InvalidOperationException(
                        $"Speech profile '{assetName}' cue '{cueKey}' has no lines.");
                }

                for (int index = 0; index < lines.Count; index++)
                {
                    if (string.IsNullOrWhiteSpace(lines[index]))
                    {
                        throw new InvalidOperationException(
                            $"Speech profile '{assetName}' cue '{cueKey}' contains an empty line.");
                    }
                }
            }
        }

        [SerializeField] private string speakerKey;
        [SerializeField] private List<SpeechEntry> entries =
            new List<SpeechEntry>();

        public int EntryCount => entries == null ? 0 : entries.Count;

        public string SpeakerKey => speakerKey;

        public bool HasCue(string cueKey)
        {
            return TryGetLines(cueKey, out _);
        }

        public int GetLineCount(string cueKey)
        {
            return TryGetLines(cueKey, out IReadOnlyList<string> lines)
                ? lines.Count
                : 0;
        }

        public bool TryGetLines(
            string cueKey,
            out IReadOnlyList<string> lines)
        {
            if (!string.IsNullOrWhiteSpace(cueKey) && entries != null)
            {
                for (int index = 0; index < entries.Count; index++)
                {
                    SpeechEntry entry = entries[index];
                    if (entry != null && string.Equals(
                        entry.CueKey,
                        cueKey,
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
            if (string.IsNullOrWhiteSpace(speakerKey))
            {
                throw new InvalidOperationException(
                    $"Speech profile '{name}' requires a speaker key.");
            }

            if (entries == null || entries.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Speech profile '{name}' requires at least one cue.");
            }

            var keys = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < entries.Count; index++)
            {
                SpeechEntry entry = entries[index];
                if (entry == null)
                {
                    throw new InvalidOperationException(
                        $"Speech profile '{name}' contains a null cue.");
                }

                entry.ValidateOrThrow(name);
                if (!keys.Add(entry.CueKey))
                {
                    throw new InvalidOperationException(
                        $"Speech profile '{name}' contains duplicate cue '{entry.CueKey}'.");
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
