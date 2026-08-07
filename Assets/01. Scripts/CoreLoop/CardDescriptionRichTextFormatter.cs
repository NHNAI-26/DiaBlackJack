namespace DiaBlackJack.CoreLoop
{
    /// <summary>
    /// Wraps the four core action terms (히트/스탠드/체인지/버스트) in TMP rich-text
    /// bold+color tags wherever they appear in card/codex description text. Pure string
    /// substitution — no UnityEngine reference — so it can run from CardDefinition's
    /// constructor as well as from Unity-facing presentation code.
    /// </summary>
    public static class CardDescriptionRichTextFormatter
    {
        private static readonly (string Term, string ColorHex)[] Terms =
        {
            ("히트", "2196F3"),
            ("스탠드", "FF9800"),
            ("체인지", "4CAF50"),
            ("버스트", "B71C1C"),
        };

        public static string Apply(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            foreach ((string term, string colorHex) in Terms)
            {
                text = text.Replace(
                    term,
                    $"<b><color=#{colorHex}>{term}</color></b>");
            }

            return text;
        }
    }
}
