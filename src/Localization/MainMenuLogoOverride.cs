using System;
using System.IO;
using System.Reflection;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.UI;

namespace Combolands.Localization
{
    internal static class MainMenuLogoOverride
    {
        private const string OriginalSpriteName = "MainMenuLogo";
        private const string ReplacementSpriteName = "MainMenuLogo.zh-Hant";
        private const string ResourceName =
            "Combolands.Localization.Assets.MainMenuLogo.zh-Hant.png";

        // The authored MainMenuLogo sprite occupies only the upper portion of
        // its 440x275 Image. The localized artwork fills most of its texture,
        // so it needs a smaller, upper-centered RectTransform while active.
        private static readonly Vector2 LocalizedLogoSize = new Vector2(250f, 83f);
        private static readonly Vector2 LocalizedLogoAnchoredPosition =
            new Vector2(0f, 88f);

        private static ManualLogSource _log;
        private static Image _logoImage;
        private static Sprite _originalSprite;
        private static RectTransform _logoRect;
        private static Vector2 _originalSizeDelta;
        private static Vector2 _originalAnchoredPosition;
        private static bool _originalLayoutCaptured;
        private static Texture2D _replacementTexture;
        private static Sprite _replacementSprite;
        private static bool _loadAttempted;
        private static bool _loadFailureLogged;
        private static bool _lookupFailureLogged;

        internal static void Configure(ManualLogSource log)
        {
            _log = log;
        }

        internal static void Refresh()
        {
            try
            {
                Image logoImage = ResolveLogoImage();
                if (logoImage == null)
                {
                    if (!_lookupFailureLogged)
                    {
                        _lookupFailureLogged = true;
                        _log?.LogWarning(
                            $"Main-menu logo override skipped: no Image using sprite '{OriginalSpriteName}' was found.");
                    }

                    return;
                }

                _lookupFailureLogged = false;

                CaptureOriginalState(logoImage);

                if (LocalizationState.IsChinese)
                {
                    Sprite replacement = GetOrCreateReplacementSprite();
                    if (replacement != null)
                    {
                        ApplyLocalizedLayout();
                        logoImage.sprite = replacement;
                    }

                    return;
                }

                RestoreOriginalLayout();
                if (_originalSprite != null)
                {
                    logoImage.sprite = _originalSprite;
                }
            }
            catch (Exception ex)
            {
                _log?.LogWarning(
                    $"Main-menu logo override failed; keeping the current logo. " +
                    $"{ex.GetType().Name}: {ex.Message}");
            }
        }

        private static Image ResolveLogoImage()
        {
            if (_logoImage != null)
            {
                return _logoImage;
            }

            Image fallback = null;
            Image[] images = UnityEngine.Object.FindObjectsByType<Image>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            foreach (Image image in images)
            {
                if (image == null || image.sprite == null)
                {
                    continue;
                }

                string spriteName = image.sprite.name;
                if (!string.Equals(spriteName, OriginalSpriteName, StringComparison.Ordinal) &&
                    !string.Equals(spriteName, ReplacementSpriteName, StringComparison.Ordinal))
                {
                    continue;
                }

                if (string.Equals(image.gameObject.name, "Logo", StringComparison.Ordinal))
                {
                    _logoImage = image;
                    return _logoImage;
                }

                fallback ??= image;
            }

            _logoImage = fallback;
            return _logoImage;
        }

        private static void CaptureOriginalState(Image logoImage)
        {
            if (logoImage == null)
            {
                return;
            }

            if (logoImage.sprite != null &&
                string.Equals(
                    logoImage.sprite.name,
                    OriginalSpriteName,
                    StringComparison.Ordinal))
            {
                _originalSprite = logoImage.sprite;
            }

            RectTransform rect = logoImage.transform as RectTransform;
            if (rect == null)
            {
                return;
            }

            if (_logoRect != rect)
            {
                _logoRect = rect;
                _originalLayoutCaptured = false;
            }

            if (_originalLayoutCaptured)
            {
                return;
            }

            _originalSizeDelta = rect.sizeDelta;
            _originalAnchoredPosition = rect.anchoredPosition;
            _originalLayoutCaptured = true;
        }

        private static void ApplyLocalizedLayout()
        {
            if (!_originalLayoutCaptured || _logoRect == null)
            {
                return;
            }

            _logoRect.sizeDelta = LocalizedLogoSize;
            _logoRect.anchoredPosition = LocalizedLogoAnchoredPosition;
        }

        private static void RestoreOriginalLayout()
        {
            if (!_originalLayoutCaptured || _logoRect == null)
            {
                return;
            }

            _logoRect.sizeDelta = _originalSizeDelta;
            _logoRect.anchoredPosition = _originalAnchoredPosition;
        }

        private static Sprite GetOrCreateReplacementSprite()
        {
            if (_replacementSprite != null)
            {
                return _replacementSprite;
            }

            if (_loadAttempted)
            {
                return null;
            }

            _loadAttempted = true;

            try
            {
                byte[] pngBytes = ReadEmbeddedLogo();
                if (pngBytes == null || pngBytes.Length == 0)
                {
                    LogLoadFailureOnce(
                        $"Embedded resource '{ResourceName}' was not found or was empty.");
                    return null;
                }

                Texture2D texture = new Texture2D(2, 2);
                if (!ImageConversion.LoadImage(texture, pngBytes, true))
                {
                    UnityEngine.Object.Destroy(texture);
                    LogLoadFailureOnce("Unity failed to decode the embedded PNG.");
                    return null;
                }

                texture.name = ReplacementSpriteName + ".Texture";

                Sprite sprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f);
                sprite.name = ReplacementSpriteName;

                _replacementTexture = texture;
                _replacementSprite = sprite;
                return _replacementSprite;
            }
            catch (Exception ex)
            {
                LogLoadFailureOnce(
                    $"Could not create the localized logo sprite: {ex.GetType().Name}: {ex.Message}");
                return null;
            }
        }

        private static byte[] ReadEmbeddedLogo()
        {
            Assembly assembly = typeof(MainMenuLogoOverride).Assembly;
            using Stream stream = assembly.GetManifestResourceStream(ResourceName);
            if (stream == null)
            {
                return null;
            }

            using MemoryStream buffer = new MemoryStream();
            stream.CopyTo(buffer);
            return buffer.ToArray();
        }

        private static void LogLoadFailureOnce(string message)
        {
            if (_loadFailureLogged)
            {
                return;
            }

            _loadFailureLogged = true;
            _log?.LogWarning(
                $"Main-menu logo override disabled: {message} Original logo will remain in use.");
        }
    }
}
