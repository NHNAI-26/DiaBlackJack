#if UNITY_EDITOR
using System.Collections.Generic;
using Border.Audio;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DiaBlackJack.GameScene.Editor
{
    internal sealed class MoodControllerTestWindow : EditorWindow
    {
        private const string MenuPath =
            "Tools/DiaBlackJack/Mood Controller Test";
        private const float DefaultBlendDuration = 1f;
        private const double LightingPollInterval = 1.0;

        private static readonly int WindowGlassGlowColorId =
            Shader.PropertyToID("_GlassGlowColor");

        private MoodController _controller;
        private CharacterView _enemyCharacter;
        private MoodProfileSO _profile;
        private string _profileId;
        private float _duration = DefaultBlendDuration;
        private float _testPulseStrength = 0.8f;
        private bool _pollLighting;
        private bool _writePolledLightingToProfile;
        private double _nextLightingPollTime;
        private bool _hasCapturedLighting;
        private Color _capturedWindowGlassGlowColor;
        private Color _capturedVolumetricLightColor;
        private Color _capturedEnemyLightColor;
        private Color _capturedEnteranceLightColor;
        private Vector2 _scroll;
        private string _lastMessage;
        private MessageType _lastMessageType = MessageType.Info;

        [MenuItem(MenuPath)]
        private static void Open()
        {
            MoodControllerTestWindow window =
                GetWindow<MoodControllerTestWindow>("Mood Test");
            window.RefreshFromSelection();
            window.Show();
        }

        private void OnEnable()
        {
            Selection.selectionChanged -= HandleSelectionChanged;
            Selection.selectionChanged += HandleSelectionChanged;
            EditorApplication.update -= HandleEditorUpdate;
            EditorApplication.update += HandleEditorUpdate;

            if (_controller == null && _profile == null)
            {
                RefreshFromSelection();
            }

            if (_enemyCharacter == null)
            {
                FindSceneCharacter();
            }
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= HandleSelectionChanged;
            EditorApplication.update -= HandleEditorUpdate;
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawTargetSection();
            EditorGUILayout.Space(8f);
            DrawManualProfileSection();
            EditorGUILayout.Space(8f);
            DrawEntranceDoorSection();
            EditorGUILayout.Space(8f);
            DrawEnemyAppearanceSection();
            EditorGUILayout.Space(8f);
            DrawLightingCaptureSection();
            EditorGUILayout.Space(8f);
            DrawAudioReactiveLightningSection();
            EditorGUILayout.Space(8f);
            DrawRegisteredProfilesSection();
            EditorGUILayout.Space(8f);
            DrawRuntimeStatus();
            DrawLastMessage();
            EditorGUILayout.EndScrollView();
        }

        private void DrawTargetSection()
        {
            EditorGUILayout.LabelField("TARGET", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                _controller = EditorGUILayout.ObjectField(
                    "Mood Controller",
                    _controller,
                    typeof(MoodController),
                    true) as MoodController;

                if (GUILayout.Button("Selection", GUILayout.Width(82f)))
                {
                    RefreshFromSelection();
                }

                if (GUILayout.Button("Find", GUILayout.Width(56f)))
                {
                    FindSceneController();
                }
            }

            if (_controller == null)
            {
                EditorGUILayout.HelpBox(
                    "Select a MoodController in the Hierarchy or assign one here.",
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.LabelField(
                    "Scene",
                    _controller.gameObject.scene.name);
            }
        }

        private void DrawEntranceDoorSection()
        {
            EditorGUILayout.LabelField(
                "ENTRANCE DOOR",
                EditorStyles.boldLabel);

            if (_controller == null)
            {
                EditorGUILayout.HelpBox(
                    "Assign a MoodController to test the entrance door.",
                    MessageType.Info);
                return;
            }

            SerializedObject serialized = new SerializedObject(_controller);
            serialized.Update();
            DrawProfileProperty(serialized, "leftDoorBone");
            DrawProfileProperty(serialized, "rightDoorBone");
            DrawProfileProperty(serialized, "doorRotationDuration");
            DrawProfileProperty(serialized, "doorRotationAmount");
            DrawProfileProperty(serialized, "doorAnimationCurve");
            serialized.ApplyModifiedProperties();

            EditorGUILayout.LabelField(
                "Animation",
                "One shared DOTween curve rotation with mirrored direction");

            using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying))
            {
                if (GUILayout.Button("Test Entrance Door"))
                {
                    TestEntranceDoor();
                }
            }

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "Enter Play Mode before running the door animation test.",
                    MessageType.Info);
            }
        }

        private void DrawEnemyAppearanceSection()
        {
            EditorGUILayout.LabelField(
                "ENEMY APPEARANCE",
                EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                _enemyCharacter = EditorGUILayout.ObjectField(
                    "Character View",
                    _enemyCharacter,
                    typeof(CharacterView),
                    true) as CharacterView;

                if (GUILayout.Button("Selection", GUILayout.Width(82f)))
                {
                    RefreshCharacterFromSelection();
                }

                if (GUILayout.Button("Find", GUILayout.Width(56f)))
                {
                    FindSceneCharacter();
                }
            }

            if (_enemyCharacter == null)
            {
                EditorGUILayout.HelpBox(
                    "Assign a CharacterView in the scene to test enemy appearance.",
                    MessageType.Info);
                return;
            }

            SerializedObject serialized = new SerializedObject(_enemyCharacter);
            serialized.Update();
            DrawProfileProperty(serialized, "appearanceEnterDuration");
            DrawProfileProperty(serialized, "appearanceExitDuration");
            DrawProfileProperty(serialized, "appearanceRotationOffset");
            DrawProfileProperty(serialized, "appearanceEnterEase");
            DrawProfileProperty(serialized, "appearanceExitEase");
            DrawProfileProperty(serialized, "merchantExitEase");
            serialized.ApplyModifiedProperties();

            EditorGUILayout.LabelField(
                "Animation",
                "Entrance: 180° to 0° / Exit: 0° to 180°");

            using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Test Enemy Entrance"))
                    {
                        TestEnemyEntrance();
                    }

                    if (GUILayout.Button("Test Enemy Exit"))
                    {
                        TestEnemyExit();
                    }
                }

                if (GUILayout.Button("Test Merchant Exit"))
                {
                    TestMerchantExit();
                }
            }

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "Enter Play Mode before running the enemy appearance test.",
                    MessageType.Info);
            }
        }

        private void DrawManualProfileSection()
        {
            EditorGUILayout.LabelField("MANUAL PROFILE", EditorStyles.boldLabel);
            _profile = EditorGUILayout.ObjectField(
                "Mood Profile",
                _profile,
                typeof(MoodProfileSO),
                false) as MoodProfileSO;
            DrawProfileSummary(_profile);

            _duration = EditorGUILayout.FloatField(
                "Blend Duration",
                Mathf.Max(0f, _duration));

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(_controller == null ||
                                                   _profile == null))
                {
                    if (GUILayout.Button("Apply Immediate"))
                    {
                        ApplyProfile(_profile, immediate: true);
                    }

                    using (new EditorGUI.DisabledScope(
                        !EditorApplication.isPlaying))
                    {
                        if (GUILayout.Button("Blend"))
                        {
                            ApplyProfile(_profile, immediate: false);
                        }
                    }
                }
            }

            EditorGUILayout.Space(4f);
            _profileId = EditorGUILayout.TextField("Registered Id", _profileId);
            using (new EditorGUI.DisabledScope(_controller == null ||
                                               string.IsNullOrWhiteSpace(_profileId)))
            {
                if (GUILayout.Button("Apply Registered Id"))
                {
                    ApplyRegisteredId();
                }
            }
        }

        private void DrawAudioReactiveLightningSection()
        {
            EditorGUILayout.LabelField(
                "AUDIO REACTIVE LIGHTNING",
                EditorStyles.boldLabel);

            if (_profile == null)
            {
                EditorGUILayout.HelpBox(
                    "Assign a Mood Profile to edit lightning options.",
                    MessageType.Info);
            }
            else
            {
                SerializedObject serializedProfile =
                    new SerializedObject(_profile);
                serializedProfile.Update();
                DrawProfileProperty(
                    serializedProfile,
                    "enableAudioReactiveLightning");
                DrawProfileProperty(serializedProfile, "lightningSfxIds");
                DrawProfileProperty(serializedProfile, "lightningSfxPlayChance");
                DrawProfileProperty(serializedProfile, "lightningSfxInterval");
                DrawProfileProperty(serializedProfile, "lightningSensitivity");
                DrawProfileProperty(serializedProfile, "lightningThreshold");
                DrawProfileProperty(serializedProfile, "lightningMaxBoost");
                DrawProfileProperty(serializedProfile, "lightningAttackSpeed");
                DrawProfileProperty(serializedProfile, "lightningReleaseSpeed");
                serializedProfile.ApplyModifiedProperties();
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(
                "Controller Active",
                _controller != null &&
                _controller.IsAudioReactiveLightningActive
                    ? "On"
                    : "Off");
            EditorGUILayout.LabelField(
                "Controller Boost",
                _controller == null
                    ? "<none>"
                    : _controller.CurrentAudioReactiveLightningBoost
                        .ToString("0.000"));
            EditorGUILayout.LabelField(
                "Lightning SFX",
                _controller == null ||
                string.IsNullOrWhiteSpace(
                    _controller.CurrentAudioReactiveLightningSfxId)
                    ? "<none>"
                    : _controller.CurrentAudioReactiveLightningSfxId);
            EditorGUILayout.LabelField(
                "Lightning SFX RMS",
                _controller == null
                    ? "0.000"
                    : _controller.CurrentAudioReactiveLightningRms
                        .ToString("0.000"));

            _testPulseStrength = EditorGUILayout.FloatField(
                "Test Pulse Strength",
                Mathf.Max(0f, _testPulseStrength));
            using (new EditorGUI.DisabledScope(_controller == null ||
                                               !_controller.IsAudioReactiveLightningActive))
            {
                if (GUILayout.Button("Force Lightning Pulse"))
                {
                    bool triggered = _controller.TriggerAudioReactiveLightningPulse(
                        _testPulseStrength);
                    SetMessage(
                        triggered
                            ? "Triggered audio reactive lightning pulse."
                            : "Audio reactive lightning is not active.",
                        triggered ? MessageType.Info : MessageType.Warning);
                    RepaintScene(_controller);
                }
            }

            using (new EditorGUI.DisabledScope(
                       _controller == null ||
                       !_controller.IsAudioReactiveLightningActive ||
                       !EditorApplication.isPlaying))
            {
                if (GUILayout.Button("Play Random Lightning SFX"))
                {
                    bool triggered =
                        _controller.TriggerAudioReactiveLightningSfx();
                    SetMessage(
                        triggered
                            ? "Played random lightning SFX."
                            : "Lightning SFX could not be played.",
                        triggered ? MessageType.Info : MessageType.Warning);
                }
            }
        }

        private void DrawLightingCaptureSection()
        {
            EditorGUILayout.LabelField("LIGHTING CAPTURE", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                _pollLighting = EditorGUILayout.Toggle(
                    "Poll Every 1s",
                    _pollLighting);

                using (new EditorGUI.DisabledScope(_controller == null))
                {
                    if (GUILayout.Button("Capture Now", GUILayout.Width(112f)))
                    {
                        CaptureLighting(showMessage: true);
                    }
                }
            }

            using (new EditorGUI.DisabledScope(_profile == null))
            {
                _writePolledLightingToProfile = EditorGUILayout.Toggle(
                    "Write To Profile",
                    _writePolledLightingToProfile);
            }

            if (_writePolledLightingToProfile && _profile == null)
            {
                EditorGUILayout.HelpBox(
                    "Assign a Mood Profile before writing captured lighting.",
                    MessageType.Warning);
            }

            if (!_hasCapturedLighting)
            {
                EditorGUILayout.HelpBox(
                    "Captured lighting values will appear here.",
                    MessageType.Info);
                return;
            }

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ColorField(
                    new GUIContent("Window Glass Glow"),
                    _capturedWindowGlassGlowColor,
                    showEyedropper: false,
                    showAlpha: true,
                    hdr: true);
                EditorGUILayout.ColorField(
                    new GUIContent("Volumetric Light"),
                    _capturedVolumetricLightColor,
                    showEyedropper: false,
                    showAlpha: true,
                    hdr: true);
                EditorGUILayout.ColorField(
                    new GUIContent("Enemy Light"),
                    _capturedEnemyLightColor,
                    showEyedropper: false,
                    showAlpha: true,
                    hdr: true);
                EditorGUILayout.ColorField(
                    new GUIContent("Enterance Light"),
                    _capturedEnteranceLightColor,
                    showEyedropper: false,
                    showAlpha: true,
                    hdr: true);
            }
        }

        private static void DrawProfileProperty(
            SerializedObject serialized,
            string propertyName)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                EditorGUILayout.PropertyField(property);
            }
        }

        private void DrawRegisteredProfilesSection()
        {
            EditorGUILayout.LabelField(
                "REGISTERED PROFILES",
                EditorStyles.boldLabel);

            if (_controller == null)
            {
                return;
            }

            SerializedObject serialized = new SerializedObject(_controller);
            SerializedProperty profiles =
                serialized.FindProperty("moodProfiles");
            if (profiles == null || !profiles.isArray ||
                profiles.arraySize == 0)
            {
                EditorGUILayout.HelpBox(
                    "This MoodController has no registered profiles.",
                    MessageType.Warning);
                return;
            }

            for (int i = 0; i < profiles.arraySize; i++)
            {
                MoodProfileSO profile = profiles
                    .GetArrayElementAtIndex(i)
                    .objectReferenceValue as MoodProfileSO;
                DrawRegisteredProfile(i, profile);
            }
        }

        private void DrawRegisteredProfile(int index, MoodProfileSO profile)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                string title = profile == null
                    ? $"#{index + 1} <Missing>"
                    : $"#{index + 1} {profile.Id}";
                EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
                DrawProfileSummary(profile);

                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(profile == null))
                    {
                        if (GUILayout.Button("Use"))
                        {
                            _profile = profile;
                            _profileId = profile.Id;
                            Repaint();
                        }

                        if (GUILayout.Button("Apply"))
                        {
                            ApplyProfile(profile, immediate: true);
                        }

                        using (new EditorGUI.DisabledScope(
                            !EditorApplication.isPlaying))
                        {
                            if (GUILayout.Button("Blend"))
                            {
                                ApplyProfile(profile, immediate: false);
                            }
                        }
                    }
                }
            }
        }

        private void DrawRuntimeStatus()
        {
            EditorGUILayout.LabelField("RUNTIME", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "Play Mode",
                EditorApplication.isPlaying ? "On" : "Off");
            EditorGUILayout.LabelField(
                "Sound Manager",
                SoundManager.Current == null ? "Missing" : "Ready");

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "Immediate color preview works in Edit Mode. BGM, " +
                    "lightning SFX, and blend timing should be tested in " +
                    "Play Mode.",
                    MessageType.Info);
            }
        }

        private void DrawLastMessage()
        {
            if (!string.IsNullOrEmpty(_lastMessage))
            {
                EditorGUILayout.HelpBox(_lastMessage, _lastMessageType);
            }
        }

        private static void DrawProfileSummary(MoodProfileSO profile)
        {
            if (profile == null)
            {
                return;
            }

            EditorGUILayout.LabelField("Id", profile.Id);
            EditorGUILayout.LabelField(
                "BGM Candidates",
                FormatBgmIds(profile));
            EditorGUILayout.LabelField(
                "Lightning SFX Candidates",
                FormatLightningSfxIds(profile));
        }

        private void ApplyProfile(MoodProfileSO profile, bool immediate)
        {
            if (_controller == null)
            {
                SetMessage("MoodController is missing.", MessageType.Error);
                return;
            }

            if (profile == null)
            {
                SetMessage("MoodProfile is missing.", MessageType.Error);
                return;
            }

            if (!profile.HasValidId)
            {
                SetMessage(
                    "MoodProfile requires a non-empty id.",
                    MessageType.Error);
                return;
            }

            if (!immediate && !EditorApplication.isPlaying)
            {
                SetMessage(
                    "Blend preview is available in Play Mode.",
                    MessageType.Warning);
                return;
            }

            if (immediate)
            {
                _controller.SetMoodImmediate(profile);
            }
            else
            {
                _controller.BlendToMood(
                    profile,
                    ResolveBlendPreviewDuration());
            }

            RepaintScene(_controller);
            SetMessage(
                BuildAppliedMessage(profile, immediate),
                MessageType.Info);
        }

        private void ApplyRegisteredId()
        {
            if (_controller == null)
            {
                SetMessage("MoodController is missing.", MessageType.Error);
                return;
            }

            float duration = EditorApplication.isPlaying
                ? ResolveBlendPreviewDuration()
                : 0f;
            bool applied = _controller.TryBlendToMood(_profileId, duration);
            RepaintScene(_controller);
            SetMessage(
                applied
                    ? $"Applied registered mood '{_profileId}'."
                    : $"Registered mood '{_profileId}' was not found.",
                applied ? MessageType.Info : MessageType.Warning);
        }

        private void TestEntranceDoor()
        {
            if (_controller == null)
            {
                SetMessage("MoodController is missing.", MessageType.Error);
                return;
            }

            bool started = _controller.TryPlayEntranceDoorAnimation();
            RepaintScene(_controller);
            SetMessage(
                started
                    ? "Started the entrance door animation."
                    : "Door animation could not start. Assign both door bones and set a duration above zero.",
                started ? MessageType.Info : MessageType.Warning);
        }

        private void TestEnemyEntrance()
        {
            if (_enemyCharacter == null)
            {
                SetMessage("CharacterView is missing.", MessageType.Error);
                return;
            }

            EnsureCharacterRootIsActive(_enemyCharacter);
            _enemyCharacter.PlayEntranceAnimation();
            RepaintScene(_controller);
            SetMessage(
                "Started enemy entrance animation (180° to 0°).",
                MessageType.Info);
        }

        private void TestEnemyExit()
        {
            if (_enemyCharacter == null)
            {
                SetMessage("CharacterView is missing.", MessageType.Error);
                return;
            }

            EnsureCharacterRootIsActive(_enemyCharacter);
            _enemyCharacter.PlayExitAnimation(null);
            RepaintScene(_controller);
            SetMessage(
                "Started enemy exit animation (0° to 180°).",
                MessageType.Info);
        }

        private void TestMerchantExit()
        {
            if (_enemyCharacter == null)
            {
                SetMessage("CharacterView is missing.", MessageType.Error);
                return;
            }

            EnsureCharacterRootIsActive(_enemyCharacter);
            _enemyCharacter.EnterMerchant();
            _enemyCharacter.PlayExitAnimation(_enemyCharacter.ExitMerchant);
            RepaintScene(_controller);
            SetMessage(
                "Started merchant exit animation with the merchant Ease.",
                MessageType.Info);
        }

        private void CaptureLighting(bool showMessage)
        {
            if (_controller == null)
            {
                if (showMessage)
                {
                    SetMessage(
                        "MoodController is missing.",
                        MessageType.Error);
                }

                return;
            }

            SerializedObject serialized = new SerializedObject(_controller);
            _capturedWindowGlassGlowColor = ResolveWindowGlassGlowColor(
                serialized);
            _capturedVolumetricLightColor = ResolveLightColor(
                serialized,
                "volumetricLight");
            _capturedEnemyLightColor = ResolveLightColor(
                serialized,
                "enemyLight");
            _capturedEnteranceLightColor = ResolveLightColor(
                serialized,
                "enteranceLight");
            _hasCapturedLighting = true;

            if (_writePolledLightingToProfile && _profile != null)
            {
                WriteCapturedLightingToProfile();
            }

            if (showMessage)
            {
                SetMessage(
                    _writePolledLightingToProfile && _profile != null
                        ? $"Captured lighting and updated '{_profile.name}'."
                        : "Captured current lighting.",
                    MessageType.Info);
            }
            else
            {
                Repaint();
            }
        }

        private void WriteCapturedLightingToProfile()
        {
            Undo.RecordObject(_profile, "Capture Mood Lighting");
            SerializedObject serializedProfile = new SerializedObject(_profile);
            serializedProfile.FindProperty("windowGlassGlowColor").colorValue =
                _capturedWindowGlassGlowColor;
            serializedProfile.FindProperty("volumetricLightColor").colorValue =
                _capturedVolumetricLightColor;
            serializedProfile.FindProperty("enemyLightColor").colorValue =
                _capturedEnemyLightColor;
            serializedProfile.FindProperty("enteranceLightColor").colorValue =
                _capturedEnteranceLightColor;
            serializedProfile.ApplyModifiedProperties();
            EditorUtility.SetDirty(_profile);
        }

        private void RefreshFromSelection()
        {
            MoodProfileSO selectedProfile =
                Selection.activeObject as MoodProfileSO;
            if (selectedProfile != null)
            {
                _profile = selectedProfile;
                _profileId = selectedProfile.Id;
                Repaint();
                return;
            }

            GameObject selected = Selection.activeGameObject;
            if (selected != null)
            {
                CharacterView character = selected.GetComponentInParent<CharacterView>();
                character ??= selected.GetComponentInChildren<CharacterView>(true);
                if (character != null)
                {
                    _enemyCharacter = character;
                }

                MoodController controller =
                    selected.GetComponentInParent<MoodController>();
                if (controller != null)
                {
                    _controller = controller;
                    Repaint();
                    return;
                }

                if (character != null)
                {
                    Repaint();
                    return;
                }
            }

            FindSceneController();
        }

        private void RefreshCharacterFromSelection()
        {
            GameObject selected = Selection.activeGameObject;
            CharacterView character = selected == null
                ? null
                : selected.GetComponentInParent<CharacterView>();
            character ??= selected == null
                ? null
                : selected.GetComponentInChildren<CharacterView>(true);

            if (character == null)
            {
                SetMessage(
                    "No CharacterView was found in the current selection.",
                    MessageType.Warning);
                return;
            }

            _enemyCharacter = character;
            SetMessage(
                $"Using CharacterView on '{character.name}'.",
                MessageType.Info);
            Repaint();
        }

        private void FindSceneCharacter()
        {
            CharacterView[] characters =
                Resources.FindObjectsOfTypeAll<CharacterView>();
            foreach (CharacterView character in characters)
            {
                if (character == null ||
                    EditorUtility.IsPersistent(character) ||
                    !character.gameObject.scene.IsValid())
                {
                    continue;
                }

                _enemyCharacter = character;
                SetMessage(
                    $"Using CharacterView on '{character.name}'.",
                    MessageType.Info);
                Repaint();
                return;
            }

            _enemyCharacter = null;
            SetMessage(
                "No scene CharacterView was found.",
                MessageType.Warning);
            Repaint();
        }

        private static void EnsureCharacterRootIsActive(CharacterView character)
        {
            Transform current = character.transform;
            while (current != null)
            {
                if (current.name == "Characters")
                {
                    current.gameObject.SetActive(true);
                    character.gameObject.SetActive(true);
                    return;
                }

                current = current.parent;
            }

            character.gameObject.SetActive(true);
        }

        private void FindSceneController()
        {
            MoodController[] controllers =
                Resources.FindObjectsOfTypeAll<MoodController>();
            foreach (MoodController controller in controllers)
            {
                if (controller == null ||
                    EditorUtility.IsPersistent(controller) ||
                    !controller.gameObject.scene.IsValid())
                {
                    continue;
                }

                _controller = controller;
                SetMessage(
                    $"Using MoodController on '{controller.name}'.",
                    MessageType.Info);
                Repaint();
                return;
            }

            _controller = null;
            SetMessage(
                "No scene MoodController was found.",
                MessageType.Warning);
            Repaint();
        }

        private void HandleSelectionChanged()
        {
            Repaint();
        }

        private void HandleEditorUpdate()
        {
            if (!_pollLighting)
            {
                return;
            }

            double time = EditorApplication.timeSinceStartup;
            if (time < _nextLightingPollTime)
            {
                return;
            }

            _nextLightingPollTime = time + LightingPollInterval;
            CaptureLighting(showMessage: false);
        }

        private void SetMessage(string message, MessageType type)
        {
            _lastMessage = message;
            _lastMessageType = type;
            Repaint();
        }

        private static void RepaintScene(MoodController controller)
        {
            if (controller != null &&
                controller.gameObject.scene.IsValid() &&
                !EditorApplication.isPlaying)
            {
                EditorSceneManager.MarkSceneDirty(
                    controller.gameObject.scene);
            }

            SceneView.RepaintAll();
        }

        private static string BuildAppliedMessage(
            MoodProfileSO profile,
            bool immediate)
        {
            return $"{(immediate ? "Applied" : "Blending")} mood " +
                   $"'{profile.Id}'.";
        }

        private float ResolveBlendPreviewDuration()
        {
            return _duration <= 0f ? DefaultBlendDuration : _duration;
        }

        private static Color ResolveWindowGlassGlowColor(
            SerializedObject serialized)
        {
            SerializedProperty renderers =
                serialized.FindProperty("windowGlassRenderers");
            if (renderers == null || !renderers.isArray)
            {
                return Color.white;
            }

            MaterialPropertyBlock properties = new MaterialPropertyBlock();
            for (int i = 0; i < renderers.arraySize; i++)
            {
                Renderer renderer = renderers
                    .GetArrayElementAtIndex(i)
                    .objectReferenceValue as Renderer;
                if (renderer == null)
                {
                    continue;
                }

                renderer.GetPropertyBlock(properties);
                if (!properties.isEmpty)
                {
                    return properties.GetColor(WindowGlassGlowColorId);
                }

                Material material = renderer.sharedMaterial;
                if (material != null &&
                    material.HasProperty(WindowGlassGlowColorId))
                {
                    return material.GetColor(WindowGlassGlowColorId);
                }
            }

            return Color.white;
        }

        private static Color ResolveLightColor(
            SerializedObject serialized,
            string propertyName)
        {
            Light light = serialized
                .FindProperty(propertyName)
                ?.objectReferenceValue as Light;
            return light == null ? Color.white : light.color;
        }

        private static string FormatBgmIds(MoodProfileSO profile)
        {
            if (profile.BgmIds == null || profile.BgmIds.Count == 0)
            {
                return "<none>";
            }

            List<string> ids = new List<string>();
            foreach (string bgmId in profile.BgmIds)
            {
                if (!string.IsNullOrWhiteSpace(bgmId))
                {
                    ids.Add(bgmId);
                }
            }

            return ids.Count == 0 ? "<none>" : string.Join(", ", ids);
        }

        private static string FormatLightningSfxIds(MoodProfileSO profile)
        {
            if (profile.LightningSfxIds == null ||
                profile.LightningSfxIds.Count == 0)
            {
                return "<none>";
            }

            List<string> ids = new List<string>();
            foreach (string sfxId in profile.LightningSfxIds)
            {
                if (!string.IsNullOrWhiteSpace(sfxId))
                {
                    ids.Add(sfxId);
                }
            }

            return ids.Count == 0 ? "<none>" : string.Join(", ", ids);
        }
    }
}
#endif
