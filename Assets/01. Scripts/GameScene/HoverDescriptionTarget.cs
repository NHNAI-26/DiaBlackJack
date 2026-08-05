using System;
using System.Collections.Generic;
using DiaBlackJack.Content;
using UnityEngine;

namespace DiaBlackJack.GameScene
{
    internal readonly struct HoverDescriptionValue
    {
        public HoverDescriptionValue(string name, object value)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Value = value == null ? string.Empty : value.ToString();
        }

        public string Name { get; }

        public string Value { get; }
    }

    [DisallowMultipleComponent]
    public sealed class HoverDescriptionTarget : MonoBehaviour
    {
        [SerializeField] private HoverDescriptionSO description;
        [SerializeField] private Transform worldAnchor;
        [SerializeField] private Collider targetCollider;
        [SerializeField] private bool showBelow;

        private readonly Dictionary<string, string> _tokens =
            new Dictionary<string, string>(StringComparer.Ordinal);
        private string _stateKey;

        public HoverDescriptionSO Description => description;

        public bool HasRequiredReferences => description != null;

        internal string ResolvedDescription => description == null
            ? string.Empty
            : description.ResolveDescription(_stateKey, _tokens);

        internal void Configure(
            string stateKey,
            params HoverDescriptionValue[] values)
        {
            _stateKey = string.IsNullOrWhiteSpace(stateKey)
                ? null
                : stateKey.Trim();
            _tokens.Clear();

            if (values != null)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    HoverDescriptionValue value = values[i];
                    if (!_tokens.TryAdd(value.Name, value.Value))
                    {
                        throw new ArgumentException(
                            $"Duplicate hover-description token: {value.Name}",
                            nameof(values));
                    }
                }
            }

            if (description != null)
            {
                description.ResolveDescription(_stateKey, _tokens);
            }
        }

        internal bool TryCreateRequest(
            Camera camera,
            out CardHoverBadgeRequest request)
        {
            request = null;
            if (description == null || camera == null || !isActiveAndEnabled)
            {
                return false;
            }

            Vector3 worldPosition = worldAnchor != null
                ? worldAnchor.position
                : targetCollider != null
                    ? targetCollider.bounds.center
                    : transform.position;
            Vector3 screenPosition = camera.WorldToScreenPoint(worldPosition);
            if (screenPosition.z <= 0f)
            {
                return false;
            }

            request = new CardHoverBadgeRequest(
                description.Title,
                description.ResolveDescription(_stateKey, _tokens),
                new Vector2(screenPosition.x, screenPosition.y),
                showBelow);
            return true;
        }

        private void OnValidate()
        {
            targetCollider ??= GetComponentInChildren<Collider>(true);
            if (description == null)
            {
                return;
            }

            try
            {
                description.ValidateOrThrow();
            }
            catch (InvalidOperationException exception)
            {
                Debug.LogError(exception.Message, this);
            }
        }
    }
}
