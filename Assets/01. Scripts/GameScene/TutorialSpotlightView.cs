using UnityEngine;
using UnityEngine.UI;

namespace DiaBlackJack.GameScene
{
    [DisallowMultipleComponent]
    internal sealed class TutorialSpotlightView : MaskableGraphic
    {
        private const int SegmentCount = 96;
        internal const float DefaultDimAlpha = 0.72f;
        internal const float DefaultFeatherPixels = 16f;
        private Vector2 _screenCenter;
        private float _screenRadius;
        private float _featherPixels = DefaultFeatherPixels;

        internal static TutorialSpotlightView Create(Transform parent)
        {
            var root = new GameObject(
                "TutorialSpotlight",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TutorialSpotlightView));
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.SetAsLastSibling();

            TutorialSpotlightView view = root.GetComponent<TutorialSpotlightView>();
            view.color = new Color(0f, 0f, 0f, DefaultDimAlpha);
            view.raycastTarget = false;
            view.gameObject.SetActive(false);
            return view;
        }

        internal float FeatherPixels => _featherPixels;

        internal void Show(
            Vector2 screenCenter,
            float screenRadius,
            float dimAlpha = DefaultDimAlpha,
            float featherPixels = DefaultFeatherPixels)
        {
            _screenCenter = screenCenter;
            _screenRadius = Mathf.Max(1f, screenRadius);
            _featherPixels = Mathf.Max(0f, featherPixels);
            color = new Color(0f, 0f, 0f, Mathf.Clamp01(dimAlpha));
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            transform.SetAsLastSibling();
            SetVerticesDirty();
        }

        internal void Hide()
        {
            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
        }

        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();
            RectTransform rectTransform = (RectTransform)transform;
            Rect rect = rectTransform.rect;
            Canvas canvas = canvasRenderer.GetComponentInParent<Canvas>();
            Camera camera = canvas != null &&
                canvas.renderMode != RenderMode.ScreenSpaceOverlay
                    ? canvas.worldCamera
                    : null;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rectTransform, _screenCenter, camera, out Vector2 center))
            {
                return;
            }

            float localPerPixel = Screen.width > 0
                ? rect.width / Screen.width
                : 1f;
            float innerRadius = _screenRadius * localPerPixel;
            float featherRadius = innerRadius + _featherPixels * localPerPixel;
            Color32 dark = color;
            Color32 clear = new Color(color.r, color.g, color.b, 0f);

            for (int index = 0; index < SegmentCount; index++)
            {
                float angle = Mathf.PI * 2f * index / SegmentCount;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 outer = center + direction * DistanceToRectEdge(
                    center, direction, rect);
                helper.AddVert(outer, dark, Vector2.zero);
                helper.AddVert(center + direction * featherRadius, dark, Vector2.zero);
                helper.AddVert(center + direction * innerRadius, clear, Vector2.zero);
            }

            for (int index = 0; index < SegmentCount; index++)
            {
                int next = (index + 1) % SegmentCount;
                int outer = index * 3;
                int feather = outer + 1;
                int inner = outer + 2;
                int nextOuter = next * 3;
                int nextFeather = nextOuter + 1;
                int nextInner = nextOuter + 2;
                helper.AddTriangle(outer, nextOuter, nextFeather);
                helper.AddTriangle(outer, nextFeather, feather);
                helper.AddTriangle(feather, nextFeather, nextInner);
                helper.AddTriangle(feather, nextInner, inner);
            }
        }

        private static float DistanceToRectEdge(
            Vector2 center,
            Vector2 direction,
            Rect rect)
        {
            float horizontal = direction.x > 0f
                ? (rect.xMax - center.x) / direction.x
                : direction.x < 0f
                    ? (rect.xMin - center.x) / direction.x
                    : float.PositiveInfinity;
            float vertical = direction.y > 0f
                ? (rect.yMax - center.y) / direction.y
                : direction.y < 0f
                    ? (rect.yMin - center.y) / direction.y
                    : float.PositiveInfinity;
            return Mathf.Max(0f, Mathf.Min(horizontal, vertical));
        }
    }
}
