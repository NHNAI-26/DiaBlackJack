using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Border.UI
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Graphic))]
    public sealed class UIAlphaSpriteUvRectBinder : UIBehaviour, IMaterialModifier
    {
        private static readonly int UVAlphaFadeUVRectId =
            Shader.PropertyToID("_UVAlphaFadeUVRect");

        private Graphic _graphic;
        private Image _image;
        private Material _material;
        private Sprite _trackedSprite;
        private Vector4 _trackedUvRect = new Vector4(0f, 0f, 1f, 1f);

        private Graphic Graphic
        {
            get
            {
                if (_graphic == null)
                    _graphic = GetComponent<Graphic>();

                return _graphic;
            }
        }

        private Image Image
        {
            get
            {
                if (_image == null)
                    _image = GetComponent<Image>();

                return _image;
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Refresh(forceDirty: true);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            DestroyMaterial();
            _trackedSprite = null;
            _trackedUvRect = new Vector4(0f, 0f, 1f, 1f);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            DestroyMaterial();
        }

        protected override void OnDidApplyAnimationProperties()
        {
            base.OnDidApplyAnimationProperties();
            Refresh(forceDirty: true);
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            Refresh(forceDirty: true);
        }
#endif

        private void LateUpdate()
        {
            Refresh(forceDirty: false);
        }

        public Material GetModifiedMaterial(Material baseMaterial)
        {
            if (baseMaterial == null)
                return baseMaterial;

            if (_material == null || _material.shader != baseMaterial.shader)
            {
                DestroyMaterial();
                _material = new Material(baseMaterial)
                {
                    name = baseMaterial.name + " (UI Alpha UV Rect)"
                };
                _material.hideFlags = HideFlags.HideAndDontSave;
            }
            else
            {
                _material.CopyPropertiesFromMaterial(baseMaterial);
            }

            if (_material.HasVector(UVAlphaFadeUVRectId))
                _material.SetVector(UVAlphaFadeUVRectId, _trackedUvRect);

            return _material;
        }

        private void Refresh(bool forceDirty)
        {
            Sprite sprite = CurrentSprite();
            Vector4 uvRect = GetSpriteUvRect(sprite);
            if (!forceDirty &&
                sprite == _trackedSprite &&
                Approximately(uvRect, _trackedUvRect))
            {
                return;
            }

            _trackedSprite = sprite;
            _trackedUvRect = uvRect;

            if (_material != null && _material.HasVector(UVAlphaFadeUVRectId))
                _material.SetVector(UVAlphaFadeUVRectId, _trackedUvRect);

            if (Graphic != null)
                Graphic.SetMaterialDirty();
        }

        private Sprite CurrentSprite()
        {
            Image image = Image;
            if (image == null)
                return null;

            return image.overrideSprite != null ? image.overrideSprite : image.sprite;
        }

        private static Vector4 GetSpriteUvRect(Sprite sprite)
        {
            if (sprite == null)
                return new Vector4(0f, 0f, 1f, 1f);

            Vector2[] uvs = sprite.uv;
            if (uvs == null || uvs.Length == 0)
                return new Vector4(0f, 0f, 1f, 1f);

            Vector2 minimum = uvs[0];
            Vector2 maximum = uvs[0];
            for (int i = 1; i < uvs.Length; i++)
            {
                minimum = Vector2.Min(minimum, uvs[i]);
                maximum = Vector2.Max(maximum, uvs[i]);
            }

            Vector2 size = maximum - minimum;
            return new Vector4(
                minimum.x,
                minimum.y,
                Mathf.Max(size.x, 0.00001f),
                Mathf.Max(size.y, 0.00001f));
        }

        private static bool Approximately(Vector4 left, Vector4 right)
        {
            return Mathf.Approximately(left.x, right.x) &&
                Mathf.Approximately(left.y, right.y) &&
                Mathf.Approximately(left.z, right.z) &&
                Mathf.Approximately(left.w, right.w);
        }

        private void DestroyMaterial()
        {
            if (_material == null)
                return;

            if (Application.isPlaying)
                Destroy(_material);
            else
                DestroyImmediate(_material);

            _material = null;
        }
    }
}
