using System;
using System.Collections.Generic;
using System.Linq;
using DiaBlackJack.Content;
using DiaBlackJack.GameScene;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DiaBlackJack.CoreLoop.Tests
{
    [Category("GSH02")]
    public sealed class HoverDescriptionSOTests
    {
        private const string Folder =
            "Assets/02. ScriptableObjects/HoverDescriptions/";

        private static readonly string[] AssetNames =
        {
            "player-draw-deck",
            "player-discard-deck",
            "enemy-draw-deck",
            "enemy-discard-deck",
            "hit",
            "stand",
            "change",
            "contract-paper",
            "codex",
            "mammon-die",
            "lighter",
            "whiskey"
        };

        [Test]
        public void GSH02_U01_DefaultAndStateTemplatesReplaceRuntimeTokens()
        {
            HoverDescriptionSO description = CreateDescription(
                "위스키",
                "영혼을 {amount} 회복합니다. 가격: {gold} {price}",
                "soul-full",
                "영혼을 {amount} 회복합니다. 가격: {gold} {price}\n" +
                "영혼이 이미 가득 찼습니다.");
            try
            {
                var tokens = new Dictionary<string, string>
                {
                    { "amount", "2" },
                    { "gold", "G" },
                    { "price", "3" }
                };

                Assert.That(
                    description.ResolveDescription(null, tokens),
                    Is.EqualTo("영혼을 2 회복합니다. 가격: G 3"));
                Assert.That(
                    description.ResolveDescription("soul-full", tokens),
                    Does.EndWith("영혼이 이미 가득 찼습니다."));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(description);
            }
        }

        [TestCase("", "본문")]
        [TestCase("제목", "")]
        public void GSH02_U02_EmptyRequiredTextIsRejected(
            string title,
            string body)
        {
            HoverDescriptionSO description = CreateDescription(title, body);
            try
            {
                Assert.That(
                    () => description.ValidateOrThrow(),
                    Throws.TypeOf<InvalidOperationException>());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(description);
            }
        }

        [Test]
        public void GSH02_U03_DuplicateStateKeysAreRejected()
        {
            HoverDescriptionSO description = CreateDescription(
                "위스키",
                "기본",
                "soul-full",
                "가득 참");
            SerializedObject serialized = new SerializedObject(description);
            SerializedProperty states = serialized.FindProperty(
                "stateDescriptions");
            states.arraySize = 2;
            SetState(states.GetArrayElementAtIndex(1), "soul-full", "중복");
            serialized.ApplyModifiedPropertiesWithoutUndo();

            try
            {
                Assert.That(
                    () => description.ValidateOrThrow(),
                    Throws.TypeOf<InvalidOperationException>());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(description);
            }
        }

        [Test]
        public void GSH02_U04_UnresolvedAndUnsupportedTokensAreRejected()
        {
            HoverDescriptionSO unresolved = CreateDescription(
                "라이터",
                "가격: {gold} {price}");
            HoverDescriptionSO unsupported = CreateDescription(
                "라이터",
                "가격: {unknown}");
            try
            {
                Assert.That(
                    () => unresolved.ResolveDescription(
                        null,
                        new Dictionary<string, string>
                        {
                            { "gold", "G" }
                        }),
                    Throws.TypeOf<InvalidOperationException>());
                Assert.That(
                    () => unsupported.ValidateOrThrow(),
                    Throws.TypeOf<InvalidOperationException>());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(unresolved);
                UnityEngine.Object.DestroyImmediate(unsupported);
            }
        }

        [Test]
        public void GSH02_U05_TwelveKoreanAssetsExistAndValidate()
        {
            foreach (string assetName in AssetNames)
            {
                HoverDescriptionSO description =
                    AssetDatabase.LoadAssetAtPath<HoverDescriptionSO>(
                        Folder + assetName + ".asset");
                Assert.That(description, Is.Not.Null, assetName);
                Assert.That(
                    description.Title.Any(character =>
                        character >= '\uac00' && character <= '\ud7a3'),
                    Is.True,
                    assetName);
                Assert.That(
                    () => description.ValidateOrThrow(),
                    Throws.Nothing,
                    assetName);
            }
        }

        [Test]
        public void GSH02_U06_RelatedPrefabsReferenceEveryDescriptionAsset()
        {
            GameObject table = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/03. Prefabs/TableObjects/Table Controller.prefab");
            GameObject die = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Resources/Prefabs/MammonDie_Prototype.prefab");
            Assert.That(table, Is.Not.Null);
            Assert.That(die, Is.Not.Null);

            HoverDescriptionTarget[] tableTargets =
                table.GetComponentsInChildren<HoverDescriptionTarget>(true);
            HoverDescriptionTarget[] dieTargets =
                die.GetComponentsInChildren<HoverDescriptionTarget>(true);
            Assert.That(tableTargets, Has.Length.EqualTo(12));
            Assert.That(dieTargets, Has.Length.EqualTo(1));
            Assert.That(
                tableTargets.Concat(dieTargets)
                    .All(target => target.HasRequiredReferences),
                Is.True);
            Assert.That(
                tableTargets.Concat(dieTargets)
                    .Select(target => target.Description.name)
                    .Distinct(StringComparer.Ordinal),
                Is.EquivalentTo(AssetNames));

            Assert.That(
                tableTargets.Single(target => target.name == "Hit")
                    .Description.name,
                Is.EqualTo("hit"));
            Assert.That(
                tableTargets.Single(target => target.name == "EnemyRemain")
                    .Description.name,
                Is.EqualTo("enemy-draw-deck"));
            Assert.That(
                tableTargets.Single(target => target.name == "EnemyDiscard")
                    .Description.name,
                Is.EqualTo("enemy-discard-deck"));
            Assert.That(
                table.transform.Find(
                    "ShopItems/UtilityItemHolder/ShopItem_Lighter/HoverBadge"),
                Is.Null);
            Assert.That(
                table.transform.Find(
                    "ShopItems/UtilityItemHolder/ShopItem_Whiskey/HoverBadge"),
                Is.Null);
        }

        private static HoverDescriptionSO CreateDescription(
            string title,
            string body,
            string stateKey = null,
            string stateBody = null)
        {
            HoverDescriptionSO description =
                ScriptableObject.CreateInstance<HoverDescriptionSO>();
            description.hideFlags = HideFlags.HideAndDontSave;
            SerializedObject serialized = new SerializedObject(description);
            serialized.FindProperty("title").stringValue = title;
            serialized.FindProperty("descriptionTemplate").stringValue = body;
            SerializedProperty states = serialized.FindProperty(
                "stateDescriptions");
            states.arraySize = string.IsNullOrEmpty(stateKey) ? 0 : 1;
            if (states.arraySize == 1)
            {
                SetState(states.GetArrayElementAtIndex(0), stateKey, stateBody);
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return description;
        }

        private static void SetState(
            SerializedProperty state,
            string key,
            string body)
        {
            state.FindPropertyRelative("stateKey").stringValue = key;
            state.FindPropertyRelative("descriptionTemplate").stringValue = body;
        }
    }
}
