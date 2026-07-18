using UnityEngine;

namespace DiaBlackJack.CoreLoop.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CoreLoopView))]
    public sealed class CoreLoopController : MonoBehaviour
    {
        [SerializeField] private int seed = 20260719;

        private CoreLoopSession _session;
        private CoreLoopView _view;
        private bool _inputLocked;
        private int _battleIndex;

        public CoreLoopBattle Battle => _session?.Battle;

        public CoreLoopViewModel CurrentViewModel { get; private set; }

        private void Awake()
        {
            if (!TryGetComponent(out _view))
            {
                throw new MissingComponentException($"{nameof(CoreLoopView)} is required.");
            }

            _view.HitRequested += RequestHit;
            _view.StandRequested += RequestStand;
            _view.RestartRequested += RequestRestart;

            _session = new CoreLoopSession(CreateBattle);
            RefreshView();
        }

        private void OnDestroy()
        {
            CancelInvoke(nameof(UnlockInput));
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

        private void ProcessInput(System.Func<bool> action)
        {
            if (_inputLocked || action == null)
            {
                return;
            }

            _inputLocked = true;
            _view.SetInputLocked(true);
            if (!action())
            {
                UnlockInput();
            }
            else
            {
                Invoke(nameof(UnlockInput), 0f);
            }

            RefreshView();
        }

        private void RefreshView()
        {
            CurrentViewModel = CoreLoopPresenter.Create(_session.Battle);
            _view.Render(CurrentViewModel);
        }

        private void UnlockInput()
        {
            _inputLocked = false;
            _view.SetInputLocked(false);
        }
    }
}
