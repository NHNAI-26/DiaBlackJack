using System;
using System.Collections;
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
            _view.ChangeRequested += RequestBeginChange;
            _view.ChangeCandidateRequested += RequestSelectChangedCard;
            _view.CardUseRequested += RequestBeginCardUse;
            _view.CardEffectChoiceRequested += RequestResolveCardChoice;
            _view.DemonContractBeginRequested += RequestBeginDemonContract;
            _view.DemonContractChoiceRequested += RequestResolveDemonContract;
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
            if (_view == null)
            {
                return;
            }

            _view.HitRequested -= RequestHit;
            _view.StandRequested -= RequestStand;
            _view.ChangeRequested -= RequestBeginChange;
            _view.ChangeCandidateRequested -= RequestSelectChangedCard;
            _view.CardUseRequested -= RequestBeginCardUse;
            _view.CardEffectChoiceRequested -= RequestResolveCardChoice;
            _view.DemonContractBeginRequested -= RequestBeginDemonContract;
            _view.DemonContractChoiceRequested -= RequestResolveDemonContract;
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

        public void RequestBeginCardUse(int cardId)
        {
            ProcessInput(() => IsStageBattle
                ? _stageSession.TryBeginPlayerCardUse(cardId)
                : _session.TryBeginPlayerCardUse(cardId));
        }

        public void RequestResolveCardChoice(int optionId)
        {
            ProcessInput(() => IsStageBattle
                ? _stageSession.TryResolvePlayerCardChoice(optionId)
                : _session.TryResolvePlayerCardChoice(optionId));
        }

        public void RequestBeginDemonContract()
        {
            ProcessInput(() => IsStageBattle
                ? _stageSession.TryBeginPlayerDemonContract()
                : _session.TryBeginPlayerDemonContract());
        }

        public void RequestResolveDemonContract(int interactionId, int optionId)
        {
            ProcessInput(() => IsStageBattle
                ? _stageSession.TryResolvePlayerDemonContract(interactionId, optionId)
                : _session.TryResolvePlayerDemonContract(interactionId, optionId));
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
                BlackjackDeck.CreateStandard(battleSeed + 1),
                playerDemonDeck: DemonContractDeck.CreatePrototype(
                    battleSeed + 1000));
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
                StartCoroutine(UnlockInputNextFrame());
            }
        }

        private void RefreshView()
        {
            string profileKey = IsStageBattle
                ? _stageSession.ActiveStage?.BattleProfileKey
                : null;
            CurrentViewModel = CoreLoopPresenter.Create(Battle, profileKey);
            _view.Render(CurrentViewModel);
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
