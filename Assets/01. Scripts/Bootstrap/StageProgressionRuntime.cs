using System.Collections.Generic;
using Border.SaveLoad;
using DiaBlackJack.CoreLoop;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DiaBlackJack.StageProgression.UI
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class StageProgressionRuntime : MonoBehaviour
    {
        [SerializeField] private string progressionSceneName = "StageTest";
        [SerializeField] private string battleSceneName = "CoreLoopTest";
        [SerializeField] private int seed = 20260719;

        public static StageProgressionRuntime Instance { get; private set; }

        public RunSaveFlow SaveFlow { get; private set; }

        public StageProgressionSession Session
        {
            get => SaveFlow?.Session ?? _injectedSession;
            private set => _injectedSession = value;
        }

        private StageProgressionSession _injectedSession;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            IRunSaveFileStore fileStore = new SystemRunSaveFileStore();
            SaveFlow = new RunSaveFlow(
                new RunSaveRepository(fileStore, CreatePrototypeStages(seed)),
                new RunReservationRepository(
                    fileStore,
                    DemonContractCatalog.Default),
                CreatePrototypeStages,
                CreatePrototypeSession,
                seed);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void LoadBattleScene()
        {
            SceneManager.LoadScene(battleSceneName);
        }

        public void LoadProgressionScene()
        {
            SceneManager.LoadScene(progressionSceneName);
        }

        private StageProgressionSession CreatePrototypeSession(int rootSeed)
        {
            return new StageProgressionSession(
                new RunProgress(
                    CreatePrototypeStages(rootSeed),
                    CreatePrototypePlayer()),
                rewardGenerator: new BattleRewardGenerator(
                    BattleRewardCatalog.CreateDefault(),
                    unchecked(rootSeed + 1)),
                opponentSelectionGenerator: new OpponentSelectionGenerator(
                    EnemyCombatProfileCatalog.Default,
                    rootSeed));
        }

        private static PlayerRunState CreatePrototypePlayer()
        {
            List<RunCardDefinition> cards = new List<RunCardDefinition>(20);
            int cardId = 0;
            for (int rank = 1; rank <= 10; rank++)
            {
                cards.Add(new RunCardDefinition(cardId++, rank, CardSuit.Spade));
                cards.Add(new RunCardDefinition(cardId++, rank, CardSuit.Clover));
            }

            return new PlayerRunState(
                12,
                12,
                cards);
        }

        private IReadOnlyList<StageDefinition> CreatePrototypeStages(
            int rootSeed)
        {
            return new[]
            {
                StageDefinition.CreateForEnemyProfile(
                    "normal-1",
                    "Ash Gate",
                    StageKind.NormalCombat,
                    EnemyCombatProfileCatalog.GunslingerKey,
                    rootSeed,
                    unchecked(rootSeed + 1)),
                StageDefinition.CreateForEnemyProfile(
                    "normal-2",
                    "Blood Hall",
                    StageKind.NormalCombat,
                    EnemyCombatProfileCatalog.EnforcerKey,
                    unchecked(rootSeed + 2),
                    unchecked(rootSeed + 3)),
                StageDefinition.CreateForEnemyProfile(
                    "final-boss",
                    "Black Throne",
                    StageKind.FinalBossCombat,
                    EnemyCombatProfileCatalog.FinalBossKey,
                    unchecked(rootSeed + 4),
                    unchecked(rootSeed + 5))
            };
        }
    }
}
