using HarmonyLib;
using GameMainMenuController = Shared.UI.MainMenuController;

namespace Combolands.Localization
{
    [HarmonyPatch(typeof(GameMainMenuController), "Awake")]
    internal static class MainMenuLogoPatch
    {
        private static void Postfix()
        {
            MainMenuLogoOverride.Refresh();
        }
    }
}
