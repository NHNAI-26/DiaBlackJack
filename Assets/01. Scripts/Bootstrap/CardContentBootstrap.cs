using DiaBlackJack.Content;
using DiaBlackJack.CoreLoop;
using DiaBlackJack.StageProgression;
using UnityEngine;

namespace DiaBlackJack.Bootstrap
{
    [DefaultExecutionOrder(-200)]
    [DisallowMultipleComponent]
    public sealed class CardContentBootstrap : MonoBehaviour
    {
        [SerializeField] private CardContentCatalogSO catalog;
        [SerializeField] private EnemyContentCatalogSO enemyCatalog;

        public static CardContentBootstrap Instance { get; private set; }

        public CardContentCatalog RuntimeCatalog { get; private set; }

        public EnemyCombatProfileCatalog RuntimeEnemyCatalog { get; private set; }

        public GoldRewardCatalog RuntimeGoldRewardCatalog { get; private set; }

        public EnemyContentCatalogSO EnemyCatalog => enemyCatalog;

        private void Awake()
        {
            if (transform.parent != null && HasSceneRootBootstrap())
            {
                Destroy(this);
                return;
            }

            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            if (catalog == null)
            {
                Debug.LogError("CardContentBootstrap requires a CardContentCatalogSO.", this);
                enabled = false;
                return;
            }

            if (enemyCatalog == null)
            {
                Debug.LogError(
                    "CardContentBootstrap requires an EnemyContentCatalogSO.",
                    this);
                enabled = false;
                return;
            }

            RuntimeCatalog = catalog.BuildRuntimeCatalog();
            CardDefinitionCatalog.Install(RuntimeCatalog);
            DemonContractCatalog.Install(RuntimeCatalog);
            RuntimeEnemyCatalog = enemyCatalog.BuildRuntimeCatalog();
            RuntimeGoldRewardCatalog = enemyCatalog.BuildGoldRewardCatalog();
            EnemyCombatProfileCatalog.Install(RuntimeEnemyCatalog);
            GoldRewardCatalog.Install(RuntimeGoldRewardCatalog);
            Instance = this;
            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        private bool HasSceneRootBootstrap()
        {
            CardContentBootstrap[] bootstraps =
                FindObjectsByType<CardContentBootstrap>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            for (int index = 0; index < bootstraps.Length; index++)
            {
                CardContentBootstrap bootstrap = bootstraps[index];
                if (bootstrap != this && bootstrap.transform.parent == null)
                {
                    return true;
                }
            }

            return false;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
