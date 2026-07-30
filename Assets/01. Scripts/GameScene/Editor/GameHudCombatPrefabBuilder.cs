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
        private const string BrushAssetPath = "Assets/05. Arts/UI/Brush_UI.psd";
        private const string CardContentCatalogPath =
            "Assets/02. ScriptableObjects/Cards/CardContentCatalog.asset";
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

                Sprite hitBrush = FindSprite("Brush_UI_4");
                Sprite standBrush = FindSprite("Brush_UI_5");
                Sprite changeBrush = FindSprite("Brush_UI_9");
                Sprite contractBrush = FindSprite("Brush_UI_10");
                Sprite panelBrush = FindSprite("Brush_UI_8");

                RectTransform controls = CreateRect("CombatControls", hudRoot.transform);
                Stretch(controls);

                RectTransform actionRow = CreateRect("ActionRow", controls);
                actionRow.anchorMin = new Vector2(0.5f, 0f);
                actionRow.anchorMax = new Vector2(0.5f, 0f);
                actionRow.pivot = new Vector2(0.5f, 0f);
                actionRow.anchoredPosition = new Vector2(0f, 36f);
                actionRow.sizeDelta = new Vector2(720f, 94f);
                HorizontalLayoutGroup actionLayout = actionRow.gameObject.AddComponent<HorizontalLayoutGroup>();
                actionLayout.spacing = 12f;
                actionLayout.childAlignment = TextAnchor.MiddleCenter;
                actionLayout.childControlWidth = false;
                actionLayout.childControlHeight = false;
                actionLayout.childForceExpandWidth = false;
                actionLayout.childForceExpandHeight = false;

                GameHudActionButton hit = CreateActionButton(
                    "Hit", actionRow, hitBrush, "HIT", font);
                GameHudActionButton stand = CreateActionButton(
                    "Stand", actionRow, standBrush, "STAND", font);
                GameHudActionButton change = CreateActionButton(
                    "Change", actionRow, changeBrush, "CHANGE", font);
                GameHudActionButton contract = CreateActionButton(
                    "Contract", actionRow, contractBrush, "CONTRACT", font);

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

                RectTransform candidatePanel = CreateOverlay("ContractCandidatePanel", controls);
                TMP_Text candidatePrompt = CreateText(
                    "Prompt", candidatePanel, font, 21f, TextAlignmentOptions.Center);
                candidatePrompt.rectTransform.anchorMin = new Vector2(0.5f, 1f);
                candidatePrompt.rectTransform.anchorMax = new Vector2(0.5f, 1f);
                candidatePrompt.rectTransform.pivot = new Vector2(0.5f, 1f);
                candidatePrompt.rectTransform.anchoredPosition = new Vector2(0f, -86f);
                candidatePrompt.rectTransform.sizeDelta = new Vector2(860f, 70f);
                GameHudChoiceButton[] candidateSlots = CreateContractCandidates(
                    candidatePanel,
                    font,
                    panelBrush);

                CardContentCatalogSO contentCatalog =
                    AssetDatabase.LoadAssetAtPath<CardContentCatalogSO>(CardContentCatalogPath);
                if (contentCatalog == null)
                {
                    throw new InvalidOperationException("Card content catalog asset was not found.");
                }

                AssignHudReferences(
                    hud,
                    controls.gameObject,
                    actionRow.gameObject,
                    hit,
                    stand,
                    change,
                    contract,
                    tooltip,
                    tooltipText,
                    optionPanel.gameObject,
                    optionPrompt,
                    optionScroll,
                    optionSlots,
                    candidatePanel.gameObject,
                    candidatePrompt,
                    candidateSlots,
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

        private static GameHudActionButton CreateActionButton(
            string name,
            RectTransform parent,
            Sprite sprite,
            string label,
            TMP_FontAsset font)
        {
            RectTransform root = CreateRect(name, parent);
            root.sizeDelta = new Vector2(168f, 86f);
            Image image = root.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            Button button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            TMP_Text labelText = CreateText(
                "Label", root, font, 22f, TextAlignmentOptions.Center);
            Stretch(labelText.rectTransform, 10f);
            labelText.text = label;
            GameHudActionButton action = root.gameObject.AddComponent<GameHudActionButton>();
            AssignActionButtonReferences(action, button, labelText);
            return action;
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
            AssignChoiceButtonReferences(choice, button, label, null, null, null, null);
            return choice;
        }

        private static GameHudChoiceButton[] CreateContractCandidates(
            RectTransform parent,
            TMP_FontAsset font,
            Sprite panelBrush)
        {
            RectTransform row = CreateRect("CandidateRow", parent);
            row.anchorMin = new Vector2(0.14f, 0.18f);
            row.anchorMax = new Vector2(0.86f, 0.78f);
            row.pivot = new Vector2(0.5f, 0.5f);
            row.offsetMin = Vector2.zero;
            row.offsetMax = Vector2.zero;

            GameHudChoiceButton detailSlot = CreateContractCandidate(
                "CandidateSlot_1",
                row,
                font,
                panelBrush);
            Stretch((RectTransform)detailSlot.transform);
            GameHudChoiceButton unusedSlot = CreateContractCandidate(
                "CandidateSlot_2",
                row,
                font,
                panelBrush);
            Stretch((RectTransform)unusedSlot.transform);
            unusedSlot.gameObject.SetActive(false);

            return new[]
            {
                detailSlot,
                unusedSlot
            };
        }

        private static GameHudChoiceButton CreateContractCandidate(
            string name,
            RectTransform parent,
            TMP_FontAsset font,
            Sprite panelBrush)
        {
            RectTransform root = CreatePanel(
                name, parent, panelBrush, new Color(0.15f, 0.1f, 0.12f, 0.99f));
            root.sizeDelta = new Vector2(440f, 390f);
            Button button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = root.GetComponent<Image>();

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

            TMP_Text title = CreateText(
                "Title",
                root,
                font,
                30f,
                TextAlignmentOptions.Center);
            title.fontSizeMin = 20f;
            title.fontStyle = FontStyles.Bold;
            SetRect(
                title.rectTransform,
                new Vector2(0.0272f, 0.8381f),
                new Vector2(0.2228f, 0.9524f),
                Vector2.zero,
                Vector2.zero);
            TMP_Text ability = CreateText(
                "Ability",
                root,
                font,
                24f,
                TextAlignmentOptions.TopLeft);
            ability.fontSizeMin = 20f;
            SetRect(
                ability.rectTransform,
                new Vector2(0.2391f, 0.4881f),
                new Vector2(0.9728f, 0.8929f),
                Vector2.zero,
                Vector2.zero);
            TMP_Text cost = CreateText(
                "Cost",
                root,
                font,
                24f,
                TextAlignmentOptions.TopLeft);
            cost.fontSizeMin = 20f;
            SetRect(
                cost.rectTransform,
                new Vector2(0.2391f, 0.0714f),
                new Vector2(0.9728f, 0.4286f),
                Vector2.zero,
                Vector2.zero);
            TMP_Text label = CreateText("Label", root, font, 17f, TextAlignmentOptions.Center);
            SetRect(label.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(172f, 16f), new Vector2(-18f, 58f));

            GameHudChoiceButton choice = root.gameObject.AddComponent<GameHudChoiceButton>();
            AssignChoiceButtonReferences(choice, button, label, faceImage, title, ability, cost);
            return choice;
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
            GameObject actionRow,
            GameHudActionButton hit,
            GameHudActionButton stand,
            GameHudActionButton change,
            GameHudActionButton contract,
            RectTransform tooltip,
            TMP_Text tooltipText,
            GameObject optionPanel,
            TMP_Text optionPrompt,
            ScrollRect optionScroll,
            GameHudChoiceButton[] optionSlots,
            GameObject candidatePanel,
            TMP_Text candidatePrompt,
            GameHudChoiceButton[] candidateSlots,
            GameObject automaticResult,
            TMP_Text automaticResultText,
            CardContentCatalogSO cardContentCatalog)
        {
            SerializedObject serialized = new SerializedObject(hud);
            serialized.FindProperty("combatControlsRoot").objectReferenceValue = controls;
            serialized.FindProperty("actionRow").objectReferenceValue = actionRow;
            serialized.FindProperty("hitButton").objectReferenceValue = hit;
            serialized.FindProperty("standButton").objectReferenceValue = stand;
            serialized.FindProperty("changeButton").objectReferenceValue = change;
            serialized.FindProperty("contractButton").objectReferenceValue = contract;
            serialized.FindProperty("actionTooltip").objectReferenceValue = tooltip;
            serialized.FindProperty("actionTooltipText").objectReferenceValue = tooltipText;
            serialized.FindProperty("optionPanel").objectReferenceValue = optionPanel;
            serialized.FindProperty("combatPromptText").objectReferenceValue = optionPrompt;
            serialized.FindProperty("optionScrollRect").objectReferenceValue = optionScroll;
            AssignArray(serialized.FindProperty("optionSlots"), optionSlots);
            serialized.FindProperty("contractCandidatePanel").objectReferenceValue = candidatePanel;
            serialized.FindProperty("contractCandidatePromptText").objectReferenceValue = candidatePrompt;
            AssignArray(serialized.FindProperty("contractCandidateSlots"), candidateSlots);
            serialized.FindProperty("automaticCardResultPanel").objectReferenceValue = automaticResult;
            serialized.FindProperty("automaticCardResultText").objectReferenceValue = automaticResultText;
            serialized.FindProperty("cardContentCatalog").objectReferenceValue = cardContentCatalog;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignActionButtonReferences(
            GameHudActionButton action,
            Button button,
            TMP_Text label)
        {
            SerializedObject serialized = new SerializedObject(action);
            serialized.FindProperty("button").objectReferenceValue = button;
            serialized.FindProperty("labelText").objectReferenceValue = label;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignChoiceButtonReferences(
            GameHudChoiceButton choice,
            Button button,
            TMP_Text label,
            Image face,
            TMP_Text title,
            TMP_Text ability,
            TMP_Text cost)
        {
            SerializedObject serialized = new SerializedObject(choice);
            serialized.FindProperty("button").objectReferenceValue = button;
            serialized.FindProperty("labelText").objectReferenceValue = label;
            serialized.FindProperty("faceImage").objectReferenceValue = face;
            serialized.FindProperty("titleText").objectReferenceValue = title;
            serialized.FindProperty("abilityText").objectReferenceValue = ability;
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
