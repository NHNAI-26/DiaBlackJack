using System;
using System.Collections.Generic;
using System.Reflection;
using Border.Settings;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DiaBlackJack.Settings.Tests
{
    public sealed class GameSettingsTests
    {
        private PlayerPrefsSettingsRepository _repository;

        [SetUp]
        public void SetUp()
        {
            _repository = new PlayerPrefsSettingsRepository(
                "DiaBlackJack.Tests.Settings." + Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void TearDown()
        {
            _repository.DeleteAll();
        }

        [Test]
        public void SET01_U01_DefaultsUseBorderlessAndExpectedVolumes()
        {
            GameSettingsDefaultsSO defaults =
                ScriptableObject.CreateInstance<GameSettingsDefaultsSO>();
            try
            {
                GameSettingsSnapshot snapshot =
                    defaults.CreateSnapshot(2560, 1440);

                Assert.That(snapshot.ResolutionWidth, Is.EqualTo(2560));
                Assert.That(snapshot.ResolutionHeight, Is.EqualTo(1440));
                Assert.That(
                    snapshot.WindowMode,
                    Is.EqualTo(GameWindowMode.BorderlessFullscreen));
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
        public void SET01_U02_SnapshotClampsInvalidAudioAndWindowMode()
        {
            GameSettingsSnapshot snapshot = new GameSettingsSnapshot(
                1920,
                1080,
                (GameWindowMode)999,
                float.NaN,
                -0.2f,
                float.PositiveInfinity);

            Assert.That(
                snapshot.WindowMode,
                Is.EqualTo(GameWindowMode.BorderlessFullscreen));
            Assert.That(snapshot.MasterVolume, Is.EqualTo(0f));
            Assert.That(snapshot.BgmVolume, Is.EqualTo(0f));
            Assert.That(snapshot.SfxVolume, Is.EqualTo(1f));
        }

        [Test]
        public void SET01_U03_PlayerPrefsRepositoryRoundTripsAllSettings()
        {
            GameSettingsSnapshot expected = new GameSettingsSnapshot(
                1920,
                1080,
                GameWindowMode.ExclusiveFullscreen,
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
        public void SET01_U04_MissingPlayerPrefsReturnsFalse()
        {
            Assert.That(
                _repository.TryLoad(out GameSettingsSnapshot snapshot),
                Is.False);
            Assert.That(snapshot, Is.EqualTo(default(GameSettingsSnapshot)));
        }

        [Test]
        public void SET02_U01_ResolutionCatalogDeduplicatesAndKeepsHighestRate()
        {
            List<DisplayResolutionOption> source =
                new List<DisplayResolutionOption>
            {
                new DisplayResolutionOption(1920, 1080, 60, 1),
                new DisplayResolutionOption(1280, 720, 60, 1),
                new DisplayResolutionOption(1920, 1080, 144, 1),
                new DisplayResolutionOption(0, 0, 60, 1)
            };

            List<DisplayResolutionOption> options =
                SettingsGraphicsUtility.BuildResolutionOptions(
                    source,
                    new DisplayResolutionOption(2560, 1440, 60, 1));

            Assert.That(options.Count, Is.EqualTo(2));
            Assert.That(options[0].Width, Is.EqualTo(1280));
            Assert.That(options[0].Height, Is.EqualTo(720));
            Assert.That(options[1].Width, Is.EqualTo(1920));
            Assert.That(options[1].Height, Is.EqualTo(1080));
            Assert.That(options[1].RefreshRate, Is.EqualTo(144d));
        }

        [Test]
        public void SET02_U02_EmptyResolutionCatalogUsesFallback()
        {
            DisplayResolutionOption fallback =
                new DisplayResolutionOption(2560, 1440, 120, 1);

            List<DisplayResolutionOption> options =
                SettingsGraphicsUtility.BuildResolutionOptions(
                    Array.Empty<DisplayResolutionOption>(),
                    fallback);

            Assert.That(options, Has.Count.EqualTo(1));
            Assert.That(options[0], Is.EqualTo(fallback));
        }

        [TestCase(
            GameWindowMode.Windowed,
            FullScreenMode.Windowed)]
        [TestCase(
            GameWindowMode.ExclusiveFullscreen,
            FullScreenMode.ExclusiveFullScreen)]
        [TestCase(
            GameWindowMode.BorderlessFullscreen,
            FullScreenMode.FullScreenWindow)]
        public void SET02_U03_WindowModesMapToUnityModes(
            GameWindowMode mode,
            FullScreenMode expected)
        {
            Assert.That(
                SettingsGraphicsUtility.GetFullScreenMode(mode),
                Is.EqualTo(expected));
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
                            1920,
                            1080,
                            GameWindowMode.Windowed,
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
