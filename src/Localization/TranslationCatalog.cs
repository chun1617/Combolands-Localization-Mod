using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using BepInEx.Logging;

namespace Combolands.Localization
{
    internal static class TranslationCatalog
    {
        [DataContract]
        private sealed class CatalogDocument
        {
            [DataMember(Name = "locale")]
            public string Locale = string.Empty;

            [DataMember(Name = "entries")]
            public List<CatalogEntry> Entries = new List<CatalogEntry>();
        }

        [DataContract]
        private sealed class CatalogEntry
        {
            [DataMember(Name = "key")]
            public string Key = string.Empty;

            [DataMember(Name = "text")]
            public string Text = string.Empty;
        }

        private static readonly Dictionary<string, string> TraditionalEntries =
            new Dictionary<string, string>(StringComparer.Ordinal);

        private static readonly Dictionary<string, string> SimplifiedEntries =
            new Dictionary<string, string>(StringComparer.Ordinal);

        public static int Count => TraditionalEntries.Count;

        public static bool TryGet(string key, out string value)
        {
            return TryGetForLanguage(key, LocalizationState.Current, out value);
        }

        public static bool TryGetForLanguage(
            string key,
            SupportedLanguage language,
            out string value)
        {
            if (key == null || language == SupportedLanguage.English)
            {
                value = null;
                return false;
            }

            if (!TraditionalEntries.TryGetValue(key, out string traditional))
            {
                value = null;
                return false;
            }

            if (language == SupportedLanguage.TraditionalChinese)
            {
                value = traditional;
                return true;
            }

            if (SimplifiedEntries.TryGetValue(key, out value))
            {
                return true;
            }

            value = ChineseTextConverter.ToSimplified(traditional);
            SimplifiedEntries[key] = value;
            return true;
        }

        public static void Load(string path, ManualLogSource log)
        {
            TraditionalEntries.Clear();
            SimplifiedEntries.Clear();

            if (!File.Exists(path))
            {
                log.LogWarning($"zh-Hant catalog not found: {path}. Original English will be preserved.");
                return;
            }

            byte[] bytes = File.ReadAllBytes(path);
            if (bytes.Length == 0)
            {
                log.LogWarning($"zh-Hant catalog is empty: {path}. Original English will be preserved.");
                return;
            }

            CatalogDocument document;
            var serializer = new DataContractJsonSerializer(typeof(CatalogDocument));
            using (var stream = new MemoryStream(bytes, writable: false))
            {
                document = serializer.ReadObject(stream) as CatalogDocument;
            }

            if (document == null || document.Entries == null)
            {
                throw new SerializationException("zh-Hant catalog did not contain an entries array.");
            }

            foreach (CatalogEntry entry in document.Entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.Key))
                {
                    throw new SerializationException("zh-Hant catalog contains an entry with a blank key.");
                }

                if (!TraditionalEntries.TryAdd(entry.Key, entry.Text ?? string.Empty))
                {
                    throw new SerializationException($"Duplicate zh-Hant key: {entry.Key}");
                }
            }

            if (bytes.Length > 0 && TraditionalEntries.Count == 0)
            {
                throw new SerializationException("Non-empty zh-Hant catalog parsed to zero translations.");
            }

        }
    }

    internal static class ChineseTextConverter
    {
        private const uint LcmapSimplifiedChinese = 0x02000000;
        private const string SimplifiedChineseLocale = "zh-CN";

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int LCMapStringEx(
            string lpLocaleName,
            uint dwMapFlags,
            string lpSrcStr,
            int cchSrc,
            StringBuilder lpDestStr,
            int cchDest,
            IntPtr lpVersionInformation,
            IntPtr lpReserved,
            IntPtr sortHandle);

        public static string ToSimplified(string source)
        {
            if (string.IsNullOrEmpty(source))
            {
                return source;
            }

            int required = LCMapStringEx(
                SimplifiedChineseLocale,
                LcmapSimplifiedChinese,
                source,
                source.Length,
                null,
                0,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero);

            if (required <= 0)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            var buffer = new StringBuilder(required);
            int written = LCMapStringEx(
                SimplifiedChineseLocale,
                LcmapSimplifiedChinese,
                source,
                source.Length,
                buffer,
                buffer.Capacity,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero);

            if (written <= 0)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            return buffer.ToString();
        }
    }
}
