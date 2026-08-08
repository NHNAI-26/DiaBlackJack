#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Border.UI;
using DiaBlackJack.Content;
using DiaBlackJack.CoreLoop;
using DiaBlackJack.CoreLoop.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DiaBlackJack.GameScene.Editor
{
    /// <summary>Creates the authored combat HUD hierarchy once, without runtime GameObject creation.</summary>
    internal static class GameHudCombatPrefabBuilder
    {
        private const string HudPrefabPath = "Assets/03. Prefabs/UI/HUD.prefab";
        private const string DefaultButtonPrefabPath =
            "Assets/03. Prefabs/UI/DefaultButton.prefab";
        private const string RevolverNumberSelectorPrefabPath =
            "Assets/03. Prefabs/UI/GameScene/RevolverNumberSelector.prefab";
        private const string CombatPromptPrefabPath =
            "Assets/03. Prefabs/UI/GameScene/CombatPrompt.prefab";
        private const string CombatPromptLayoutRootName = "LayoutRoot";
        private const string CombatPromptCatalogPath =
            "Assets/02. ScriptableObjects/UI/CombatPromptCatalog.asset";
        private const string CoreLoopTestScenePath =
            "Assets/00. Scenes/CoreLoopTest.unity";
        private const string BrushAssetPath = "Assets/05. Arts/UI/Brush_UI.psd";
        private const string BrushSelectAssetPath =
            "Assets/05. Arts/UI/brush_select.png";
        private const string DefaultFontAssetPath =
            "Assets/05. Arts/Fonts/전주완판본체/전주완판본 순R SDF.asset";
        private const string CardContentCatalogPath =
            "Assets/02. ScriptableObjects/Cards/CardContentCatalog.asset";
        private const string ContractDetailTextMaterialPath =
            "Assets/05. Arts/Material/Card/DemonCardHoverDetail_Text.mat";
        private const int OptionSlotCount = 100;

        [MenuItem("DiaBlackJack/Build GameScene Combat HUD")]
        private static void Build()
        {
            CombatPromptCatalogSO promptCatalog =
                CreateOrLoadCombatPromptCatalog();
            ValidateCombatPromptPrefabAsset(promptCatalog);
            BuildRevolverNumberSelectorPrefabAsset();
            GameObject hudRoot = PrefabUtility.LoadPrefabContents(HudPrefabPath);
            try
            {
                GameHudView hud = hudRoot.GetComponent<GameHudView>();
                if (hud == null)
                {
                    throw new InvalidOperationException("HUD prefab is missing GameHudView.");
                }

                Transform existing = hudRoot.transform.Find("CombatControls");
                if (existing != null)
                {
                    UnityEngine.Object.DestroyImmediate(existing.gameObject);
                }

                TMP_FontAsset font = hudRoot.GetComponentInChildren<TMP_Text>(true)?.font;
                if (font == null)
                {
                    throw new InvalidOperationException("HUD prefab has no TMP font to reuse.");
                }

                Material contractDetailTextMaterial =
                    AssetDatabase.LoadAssetAtPath<Material>(
                        ContractDetailTextMaterialPath);
                if (contractDetailTextMaterial == null)
                {
                    throw new InvalidOperationException(
                        "Contract detail TMP material asset was not found.");
                }

                Sprite panelBrush = FindSprite("Brush_UI_8");
                CreateShopLeaveControl(hudRoot, hud);

                RectTransform controls = CreateRect("CombatControls", hudRoot.transform);
                Stretch(controls);
                CombatPromptView combatPrompt =
                    InstallCombatPrompt(controls);

                RectTransform tooltip = CreatePanel(
                    "ActionTooltip", controls, panelBrush, new Color(0.08f, 0.06f, 0.07f, 0.96f));
                tooltip.anchorMin = new Vector2(0.5f, 0f);
                tooltip.anchorMax = new Vector2(0.5f, 0f);
                tooltip.sizeDelta = new Vector2(500f, 112f);
                TMP_Text tooltipText = CreateText(
                    "Text", tooltip, font, 18f, TextAlignmentOptions.Center);
                Stretch(tooltipText.rectTransform, 16f);
                tooltip.gameObject.SetActive(false);

                RectTransform optionPanel = CreateOverlay("OptionPanel", controls);
                TMP_Text optionPrompt = CreateText(
                    "HeaderText", optionPanel, font, 22f, TextAlignmentOptions.Center);
                optionPrompt.rectTransform.anchorMin = new Vector2(0.5f, 1f);
                optionPrompt.rectTransform.anchorMax = new Vector2(0.5f, 1f);
                optionPrompt.rectTransform.pivot = new Vector2(0.5f, 1f);
                optionPrompt.rectTransform.anchoredPosition = new Vector2(0f, -86f);
                optionPrompt.rectTransform.sizeDelta = new Vector2(860f, 74f);

                ScrollRect optionScroll = CreateOptionScroll(
                    optionPanel,
                    panelBrush,
                    out GameHudChoiceButton[] optionSlots);

                RectTransform contractDetailPanel = CreateOverlay(
                    "ContractDetailPanel",
                    controls);
                GameHudContractDetailView contractDetail = CreateContractDetail(
                    contractDetailPanel,
                    font,
                    panelBrush,
                    contractDetailTextMaterial);

                CardContentCatalogSO contentCatalog =
                    AssetDatabase.LoadAssetAtPath<CardContentCatalogSO>(CardContentCatalogPath);
                if (contentCatalog == null)
                {
                    throw new InvalidOperationException("Card content catalog asset was not found.");
                }

                AssignHudReferences(
                    hud,
                    controls.gameObject,
                    tooltip,
                    tooltipText,
                    optionPanel.gameObject,
                    optionPrompt,
                    combatPrompt,
                    optionScroll,
                    optionSlots,
                    contractDetailPanel.gameObject,
                    contractDetail,
                    contentCatalog);
                InstallRevolverNumberSelector(hudRoot, hud);
                PrefabUtility.SaveAsPrefabAsset(hudRoot, HudPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(hudRoot);
            }
        }

        [MenuItem("DiaBlackJack/Build Revolver Number Selector")]
        private static void BuildRevolverNumberSelector()
        {
            BuildRevolverNumberSelectorPrefabAsset();
            GameObject hudRoot = PrefabUtility.LoadPrefabContents(HudPrefabPath);
            try
            {
                GameHudView hud = hudRoot.GetComponent<GameHudView>();
                if (hud == null)
                {
                    throw new InvalidOperationException("HUD prefab is missing GameHudView.");
                }

                InstallRevolverNumberSelector(hudRoot, hud);
                PrefabUtility.SaveAsPrefabAsset(hudRoot, HudPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(hudRoot);
            }
        }

        [MenuItem("DiaBlackJack/Build Combat Prompt")]
        private static void BuildCombatPrompt()
        {
            CombatPromptCatalogSO catalog = CreateOrLoadCombatPromptCatalog();
            ValidateCombatPromptPrefabAsset(catalog);

            GameObject hudRoot = PrefabUtility.LoadPrefabContents(HudPrefabPath);
            try
            {
                GameHudView hud = hudRoot.GetComponent<GameHudView>();
                Transform controls = hudRoot.transform.Find("CombatControls");
                if (hud == null || controls == null)
                {
                    throw new InvalidOperationException(
                        "HUD prefab requires GameHudView and CombatControls.");
                }

                CombatPromptView combatPrompt =
                    InstallCombatPrompt(controls);
                TMP_Text header = controls.Find("OptionPanel/HeaderText")
                    ?.GetComponent<TMP_Text>();
                if (header == null)
                {
                    Transform legacyPrompt = controls.Find("OptionPanel/Prompt");
                    if (legacyPrompt != null)
                    {
                        legacyPrompt.name = "HeaderText";
                        header = legacyPrompt.GetComponent<TMP_Text>();
                    }
                }

                SerializedObject serialized = new SerializedObject(hud);
                serialized.FindProperty("combatHeaderText").objectReferenceValue = header;
                serialized.FindProperty("combatPromptView").objectReferenceValue =
                    combatPrompt;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(hudRoot, HudPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(hudRoot);
            }

            WireCoreLoopTestCatalog(catalog);
            AssetDatabase.SaveAssets();
        }

        [MenuItem("DiaBlackJack/Build Shop Leave Control")]
        private static void BuildShopLeaveControl()
        {
            GameObject hudRoot = PrefabUtility.LoadPrefabContents(HudPrefabPath);
            try
            {
                GameHudView hud = hudRoot.GetComponent<GameHudView>();
                if (hud == null)
                {
                    throw new InvalidOperationException(
                        "HUD prefab is missing GameHudView.");
                }

                CreateShopLeaveControl(hudRoot, hud);
                PrefabUtility.SaveAsPrefabAsset(hudRoot, HudPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(hudRoot);
            }
        }

        [MenuItem("DiaBlackJack/Build Default Button Prefab")]
        private static void BuildDefaultButtonPrefab()
        {
            TMP_FontAsset font =
                AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DefaultFontAssetPath);
            if (font == null)
            {
                throw new InvalidOperationException(
                    "Default button TMP font asset was not found.");
            }

            GameObject root = new GameObject(
                "DefaultButton",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button),
                typeof(UIButtonScaleFeedback),
                typeof(UISelectableSoundHook));
            try
            {
                RectTransform rect = root.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(234f, 66f);

                Image background = root.GetComponent<Image>();
                background.sprite = FindSprite("Brush_UI_9");
                background.type = Image.Type.Simple;
                background.preserveAspect = true;

                Button button = root.GetComponent<Button>();
                button.targetGraphic = background;

                TMP_Text label = CreateText(
                    "Label",
                    rect,
                    font,
                    28f,
                    TextAlignmentOptions.Center);
                label.text = "버튼";
                label.fontStyle = FontStyles.Bold;
                Stretch(label.rectTransform, 8f);

                PrefabUtility.SaveAsPrefabAsset(root, DefaultButtonPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void BuildRevolverNumberSelectorPrefabAsset()
        {
            TMP_FontAsset font =
                AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DefaultFontAssetPath);
            GameObject defaultButtonPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(DefaultButtonPrefabPath);
            if (font == null || defaultButtonPrefab == null)
            {
                throw new InvalidOperationException(
                    "Revolver selector requires the default TMP font and button prefab.");
            }

            Sprite circleSprite = FindSprite(
                BrushSelectAssetPath,
                "brush_select_circle");
            Sprite leftSprite = FindSprite(
                BrushSelectAssetPath,
                "brush_select_left");
            Sprite rightSprite = FindSprite(
                BrushSelectAssetPath,
                "brush_select_right");

            GameObject root = new GameObject(
                "RevolverNumberSelector",
                typeof(RectTransform));
            try
            {
                RectTransform rootRect = root.GetComponent<RectTransform>();
                Stretch(rootRect);

                RectTransform circle = CreateRect("NumberCircle", rootRect);
                SetCenteredRect(circle, Vector2.zero, new Vector2(180f, 180f));
                Image circleImage = circle.gameObject.AddComponent<Image>();
                circleImage.sprite = circleSprite;
                circleImage.type = Image.Type.Simple;
                circleImage.preserveAspect = true;
                circleImage.color = new Color(0.03f, 0.025f, 0.025f, 0.98f);
                circleImage.raycastTarget = false;

                TMP_Text number = CreateText(
                    "Number",
                    circle,
                    font,
                    72f,
                    TextAlignmentOptions.Center);
                number.fontStyle = FontStyles.Bold;
                number.fontSizeMin = 42f;
                Stretch(number.rectTransform, 18f);

                Button previous = CreateSelectorArrow(
                    "PreviousButton",
                    rootRect,
                    leftSprite,
                    new Vector2(-160f, 0f));
                Button next = CreateSelectorArrow(
                    "NextButton",
                    rootRect,
                    rightSprite,
                    new Vector2(160f, 0f));

                GameObject confirmObject =
                    (GameObject)PrefabUtility.InstantiatePrefab(
                        defaultButtonPrefab,
                        rootRect);
                confirmObject.name = "ConfirmButton";
                RectTransform confirmRect = confirmObject.GetComponent<RectTransform>();
                Button confirm = confirmObject.GetComponent<Button>();
                TMP_Text confirmLabel =
                    confirmObject.transform.Find("Label")?.GetComponent<TMP_Text>();
                if (confirmRect == null || confirm == null || confirmLabel == null)
                {
                    throw new InvalidOperationException(
                        "Default button prefab is missing RectTransform, Button, or Label.");
                }

                SetCenteredRect(
                    confirmRect,
                    new Vector2(0f, -132f),
                    new Vector2(234f, 66f));
                confirmLabel.text = "확정";

                RevolverNumberSelectorView selector =
                    root.AddComponent<RevolverNumberSelectorView>();
                SerializedObject selectorData = new SerializedObject(selector);
                selectorData.FindProperty("numberText").objectReferenceValue = number;
                selectorData.FindProperty("previousButton").objectReferenceValue = previous;
                selectorData.FindProperty("nextButton").objectReferenceValue = next;
                selectorData.FindProperty("confirmButton").objectReferenceValue = confirm;
                selectorData.ApplyModifiedPropertiesWithoutUndo();

                root.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(root, RevolverNumberSelectorPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static CombatPromptCatalogSO CreateOrLoadCombatPromptCatalog()
        {
            CombatPromptCatalogSO catalog =
                AssetDatabase.LoadAssetAtPath<CombatPromptCatalogSO>(
                    CombatPromptCatalogPath);
            if (catalog != null)
            {
                return catalog;
            }

            const string parentFolder = "Assets/02. ScriptableObjects";
            const string uiFolder = parentFolder + "/UI";
            if (!AssetDatabase.IsValidFolder(uiFolder))
            {
                AssetDatabase.CreateFolder(parentFolder, "UI");
            }

            catalog = ScriptableObject.CreateInstance<CombatPromptCatalogSO>();
            catalog.ReplaceEntriesForEditor(CreateDefaultPromptEntries());
            AssetDatabase.CreateAsset(catalog, CombatPromptCatalogPath);
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static IReadOnlyList<CombatPromptCatalogSO.Entry>
            CreateDefaultPromptEntries()
        {
            return new[]
            {
                new CombatPromptCatalogSO.Entry(
                    CombatPromptId.ChangeCard,
                    "교체할 카드를 선택하세요."),
                new CombatPromptCatalogSO.Entry(
                    CombatPromptId.ManualAutoPistolDeclareNumber,
                    "상대 비공개 카드의 숫자를 선택하세요."),
                new CombatPromptCatalogSO.Entry(
                    CombatPromptId.ManualCrystalOrbChooseCard,
                    "가져올 카드를 선택하거나 건너뛰세요."),
                new CombatPromptCatalogSO.Entry(
                    CombatPromptId.ManualThreatHammerChooseOpponentCard,
                    "버릴 상대 공개 카드를 선택하세요."),
                new CombatPromptCatalogSO.Entry(
                    CombatPromptId.AutomaticLieDetectorDeclareNumber,
                    "{source}: 상대 비공개 카드와 비교할 숫자를 선택하세요."),
                new CombatPromptCatalogSO.Entry(
                    CombatPromptId.AutomaticPoisonDecision,
                    "{source}: 독극물의 효과를 선택하세요."),
                new CombatPromptCatalogSO.Entry(
                    CombatPromptId.AutomaticFlamethrowerChooseDiscard,
                    "{source}: 버릴 공개 카드 한 장을 선택하거나 건너뛰세요."),
                new CombatPromptCatalogSO.Entry(
                    CombatPromptId.AutomaticPocketWatchChooseManualCard,
                    "{source}: 다시 사용할 수동 카드 한 장을 선택하거나 건너뛰세요."),
                new CombatPromptCatalogSO.Entry(
                    CombatPromptId.AutomaticPocketWatchChooseDisposition,
                    "{source}: 회중시계를 유지할지 버릴지 선택하세요."),
                new CombatPromptCatalogSO.Entry(
                    CombatPromptId.AutomaticResurrectionHerbDecision,
                    "{source}: 영혼 1을 지불하고 패를 다시 받을지 선택하세요."),
                new CombatPromptCatalogSO.Entry(
                    CombatPromptId.DemonChooseContract,
                    "계약할 악마를 선택하세요."),
                new CombatPromptCatalogSO.Entry(
                    CombatPromptId.DemonBelphegorTopCard,
                    "{context}\n확인한 덱 위 카드를 처리하세요."),
                new CombatPromptCatalogSO.Entry(
                    CombatPromptId.DemonMammonReroll,
                    "현재 값을 유지할지 다시 굴릴지 선택하세요."),
                new CombatPromptCatalogSO.Entry(
                    CombatPromptId.DemonMammonApplyDie,
                    "최종 승부에 현재 주사위 값을 포함할지 선택하세요."),
                new CombatPromptCatalogSO.Entry(
                    CombatPromptId.DemonSatanDeclareFirstNumber,
                    "첫 번째 숫자를 선택하세요. ({current}/{required})"),
                new CombatPromptCatalogSO.Entry(
                    CombatPromptId.DemonSatanDeclareSecondNumber,
                    "두 번째 숫자를 선택하세요. ({current}/{required})"),
                new CombatPromptCatalogSO.Entry(
                    CombatPromptId.DemonBeelzebubChooseOwnerCard,
                    "버릴 내 공개 카드를 선택하세요. ({current}/{required})"),
                new CombatPromptCatalogSO.Entry(
                    CombatPromptId.DemonBeelzebubChooseOpponentCard,
                    "버릴 상대 공개 카드를 선택하세요. ({current}/{required})"),
                new CombatPromptCatalogSO.Entry(
                    CombatPromptId.DemonAsmodeusForceOpponentHit,
                    "차례 시작에 상대를 히트시킬지 선택하세요."),
                new CombatPromptCatalogSO.Entry(
                    CombatPromptId.DemonSatanTurnStartChoice,
                    "차례 시작에 사탄 능력을 사용할지 선택하세요."),
                new CombatPromptCatalogSO.Entry(
                    CombatPromptId.DemonPaimonChooseDeck,
                    "카드를 추방할 덱을 선택하세요."),
                new CombatPromptCatalogSO.Entry(
                    CombatPromptId.DemonPaimonChooseExileCard,
                    "전투 종료까지 추방할 카드를 선택하세요."),
                new CombatPromptCatalogSO.Entry(
                    CombatPromptId.DemonBelialChooseOpponentCard,
                    "가져와 즉시 사용할 상대 공개 카드를 선택하세요."),
                new CombatPromptCatalogSO.Entry(
                    CombatPromptId.DemonLuciferChooseAdditionalContract,
                    "추가로 계약할 악마를 선택하거나 건너뛰세요.")
            };
        }

        private static void ValidateCombatPromptPrefabAsset(
            CombatPromptCatalogSO catalog)
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(CombatPromptPrefabPath);
            CombatPromptView view = prefab == null
                ? null
                : prefab.GetComponent<CombatPromptView>();
            Transform layoutRoot = prefab == null
                ? null
                : prefab.transform.Find(CombatPromptLayoutRootName);
            if (view == null || !view.HasRequiredReferences || layoutRoot == null)
            {
                throw new InvalidOperationException(
                    "Author CombatPrompt.prefab with CombatPromptView, LayoutRoot, and all required references before building the HUD.");
            }

            SerializedObject serialized = new SerializedObject(view);
            if (serialized.FindProperty("catalog").objectReferenceValue != catalog)
            {
                throw new InvalidOperationException(
                    "CombatPrompt.prefab must reference the shared CombatPromptCatalog asset.");
            }
        }

        private static CombatPromptView InstallCombatPrompt(
            Transform controls)
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(CombatPromptPrefabPath);
            Transform existing = controls.Find("CombatPrompt");
            if (existing != null)
            {
                CombatPromptView existingView =
                    existing.GetComponent<CombatPromptView>();
                GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(
                    existing.gameObject);
                if (existingView != null &&
                    source == prefab &&
                    existing.Find(CombatPromptLayoutRootName) != null)
                {
                    Stretch(existing.GetComponent<RectTransform>());
                    return existingView;
                }

                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(
                prefab,
                controls);
            instance.name = "CombatPrompt";
            Stretch(instance.GetComponent<RectTransform>());
            return instance.GetComponent<CombatPromptView>();
        }

        private static void WireCoreLoopTestCatalog(CombatPromptCatalogSO catalog)
        {
            Scene scene = SceneManager.GetSceneByPath(CoreLoopTestScenePath);
            bool openedForEdit = !scene.IsValid() || !scene.isLoaded;
            if (openedForEdit)
            {
                scene = EditorSceneManager.OpenScene(
                    CoreLoopTestScenePath,
                    OpenSceneMode.Additive);
            }

            try
            {
                CoreLoopView view = null;
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    view = root.GetComponentInChildren<CoreLoopView>(true);
                    if (view != null)
                    {
                        break;
                    }
                }

                if (view == null)
                {
                    throw new InvalidOperationException(
                        "CoreLoopTest scene is missing CoreLoopView.");
                }

                SerializedObject serialized = new SerializedObject(view);
                serialized.FindProperty("combatPromptCatalog").objectReferenceValue =
                    catalog;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            finally
            {
                if (openedForEdit && scene.IsValid())
                {
                    EditorSceneManager.CloseScene(scene, removeScene: true);
                }
            }
        }

        private static Button CreateSelectorArrow(
            string name,
            Transform parent,
            Sprite sprite,
            Vector2 position)
        {
            RectTransform root = CreateRect(name, parent);
            SetCenteredRect(root, position, new Vector2(104f, 104f));
            Image image = root.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = new Color(0.03f, 0.025f, 0.025f, 0.98f);

            Button button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            root.gameObject.AddComponent<UIButtonScaleFeedback>();
            root.gameObject.AddComponent<UISelectableSoundHook>();
            return button;
        }

        private static void InstallRevolverNumberSelector(
            GameObject hudRoot,
            GameHudView hud)
        {
            Transform controls = hudRoot.transform.Find("CombatControls");
            GameObject selectorPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    RevolverNumberSelectorPrefabPath);
            if (controls == null || selectorPrefab == null)
            {
                throw new InvalidOperationException(
                    "HUD CombatControls or revolver selector prefab was not found.");
            }

            Transform existing = controls.Find("RevolverNumberSelector");
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }

            GameObject instance =
                (GameObject)PrefabUtility.InstantiatePrefab(selectorPrefab, controls);
            instance.name = "RevolverNumberSelector";
            Stretch(instance.GetComponent<RectTransform>());
            instance.SetActive(false);

            RevolverNumberSelectorView selector =
                instance.GetComponent<RevolverNumberSelectorView>();
            if (selector == null)
            {
                throw new InvalidOperationException(
                    "Revolver selector prefab is missing its view component.");
            }

            SerializedObject hudData = new SerializedObject(hud);
            hudData.FindProperty("revolverNumberSelector").objectReferenceValue = selector;
            hudData.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateShopLeaveControl(
            GameObject hudRoot,
            GameHudView hud)
        {
            Transform existing = hudRoot.transform.Find("ShopLeaveRoot");
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }

            RectTransform layer = CreateRect("ShopLeaveRoot", hudRoot.transform);
            Stretch(layer);
            Canvas canvas = layer.gameObject.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 150;
            layer.gameObject.AddComponent<GraphicRaycaster>();

            GameObject defaultButtonPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(DefaultButtonPrefabPath);
            if (defaultButtonPrefab == null)
            {
                throw new InvalidOperationException(
                    "Default button prefab was not found. Build it before rebuilding the HUD.");
            }

            GameObject buttonObject =
                (GameObject)PrefabUtility.InstantiatePrefab(defaultButtonPrefab, layer);
            buttonObject.name = "ShopLeaveButton";
            RectTransform buttonRoot = buttonObject.GetComponent<RectTransform>();
            Button button = buttonObject.GetComponent<Button>();
            TMP_Text label = buttonObject.transform.Find("Label")?.GetComponent<TMP_Text>();
            if (buttonRoot == null || button == null || label == null)
            {
                throw new InvalidOperationException(
                    "Default button prefab is missing RectTransform, Button, or Label.");
            }

            buttonRoot.anchorMin = new Vector2(0.5f, 0f);
            buttonRoot.anchorMax = new Vector2(0.5f, 0f);
            buttonRoot.pivot = new Vector2(0.5f, 0f);
            buttonRoot.anchoredPosition = new Vector2(0f, 24f);
            buttonRoot.sizeDelta = new Vector2(234f, 66f);
            label.text = "상점 나가기";

            SerializedObject serialized = new SerializedObject(hud);
            serialized.FindProperty("shopLeaveRoot").objectReferenceValue =
                layer.gameObject;
            serialized.FindProperty("shopLeaveButton").objectReferenceValue =
                button;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            layer.gameObject.SetActive(false);
        }

        private static ScrollRect CreateOptionScroll(
            RectTransform parent,
            Sprite panelBrush,
            out GameHudChoiceButton[] slots)
        {
            RectTransform scrollRoot = CreatePanel(
                "OptionScroll", parent, panelBrush, new Color(0.08f, 0.06f, 0.07f, 0.96f));
            scrollRoot.anchorMin = new Vector2(0.5f, 0.5f);
            scrollRoot.anchorMax = new Vector2(0.5f, 0.5f);
            scrollRoot.pivot = new Vector2(0.5f, 0.5f);
            scrollRoot.anchoredPosition = new Vector2(0f, -20f);
            scrollRoot.sizeDelta = new Vector2(700f, 400f);
            ScrollRect scroll = scrollRoot.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            RectTransform viewport = CreateRect("Viewport", scrollRoot);
            Stretch(viewport, 14f);
            viewport.gameObject.AddComponent<RectMask2D>();

            RectTransform content = CreateRect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            GameObject defaultButtonPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(DefaultButtonPrefabPath);
            if (defaultButtonPrefab == null)
            {
                throw new InvalidOperationException(
                    "Default button prefab was not found. Build it before rebuilding the HUD.");
            }

            slots = new GameHudChoiceButton[OptionSlotCount];
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i] = CreateOptionSlot(
                    $"OptionSlot_{i + 1:000}",
                    content,
                    defaultButtonPrefab);
            }

            scroll.viewport = viewport;
            scroll.content = content;
            return scroll;
        }

        private static GameHudChoiceButton CreateOptionSlot(
            string name,
            RectTransform parent,
            GameObject defaultButtonPrefab)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(
                defaultButtonPrefab,
                parent);
            instance.name = name;
            RectTransform root = instance.GetComponent<RectTransform>();
            Button button = instance.GetComponent<Button>();
            TMP_Text label = instance.transform.Find("Label")?.GetComponent<TMP_Text>();
            if (root == null || button == null || label == null)
            {
                throw new InvalidOperationException(
                    "Default button prefab is missing RectTransform, Button, or Label.");
            }

            LayoutElement layout = instance.GetComponent<LayoutElement>() ??
                instance.AddComponent<LayoutElement>();
            layout.preferredHeight = 66f;
            GameHudChoiceButton choice =
                instance.GetComponent<GameHudChoiceButton>() ??
                instance.AddComponent<GameHudChoiceButton>();
            AssignChoiceButtonReferences(choice, button, label);
            return choice;
        }

        private static GameHudContractDetailView CreateContractDetail(
            RectTransform parent,
            TMP_FontAsset font,
            Sprite panelBrush,
            Material textMaterial)
        {
            RectTransform layout = CreateRect("DetailLayout", parent);
            layout.anchorMin = new Vector2(0.14f, 0.18f);
            layout.anchorMax = new Vector2(0.86f, 0.78f);
            layout.pivot = new Vector2(0.5f, 0.5f);
            layout.offsetMin = Vector2.zero;
            layout.offsetMax = Vector2.zero;

            RectTransform root = CreatePanel(
                "ContractDetail",
                layout,
                panelBrush,
                new Color(0.15f, 0.1f, 0.12f, 0.99f));
            root.sizeDelta = new Vector2(440f, 390f);
            Stretch(root);

            RectTransform face = CreateRect("Face", root);
            SetRect(
                face,
                new Vector2(0.0272f, 0.0595f),
                new Vector2(0.2011f, 0.8214f),
                Vector2.zero,
                Vector2.zero);
            Image faceImage = face.gameObject.AddComponent<Image>();
            faceImage.preserveAspect = true;
            faceImage.raycastTarget = false;

            RectTransform titlePanel = CreatePanel(
                "Title", root, panelBrush, new Color(0.07f, 0.05f, 0.06f, 0.94f));
            SetRect(
                titlePanel,
                new Vector2(0.0272f, 0.8381f),
                new Vector2(0.2228f, 0.9524f),
                Vector2.zero,
                Vector2.zero);
            TMP_Text title = CreateText(
                "txtTitle",
                titlePanel,
                font,
                30f,
                TextAlignmentOptions.Center);
            title.fontSizeMin = 20f;
            title.fontStyle = FontStyles.Bold;
            title.fontSharedMaterial = textMaterial;
            Stretch(title.rectTransform, 8f);

            RectTransform abilityPanel = CreatePanel(
                "Ability", root, panelBrush, new Color(0.07f, 0.05f, 0.06f, 0.94f));
            SetRect(
                abilityPanel,
                new Vector2(0.2391f, 0.4881f),
                new Vector2(0.9728f, 0.8929f),
                Vector2.zero,
                Vector2.zero);
            TMP_Text abilityLabel = CreateText(
                "txtAbilityLabel",
                abilityPanel,
                font,
                24f,
                TextAlignmentOptions.TopLeft);
            ConfigureDetailLabel(
                abilityLabel,
                "ACTIVE",
                new Color(0.827f, 0.294f, 0.247f, 1f));
            abilityLabel.fontSharedMaterial = textMaterial;
            TMP_Text ability = CreateText(
                "txtAbility",
                abilityPanel,
                font,
                24f,
                TextAlignmentOptions.TopLeft);
            ability.fontSizeMin = 20f;
            ability.fontSharedMaterial = textMaterial;
            ConfigureDetailBody(ability.rectTransform);

            RectTransform costPanel = CreatePanel(
                "Cost", root, panelBrush, new Color(0.07f, 0.05f, 0.06f, 0.94f));
            SetRect(
                costPanel,
                new Vector2(0.2391f, 0.0714f),
                new Vector2(0.9728f, 0.4286f),
                Vector2.zero,
                Vector2.zero);
            TMP_Text costLabel = CreateText(
                "txtCostLabel",
                costPanel,
                font,
                24f,
                TextAlignmentOptions.TopLeft);
            ConfigureDetailLabel(
                costLabel,
                "COST",
                new Color(0.843f, 0.647f, 0.231f, 1f));
            costLabel.fontSharedMaterial = textMaterial;
            TMP_Text cost = CreateText(
                "txtCost",
                costPanel,
                font,
                24f,
                TextAlignmentOptions.TopLeft);
            cost.fontSizeMin = 20f;
            cost.fontSharedMaterial = textMaterial;
            ConfigureDetailBody(cost.rectTransform);

            GameHudContractDetailView detail =
                root.gameObject.AddComponent<GameHudContractDetailView>();
            AssignContractDetailReferences(
                detail,
                faceImage,
                title,
                abilityLabel,
                ability,
                costLabel,
                cost);
            return detail;
        }

        private static void ConfigureDetailLabel(
            TMP_Text label,
            string value,
            Color color)
        {
            label.enableAutoSizing = false;
            label.fontStyle = FontStyles.Bold;
            label.color = color;
            label.text = value;
            SetRect(
                label.rectTransform,
                new Vector2(0f, 1f),
                Vector2.one,
                new Vector2(40f, -58f),
                new Vector2(-40f, -30f));
        }

        private static void ConfigureDetailBody(RectTransform body)
        {
            SetRect(
                body,
                Vector2.zero,
                Vector2.one,
                new Vector2(40f, 30f),
                new Vector2(-40f, -62f));
        }

        private static RectTransform CreateOverlay(string name, RectTransform parent)
        {
            RectTransform overlay = CreateRect(name, parent);
            Stretch(overlay);
            Image image = overlay.gameObject.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.62f);
            return overlay;
        }

        private static RectTransform CreatePanel(
            string name,
            Transform parent,
            Sprite sprite,
            Color color)
        {
            RectTransform root = CreateRect(name, parent);
            Image image = root.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = color;
            return root;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return gameObject.GetComponent<RectTransform>();
        }

        private static TMP_Text CreateText(
            string name,
            Transform parent,
            TMP_FontAsset font,
            float fontSize,
            TextAlignmentOptions alignment)
        {
            RectTransform root = CreateRect(name, parent);
            TextMeshProUGUI text = root.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSize = fontSize;
            text.enableAutoSizing = true;
            text.fontSizeMin = 10f;
            text.fontSizeMax = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            text.text = string.Empty;
            return text;
        }

        private static Sprite FindSprite(string name)
        {
            return FindSprite(BrushAssetPath, name);
        }

        private static Sprite FindSprite(string assetPath, string name)
        {
            foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(assetPath))
            {
                Sprite sprite = asset as Sprite;
                if (sprite != null && string.Equals(sprite.name, name, StringComparison.Ordinal))
                {
                    return sprite;
                }
            }

            throw new InvalidOperationException($"Brush sprite '{name}' was not found.");
        }

        private static void SetCenteredRect(
            RectTransform rectTransform,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = sizeDelta;
        }

        private static void AssignHudReferences(
            GameHudView hud,
            GameObject controls,
            RectTransform tooltip,
            TMP_Text tooltipText,
            GameObject optionPanel,
            TMP_Text optionHeader,
            CombatPromptView combatPrompt,
            ScrollRect optionScroll,
            GameHudChoiceButton[] optionSlots,
            GameObject contractDetailPanel,
            GameHudContractDetailView contractDetail,
            CardContentCatalogSO cardContentCatalog)
        {
            SerializedObject serialized = new SerializedObject(hud);
            serialized.FindProperty("combatControlsRoot").objectReferenceValue = controls;
            serialized.FindProperty("actionTooltip").objectReferenceValue = tooltip;
            serialized.FindProperty("actionTooltipText").objectReferenceValue = tooltipText;
            serialized.FindProperty("optionPanel").objectReferenceValue = optionPanel;
            serialized.FindProperty("combatHeaderText").objectReferenceValue = optionHeader;
            serialized.FindProperty("combatPromptView").objectReferenceValue = combatPrompt;
            serialized.FindProperty("optionScrollRect").objectReferenceValue = optionScroll;
            AssignArray(serialized.FindProperty("optionSlots"), optionSlots);
            serialized.FindProperty("contractDetailPanel").objectReferenceValue =
                contractDetailPanel;
            serialized.FindProperty("contractDetailView").objectReferenceValue = contractDetail;
            serialized.FindProperty("cardContentCatalog").objectReferenceValue = cardContentCatalog;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignChoiceButtonReferences(
            GameHudChoiceButton choice,
            Button button,
            TMP_Text label)
        {
            SerializedObject serialized = new SerializedObject(choice);
            serialized.FindProperty("button").objectReferenceValue = button;
            serialized.FindProperty("labelText").objectReferenceValue = label;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignContractDetailReferences(
            GameHudContractDetailView detail,
            Image face,
            TMP_Text title,
            TMP_Text abilityLabel,
            TMP_Text ability,
            TMP_Text costLabel,
            TMP_Text cost)
        {
            SerializedObject serialized = new SerializedObject(detail);
            serialized.FindProperty("faceImage").objectReferenceValue = face;
            serialized.FindProperty("titleText").objectReferenceValue = title;
            serialized.FindProperty("abilityLabelText").objectReferenceValue =
                abilityLabel;
            serialized.FindProperty("abilityText").objectReferenceValue = ability;
            serialized.FindProperty("costLabelText").objectReferenceValue = costLabel;
            serialized.FindProperty("costText").objectReferenceValue = cost;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignArray<T>(SerializedProperty property, IReadOnlyList<T> values)
            where T : UnityEngine.Object
        {
            property.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }

        private static void Stretch(RectTransform rectTransform, float inset = 0f)
        {
            SetRect(
                rectTransform,
                Vector2.zero,
                Vector2.one,
                new Vector2(inset, inset),
                new Vector2(-inset, -inset));
        }

        private static void SetRect(
            RectTransform rectTransform,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = offsetMin;
            rectTransform.offsetMax = offsetMax;
        }
    }
}
#endif
