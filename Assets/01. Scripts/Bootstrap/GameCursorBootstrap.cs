using UnityEngine;
using UnityEngine.InputSystem;

namespace DiaBlackJack.Bootstrap
{
    [DisallowMultipleComponent]
    internal sealed class GameCursorBootstrap : MonoBehaviour
    {
        private const string DefaultCursorResourcePath = "MouseCursor";
        private const string PressedCursorResourcePath = "MouseCursorPressed";
        private static readonly Vector2 CursorHotspot = new Vector2(24f, 1f);

        private static GameCursorBootstrap _instance;

        private Texture2D _defaultCursor;
        private Texture2D _pressedCursor;
        private bool _isPressed;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateRuntimeInstance()
        {
            if (_instance != null)
            {
                return;
            }

            GameObject runtimeObject =
                new GameObject(nameof(GameCursorBootstrap));
            DontDestroyOnLoad(runtimeObject);
            runtimeObject.AddComponent<GameCursorBootstrap>();
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            _defaultCursor =
                Resources.Load<Texture2D>(DefaultCursorResourcePath);
            _pressedCursor =
                Resources.Load<Texture2D>(PressedCursorResourcePath);

            if (_defaultCursor == null || _pressedCursor == null)
            {
                Debug.LogError(
                    "Game cursor textures are missing from Resources.",
                    this);
                enabled = false;
                return;
            }

            ApplyCursor(false);
        }

        private void Update()
        {
            Mouse mouse = Mouse.current;
            bool isPressed = mouse != null && mouse.leftButton.isPressed;
            if (isPressed == _isPressed)
            {
                return;
            }

            ApplyCursor(isPressed);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus || _defaultCursor == null || _pressedCursor == null)
            {
                return;
            }

            Mouse mouse = Mouse.current;
            ApplyCursor(mouse != null && mouse.leftButton.isPressed);
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void ApplyCursor(bool isPressed)
        {
            _isPressed = isPressed;
            Cursor.SetCursor(
                isPressed ? _pressedCursor : _defaultCursor,
                CursorHotspot,
                CursorMode.ForceSoftware);
        }
    }
}
