using DiaBlackJack.GameScene;
using NUnit.Framework;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace DiaBlackJack.CoreLoop.Tests
{
    public sealed class MoodControllerTests
    {
        private static readonly int GlassGlowColorId =
            Shader.PropertyToID("_GlassGlowColor");

        [Test]
        public void MOO01_U01_MoodProfileRejectsEmptyId()
        {
            MoodProfileSO profile = ScriptableObject.CreateInstance<MoodProfileSO>();
            try
            {
                SetPrivateField(profile, "id", string.Empty);

                Assert.That(profile.HasValidId, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void MOO01_U02_TryBlendToMoodReturnsOnlyRegisteredProfile()
        {
            MoodProfileSO profile = CreateProfile("round-1");
            GameObject root = new GameObject("MoodControllerTest");
            MoodController controller = root.AddComponent<MoodController>();
            try
            {
                SetPrivateField(
                    controller,
                    "moodProfiles",
                    new List<MoodProfileSO> { profile });

                Assert.That(controller.TryBlendToMood("round-1", 0f), Is.True);
                Assert.That(controller.TryBlendToMood("missing", 0f), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void MOO01_U03_ImmediateMoodAppliesLightColors()
        {
            MoodProfileSO profile = CreateProfile(
                "round-1",
                volumetricColor: Color.blue,
                enemyColor: Color.red,
                enteranceColor: Color.green);
            GameObject root = new GameObject("MoodControllerTest");
            GameObject volumetricObject = new GameObject("VolumetricLight");
            GameObject enemyObject = new GameObject("EnemyLight");
            GameObject enteranceObject = new GameObject("EnteranceLight");
            MoodController controller = root.AddComponent<MoodController>();
            Light volumetricLight = volumetricObject.AddComponent<Light>();
            Light enemyLight = enemyObject.AddComponent<Light>();
            Light enteranceLight = enteranceObject.AddComponent<Light>();
            try
            {
                SetPrivateField(controller, "volumetricLight", volumetricLight);
                SetPrivateField(controller, "enemyLight", enemyLight);
                SetPrivateField(controller, "enteranceLight", enteranceLight);

                controller.BlendToMood(profile, 0f);

                Assert.That(volumetricLight.color, Is.EqualTo(Color.blue));
                Assert.That(enemyLight.color, Is.EqualTo(Color.red));
                Assert.That(enteranceLight.color, Is.EqualTo(Color.green));

                volumetricLight.color = Color.white;
                enemyLight.color = Color.white;
                enteranceLight.color = Color.white;

                controller.BlendToMood(profile, -1f);

                Assert.That(volumetricLight.color, Is.EqualTo(Color.blue));
                Assert.That(enemyLight.color, Is.EqualTo(Color.red));
                Assert.That(enteranceLight.color, Is.EqualTo(Color.green));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(volumetricObject);
                Object.DestroyImmediate(enemyObject);
                Object.DestroyImmediate(enteranceObject);
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void MOO01_U04_WindowGlowUsesPropertyBlockOnly()
        {
            Color sourceColor = Color.white;
            Color targetColor = new Color(0.35f, 0.05f, 0.1f, 1f);
            MoodProfileSO profile = CreateProfile(
                "round-1",
                windowColor: targetColor);
            GameObject root = new GameObject("MoodControllerTest");
            GameObject windowObject = new GameObject("Window");
            MoodController controller = root.AddComponent<MoodController>();
            MeshRenderer renderer = windowObject.AddComponent<MeshRenderer>();
            Shader shader = Shader.Find("Shader/Uber Lit");
            Assert.That(shader, Is.Not.Null);
            Material material = new Material(shader);
            MaterialPropertyBlock properties = new MaterialPropertyBlock();
            try
            {
                material.SetColor(GlassGlowColorId, sourceColor);
                renderer.sharedMaterial = material;
                SetPrivateField(
                    controller,
                    "windowGlassRenderers",
                    new Renderer[] { renderer });

                controller.SetMoodImmediate(profile);

                renderer.GetPropertyBlock(properties);
                Assert.That(properties.GetColor(GlassGlowColorId), Is.EqualTo(targetColor));
                Assert.That(material.GetColor(GlassGlowColorId), Is.EqualTo(sourceColor));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(windowObject);
                Object.DestroyImmediate(material);
                Object.DestroyImmediate(profile);
            }
        }

        private static MoodProfileSO CreateProfile(
            string id,
            Color? windowColor = null,
            Color? volumetricColor = null,
            Color? enemyColor = null,
            Color? enteranceColor = null)
        {
            MoodProfileSO profile = ScriptableObject.CreateInstance<MoodProfileSO>();
            SetPrivateField(profile, "id", id);
            SetPrivateField(
                profile,
                "windowGlassGlowColor",
                windowColor ?? Color.white);
            SetPrivateField(
                profile,
                "volumetricLightColor",
                volumetricColor ?? Color.white);
            SetPrivateField(profile, "enemyLightColor", enemyColor ?? Color.white);
            SetPrivateField(
                profile,
                "enteranceLightColor",
                enteranceColor ?? Color.white);
            return profile;
        }

        private static void SetPrivateField<T>(
            object target,
            string name,
            T value)
        {
            FieldInfo field = target.GetType().GetField(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(target, value);
        }
    }
}
