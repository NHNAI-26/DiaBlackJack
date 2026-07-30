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
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
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
            DontDestroyOnLoad(gameObject);
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
