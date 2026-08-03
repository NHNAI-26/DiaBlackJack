using System.Linq;
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
            Color baseColor = renderer.color;
            Vector3 baseScale = _root.transform.localScale;
            Assert.That(
                view.TrySetEnemyProfile(EnemyCombatProfileCatalog.GunslingerKey),
                Is.True);
            view.Render(CharacterVisualState.AttackThreatened, "GUESS");

            view.EnterMerchant();
            Assert.That(renderer.sprite, Is.SameAs(_merchant));

            view.ExitMerchant();
            Assert.That(renderer.sprite, Is.SameAs(_attackThreatened));
            Assert.That(renderer.color, Is.EqualTo(baseColor));
            Assert.That(_root.transform.localScale, Is.EqualTo(baseScale));
        }

        [Test]
        public void GSV01_U04_RenderKeepsAuthoredColorAndScaleWhileShowingActionLabel()
        {
            CharacterView view = ConfigureView();
            SpriteRenderer renderer = _root.GetComponent<SpriteRenderer>();
            Component actionLabel = CreateActionLabel();
            SerializedObject serialized = new SerializedObject(view);
            serialized.FindProperty("actionLabel").objectReferenceValue = actionLabel;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Color authoredColor = new Color(0.25f, 0.45f, 0.75f, 1f);
            Vector3 authoredScale = new Vector3(1.4f, 0.8f, 1f);
            renderer.color = authoredColor;
            _root.transform.localScale = authoredScale;

            view.Render(CharacterVisualState.UseCard, "USE: REVOLVER");

            Assert.That(renderer.color, Is.EqualTo(authoredColor));
            Assert.That(_root.transform.localScale, Is.EqualTo(authoredScale));
            Behaviour actionLabelBehaviour = actionLabel as Behaviour;
            Assert.That(actionLabelBehaviour, Is.Not.Null);
            Assert.That(actionLabelBehaviour.enabled, Is.True);
            SerializedObject labelSerialized = new SerializedObject(actionLabel);
            Assert.That(
                labelSerialized.FindProperty("m_text").stringValue,
                Is.EqualTo("USE: REVOLVER"));
        }

        [Test]
        public void GSV01_U05_ActionLabelAlwaysRendersInFrontOfEnemySprite()
        {
            CharacterView view = ConfigureView();
            SpriteRenderer spriteRenderer = _root.GetComponent<SpriteRenderer>();
            Component actionLabel = CreateActionLabel();
            SerializedObject serialized = new SerializedObject(view);
            serialized.FindProperty("actionLabel").objectReferenceValue = actionLabel;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Renderer labelRenderer = actionLabel.GetComponent<Renderer>();
            spriteRenderer.sortingLayerName = "Default";
            spriteRenderer.sortingOrder = 7;
            labelRenderer.sortingOrder = 0;

            view.Render(CharacterVisualState.Idle, "HIT");

            Assert.That(
                labelRenderer.sortingLayerID,
                Is.EqualTo(spriteRenderer.sortingLayerID));
            Assert.That(
                labelRenderer.sortingOrder,
                Is.GreaterThan(spriteRenderer.sortingOrder));
        }

        [Test]
        public void CUM09_U01_RevolverHitStaysThreatenedUntilImpactEvent()
        {
            CharacterVisualState beforeImpact = GameManager.ResolveRevolverTimedVisual(
                CombatantSide.Enemy,
                CharacterVisualState.Attacked,
                impactPending: true,
                impactTargetSide: CombatantSide.Enemy);
            CharacterVisualState afterImpact = GameManager.ResolveRevolverTimedVisual(
                CombatantSide.Enemy,
                CharacterVisualState.Attacked,
                impactPending: false,
                impactTargetSide: CombatantSide.Enemy);

            Assert.That(beforeImpact, Is.EqualTo(CharacterVisualState.AttackThreatened));
            Assert.That(afterImpact, Is.EqualTo(CharacterVisualState.Attacked));
        }

        [Test]
        public void CUM09_U02_RevolverImpactReceiverRaisesOnlyExplicitImpactEvent()
        {
            RevolverAnimationEventReceiver receiver =
                _root.AddComponent<RevolverAnimationEventReceiver>();
            int impactCount = 0;
            receiver.ShotImpact += () => impactCount++;

            receiver.NotifyShotImpact();

            Assert.That(impactCount, Is.EqualTo(1));
        }

        [TestCase(
            "Assets/05. Arts/Animation/Revolover/Revolver_Shoot_PlayerSuccess.anim")]
        [TestCase(
            "Assets/05. Arts/Animation/Revolover/Revolver_ShootEnemySuccess.anim")]
        public void CUM09_U03_RevolverSuccessImpactMatchesGunFireFrame(string assetPath)
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
            AnimationEvent[] events = AnimationUtility.GetAnimationEvents(clip);
            AnimationEvent impact = events.Single(item =>
                item.functionName == "NotifyShotImpact");
            AnimationEvent gunFire = events.Single(item =>
                item.functionName == "PlayVfx" &&
                item.stringParameter == "gun-fire");

            Assert.That(impact.time, Is.EqualTo(gunFire.time).Within(0.0001f));
        }

        [Test]
        public void CUM14_U03_KnifeHitStaysThreatenedUntilImpactEvent()
        {
            CharacterVisualState beforeImpact = GameManager.ResolveKnifeTimedVisual(
                CombatantSide.Enemy,
                CharacterVisualState.Attacked,
                impactPending: true,
                impactTargetSide: CombatantSide.Enemy);
            CharacterVisualState afterImpact = GameManager.ResolveKnifeTimedVisual(
                CombatantSide.Enemy,
                CharacterVisualState.Attacked,
                impactPending: false,
                impactTargetSide: CombatantSide.Enemy);

            Assert.That(beforeImpact, Is.EqualTo(CharacterVisualState.AttackThreatened));
            Assert.That(afterImpact, Is.EqualTo(CharacterVisualState.Attacked));
        }

        [Test]
        public void CUM14_U04_KnifeImpactReceiverRaisesOnlyExplicitImpactEvent()
        {
            KnifeAnimationEventReceiver receiver =
                _root.AddComponent<KnifeAnimationEventReceiver>();
            int impactCount = 0;
            receiver.KnifeImpact += () => impactCount++;

            receiver.NotifyKnifeImpact();

            Assert.That(impactCount, Is.EqualTo(1));
        }

        [TestCase("Assets/05. Arts/Animation/Knife/Knife_Attack.anim", "ShakeCamera")]
        [TestCase("Assets/05. Arts/Animation/Knife/Knife_Attack_Enemy.anim", "ShakeCameraTap")]
        public void CUM14_U05_KnifeImpactMatchesAuthoredHitFrame(
            string assetPath,
            string hitEventName)
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
            AnimationEvent[] events = AnimationUtility.GetAnimationEvents(clip);
            AnimationEvent impact = events.Single(item =>
                item.functionName == "NotifyKnifeImpact");
            AnimationEvent authoredHit = events.Single(item =>
                item.functionName == hitEventName);

            Assert.That(impact.time, Is.EqualTo(authoredHit.time).Within(0.0001f));
        }

        [TestCase(CombatantSide.Player)]
        [TestCase(CombatantSide.Enemy)]
        public void CUM14_U06_KnifeAnimationKeepsCurrentCameraView(
            CombatantSide actorSide)
        {
            Assert.That(
                GameManager.ResolveKnifeCameraView(actorSide),
                Is.EqualTo(GameSceneCameraView.Current));
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

        private Component CreateActionLabel()
        {
            GameObject labelObject = new GameObject("ActionLabel");
            labelObject.transform.SetParent(_root.transform);
            System.Type textMeshProType = System.Type.GetType(
                "TMPro.TextMeshPro, Unity.TextMeshPro");
            Assert.That(textMeshProType, Is.Not.Null);
            return labelObject.AddComponent(textMeshProType);
        }
    }
}
