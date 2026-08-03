using System;
using UnityEngine;

namespace DiaBlackJack.GameScene
{
    internal enum CardSelectionFanPreset
    {
        TwoCards,
        TenCards
    }

    internal readonly struct CardSelectionFanPose
    {
        internal CardSelectionFanPose(
            Vector2 viewportPosition,
            float cameraDistance,
            float angle,
            float scale,
            float poseLerp,
            int baseSortingOrder)
        {
            ViewportPosition = viewportPosition;
            CameraDistance = cameraDistance;
            Angle = angle;
            Scale = scale;
            PoseLerp = poseLerp;
            BaseSortingOrder = baseSortingOrder;
        }

        internal Vector2 ViewportPosition { get; }

        internal float CameraDistance { get; }

        internal float Angle { get; }

        internal float Scale { get; }

        internal float PoseLerp { get; }

        internal int BaseSortingOrder { get; }
    }

    [Serializable]
    internal sealed class CardSelectionFanProfile
    {
        [SerializeField] private Vector2 viewportCenter;
        [SerializeField] private float cameraDistance;
        [SerializeField] private float halfViewportWidth;
        [SerializeField] private float edgeLift;
        [SerializeField] private float maximumFanAngle;
        [SerializeField] private float cardScale;
        [SerializeField] private float hoverViewportLift;
        [SerializeField] private float hoverCameraPull;
        [SerializeField] private float poseLerp;
        [SerializeField] private int baseSortingOrder;

        private CardSelectionFanProfile(
            Vector2 viewportCenter,
            float cameraDistance,
            float halfViewportWidth,
            float edgeLift,
            float maximumFanAngle,
            float cardScale,
            float hoverViewportLift,
            float hoverCameraPull,
            float poseLerp,
            int baseSortingOrder)
        {
            this.viewportCenter = viewportCenter;
            this.cameraDistance = cameraDistance;
            this.halfViewportWidth = halfViewportWidth;
            this.edgeLift = edgeLift;
            this.maximumFanAngle = maximumFanAngle;
            this.cardScale = cardScale;
            this.hoverViewportLift = hoverViewportLift;
            this.hoverCameraPull = hoverCameraPull;
            this.poseLerp = poseLerp;
            this.baseSortingOrder = baseSortingOrder;
        }

        internal static CardSelectionFanProfile CreateTwoCardDefault()
        {
            return new CardSelectionFanProfile(
                new Vector2(0.5f, 0f),
                cameraDistance: 1.5f,
                halfViewportWidth: 0.075f,
                edgeLift: -0.025f,
                maximumFanAngle: 10f,
                cardScale: 1f,
                hoverViewportLift: 0.18f,
                hoverCameraPull: 0.1f,
                poseLerp: 14f,
                baseSortingOrder: 80);
        }

        internal static CardSelectionFanProfile CreateTenCardDefault()
        {
            return new CardSelectionFanProfile(
                new Vector2(0.5f, 0.13f),
                cameraDistance: 1.55f,
                halfViewportWidth: 0.25f,
                edgeLift: -0.05f,
                maximumFanAngle: 16f,
                cardScale: 0.58f,
                hoverViewportLift: 0.15f,
                hoverCameraPull: 0.08f,
                poseLerp: 14f,
                baseSortingOrder: 90);
        }

        internal void ClampValues()
        {
            cameraDistance = Mathf.Max(0.01f, cameraDistance);
            halfViewportWidth = Mathf.Max(0f, halfViewportWidth);
            maximumFanAngle = Mathf.Clamp(maximumFanAngle, 0f, 180f);
            cardScale = Mathf.Max(0.01f, cardScale);
            hoverViewportLift = Mathf.Max(0f, hoverViewportLift);
            hoverCameraPull = Mathf.Clamp(
                hoverCameraPull,
                0f,
                cameraDistance - 0.01f);
            poseLerp = Mathf.Max(0.01f, poseLerp);
        }

        internal CardSelectionFanPose Evaluate(
            int index,
            int count,
            bool hovered)
        {
            float normalized = count == 1
                ? 0f
                : Mathf.Lerp(-1f, 1f, index / (count - 1f));
            Vector2 position = new Vector2(
                viewportCenter.x + normalized * halfViewportWidth,
                viewportCenter.y + edgeLift * normalized * normalized);
            float distance = cameraDistance;
            float curveDirection = edgeLift < 0f ? -1f : 1f;
            float angle = -normalized * maximumFanAngle * curveDirection;
            if (hovered)
            {
                position.y += hoverViewportLift;
                distance -= hoverCameraPull;
                angle = 0f;
            }

            return new CardSelectionFanPose(
                position,
                distance,
                angle,
                cardScale,
                poseLerp,
                baseSortingOrder);
        }
    }

    [DisallowMultipleComponent]
    public sealed class CardSelectionFanLayout : MonoBehaviour
    {
        [SerializeField] private CardSelectionFanProfile twoCardProfile =
            CardSelectionFanProfile.CreateTwoCardDefault();
        [SerializeField] private CardSelectionFanProfile tenCardProfile =
            CardSelectionFanProfile.CreateTenCardDefault();

        private void OnValidate()
        {
            EnsureProfiles();
            twoCardProfile.ClampValues();
            tenCardProfile.ClampValues();
        }

        internal bool TryGetPose(
            CardSelectionFanPreset preset,
            int index,
            int count,
            bool hovered,
            out CardSelectionFanPose pose)
        {
            if (!Enum.IsDefined(typeof(CardSelectionFanPreset), preset) ||
                count <= 0 ||
                index < 0 ||
                index >= count)
            {
                pose = default;
                return false;
            }

            EnsureProfiles();
            pose = ResolveProfile(preset).Evaluate(index, count, hovered);
            return true;
        }

        internal int GetBaseSortingOrder(CardSelectionFanPreset preset)
        {
            EnsureProfiles();
            return ResolveProfile(preset).Evaluate(0, 1, false).BaseSortingOrder;
        }

        private CardSelectionFanProfile ResolveProfile(
            CardSelectionFanPreset preset)
        {
            return preset == CardSelectionFanPreset.TenCards
                ? tenCardProfile
                : twoCardProfile;
        }

        private void EnsureProfiles()
        {
            twoCardProfile ??= CardSelectionFanProfile.CreateTwoCardDefault();
            tenCardProfile ??= CardSelectionFanProfile.CreateTenCardDefault();
        }
    }
}
