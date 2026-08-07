using System;
using System.Collections.Generic;
using DiaBlackJack.CoreLoop;

namespace DiaBlackJack.GameScene
{
    /// <summary>
    /// Drives the first-play tutorial's scripted sequence: shows the user's 0-6 section
    /// dialogue one line at a time through <see cref="TutorialNarratorView"/>, and — between
    /// dialogue blocks — opens a single-action gate on <see cref="GameManager"/> (which
    /// specific Hit/Stand/Change/revolver-number/contract-candidate/contract-option is legal)
    /// and waits for the player to actually perform it before advancing.
    ///
    /// Not a MonoBehaviour — mirrors <see cref="EnemySpeechDirector"/>'s shape: <see
    /// cref="GameManager"/> owns and constructs it, and drives it by calling <see
    /// cref="Observe"/> once per render (same points that already drive EnemySpeechDirector),
    /// rather than this type independently subscribing to <c>CoreLoopBattle.Stepped</c> and
    /// racing GameManager's own consumption of that event.
    ///
    /// Two bracketed stage directions in the user's script could not be reproduced exactly as
    /// written without weakening real engine rules, and are handled invisibly instead (see
    /// <see cref="TutorialEnemyPolicy"/> for both): the enemy's round-2 "리볼버" card is
    /// mechanically a Crystal Orb (rank 5 is Crystal Orb, not AutoPistol, in the current
    /// catalog) — harmless, since AutoPistol's guess is a pure rank comparison and doesn't
    /// care what the target card's own effect is. And the enemy's round-3 second action is a
    /// second silent Hit instead of the scripted Change, because <c>ShouldEnemyChange()</c>
    /// only allows an AI-initiated Change when its hidden card is already revealed or it is
    /// already bust; neither holds here, and forcing it would weaken a deliberate AI-fairness
    /// gate for one flavor beat that is never shown as narrator text anyway.
    /// </summary>
    internal sealed class TutorialDirector
    {
        // Any value that is never Hit/Stand/BeginChange makes all three fail the restriction's
        // equality check — i.e. "no primary action is legal", without needing a dedicated
        // tri-state on top of GameSceneCombatHudCommandKind?.
        private const GameSceneCombatHudCommandKind BlockAllPrimaryActions =
            GameSceneCombatHudCommandKind.BeginContract;

        private const int AsmodeusForceHitOptionId = 1;

        private readonly GameManager _gameManager;
        private readonly TutorialNarratorView _narrator;
        private readonly List<Step> _steps;

        private bool _begun;
        private int _stepIndex = -1;
        private int _dialogueLineIndex;
        private bool _gateActive;
        private BattleSnapshot _gateEntrySnapshot;

        public TutorialDirector(GameManager gameManager, TutorialNarratorView narrator)
        {
            _gameManager = gameManager ?? throw new ArgumentNullException(nameof(gameManager));
            _narrator = narrator ?? throw new ArgumentNullException(nameof(narrator));
            _narrator.LineAdvanceRequested += HandleLineAdvanceRequested;
            _steps = BuildSteps();
        }

        /// <summary>Fires once the intro (sections 0-1) dialogue's last line is dismissed.</summary>
        public event Action IntroCompleted;

        public bool IsFinished => _begun && _stepIndex >= _steps.Count;

        public void BeginIntro()
        {
            if (_begun)
            {
                return;
            }

            _begun = true;
            AdvanceStep();
        }

        /// <summary>
        /// Call once per render (GameManager already drives <see cref="EnemySpeechDirector"/>
        /// from the same points). No-ops outside an active gate step.
        /// </summary>
        public void Observe()
        {
            if (!_begun || !_gateActive || _stepIndex >= _steps.Count)
            {
                return;
            }

            CoreLoopBattle battle = _gameManager.Battle;
            if (battle == null)
            {
                return;
            }

            var current = new BattleSnapshot(battle);
            if (current.Equals(_gateEntrySnapshot))
            {
                return;
            }

            _steps[_stepIndex].OnExit?.Invoke(_gameManager);
            _gateActive = false;
            AdvanceStep();
        }

