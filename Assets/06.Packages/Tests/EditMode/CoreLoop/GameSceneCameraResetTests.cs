using System;
using System.Reflection;
using DiaBlackJack.GameScene;
using DiaBlackJack.StageProgression;
using DiaBlackJack.StageProgression.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DiaBlackJack.CoreLoop.Tests
{
    [Category("GSV18")]
    public sealed class GameSceneCameraResetTests
    {
        private const string CameraPrefabPath =
            "Assets/03. Prefabs/Map/Camera.prefab";

        private GameObject _root;
        private GameManager _manager;
        private ShopController _shop;
        private GameSceneCameraViewController _cameraController;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("GSV18 Camera Reset Test Root");
            GameObject cameraPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(CameraPrefabPath);
            Assert.That(cameraPrefab, Is.Not.Null);
            GameObject cameraInstance = UnityEngine.Object.Instantiate(cameraPrefab);
            cameraInstance.transform.SetParent(_root.transform);
            _cameraController = cameraInstance
                .GetComponentInChildren<GameSceneCameraViewController>(true);
            Assert.That(_cameraController, Is.Not.Null);

            _manager = _root.AddComponent<GameManager>();
            _shop = CreateChild("Shop").AddComponent<ShopController>();
            SetField(_manager, "cameraViewController", _cameraController);
            SetField(_manager, "shop", _shop);
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_root);
        }

        [Test]
        public void GSV18_U01_EnemyAppearanceResetsTableTopViewToCurrent()
        {
            Assert.That(
                _cameraController.SetView(GameSceneCameraView.TableTop),
                Is.True);

            _manager.PrepareEnemyAppearance(null);

            Assert.That(
                _cameraController.CurrentView,
                Is.EqualTo(GameSceneCameraView.Current));
        }

        [Test]
        public void GSV18_U02_StandaloneShopOpeningResetsTableTopViewToCurrent()
        {
            Assert.That(
                _cameraController.SetView(GameSceneCameraView.TableTop),
                Is.True);

            Assert.That(_manager.DebugOpenStandaloneShop(), Is.True);

            Assert.That(
                _cameraController.CurrentView,
                Is.EqualTo(GameSceneCameraView.Current));
        }

        [Test]
        public void GSV18_U03_FormalShopResetsOnlyOnInitialEntry()
        {
            StageProgressionViewModel model = CreateShopModel();
            Assert.That(
                _cameraController.SetView(GameSceneCameraView.TableTop),
                Is.True);

            Assert.That(_manager.BindFormalShop(model, 5), Is.True);
            Assert.That(
                _cameraController.CurrentView,
                Is.EqualTo(GameSceneCameraView.Current));

            Assert.That(
                _cameraController.SetView(GameSceneCameraView.TableTop),
                Is.True);
            Assert.That(_manager.BindFormalShop(model, 5), Is.True);
            Assert.That(
                _cameraController.CurrentView,
                Is.EqualTo(GameSceneCameraView.TableTop));
        }

        private GameObject CreateChild(string name)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(_root.transform);
            return child;
        }

        private static StageProgressionViewModel CreateShopModel()
        {
            return new StageProgressionViewModel(
                stageProgress: "STAGE 1 / 3",
                stageName: "ASH GATE",
                stageKind: "NORMAL COMBAT",
                playerSoul: "10 / 10 SOUL",
                state: StageProgressionState.StageCleared,
                message: "SHOP",
                canStartRun: false,
                canAdvanceStage: false,
                canRestartRun: false,
                rewardTier: string.Empty,
                rewardOptions: Array.Empty<BattleRewardOptionViewModel>(),
                canSelectReward: false,
                canSkipReward: false,
                rewardCompletionMessage: string.Empty,
                rewardResult: string.Empty,
                deckCount: 0,
                opponentOfferId: null,
                opponentCandidates: Array.Empty<OpponentCandidateViewModel>(),
                focusedOpponentProfileKey: null,
                canFocusOpponent: false,
                canConfirmOpponent: false,
                startingDemonGrantId: null,
                startingDemonGrantCards: Array.Empty<StartingDemonGrantCardViewModel>(),
                isStartingDemonReveal: false,
                playerGold: "5 GOLD",
                goldResult: string.Empty,
                isShop: true,
                shopOfferId: 1,
                shopCardOptions: Array.Empty<ShopCardOptionViewModel>(),
                shopOwnedCards: Array.Empty<ShopOwnedCardViewModel>(),
                lighterLabel: "LIGHTER",
                lighterPriceAmount: 2,
                isLighterUsed: false,
                whiskeyLabel: "WHISKEY",
                whiskeyPriceAmount: 2,
                isWhiskeyUsed: false,
                canRestAtShop: true,
                canLeaveShop: true,
                shopTransactionResult: string.Empty);
        }

        private static void SetField<T>(object target, string fieldName, T value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field: {fieldName}");
            field.SetValue(target, value);
        }
    }
}
