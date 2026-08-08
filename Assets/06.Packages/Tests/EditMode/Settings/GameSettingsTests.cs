using System;
using System.Reflection;
using Border.Settings;
using DiaBlackJack.GameScene;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DiaBlackJack.Settings.Tests
{
    public sealed class GameSettingsTests
    {
        private PlayerPrefsSettingsRepository _repository;
        private string _prefix;

        [SetUp]
        public void SetUp()
        {
            _prefix =
                "DiaBlackJack.Tests.Settings." + Guid.NewGuid().ToString("N");
            _repository = new PlayerPrefsSettingsRepository(_prefix);
        }

        [TearDown]
        public void TearDown()
        {
            _repository.DeleteAll();
        }

        [Test]
        [Category("SET06")]
        public void SET06_U01_DefaultsUseNormalTooltipAndExpectedVolumes()
        {
            GameSettingsDefaultsSO defaults =
                ScriptableObject.CreateInstance<GameSettingsDefaultsSO>();
            try
            {
                GameSettingsSnapshot snapshot =
                    defaults.CreateSnapshot();

                Assert.That(
                    snapshot.HoverTooltipSize,
                    Is.EqualTo(HoverTooltipSize.Normal));
                Assert.That(snapshot.MasterVolume, Is.EqualTo(1f));
                Assert.That(snapshot.BgmVolume, Is.EqualTo(0.8f));
                Assert.That(snapshot.SfxVolume, Is.EqualTo(1f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(defaults);
            }
        }

        [Test]
        [Category("SET06")]
        public void SET06_U02_SnapshotClampsInvalidAudioAndTooltipSize()
        {
            GameSettingsSnapshot snapshot = new GameSettingsSnapshot(
                (HoverTooltipSize)999,
                float.NaN,
                -0.2f,
                float.PositiveInfinity);

            Assert.That(
                    snapshot.HoverTooltipSize,
                    Is.EqualTo(HoverTooltipSize.Normal));
            Assert.That(snapshot.MasterVolume, Is.EqualTo(0f));
            Assert.That(snapshot.BgmVolume, Is.EqualTo(0f));
            Assert.That(snapshot.SfxVolume, Is.EqualTo(1f));
        }

        [Test]
        [Category("SET06")]
        public void SET06_U03_PlayerPrefsRepositoryRoundTripsAllSettings()
        {
            GameSettingsSnapshot expected = new GameSettingsSnapshot(
                HoverTooltipSize.Large,
                0.75f,
                0.5f,
                0.25f);

            Assert.That(_repository.TrySave(expected), Is.True);
            Assert.That(
                _repository.TryLoad(out GameSettingsSnapshot actual),
                Is.True);
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        [Category("SET06")]
        public void SET06_U04_MissingPlayerPrefsReturnsFalse()
        {
            Assert.That(
                _repository.TryLoad(out GameSettingsSnapshot snapshot),
                Is.False);
            Assert.That(snapshot, Is.EqualTo(default(GameSettingsSnapshot)));
        }

        [TestCase(HoverTooltipSize.Small, 1f)]
        [TestCase(HoverTooltipSize.Normal, 1.3f)]
        [TestCase(HoverTooltipSize.Large, 1.5f)]
        [Category("SET06")]
        public void SET06_U05_TooltipSizesMapToExpectedScale(
            HoverTooltipSize size,
            float expected)
        {
            Vector3 scale = HoverTooltipSizeUtility.GetScale(size);

            Assert.That(scale.x, Is.EqualTo(expected));
            Assert.That(scale.y, Is.EqualTo(expected));
            Assert.That(scale.z, Is.EqualTo(1f));
        }

        [Test]
        [Category("SET06")]
        public void SET06_U06_VersionOnePreservesAudioAndUsesNormalTooltip()
        {
            PlayerPrefs.SetInt(_prefix + ".Version", 1);
            PlayerPrefs.SetInt(_prefix + ".ResolutionWidth", 1280);
            PlayerPrefs.SetInt(_prefix + ".ResolutionHeight", 720);
            PlayerPrefs.SetInt(_prefix + ".WindowMode", 0);
            PlayerPrefs.SetFloat(_prefix + ".MasterVolume", 0.7f);
            PlayerPrefs.SetFloat(_prefix + ".BgmVolume", 0.6f);
            PlayerPrefs.SetFloat(_prefix + ".SfxVolume", 0.5f);

            Assert.That(
                _repository.TryLoad(out GameSettingsSnapshot snapshot),
                Is.True);
            Assert.That(
                snapshot.HoverTooltipSize,
                Is.EqualTo(HoverTooltipSize.Normal));
            Assert.That(snapshot.MasterVolume, Is.EqualTo(0.7f));
            Assert.That(snapshot.BgmVolume, Is.EqualTo(0.6f));
            Assert.That(snapshot.SfxVolume, Is.EqualTo(0.5f));
        }

        [Test]
        [Category("SET06")]
        public void SET06_U07_SaveRemovesDeprecatedDisplayKeys()
        {
            PlayerPrefs.SetInt(_prefix + ".ResolutionWidth", 1280);
            PlayerPrefs.SetInt(_prefix + ".ResolutionHeight", 720);
            PlayerPrefs.SetInt(_prefix + ".WindowMode", 0);
            GameSettingsSnapshot snapshot = new GameSettingsSnapshot(
                HoverTooltipSize.Small,
                1f,
                0.8f,
                1f);

            Assert.That(_repository.TrySave(snapshot), Is.True);
            Assert.That(
                PlayerPrefs.HasKey(_prefix + ".ResolutionWidth"),
                Is.False);
            Assert.That(
                PlayerPrefs.HasKey(_prefix + ".ResolutionHeight"),
                Is.False);
            Assert.That(
                PlayerPrefs.HasKey(_prefix + ".WindowMode"),
                Is.False);
        }

        [TestCase(-1, 3, 2)]
        [TestCase(0, 3, 0)]
        [TestCase(2, 3, 2)]
        [TestCase(3, 3, 0)]
        [TestCase(7, 3, 1)]
        public void SET03_U01_ArrowSelectorWrapsBothDirections(
            int index,
            int count,
            int expected)
        {
            Assert.That(
                UISettingsArrowSelector.WrapIndex(index, count),
                Is.EqualTo(expected));
        }

        [Test]
        [Category("SET06")]
        public void SET06_U08_SettingsPrefabUsesOnlyTooltipSizeSelector()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/03. Prefabs/UI/PauseSettingsCanvas.prefab");

            Assert.That(prefab, Is.Not.Null);
            Transform settingsPanel = prefab.transform.Find("SettingsPanel");
            Assert.That(settingsPanel, Is.Not.Null);
            Assert.That(
                settingsPanel.Find("TooltipSizeSelector"),
                Is.Not.Null);
            Assert.That(
                settingsPanel.Find("ResolutionSelector"),
                Is.Null);
            Assert.That(
                settingsPanel.Find("WindowModeSelector"),
                Is.Null);

            PauseSettingsController controller =
                prefab.GetComponent<PauseSettingsController>();
            SerializedObject serializedController =
                new SerializedObject(controller);
            Assert.That(
                serializedController.FindProperty("tooltipSizeSelector")
                    .objectReferenceValue,
                Is.Not.Null);
        }

        [Test]
        [Category("SET06")]
        public void SET06_U09_HudRespondsToTooltipSizePreview()
        {
            GameObject settingsSystemInstance = null;
            GameObject hudInstance = null;
            try
            {
                typeof(SettingsSystem)
                    .GetField(
                        "_current",
                        BindingFlags.Static | BindingFlags.NonPublic)
                    .SetValue(null, null);
                GameObject settingsSystemPrefab =
                    AssetDatabase.LoadAssetAtPath<GameObject>(
                        "Assets/03. Prefabs/Manager/SettingsSystem.prefab");
                GameObject hudPrefab =
                    AssetDatabase.LoadAssetAtPath<GameObject>(
                        "Assets/03. Prefabs/UI/HUD.prefab");
                Assert.That(settingsSystemPrefab, Is.Not.Null);
                Assert.That(hudPrefab, Is.Not.Null);

                settingsSystemInstance =
                    UnityEngine.Object.Instantiate(settingsSystemPrefab);
                SettingsSystem settingsSystem =
                    settingsSystemInstance.GetComponent<SettingsSystem>();
                typeof(SettingsSystem)
                    .GetField(
                        "_current",
                        BindingFlags.Static | BindingFlags.NonPublic)
                    .SetValue(null, settingsSystem);
                typeof(SettingsSystem)
                    .GetField(
                        "_repository",
                        BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(settingsSystem, _repository);
                typeof(SettingsSystem)
                    .GetField(
                        "_snapshot",
                        BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(
                        settingsSystem,
                        new GameSettingsSnapshot(
                            HoverTooltipSize.Normal,
                            1f,
                            0.8f,
                            1f));

                hudInstance = UnityEngine.Object.Instantiate(hudPrefab);
                GameHudView hud = hudInstance.GetComponent<GameHudView>();
                Assert.That(hud, Is.Not.Null);
                typeof(GameHudView)
                    .GetMethod(
                        "Awake",
                        BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(hud, null);
                RectTransform tooltip = hudInstance.transform
                    .Find("CardHoverTooltipRoot") as RectTransform;
                Assert.That(tooltip, Is.Not.Null);
                Assert.That(tooltip.localScale.x, Is.EqualTo(1.3f));

                settingsSystem.PreviewHoverTooltipSize(
                    HoverTooltipSize.Large);
                Assert.That(tooltip.localScale, Is.EqualTo(
                    new Vector3(1.5f, 1.5f, 1f)));

                settingsSystem.PreviewHoverTooltipSize(
                    HoverTooltipSize.Small);
                Assert.That(tooltip.localScale, Is.EqualTo(Vector3.one));
            }
            finally
            {
                typeof(SettingsSystem)
                    .GetField(
                        "_current",
                        BindingFlags.Static | BindingFlags.NonPublic)
                    .SetValue(null, null);
                if (hudInstance != null)
                {
                    UnityEngine.Object.DestroyImmediate(hudInstance);
                }

                if (settingsSystemInstance != null)
                {
                    UnityEngine.Object.DestroyImmediate(
                        settingsSystemInstance);
                }
            }
        }

        [Test]
        [Category("SET06")]
        public void SET06_U10_TooltipSelectorCyclesBothDirections()
        {
            GameObject instance = null;
            try
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/03. Prefabs/UI/PauseSettingsCanvas.prefab");
                Assert.That(prefab, Is.Not.Null);

                instance = UnityEngine.Object.Instantiate(prefab);
                Transform selectorRoot = instance.transform.Find(
                    "SettingsPanel/TooltipSizeSelector");
                UISettingsArrowSelector selector =
                    selectorRoot.GetComponent<UISettingsArrowSelector>();
                typeof(UISettingsArrowSelector)
                    .GetMethod(
                        "Awake",
                        BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(selector, null);
                selector.SetOptions(
                    new[] { "작게", "보통", "크게" },
                    (int)HoverTooltipSize.Normal);

                Button left = selectorRoot.Find("LeftButton")
                    .GetComponent<Button>();
                Button right = selectorRoot.Find("RightButton")
                    .GetComponent<Button>();
                Component value = selectorRoot.Find("Value").gameObject
                    .GetComponent("TextMeshProUGUI");
                SerializedObject serializedValue =
                    new SerializedObject(value);
                SerializedProperty valueText =
                    serializedValue.FindProperty("m_text");

                serializedValue.Update();
                Assert.That(valueText.stringValue, Is.EqualTo("보통"));
                right.onClick.Invoke();
                Assert.That(selector.Index, Is.EqualTo(2));
                serializedValue.Update();
                Assert.That(valueText.stringValue, Is.EqualTo("크게"));
                right.onClick.Invoke();
                Assert.That(selector.Index, Is.Zero);
                serializedValue.Update();
                Assert.That(valueText.stringValue, Is.EqualTo("작게"));
                left.onClick.Invoke();
                Assert.That(selector.Index, Is.EqualTo(2));
                serializedValue.Update();
                Assert.That(valueText.stringValue, Is.EqualTo("크게"));
            }
            finally
            {
                if (instance != null)
                {
                    UnityEngine.Object.DestroyImmediate(instance);
                }
            }
        }

        [Test]
        [Category("MMUI01")]
        public void MMUI01_U02_SettingsOnlyPanelClosesWithoutChangingTimeScale()
        {
            GameObject settingsSystemInstance = null;
            GameObject settingsCanvasInstance = null;
            float previousTimeScale = Time.timeScale;
            try
            {
                GameObject settingsSystemPrefab =
                    AssetDatabase.LoadAssetAtPath<GameObject>(
                        "Assets/03. Prefabs/Manager/SettingsSystem.prefab");
                GameObject settingsCanvasPrefab =
                    AssetDatabase.LoadAssetAtPath<GameObject>(
                        "Assets/03. Prefabs/UI/PauseSettingsCanvas.prefab");
                Assert.That(settingsSystemPrefab, Is.Not.Null);
                Assert.That(settingsCanvasPrefab, Is.Not.Null);

                settingsSystemInstance =
                    UnityEngine.Object.Instantiate(settingsSystemPrefab);
                SettingsSystem settingsSystem =
                    settingsSystemInstance.GetComponent<SettingsSystem>();
                typeof(SettingsSystem)
                    .GetField(
                        "_current",
                        BindingFlags.Static | BindingFlags.NonPublic)
                    .SetValue(null, settingsSystem);
                typeof(SettingsSystem)
                    .GetField(
                        "_repository",
                        BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(settingsSystem, _repository);
                typeof(SettingsSystem)
                    .GetField(
                        "_snapshot",
                        BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(
                        settingsSystem,
                        new GameSettingsSnapshot(
                            HoverTooltipSize.Normal,
                            1f,
                            0.8f,
                            1f));
                settingsCanvasInstance =
                    UnityEngine.Object.Instantiate(settingsCanvasPrefab);
                PauseSettingsController controller =
                    settingsCanvasInstance.GetComponent<PauseSettingsController>();
                Assert.That(controller, Is.Not.Null);

                SerializedObject serializedController =
                    new SerializedObject(controller);
                serializedController.FindProperty("settingsOnlyMode").boolValue =
                    true;
                serializedController.ApplyModifiedPropertiesWithoutUndo();
                typeof(PauseSettingsController)
                    .GetMethod(
                        "Awake",
                        BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(controller, null);

                int closedCount = 0;
                controller.SettingsPanelClosed += () => closedCount++;
                Time.timeScale = 0.75f;

                Assert.That(controller.OpenSettingsPanel(), Is.True);
                Assert.That(
                    controller.State,
                    Is.EqualTo(PauseMenuState.Settings));
                Assert.That(Time.timeScale, Is.EqualTo(0.75f));

                Button backButton = null;
                Button[] buttons =
                    settingsCanvasInstance.GetComponentsInChildren<Button>(true);
                for (int i = 0; i < buttons.Length; i++)
                {
                    if (buttons[i].name == "BackButton")
                    {
                        backButton = buttons[i];
                        break;
                    }
                }

                Assert.That(backButton, Is.Not.Null);
                backButton.onClick.Invoke();

                Assert.That(controller.State, Is.EqualTo(PauseMenuState.Hidden));
                Assert.That(Time.timeScale, Is.EqualTo(0.75f));
                Assert.That(closedCount, Is.EqualTo(1));
            }
            finally
            {
                Time.timeScale = previousTimeScale;
                typeof(SettingsSystem)
                    .GetField(
                        "_current",
                        BindingFlags.Static | BindingFlags.NonPublic)
                    .SetValue(null, null);
                if (settingsCanvasInstance != null)
                {
                    UnityEngine.Object.DestroyImmediate(settingsCanvasInstance);
                }

                if (settingsSystemInstance != null)
                {
                    UnityEngine.Object.DestroyImmediate(settingsSystemInstance);
                }
            }
        }

        [Test]
        [Category("UIR01")]
        public void UIR01_U07_SettingsPrefabUsesBrushPanelsAndNestedDefaultButtons()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/03. Prefabs/UI/PauseSettingsCanvas.prefab");
            Assert.That(prefab, Is.Not.Null);
            string[] panelPaths =
            {
                "PausePanel", "SettingsPanel", "QuitConfirmationPanel"
            };
            for (int index = 0; index < panelPaths.Length; index++)
            {
                Image image = prefab.transform.Find(panelPaths[index])
                    .GetComponent<Image>();
                Assert.That(image.sprite.name, Is.EqualTo("Brush_UI_8"));
                Assert.That(image.type, Is.EqualTo(Image.Type.Sliced));
            }

            string[] buttonPaths =
            {
                "PausePanel/ContinueButton",
                "PausePanel/SettingsButton",
                "PausePanel/QuitButton",
                "SettingsPanel/BackButton",
                "QuitConfirmationPanel/ConfirmQuitButton",
                "QuitConfirmationPanel/CancelQuitButton"
            };
            for (int index = 0; index < buttonPaths.Length; index++)
            {
                GameObject button = prefab.transform.Find(buttonPaths[index])
                    .gameObject;
                GameObject source =
                    PrefabUtility.GetCorrespondingObjectFromSource(button);
                Assert.That(
                    AssetDatabase.GetAssetPath(source),
                    Is.EqualTo("Assets/03. Prefabs/UI/DefaultButton.prefab"),
                    buttonPaths[index]);
            }
        }
    }
}
