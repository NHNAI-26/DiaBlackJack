using TMPro;
using UnityEngine;

namespace DiaBlackJack.GameScene
{
    [DisallowMultipleComponent]
    public sealed class TableCombatCommandView : MonoBehaviour
    {
        [SerializeField] private GameSceneCombatHudCommandKind commandKind;
        [SerializeField] private SpriteRenderer artworkRenderer;
        [SerializeField] private TMP_Text labelText;
        [SerializeField] private Collider hitCollider;
        [SerializeField] private Color interactableColor =
            new Color(0.91f, 0.85f, 0.69f, 1f);
        [SerializeField] private Color hoveredColor =
            new Color(0.72f, 0.23f, 0.20f, 1f);
        [SerializeField] private Color disabledColor =
            new Color(0.35f, 0.35f, 0.35f, 0.35f);

        private GameSceneCombatHudActionViewModel _model;
        private bool _isHovered;

        public GameSceneCombatHudCommandKind Kind => commandKind;

        public bool IsInteractable => _model != null && _model.IsInteractable;

        public string Tooltip => _model == null ? string.Empty : _model.Tooltip;

        public Vector3 TooltipWorldPosition => artworkRenderer == null
            ? transform.position
            : artworkRenderer.bounds.center;

        public bool HasRequiredReferences =>
            artworkRenderer != null &&
            hitCollider != null &&
            labelText != null;

        public void Render(GameSceneCombatHudActionViewModel model)
        {
            _model = model != null && model.Command.Kind == commandKind
                ? model
                : null;
            _isHovered = false;

            if (labelText != null)
            {
                labelText.text = _model == null ? string.Empty : _model.Label;
            }

            if (hitCollider != null)
            {
                hitCollider.enabled = IsInteractable;
            }

            ApplyColor();
        }

        public void ResetView()
        {
            _model = null;
            _isHovered = false;
            if (labelText != null)
            {
                labelText.text = string.Empty;
            }

            if (hitCollider != null)
            {
                hitCollider.enabled = false;
            }

            ApplyColor();
        }

        public void SetHovered(bool isHovered)
        {
            _isHovered = isHovered && IsInteractable;
            ApplyColor();
        }

        public bool TryGetCommand(out GameSceneCombatHudCommand command)
        {
            if (IsInteractable)
            {
                command = _model.Command;
                return true;
            }

            command = default;
            return false;
        }

        private void ApplyColor()
        {
            Color color = !IsInteractable
                ? disabledColor
                : _isHovered
                    ? hoveredColor
                    : interactableColor;
            if (artworkRenderer != null)
            {
                artworkRenderer.color = color;
            }

            if (labelText != null)
            {
                labelText.color = color;
            }
        }
    }
}
