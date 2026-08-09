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

        [TestCase(GameFlowScreen.StartingDemonReveal, null, "readyStage")]
        [TestCase(GameFlowScreen.OpponentSelection, null, "readyStage")]
        [TestCase(GameFlowScreen.Shop, null, "shopStage")]
        [TestCase(GameFlowScreen.Combat, "cowardly-gambler", "cowardlyGambler")]
        [TestCase(GameFlowScreen.Combat, "gunslinger", "gunslinger")]
        [TestCase(GameFlowScreen.Combat, "cultist", "fanatic")]
        [TestCase(GameFlowScreen.Combat, "trickster", "fraud")]
        [TestCase(GameFlowScreen.Combat, "enforcer", "executor")]
        [TestCase(GameFlowScreen.Combat, "final-boss", "bossStage")]
        [TestCase(GameFlowScreen.FinalBossReveal, "final-boss", "bossStage")]
        [TestCase(GameFlowScreen.RunVictory, "final-boss", "readyStage")]
        [TestCase(GameFlowScreen.RunDefeat, "gunslinger", "readyStage")]
        public void MOO02_U01_FlowScreenResolvesExpectedMood(
            GameFlowScreen screen,
            string profileKey,
            string expectedMoodId)
        {
            Assert.That(
                GameSceneMoodResolver.Resolve(screen, profileKey),
                Is.EqualTo(expectedMoodId));
        }

        [Test]
        public void MOO02_U02_VisibleCharacterConsumesPendingBgmWithoutEntranceAnimation()
        {
            MoodProfileSO profile = CreateProfile("boss");
            GameObject controllerObject = new GameObject("FlowControllerTest");
            GameObject characters = new GameObject("Characters");
            GameObject enemy = new GameObject("EnemyCharacter");
            controllerObject.SetActive(false);
            CharacterView characterView = enemy.AddComponent<CharacterView>();
            MoodController moodController =
                controllerObject.AddComponent<MoodController>();
            GameFlowController flowController =
                controllerObject.AddComponent<GameFlowController>();
            try
            {
                moodController.SetMoodImmediate(profile);
                SetPrivateField(flowController, "charactersRoot", characters);
                SetPrivateField(flowController, "enemyCharacter", characterView);
                SetPrivateField(flowController, "moodController", moodController);
                SetPrivateField(flowController, "_hasPresentedCharacters", true);

                FieldInfo pendingField = typeof(MoodController).GetField(
                    "_pendingBgmProfile",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo showCharacters = typeof(GameFlowController).GetMethod(
                    "ShowCharactersWithEntrance",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(pendingField, Is.Not.Null);
                Assert.That(showCharacters, Is.Not.Null);
                Assert.That(
                    pendingField.GetValue(moodController),
                    Is.SameAs(profile));

                showCharacters.Invoke(flowController, null);

                Assert.That(pendingField.GetValue(moodController), Is.Null);
                showCharacters.Invoke(flowController, null);
                Assert.That(pendingField.GetValue(moodController), Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(controllerObject);
                Object.DestroyImmediate(characters);
                Object.DestroyImmediate(enemy);
                Object.DestroyImmediate(profile);
            }
        }

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
        public void MOO01_U05_MoodProfileAcceptsOptionalBgmIds()
        {
            MoodProfileSO profile = CreateProfile(
                "round-1",
                bgmIds: new[] { "normalStage01", "normalStage02" });
            try
            {
                Assert.That(profile.HasBgmIds, Is.True);
                Assert.That(profile.TryGetRandomBgmId(out string bgmId), Is.True);
                Assert.That(
                    bgmId == "normalStage01" ||
                    bgmId == "normalStage02",
                    Is.True);
            }
            finally
            {
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void MOO01_U06_MoodProfileDisablesAudioReactiveLightningByDefault()
        {
            MoodProfileSO profile = CreateProfile("round-1");
            try
            {
                Assert.That(profile.EnableAudioReactiveLightning, Is.False);
                Assert.That(profile.HasLightningSfxIds, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void MOO01_U08_MoodProfileAcceptsLightningSfxIds()
        {
            MoodProfileSO profile = CreateProfile(
                "boss",
                lightningSfxIds: new[] { "lightning01", "lightning02" });
            try
            {
                Assert.That(profile.HasLightningSfxIds, Is.True);
                Assert.That(
                    profile.TryGetRandomLightningSfxId(out string sfxId),
                    Is.True);
                Assert.That(
                    sfxId == "lightning01" ||
                    sfxId == "lightning02",
                    Is.True);
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
                AssertColorApproximately(
                    properties.GetColor(GlassGlowColorId),
                    targetColor);
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

        [Test]
        public void MOO01_U07_AudioReactiveLightningBoostsAndRestoresLighting()
        {
            Color reactiveColor = new Color(0.2f, 0.1f, 0.05f, 1f);
            Color restoredColor = new Color(0.05f, 0.2f, 0.1f, 1f);
            MoodProfileSO reactiveProfile = CreateProfile(
                "boss",
                windowColor: reactiveColor,
                lightningSfxIds: new[] { "lightning01" },
                enableAudioReactiveLightning: true,
                lightningMaxBoost: 0.5f);
            MoodProfileSO normalProfile = CreateProfile(
                "normal",
                windowColor: restoredColor);
            GameObject root = new GameObject("MoodControllerTest");
            GameObject windowObject = new GameObject("Window");
            GameObject volumetricObject = new GameObject("VolumetricLight");
            GameObject enemyObject = new GameObject("EnemyLight");
            GameObject enteranceObject = new GameObject("EnteranceLight");
            MoodController controller = root.AddComponent<MoodController>();
            MeshRenderer renderer = windowObject.AddComponent<MeshRenderer>();
            Light volumetricLight = volumetricObject.AddComponent<Light>();
            Light enemyLight = enemyObject.AddComponent<Light>();
            Light enteranceLight = enteranceObject.AddComponent<Light>();
            Shader shader = Shader.Find("Shader/Uber Lit");
            Assert.That(shader, Is.Not.Null);
            Material material = new Material(shader);
            MaterialPropertyBlock properties = new MaterialPropertyBlock();
            try
            {
                volumetricLight.intensity = 2f;
                enemyLight.intensity = 3f;
                enteranceLight.intensity = 4f;
                material.SetColor(GlassGlowColorId, Color.white);
                renderer.sharedMaterial = material;
                SetPrivateField(
                    controller,
                    "windowGlassRenderers",
                    new Renderer[] { renderer });
                SetPrivateField(controller, "volumetricLight", volumetricLight);
                SetPrivateField(controller, "enemyLight", enemyLight);
                SetPrivateField(controller, "enteranceLight", enteranceLight);

                controller.SetMoodImmediate(reactiveProfile);
                bool pulsed = controller.TriggerAudioReactiveLightningPulse(0.5f);

                Assert.That(controller.IsAudioReactiveLightningActive, Is.True);
                Assert.That(pulsed, Is.True);
                Assert.That(volumetricLight.intensity, Is.EqualTo(3f));
                Assert.That(enemyLight.intensity, Is.EqualTo(4.5f));
                Assert.That(enteranceLight.intensity, Is.EqualTo(6f));
                renderer.GetPropertyBlock(properties);
                AssertColorApproximately(
                    properties.GetColor(GlassGlowColorId),
                    new Color(
                        reactiveColor.r * 1.5f,
                        reactiveColor.g * 1.5f,
                        reactiveColor.b * 1.5f,
                        reactiveColor.a));

                controller.SetMoodImmediate(normalProfile);

                Assert.That(controller.IsAudioReactiveLightningActive, Is.False);
                Assert.That(volumetricLight.intensity, Is.EqualTo(2f));
                Assert.That(enemyLight.intensity, Is.EqualTo(3f));
                Assert.That(enteranceLight.intensity, Is.EqualTo(4f));
                renderer.GetPropertyBlock(properties);
                AssertColorApproximately(
                    properties.GetColor(GlassGlowColorId),
                    restoredColor);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(windowObject);
                Object.DestroyImmediate(volumetricObject);
                Object.DestroyImmediate(enemyObject);
                Object.DestroyImmediate(enteranceObject);
                Object.DestroyImmediate(material);
                Object.DestroyImmediate(reactiveProfile);
                Object.DestroyImmediate(normalProfile);
            }
        }

        [Test]
        public void MOO01_U09_BlendFromAudioReactiveLightningKeepsBoostForSmoothFade()
        {
            Color startColor = Color.red;
            Color targetColor = Color.blue;
            MoodProfileSO reactiveProfile = CreateProfile(
                "boss",
                lightningSfxIds: new[] { "lightning01" },
                volumetricColor: startColor,
                enableAudioReactiveLightning: true,
                lightningMaxBoost: 0.5f);
            MoodProfileSO normalProfile = CreateProfile(
                "normal",
                volumetricColor: targetColor);
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
                volumetricLight.intensity = 2f;
                enemyLight.intensity = 3f;
                enteranceLight.intensity = 4f;
                SetPrivateField(controller, "volumetricLight", volumetricLight);
                SetPrivateField(controller, "enemyLight", enemyLight);
                SetPrivateField(controller, "enteranceLight", enteranceLight);

                controller.SetMoodImmediate(reactiveProfile);
                controller.TriggerAudioReactiveLightningPulse(0.5f);
                controller.BlendToMood(normalProfile, 1f);

                Assert.That(controller.IsAudioReactiveLightningActive, Is.False);
                Assert.That(
                    controller.CurrentAudioReactiveLightningBoost,
                    Is.EqualTo(0.5f));
                Assert.That(volumetricLight.intensity, Is.EqualTo(3f));
                Assert.That(enemyLight.intensity, Is.EqualTo(4.5f));
                Assert.That(enteranceLight.intensity, Is.EqualTo(6f));

                InvokeUpdate(controller, 30);

                Assert.That(volumetricLight.color.r, Is.LessThan(startColor.r));
                Assert.That(volumetricLight.color.b, Is.GreaterThan(startColor.b));
                Assert.That(volumetricLight.color, Is.Not.EqualTo(targetColor));
                Assert.That(volumetricLight.intensity, Is.LessThan(3f));
                Assert.That(volumetricLight.intensity, Is.GreaterThan(2f));

                InvokeUpdate(controller, 31);

                AssertColorApproximately(volumetricLight.color, targetColor);
                Assert.That(volumetricLight.intensity, Is.EqualTo(2f));

                controller.SetMoodImmediate(normalProfile);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(volumetricObject);
                Object.DestroyImmediate(enemyObject);
                Object.DestroyImmediate(enteranceObject);
                Object.DestroyImmediate(reactiveProfile);
                Object.DestroyImmediate(normalProfile);
            }
        }

        private static void AssertColorApproximately(Color actual, Color expected)
        {
            const float tolerance = 0.0001f;
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(tolerance));
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(tolerance));
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(tolerance));
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(tolerance));
        }

        private static void InvokeUpdate(MoodController controller, int count)
        {
            MethodInfo update = typeof(MoodController).GetMethod(
                "Update",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(update, Is.Not.Null);
            for (int i = 0; i < count; i++)
            {
                update.Invoke(controller, null);
            }
        }

        private static MoodProfileSO CreateProfile(
            string id,
            IEnumerable<string> bgmIds = null,
            IEnumerable<string> lightningSfxIds = null,
            Color? windowColor = null,
            Color? volumetricColor = null,
            Color? enemyColor = null,
            Color? enteranceColor = null,
            bool enableAudioReactiveLightning = false,
            float lightningMaxBoost = 0.8f)
        {
            MoodProfileSO profile = ScriptableObject.CreateInstance<MoodProfileSO>();
            SetPrivateField(profile, "id", id);
            SetPrivateField(
                profile,
                "bgmIds",
                bgmIds == null
                    ? new List<string>()
                    : new List<string>(bgmIds));
            SetPrivateField(
                profile,
                "lightningSfxIds",
                lightningSfxIds == null
                    ? new List<string>()
                    : new List<string>(lightningSfxIds));
            SetPrivateField(
                profile,
                "enableAudioReactiveLightning",
                enableAudioReactiveLightning);
            SetPrivateField(profile, "lightningMaxBoost", lightningMaxBoost);
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
