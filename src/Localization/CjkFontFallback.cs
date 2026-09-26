using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Combolands.Localization
{
    internal static class CjkFontFallback
    {
        private const string TraditionalProbeText = "繁體中文測試";
        private const string SimplifiedProbeText = "简体中文测试";

        private static readonly string[] TraditionalPreferredFamilies =
        {
            "Microsoft JhengHei UI",
            "Microsoft JhengHei",
            "Noto Sans CJK TC",
            "Noto Sans TC",
            "PingFang TC",
            "Heiti TC",
            "Arial Unicode MS"
        };

        private static readonly string[] SimplifiedPreferredFamilies =
        {
            "Microsoft YaHei UI",
            "Microsoft YaHei",
            "Noto Sans CJK SC",
            "Noto Sans SC",
            "DengXian",
            "SimHei",
            "SimSun",
            "Arial Unicode MS"
        };

        public static TMP_FontAsset TraditionalFontAsset { get; private set; }
        public static TMP_FontAsset SimplifiedFontAsset { get; private set; }

        public static TMP_FontAsset FontAsset =>
            LocalizationState.IsSimplifiedChinese
                ? SimplifiedFontAsset ?? TraditionalFontAsset
                : TraditionalFontAsset ?? SimplifiedFontAsset;

        public static string TraditionalFontDescription { get; private set; }
        public static string SimplifiedFontDescription { get; private set; }

        public static string SelectedFontDescription =>
            LocalizationState.IsSimplifiedChinese
                ? SimplifiedFontDescription ?? TraditionalFontDescription
                : TraditionalFontDescription ?? SimplifiedFontDescription;

        public static void Initialize()
        {
            FontEngine.InitializeFontEngine();

            MethodInfo getSystemFontReferences = typeof(FontEngine).GetMethod(
                "GetSystemFontReferences",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            if (getSystemFontReferences == null)
            {
                throw new MissingMethodException(typeof(FontEngine).FullName, "GetSystemFontReferences");
            }

            object rawReferences = getSystemFontReferences.Invoke(null, null);
            if (!(rawReferences is IEnumerable enumerable))
            {
                throw new InvalidOperationException("TextCore system font references were not enumerable.");
            }

            var candidates = new List<FontCandidate>();
            foreach (object item in enumerable)
            {
                if (item != null)
                {
                    candidates.Add(FontCandidate.FromReference(item));
                }
            }

            TraditionalFontAsset = CreateBest(
                candidates,
                TraditionalPreferredFamilies,
                TraditionalProbeText,
                out string traditionalDescription);

            TraditionalFontDescription = traditionalDescription;

            SimplifiedFontAsset = CreateBest(
                candidates,
                SimplifiedPreferredFamilies,
                SimplifiedProbeText,
                out string simplifiedDescription);

            SimplifiedFontDescription = simplifiedDescription;

            if (TraditionalFontAsset == null && SimplifiedFontAsset == null)
            {
                throw new InvalidOperationException(
                    "No usable system CJK font passed either Traditional or Simplified Chinese glyph probes.");
            }

            InstallGlobalFallback(TraditionalFontAsset);
            if (!ReferenceEquals(TraditionalFontAsset, SimplifiedFontAsset))
            {
                InstallGlobalFallback(SimplifiedFontAsset);
            }

        }

        private static TMP_FontAsset CreateBest(
            List<FontCandidate> candidates,
            string[] preferredFamilies,
            string probeText,
            out string description)
        {
            var ordered = new List<FontCandidate>(candidates);
            ordered.Sort((left, right) => CompareCandidates(left, right, preferredFamilies));

            foreach (FontCandidate candidate in ordered)
            {
                TMP_FontAsset font = TryCreate(candidate, probeText);
                if (font == null)
                {
                    continue;
                }

                description =
                    $"{candidate.FamilyName}/{candidate.StyleName} " +
                    $"[{candidate.FilePath}#{candidate.FaceIndex}]";

                UnityEngine.Object.DontDestroyOnLoad(font);
                return font;
            }

            description = null;
            return null;
        }

        private static void InstallGlobalFallback(TMP_FontAsset font)
        {
            if (font == null)
            {
                return;
            }

            IList<TMP_FontAsset> globalFallbacks = TMP_Settings.fallbackFontAssets;
            if (globalFallbacks != null && !globalFallbacks.Contains(font))
            {
                globalFallbacks.Insert(0, font);
            }
        }

        private static TMP_FontAsset TryCreate(
            FontCandidate candidate,
            string probeText)
        {
            if (string.IsNullOrEmpty(candidate.FamilyName) || string.IsNullOrEmpty(candidate.StyleName))
            {
                return null;
            }

            try
            {
                TMP_FontAsset font = TMP_FontAsset.CreateFontAsset(
                    candidate.FamilyName,
                    candidate.StyleName,
                    64);

                if (font == null)
                {
                    return null;
                }

                font.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                font.isMultiAtlasTexturesEnabled = true;

                if (!font.TryAddCharacters(probeText, out string missingCharacters) ||
                    !string.IsNullOrEmpty(missingCharacters))
                {
                    UnityEngine.Object.Destroy(font);
                    return null;
                }

                return font;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static int CompareCandidates(
            FontCandidate left,
            FontCandidate right,
            string[] preferredFamilies)
        {
            int rank = Rank(left.FamilyName, preferredFamilies)
                .CompareTo(Rank(right.FamilyName, preferredFamilies));
            if (rank != 0)
            {
                return rank;
            }

            rank = string.Compare(left.FamilyName, right.FamilyName, StringComparison.OrdinalIgnoreCase);
            if (rank != 0)
            {
                return rank;
            }

            return string.Compare(left.StyleName, right.StyleName, StringComparison.OrdinalIgnoreCase);
        }

        private static int Rank(string familyName, string[] preferredFamilies)
        {
            for (int i = 0; i < preferredFamilies.Length; i++)
            {
                if (string.Equals(familyName, preferredFamilies[i], StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return preferredFamilies.Length;
        }

        private sealed class FontCandidate
        {
            public string FamilyName;
            public string StyleName;
            public string FilePath;
            public int FaceIndex;

            public static FontCandidate FromReference(object reference)
            {
                Type type = reference.GetType();
                return new FontCandidate
                {
                    FamilyName = ReadString(type, reference, "familyName"),
                    StyleName = ReadString(type, reference, "styleName"),
                    FilePath = ReadString(type, reference, "filePath"),
                    FaceIndex = ReadInt(type, reference, "faceIndex")
                };
            }

            private static string ReadString(Type type, object instance, string name)
            {
                object value = ReadMember(type, instance, name);
                return value as string ?? string.Empty;
            }

            private static int ReadInt(Type type, object instance, string name)
            {
                object value = ReadMember(type, instance, name);
                return value == null ? 0 : Convert.ToInt32(value);
            }

            private static object ReadMember(Type type, object instance, string name)
            {
                const BindingFlags flags =
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

                FieldInfo field = type.GetField(name, flags);
                if (field != null)
                {
                    return field.GetValue(instance);
                }

                PropertyInfo property = type.GetProperty(name, flags);
                if (property != null)
                {
                    return property.GetValue(instance, null);
                }

                return null;
            }
        }
    }

    [HarmonyPatch(typeof(TextMeshProUGUI), "OnEnable")]
    internal static class CjkFontFallbackPatch
    {
        private const string SelectGuildsEnglish = "Select Guilds";
        private const string SelectGuildsKey = "Ui.SelectGuilds";


        internal static string ResolveDirectText(string text)
        {
            TranslationCatalog.TryGetForLanguage(
                SelectGuildsKey,
                SupportedLanguage.TraditionalChinese,
                out string traditional);

            TranslationCatalog.TryGetForLanguage(
                SelectGuildsKey,
                SupportedLanguage.SimplifiedChinese,
                out string simplified);

            if (LocalizationState.Current == SupportedLanguage.English)
            {
                return string.Equals(text, traditional, StringComparison.Ordinal) ||
                       string.Equals(text, simplified, StringComparison.Ordinal)
                    ? SelectGuildsEnglish
                    : text;
            }

            if (!TranslationCatalog.TryGet(SelectGuildsKey, out string translated) ||
                string.IsNullOrEmpty(translated))
            {
                return text;
            }

            return string.Equals(text, SelectGuildsEnglish, StringComparison.Ordinal) ||
                   string.Equals(text, traditional, StringComparison.Ordinal) ||
                   string.Equals(text, simplified, StringComparison.Ordinal)
                ? translated
                : text;
        }

        internal static void RefreshDirectText(TextMeshProUGUI text)
        {
            if (text == null)
            {
                return;
            }

            string before = text.text;
            string after = ResolveDirectText(before);
            if (string.Equals(before, after, StringComparison.Ordinal))
            {
                return;
            }

            text.text = after;
        }

        internal static void AttachCurrentFallback(TextMeshProUGUI text)
        {
            TMP_FontAsset fallback = CjkFontFallback.FontAsset;
            TMP_FontAsset source = text?.font;
            if (fallback == null || source == null || ReferenceEquals(source, fallback))
            {
                return;
            }

            if (source.fallbackFontAssetTable == null)
            {
                source.fallbackFontAssetTable = new List<TMP_FontAsset>();
            }

            source.fallbackFontAssetTable.Remove(fallback);
            source.fallbackFontAssetTable.Insert(0, fallback);

            text.havePropertiesChanged = true;
            text.ForceMeshUpdate(true, true);
            text.SetAllDirty();
        }

        [HarmonyPostfix]
        private static void Postfix(TextMeshProUGUI __instance)
        {
            RefreshDirectText(__instance);
            TextScaleManager.Apply(__instance);
            AttachCurrentFallback(__instance);
        }
    }
}
