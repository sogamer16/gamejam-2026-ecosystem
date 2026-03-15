using UnityEngine;
using UnityEngine.UI;

public static class BrandLogoUtility
{
    private static Sprite cachedLogo;
    private static Texture2D cachedTexture;

    public static Sprite GetLogoSprite()
    {
        if (cachedLogo == null)
        {
            cachedLogo = Resources.Load<Sprite>("Brand/JarboundLogo");
        }

        if (cachedLogo == null)
        {
            cachedTexture = Resources.Load<Texture2D>("Brand/JarboundLogo");
            if (cachedTexture != null)
            {
                cachedLogo = Sprite.Create(cachedTexture, new Rect(0f, 0f, cachedTexture.width, cachedTexture.height), new Vector2(0.5f, 0.5f), 100f);
            }
        }

        return cachedLogo;
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
