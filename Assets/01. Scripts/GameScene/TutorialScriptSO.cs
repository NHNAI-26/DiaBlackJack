using System.Collections.Generic;
using UnityEngine;

namespace DiaBlackJack.GameScene
{
    public enum TutorialStepKind
    {
        Dialogue,
        Gate
    }

    public enum TutorialGateKind
    {
        Hit,
        Stand,
        BeginChange,
        Revolver,
        ContractCandidate,
        ContractOption,
        DeckPreview,
        RevolverResolve,
        EnemyLieDetectorResolved
    }

    public enum TutorialSpeakerKind
    {
        NarratorAsmodeus,
        PlayerContractedAsmodeus
    }

    public enum TutorialHighlightTarget
    {
        None,
        Hit,
        Stand,
        Change,
        RevolverCard,
        ContractPaper,
        PlayerDrawDeck
    }

    /// <summary>
    /// One entry in a <see cref="TutorialScriptSO"/>. Dialogue entries carry only text —
    /// gate entries carry no text at all, just which behavior to attach (the actual
    /// Hit/Stand/Revolver/contract wiring stays in <c>TutorialDirector</c>, since it calls
    /// into live <c>GameManager</c> state and isn't meaningfully "content").
    /// </summary>
    [System.Serializable]
    public sealed class TutorialStepEntry
    {
        public TutorialStepKind kind = TutorialStepKind.Dialogue;

        [TextArea(1, 4)]
        public string[] lines = System.Array.Empty<string>();

        [Tooltip("Section 0-1 intro dialogue — its completion holds the enemy entrance/round-1 reveal until dismissed.")]
        public bool isIntro;

        [Min(0f)]
        [Tooltip("Delay before the first line of this dialogue block is shown.")]
        public float delayBeforeSeconds;

        [Tooltip("Round-1 soul-loss recap dialogue — its completion is what reveals round 2's held-back deal.")]
        public bool defersRoundTwoReveal;

        [Tooltip("Dialogue speaker. Contracted Asmodeus follows the player's actual card.")]
        public TutorialSpeakerKind speaker = TutorialSpeakerKind.NarratorAsmodeus;

        [Tooltip("Wait until the battle-end skull and enemy exit presentation completes.")]
        public bool waitForBattleEndPresentation;

        public TutorialGateKind gateKind = TutorialGateKind.Hit;

        [Tooltip("ContractCandidate gate only: the DemonContractCatalog key that stays selectable.")]
        public string contractDefinitionKey;

        [Tooltip("ContractOption gate only: the option id that stays selectable.")]
        public int contractOptionId;
    }

    /// <summary>
    /// Data for the scripted first-play tutorial's dialogue and step order — see
    /// <c>TutorialDirector</c> for how each entry gets turned into a driven step.
    /// </summary>
    [CreateAssetMenu(
        fileName = "TutorialScript",
        menuName = "DiaBlackJack/Tutorial Script")]
    public sealed class TutorialScriptSO : ScriptableObject
    {
        [SerializeField] private List<TutorialStepEntry> steps = new List<TutorialStepEntry>();

        public IReadOnlyList<TutorialStepEntry> Steps => steps;
    }
}
