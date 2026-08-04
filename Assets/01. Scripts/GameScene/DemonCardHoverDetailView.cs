using DiaBlackJack.CoreLoop.UI;
using UnityEngine;

namespace DiaBlackJack.GameScene
{
    [DisallowMultipleComponent]
    public sealed class DemonCardHoverDetailView : MonoBehaviour
    {
        [SerializeField] private GameHudContractDetailView detailView;

        public GameHudContractDetailView DetailView => detailView;

        public bool HasRequiredReferences =>
            detailView != null &&
            detailView.HasRequiredReferences &&
            detailView.HasEnglishNameReference;

        public void Render(
            GameSceneCombatHudContractCandidateViewModel model,
            Sprite faceSprite)
        {
            if (detailView != null)
            {
                detailView.Render(model, faceSprite);
            }
        }

        public void Render(
            GameSceneDemonCardViewModel model,
            Sprite faceSprite)
        {
            if (detailView != null)
            {
                detailView.Render(model, faceSprite);
            }
        }
    }
}
