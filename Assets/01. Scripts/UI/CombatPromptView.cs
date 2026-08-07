using DiaBlackJack.CoreLoop;
using TMPro;
using UnityEngine;

namespace DiaBlackJack.GameScene
{
    [DisallowMultipleComponent]
    public sealed class CombatPromptView : MonoBehaviour
    {
        [SerializeField] private CombatPromptCatalogSO catalog;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text promptText;

        private bool _loggedMissingReference;

        public bool HasRequiredReferences =>
            catalog != null && canvasGroup != null && promptText != null;

        public void Render(CombatPromptRequest request)
        {
            if (!HasRequiredReferences)
            {
                LogMissingReferenceOnce();
                Hide();
                return;
            }

            if (!catalog.TryResolve(request, out string resolvedText))
            {
                Hide();
                return;
            }

            CurrencyIconText.Set(promptText, resolvedText);
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (canvasGroup == null)
            {
                gameObject.SetActive(false);
                return;
            }

            canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }

        private void LogMissingReferenceOnce()
        {
            if (_loggedMissingReference)
            {
                return;
            }

            _loggedMissingReference = true;
            Debug.LogError(
                "CombatPromptView requires a catalog, CanvasGroup, and TMP text.",
                this);
        }
    }
}