        private void HandleLineAdvanceRequested()
        {
            if (!_begun || _stepIndex < 0 || _stepIndex >= _steps.Count)
            {
                return;
            }

            Step step = _steps[_stepIndex];
            if (step.Kind != StepKind.Dialogue)
            {
                return;
            }

            bool wasIntroStep = step.IsIntro;
            _dialogueLineIndex++;
            if (_dialogueLineIndex < step.Lines.Length)
            {
                _narrator.ShowLine(step.Lines[_dialogueLineIndex]);
                return;
            }

            AdvanceStep();
            if (wasIntroStep)
            {
                IntroCompleted?.Invoke();
            }
        }

        private void AdvanceStep()
        {
            _stepIndex++;
            if (_stepIndex >= _steps.Count)
            {
                _narrator.Hide();
                return;
            }

            Step step = _steps[_stepIndex];
            if (step.Kind == StepKind.Dialogue)
            {
                _dialogueLineIndex = 0;
                _narrator.ShowLine(step.Lines[0]);
                return;
            }

            _narrator.Hide();
            _gateEntrySnapshot = new BattleSnapshot(_gameManager.Battle);
            _gateActive = true;
            step.OnEnter?.Invoke(_gameManager);
        }

        private static List<Step> BuildSteps()
        {
            return new List<Step>
            {
                Dialogue(
                    isIntro: true,
                    "드디어 정신을 차렸군.",
                    "넌 지금부터 아주 간단한 게임을 하게 될 거야. 블랙잭.",
                    "그런데 … 그냥 하면 재미 없잖아? 뭐 하나 걸고 하는 편이 스릴 있지 않겠어?",
                    "돈? 그런 건 따분하지.",
                    "네 목숨. 그래, 그게 좋겠네.",
                    "승부에서 패배하면, 패자는 죽는다.",
                    "살기 위해서 어떤 방법이든 써보라고. 총이든, 칼이든, 아니면 우리 악마들의 힘이든 …",
                    "블랙잭은 자신의 카드 합을 21에 가깝게 만드는 게임이야.",
                    "하지만 절대 21을 넘겨서는 안 되지.",
                    "21을 넘기지 않으면서, 상대보다 21에 가깝게 맞춘 사람이 그 판을 가져간다. 간단하지?",
                    "한 번 해보자고."),

                // Section 2, part 1 — round 1 is already dealt by the time this shows.
                Dialogue(
                    "라운드가 시작되면 비공개 카드와 공개 카드를 1장씩 받아.",
                    "비공개 카드는 자신만 알 수 있지만, 공개 카드는 누구든지 알 수 있지.",
                    "비공개 카드는 커서를 올리면 바로 알 수 있으니 항상 참고하도록.",
                    "[히트] 버튼을 누르면, 자신의 덱에서 카드 한 장을 뽑아 공개 카드에 추가할 수 있어."),
                PrimaryActionGate(GameSceneCombatHudCommandKind.Hit),

                Dialogue(
                    "내 턴에 행동을 하나 선택하면, 상대 턴으로 차례가 넘어간다.",
                    "이번엔 상대도 히트를 했군. 21까진 아직 모자라니 한 번 더 [히트]해봐."),
                PrimaryActionGate(GameSceneCombatHudCommandKind.Hit),

                Dialogue(
                    "A 카드라, 운이 좋군.",
                    "A 카드는 1과 11 중 21에 가깝게 자동으로 설정되는 카드지. 딱 마침 합이 21이 됐군.",
                    "앞으로 어떤 카드가 들어올지 궁금하다면 네 덱을 클릭해봐. 어떤 카드가 남아있는지 알 수 있을테니.",
                    "숫자 합이 21이 되었으니, 더 이상 진행할 필요는 없겠지.",
                    "게임 도중 '공개 카드 숫자의 합'이 21 이상이 되면, [버스트]한다. 패배한다는 뜻이지.",
                    "넌 이미 카드 숫자 합이 21이 되었으니 차례를 더 진행할 필요는 없겠군.",
                    "[스탠드]를 선택하면 이번 라운드 내 차례를 종료할 수 있어.",
                    "단, 상대는 혼자서 계속 차례를 진행할 수 있게 되니 반드시 모든 행동을 다 진행했을 때만 선택하도록.",
                    "이번엔 더 이상 할 수 있는 게 없으니 [스탠드]하자고."),
                PrimaryActionGate(GameSceneCombatHudCommandKind.Stand),

                // Section 3, continued — round 1 has resolved by now.
                Dialogue(
                    "패자는 대가를 치뤄야겠지.",
                    "라운드에서 패배하면 영혼 1을 잃게 된다.",
                    "만약 둘 다 21을 넘기면, 둘 다 패배한다.",
                    "영혼을 모두 잃으면 <b>죽는다.</b> 당연한 일이지.",
                    "21을 넘기지 않았는데 무승부면 어떻게 되냐고? 참나, 왜 이렇게 캐물어?",
                    "그래, 기분이다. 그런 경우에는 무조건 네가 승리하게 해줄게. 됐니?"),

                // Section 4 — round 2 is already dealt.
                Dialogue("한참 모자라는군. [히트]해보라고."),
                PrimaryActionGate(GameSceneCombatHudCommandKind.Hit),

                Dialogue(
                    "상대가 바보 같이 비공개 카드를 사용해버렸군. 리볼버를 클릭해서 저 녀석 면상에 총알을 박아보자고.",
                    "카드 게임에서 무기를 쓰는 게 어딨냐고? 이봐, 성자라도 상대하고 있는 줄 알았어?",
                    "5~10 사이의 카드들은 내 차례에 사용할 수 있는 카드들이다. 공개 카드든, 비공개 카드든 클릭하면 사용할 수 있지.",
                    "다만 비공개 카드를 사용하려면 <b>네 카드를 상대에게 보여줘야 한다.</b> 그 리스크를 감수할 수 있다면 사용해봐.",
                    "리볼버를 사용하면 상대의 비공개 카드를 추측할 수 있는 기회를 갖는다. 맞힌다면 상대를 [버스트]시킬 수 있지.",
                    "저 녀석의 비공개 카드는 5. 자, 죽이자고."),
                RevolverGate(),

                // Section 5 — round 3 is already dealt.
                Dialogue(
                    "19라. 히트하기엔 너무 크고, 스탠드하기엔 불안한 숫자군.",
                    "이럴 땐 [체인지]를 하는 것도 방법이지.",
                    "[체인지]를 선택하면 자신의 덱에서 2장을 뽑아 나만 확인하고, 그 중 1장을 비공개 카드로 바꿀 수 있다.",
                    "기존 비공개 카드와 선택하지 않은 카드는 버려진다.",
                    "[체인지]는 한 스테이지 내에서 단 한 번은 무료지만, 그 다음부터는 비용으로 지불해야 하는 영혼이 1씩 오르게 된다.",
                    "강력한 행동이니만큼, 어떤 게 더 좋을지는 생각해보라고.",
                    "일단 지금은 공짜니까 한 번 해봐."),
                PrimaryActionGate(GameSceneCombatHudCommandKind.BeginChange),

                // Section 6 — the enemy's own Hit already ran synchronously as part of the
                // Change gate above.
                Dialogue(
                    "아 … 따분하군. 따분해.",
                    "이봐, 좀 더 매력적인 게임을 하고 싶지 않아?",
                    "조금 더 악랄하고 … 강력한 … 그런 게임 말이야.",
                    "좋지? 그래. 좋아할 줄 알았어. 내가 다 기쁘네.",
                    "우측의 계약서를 클릭하면, 너의 악마 덱에 있는 악마 카드 중 2장을 뽑고 그 중 1명과 계약할 수 있지.",
                    "나와 계약하자. 누구도 상상할 수 없는 강한 힘을 주지."),
                ContractCandidateGate(DemonContractCatalog.AsmodeusKey),

                Dialogue(
                    "자, 계약은 성사됐다.",
                    "악마와 계약하면 스테이지 내내 강력한 힘을 얻게 되지만, 그에 상응하는 대가도 함께 얻게 되지.",
                    "나의 힘은 '자신의 차례를 시작할 때, 상대를 강제로 히트시키는 능력'이다.",
                    "대신 '더 이상 7 이하의 카드'를 사용할 수 없지.",
                    "계약할 때는 그런 말 없지 않았냐고? 이봐, 나는 악마라고.",
                    "모든 악마들은 저마다의 능력과 대가를 가지고 있다. 원한다면, 좌측의 도감을 눌러 모든 악마들과 상대의 정보를 확인해봐.",
                    "저 멍청이한테 카드 하나 선물해주자고."),
                ContractOptionGate(AsmodeusForceHitOptionId),

                Dialogue(
                    "축하해. 첫 데뷔전을 성공적으로 치뤘군.",
                    "이런 식으로 총 3번 승리하면 널 이 곳에서 내보내주지.",
                    "스테이지 도중 깎인 영혼은 스테이지 끝났다고 자동으로 회복시켜주지 않으니 명심하도록.",
                    "그럼 무운을 빌게. 또 만나자고.")
            };
        }

