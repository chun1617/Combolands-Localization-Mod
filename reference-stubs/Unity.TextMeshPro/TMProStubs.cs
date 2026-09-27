using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TMPro
{
    public enum AtlasPopulationMode
    {
        Static = 0,
        Dynamic = 1
    }

    public enum TextAlignmentOptions
    {
        MidlineLeft = 0
    }

    public enum HorizontalAlignmentOptions
    {
        Left = 0
    }

    public class TMP_FontAsset : Object
    {
        public AtlasPopulationMode atlasPopulationMode { get; set; }
        public bool isMultiAtlasTexturesEnabled { get; set; }
        public List<TMP_FontAsset> fallbackFontAssetTable { get; set; }
        public static TMP_FontAsset CreateFontAsset(string familyName, string styleName, int pointSize) => new TMP_FontAsset();
        public bool TryAddCharacters(string characters, out string missingCharacters, bool includeFontFeatures = false)
        {
            missingCharacters = string.Empty;
            return true;
        }
    }

    public static class TMP_Settings
    {
        public static List<TMP_FontAsset> fallbackFontAssets { get; } = new List<TMP_FontAsset>();
    }

    public struct TMP_CharacterInfo
    {
        public bool isVisible;
        public Vector3 bottomLeft;
        public Vector3 topLeft;
    }

    public class TMP_TextInfo
    {
        public int characterCount;
        public TMP_CharacterInfo[] characterInfo = new TMP_CharacterInfo[0];
    }

    public class TMP_Text : Graphic
    {
        public string text { get; set; }
        public TMP_FontAsset font { get; set; }
        public bool havePropertiesChanged { get; set; }
        public bool enableAutoSizing { get; set; }
        public float fontSize { get; set; }
        public float fontSizeMin { get; set; }
        public float fontSizeMax { get; set; }
        public Vector4 margin { get; set; }
        public TextAlignmentOptions alignment { get; set; }
        public HorizontalAlignmentOptions horizontalAlignment { get; set; }
        public RectTransform rectTransform { get; }
        public TMP_TextInfo textInfo { get; } = new TMP_TextInfo();
        public void ForceMeshUpdate(bool ignoreActiveState = false, bool forceTextReparsing = false) { }
    }

    public class TextMeshProUGUI : TMP_Text { }

    public class TMP_Dropdown : Selectable
    {
        public class DropdownEvent : UnityEvent<int> { }

        public class OptionData
        {
            public OptionData(string text) { this.text = text; }
            public string text { get; set; }
        }

        public DropdownEvent onValueChanged { get; set; }
        public int value { get; set; }
        public void ClearOptions() { }
        public void AddOptions(List<OptionData> options) { }
        public void SetValueWithoutNotify(int input) { }
        public void RefreshShownValue() { }
    }
}
