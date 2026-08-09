using System;
using System.Collections.Generic;
using Border.Audio;
using DG.Tweening;
using UnityEngine;

namespace DiaBlackJack.GameScene
{
    [DisallowMultipleComponent]
    public sealed class CombatActionSkullView : MonoBehaviour
    {
        private static readonly string[] LandingSfxIds =
        {
            "skullLay01",
            "skullLay02",
            "skullLay03",
            "skullLay04",
        };

        private const string DissolveSfxId = "dissolve";
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int FresnelColorId = Shader.PropertyToID("_RimColor");
        private static readonly int DissolveAmountId =
            Shader.PropertyToID("_DissolveAmount");
        private static readonly int DissolveEnabledId =
            Shader.PropertyToID("_DissolveEnabled");

        [SerializeField, Min(0.01f)] private float moveDuration = 0.3f;
        [SerializeField, Min(0f)] private float jumpHeight = 0.22f;
        [SerializeField, Min(0.01f)] private float dissolveDuration = 0.6f;

        private readonly List<Material> _materials = new List<Material>();
        private Tween _activeTween;
        private Transform _followTarget;
        private Vector3 _followOffset;
        private Vector3 _homePosition;
        private bool _initialized;

        public float MoveDuration => moveDuration;

        public float DissolveDuration => dissolveDuration;

        public bool IsVisible => gameObject.activeSelf;

        public void Initialize(Color baseColor, Vector3 homePosition)
        {
            CacheMaterialInstances();
            _initialized = true;
            SetBaseColor(baseColor);
            ResetView(homePosition);
        }

        public void SetBaseColor(Color baseColor)
        {
            CacheMaterialInstances();
            for (int index = 0; index < _materials.Count; index++)
            {
                Material material = _materials[index];
                if (material.HasProperty(BaseColorId))
                {
                    material.SetColor(BaseColorId, baseColor);
                }
            }
        }

        public void SetFresnelColor(Color fresnelColor)
        {
            CacheMaterialInstances();
            for (int index = 0; index < _materials.Count; index++)
            {
                Material material = _materials[index];
                if (material.HasProperty(FresnelColorId))
                {
                    material.SetColor(FresnelColorId, fresnelColor);
                }
            }
        }

        public void ResetView(Vector3 homePosition)
        {
            _homePosition = homePosition;
            _followTarget = null;
            KillActiveTween();
            ResetDissolveMaterials();
            transform.position = homePosition;
            gameObject.SetActive(false);
        }

        public Tween MoveTo(Transform target, Vector3 worldOffset)
        {
            if (target == null)
            {
                return null;
            }

            EnsureInitialized();
            KillActiveTween();
            if (!gameObject.activeSelf)
            {
                transform.position = _homePosition;
                gameObject.SetActive(true);
            }

            _followTarget = target;
            _followOffset = worldOffset;
            Vector3 destination = target.position + worldOffset;
            _activeTween = transform
                .DOJump(destination, jumpHeight, 1, moveDuration)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    transform.position = _followTarget != null
                        ? _followTarget.position + _followOffset
                        : destination;
                    SoundManager.Current?.PlaySfx(
                        LandingSfxIds[UnityEngine.Random.Range(
                            0,
                            LandingSfxIds.Length)]);
                    _activeTween = null;
                });
            return _activeTween;
        }

        public void EnsureVisibleAtHome()
        {
            EnsureInitialized();
            KillActiveTween();
            _followTarget = null;
            transform.position = _homePosition;
            gameObject.SetActive(true);
        }

        public Tween PlayDissolve()
        {
            EnsureVisibleAtHomeIfHidden();
            KillActiveTween();
            _followTarget = null;
            SetDissolveAmount(0f, enabled: true);
            SoundManager.Current?.PlaySfx(DissolveSfxId);
            _activeTween = DOVirtual
                .Float(0f, 1f, dissolveDuration, amount =>
                    SetDissolveAmount(amount, enabled: true))
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    _activeTween = null;
                    gameObject.SetActive(false);
                });
            return _activeTween;
        }

        private void LateUpdate()
        {
            if (_activeTween == null && _followTarget != null)
            {
                transform.position = _followTarget.position + _followOffset;
            }
        }

        private void OnDisable()
        {
            if (_activeTween != null && _activeTween.IsActive())
            {
                _activeTween.Kill();
                _activeTween = null;
            }
        }

        private void OnDestroy()
        {
            KillActiveTween();
            for (int index = 0; index < _materials.Count; index++)
            {
                Material material = _materials[index];
                if (material == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(material);
                }
                else
                {
                    DestroyImmediate(material);
                }
            }

            _materials.Clear();
        }

        private void EnsureInitialized()
        {
            if (!_initialized)
            {
                Initialize(Color.white, transform.position);
            }
        }

        private void EnsureVisibleAtHomeIfHidden()
        {
            EnsureInitialized();
            if (!gameObject.activeSelf)
            {
                EnsureVisibleAtHome();
            }
        }

        private void CacheMaterialInstances()
        {
            if (_materials.Count > 0)
            {
                return;
            }

            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            for (int rendererIndex = 0;
                 rendererIndex < renderers.Length;
                 rendererIndex++)
            {
                Renderer renderer = renderers[rendererIndex];
                Material[] source = renderer.sharedMaterials;
                Material[] instances = new Material[source.Length];
                for (int materialIndex = 0;
                     materialIndex < source.Length;
                     materialIndex++)
                {
                    Material instance = source[materialIndex] == null
                        ? null
                        : new Material(source[materialIndex]);
                    instances[materialIndex] = instance;
                    if (instance != null)
                    {
                        _materials.Add(instance);
                    }
                }

                renderer.sharedMaterials = instances;
            }
        }

        private void ResetDissolveMaterials()
        {
            SetDissolveAmount(0f, enabled: false);
        }

        private void SetDissolveAmount(float amount, bool enabled)
        {
            for (int index = 0; index < _materials.Count; index++)
            {
                Material material = _materials[index];
                if (enabled)
                {
                    material.EnableKeyword("_DISSOLVE_ON");
                }
                else
                {
                    material.DisableKeyword("_DISSOLVE_ON");
                }

                if (material.HasProperty(DissolveEnabledId))
                {
                    material.SetFloat(DissolveEnabledId, enabled ? 1f : 0f);
                }

                if (material.HasProperty(DissolveAmountId))
                {
                    material.SetFloat(DissolveAmountId, amount);
                }
            }
        }

        private void KillActiveTween()
        {
            if (_activeTween != null && _activeTween.IsActive())
            {
                _activeTween.Kill();
            }

            _activeTween = null;
        }
    }
}
