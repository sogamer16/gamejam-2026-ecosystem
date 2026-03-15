using UnityEngine;
using UnityEngine.UI;

public static class BrandLogoUtility
{
    private static Sprite cachedLogo;

    public static Sprite GetLogoSprite()
    {
        if (cachedLogo == null)
        {
            cachedLogo = Resources.Load<Sprite>("Brand/JarboundLogo");
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
