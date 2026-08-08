using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DiaBlackJack.Content;
using DiaBlackJack.StageProgression;
using UnityEngine;

namespace DiaBlackJack.GameScene
{
    internal enum RunResultDialogueClickResult
    {
        CompleteCurrentLine,
        ShowNextLine,
        CompleteDialogue
    }

    public sealed class RunResultDialogueViewModel
    {
        internal RunResultDialogueViewModel(
            IEnumerable<string> lines,
            float charactersPerSecond)
        {
            Lines = new ReadOnlyCollection<string>(new List<string>(lines));
            CharactersPerSecond = charactersPerSecond;
        }

        public float CharactersPerSecond { get; }

        public IReadOnlyList<string> Lines { get; }
    }

    public static class RunResultDialoguePresenter
    {
        public static RunResultDialogueViewModel Create(
            GameFlowScreen screen,
            PlayerRunState player,
            StageDefinition activeStage,
            RunResultDialogueSO dialogue)
        {
            if (player == null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            if (activeStage == null)
            {
                throw new ArgumentNullException(nameof(activeStage));
            }

            if (dialogue == null)
            {
                throw new ArgumentNullException(nameof(dialogue));
            }

            return CreateForPreview(
                screen,
                player.HasMadeDemonContract,
                activeStage.BattleProfileKey,
                activeStage.DisplayName,
                dialogue);
        }

        internal static RunResultDialogueViewModel CreateForPreview(
            GameFlowScreen screen,
            bool hasMadeDemonContract,
            string opponentProfileKey,
            string opponentDisplayName,
            RunResultDialogueSO dialogue)
        {
            if (dialogue == null)
            {
                throw new ArgumentNullException(nameof(dialogue));
            }

            dialogue.ValidateOrThrow();
            var lines = new List<string>();
            switch (screen)
            {
                case GameFlowScreen.RunVictory:
                    Add(lines, dialogue.VictoryLines);
                    if (hasMadeDemonContract)
                    {
                        Add(lines, dialogue.ContractedVictoryLines);
                    }

                    break;
                case GameFlowScreen.RunDefeat:
                    Add(lines, dialogue.DefeatOpeningLines);
                    if (dialogue.TryGetOpponentDefeatLines(
                        opponentProfileKey,
                        out IReadOnlyList<string> opponentLines))
                    {
                        Add(lines, opponentLines);
                    }
                    else
                    {
                        string opponentName = string.IsNullOrWhiteSpace(
                            opponentDisplayName)
                            ? "상대"
                            : opponentDisplayName;
                        lines.Add(
                            $"{opponentName}는 아무래도 쉽지 않은 상대죠.");
                        Debug.LogWarning(
                            $"Run result dialogue is missing opponent '{opponentProfileKey}'.");
                    }

                    Add(lines, dialogue.DefeatClosingLines);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(screen),
                        "Run result dialogue requires a terminal screen.");
            }

            return new RunResultDialogueViewModel(
                lines,
                dialogue.CharactersPerSecond);
        }

        private static void Add(List<string> target, IReadOnlyList<string> source)
        {
            for (int index = 0; index < source.Count; index++)
            {
                target.Add(source[index]);
            }
        }
    }

    internal sealed class RunResultDialogueSequence
    {
        private readonly IReadOnlyList<string> _lines;
        private int _lineIndex;
        private bool _isCompleted;

        internal RunResultDialogueSequence(IReadOnlyList<string> lines)
        {
            if (lines == null || lines.Count == 0)
            {
                throw new ArgumentException(
                    "Run result dialogue requires at least one line.",
                    nameof(lines));
            }

            _lines = lines;
        }

        internal string CurrentLine => _lines[_lineIndex];

        internal bool IsCompleted => _isCompleted;

        internal event Action Completed;

        internal bool TryAdvance()
        {
            if (_lineIndex >= _lines.Count - 1)
            {
                return false;
            }

            _lineIndex++;
            return true;
        }

        internal RunResultDialogueClickResult HandleClick(
            bool currentLineComplete)
        {
            if (!currentLineComplete)
            {
                return RunResultDialogueClickResult.CompleteCurrentLine;
            }

            if (TryAdvance())
            {
                return RunResultDialogueClickResult.ShowNextLine;
            }

            Complete();
            return RunResultDialogueClickResult.CompleteDialogue;
        }

        internal void Complete()
        {
            if (_isCompleted)
            {
                return;
            }

            _isCompleted = true;
            Completed?.Invoke();
        }
    }
}
