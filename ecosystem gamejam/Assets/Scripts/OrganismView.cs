using UnityEngine;
using UnityEngine.UI;

public class OrganismView : MonoBehaviour
{
    private RectTransform rectTransform;
    private Image bodyImage;
    private Image healthFill;
    private Text label;
    private Vector2 basePosition;
    private float bobSeed;

    public SpeciesType Species { get; private set; }
    public float Health { get; private set; }
    public float ReproductionProgress { get; set; }

    public void Initialize(
        SpeciesType species,
        string displayName,
        Color color,
        Sprite sprite,
        RectTransform parent,
        Vector2 anchoredPosition)
    {
        Species = species;
        Health = 1f;
        basePosition = anchoredPosition;
        bobSeed = Random.Range(0f, 10f);

        rectTransform = gameObject.AddComponent<RectTransform>();
        rectTransform.SetParent(parent, false);
        rectTransform.sizeDelta = new Vector2(52f, 52f);
        rectTransform.anchoredPosition = anchoredPosition;

        bodyImage = gameObject.AddComponent<Image>();
        bodyImage.sprite = sprite;
        bodyImage.color = color;

        GameObject textObject = new GameObject("Label");
        RectTransform textRect = textObject.AddComponent<RectTransform>();
        textRect.SetParent(rectTransform, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        label = textObject.AddComponent<Text>();
        label.alignment = TextAnchor.MiddleCenter;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontStyle = FontStyle.Bold;
        label.text = displayName.Substring(0, 1);
        label.color = new Color(0.1f, 0.15f, 0.2f);
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = 12;
        label.resizeTextMaxSize = 24;

        GameObject healthRoot = new GameObject("HealthBar");
        RectTransform healthRootRect = healthRoot.AddComponent<RectTransform>();
        healthRootRect.SetParent(rectTransform, false);
        healthRootRect.anchorMin = new Vector2(0.1f, 0f);
        healthRootRect.anchorMax = new Vector2(0.9f, 0f);
        healthRootRect.pivot = new Vector2(0.5f, 0f);
        healthRootRect.anchoredPosition = new Vector2(0f, -8f);
        healthRootRect.sizeDelta = new Vector2(0f, 8f);

        Image barBack = healthRoot.AddComponent<Image>();
        barBack.sprite = sprite;
        barBack.color = new Color(0.15f, 0.2f, 0.25f, 0.85f);

        GameObject fillObject = new GameObject("Fill");
        RectTransform fillRect = fillObject.AddComponent<RectTransform>();
        fillRect.SetParent(healthRootRect, false);
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.offsetMin = new Vector2(1f, 1f);
        fillRect.offsetMax = new Vector2(-1f, -1f);
        fillRect.pivot = new Vector2(0f, 0.5f);

        healthFill = fillObject.AddComponent<Image>();
        healthFill.sprite = sprite;

        RefreshHealthVisual();
    }

    public void AdjustHealth(float delta)
    {
        Health = Mathf.Clamp01(Health + delta);
        RefreshHealthVisual();
    }

    public void SetBasePosition(Vector2 position)
    {
        basePosition = position;
    }

    public bool IsDead()
    {
        return Health <= 0.01f;
    }

    private void Update()
    {
        if (rectTransform == null)
        {
            return;
        }

        float bobX = Mathf.Sin((Time.unscaledTime * 0.9f) + bobSeed) * 6f;
        float bobY = Mathf.Cos((Time.unscaledTime * 1.3f) + bobSeed) * 4f;
        rectTransform.anchoredPosition = basePosition + new Vector2(bobX, bobY);
    }

    private void RefreshHealthVisual()
    {
        if (healthFill == null)
        {
            return;
        }

        RectTransform fillRect = healthFill.rectTransform;
        fillRect.localScale = new Vector3(Mathf.Clamp01(Health), 1f, 1f);

        if (Health > 0.66f)
        {
            healthFill.color = new Color(0.44f, 0.82f, 0.38f);
        }
        else if (Health > 0.33f)
        {
            healthFill.color = new Color(0.95f, 0.78f, 0.23f);
        }
        else
        {
            healthFill.color = new Color(0.92f, 0.35f, 0.31f);
        }
    }
}
