using System.Collections.Generic;
using DiaBlackJack.Bootstrap;
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
        [SerializeField] private string battleSceneName = "GameScene";
        [SerializeField] private string mainMenuSceneName = "MainMenuScene";
        [SerializeField] private int seed = 20260719;

        public static StageProgressionRuntime Instance { get; private set; }

        public RunSaveFlow SaveFlow { get; private set; }

        private TutorialProgressStore _tutorialProgressStore;

        public FormalRunSession FormalSession
        {
            get
            {
                StageProgressionSession session = Session;
                if (session == null || session.UsesBattleRewards)
                {
                    _formalSession = null;
                    _formalCombatSession = null;
                    return null;
                }

                if (!ReferenceEquals(session, _formalCombatSession))
                {
                    _formalCombatSession = session;
                    _formalSession = new FormalRunSession(
                        session,
                        CreateShopOfferGenerator(unchecked(
                            (SaveFlow?.ActiveRootSeed ?? seed) + 2)),
                        SaveFlow?.RestoredCompletedShopCount ?? 0,
                        SaveFlow?.RestoredUtilityPriceLevel ?? 0);
                    _formalSession.CombatSettlementCompleted +=
                        HandleFormalCombatSettlementCompleted;
                    _formalSession.ShopExited += HandleFormalShopExited;
                    _formalSession.RunEnded += HandleFormalRunEnded;
                }

                _formalSession.SynchronizeExternalState();
                return _formalSession;
            }
        }

        public StageProgressionSession Session
        {
            get => SaveFlow?.Session ?? _injectedSession;
            private set => _injectedSession = value;
        }

        private StageProgressionSession _injectedSession;
        private FormalRunSession _formalSession;
        private StageProgressionSession _formalCombatSession;

        private void HandleFormalCombatSettlementCompleted()
        {
            SaveFlow?.TryCheckpointCombatSettlement(
                _formalSession.CompletedShopCount,
                _formalSession.UtilityPriceLevel);
        }

        private void HandleFormalShopExited()
        {
            SaveFlow?.TryCheckpointShopExit(
                _formalSession.CompletedShopCount,
                _formalSession.UtilityPriceLevel);
        }

        private void HandleFormalRunEnded()
        {
            SaveFlow?.TryCheckpointFormalRunEnd(
                _formalSession.CompletedShopCount,
                _formalSession.UtilityPriceLevel);
        }
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
            _tutorialProgressStore = new TutorialProgressStore(fileStore);
            SaveFlow = new RunSaveFlow(
                new RunSaveRepository(fileStore, CreatePrototypeStages(seed)),
                new RunReservationRepository(
                    fileStore,
                    DemonContractCatalog.Default),
                CreatePrototypeStages,
                CreatePrototypeSession,
                seed,
                usesBattleRewards: false);
            _ = FormalSession;
        }

        /// <summary>
        /// Starts the active run, exactly like calling <see cref="RunSaveFlow.TryStartRun"/>
        /// directly, but also records the scripted first-play tutorial as seen the moment a
        /// tutorial-flavored session is actually started — not merely constructed (the
        /// session factory below can run speculatively, e.g. while restoring a save, so the
        /// flag must only flip once a tutorial run is genuinely underway).
        /// </summary>
        public bool TryStartRun()
        {
            bool started = SaveFlow != null && SaveFlow.TryStartRun();
            if (started && SaveFlow.Session != null && SaveFlow.Session.IsTutorialRun)
            {
                _tutorialProgressStore.TryMarkSeen();
            }

            return started;
        }

        private StageProgressionSession CreateSessionForCurrentTutorialState(int rootSeed)
        {
            return _tutorialProgressStore.HasSeenTutorial
                ? CreatePrototypeSession(rootSeed)
                : CreateTutorialSession(rootSeed);
        }

        internal static StageProgressionSession CreateTutorialSession(int rootSeed)
        {
            return new StageProgressionSession(
                new RunProgress(
                    CreatePrototypeStages(rootSeed),
                    CreatePrototypePlayer()),
                battleFactory: TutorialBattleFactory.Create,
                rewardGenerator: new BattleRewardGenerator(
                    BattleRewardCatalog.CreateDefault(),
                    unchecked(rootSeed + 1)),
                opponentSelectionGenerator: new OpponentSelectionGenerator(
                    EnemyCombatProfileCatalog.Default,
                    rootSeed),
                startingDemonGrantGenerator: new StartingDemonGrantGenerator(
                    DemonContractCatalog.Default,
                    unchecked(rootSeed + 3),
                    DemonContractCatalog.PlayerDefaultDemonDeckKeys,
                    forcedFirstGrantDefinitionKeys: new[]
                    {
                        DemonContractCatalog.BeelzebubKey,
                        DemonContractCatalog.AsmodeusKey
                    }),
                usesBattleRewards: false,
                forcedFirstStageOpponentProfileKey:
                    EnemyCombatProfileCatalog.CowardlyGamblerKey);
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

        public void LoadMainMenuScene()
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }

        internal static StageProgressionSession CreatePrototypeSession(int rootSeed)
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
                    rootSeed),
                startingDemonGrantGenerator: new StartingDemonGrantGenerator(
                    DemonContractCatalog.Default,
                    unchecked(rootSeed + 3)),
                usesBattleRewards: false);
        }

        private ShopOfferGenerator CreateShopOfferGenerator(int shopSeed)
        {
            CardContentCatalog catalog = CardContentBootstrap.Instance?.RuntimeCatalog;
            return catalog == null
                ? new ShopOfferGenerator(shopSeed)
                : new ShopOfferGenerator(catalog, shopSeed);
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
                cards,
                demonDeck: new List<RunDemonDefinition>());
        }

        private static IReadOnlyList<StageDefinition> CreatePrototypeStages(
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
