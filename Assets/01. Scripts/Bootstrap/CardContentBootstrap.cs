using DiaBlackJack.Content;
using DiaBlackJack.CoreLoop;
using UnityEngine;

namespace DiaBlackJack.Bootstrap
{
    [DefaultExecutionOrder(-200)]
    [DisallowMultipleComponent]
    public sealed class CardContentBootstrap : MonoBehaviour
    {
        [SerializeField] private CardContentCatalogSO catalog;

        public static CardContentBootstrap Instance { get; private set; }

        public CardContentCatalog RuntimeCatalog { get; private set; }

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

            RuntimeCatalog = catalog.BuildRuntimeCatalog();
            CardDefinitionCatalog.Install(RuntimeCatalog);
            DemonContractCatalog.Install(RuntimeCatalog);
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
