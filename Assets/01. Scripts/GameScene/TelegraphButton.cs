using UnityEngine;
using UnityEngine.Events;

namespace DiaBlackJack.GameScene
{
    public enum TelegraphButtonKind
    {
        NewGame = 0,
        Tutorial = 1,
        Setting = 2
    }

    /// <summary>
    /// A collider-backed telegraph button. The empty UnityEvent is intentional:
    /// each option can be wired to a scene action from the prefab inspector.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class TelegraphButton : MonoBehaviour
    {
        [SerializeField] private TelegraphButtonKind buttonKind;
        [SerializeField] private UnityEvent onClicked = new UnityEvent();

        private Telegraph _telegraph;

        public TelegraphButtonKind ButtonKind => buttonKind;
        public Telegraph Telegraph => _telegraph;

        internal float TargetAngle => buttonKind switch
        {
            TelegraphButtonKind.NewGame => 45f,
            TelegraphButtonKind.Tutorial => -45f,
            TelegraphButtonKind.Setting => -135f,
            _ => Telegraph.NoHoverAngle
        };

        private void Awake()
        {
            AutoBindTelegraph();
        }

        private void OnEnable()
        {
            AutoBindTelegraph();
        }

        internal void InvokeClick()
        {
            onClicked?.Invoke();
        }

        private void AutoBindTelegraph()
        {
            _telegraph ??= GetComponentInParent<Telegraph>();
        }

        private void OnValidate()
        {
            AutoBindTelegraph();
        }
    }
}
