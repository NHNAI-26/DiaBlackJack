#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using DiaBlackJack.Content;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DiaBlackJack.GameScene.Editor
{
    /// <summary>Creates the authored combat HUD hierarchy once, without runtime GameObject creation.</summary>
    internal static class GameHudCombatPrefabBuilder
    {
        private const string HudPrefabPath = "Assets/03. Prefabs/UI/HUD.prefab";
        private const string DefaultButtonPrefabPath =
            "Assets/03. Prefabs/UI/DefaultButton.prefab";
        private const string BrushAssetPath = "Assets/05. Arts/UI/Brush_UI.psd";
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

                RectTransform tooltip = CreatePanel(
                    "ActionTooltip", controls, panelBrush, new Color(0.08f, 0.06f, 0.07f, 0.96f));
                tooltip.anchorMin = new Vector2(0.5f, 0f);
                tooltip.anchorMax = new Vector2(0.5f, 0f);
                tooltip.sizeDelta = new Vector2(500f, 112f);
                TMP_Text tooltipText = CreateText(
                    "Text", tooltip, font, 18f, TextAlignmentOptions.Center);
                Stretch(tooltipText.rectTransform, 16f);
                tooltip.gameObject.SetActive(false);

                RectTransform automaticResult = CreatePanel(
                    "AutomaticCardResult", controls, panelBrush, new Color(0.07f, 0.05f, 0.06f, 0.94f));
                automaticResult.anchorMin = new Vector2(0.5f, 1f);
                automaticResult.anchorMax = new Vector2(0.5f, 1f);
                automaticResult.pivot = new Vector2(0.5f, 1f);
                automaticResult.anchoredPosition = new Vector2(0f, -98f);
                automaticResult.sizeDelta = new Vector2(660f, 100f);
                TMP_Text automaticResultText = CreateText(
                    "Text", automaticResult, font, 16f, TextAlignmentOptions.Center);
                Stretch(automaticResultText.rectTransform, 14f);

                RectTransform optionPanel = CreateOverlay("OptionPanel", controls);
                TMP_Text optionPrompt = CreateText(
                    "Prompt", optionPanel, font, 22f, TextAlignmentOptions.Center);
                optionPrompt.rectTransform.anchorMin = new Vector2(0.5f, 1f);
                optionPrompt.rectTransform.anchorMax = new Vector2(0.5f, 1f);
                optionPrompt.rectTransform.pivot = new Vector2(0.5f, 1f);
                optionPrompt.rectTransform.anchoredPosition = new Vector2(0f, -86f);
                optionPrompt.rectTransform.sizeDelta = new Vector2(860f, 74f);

                ScrollRect optionScroll = CreateOptionScroll(
                    optionPanel,
                    font,
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
                    optionScroll,
                    optionSlots,
                    contractDetailPanel.gameObject,
                    contractDetail,
                    automaticResult.gameObject,
                    automaticResultText,
                    contentCatalog);
                PrefabUtility.SaveAsPrefabAsset(hudRoot, HudPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(hudRoot);
            }
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
                typeof(Button));
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
            TMP_FontAsset font,
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

            slots = new GameHudChoiceButton[OptionSlotCount];
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i] = CreateOptionSlot($"OptionSlot_{i + 1:000}", content, font);
            }

            scroll.viewport = viewport;
            scroll.content = content;
            return scroll;
        }

        private static GameHudChoiceButton CreateOptionSlot(
            string name,
            RectTransform parent,
            TMP_FontAsset font)
        {
            RectTransform root = CreateRect(name, parent);
            Image image = root.gameObject.AddComponent<Image>();
            image.color = new Color(0.28f, 0.12f, 0.14f, 1f);
            LayoutElement layout = root.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 78f;
            Button button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            TMP_Text label = CreateText("Label", root, font, 21f, TextAlignmentOptions.Center);
            Stretch(label.rectTransform, 12f);
            GameHudChoiceButton choice = root.gameObject.AddComponent<GameHudChoiceButton>();
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
            foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(BrushAssetPath))
            {
                Sprite sprite = asset as Sprite;
                if (sprite != null && string.Equals(sprite.name, name, StringComparison.Ordinal))
                {
                    return sprite;
                }
            }

            throw new InvalidOperationException($"Brush sprite '{name}' was not found.");
        }

        private static void AssignHudReferences(
            GameHudView hud,
            GameObject controls,
            RectTransform tooltip,
            TMP_Text tooltipText,
            GameObject optionPanel,
            TMP_Text optionPrompt,
            ScrollRect optionScroll,
            GameHudChoiceButton[] optionSlots,
            GameObject contractDetailPanel,
            GameHudContractDetailView contractDetail,
            GameObject automaticResult,
            TMP_Text automaticResultText,
            CardContentCatalogSO cardContentCatalog)
        {
            SerializedObject serialized = new SerializedObject(hud);
            serialized.FindProperty("combatControlsRoot").objectReferenceValue = controls;
            serialized.FindProperty("actionTooltip").objectReferenceValue = tooltip;
            serialized.FindProperty("actionTooltipText").objectReferenceValue = tooltipText;
            serialized.FindProperty("optionPanel").objectReferenceValue = optionPanel;
            serialized.FindProperty("combatPromptText").objectReferenceValue = optionPrompt;
            serialized.FindProperty("optionScrollRect").objectReferenceValue = optionScroll;
            AssignArray(serialized.FindProperty("optionSlots"), optionSlots);
            serialized.FindProperty("contractDetailPanel").objectReferenceValue =
                contractDetailPanel;
            serialized.FindProperty("contractDetailView").objectReferenceValue = contractDetail;
            serialized.FindProperty("automaticCardResultPanel").objectReferenceValue = automaticResult;
            serialized.FindProperty("automaticCardResultText").objectReferenceValue = automaticResultText;
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
