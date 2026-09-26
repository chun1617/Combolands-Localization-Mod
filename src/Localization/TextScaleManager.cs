using System;
using System.Collections.Generic;
using BepInEx.Logging;
using TMPro;
using UnityEngine;

namespace Combolands.Localization
{
    internal static class TextScaleManager
    {
        private static readonly Dictionary<int, BaselineEntry> Baselines =
            new Dictionary<int, BaselineEntry>();

        private static ManualLogSource _log;

        public static float CurrentScale { get; private set; } =
            ModSettings.DefaultTextScale;

        public static void Initialize(float scale, ManualLogSource log)
        {
            _log = log;
            CurrentScale = ModSettings.NormalizeTextScale(scale);
        }

        public static bool SetScale(float scale)
        {
            float normalized = ModSettings.NormalizeTextScale(scale);
            bool changed = !NearlyEqual(CurrentScale, normalized);

            CurrentScale = normalized;
            RefreshAll();

            if (changed)
            {
                _log?.LogInfo($"Text scale applied: {CurrentScale:0.00}");
            }

            return changed;
        }

        public static void RefreshAll()
        {
            CleanupDeadEntries();

            TextMeshProUGUI[] textMeshes =
                UnityEngine.Object.FindObjectsByType<TextMeshProUGUI>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None);

            foreach (TextMeshProUGUI textMesh in textMeshes)
            {
                if (textMesh == null)
                {
                    continue;
                }

                Apply(textMesh);
            }

        }

        public static void Apply(TextMeshProUGUI text)
        {
            if (text == null)
            {
                return;
            }

            BaselineEntry baseline = GetOrCaptureBaseline(text);

            if (text.enableAutoSizing)
            {
                text.fontSizeMin = ScaleBaseline(baseline.FontSizeMin, CurrentScale);
                text.fontSizeMax = ScaleBaseline(baseline.FontSizeMax, CurrentScale);
                return;
            }

            text.fontSize = ScaleBaseline(baseline.FontSize, CurrentScale);
        }

        internal static void CopyCloneBaselines(GameObject sourceRoot, GameObject cloneRoot)
        {
            if (sourceRoot == null || cloneRoot == null)
            {
                return;
            }

            TextMeshProUGUI[] sourceTexts =
                sourceRoot.GetComponentsInChildren<TextMeshProUGUI>(true);
            TextMeshProUGUI[] cloneTexts =
                cloneRoot.GetComponentsInChildren<TextMeshProUGUI>(true);

            int sharedCount = Math.Min(sourceTexts.Length, cloneTexts.Length);
            for (int i = 0; i < sharedCount; i++)
            {
                TextMeshProUGUI source = sourceTexts[i];
                TextMeshProUGUI clone = cloneTexts[i];
                if (source == null || clone == null)
                {
                    continue;
                }

                BaselineEntry sourceBaseline = GetOrCaptureBaseline(source);
                Baselines[clone.GetInstanceID()] = new BaselineEntry(
                    clone,
                    sourceBaseline.FontSize,
                    sourceBaseline.FontSizeMin,
                    sourceBaseline.FontSizeMax);

                // Keep inactive template text at its unscaled baseline. TMP_Dropdown
                // clones its inactive item template when opened; scaling the template here
                // would make the runtime clone capture an already-scaled font size and
                // apply CurrentScale a second time (for example 1.40 x 1.40).
                if (clone.gameObject.activeInHierarchy)
                {
                    Apply(clone);
                }
            }

            if (sourceTexts.Length != cloneTexts.Length)
            {
                _log?.LogWarning(
                    $"TMP clone baseline count mismatch: source={sourceTexts.Length}, " +
                    $"clone={cloneTexts.Length}. Matched={sharedCount}.");
            }
        }

        internal static float ScaleBaseline(float baseline, float scale)
        {
            return baseline * scale;
        }

        private static BaselineEntry GetOrCaptureBaseline(TextMeshProUGUI text)
        {
            int instanceId = text.GetInstanceID();

            if (Baselines.TryGetValue(instanceId, out BaselineEntry existing) &&
                existing.Text != null &&
                ReferenceEquals(existing.Text, text))
            {
                return existing;
            }

            var captured = new BaselineEntry(
                text,
                text.fontSize,
                text.fontSizeMin,
                text.fontSizeMax);

            Baselines[instanceId] = captured;
            return captured;
        }

        private static void CleanupDeadEntries()
        {
            if (Baselines.Count == 0)
            {
                return;
            }

            var deadInstanceIds = new List<int>();
            foreach (KeyValuePair<int, BaselineEntry> pair in Baselines)
            {
                if (pair.Value.Text == null)
                {
                    deadInstanceIds.Add(pair.Key);
                }
            }

            foreach (int instanceId in deadInstanceIds)
            {
                Baselines.Remove(instanceId);
            }
        }

        private static bool NearlyEqual(float left, float right)
        {
            return Math.Abs(left - right) < 0.0001f;
        }

        private sealed class BaselineEntry
        {
            public BaselineEntry(
                TextMeshProUGUI text,
                float fontSize,
                float fontSizeMin,
                float fontSizeMax)
            {
                Text = text;
                FontSize = fontSize;
                FontSizeMin = fontSizeMin;
                FontSizeMax = fontSizeMax;
            }

            public TextMeshProUGUI Text { get; }
            public float FontSize { get; }
            public float FontSizeMin { get; }
            public float FontSizeMax { get; }
        }
    }
}
