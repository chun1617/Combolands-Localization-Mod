using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DropdownGroup = UI.SettingsMenu.DropdownGroup;
using GameSettingsMenu = Shared.UI.SettingsMenu;
using SettingsGroup = UI.SettingsMenu.SettingsGroup;
using SliderGroup = UI.SettingsMenu.SliderGroup;

namespace Combolands.Localization
{
    internal static class SettingsMenuPatch
    {
        private const string SettingsPanelPath = "SettingsMenuPanel";
        private const string VerticalLayoutName = "VerticalLayout";
        private const string ScrollHostName = "ModScrollHost";
        private const string ScrollViewportName = "Viewport";
        private const string ScrollbarName = "ModVerticalScrollbar";
        private const float ScrollbarWidth = 12f;
        private const float ScrollbarGutter = ScrollbarWidth + 4f;
        private const float CanvasVerticalSafetyMargin = 12f;
        private const string WindowModeTemplateName = "WindowMode";
        private const string GameSpeedTemplateName = "GameSpeed";
        private const string LanguageRowName = "Language";
        private const string TextScaleRowName = "TextScale";

        private static readonly FieldInfo LabelField =
            AccessTools.Field(typeof(SettingsGroup), "_label");

        private static ManualLogSource _log;
        private static GameSettingsMenu _owner;
        private static GameObject _languageRow;
        private static GameObject _textScaleRow;
        private static TMP_Dropdown _languageDropdown;
        private static Slider _textScaleSlider;
        private static TextMeshProUGUI _languageLabel;
        private static TextMeshProUGUI _textScaleLabel;
        private static SupportedLanguage _capturedApplyLanguage;
        private static float _capturedApplyTextScale;
        private static bool _hasCapturedApplyValues;
        private static RectTransform _contentRect;
        private static RectTransform _scrollHostRect;
        private static RectTransform _viewportRect;
        private static ScrollRect _scrollRect;
        private static Scrollbar _verticalScrollbar;
        private static bool _renderAlignmentWarningLogged;

        internal static void Configure(ManualLogSource log)
        {
            _log = log;

            if (LabelField == null)
            {
                throw new MissingFieldException(typeof(SettingsGroup).FullName, "_label");
            }
        }

        internal static void LogWarning(string message)
        {
            _log?.LogWarning(message);
        }

        internal static SupportedLanguage PendingLanguage
        {
            get
            {
                if (_languageDropdown == null)
                {
                    return LocalizationState.Current;
                }

                return ModSettings.NormalizeLanguage(
                    (SupportedLanguage)_languageDropdown.value);
            }
        }

        internal static float PendingTextScale
        {
            get
            {
                if (_textScaleSlider == null)
                {
                    return TextScaleManager.CurrentScale;
                }

                return SliderValueToScale(_textScaleSlider.value);
            }
        }

        internal static void EnsureStructure(GameSettingsMenu settingsMenu)
        {
            if (settingsMenu == null)
            {
                return;
            }

            if (_owner != settingsMenu)
            {
                ResetReferences(settingsMenu);
            }

            Transform panel = settingsMenu.transform.Find(SettingsPanelPath);
            if (!(panel is RectTransform panelRect))
            {
                _log?.LogWarning(
                    $"Settings UI injection skipped: '{SettingsPanelPath}' was not found.");
                return;
            }

            RectTransform layoutRect = ResolveVerticalLayout(panelRect);
            if (layoutRect == null)
            {
                _log?.LogWarning(
                    $"Settings UI injection skipped: '{SettingsPanelPath}/{VerticalLayoutName}' was not found.");
                return;
            }

            _contentRect = layoutRect;

            if (_languageRow == null)
            {
                InjectLanguageRow(layoutRect);
            }

            if (_textScaleRow == null)
            {
                InjectTextScaleRow(layoutRect);
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRect);

            float requiredContentHeight = MeasureRequiredContentHeight(layoutRect);
            float requiredContentWidth = MeasureRequiredContentWidth(layoutRect);
            EnsureScrollHierarchy(
                panelRect,
                layoutRect,
                requiredContentHeight,
                requiredContentWidth);

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRect);
            LayoutRebuilder.ForceRebuildLayoutImmediate(panelRect);
            AlignSettingsLabelsLeft(layoutRect);
            EnsureLabelRenderHook();
        }

