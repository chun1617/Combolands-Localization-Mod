using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Library.Localization
{
    public class LocalizedStringAsset : Object
    {
        public string Key;
        public string GetText() => string.Empty;
    }

    public class LocalizedTextMeshProUGUI : MonoBehaviour
    {
        public void SetText() { }
    }
}

namespace Shared.UI
{
    public class SettingsMenu : MonoBehaviour
    {
        public void Show() { }
        public void PressApply() { }
        public void PressCancel() { }
    }
}

namespace UI.SettingsMenu
{
    public class SettingsGroup : MonoBehaviour
    {
        protected TextMeshProUGUI _label;
    }

    public class DropdownGroup : SettingsGroup
    {
        public TMP_Dropdown Dropdown { get; set; }
    }

    public class SliderGroup : SettingsGroup
    {
        public Slider Slider { get; set; }
    }
}
