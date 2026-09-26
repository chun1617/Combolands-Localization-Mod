using System;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using GameLocalizedTextMeshProUGUI = Library.Localization.LocalizedTextMeshProUGUI;
using TMPro;
using UnityEngine;

namespace Combolands.Localization
{
    internal static class LocalizationRefresh
    {
        private static ManualLogSource _log;
        private static MethodInfo _setTextMethod;

        internal static void Configure(ManualLogSource log)
        {
            _log = log;
            _setTextMethod = AccessTools.Method(typeof(GameLocalizedTextMeshProUGUI), "SetText");

            if (_setTextMethod == null)
            {
                throw new MissingMethodException(
                    typeof(GameLocalizedTextMeshProUGUI).FullName,
                    "SetText");
            }
        }

        public static void RefreshAll()
        {
            EnsureConfigured();

            GameLocalizedTextMeshProUGUI[] localizedTexts =
                UnityEngine.Object.FindObjectsByType<GameLocalizedTextMeshProUGUI>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None);

            foreach (GameLocalizedTextMeshProUGUI localizedText in localizedTexts)
            {
                if (localizedText == null)
                {
                    continue;
                }

                try
                {
                    _setTextMethod.Invoke(localizedText, null);
                }
                catch (Exception ex)
                {
                    _log?.LogWarning(
                        $"Failed to refresh localized TMP '{localizedText.name}': " +
                        $"{ex.GetType().Name}: {ex.Message}");
                }
            }

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

                CjkFontFallbackPatch.RefreshDirectText(textMesh);
                CjkFontFallbackPatch.AttachCurrentFallback(textMesh);
            }

            SettingsMenuPatch.RefreshInjectedLabels();
        }

        private static void EnsureConfigured()
        {
            if (_setTextMethod == null)
            {
                throw new InvalidOperationException(
                    "LocalizationRefresh.Configure must be called before RefreshAll.");
            }
        }
    }
}
