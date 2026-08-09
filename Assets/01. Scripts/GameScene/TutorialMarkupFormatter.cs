using System;
using System.Collections.Generic;
using System.Text;

namespace DiaBlackJack.GameScene
{
    internal readonly struct TutorialMarkupResult
    {
        public TutorialMarkupResult(string text, TutorialHighlightTarget highlight)
        {
            Text = text ?? string.Empty;
            Highlight = highlight;
        }

        public string Text { get; }

        public TutorialHighlightTarget Highlight { get; }
    }

    internal static class TutorialMarkupFormatter
    {
        private readonly struct StyleRange
        {
            public StyleRange(int start, int end, string color, bool bold)
            {
                Start = start;
                End = end;
                Color = color;
                Bold = bold;
            }

            public int Start { get; }
            public int End { get; }
            public string Color { get; }
            public bool Bold { get; }
        }

        private readonly struct PendingColor
        {
            public PendingColor(
                int start,
                int end,
                string color,
                bool wholeLine)
            {
                Start = start;
                End = end;
                Color = color;
                WholeLine = wholeLine;
            }

            public int Start { get; }
            public int End { get; }
            public string Color { get; }
            public bool WholeLine { get; }

            public PendingColor EndAt(int end)
            {
                return new PendingColor(Start, end, Color, WholeLine);
            }
        }

