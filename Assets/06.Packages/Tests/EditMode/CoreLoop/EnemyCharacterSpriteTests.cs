using DiaBlackJack.CoreLoop;
using DiaBlackJack.GameScene;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DiaBlackJack.CoreLoop.Tests
{
    public sealed class EnemyCharacterSpriteTests
    {
        private GameObject _root;
        private Texture2D _texture;
        private Sprite _defaultState;
        private Sprite _attackThreatened;
        private Sprite _attacked;
        private Sprite _merchant;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("EnemyCharacterSpriteTests");
            _root.AddComponent<SpriteRenderer>();
            _root.AddComponent<CharacterView>();

            _texture = new Texture2D(4, 1);
            _defaultState = CreateSprite(0);
            _attackThreatened = CreateSprite(1);
            _attacked = CreateSprite(2);
            _merchant = CreateSprite(3);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_defaultState);
            Object.DestroyImmediate(_attackThreatened);
            Object.DestroyImmediate(_attacked);
            Object.DestroyImmediate(_merchant);
            Object.DestroyImmediate(_texture);
            Object.DestroyImmediate(_root);
        }

        [Test]
        public void GSV01_U01_ProfileSpritesFollowDefaultThreatenedAndAttackedStates()
        {
            CharacterView view = ConfigureView();
            SpriteRenderer renderer = _root.GetComponent<SpriteRenderer>();

            Assert.That(
                view.TrySetEnemyProfile(EnemyCombatProfileCatalog.GunslingerKey),
                Is.True);

            view.Render(CharacterVisualState.Idle, string.Empty);
            Assert.That(renderer.sprite, Is.SameAs(_defaultState));

            view.Render(CharacterVisualState.AttackThreatened, "GUESS");
            Assert.That(renderer.sprite, Is.SameAs(_attackThreatened));

            view.Render(CharacterVisualState.Attacked, "HIT!");
            Assert.That(renderer.sprite, Is.SameAs(_attacked));
        }

        [Test]
        public void GSV01_U02_InvalidProfileDoesNotReplaceActiveSpriteSet()
        {
            CharacterView view = ConfigureView();
            SpriteRenderer renderer = _root.GetComponent<SpriteRenderer>();
            Assert.That(
                view.TrySetEnemyProfile(EnemyCombatProfileCatalog.GunslingerKey),
                Is.True);

            Assert.That(view.TrySetEnemyProfile("missing-profile"), Is.False);
            view.Render(CharacterVisualState.Attacked, "HIT!");

            Assert.That(renderer.sprite, Is.SameAs(_attacked));
        }

        [Test]
        public void GSV01_U03_MerchantExitRestoresCurrentCombatExpression()
        {
            CharacterView view = ConfigureView();
            SpriteRenderer renderer = _root.GetComponent<SpriteRenderer>();
            Assert.That(
                view.TrySetEnemyProfile(EnemyCombatProfileCatalog.GunslingerKey),
                Is.True);
            view.Render(CharacterVisualState.AttackThreatened, "GUESS");

            view.EnterMerchant();
            Assert.That(renderer.sprite, Is.SameAs(_merchant));

            view.ExitMerchant();
            Assert.That(renderer.sprite, Is.SameAs(_attackThreatened));
        }

        private CharacterView ConfigureView()
        {
            CharacterView view = _root.GetComponent<CharacterView>();
            SerializedObject serialized = new SerializedObject(view);
            SerializedProperty profiles =
                serialized.FindProperty("enemySpriteProfiles");
            profiles.arraySize = 1;
            SerializedProperty profile = profiles.GetArrayElementAtIndex(0);
            profile.FindPropertyRelative("profileKey").stringValue =
                EnemyCombatProfileCatalog.GunslingerKey;
            profile.FindPropertyRelative("defaultState").objectReferenceValue =
                _defaultState;
            profile.FindPropertyRelative("attackThreatened").objectReferenceValue =
                _attackThreatened;
            profile.FindPropertyRelative("attacked").objectReferenceValue = _attacked;
            serialized.FindProperty("merchantSprite").objectReferenceValue = _merchant;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private Sprite CreateSprite(int x)
        {
            return Sprite.Create(
                _texture,
                new Rect(x, 0, 1, 1),
                new Vector2(0.5f, 0.5f));
        }
    }
}