        internal static void RefreshAfterShow(GameSettingsMenu settingsMenu)
        {
            EnsureStructure(settingsMenu);
            SyncControlsFromAppliedState();
            RefreshInjectedLabels();

            if (_contentRect == null || _viewportRect == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_contentRect);

            float requiredContentHeight = MeasureRequiredContentHeight(_contentRect);
            float requiredContentWidth = MeasureRequiredContentWidth(_contentRect);
            UpdateScrollSizing(requiredContentHeight, requiredContentWidth);

            if (_scrollRect != null)
            {
                _scrollRect.verticalNormalizedPosition = 1f;
            }

            Canvas.ForceUpdateCanvases();
            AlignSettingsLabelsLeft(_contentRect);
            EnsureLabelRenderHook();
        }

        internal static void CapturePendingForApply()
        {
            _hasCapturedApplyValues = false;
            _capturedApplyLanguage = PendingLanguage;
            _capturedApplyTextScale = PendingTextScale;
            _hasCapturedApplyValues = true;
        }

        internal static void CommitCapturedPending()
        {
            if (!_hasCapturedApplyValues)
            {
                return;
            }

            SupportedLanguage language = _capturedApplyLanguage;
            float textScale = _capturedApplyTextScale;
            _hasCapturedApplyValues = false;

            ModSettings.Persist(language, textScale);

            bool languageChanged = LocalizationState.Set(language);
            TextScaleManager.SetScale(textScale);

            if (languageChanged)
            {
                _log?.LogInfo($"Language applied: {LocalizationState.Current}");
                LocalizationRefresh.RefreshAll();
            }
            else
            {
                RefreshInjectedLabels();
            }

            SyncControlsFromAppliedState();
        }

        internal static void DiscardPending()
        {
            _hasCapturedApplyValues = false;
            SyncControlsFromAppliedState();
        }

        internal static void SyncControlsFromAppliedState()
        {
            if (_languageDropdown != null)
            {
                _languageDropdown.SetValueWithoutNotify((int)LocalizationState.Current);
                _languageDropdown.RefreshShownValue();
            }

            if (_textScaleSlider != null)
            {
                _textScaleSlider.SetValueWithoutNotify(
                    ScaleToSliderValue(TextScaleManager.CurrentScale));
            }

            RefreshInjectedLabels();

            if (_contentRect != null)
            {
                AlignSettingsLabelsLeft(_contentRect);
            }
        }

        internal static void RefreshInjectedLabels()
        {
            SupportedLanguage language = LocalizationState.Current;

            if (_languageLabel != null)
            {
                _languageLabel.text = ModStrings.LanguageLabel(language);
            }

            if (_textScaleLabel != null)
            {
                float scale = _textScaleSlider != null
                    ? SliderValueToScale(_textScaleSlider.value)
                    : TextScaleManager.CurrentScale;

                int percent = Mathf.RoundToInt(scale * 100f);
                _textScaleLabel.text =
                    $"{ModStrings.TextScaleLabel(language)} {percent}%";
            }
        }

        private static RectTransform ResolveVerticalLayout(RectTransform panelRect)
        {
            if (_contentRect != null)
            {
                return _contentRect;
            }

            RectTransform direct =
                panelRect.Find(VerticalLayoutName) as RectTransform;
            if (direct != null)
            {
                return direct;
            }

            return panelRect.Find(
                $"{ScrollHostName}/{ScrollViewportName}/{VerticalLayoutName}")
                as RectTransform;
        }

