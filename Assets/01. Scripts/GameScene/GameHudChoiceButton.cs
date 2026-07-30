using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DiaBlackJack.GameScene
{
    [DisallowMultipleComponent]
    public sealed class GameHudChoiceButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text labelText;
        [SerializeField] private Image faceImage;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text abilityText;
        [SerializeField] private TMP_Text costText;

        private GameSceneCombatHudCommand _command;
        private bool _isInteractable;

        public event Action<GameSceneCombatHudCommand> CommandRequested;

        private void Awake()
        {
            button ??= GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(HandleClick);
            }
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(HandleClick);
            }
        }

        public void Render(GameSceneCombatHudActionViewModel model)
        {
            if (button != null)
            {
                button.enabled = true;
            }

            _command = model == null ? default : model.Command;
            _isInteractable = model != null && model.IsInteractable;
            if (labelText != null)
            {
                labelText.text = model == null ? string.Empty : model.Label;
            }

            if (button != null)
            {
                button.interactable = _isInteractable;
            }
        }

        public void RenderContractCandidate(
            GameSceneCombatHudContractCandidateViewModel model,
            Sprite faceSprite)
        {
            if (button != null)
            {
                button.enabled = true;
            }

            _command = model == null ? default : model.Command;
            _isInteractable = model != null && model.IsInteractable;
            if (titleText != null)
            {
                titleText.text = model == null ? string.Empty : model.Title;
            }

            if (abilityText != null)
            {
                abilityText.text = model == null ? string.Empty : model.Ability;
            }

            if (costText != null)
            {
                costText.text = model == null ? string.Empty : model.Cost;
            }

            if (labelText != null)
            {
                labelText.text = model == null ? string.Empty : model.ButtonLabel;
            }

            if (faceImage != null)
            {
                faceImage.sprite = faceSprite;
                faceImage.enabled = faceSprite != null;
            }

            if (button != null)
            {
                button.interactable = _isInteractable;
            }
        }

        public void RenderContractDetail(
            GameSceneCombatHudContractCandidateViewModel model,
            Sprite faceSprite,
            float uiScale)
        {
            RenderContractCandidate(model, faceSprite);
            float scale = Mathf.Clamp(uiScale, 0.85f, 1.5f);
            _isInteractable = false;
            if (titleText != null)
            {
                titleText.text = model == null ? string.Empty : model.Title;
                ConfigureRect(
                    titleText.rectTransform,
                    new Vector2(25f, -20f) * scale,
                    new Vector2(180f, 48f) * scale);
                titleText.enableAutoSizing = true;
                titleText.fontSizeMin = Mathf.Max(20f, 20f * scale);
                titleText.fontSizeMax = 30f * scale;
                titleText.fontSize = titleText.fontSizeMax;
                titleText.fontStyle = FontStyles.Bold;
                titleText.alignment = TextAlignmentOptions.Center;
            }

            if (faceImage != null)
            {
                ConfigureRect(
                    faceImage.rectTransform,
                    new Vector2(25f, -75f) * scale,
                    new Vector2(160f, 320f) * scale);
            }

            if (abilityText != null)
            {
                abilityText.text = model == null
                    ? string.Empty
                    : "<color=#D34B3F><b>ACTIVE</b></color>\n" +
                        model.Ability;
                ConfigureDetailText(
                    abilityText,
                    new Vector2(220f, -45f) * scale,
                    new Vector2(675f, 170f) * scale,
                    scale);
            }

            if (costText != null)
            {
                costText.text = model == null
                    ? string.Empty
                    : "<color=#D7A53B><b>COST</b></color>\n" +
                        model.Cost;
                ConfigureDetailText(
                    costText,
                    new Vector2(220f, -240f) * scale,
                    new Vector2(675f, 150f) * scale,
                    scale);
            }

            if (labelText != null)
            {
                labelText.text = string.Empty;
            }

            if (button != null)
            {
                button.enabled = false;
            }
        }

        private static void ConfigureDetailText(
            TMP_Text text,
            Vector2 anchoredPosition,
            Vector2 size,
            float scale)
        {
            ConfigureRect(text.rectTransform, anchoredPosition, size);
            text.enableAutoSizing = true;
            text.fontSizeMin = Mathf.Max(20f, 20f * scale);
            text.fontSizeMax = 24f * scale;
            text.fontSize = text.fontSizeMax;
            text.fontStyle = FontStyles.Normal;
            text.alignment = TextAlignmentOptions.TopLeft;
        }

        private static void ConfigureRect(
            RectTransform rect,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        private void HandleClick()
        {
            if (_isInteractable)
            {
                CommandRequested?.Invoke(_command);
            }
        }
    }
}
