using System;
using UnityEngine;

namespace DiaBlackJack.GameScene
{
    [DisallowMultipleComponent]
    public sealed class ContractPaperView : MonoBehaviour
    {
        [SerializeField] private ContractPaperClickable[] papers;

        private int _previousVisibleCount = -1;

        public int VisibleCount { get; private set; }

        public bool HasRequiredReferences
        {
            get
            {
                EnsurePapers();
                return papers.Length == 2 &&
                    papers[0] != null &&
                    papers[1] != null;
            }
        }

        private void Awake()
        {
            EnsurePapers();
            Render(null);
        }

        public void Render(ContractPaperViewModel model)
        {
            EnsurePapers();
            int visibleCount = model == null
                ? 0
                : Mathf.Clamp(model.VisibleCount, 0, papers.Length);
            bool canPlayerBegin = model != null && model.CanPlayerBegin;
            string availableDemonNames = model?.AvailableDemonNames ?? "없음";

            // Papers are sorted top (index 0) to bottom (last index) by actual draw
            // order (SortingOrder), not name — the two don't necessarily agree, and
            // sorting by name previously left the visually front-most paper's collider
            // disabled while the paper rendered behind it silently absorbed hover and
            // clicks instead. As the stack shrinks, the top-most paper is the one that
            // goes away first, regardless of which paper the player actually clicked.
            // Only the top-most currently visible paper is hoverable/clickable — the
            // ones underneath are purely visual "how many left" filler.
            int topVisibleIndex = papers.Length - visibleCount;

            // Edit/test-mode renders apply instantly (mirrors DeckStackView.Render)
            // since DOTween sequences never tick outside Play Mode's Update loop —
            // animating here would leave the paper stuck active and never call its
            // onComplete.
            bool animateDecrease =
                Application.isPlaying &&
                _previousVisibleCount >= 0 &&
                visibleCount < _previousVisibleCount;
            int previousTopVisibleIndex = papers.Length - _previousVisibleCount;

            for (int i = 0; i < papers.Length; i++)
            {
                ContractPaperClickable paper = papers[i];
                if (paper == null)
                {
                    continue;
                }

                bool wasVisible = _previousVisibleCount >= 0 &&
                    i >= previousTopVisibleIndex;
                bool visible = i >= topVisibleIndex;
                bool isTopOfStack = i == topVisibleIndex;
                paper.ConfigureAvailableDemonNames(availableDemonNames);
                paper.SetInteractable(visible && isTopOfStack && canPlayerBegin);

                if (animateDecrease && wasVisible && !visible)
                {
                    ContractPaperClickable disappearing = paper;
                    disappearing.PlayDisappearAnimation(
                        () => disappearing.gameObject.SetActive(false));
                }
                else if (visible)
                {
                    paper.ResetVisualState();
                    paper.gameObject.SetActive(true);
                }
                else
                {
                    paper.gameObject.SetActive(false);
                }
            }

            VisibleCount = visibleCount;
            _previousVisibleCount = visibleCount;
        }

        private void EnsurePapers()
        {
            if (papers != null && papers.Length > 0)
            {
                return;
            }

            papers = GetComponentsInChildren<ContractPaperClickable>(true);
            Array.Sort(
                papers,
                (left, right) => right.SortingOrder.CompareTo(left.SortingOrder));
        }
    }
}
