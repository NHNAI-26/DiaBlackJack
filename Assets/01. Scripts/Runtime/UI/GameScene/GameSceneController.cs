using System;
using System.Collections;
using DiaBlackJack.CoreLoop;
using UnityEngine;

namespace DiaBlackJack.GameScene
{
    /// <summary>
    /// Drives a standalone CoreLoop battle for the world-space GameScene. Mirrors
    /// <c>CoreLoopController</c>'s input-lock discipline but targets only the standalone session
    /// (no run-flow / StageProgression branch). MVP surface: hit, stand, restart.
    /// The only type here that talks to the session.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(GameSceneView))]
    public sealed class GameSceneController : MonoBehaviour
    {
        [SerializeField] private int seed = 20260719;

        private CoreLoopSession _session;
        private GameSceneView _view;
        private bool _inputLocked;
        private int _battleIndex;

        public CoreLoopBattle Battle => _session?.Battle;

        private void Awake()
        {
            if (!TryGetComponent(out _view))
            {
                throw new MissingComponentException($"{nameof(GameSceneView)} is required.");
            }

            _view.HitRequested += RequestHit;
            _view.StandRequested += RequestStand;
            _view.RestartRequested += RequestRestart;

            _session = new CoreLoopSession(CreateBattle);
        }

        private void Start()
        {
            RefreshView();
        }

        private void OnDestroy()
        {
            if (_view == null)
            {
                return;
            }

            _view.HitRequested -= RequestHit;
            _view.StandRequested -= RequestStand;
            _view.RestartRequested -= RequestRestart;
        }

        public void RequestHit()
        {
            ProcessInput(_session.TryPlayerHit);
        }

        public void RequestStand()
        {
            ProcessInput(_session.TryPlayerStand);
        }

        public void RequestRestart()
        {
            ProcessInput(_session.TryRestart);
        }

        private CoreLoopBattle CreateBattle()
        {
            int battleSeed = seed + (_battleIndex * 2);
            _battleIndex++;
            return new CoreLoopBattle(
                BlackjackDeck.CreateStandard(battleSeed),
                BlackjackDeck.CreateStandard(battleSeed + 1));
        }

        private void ProcessInput(Func<bool> action)
        {
            if (_inputLocked || action == null)
            {
                return;
            }

            _inputLocked = true;
            _view.SetInputLocked(true);
            bool accepted = action();
            RefreshView();

            if (!accepted || !Application.isPlaying)
            {
                UnlockInput();
            }
            else
            {
                StartCoroutine(UnlockInputNextFrame());
            }
        }

        private void RefreshView()
        {
            _view.Render(GameScenePresenter.Create(Battle));
        }

        private void UnlockInput()
        {
            _inputLocked = false;
            _view.SetInputLocked(false);
        }

        private IEnumerator UnlockInputNextFrame()
        {
            yield return null;
            UnlockInput();
        }
    }
}
