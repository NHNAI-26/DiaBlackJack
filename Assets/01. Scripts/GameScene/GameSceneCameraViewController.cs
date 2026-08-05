using System;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DiaBlackJack.GameScene
{
    public enum GameSceneCameraView
    {
        Current = 0,
        TableTop = 1,
        EnemyFocus = 2
    }

    [DisallowMultipleComponent]
    public sealed class GameSceneCameraViewController : MonoBehaviour
    {
        private const int MinViewIndex = (int)GameSceneCameraView.Current;
        private const int MaxViewIndex = (int)GameSceneCameraView.EnemyFocus;
        private const int MaxSwitchInputViewIndex = (int)GameSceneCameraView.TableTop;

        [Header("Cinemachine")]
        [SerializeField] private CinemachineBrain brain;
        [SerializeField] private CinemachineCamera currentCamera;
        [SerializeField] private CinemachineCamera tableTopCamera;
        [SerializeField] private CinemachineCamera enemyFocusCamera;

        [Header("Input")]
        [SerializeField] private GameSceneCameraView defaultView = GameSceneCameraView.Current;
        [SerializeField] private bool switchInputEnabled = true;

        [Header("Blend")]
        [SerializeField] private float transitionSeconds = 0.45f;
        [SerializeField] private CinemachineBlendDefinition.Styles transitionStyle =
            CinemachineBlendDefinition.Styles.Custom;
        [SerializeField] private AnimationCurve transitionCurve =
            AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private int _switchInputLockCount;

        public GameSceneCameraView CurrentView { get; private set; } = GameSceneCameraView.Current;

        internal bool IsTransitioning
        {
            get
            {
                EnsureBrainReference();
                return brain != null && brain.IsBlending;
            }
        }

        private void Reset()
        {
            EnsureBrainReference();
        }

        private void OnValidate()
        {
            transitionSeconds = Mathf.Max(0f, transitionSeconds);
            transitionCurve ??= AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            EnsureBrainReference();
            ApplyBlendSettings();
        }

        private void Start()
        {
            ApplyBlendSettings();
            SetView(defaultView, instant: true);
        }

        private void OnDisable()
        {
            _switchInputLockCount = 0;
        }

        private void Update()
        {
            if (!CanUseSwitchInput)
                return;

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (keyboard.wKey.wasPressedThisFrame)
                StepView(1);
            else if (keyboard.sKey.wasPressedThisFrame)
                StepView(-1);
        }

        public bool SetView(GameSceneCameraView view, bool instant = false)
        {
            int viewIndex = (int)view;
            if (viewIndex < MinViewIndex || viewIndex > MaxViewIndex)
                return false;

            CinemachineCamera camera = GetCamera(view);
            if (camera == null)
            {
                Debug.LogWarning($"[GameSceneCameraViewController] Camera reference is missing for {view}.", this);
                return false;
            }

            ApplyBlendSettings();
            camera.Prioritize();
            CurrentView = view;

            if (instant)
            {
                EnsureBrainReference();
                if (brain != null)
                    brain.ResetState();
            }

            return true;
        }

        public bool StepView(int delta)
        {
            if (delta == 0)
                return false;

            int nextIndex = Mathf.Clamp(
                (int)CurrentView + Math.Sign(delta),
                MinViewIndex,
                MaxSwitchInputViewIndex);
            if (nextIndex == (int)CurrentView)
                return false;

            return SetView((GameSceneCameraView)nextIndex);
        }

        public void LockSwitchInputForSeconds(float seconds)
        {
            if (!Application.isPlaying || seconds <= 0f)
                return;

            LockSwitchInput();
            StartCoroutine(UnlockSwitchInputAfterSeconds(seconds));
        }

        public void LockSwitchInput()
        {
            _switchInputLockCount++;
        }

        public void UnlockSwitchInput()
        {
            _switchInputLockCount = Mathf.Max(0, _switchInputLockCount - 1);
        }

        private CinemachineCamera GetCamera(GameSceneCameraView view)
        {
            switch (view)
            {
                case GameSceneCameraView.Current:
                    return currentCamera;
                case GameSceneCameraView.TableTop:
                    return tableTopCamera;
                case GameSceneCameraView.EnemyFocus:
                    return enemyFocusCamera;
                default:
                    return null;
            }
        }

        private void EnsureBrainReference()
        {
            if (brain != null)
                return;

            brain = GetComponentInChildren<CinemachineBrain>();
            if (brain != null)
                return;

            Camera mainCamera = Camera.main;
            if (mainCamera != null)
                brain = mainCamera.GetComponent<CinemachineBrain>();
        }

        private bool CanUseSwitchInput => switchInputEnabled && _switchInputLockCount <= 0;

        private IEnumerator UnlockSwitchInputAfterSeconds(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            UnlockSwitchInput();
        }

        private void ApplyBlendSettings()
        {
            EnsureBrainReference();
            if (brain == null)
                return;

            CinemachineBlendDefinition blend = brain.DefaultBlend;
            blend.Style = transitionStyle;
            blend.Time = Mathf.Max(0f, transitionSeconds);
            blend.CustomCurve = transitionCurve ?? AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            brain.DefaultBlend = blend;
        }
    }
}