        private static Step Dialogue(params string[] lines)
        {
            return Dialogue(isIntro: false, lines);
        }

        private static Step Dialogue(bool isIntro, params string[] lines)
        {
            return new Step
            {
                Kind = StepKind.Dialogue,
                Lines = lines,
                IsIntro = isIntro
            };
        }

        private static Step PrimaryActionGate(GameSceneCombatHudCommandKind allowedAction)
        {
            return new Step
            {
                Kind = StepKind.Gate,
                OnEnter = gm => gm.SetTutorialActionRestriction(allowedAction),
                OnExit = gm => gm.SetTutorialActionRestriction(null)
            };
        }

        private static Step RevolverGate()
        {
            return new Step
            {
                Kind = StepKind.Gate,
                OnEnter = gm =>
                {
                    gm.SetTutorialActionRestriction(BlockAllPrimaryActions);
                    gm.SetTutorialRevolverForcedNumber(5);
                },
                OnExit = gm =>
                {
                    gm.SetTutorialActionRestriction(null);
                    gm.SetTutorialRevolverForcedNumber(null);
                }
            };
        }

        private static Step ContractCandidateGate(string definitionKey)
        {
            return new Step
            {
                Kind = StepKind.Gate,
                OnEnter = gm =>
                {
                    gm.SetTutorialActionRestriction(BlockAllPrimaryActions);
                    gm.SetTutorialContractPaperBlocked(false);
                    gm.SetTutorialContractRestriction(definitionKey);
                },
                OnExit = gm =>
                {
                    gm.SetTutorialActionRestriction(null);
                    gm.SetTutorialContractPaperBlocked(true);
                    gm.SetTutorialContractRestriction(null);
                }
            };
        }

