using TMPro;
using UnityEngine;

public static class TmpFontUtility
{
    private static TMP_FontAsset cachedFont;

    public static TMP_FontAsset GetFont()
    {
        if (cachedFont != null)
        {
            return cachedFont;
        }

        if (TMP_Settings.defaultFontAsset != null)
        {
            cachedFont = TMP_Settings.defaultFontAsset;
            return cachedFont;
        }

        Font builtinFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (builtinFont != null)
        {
            cachedFont = TMP_FontAsset.CreateFontAsset(builtinFont);
        }

        return cachedFont;
    }
}
