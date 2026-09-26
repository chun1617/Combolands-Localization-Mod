using System;
using BepInEx.Configuration;

namespace Combolands.Localization
{
    internal enum SupportedLanguage
    {
        English = 0,
        TraditionalChinese = 1,
        SimplifiedChinese = 2
    }

    internal static class ModSettings
    {
        internal const string Section = "General";
        internal const string LanguageKey = "Language";
        internal const string TextScaleKey = "TextScale";

        internal const SupportedLanguage DefaultLanguage = SupportedLanguage.TraditionalChinese;
        internal const float MinimumTextScale = 0.80f;
        internal const float MaximumTextScale = 1.40f;
        internal const float TextScaleStep = 0.05f;
        internal const float DefaultTextScale = 1.00f;

        private static ConfigFile _config;
        private static ConfigEntry<string> _languageEntry;
        private static ConfigEntry<float> _textScaleEntry;

        public static SupportedLanguage Language { get; private set; } = DefaultLanguage;
        public static float TextScale { get; private set; } = DefaultTextScale;

        public static void Initialize(ConfigFile config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            _config = config;
            _languageEntry = config.Bind(
                Section,
                LanguageKey,
                DefaultLanguage.ToString(),
                "Runtime language. Supported values: English, TraditionalChinese, SimplifiedChinese.");

            _textScaleEntry = config.Bind(
                Section,
                TextScaleKey,
                DefaultTextScale,
                "Text scale multiplier. Supported range: 0.80 to 1.40 in 0.05 steps.");

            Language = NormalizeLanguage(_languageEntry.Value);
            TextScale = NormalizeTextScale(_textScaleEntry.Value);

            bool normalized = false;
            string normalizedLanguage = Language.ToString();
            if (!string.Equals(_languageEntry.Value, normalizedLanguage, StringComparison.Ordinal))
            {
                _languageEntry.Value = normalizedLanguage;
                normalized = true;
            }

            if (!NearlyEqual(_textScaleEntry.Value, TextScale))
            {
                _textScaleEntry.Value = TextScale;
                normalized = true;
            }

            if (normalized)
            {
                _config.Save();
            }

        }

        public static void Persist(SupportedLanguage language, float textScale)
        {
            EnsureInitialized();

            Language = NormalizeLanguage(language);
            TextScale = NormalizeTextScale(textScale);

            _languageEntry.Value = Language.ToString();
            _textScaleEntry.Value = TextScale;
            _config.Save();
        }

        internal static SupportedLanguage NormalizeLanguage(string value)
        {
            if (string.Equals(value?.Trim(), nameof(SupportedLanguage.English), StringComparison.OrdinalIgnoreCase))
            {
                return SupportedLanguage.English;
            }

            if (string.Equals(
                    value?.Trim(),
                    nameof(SupportedLanguage.TraditionalChinese),
                    StringComparison.OrdinalIgnoreCase))
            {
                return SupportedLanguage.TraditionalChinese;
            }

            if (string.Equals(
                    value?.Trim(),
                    nameof(SupportedLanguage.SimplifiedChinese),
                    StringComparison.OrdinalIgnoreCase))
            {
                return SupportedLanguage.SimplifiedChinese;
            }

            return DefaultLanguage;
        }

        internal static SupportedLanguage NormalizeLanguage(SupportedLanguage value)
        {
            switch (value)
            {
                case SupportedLanguage.English:
                case SupportedLanguage.TraditionalChinese:
                case SupportedLanguage.SimplifiedChinese:
                    return value;
                default:
                    return DefaultLanguage;
            }
        }

        internal static float NormalizeTextScale(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return DefaultTextScale;
            }

            float clamped = Math.Max(MinimumTextScale, Math.Min(MaximumTextScale, value));
            float snapped = (float)(
                Math.Round(
                    clamped / TextScaleStep,
                    MidpointRounding.AwayFromZero) *
                TextScaleStep);

            return Math.Max(MinimumTextScale, Math.Min(MaximumTextScale, snapped));
        }

        private static void EnsureInitialized()
        {
            if (_config == null || _languageEntry == null || _textScaleEntry == null)
            {
                throw new InvalidOperationException("ModSettings.Initialize must be called before Persist.");
            }
        }

        private static bool NearlyEqual(float left, float right)
        {
            return Math.Abs(left - right) < 0.0001f;
        }
    }
}
