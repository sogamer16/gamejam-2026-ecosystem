using System.Collections.Generic;
using System.Reflection;
using NueGames.NueDeck.Scripts.Card;
using NueGames.NueDeck.Scripts.Enums;
using NueGames.NueDeck.Scripts.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EcosystemCardPrefabView : MonoBehaviour
{
    private static readonly FieldInfo NameTextFieldInfo = typeof(CardBase).GetField("nameTextField", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo DescTextFieldInfo = typeof(CardBase).GetField("descTextField", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo ManaTextFieldInfo = typeof(CardBase).GetField("manaTextField", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo CardImageInfo = typeof(CardBase).GetField("cardImage", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo PassiveImageInfo = typeof(CardBase).GetField("passiveImage", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo RarityRootListInfo = typeof(CardBase).GetField("rarityRootList", BindingFlags.Instance | BindingFlags.NonPublic);

    private CardUI cardUi;
    private RectTransform cachedRect;
    private Button selectButton;
    private TextMeshProUGUI buttonLabel;
    private TextMeshProUGUI categoryLabel;
    private TextMeshProUGUI summaryLabel;
    private Image inputSurface;
    private Image shirtFrameImage;
    private Image accentGlowImage;
    private Image shineImage;

    public RectTransform RectTransform => cachedRect;
    public Button SelectButton => selectButton;
    public Image ShineImage => shineImage;
    public bool HasTemplateCard => cardUi != null;

    public void Initialize()
    {
        if (cachedRect != null)
        {
            return;
        }

        cachedRect = transform as RectTransform;
        cardUi = GetComponent<CardUI>();
        if (cardUi == null)
        {
            cardUi = GetComponentInChildren<CardUI>(true);
        }
        if (cardUi == null)
        {
            EnsureTemplateCard();
            cardUi = GetComponent<CardUI>();
            if (cardUi == null)
            {
                cardUi = GetComponentInChildren<CardUI>(true);
            }
        }

        Canvas embeddedCanvas = GetComponent<Canvas>();
        if (embeddedCanvas != null)
        {
            embeddedCanvas.overrideSorting = false;
            embeddedCanvas.pixelPerfect = false;
        }

        inputSurface = gameObject.GetComponent<Image>();
        if (inputSurface == null)
        {
            inputSurface = gameObject.AddComponent<Image>();
        }

        inputSurface.color = new Color(1f, 1f, 1f, 0.01f);
        inputSurface.raycastTarget = true;

        selectButton = gameObject.GetComponent<Button>();
        if (selectButton == null)
        {
            selectButton = gameObject.AddComponent<Button>();
        }

        selectButton.targetGraphic = inputSurface;
        ColorBlock colors = selectButton.colors;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.08f);
        colors.pressedColor = new Color(1f, 1f, 1f, 0.12f);
        colors.selectedColor = colors.highlightedColor;
        selectButton.colors = colors;

        CreateOverlays();
    }

    private void EnsureTemplateCard()
    {
        GameObject templatePrefab = Resources.Load<GameObject>("EcosystemCards/CardUI");
        if (templatePrefab == null)
        {
            return;
        }

        GameObject templateInstance = Instantiate(templatePrefab, transform);
        templateInstance.name = "CardTemplate";
        RectTransform templateRect = templateInstance.GetComponent<RectTransform>();
        if (templateRect != null)
        {
            templateRect.anchorMin = Vector2.zero;
            templateRect.anchorMax = Vector2.one;
            templateRect.offsetMin = Vector2.zero;
            templateRect.offsetMax = Vector2.zero;
            templateRect.localScale = Vector3.one;
            templateRect.anchoredPosition = Vector2.zero;
        }
    }

    public void SetCard(string title, string summary, string category, int requiredRoll, bool risk, Color accentColor, Sprite artSprite, Sprite shirtSprite, RarityType rarity, bool selected, bool interactable)
    {
        Initialize();

        TextMeshProUGUI nameText = ReadField<TextMeshProUGUI>(NameTextFieldInfo);
        TextMeshProUGUI descText = ReadField<TextMeshProUGUI>(DescTextFieldInfo);
        TextMeshProUGUI manaText = ReadField<TextMeshProUGUI>(ManaTextFieldInfo);
        Image cardImage = ReadField<Image>(CardImageInfo);
        Image passiveImage = ReadField<Image>(PassiveImageInfo);
        List<RarityRoot> rarityRoots = RarityRootListInfo != null ? RarityRootListInfo.GetValue(cardUi) as List<RarityRoot> : null;

        if (nameText != null)
        {
            nameText.text = title;
            nameText.color = Color.black;
            nameText.fontSize = 30f;
            nameText.enableAutoSizing = true;
            nameText.fontSizeMin = 19f;
            nameText.fontSizeMax = 30f;
            nameText.textWrappingMode = TextWrappingModes.Normal;
            nameText.overflowMode = TextOverflowModes.Ellipsis;
        }

        if (descText != null)
        {
            descText.text = summary;
            descText.color = Color.black;
            descText.fontSize = 26f;
            descText.enableAutoSizing = true;
            descText.fontSizeMin = 15f;
            descText.fontSizeMax = 26f;
            descText.textWrappingMode = TextWrappingModes.Normal;
            descText.overflowMode = TextOverflowModes.Ellipsis;
            descText.lineSpacing = 1f;
            RectTransform descRect = descText.rectTransform;
            descRect.offsetMin = new Vector2(descRect.offsetMin.x, Mathf.Max(descRect.offsetMin.y, 88f));
        }

        if (manaText != null)
        {
            manaText.text = requiredRoll.ToString();
            manaText.color = interactable ? new Color(0.96f, 0.95f, 0.9f) : new Color(0.96f, 0.72f, 0.72f);
        }

        if (cardImage != null)
        {
            cardImage.sprite = artSprite;
            cardImage.preserveAspect = artSprite != null;
            cardImage.color = artSprite != null ? new Color(1f, 1f, 1f, 0.96f) : new Color(1f, 1f, 1f, 0f);
            cardImage.gameObject.SetActive(artSprite != null);
        }

        if (passiveImage != null)
        {
            passiveImage.gameObject.SetActive(selected);
            passiveImage.color = new Color(accentColor.r, accentColor.g, accentColor.b, selected ? 0.16f : 0f);
        }

        if (rarityRoots != null)
        {
            for (int i = 0; i < rarityRoots.Count; i++)
            {
                if (rarityRoots[i] != null)
                {
                    rarityRoots[i].gameObject.SetActive(rarityRoots[i].Rarity == rarity);
                }
            }
        }

        if (shirtFrameImage != null)
        {
            shirtFrameImage.sprite = shirtSprite;
            shirtFrameImage.color = Color.Lerp(accentColor, Color.white, 0.25f);
        }

        if (accentGlowImage != null)
        {
            accentGlowImage.color = Color.Lerp(accentColor, Color.white, selected ? 0.18f : 0.48f);
        }

        if (categoryLabel != null)
        {
            categoryLabel.text = category.ToUpperInvariant() + (risk ? "  RISK" : string.Empty);
            categoryLabel.color = Color.black;
        }

        if (summaryLabel != null)
        {
            summaryLabel.text = summary;
            summaryLabel.color = Color.black;
        }

        if (buttonLabel != null)
        {
            buttonLabel.text = selected ? "Selected" : "Play";
            buttonLabel.color = Color.black;
        }

        if (selectButton != null)
        {
            selectButton.interactable = interactable;
        }

        ConfigureFoilMaterial(accentColor, artSprite, shirtSprite, risk, selected);
    }

    private void CreateOverlays()
    {
        accentGlowImage = CreateOverlayImage("AccentGlow", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(1f, 1f, 1f, 0.08f));
        accentGlowImage.transform.SetAsFirstSibling();

        shirtFrameImage = CreateOverlayImage("ShirtFrame", new Vector2(0.08f, 0.1f), new Vector2(0.92f, 0.9f), Vector2.zero, Vector2.zero, Color.white);
        shirtFrameImage.preserveAspect = false;
        shirtFrameImage.transform.SetAsFirstSibling();

        shineImage = CreateOverlayImage("FoilShine", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(42f, 0f), new Color(1f, 1f, 1f, 0.08f));
        shineImage.rectTransform.pivot = new Vector2(0f, 0.5f);
        shineImage.transform.SetAsLastSibling();

        categoryLabel = CreateLabel("CategoryLabel", 14, FontStyles.Bold, TextAlignmentOptions.TopLeft);
        categoryLabel.textWrappingMode = TextWrappingModes.Normal;
        categoryLabel.rectTransform.anchorMin = new Vector2(0f, 1f);
        categoryLabel.rectTransform.anchorMax = new Vector2(1f, 1f);
        categoryLabel.rectTransform.offsetMin = new Vector2(26f, -60f);
        categoryLabel.rectTransform.offsetMax = new Vector2(-26f, -16f);

        summaryLabel = CreateLabel("SummaryLabel", 13, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        summaryLabel.enableAutoSizing = true;
        summaryLabel.fontSizeMin = 10f;
        summaryLabel.fontSizeMax = 13f;
        summaryLabel.textWrappingMode = TextWrappingModes.Normal;
        summaryLabel.overflowMode = TextOverflowModes.Ellipsis;
        summaryLabel.lineSpacing = -8f;
        summaryLabel.rectTransform.anchorMin = new Vector2(0f, 0f);
        summaryLabel.rectTransform.anchorMax = new Vector2(1f, 0f);
        summaryLabel.rectTransform.offsetMin = new Vector2(28f, 58f);
        summaryLabel.rectTransform.offsetMax = new Vector2(-28f, 116f);

        GameObject buttonObject = new GameObject("PlayChip", typeof(RectTransform), typeof(Image));
        buttonObject.transform.SetParent(transform, false);
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0f);
        buttonRect.anchorMax = new Vector2(0.5f, 0f);
        buttonRect.pivot = new Vector2(0.5f, 0f);
        buttonRect.sizeDelta = new Vector2(152f, 38f);
        buttonRect.anchoredPosition = new Vector2(0f, 14f);
        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = new Color(0.93f, 0.84f, 0.63f, 0.95f);
        buttonImage.raycastTarget = false;

        buttonLabel = CreateLabel("PlayChipLabel", 16, FontStyles.Bold, TextAlignmentOptions.Center);
        buttonLabel.transform.SetParent(buttonObject.transform, false);
        RectTransform labelRect = buttonLabel.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
    }

    private Image CreateOverlayImage(string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color color)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(transform, false);
        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        RectTransform rect = image.rectTransform;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        return image;
    }

    private TextMeshProUGUI CreateLabel(string name, int size, FontStyles style, TextAlignmentOptions alignment)
    {
        GameObject labelObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(transform, false);
        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.font = TmpFontUtility.GetFont();
        label.fontSize = size;
        label.fontStyle = style;
        label.alignment = alignment;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.raycastTarget = false;
        return label;
    }

    private T ReadField<T>(FieldInfo fieldInfo) where T : class
    {
        if (cardUi == null || fieldInfo == null)
        {
            return null;
        }

        return fieldInfo.GetValue(cardUi) as T;
    }

    private void ConfigureFoilMaterial(Color accentColor, Sprite artSprite, Sprite shirtSprite, bool risk, bool selected)
    {
        if (shineImage == null || shineImage.material == null)
        {
            return;
        }

        Material material = shineImage.material;
        Texture patternTexture = shirtSprite != null ? shirtSprite.texture : Texture2D.whiteTexture;
        Texture maskTexture = artSprite != null ? artSprite.texture : patternTexture;
        material.SetTexture("_PatternTex", patternTexture);
        material.SetTexture("_MaskTex", maskTexture);
        material.SetColor("_BaseColor", new Color(1f, 1f, 1f, selected ? 0.18f : 0.12f));
        material.SetColor("_FoilColorA", Color.Lerp(accentColor, Color.cyan, 0.35f));
        material.SetColor("_FoilColorB", risk ? new Color(1f, 0.34f, 0.56f, 1f) : Color.Lerp(accentColor, new Color(0.85f, 0.45f, 1f), 0.55f));
        material.SetColor("_FoilColorC", risk ? new Color(1f, 0.87f, 0.34f, 1f) : Color.Lerp(accentColor, new Color(1f, 0.92f, 0.42f), 0.45f));
        material.SetFloat("_Strength", selected ? 0.42f : 0.3f);
        material.SetFloat("_PatternStrength", risk ? 0.92f : 0.7f);
        material.SetFloat("_RainbowStrength", risk ? 0.82f : 0.62f);
        material.SetFloat("_FresnelPower", risk ? 2.4f : 3.1f);
        material.SetVector("_PatternTiling", risk ? new Vector4(4.2f, 5.4f, 0f, 0f) : new Vector4(3.0f, 4.0f, 0f, 0f));
        material.SetVector("_PatternScroll", risk ? new Vector4(0.16f, 0.09f, 0f, 0f) : new Vector4(0.08f, 0.16f, 0f, 0f));
    }
}
