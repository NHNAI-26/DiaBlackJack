using System;
using System.Collections.Generic;
using DiaBlackJack.CoreLoop;
using DiaBlackJack.CoreLoop.UI;
using TMPro;
using UnityEngine;

namespace DiaBlackJack.GameScene
{
    /// <summary>
    /// World-space renderer for a standalone CoreLoop battle. Card visuals are spawned under two
    /// scene-placed anchors (<see cref="playerHandAnchor"/> / <see cref="enemyHandAnchor"/>) that the
    /// designer positions over the table — the camera and anchors are authored in the scene, never
    /// moved from code. Cards are laid out center-aligned along the anchor's local X. Souls/round
    /// render on a screen canvas. Actions are temporary IMGUI buttons (the project uses the new Input
    /// System only, so legacy OnMouseDown / Input.GetKey do not fire); diegetic clicks come later.
    /// The view owns no game state: <see cref="Render"/> stores the model and input is raised upward.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameSceneView : MonoBehaviour
    {
        [Header("Scene-placed hand anchors (positioned by the designer over the table)")]
        [SerializeField] private Transform playerHandAnchor;
        [SerializeField] private Transform enemyHandAnchor;

        [Header("Card layout")]
        [SerializeField] private float cardSpacing = 1.1f;
        [SerializeField] private Vector2 cardSize = new Vector2(0.9f, 1.3f);
        [SerializeField] private float cardDepthStagger = 0.01f;
        [SerializeField] private Color cardFrontColor = new Color(0.96f, 0.95f, 0.9f);
        [SerializeField] private Color cardBackColor = new Color(0.55f, 0.12f, 0.14f);
        [SerializeField] private Color cardRankColor = new Color(0.1f, 0.05f, 0.05f);

        public event Action HitRequested;
        public event Action StandRequested;
        public event Action RestartRequested;

        private Material _frontMaterial;
        private Material _backMaterial;

        private TextMeshProUGUI _playerSoulText;
        private TextMeshProUGUI _enemySoulText;
        private TextMeshProUGUI _roundText;

        private readonly List<GameObject> _playerCards = new List<GameObject>();
        private readonly List<GameObject> _enemyCards = new List<GameObject>();

        private bool _inputLocked;
        private GameSceneViewModel _model;
        private GUIStyle _buttonStyle;

        private void Awake()
        {
            _frontMaterial = CreateUnlitMaterial(cardFrontColor);
            _backMaterial = CreateUnlitMaterial(cardBackColor);
            BuildCanvas();
        }

        public void Render(GameSceneViewModel model)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            CoreLoopViewModel core = model.Core;

            _playerSoulText.text = $"YOU\n{core.PlayerSoul}";
            _enemySoulText.text = $"ENEMY\n{core.EnemySoul}";
            _roundText.text = BuildRoundText(core);

            RebuildHand(playerHandAnchor, _playerCards, model.PlayerCards);
            RebuildHand(enemyHandAnchor, _enemyCards, model.EnemyCards);
        }

        public void SetInputLocked(bool inputLocked)
        {
            _inputLocked = inputLocked;
        }

        private static string BuildRoundText(CoreLoopViewModel core)
        {
            switch (core.Outcome)
            {
                case BattleOutcome.PlayerVictory:
                    return "VICTORY";
                case BattleOutcome.PlayerDefeat:
                    return "DEFEAT";
                default:
                    return $"ROUND {core.RoundNumber}";
            }
        }

        private void OnGUI()
        {
            if (_model == null)
            {
                return;
            }

            _buttonStyle ??= new GUIStyle(GUI.skin.button) { fontSize = 20, fixedHeight = 46f };

            CoreLoopViewModel core = _model.Core;
            bool live = !_inputLocked;
            bool ended = core.State == CoreLoopState.BattleEnded;

            const float w = 150f;
            const float h = 46f;
            float y = Screen.height - h - 24f;
            float cx = Screen.width * 0.5f;

            if (ended)
            {
                using (new GUIEnabledScope(live && core.CanRestart))
                {
                    if (GUI.Button(new Rect(cx - w * 0.5f, y, w, h), "RESTART", _buttonStyle))
                    {
                        RestartRequested?.Invoke();
                    }
                }

                return;
            }

            using (new GUIEnabledScope(live && core.CanHit))
            {
                if (GUI.Button(new Rect(cx - w - 8f, y, w, h), "HIT", _buttonStyle))
                {
                    HitRequested?.Invoke();
                }
            }

            using (new GUIEnabledScope(live && core.CanStand))
            {
                if (GUI.Button(new Rect(cx + 8f, y, w, h), "STAND", _buttonStyle))
                {
                    StandRequested?.Invoke();
                }
            }
        }

