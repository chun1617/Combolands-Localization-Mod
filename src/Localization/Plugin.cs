using System;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace Combolands.Localization
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.combolands.localization";
        public const string PluginName = "Localization";
        public const string PluginVersion = "0.4.0";

        private Harmony _harmony;

        private void Awake()
        {
            ManualLogSource log = Logger;
            string pluginDirectory = Path.GetDirectoryName(Info.Location) ?? Paths.PluginPath;
            string catalogPath = Path.Combine(pluginDirectory, "zh-Hant.json");

            try
            {
                ModSettings.Initialize(Config);
                LocalizationState.Initialize(ModSettings.Language);
                TextScaleManager.Initialize(ModSettings.TextScale, log);

                TranslationCatalog.Load(catalogPath, log);
                CjkFontFallback.Initialize();
                LocalizationRefresh.Configure(log);
                SettingsMenuPatch.Configure(log);
                MainMenuLogoOverride.Configure(log);

                _harmony = new Harmony(PluginGuid);
                _harmony.PatchAll();
                TextScaleManager.RefreshAll();

                log.LogInfo(
                    $"{PluginName} {PluginVersion} loaded. " +
                    $"Language={LocalizationState.Current}; " +
                    $"TextScale={TextScaleManager.CurrentScale:0.00}; " +
                    $"Translations={TranslationCatalog.Count}; " +
                    $"TraditionalFont={CjkFontFallback.TraditionalFontDescription ?? "<none>"}; " +
                    $"SimplifiedFont={CjkFontFallback.SimplifiedFontDescription ?? "<none>"}.");
            }
            catch (Exception ex)
            {
                log.LogError($"Failed to initialize {PluginName}: {ex}");
            }
        }

        private void OnDestroy()
        {
            if (_harmony != null)
            {
                _harmony.UnpatchSelf();
                _harmony = null;
            }
        }
    }
}