        private static float MeasureRequiredContentHeight(RectTransform layoutRect)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRect);

            float preferredHeight = LayoutUtility.GetPreferredHeight(layoutRect);
            float requiredHeight = Mathf.Max(preferredHeight, layoutRect.rect.height);
            float minY = float.PositiveInfinity;
            float maxY = float.NegativeInfinity;

            for (int i = 0; i < layoutRect.childCount; i++)
            {
                if (!(layoutRect.GetChild(i) is RectTransform childRect) ||
                    !childRect.gameObject.activeSelf)
                {
                    continue;
                }

                Bounds childBounds =
                    RectTransformUtility.CalculateRelativeRectTransformBounds(
                        layoutRect,
                        childRect);
                minY = Mathf.Min(minY, childBounds.min.y);
                maxY = Mathf.Max(maxY, childBounds.max.y);
            }

            if (!float.IsInfinity(minY) && !float.IsInfinity(maxY))
            {
                requiredHeight = Mathf.Max(
                    requiredHeight,
                    Mathf.Max(0f, maxY - minY));
            }

            return requiredHeight;
        }

        private static float MeasureRequiredContentWidth(RectTransform layoutRect)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRect);

            float preferredWidth = LayoutUtility.GetPreferredWidth(layoutRect);
            float requiredWidth = Mathf.Max(preferredWidth, layoutRect.rect.width);
            float minX = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;

            for (int i = 0; i < layoutRect.childCount; i++)
            {
                if (!(layoutRect.GetChild(i) is RectTransform childRect) ||
                    !childRect.gameObject.activeSelf)
                {
                    continue;
                }

                Bounds childBounds =
                    RectTransformUtility.CalculateRelativeRectTransformBounds(
                        layoutRect,
                        childRect);
                minX = Mathf.Min(minX, childBounds.min.x);
                maxX = Mathf.Max(maxX, childBounds.max.x);
            }

            if (!float.IsInfinity(minX) && !float.IsInfinity(maxX))
            {
                requiredWidth = Mathf.Max(
                    requiredWidth,
                    Mathf.Max(0f, maxX - minX));
            }

            return requiredWidth;
        }

        private static void EnsureScrollHierarchy(
            RectTransform panelRect,
            RectTransform layoutRect,
            float requiredContentHeight,
            float requiredContentWidth)
        {
            Transform existingHost = panelRect.Find(ScrollHostName);
            if (_scrollHostRect == null && existingHost != null)
            {
                _scrollHostRect = existingHost as RectTransform;
            }

            if (_scrollHostRect == null)
            {
                int originalSiblingIndex = layoutRect.GetSiblingIndex();
                var hostObject = new GameObject(
                    ScrollHostName,
                    typeof(RectTransform),
                    typeof(LayoutElement),
                    typeof(ScrollRect));
                hostObject.layer = panelRect.gameObject.layer;
                _scrollHostRect = hostObject.GetComponent<RectTransform>();
                _scrollHostRect.SetParent(panelRect, false);
                _scrollHostRect.SetSiblingIndex(originalSiblingIndex);
                _scrollHostRect.anchorMin = new Vector2(0f, 1f);
                _scrollHostRect.anchorMax = new Vector2(1f, 1f);
                _scrollHostRect.pivot = new Vector2(0.5f, 1f);
                _scrollHostRect.sizeDelta = Vector2.zero;
            }

            if (_viewportRect == null)
            {
                Transform existingViewport = _scrollHostRect.Find(ScrollViewportName);
                _viewportRect = existingViewport as RectTransform;
            }

            if (_viewportRect == null)
            {
                var viewportObject = new GameObject(
                    ScrollViewportName,
                    typeof(RectTransform),
                    typeof(RectMask2D));
                viewportObject.layer = panelRect.gameObject.layer;
                _viewportRect = viewportObject.GetComponent<RectTransform>();
                _viewportRect.SetParent(_scrollHostRect, false);
                _viewportRect.anchorMin = Vector2.zero;
                _viewportRect.anchorMax = Vector2.one;
                _viewportRect.pivot = new Vector2(0.5f, 0.5f);
                _viewportRect.offsetMin = Vector2.zero;
                _viewportRect.offsetMax = new Vector2(-ScrollbarGutter, 0f);
            }

            Image viewportRaycastImage = _viewportRect.GetComponent<Image>();
            if (viewportRaycastImage == null)
            {
                viewportRaycastImage = _viewportRect.gameObject.AddComponent<Image>();
            }

            viewportRaycastImage.color = Color.clear;
            viewportRaycastImage.raycastTarget = true;

            if (layoutRect.parent != _viewportRect)
            {
                layoutRect.SetParent(_viewportRect, false);
            }

            layoutRect.anchorMin = new Vector2(0f, 1f);
            layoutRect.anchorMax = new Vector2(1f, 1f);
            layoutRect.pivot = new Vector2(0.5f, 1f);
            layoutRect.anchoredPosition = Vector2.zero;
            layoutRect.sizeDelta = new Vector2(0f, layoutRect.sizeDelta.y);

            ContentSizeFitter contentFitter =
                layoutRect.GetComponent<ContentSizeFitter>();
            if (contentFitter == null)
            {
                contentFitter = layoutRect.gameObject.AddComponent<ContentSizeFitter>();
            }

            contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            EnsureVerticalScrollbar(panelRect);

            _scrollRect = _scrollHostRect.GetComponent<ScrollRect>();
            _scrollRect.horizontal = false;
            _scrollRect.vertical = true;
            _scrollRect.movementType = ScrollRect.MovementType.Clamped;
            _scrollRect.inertia = true;
            _scrollRect.scrollSensitivity = 36f;
            _scrollRect.viewport = _viewportRect;
            _scrollRect.content = layoutRect;
            _scrollRect.verticalScrollbar = _verticalScrollbar;
            _scrollRect.verticalScrollbarVisibility =
                ScrollRect.ScrollbarVisibility.Permanent;
            _scrollRect.verticalScrollbarSpacing = 2f;

            UpdateScrollSizing(requiredContentHeight, requiredContentWidth);
            _scrollRect.verticalNormalizedPosition = 1f;
        }

        private static void UpdateScrollSizing(
            float requiredContentHeight,
            float requiredContentWidth)
        {
            if (_scrollHostRect == null ||
                _viewportRect == null ||
                _contentRect == null)
            {
                return;
            }

            RectTransform panelRect = _scrollHostRect.parent as RectTransform;
            if (panelRect == null)
            {
                return;
            }

            LayoutElement hostLayout = _scrollHostRect.GetComponent<LayoutElement>();
            float maximumViewportHeight =
                ResolveMaximumViewportHeight(panelRect);
            float viewportHeight = Mathf.Min(
                Mathf.Max(1f, requiredContentHeight),
                maximumViewportHeight);
            float hostPreferredWidth =
                Mathf.Max(1f, requiredContentWidth) + ScrollbarGutter;

            hostLayout.preferredWidth = hostPreferredWidth;
            hostLayout.minWidth = hostPreferredWidth;
            hostLayout.flexibleWidth = 0f;
            hostLayout.preferredHeight = viewportHeight;
            hostLayout.minHeight = 0f;
            hostLayout.flexibleHeight = 0f;

            LayoutRebuilder.ForceRebuildLayoutImmediate(_contentRect);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_scrollHostRect);
            LayoutRebuilder.ForceRebuildLayoutImmediate(panelRect);
            Canvas.ForceUpdateCanvases();

            bool hasOverflow =
                _contentRect.rect.height > _viewportRect.rect.height + 0.5f;
            if (_verticalScrollbar != null)
            {
                _verticalScrollbar.gameObject.SetActive(hasOverflow);
                _verticalScrollbar.interactable = hasOverflow;
                _verticalScrollbar.size = _contentRect.rect.height > 0.5f
                    ? Mathf.Clamp01(
                        _viewportRect.rect.height / _contentRect.rect.height)
                    : 1f;
            }

        }

        private static float ResolveMaximumViewportHeight(RectTransform panelRect)
        {
            Canvas ownerCanvas = panelRect.GetComponentInParent<Canvas>();
            Canvas rootCanvas = ownerCanvas != null ? ownerCanvas.rootCanvas : null;
            RectTransform canvasRect = rootCanvas != null
                ? rootCanvas.transform as RectTransform
                : null;
            if (canvasRect == null || canvasRect.rect.height <= 1f)
            {
                _log?.LogWarning(
                    "Settings viewport cap fell back because a valid root Canvas rect could not be resolved.");
                return Mathf.Max(1f, panelRect.rect.height);
            }

            VerticalLayoutGroup panelLayout =
                panelRect.GetComponent<VerticalLayoutGroup>();
            float panelChromeHeight = panelLayout != null
                ? panelLayout.padding.top + panelLayout.padding.bottom
                : 0f;

            return Mathf.Max(
                1f,
                canvasRect.rect.height -
                CanvasVerticalSafetyMargin -
                panelChromeHeight);
        }

        private static void EnsureVerticalScrollbar(RectTransform panelRect)
        {
            if (_scrollHostRect == null)
            {
                return;
            }

            if (_verticalScrollbar == null)
            {
                Transform existing = _scrollHostRect.Find(ScrollbarName);
                if (existing != null)
                {
                    _verticalScrollbar = existing.GetComponent<Scrollbar>();
                }
            }

            if (_verticalScrollbar != null)
            {
                return;
            }

            var scrollbarObject = new GameObject(
                ScrollbarName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Scrollbar));
            scrollbarObject.layer = panelRect.gameObject.layer;
            RectTransform scrollbarRect =
                scrollbarObject.GetComponent<RectTransform>();
            scrollbarRect.SetParent(_scrollHostRect, false);
            scrollbarRect.anchorMin = new Vector2(1f, 0f);
            scrollbarRect.anchorMax = new Vector2(1f, 1f);
            scrollbarRect.pivot = new Vector2(1f, 0.5f);
            scrollbarRect.offsetMin = new Vector2(-ScrollbarWidth, 4f);
            scrollbarRect.offsetMax = new Vector2(-2f, -4f);

            Image background = scrollbarObject.GetComponent<Image>();
            background.color = new Color(0.14f, 0.08f, 0.04f, 0.92f);
            background.raycastTarget = true;

            var slidingArea = new GameObject(
                "Sliding Area",
                typeof(RectTransform));
            slidingArea.layer = panelRect.gameObject.layer;
            RectTransform slidingRect = slidingArea.GetComponent<RectTransform>();
            slidingRect.SetParent(scrollbarRect, false);
            slidingRect.anchorMin = Vector2.zero;
            slidingRect.anchorMax = Vector2.one;
            slidingRect.offsetMin = new Vector2(2f, 2f);
            slidingRect.offsetMax = new Vector2(-2f, -2f);

            var handleObject = new GameObject(
                "Handle",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            handleObject.layer = panelRect.gameObject.layer;
            RectTransform handleRect = handleObject.GetComponent<RectTransform>();
            handleRect.SetParent(slidingRect, false);
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = Vector2.one;
            handleRect.offsetMin = Vector2.zero;
            handleRect.offsetMax = Vector2.zero;

            Image handleImage = handleObject.GetComponent<Image>();
            handleImage.color = new Color(1f, 0.88f, 0.58f, 1f);
            handleImage.raycastTarget = true;

            _verticalScrollbar = scrollbarObject.GetComponent<Scrollbar>();
            _verticalScrollbar.handleRect = handleRect;
            _verticalScrollbar.targetGraphic = handleImage;
            _verticalScrollbar.direction = Scrollbar.Direction.BottomToTop;
            _verticalScrollbar.numberOfSteps = 0;

            Navigation navigation = _verticalScrollbar.navigation;
            navigation.mode = Navigation.Mode.None;
            _verticalScrollbar.navigation = navigation;
        }

        private static void ResetReferences(GameSettingsMenu owner)
        {
            _owner = owner;
            _renderAlignmentWarningLogged = false;
            _languageRow = null;
            _textScaleRow = null;
            _languageDropdown = null;
            _textScaleSlider = null;
            _languageLabel = null;
            _textScaleLabel = null;
            _contentRect = null;
            _scrollHostRect = null;
            _viewportRect = null;
            _scrollRect = null;
            _verticalScrollbar = null;
        }

        private static void InjectLanguageRow(Transform verticalLayout)
        {
            if (verticalLayout.Find(LanguageRowName) != null)
            {
                _log?.LogWarning(
                    $"Language setting row was not injected because '{LanguageRowName}' already exists.");
                return;
            }

            Transform template = verticalLayout.Find(WindowModeTemplateName);
            if (template == null)
            {
                _log?.LogWarning(
                    $"Language setting row was not injected: '{WindowModeTemplateName}' template not found.");
                return;
            }

            GameObject row = UnityEngine.Object.Instantiate(
                template.gameObject,
                verticalLayout,
                false);

            row.name = LanguageRowName;
            row.transform.SetSiblingIndex(template.GetSiblingIndex());
            TextScaleManager.CopyCloneBaselines(template.gameObject, row);

            DropdownGroup group = row.GetComponent<DropdownGroup>();
            if (group == null || group.Dropdown == null)
            {
                _log?.LogWarning(
                    "Language setting row clone did not contain the expected DropdownGroup/TMP_Dropdown.");
                UnityEngine.Object.Destroy(row);
                return;
            }

            TextMeshProUGUI label = GetLabel(group);
            TMP_Dropdown dropdown = group.Dropdown;
            if (label == null)
            {
                _log?.LogWarning(
                    "Language setting row clone did not expose the expected SettingsGroup label.");
                UnityEngine.Object.Destroy(row);
                return;
            }

            group.enabled = false;
            UnityEngine.Object.Destroy(group);

            dropdown.onValueChanged = new TMP_Dropdown.DropdownEvent();
            dropdown.ClearOptions();
            dropdown.AddOptions(
                new List<TMP_Dropdown.OptionData>
                {
                    new TMP_Dropdown.OptionData(ModStrings.EnglishOption),
                    new TMP_Dropdown.OptionData(ModStrings.TraditionalChineseOption),
                    new TMP_Dropdown.OptionData(ModStrings.SimplifiedChineseOption)
                });

            dropdown.SetValueWithoutNotify((int)LocalizationState.Current);
            dropdown.RefreshShownValue();
            ResetNavigationToAutomatic(dropdown);

            _languageRow = row;
            _languageDropdown = dropdown;
            _languageLabel = label;
        }

        private static void InjectTextScaleRow(Transform verticalLayout)
        {
            if (verticalLayout.Find(TextScaleRowName) != null)
            {
                _log?.LogWarning(
                    $"Text Scale setting row was not injected because '{TextScaleRowName}' already exists.");
                return;
            }

            Transform template = verticalLayout.Find(GameSpeedTemplateName);
            if (template == null)
            {
                _log?.LogWarning(
                    $"Text Scale setting row was not injected: '{GameSpeedTemplateName}' template not found.");
                return;
            }

            GameObject row = UnityEngine.Object.Instantiate(
                template.gameObject,
                verticalLayout,
                false);

            row.name = TextScaleRowName;

            Transform windowModeTemplate = verticalLayout.Find(WindowModeTemplateName);
            int insertionIndex = _languageRow != null
                ? _languageRow.transform.GetSiblingIndex() + 1
                : windowModeTemplate != null
                    ? windowModeTemplate.GetSiblingIndex()
                    : template.GetSiblingIndex();

            row.transform.SetSiblingIndex(insertionIndex);
            TextScaleManager.CopyCloneBaselines(template.gameObject, row);

            SliderGroup group = row.GetComponent<SliderGroup>();
            if (group == null || group.Slider == null)
            {
                _log?.LogWarning(
                    "Text Scale setting row clone did not contain the expected SliderGroup/Slider.");
                UnityEngine.Object.Destroy(row);
                return;
            }

            TextMeshProUGUI label = GetLabel(group);
            Slider slider = group.Slider;
            if (label == null)
            {
                _log?.LogWarning(
                    "Text Scale setting row clone did not expose the expected SettingsGroup label.");
                UnityEngine.Object.Destroy(row);
                return;
            }

            group.enabled = false;
            UnityEngine.Object.Destroy(group);

            slider.onValueChanged = new Slider.SliderEvent();
            slider.minValue = ScaleToSliderValue(ModSettings.MinimumTextScale);
            slider.maxValue = ScaleToSliderValue(ModSettings.MaximumTextScale);
            slider.wholeNumbers = true;
            slider.SetValueWithoutNotify(ScaleToSliderValue(TextScaleManager.CurrentScale));
            slider.onValueChanged.AddListener(OnTextScaleSliderChanged);
            ResetNavigationToAutomatic(slider);

            _textScaleRow = row;
            _textScaleSlider = slider;
            _textScaleLabel = label;
        }

        private static TextMeshProUGUI GetLabel(SettingsGroup group)
        {
            return LabelField.GetValue(group) as TextMeshProUGUI;
        }

        private static void EnsureLabelRenderHook()
        {
            Canvas.willRenderCanvases -= OnWillRenderCanvases;
            Canvas.willRenderCanvases += OnWillRenderCanvases;
        }

        private static void OnWillRenderCanvases()
        {
            if (_contentRect == null ||
                !_contentRect.gameObject.activeInHierarchy)
            {
                return;
            }

            try
            {
                AlignSettingsLabelsLeft(_contentRect, false);
            }
            catch (Exception ex)
            {
                if (!_renderAlignmentWarningLogged)
                {
                    _renderAlignmentWarningLogged = true;
                    _log?.LogWarning(
                        $"Settings render-phase label alignment failed: " +
                        $"{ex.GetType().Name}: {ex.Message}");
                }
            }
        }

        private static void AlignSettingsLabelsLeft(
            RectTransform layoutRect,
            bool forceLayout = true)
        {
            if (layoutRect == null)
            {
                return;
            }

            var labels = new List<TextMeshProUGUI>();
            for (int i = 0; i < layoutRect.childCount; i++)
            {
                Transform row = layoutRect.GetChild(i);
                SettingsGroup group = row.GetComponent<SettingsGroup>();
                if (group == null)
                {
                    continue;
                }

                AddUniqueLabel(labels, GetLabel(group));
            }

            AddUniqueLabel(labels, _languageLabel);
            AddUniqueLabel(labels, _textScaleLabel);

            if (labels.Count == 0)
            {
                return;
            }

            if (forceLayout)
            {
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRect);
            }

            VerticalLayoutGroup layoutGroup =
                layoutRect.GetComponent<VerticalLayoutGroup>();
            float targetRectLeft =
                layoutRect.rect.xMin +
                (layoutGroup != null ? layoutGroup.padding.left : 0f);

            for (int i = 0; i < labels.Count; i++)
            {
                TextMeshProUGUI label = labels[i];
                Vector4 margin = label.margin;
                margin.x = 0f;
                label.margin = margin;
                label.alignment = TextAlignmentOptions.MidlineLeft;
                label.horizontalAlignment = HorizontalAlignmentOptions.Left;
                label.ForceMeshUpdate();
            }


            for (int i = 0; i < labels.Count; i++)
            {
                RectTransform labelRect = labels[i].rectTransform;
                float currentLeft =
                    ResolveRectLeftInLayout(layoutRect, labelRect);
                float deltaX = targetRectLeft - currentLeft;
                ShiftRectInLayout(layoutRect, labelRect, deltaX);
            }

            for (int i = 0; i < labels.Count; i++)
            {
                labels[i].ForceMeshUpdate();
            }

            float targetGlyphLeft = float.PositiveInfinity;
            for (int i = 0; i < labels.Count; i++)
            {
                targetGlyphLeft = Mathf.Min(
                    targetGlyphLeft,
                    ResolveVisibleGlyphLeftInLayout(layoutRect, labels[i]));
            }

            for (int i = 0; i < labels.Count; i++)
            {
                TextMeshProUGUI label = labels[i];
                float glyphLeft =
                    ResolveVisibleGlyphLeftInLayout(layoutRect, label);
                float deltaX = targetGlyphLeft - glyphLeft;
                ShiftRectInLayout(
                    layoutRect,
                    label.rectTransform,
                    deltaX);
                label.ForceMeshUpdate();
            }

        }

        private static void AddUniqueLabel(
            List<TextMeshProUGUI> labels,
            TextMeshProUGUI label)
        {
            if (label != null && !labels.Contains(label))
            {
                labels.Add(label);
            }
        }

        private static float ResolveRectLeftInLayout(
            RectTransform layoutRect,
            RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);

            float left = float.PositiveInfinity;
            for (int i = 0; i < corners.Length; i++)
            {
                left = Mathf.Min(
                    left,
                    layoutRect.InverseTransformPoint(corners[i]).x);
            }

            return left;
        }

        private static float ResolveVisibleGlyphLeftInLayout(
            RectTransform layoutRect,
            TextMeshProUGUI label)
        {
            label.ForceMeshUpdate();

            float localLeft = float.PositiveInfinity;
            TMP_TextInfo textInfo = label.textInfo;
            for (int i = 0; i < textInfo.characterCount; i++)
            {
                TMP_CharacterInfo character = textInfo.characterInfo[i];
                if (!character.isVisible)
                {
                    continue;
                }

                localLeft = Mathf.Min(
                    localLeft,
                    Mathf.Min(character.bottomLeft.x, character.topLeft.x));
            }

            if (float.IsInfinity(localLeft))
            {
                return ResolveRectLeftInLayout(
                    layoutRect,
                    label.rectTransform);
            }

            Vector3 worldPoint =
                label.rectTransform.TransformPoint(
                    new Vector3(localLeft, 0f, 0f));
            return layoutRect.InverseTransformPoint(worldPoint).x;
        }

        private static void ShiftRectInLayout(
            RectTransform layoutRect,
            RectTransform rect,
            float deltaX)
        {
            if (Mathf.Abs(deltaX) <= 0.01f)
            {
                return;
            }

            Vector3 worldOrigin = layoutRect.TransformPoint(Vector3.zero);
            Vector3 worldTarget =
                layoutRect.TransformPoint(new Vector3(deltaX, 0f, 0f));
            rect.position += worldTarget - worldOrigin;
        }

        private static void OnTextScaleSliderChanged(float _)
        {
            RefreshInjectedLabels();
            if (_contentRect != null)
            {
                AlignSettingsLabelsLeft(_contentRect);
            }
        }

        private static float ScaleToSliderValue(float scale)
        {
            float normalized = ModSettings.NormalizeTextScale(scale);
            return Mathf.Round(normalized / ModSettings.TextScaleStep);
        }

        private static float SliderValueToScale(float sliderValue)
        {
            float rawScale = Mathf.Round(sliderValue) * ModSettings.TextScaleStep;
            return ModSettings.NormalizeTextScale(rawScale);
        }

        private static void ResetNavigationToAutomatic(Selectable selectable)
        {
            if (selectable == null)
            {
                return;
            }

            Navigation navigation = selectable.navigation;
            navigation.mode = Navigation.Mode.Automatic;
            navigation.selectOnUp = null;
            navigation.selectOnDown = null;
            navigation.selectOnLeft = null;
            navigation.selectOnRight = null;
            selectable.navigation = navigation;
        }
    }

    [HarmonyPatch(typeof(GameSettingsMenu), "Awake")]
    internal static class SettingsMenuAwakePatch
    {
        [HarmonyPostfix]
        private static void Postfix(GameSettingsMenu __instance)
        {
            try
            {
                SettingsMenuPatch.EnsureStructure(__instance);
            }
            catch (Exception ex)
            {
                SettingsMenuPatch.LogWarning(
                    $"Settings UI injection failed during Awake: {ex.GetType().Name}: {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(GameSettingsMenu), nameof(GameSettingsMenu.Show))]
    internal static class SettingsMenuShowPatch
    {
        [HarmonyPostfix]
        private static void Postfix(GameSettingsMenu __instance)
        {
            try
            {
                SettingsMenuPatch.RefreshAfterShow(__instance);
            }
            catch (Exception ex)
            {
                SettingsMenuPatch.LogWarning(
                    $"Settings UI refresh failed during Show: {ex.GetType().Name}: {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(GameSettingsMenu), nameof(GameSettingsMenu.PressApply))]
    internal static class SettingsMenuApplyPatch
    {
        [HarmonyPrefix]
        private static void Prefix()
        {
            try
            {
                SettingsMenuPatch.CapturePendingForApply();
            }
            catch (Exception ex)
            {
                SettingsMenuPatch.LogWarning(
                    $"Failed to capture mod settings before Apply: {ex.GetType().Name}: {ex.Message}");
            }
        }

        [HarmonyPostfix]
        private static void Postfix()
        {
            try
            {
                SettingsMenuPatch.CommitCapturedPending();
            }
            catch (Exception ex)
            {
                SettingsMenuPatch.LogWarning(
                    $"Failed to commit mod settings after Apply: {ex.GetType().Name}: {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(GameSettingsMenu), nameof(GameSettingsMenu.PressCancel))]
    internal static class SettingsMenuCancelPatch
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            try
            {
                SettingsMenuPatch.DiscardPending();
            }
            catch (Exception ex)
            {
                SettingsMenuPatch.LogWarning(
                    $"Failed to restore mod settings after Cancel: {ex.GetType().Name}: {ex.Message}");
            }
        }
    }
}