        private static Step ContractOptionGate(int optionId)
        {
            return new Step
            {
                Kind = StepKind.Gate,
                OnEnter = gm => gm.SetTutorialContractOptionRestriction(optionId),
                OnExit = gm => gm.SetTutorialContractOptionRestriction(null)
            };
        }

        private enum StepKind
        {
            Dialogue,
            Gate
        }

        private sealed class Step
        {
            public StepKind Kind;
            public string[] Lines;
            public bool IsIntro;
            public Action<GameManager> OnEnter;
            public Action<GameManager> OnExit;
        }

        private readonly struct BattleSnapshot : IEquatable<BattleSnapshot>
        {
            public BattleSnapshot(CoreLoopBattle battle)
            {
                TurnNumber = battle?.TurnNumber ?? 0;
                RoundNumber = battle?.RoundNumber ?? 0;
                ResolutionId = battle?.LastResolution?.Id ?? 0;
            }

            private int TurnNumber { get; }

            private int RoundNumber { get; }

            private long ResolutionId { get; }

            public bool Equals(BattleSnapshot other)
            {
                return TurnNumber == other.TurnNumber &&
                    RoundNumber == other.RoundNumber &&
                    ResolutionId == other.ResolutionId;
            }

            public override bool Equals(object obj)
            {
                return obj is BattleSnapshot other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = 17;
                    hash = hash * 31 + TurnNumber;
                    hash = hash * 31 + RoundNumber;
                    hash = hash * 31 + ResolutionId.GetHashCode();
                    return hash;
                }
            }
        }
    }
}
