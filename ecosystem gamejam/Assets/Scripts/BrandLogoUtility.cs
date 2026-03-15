using UnityEngine;
using UnityEngine.UI;

public static class BrandLogoUtility
{
    private static Sprite cachedLogo;
    private static Texture2D cachedTexture;
    private const byte TransparentThreshold = 20;

    public static Sprite GetLogoSprite()
    {
        if (cachedLogo == null)
        {
            cachedLogo = CreateProcessedLogo("CardArt/logo");
        }

        if (cachedLogo == null)
        {
            cachedLogo = Resources.Load<Sprite>("Brand/JarboundLogo");
        }

        if (cachedLogo == null)
        {
            cachedLogo = CreateProcessedLogo("Brand/JarboundLogo");
        }

        return cachedLogo;
    }

    private static Sprite CreateProcessedLogo(string resourcePath)
    {
        cachedTexture = Resources.Load<Texture2D>(resourcePath);
        if (cachedTexture == null)
        {
            return null;
        }

        try
        {
            Color32[] pixels = cachedTexture.GetPixels32();
            Texture2D cleaned = new Texture2D(cachedTexture.width, cachedTexture.height, TextureFormat.RGBA32, false);
            Color32[] output = new Color32[pixels.Length];
            for (int i = 0; i < pixels.Length; i++)
            {
                Color32 pixel = pixels[i];
                bool nearBlack = pixel.r <= TransparentThreshold && pixel.g <= TransparentThreshold && pixel.b <= TransparentThreshold;
                output[i] = nearBlack ? new Color32(0, 0, 0, 0) : pixel;
            }

            cleaned.SetPixels32(output);
            cleaned.Apply();
            return Sprite.Create(cleaned, new Rect(0f, 0f, cleaned.width, cleaned.height), new Vector2(0.5f, 0.5f), 100f);
        }
        catch
        {
            return Resources.Load<Sprite>(resourcePath);
        }
    }

    public static Image CreateLogoImage(string name, Transform parent)
    {
        Sprite logo = GetLogoSprite();
        if (logo == null)
        {
            return null;
        }

        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);

        Image image = go.AddComponent<Image>();
        image.sprite = logo;
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.color = Color.white;
        return image;
    }
}
