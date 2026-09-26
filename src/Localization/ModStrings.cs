namespace Combolands.Localization
{
    internal static class ModStrings
    {
        public static string LanguageLabel(SupportedLanguage language)
        {
            switch (language)
            {
                case SupportedLanguage.TraditionalChinese:
                    return "語言";
                case SupportedLanguage.SimplifiedChinese:
                    return "语言";
                default:
                    return "Language";
            }
        }

        public static string TextScaleLabel(SupportedLanguage language)
        {
            switch (language)
            {
                case SupportedLanguage.TraditionalChinese:
                    return "文字縮放比例";
                case SupportedLanguage.SimplifiedChinese:
                    return "文字缩放比例";
                default:
                    return "Text Scale";
            }
        }

        public static string EnglishOption => "English";

        public static string TraditionalChineseOption => "繁體中文";

        public static string SimplifiedChineseOption => "简体中文";
    }
}
