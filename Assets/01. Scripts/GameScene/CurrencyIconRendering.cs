using TMPro;
using UnityEngine;

namespace DiaBlackJack.GameScene
{
    internal static class CurrencyIconText
    {
        public static void Set(TMP_Text target, string value)
        {
            if (target == null)
            {
                return;
            }

            target.richText = true;
            target.text = CurrencyIconMarkup.FormatForTmp(value);
        }
    }

    internal static class CurrencyIconGui
    {
        internal const int IconTextureSize = 24;
        private const string SpriteAssetResourcePath = "Sprite Assets/";
        private static Texture2D _goldTexture;
        private static Texture2D _soulTexture;

        public static GUIContent Content(string value)
        {
            CurrencyIconKind kind = CurrencyIconMarkup.DetectFirst(value);
            if (kind == CurrencyIconKind.None)
            {
                return new GUIContent(value ?? string.Empty);
            }

            Texture2D texture = GetTexture(kind);
            if (texture == null)
            {
                return new GUIContent(value ?? string.Empty);
            }

            return new GUIContent(
                CurrencyIconMarkup.RemoveWords(value, kind),
                texture);
        }

        public static GUIContent Soul(string value)
        {
            return ContentForKind(value, CurrencyIconKind.Soul);
        }

        public static GUIContent Gold(string value)
        {
            return ContentForKind(value, CurrencyIconKind.Gold);
        }

        private static GUIContent ContentForKind(
            string value,
            CurrencyIconKind kind)
        {
            Texture2D texture = GetTexture(kind);
            return texture == null
                ? new GUIContent(value ?? string.Empty)
                : new GUIContent(
                    CurrencyIconMarkup.RemoveWords(value, kind),
                    texture);
        }

        private static Texture2D GetTexture(CurrencyIconKind kind)
        {
            switch (kind)
            {
                case CurrencyIconKind.Gold:
                    return _goldTexture ??= LoadTexture(
                        CurrencyIconMarkup.GoldSpriteAssetName);
                case CurrencyIconKind.Soul:
                    return _soulTexture ??= LoadTexture(
                        CurrencyIconMarkup.SoulSpriteAssetName);
                default:
                    return null;
            }
        }

        private static Texture2D LoadTexture(string assetName)
        {
            TMP_SpriteAsset spriteAsset = Resources.Load<TMP_SpriteAsset>(
                SpriteAssetResourcePath + assetName);
            Texture source = spriteAsset == null
                ? null
                : spriteAsset.spriteSheet;
            return source == null ? null : CreateThumbnail(source, assetName);
        }

        private static Texture2D CreateThumbnail(Texture source, string assetName)
        {
            RenderTexture previous = RenderTexture.active;
            RenderTexture target = RenderTexture.GetTemporary(
                IconTextureSize,
                IconTextureSize,
                0,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.sRGB);
            try
            {
                Graphics.Blit(source, target);
                RenderTexture.active = target;
                Texture2D thumbnail = new Texture2D(
                    IconTextureSize,
                    IconTextureSize,
                    TextureFormat.RGBA32,
                    mipChain: false)
                {
                    name = assetName + " IMGUI Thumbnail",
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    hideFlags = HideFlags.HideAndDontSave
                };
                thumbnail.ReadPixels(
                    new Rect(0f, 0f, IconTextureSize, IconTextureSize),
                    0,
                    0,
                    recalculateMipMaps: false);
                thumbnail.Apply(updateMipmaps: false, makeNoLongerReadable: true);
                return thumbnail;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
            }
        }
    }
}
