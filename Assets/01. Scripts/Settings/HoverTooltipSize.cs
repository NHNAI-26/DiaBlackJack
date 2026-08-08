using UnityEngine;

namespace Border.Settings
{
    public enum HoverTooltipSize
    {
        Small = 0,
        Normal = 1,
        Large = 2
    }

    internal static class HoverTooltipSizeUtility
    {
        internal static HoverTooltipSize Normalize(HoverTooltipSize size)
        {
            return size == HoverTooltipSize.Small ||
                size == HoverTooltipSize.Normal ||
                size == HoverTooltipSize.Large
                    ? size
                    : HoverTooltipSize.Normal;
        }

        internal static Vector3 GetScale(HoverTooltipSize size)
        {
            switch (Normalize(size))
            {
                case HoverTooltipSize.Small:
                    return Vector3.one;
                case HoverTooltipSize.Large:
                    return new Vector3(1.5f, 1.5f, 1f);
                default:
                    return new Vector3(1.3f, 1.3f, 1f);
            }
        }
    }
}
