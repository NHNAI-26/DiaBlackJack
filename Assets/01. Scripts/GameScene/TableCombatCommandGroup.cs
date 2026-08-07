using System.Collections.Generic;
using UnityEngine;

namespace DiaBlackJack.GameScene
{
    [DisallowMultipleComponent]
    public sealed class TableCombatCommandGroup : MonoBehaviour
    {
        [SerializeField] private TableCombatCommandView hit;
        [SerializeField] private TableCombatCommandView stand;
        [SerializeField] private TableCombatCommandView change;

        public int CommandViewCount => 3;

        public bool HasRequiredReferences =>
            HasCommand(hit, GameSceneCombatHudCommandKind.Hit) &&
            HasCommand(stand, GameSceneCombatHudCommandKind.Stand) &&
            HasCommand(change, GameSceneCombatHudCommandKind.BeginChange);

        public void Render(GameSceneCombatHudViewModel model)
        {
            bool show = model != null &&
                model.Mode == GameSceneCombatHudMode.Actions;
            gameObject.SetActive(show);
            if (!show)
            {
                return;
            }

            IReadOnlyList<GameSceneCombatHudActionViewModel> actions =
                model.PrimaryActions;
            hit.Render(FindAction(actions, GameSceneCombatHudCommandKind.Hit));
            stand.Render(FindAction(actions, GameSceneCombatHudCommandKind.Stand));
            change.Render(FindAction(
                actions,
                GameSceneCombatHudCommandKind.BeginChange));
        }

        public void ResetView()
        {
            hit?.ResetView();
            stand?.ResetView();
            change?.ResetView();
            gameObject.SetActive(false);
        }

        internal TableCombatCommandView GetView(GameSceneCombatHudCommandKind kind)
        {
            switch (kind)
            {
                case GameSceneCombatHudCommandKind.Hit:
                    return hit;
                case GameSceneCombatHudCommandKind.Stand:
                    return stand;
                case GameSceneCombatHudCommandKind.BeginChange:
                    return change;
                default:
                    return null;
            }
        }

        private static bool HasCommand(
            TableCombatCommandView view,
            GameSceneCombatHudCommandKind kind)
        {
            return view != null &&
                view.Kind == kind &&
                view.HasRequiredReferences;
        }

        private static GameSceneCombatHudActionViewModel FindAction(
            IReadOnlyList<GameSceneCombatHudActionViewModel> actions,
            GameSceneCombatHudCommandKind kind)
        {
            if (actions == null)
            {
                return null;
            }

            for (int i = 0; i < actions.Count; i++)
            {
                GameSceneCombatHudActionViewModel action = actions[i];
                if (action != null && action.Command.Kind == kind)
                {
                    return action;
                }
            }

            return null;
        }
    }
}
