using System.Collections.Generic;
using UnityEngine;

namespace DiaBlackJack.Rendering
{
    public static class PostProcessOutlineRegistry
    {
        private static readonly List<Target> Targets = new List<Target>();

        public static bool HasTargets
        {
            get
            {
                RemoveInvalidTargets();
                return Targets.Count > 0;
            }
        }

        public static void Register(Renderer renderer, Color color, float widthPixels)
        {
            if (renderer == null)
            {
                return;
            }

            widthPixels = Mathf.Max(0f, widthPixels);
            for (int i = 0; i < Targets.Count; i++)
            {
                if (Targets[i].Renderer == renderer)
                {
                    Targets[i] = new Target(renderer, color, widthPixels);
                    return;
                }
            }

            Targets.Add(new Target(renderer, color, widthPixels));
        }

        public static void Unregister(Renderer renderer)
        {
            if (renderer == null)
            {
                return;
            }

            for (int i = Targets.Count - 1; i >= 0; i--)
            {
                if (Targets[i].Renderer == renderer)
                {
                    Targets.RemoveAt(i);
                }
            }
        }

        public static void FillTargets(List<Target> results)
        {
            results.Clear();
            RemoveInvalidTargets();
            results.AddRange(Targets);
        }

        public static void Clear()
        {
            Targets.Clear();
        }

        private static void RemoveInvalidTargets()
        {
            for (int i = Targets.Count - 1; i >= 0; i--)
            {
                Renderer renderer = Targets[i].Renderer;
                if (renderer == null ||
                    !renderer.enabled ||
                    !renderer.gameObject.activeInHierarchy)
                {
                    Targets.RemoveAt(i);
                }
            }
        }

        public struct Target
        {
            public Target(Renderer renderer, Color color, float widthPixels)
            {
                Renderer = renderer;
                Color = color;
                WidthPixels = widthPixels;
            }

            public Renderer Renderer { get; }
            public Color Color { get; }
            public float WidthPixels { get; }
        }
    }
}
