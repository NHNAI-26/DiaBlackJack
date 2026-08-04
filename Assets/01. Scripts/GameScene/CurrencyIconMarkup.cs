using System;
using System.Text.RegularExpressions;

namespace DiaBlackJack.GameScene
{
    internal enum CurrencyIconKind
    {
        None = 0,
        Gold = 1,
        Soul = 2
    }

    internal static class CurrencyIconMarkup
    {
        public const string GoldSpriteAssetName = "GoldIcon";
        public const string SoulSpriteAssetName = "SoulIcon";
        public const string GoldTag =
            "<size=115%><sprite=\"GoldIcon\" index=0></size>";
        public const string SoulTag =
            "<size=135%><sprite=\"SoulIcon\" index=0></size>";

        private static readonly Regex GoldWord = new Regex(
            @"\bGOLD\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        private static readonly Regex SoulWord = new Regex(
            @"\bSOULS?\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        private static readonly Regex RepeatedHorizontalWhitespace = new Regex(
            @"[ \t]{2,}",
            RegexOptions.CultureInvariant);

        public static string FormatForTmp(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value ?? string.Empty;
            }

            string formatted = GoldWord.Replace(value, GoldTag);
            formatted = SoulWord.Replace(formatted, SoulTag);
            formatted = formatted.Replace("골드", GoldTag);
            return formatted.Replace("영혼", SoulTag);
        }

        public static string FormatChangeActionLabel(string changeActionText)
        {
            const string prefix = "CHANGE (";
            if (string.IsNullOrEmpty(changeActionText) ||
                !changeActionText.StartsWith(prefix, StringComparison.Ordinal))
            {
                return "CHANGE";
            }

            int costEnd = changeActionText.IndexOf('|', prefix.Length);
            if (costEnd < 0)
            {
                return "CHANGE";
            }

            string cost = changeActionText.Substring(
                prefix.Length,
                costEnd - prefix.Length).Trim();
            if (cost.EndsWith(" SOUL", StringComparison.Ordinal))
            {
                cost = cost.Substring(0, cost.Length - " SOUL".Length);
            }

            return cost == "FREE"
                ? $"CHANGE {SoulTag} 0"
                : $"CHANGE {SoulTag} {cost}";
        }

        public static CurrencyIconKind DetectFirst(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return CurrencyIconKind.None;
            }

            Match gold = GoldWord.Match(value);
            int goldIndex = gold.Success ? gold.Index : value.IndexOf("골드", StringComparison.Ordinal);
            Match soul = SoulWord.Match(value);
            int soulIndex = soul.Success ? soul.Index : value.IndexOf("영혼", StringComparison.Ordinal);
            if (goldIndex < 0)
            {
                return soulIndex < 0 ? CurrencyIconKind.None : CurrencyIconKind.Soul;
            }

            return soulIndex < 0 || goldIndex <= soulIndex
                ? CurrencyIconKind.Gold
                : CurrencyIconKind.Soul;
        }

        public static string RemoveWords(string value, CurrencyIconKind kind)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value ?? string.Empty;
            }

            string withoutWord;
            switch (kind)
            {
                case CurrencyIconKind.Gold:
                    withoutWord = GoldWord.Replace(value, string.Empty)
                        .Replace("골드", string.Empty);
                    break;
                case CurrencyIconKind.Soul:
                    withoutWord = SoulWord.Replace(value, string.Empty)
                        .Replace("영혼", string.Empty);
                    break;
                default:
                    return value;
            }

            return RepeatedHorizontalWhitespace.Replace(withoutWord, " ").Trim();
        }
    }
}