        private static readonly Dictionary<string, string> Colors =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "빨강", "#B71C1C" },
                { "파랑", "#2196F3" },
                { "주황", "#FF9800" },
                { "보라", "#9C27B0" },
                { "초록", "#4CAF50" },
                { "하늘색", "#64D8FF" }
            };

        public static TutorialMarkupResult Format(string source)
        {
            string content = source ?? string.Empty;
            TutorialHighlightTarget highlight = ExtractHighlight(ref content);
            var plain = new StringBuilder(content.Length);
            var boldRanges = new List<StyleRange>();
            var colors = new List<PendingColor>();
            int boldStart = -1;

            for (int index = 0; index < content.Length;)
            {
                if (index + 1 < content.Length &&
                    content[index] == '*' && content[index + 1] == '*')
                {
                    if (boldStart < 0)
                    {
                        boldStart = plain.Length;
                    }
                    else
                    {
                        boldRanges.Add(new StyleRange(
                            boldStart, plain.Length, null, bold: true));
                        boldStart = -1;
                    }

                    index += 2;
                    continue;
                }

                if (TryReadColor(content, index, out int consumed,
                        out string color, out bool wholeLine))
                {
                    colors.Add(new PendingColor(
                        plain.Length,
                        end: -1,
                        color,
                        wholeLine));
                    index += consumed;
                    continue;
                }

                const string colorReset = "(색 빼기)";
                if (content.AsSpan(index).StartsWith(
                        colorReset.AsSpan(),
                        StringComparison.Ordinal))
                {
                    for (int colorIndex = colors.Count - 1;
                         colorIndex >= 0;
                         colorIndex--)
                    {
                        if (colors[colorIndex].End >= 0)
                        {
                            continue;
                        }

                        colors[colorIndex] =
                            colors[colorIndex].EndAt(plain.Length);
                        break;
                    }

                    index += colorReset.Length;
                    continue;
                }

                plain.Append(content[index]);
                index++;
            }

            if (boldStart >= 0)
            {
                boldRanges.Add(new StyleRange(
                    boldStart, plain.Length, null, bold: true));
            }

            var ranges = new List<StyleRange>(boldRanges);
            string plainText = plain.ToString().TrimEnd();
            foreach (PendingColor pending in colors)
            {
                int end = pending.End >= 0
                    ? pending.End
                    : pending.WholeLine
                    ? plainText.Length
                    : FindWordEnd(plainText, pending.Start);
                end = AdjustToContainingBoldRange(
                    pending.Start,
                    end,
                    boldRanges,
                    fillContainingBoldRange:
                        pending.End < 0 && !pending.WholeLine);
                if (end > pending.Start)
                {
                    ranges.Add(new StyleRange(
                        pending.Start, end, pending.Color, bold: false));
                }
            }

            return new TutorialMarkupResult(
                ApplyRanges(plainText, ranges), highlight);
        }

        private static int AdjustToContainingBoldRange(
            int start,
            int end,
            IReadOnlyList<StyleRange> boldRanges,
            bool fillContainingBoldRange)
        {
            for (int index = 0; index < boldRanges.Count; index++)
            {
                StyleRange range = boldRanges[index];
                if (start < range.Start || start >= range.End)
                {
                    continue;
                }

                return fillContainingBoldRange
                    ? range.End
                    : Math.Min(end, range.End);
            }

            return end;
        }

        private static bool TryReadColor(
            string source,
            int index,
            out int consumed,
            out string color,
            out bool wholeLine)
        {
            consumed = 0;
            color = null;
            wholeLine = false;
            if (source[index] != '(')
            {
                return false;
            }

            int close = source.IndexOf(')', index + 1);
            if (close < 0)
            {
                return false;
            }

            string marker = source.Substring(index + 1, close - index - 1);
            const string wholePrefix = "전체 ";
            if (marker.StartsWith(wholePrefix, StringComparison.Ordinal))
            {
                wholeLine = true;
                marker = marker.Substring(wholePrefix.Length);
            }

            if (!Colors.TryGetValue(marker, out color))
            {
                return false;
            }

            consumed = close - index + 1;
            return true;
        }

        private static TutorialHighlightTarget ExtractHighlight(ref string content)
        {
            (string Suffix, TutorialHighlightTarget Target)[] commands =
            {
                ("(히트 버튼 하이라이트)", TutorialHighlightTarget.Hit),
                ("(스탠드 버튼 하이라이트)", TutorialHighlightTarget.Stand),
                ("(체인지 버튼 하이라이트)", TutorialHighlightTarget.Change),
                ("(리볼버 카드 하이라이트)", TutorialHighlightTarget.RevolverCard),
                ("(내 덱 하이라이트)", TutorialHighlightTarget.PlayerDrawDeck),
                ("(계약서 하이라이트)", TutorialHighlightTarget.ContractPaper),
                ("(계약서 하아라이트)", TutorialHighlightTarget.ContractPaper)
            };

            string trimmed = content.TrimEnd();
            foreach ((string suffix, TutorialHighlightTarget target) in commands)
            {
                if (!trimmed.EndsWith(suffix, StringComparison.Ordinal))
                {
                    continue;
                }

                content = trimmed.Substring(0, trimmed.Length - suffix.Length)
                    .TrimEnd();
                return target;
            }

            content = trimmed;
            return TutorialHighlightTarget.None;
        }

        private static int FindWordEnd(string text, int start)
        {
            int index = Math.Min(start, text.Length);
            while (index < text.Length && !char.IsWhiteSpace(text[index]))
            {
                index++;
            }

            return index;
        }

        private static string ApplyRanges(
            string plain,
            IReadOnlyList<StyleRange> ranges)
        {
            var result = new StringBuilder(plain.Length + 32);
            bool bold = false;
            string color = null;
            for (int index = 0; index <= plain.Length; index++)
            {
                bool nextBold = false;
                string nextColor = null;
                for (int rangeIndex = 0; rangeIndex < ranges.Count; rangeIndex++)
                {
                    StyleRange range = ranges[rangeIndex];
                    if (index < range.Start || index >= range.End)
                    {
                        continue;
                    }

                    nextBold |= range.Bold;
                    nextColor ??= range.Color;
                }

                if (bold != nextBold || color != nextColor)
                {
                    bool boldChanged = bold != nextBold;
                    bool colorChanged = color != nextColor;
                    if (color != null && (colorChanged || boldChanged))
                    {
                        result.Append("</color>");
                    }
                    if (bold && !nextBold)
                    {
                        result.Append("</b>");
                    }
                    if (!bold && nextBold)
                    {
                        result.Append("<b>");
                    }
                    if (nextColor != null && (colorChanged || boldChanged))
                    {
                        result.Append("<color=").Append(nextColor).Append('>');
                    }

                    bold = nextBold;
                    color = nextColor;
                }

                if (index < plain.Length)
                {
                    AppendEscaped(result, plain[index]);
                }
            }

            return result.ToString();
        }

        private static void AppendEscaped(StringBuilder builder, char character)
        {
            switch (character)
            {
                case '<':
                    builder.Append("&lt;");
                    break;
                case '>':
                    builder.Append("&gt;");
                    break;
                case '&':
                    builder.Append("&amp;");
                    break;
                default:
                    builder.Append(character);
                    break;
            }
        }
    }
}
