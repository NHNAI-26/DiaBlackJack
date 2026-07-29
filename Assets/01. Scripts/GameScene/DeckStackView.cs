using System.Collections.Generic;
using UnityEngine;

namespace DiaBlackJack.GameScene
{
    [DisallowMultipleComponent]
    public sealed class DeckStackView : MonoBehaviour
    {
        [SerializeField] private Renderer[] renderers;
        [SerializeField] private Collider[] colliders;
        [SerializeField] private float minimumHeight = 0.08f;
        [SerializeField] private float heightPerCard = 0.035f;
        [SerializeField] private float maximumHeight = 1.2f;

        private Vector3 _baseLocalScale;
        private Vector3 _baseLocalPosition;
        private bool _initialized;

        private void Awake()
        {
            CaptureBaseTransform();
            AutoBindMissingReferences();
        }

        private void Reset()
        {
            CaptureBaseTransform();
            AutoBindMissingReferences();
        }

        private void OnValidate()
        {
            minimumHeight = Mathf.Max(0.001f, minimumHeight);
            heightPerCard = Mathf.Max(0f, heightPerCard);
            maximumHeight = Mathf.Max(minimumHeight, maximumHeight);
            AutoBindMissingReferences();
        }

        public void Render(int cardCount)
        {
            CaptureBaseTransform();
            AutoBindMissingReferences();

            bool visible = cardCount > 0;
            SetVisible(visible);
            transform.localPosition = _baseLocalPosition;
            if (!visible)
            {
                return;
            }

            float height = Mathf.Clamp(
                minimumHeight + (cardCount - 1) * heightPerCard,
                minimumHeight,
                maximumHeight);
            Vector3 scale = _baseLocalScale;
            scale.y = height;
            transform.localScale = scale;
        }

        private void SetVisible(bool visible)
        {
            if (renderers != null)
            {
                for (int i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i] != null)
                    {
                        renderers[i].enabled = visible;
                    }
                }
            }

            if (colliders != null)
            {
                for (int i = 0; i < colliders.Length; i++)
                {
                    if (colliders[i] != null)
                    {
                        colliders[i].enabled = visible;
                    }
                }
            }
        }

        private void CaptureBaseTransform()
        {
            if (_initialized)
            {
                return;
            }

            _baseLocalScale = transform.localScale;
            _baseLocalPosition = transform.localPosition;
            _initialized = true;
        }

        private void AutoBindMissingReferences()
        {
            if (renderers == null || renderers.Length == 0)
            {
                renderers = GetComponentsInChildren<Renderer>(includeInactive: true);
            }

            if (colliders == null || colliders.Length == 0)
            {
                colliders = GetComponentsInChildren<Collider>(includeInactive: true);
            }
        }
    }
}
