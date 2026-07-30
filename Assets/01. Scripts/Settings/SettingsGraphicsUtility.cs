using System.Collections.Generic;
using UnityEngine;

namespace Border.Settings
{
    public static class SettingsGraphicsUtility
    {
        public const int FullScreenModeIndex = 0;
        public const int WindowedModeIndex = 1;
        public const int BorderlessWindowModeIndex = 2;

        public static List<DisplayResolutionOption> GetResolutionOptions()
        {
            Resolution[] resolutions = Screen.resolutions;
            var source = new List<DisplayResolutionOption>(
                resolutions == null ? 0 : resolutions.Length);
            if (resolutions != null)
            {
                for (int i = 0; i < resolutions.Length; i++)
                {
                    Resolution resolution = resolutions[i];
                    source.Add(new DisplayResolutionOption(
                        resolution.width,
                        resolution.height,
                        resolution.refreshRateRatio.numerator,
                        resolution.refreshRateRatio.denominator));
                }
            }

            Resolution current = Screen.currentResolution;
            var fallback = new DisplayResolutionOption(
                Mathf.Max(1, current.width),
                Mathf.Max(1, current.height),
                current.refreshRateRatio.numerator,
                current.refreshRateRatio.denominator);
            return BuildResolutionOptions(source, fallback);
        }

        internal static List<DisplayResolutionOption> BuildResolutionOptions(
            IEnumerable<DisplayResolutionOption> source,
            DisplayResolutionOption fallback)
        {
            var highestBySize =
                new Dictionary<(int Width, int Height), DisplayResolutionOption>();
            if (source != null)
            {
                foreach (DisplayResolutionOption option in source)
                {
                    if (option.Width <= 0 || option.Height <= 0)
                    {
                        continue;
                    }

                    var key = (option.Width, option.Height);
                    if (!highestBySize.TryGetValue(
                            key,
                            out DisplayResolutionOption existing) ||
                        option.RefreshRate > existing.RefreshRate)
                    {
                        highestBySize[key] = option;
                    }
                }
            }

            if (highestBySize.Count == 0)
            {
                highestBySize[(fallback.Width, fallback.Height)] = fallback;
            }

            var result =
                new List<DisplayResolutionOption>(highestBySize.Values);
            result.Sort((left, right) =>
            {
                int widthComparison = left.Width.CompareTo(right.Width);
                return widthComparison != 0
                    ? widthComparison
                    : left.Height.CompareTo(right.Height);
            });
            return result;
        }

        public static DisplayResolutionOption ValidateResolution(
            IReadOnlyList<DisplayResolutionOption> options,
            int width,
            int height)
        {
            if (options != null)
            {
                for (int i = 0; i < options.Count; i++)
                {
                    if (options[i].Width == width &&
                        options[i].Height == height)
                    {
                        return options[i];
                    }
                }
            }

            Resolution current = Screen.currentResolution;
            if (options != null)
            {
                for (int i = 0; i < options.Count; i++)
                {
                    if (options[i].Width == current.width &&
                        options[i].Height == current.height)
                    {
                        return options[i];
                    }
                }

                if (options.Count > 0)
                {
                    return options[options.Count - 1];
                }
            }

            return new DisplayResolutionOption(
                Mathf.Max(1, current.width),
                Mathf.Max(1, current.height),
                current.refreshRateRatio.numerator,
                current.refreshRateRatio.denominator);
        }

        public static FullScreenMode GetFullScreenMode(GameWindowMode mode)
        {
            switch (mode)
            {
                case GameWindowMode.Windowed:
                    return FullScreenMode.Windowed;
                case GameWindowMode.ExclusiveFullscreen:
                    return FullScreenMode.ExclusiveFullScreen;
                case GameWindowMode.BorderlessFullscreen:
                default:
                    return FullScreenMode.FullScreenWindow;
            }
        }

        public static FullScreenMode GetFullScreenMode(int modeIndex)
        {
            switch (GetValidatedWindowModeIndex(modeIndex))
            {
                case FullScreenModeIndex:
                    return FullScreenMode.ExclusiveFullScreen;
                case WindowedModeIndex:
                    return FullScreenMode.Windowed;
                default:
                    return FullScreenMode.FullScreenWindow;
            }
        }

        public static int GetValidatedWindowModeIndex(int modeIndex)
        {
            return modeIndex >= FullScreenModeIndex &&
                modeIndex <= BorderlessWindowModeIndex
                ? modeIndex
                : BorderlessWindowModeIndex;
        }

        public static int GetValidatedResolutionIndex(
            IReadOnlyList<Resolution> resolutions,
            int resolutionIndex)
        {
            if (resolutions == null || resolutions.Count == 0)
            {
                return 0;
            }

            return Mathf.Clamp(resolutionIndex, 0, resolutions.Count - 1);
        }

        public static List<Resolution> GetResolutionsList()
        {
            Resolution[] resolutions = Screen.resolutions;
            var result = new List<Resolution>();
            if (resolutions != null)
            {
                result.AddRange(resolutions);
            }

            if (result.Count == 0)
            {
                result.Add(Screen.currentResolution);
            }

            result.Reverse();
            return result;
        }

        public static Resolution ApplyGraphicsSettings(
            int resolutionIndex,
            int windowModeIndex)
        {
            List<Resolution> resolutions = GetResolutionsList();
            int validatedIndex =
                GetValidatedResolutionIndex(resolutions, resolutionIndex);
            Resolution resolution = resolutions[validatedIndex];
            Screen.SetResolution(
                resolution.width,
                resolution.height,
                GetFullScreenMode(windowModeIndex));
            return resolution;
        }

        public static void Apply(GameSettingsSnapshot settings)
        {
            List<DisplayResolutionOption> options = GetResolutionOptions();
            DisplayResolutionOption selected = ValidateResolution(
                options,
                settings.ResolutionWidth,
                settings.ResolutionHeight);

            if (settings.WindowMode ==
                GameWindowMode.BorderlessFullscreen)
            {
                Resolution native = Screen.currentResolution;
                Screen.SetResolution(
                    native.width,
                    native.height,
                    FullScreenMode.FullScreenWindow);
                return;
            }

            var refreshRate = new RefreshRate
            {
                numerator = selected.RefreshNumerator,
                denominator = selected.RefreshDenominator
            };
            Screen.SetResolution(
                selected.Width,
                selected.Height,
                GetFullScreenMode(settings.WindowMode),
                refreshRate);
        }
    }
}