        private void RebuildHand(
            Transform anchor,
            List<GameObject> pool,
            IReadOnlyList<GameSceneCardViewModel> cards)
        {
            foreach (GameObject go in pool)
            {
                Destroy(go);
            }

            pool.Clear();

            if (anchor == null)
            {
                return;
            }

            float offset = -(cards.Count - 1) * 0.5f * cardSpacing;
            for (int i = 0; i < cards.Count; i++)
            {
                GameObject go = BuildCard(cards[i]);
                go.transform.SetParent(anchor, false);
                go.transform.localPosition = new Vector3(offset + i * cardSpacing, 0f, i * cardDepthStagger);
                go.transform.localRotation = Quaternion.identity;
                pool.Add(go);
            }
        }

        private GameObject BuildCard(GameSceneCardViewModel card)
        {
            var root = new GameObject(card.IsFaceUp ? $"Card_{card.Rank}" : "Card_Hidden");

            GameObject face = GameObject.CreatePrimitive(PrimitiveType.Quad);
            face.name = "Face";
            Destroy(face.GetComponent<Collider>());
            face.transform.SetParent(root.transform, false);
            face.transform.localScale = new Vector3(cardSize.x, cardSize.y, 1f);
            face.GetComponent<MeshRenderer>().sharedMaterial =
                card.IsFaceUp ? _frontMaterial : _backMaterial;

            if (card.IsFaceUp)
            {
                var label = new GameObject("Rank");
                label.transform.SetParent(root.transform, false);
                // The card quad faces -Z, so the camera reads it from behind: push the label to the
                // camera side and flip it 180° about Y so the rank is not mirrored.
                label.transform.localPosition = new Vector3(0f, 0f, 0.02f);
                label.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                var tmp = label.AddComponent<TextMeshPro>();
                tmp.text = card.Rank.ToString();
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.fontSize = 9f;
                tmp.color = cardRankColor;
                tmp.rectTransform.sizeDelta = new Vector2(cardSize.x, cardSize.y);
                ApplyFont(tmp);
            }

            return root;
        }

        private void BuildCanvas()
        {
            var canvasGo = new GameObject("GameSceneCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();

            _playerSoulText = NewLabel(canvas.transform, "PlayerSoul",
                new Vector2(0f, 1f), new Vector2(24f, -20f), TextAlignmentOptions.TopLeft);
            _enemySoulText = NewLabel(canvas.transform, "EnemySoul",
                new Vector2(1f, 1f), new Vector2(-24f, -20f), TextAlignmentOptions.TopRight);
            _roundText = NewLabel(canvas.transform, "Round",
                new Vector2(0.5f, 1f), new Vector2(0f, -20f), TextAlignmentOptions.Top);
        }

        private TextMeshProUGUI NewLabel(
            Transform parent,
            string name,
            Vector2 anchor,
            Vector2 anchoredPosition,
            TextAlignmentOptions alignment)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.alignment = alignment;
            tmp.fontSize = 32f;
            tmp.color = Color.white;
            tmp.enableWordWrapping = false;
            ApplyFont(tmp);

            RectTransform rect = tmp.rectTransform;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.sizeDelta = new Vector2(360f, 90f);
            rect.anchoredPosition = anchoredPosition;
            return tmp;
        }

        private static void ApplyFont(TMP_Text tmp)
        {
            if (TMP_Settings.defaultFontAsset != null)
            {
                tmp.font = TMP_Settings.defaultFontAsset;
            }
        }

        private static Material CreateUnlitMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            bool urp = shader != null;
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            var material = new Material(shader);
            if (urp)
            {
                material.SetColor("_BaseColor", color);
                material.SetFloat("_Cull", 0f); // double-sided: visible from either anchor facing
            }
            else
            {
                material.color = color;
            }

            return material;
        }

        private readonly struct GUIEnabledScope : IDisposable
        {
            private readonly bool _previous;

            public GUIEnabledScope(bool enabled)
            {
                _previous = GUI.enabled;
                GUI.enabled = enabled;
            }

            public void Dispose()
            {
                GUI.enabled = _previous;
            }
        }
    }
}
