using HarmonyLib;
using GameLocalizedStringAsset = Library.Localization.LocalizedStringAsset;

namespace Combolands.Localization
{
    [HarmonyPatch(typeof(GameLocalizedStringAsset), nameof(GameLocalizedStringAsset.GetText))]
    internal static class TranslationPatch
    {
        [HarmonyPostfix]
        private static void Postfix(GameLocalizedStringAsset __instance, ref string __result)
        {
            if (!LocalizationState.IsChinese)
            {
                return;
            }

            string key = __instance?.Key;
            if (!TranslationCatalog.TryGet(key, out string translated))
            {
                return;
            }

            __result = translated;
        }
    }
}
