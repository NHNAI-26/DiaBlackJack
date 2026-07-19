using System.Collections.Generic;
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

        public StageProgressionSession Session { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            Session = new StageProgressionSession(CreatePrototypeProgress());
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

        private RunProgress CreatePrototypeProgress()
        {
            var cards = new List<RunCardDefinition>(20);
            int cardId = 0;
            for (int rank = 1; rank <= 10; rank++)
            {
                cards.Add(new RunCardDefinition(cardId++, rank));
                cards.Add(new RunCardDefinition(cardId++, rank));
            }

            var player = new PlayerRunState(12, 12, cards);
            var stages = new[]
            {
                new StageDefinition(
                    "normal-1",
                    "Ash Gate",
                    StageKind.NormalCombat,
                    3,
                    seed,
                    seed + 1),
                new StageDefinition(
                    "normal-2",
                    "Blood Hall",
                    StageKind.NormalCombat,
                    4,
                    seed + 2,
                    seed + 3),
                new StageDefinition(
                    "final-boss",
                    "Black Throne",
                    StageKind.FinalBossCombat,
                    7,
                    seed + 4,
                    seed + 5)
            };

            return new RunProgress(stages, player);
        }
    }
}
