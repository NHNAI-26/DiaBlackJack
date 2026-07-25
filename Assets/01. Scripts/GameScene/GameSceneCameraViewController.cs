using System;
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

        [Header("Cinemachine")]
        [SerializeField] private CinemachineBrain brain;
        [SerializeField] private CinemachineCamera currentCamera;
        [SerializeField] private CinemachineCamera tableTopCamera;
        [SerializeField] private CinemachineCamera enemyFocusCamera;

        [Header("Input")]
        [SerializeField] private GameSceneCameraView defaultView = GameSceneCameraView.Current;
        [SerializeField] private bool switchInputEnabled = true;

        public GameSceneCameraView CurrentView { get; private set; } = GameSceneCameraView.Current;

        private void Reset()
        {
            EnsureBrainReference();
        }

        private void OnValidate()
        {
            EnsureBrainReference();
        }

        private void Start()
        {
            SetView(defaultView, instant: true);
        }

        private void Update()
        {
            if (!switchInputEnabled)
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

            int nextIndex = Mathf.Clamp((int)CurrentView + Math.Sign(delta), MinViewIndex, MaxViewIndex);
            if (nextIndex == (int)CurrentView)
                return false;

            return SetView((GameSceneCameraView)nextIndex);
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
    }
}
