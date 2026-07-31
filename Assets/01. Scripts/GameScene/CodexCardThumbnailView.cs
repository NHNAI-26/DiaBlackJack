using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DiaBlackJack.GameScene
{
    [DisallowMultipleComponent]
    public sealed class CodexCardThumbnailView : MonoBehaviour
    {
        [SerializeField] private Image faceImage;
        [SerializeField] private TMP_Text fallbackText;
        [SerializeField] private TMP_Text nameText;

        public void Render(string displayName, Sprite faceSprite)
        {
            string safeName = displayName ?? string.Empty;
            if (faceImage != null)
            {
                faceImage.sprite = faceSprite;
                faceImage.enabled = faceSprite != null;
            }

            if (fallbackText != null)
            {
                fallbackText.text = safeName;
                fallbackText.gameObject.SetActive(faceSprite == null);
            }

            if (nameText != null)
            {
                nameText.text = safeName;
            }
        }
    }
}
