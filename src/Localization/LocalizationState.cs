namespace Combolands.Localization
{
    internal static class LocalizationState
    {
        public static SupportedLanguage Current { get; private set; } =
            SupportedLanguage.TraditionalChinese;

        public static bool IsTraditionalChinese =>
            Current == SupportedLanguage.TraditionalChinese;

        public static bool IsSimplifiedChinese =>
            Current == SupportedLanguage.SimplifiedChinese;

        public static bool IsChinese =>
            IsTraditionalChinese || IsSimplifiedChinese;

        public static void Initialize(SupportedLanguage language)
        {
            Current = ModSettings.NormalizeLanguage(language);
        }

        public static bool Set(SupportedLanguage language)
        {
            SupportedLanguage normalized = ModSettings.NormalizeLanguage(language);
            if (Current == normalized)
            {
                return false;
            }

            Current = normalized;
            return true;
        }
    }
}
