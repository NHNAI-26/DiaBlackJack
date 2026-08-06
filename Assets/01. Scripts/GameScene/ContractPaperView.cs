using System;
using UnityEngine;

namespace DiaBlackJack.GameScene
{
    [DisallowMultipleComponent]
    public sealed class ContractPaperView : MonoBehaviour
    {
        [SerializeField] private ContractPaperClickable[] papers;

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

            // Papers are sorted top (index 0) to bottom (last index); as the stack
            // shrinks, the top-most paper is the one that goes away first, regardless
            // of which paper the player actually clicked. Only the top-most currently
            // visible paper is hoverable/clickable — the ones underneath are purely
            // visual "how many left" filler.
            int topVisibleIndex = papers.Length - visibleCount;

            for (int i = 0; i < papers.Length; i++)
            {
                ContractPaperClickable paper = papers[i];
                if (paper == null)
                {
                    continue;
                }

                bool visible = i >= topVisibleIndex;
                bool isTopOfStack = i == topVisibleIndex;
                paper.SetInteractable(visible && isTopOfStack && canPlayerBegin);
                paper.gameObject.SetActive(visible);
            }

            VisibleCount = visibleCount;
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
                (left, right) => string.CompareOrdinal(left.name, right.name));
        }
    }
}
