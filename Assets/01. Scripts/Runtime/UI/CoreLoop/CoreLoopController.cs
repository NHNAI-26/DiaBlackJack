using System;
using DiaBlackJack.StageProgression;
using DiaBlackJack.StageProgression.UI;
using UnityEngine;

namespace DiaBlackJack.CoreLoop.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CoreLoopView))]
    public sealed class CoreLoopController : MonoBehaviour
    {
        [SerializeField] private int seed = 20260719;

        private CoreLoopSession _session;
        private StageProgressionSession _stageSession;
        private StageProgressionRuntime _stageRuntime;
        private CoreLoopView _view;
        private bool _inputLocked;
        private int _battleIndex;

        public CoreLoopBattle Battle => IsStageBattle ? _stageSession.Battle : _session?.Battle;

        public CoreLoopViewModel CurrentViewModel { get; private set; }

        private void Awake()
        {
            if (!TryGetComponent(out _view))
            {
                throw new MissingComponentException($"{nameof(CoreLoopView)} is required.");
            }

            _view.HitRequested += RequestHit;
            _view.StandRequested += RequestStand;
            _view.FoldRequested += RequestFold;
            _view.ChangeRequested += RequestBeginChange;
            _view.ChangeCandidateRequested += RequestSelectChangedCard;
            _view.RestartRequested += RequestRestart;

            _stageRuntime = StageProgressionRuntime.Instance;
            if (_stageRuntime != null &&
                _stageRuntime.Session != null &&
                _stageRuntime.Session.Progress.State == StageProgressionState.InBattle &&
                _stageRuntime.Session.Battle != null)
            {
                _stageSession = _stageRuntime.Session;
            }
            else
            {
                _session = new CoreLoopSession(CreateBattle);
            }

            RefreshView();
        }

        private void OnDestroy()
        {
            if (Application.isPlaying)
            {
                CancelInvoke(nameof(UnlockInput));
            }

            if (_view == null)
            {
                return;
            }

            _view.HitRequested -= RequestHit;
            _view.StandRequested -= RequestStand;
            _view.FoldRequested -= RequestFold;
            _view.ChangeRequested -= RequestBeginChange;
            _view.ChangeCandidateRequested -= RequestSelectChangedCard;
            _view.RestartRequested -= RequestRestart;
        }

        public void RequestHit()
        {
            ProcessInput(() => IsStageBattle
                ? _stageSession.TryPlayerHit()
                : _session.TryPlayerHit());
        }

        public void RequestStand()
        {
            ProcessInput(() => IsStageBattle
                ? _stageSession.TryPlayerStand()
                : _session.TryPlayerStand());
        }

        public void RequestFold()
        {
            ProcessInput(() => IsStageBattle
                ? _stageSession.TryPlayerFold()
                : _session.TryPlayerFold());
        }

        public void RequestBeginChange()
        {
            ProcessInput(() => IsStageBattle
                ? _stageSession.TryBeginPlayerChange()
                : _session.TryBeginPlayerChange());
        }

        public void RequestSelectChangedCard(int candidateIndex)
        {
            ProcessInput(() => IsStageBattle
                ? _stageSession.TrySelectChangedCard(candidateIndex)
                : _session.TrySelectChangedCard(candidateIndex));
        }

        public void RequestRestart()
        {
            if (IsStageBattle)
            {
                _stageRuntime.LoadProgressionScene();
                return;
            }

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

        private bool IsStageBattle => _stageSession != null;

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

            if (!accepted)
            {
                UnlockInput();
            }
            else if (IsStageBattle &&
                _stageSession.Progress.State != StageProgressionState.InBattle)
            {
                UnlockInput();
                _stageRuntime.LoadProgressionScene();
            }
            else if (!Application.isPlaying)
            {
                UnlockInput();
            }
            else
            {
                Invoke(nameof(UnlockInput), 0f);
            }
        }

        private void RefreshView()
        {
            CurrentViewModel = CoreLoopPresenter.Create(Battle);
            _view.Render(CurrentViewModel);
        }

        private void UnlockInput()
        {
            _inputLocked = false;
            _view.SetInputLocked(false);
        }
    }
}
