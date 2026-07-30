using System;
using System.Collections.Generic;
using Border.Settings;
using NUnit.Framework;
using UnityEngine;

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
    }
}
