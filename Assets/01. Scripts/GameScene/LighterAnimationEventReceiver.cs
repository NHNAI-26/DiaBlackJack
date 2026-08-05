using Border.Core;
using UnityEngine;

namespace DiaBlackJack.GameScene
{
    [DisallowMultipleComponent]
    public sealed class LighterAnimationEventReceiver :
        PresentationAnimationEventReceiver
    {
        [Header("Animator")]
        [SerializeField] private Animator animator;

        [Header("Fire Shader")]
        [SerializeField] private Renderer fireRenderer;
        [SerializeField] private LighterDragTriggerController interaction;

        private static readonly int IgnitionRevealId = Shader.PropertyToID("_IgnitionReveal");
        private static readonly int IgnitionRevealHeightId = Shader.PropertyToID("_IgnitionRevealHeight");

        private MaterialPropertyBlock firePropertyBlock;

        protected override void Awake()
        {
            base.Awake();
            animator ??= GetComponent<Animator>();
            interaction ??= GetComponent<LighterDragTriggerController>();
            firePropertyBlock = new MaterialPropertyBlock();
            fireRenderer ??= transform.Find("fire")?.GetComponent<Renderer>();
        }

        public void SetAnimatorTrigger(string triggerName)
        {
            string id = Key(triggerName);
            if (string.IsNullOrEmpty(id))
            {
                Log.W("[LighterAnimationEventReceiver] Cannot set an Animator trigger with an empty trigger name.", this);
                return;
            }

            Animator resolvedAnimator = ResolveAnimator();
            if (resolvedAnimator == null)
            {
                Log.W($"[LighterAnimationEventReceiver] Animator is unavailable for trigger '{id}'.", this);
                return;
            }

            if (id == "CardFire")
            {
                interaction ??= GetComponent<LighterDragTriggerController>();
                interaction?.EnableBurnCardDissolve();
            }
            resolvedAnimator.SetTrigger(id);
        }

        public void SetFireRevealHeight(float height)
        {
            SetFireReveal(1f, height);
        }

        public void EnableFireReveal()
        {
            SetFireReveal(1f, 0f);
        }

        public void DisableFireReveal()
        {
            SetFireReveal(0f, 1f);
        }

        public void HideFire()
        {
            if (ResolveFireRenderer() != null)
                fireRenderer.enabled = false;
        }

        public void ShowFire()
        {
            if (ResolveFireRenderer() != null)
                fireRenderer.enabled = true;
        }

        private void SetFireReveal(float enabled, float height)
        {
            if (ResolveFireRenderer() == null)
                return;

            firePropertyBlock ??= new MaterialPropertyBlock();
            fireRenderer.GetPropertyBlock(firePropertyBlock);
            firePropertyBlock.SetFloat(IgnitionRevealId, Mathf.Clamp01(enabled));
            firePropertyBlock.SetFloat(IgnitionRevealHeightId, Mathf.Clamp01(height));
            fireRenderer.SetPropertyBlock(firePropertyBlock);
        }

        private Renderer ResolveFireRenderer()
        {
            if (fireRenderer != null)
                return fireRenderer;

            fireRenderer = transform.Find("fire")?.GetComponent<Renderer>();
            if (fireRenderer == null)
                Log.W("[LighterAnimationEventReceiver] Fire renderer is not assigned.", this);

            return fireRenderer;
        }

        private Animator ResolveAnimator()
        {
            if (animator != null)
                return animator;

            animator = GetComponent<Animator>();
            return animator;
        }

    }
}
