using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class SetupSceneController : MonoBehaviour
{
    private static readonly Color BackgroundColor = new Color(0.05f, 0.1f, 0.12f, 1f);
    private static readonly Color GlowTopColor = new Color(0.19f, 0.36f, 0.35f, 0.28f);
    private static readonly Color GlowBottomColor = new Color(0.3f, 0.2f, 0.14f, 0.2f);
    private static readonly Color CardTint = new Color(0.96f, 0.99f, 0.97f, 0.98f);
    private static readonly Color ControlTint = new Color(0.92f, 0.98f, 0.96f, 0.97f);
    private static readonly Color SelectorTint = new Color(0.82f, 0.92f, 0.92f, 0.9f);
    private static readonly Color ValueTint = new Color(0.17f, 0.3f, 0.33f, 0.26f);
    private static readonly Color HeadingColor = new Color(0.11f, 0.21f, 0.22f, 1f);
    private static readonly Color BodyColor = new Color(0.26f, 0.37f, 0.39f, 1f);
    private static readonly Color DetailColor = new Color(0.18f, 0.28f, 0.29f, 1f);

    private readonly string[] difficulties = { "Easy", "Medium", "Hard" };
    private readonly string[] jars = { "Balanced", "HighNitrates", "SnailHeavy", "Overgrown", "Fragile" };
    private readonly string[] temperatures = { "Cold", "Warm", "Hot" };

    private readonly string[] difficultyDescs =
    {
        "More re-rolls, forgiving thresholds. Good for learning the game.",
        "The intended experience. Balanced pressure and rewards.",
        "Tight margins and fewer re-rolls. One wrong card can cascade."
    };

    private readonly string[] jarDescs =
    {
        "A healthy starting mix. Good for first runs.",
        "Elevated waste from the start. Manage nitrates quickly.",
        "Packed with snails. Algae will run low fast.",
        "Algae is thriving. Bloom risk is high from day one.",
        "Minimal populations. The ecosystem is already fragile."
    };

    private readonly string[] temperatureDescs =
    {
        "Slower algae growth. Fish are sluggish but hardier.",
        "A balanced water temperature.",
        "Rapid algae growth and higher nitrate production."
    };

    private int difficultyIndex = 1;
    private int jarIndex;
    private int temperatureLevel = 1;

    private TextMeshProUGUI difficultyValue;
    private TextMeshProUGUI jarValue;
    private TextMeshProUGUI temperatureValue;
    private TextMeshProUGUI difficultyDesc;
    private TextMeshProUGUI jarDesc;
    private TextMeshProUGUI temperatureDesc;

    private void Awake()
    {
        if (!Application.isPlaying)
        {
            if (!CanBuildEditorUi())
            {
                return;
            }

            BuildUi();
            return;
        }

        BuildUi();
        RefreshValues();
    }

    private void OnEnable()
    {
        if (!Application.isPlaying)
        {
            if (!CanBuildEditorUi())
            {
                return;
            }

            BuildUi();
        }
    }

    private void BuildUi()
    {
        Canvas canvas = FindNamedCanvas("SetupCanvas");
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("SetupCanvas", typeof(RectTransform));
            canvas = canvasObject.AddComponent<Canvas>();
        }

        ConfigureCanvas(canvas.gameObject);
        ClearChildren(canvas.transform);
        BuildUiContents(canvas.transform);
        RefreshValues();
    }

    private bool CanBuildEditorUi()
    {
        if (Application.isPlaying)
        {
            return false;
        }

        if (this == null || gameObject == null)
        {
            return false;
        }

#if UNITY_EDITOR
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            return false;
        }
#endif

        var scene = gameObject.scene;
        if (!scene.IsValid() || !scene.isLoaded)
        {
            return false;
        }

        return !string.IsNullOrEmpty(scene.path);
    }

    private void BuildUiContents(Transform parent)
    {
        GameObject background = Panel("Background", parent, BackgroundColor);
        Stretch(background.GetComponent<RectTransform>());
        background.GetComponent<Image>().raycastTarget = false;

        GameObject glowTop = Panel("GlowTop", parent, GlowTopColor);
        Place(glowTop.GetComponent<RectTransform>(), new Vector2(0.05f, 0.58f), new Vector2(0.95f, 1f), Vector2.zero, Vector2.zero);
        glowTop.GetComponent<Image>().raycastTarget = false;

        GameObject glowBottom = Panel("GlowBottom", parent, GlowBottomColor);
        Place(glowBottom.GetComponent<RectTransform>(), new Vector2(0.16f, 0f), new Vector2(0.84f, 0.28f), Vector2.zero, Vector2.zero);
        glowBottom.GetComponent<Image>().raycastTarget = false;

        GameObject card = Panel("Card", parent, CardTint);
        Place(card.GetComponent<RectTransform>(), new Vector2(0.19f, 0.08f), new Vector2(0.81f, 0.92f), Vector2.zero, Vector2.zero);
        UiThemeStyler.ApplyPanel(card.GetComponent<Image>(), ThemePanelKind.Large, CardTint);
        VerticalLayoutGroup rootLayout = card.AddComponent<VerticalLayoutGroup>();
        rootLayout.padding = new RectOffset(54, 54, 42, 42);
        rootLayout.spacing = 18;
        rootLayout.childAlignment = TextAnchor.UpperCenter;
        rootLayout.childControlWidth = true;
        rootLayout.childControlHeight = true;
        rootLayout.childForceExpandWidth = true;
        rootLayout.childForceExpandHeight = false;

        AddSetupBrandLogo(card.transform);

        GameObject header = CreateLayoutNode("Header", card.transform);
        SetLayout(header, preferredHeight: 150f);
        VerticalLayoutGroup headerLayout = header.AddComponent<VerticalLayoutGroup>();
        headerLayout.spacing = 10;
        headerLayout.childAlignment = TextAnchor.UpperCenter;
        headerLayout.childControlWidth = true;
        headerLayout.childControlHeight = true;
        headerLayout.childForceExpandWidth = true;
        headerLayout.childForceExpandHeight = false;

        TextMeshProUGUI title = Label("Title", header.transform, 54, FontStyles.Bold, TextAlignmentOptions.Center);
        SetLayout(title.gameObject, preferredHeight: 64f);
        title.text = "Glass World";
        title.enableAutoSizing = true;
        title.fontSizeMin = 36;
        title.fontSizeMax = 54;
        UiThemeStyler.ApplyTitle(title);

        TextMeshProUGUI subtitle = Label("Subtitle", header.transform, 20, FontStyles.Normal, TextAlignmentOptions.Center);
        SetLayout(subtitle.gameObject, preferredHeight: 58f);
        subtitle.text = "Shape the first few days of your jar, then step into a short, card-driven ecosystem run.";
        subtitle.color = BodyColor;
        subtitle.enableWordWrapping = true;

        GameObject selections = CreateLayoutNode("Selections", card.transform);
        SetLayout(selections, flexibleHeight: 1f);
        VerticalLayoutGroup selectionsLayout = selections.AddComponent<VerticalLayoutGroup>();
        selectionsLayout.spacing = 16;
        selectionsLayout.childAlignment = TextAnchor.UpperCenter;
        selectionsLayout.childControlWidth = true;
        selectionsLayout.childControlHeight = true;
        selectionsLayout.childForceExpandWidth = true;
        selectionsLayout.childForceExpandHeight = false;

        CreateControl(selections.transform, "Difficulty", ChangeDifficulty, out difficultyValue, out difficultyDesc);
        CreateControl(selections.transform, "Starting Jar", ChangeJar, out jarValue, out jarDesc);
        CreateControl(selections.transform, "Starting Temperature", ChangeTemperature, out temperatureValue, out temperatureDesc);

        GameObject footer = CreateLayoutNode("Footer", card.transform);
        SetLayout(footer, preferredHeight: 126f);
        VerticalLayoutGroup footerLayout = footer.AddComponent<VerticalLayoutGroup>();
        footerLayout.spacing = 14;
        footerLayout.childAlignment = TextAnchor.UpperCenter;
        footerLayout.childControlWidth = true;
        footerLayout.childControlHeight = true;
        footerLayout.childForceExpandWidth = true;
        footerLayout.childForceExpandHeight = false;

        TextMeshProUGUI hint = Label("Hint", footer.transform, 18, FontStyles.Normal, TextAlignmentOptions.Center);
        SetLayout(hint.gameObject, preferredHeight: 34f);
        hint.text = "Medium + Balanced + Warm is the smoothest place to start.";
        hint.color = DetailColor;

        Button playButton = CreateUiButton("Start Game", footer.transform, new Color(0.71f, 0.9f, 0.8f, 1f), StartGame);
        SetLayout(playButton.gameObject, preferredWidth: 280f, preferredHeight: 64f);
    }

    private void AddSetupBrandLogo(Transform parent)
    {
        GameObject logoPlate = Panel("BrandLogoPlate", parent, new Color(0.95f, 0.99f, 0.97f, 0.9f));
        LayoutElement layout = logoPlate.AddComponent<LayoutElement>();
        layout.ignoreLayout = true;
        Place(logoPlate.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-182f, -22f), new Vector2(-28f, -96f));
        UiThemeStyler.ApplyPanel(logoPlate.GetComponent<Image>(), ThemePanelKind.Small, new Color(0.95f, 0.99f, 0.97f, 0.9f));

        Outline outline = logoPlate.AddComponent<Outline>();
        outline.effectColor = new Color(0.12f, 0.22f, 0.23f, 0.16f);
        outline.effectDistance = new Vector2(2f, -2f);

        Image logoImage = BrandLogoUtility.CreateLogoImage("BrandLogo", logoPlate.transform);
        if (logoImage == null)
        {
            if (Application.isPlaying)
            {
                Destroy(logoPlate);
            }
            else
            {
                DestroyImmediate(logoPlate);
            }

            return;
        }

        Place(logoImage.rectTransform, Vector2.zero, Vector2.one, new Vector2(10f, 8f), new Vector2(-10f, -8f));
    }

    private void ChangeDifficulty(int delta)
    {
        difficultyIndex = Mathf.Clamp(difficultyIndex + delta, 0, difficulties.Length - 1);
        RefreshValues();
    }

    private void ChangeJar(int delta)
    {
        jarIndex = Mathf.Clamp(jarIndex + delta, 0, jars.Length - 1);
        RefreshValues();
    }

    private void ChangeTemperature(int delta)
    {
        temperatureLevel = Mathf.Clamp(temperatureLevel + delta, 0, temperatures.Length - 1);
        RefreshValues();
    }

    private void RefreshValues()
    {
        if (difficultyValue != null)
        {
            difficultyValue.text = difficulties[difficultyIndex];
        }

        if (jarValue != null)
        {
            jarValue.text = jars[jarIndex];
        }

        if (temperatureValue != null)
        {
            temperatureValue.text = temperatures[temperatureLevel];
        }

        if (difficultyDesc != null)
        {
            difficultyDesc.text = difficultyDescs[difficultyIndex];
        }

        if (jarDesc != null)
        {
            jarDesc.text = jarDescs[jarIndex];
        }

        if (temperatureDesc != null)
        {
            temperatureDesc.text = temperatureDescs[temperatureLevel];
        }
    }

    private void StartGame()
    {
        GameSettingsStore.Save(difficultyIndex, jarIndex, temperatureLevel);
        SceneManager.LoadScene("SampleScene");
    }

    private void CreateControl(Transform parent, string label, Action<int> onAdjust, out TextMeshProUGUI valueText, out TextMeshProUGUI descText)
    {
        GameObject box = Panel(label + "Box", parent, ControlTint);
        UiThemeStyler.ApplyPanel(box.GetComponent<Image>(), ThemePanelKind.Medium, ControlTint);
        SetLayout(box, preferredHeight: 136f);

        HorizontalLayoutGroup boxLayout = box.AddComponent<HorizontalLayoutGroup>();
        boxLayout.padding = new RectOffset(24, 24, 18, 18);
        boxLayout.spacing = 20;
        boxLayout.childAlignment = TextAnchor.MiddleCenter;
        boxLayout.childControlWidth = true;
        boxLayout.childControlHeight = true;
        boxLayout.childForceExpandWidth = false;
        boxLayout.childForceExpandHeight = false;

        GameObject info = CreateLayoutNode(label + "Info", box.transform);
        SetLayout(info, flexibleWidth: 1f, minWidth: 300f);
        VerticalLayoutGroup infoLayout = info.AddComponent<VerticalLayoutGroup>();
        infoLayout.spacing = 8;
        infoLayout.childAlignment = TextAnchor.UpperLeft;
        infoLayout.childControlWidth = true;
        infoLayout.childControlHeight = true;
        infoLayout.childForceExpandWidth = true;
        infoLayout.childForceExpandHeight = false;

        TextMeshProUGUI labelText = Label(label + "Label", info.transform, 24, FontStyles.Bold, TextAlignmentOptions.Left);
        SetLayout(labelText.gameObject, preferredHeight: 30f);
        labelText.text = label;
        labelText.color = HeadingColor;

        descText = Label(label + "Desc", info.transform, 15, FontStyles.Normal, TextAlignmentOptions.Left);
        SetLayout(descText.gameObject, preferredHeight: 44f);
        descText.color = BodyColor;
        descText.enableWordWrapping = true;

        GameObject selector = Panel(label + "Selector", box.transform, SelectorTint);
        UiThemeStyler.ApplyPanel(selector.GetComponent<Image>(), ThemePanelKind.Small, SelectorTint);
        SetLayout(selector, preferredWidth: 344f, preferredHeight: 76f);

        HorizontalLayoutGroup selectorLayout = selector.AddComponent<HorizontalLayoutGroup>();
        selectorLayout.padding = new RectOffset(14, 14, 12, 12);
        selectorLayout.spacing = 12;
        selectorLayout.childAlignment = TextAnchor.MiddleCenter;
        selectorLayout.childControlWidth = true;
        selectorLayout.childControlHeight = true;
        selectorLayout.childForceExpandWidth = false;
        selectorLayout.childForceExpandHeight = false;

        Button minus = CreateUiButton("-", selector.transform, new Color(0.96f, 0.83f, 0.78f, 1f), delegate { onAdjust(-1); });
        SetLayout(minus.gameObject, preferredWidth: 68f, preferredHeight: 52f);

        GameObject valuePlate = Panel(label + "ValuePlate", selector.transform, ValueTint);
        valuePlate.GetComponent<Image>().raycastTarget = false;
        SetLayout(valuePlate, flexibleWidth: 1f, preferredHeight: 52f);

        valueText = Label(label + "Value", valuePlate.transform, 20, FontStyles.Bold, TextAlignmentOptions.Center);
        Stretch(valueText.rectTransform);
        valueText.color = DetailColor;

        Button plus = CreateUiButton("+", selector.transform, new Color(0.8f, 0.92f, 0.9f, 1f), delegate { onAdjust(1); });
        SetLayout(plus.gameObject, preferredWidth: 68f, preferredHeight: 52f);
    }

    private static void ConfigureCanvas(GameObject canvasObject)
    {
        canvasObject.name = "SetupCanvas";

        Canvas canvas = canvasObject.GetComponent<Canvas>() ?? canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>() ?? canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        if (canvasObject.GetComponent<GraphicRaycaster>() == null)
        {
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        RectTransform rect = canvasObject.GetComponent<RectTransform>();
        if (rect != null)
        {
            Stretch(rect);
            rect.localScale = Vector3.one;
            rect.anchoredPosition = Vector2.zero;
        }
    }

    private static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            GameObject child = parent.GetChild(i).gameObject;
            if (Application.isPlaying)
            {
                Destroy(child);
            }
            else
            {
                DestroyImmediate(child);
            }
        }
    }

    private static GameObject CreateLayoutNode(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void SetLayout(GameObject target, float preferredWidth = -1f, float preferredHeight = -1f, float flexibleWidth = -1f, float flexibleHeight = -1f, float minWidth = -1f, float minHeight = -1f)
    {
        LayoutElement layout = target.GetComponent<LayoutElement>() ?? target.AddComponent<LayoutElement>();
        if (preferredWidth >= 0f)
        {
            layout.preferredWidth = preferredWidth;
        }

        if (preferredHeight >= 0f)
        {
            layout.preferredHeight = preferredHeight;
        }

        if (flexibleWidth >= 0f)
        {
            layout.flexibleWidth = flexibleWidth;
        }

        if (flexibleHeight >= 0f)
        {
            layout.flexibleHeight = flexibleHeight;
        }

        if (minWidth >= 0f)
        {
            layout.minWidth = minWidth;
        }

        if (minHeight >= 0f)
        {
            layout.minHeight = minHeight;
        }
    }

    private static GameObject Panel(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.color = color;
        return go;
    }

    private static Button CreateUiButton(string text, Transform parent, Color color, UnityEngine.Events.UnityAction onClick)
    {
        GameObject go = CreateButtonObject(text, parent, color);
        Button button = go.GetComponent<Button>() ?? go.AddComponent<Button>();
        button.targetGraphic = go.GetComponent<Image>();

        ColorBlock colors = button.colors;
        colors.highlightedColor = Color.Lerp(color, Color.white, 0.12f);
        colors.pressedColor = Color.Lerp(color, Color.black, 0.08f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(onClick);

        TextMeshProUGUI label = Label(text + "Text", go.transform, 20, FontStyles.Bold, TextAlignmentOptions.Center);
        label.text = text;
        label.color = DetailColor;
        Stretch(label.rectTransform);
        UiThemeStyler.ApplyButton(button, GetButtonKind(text), label);
        return button;
    }

    private static GameObject CreateButtonObject(string text, Transform parent, Color color)
    {
        return Panel(text + "Button", parent, color);
    }

    private static ThemeButtonKind GetButtonKind(string text)
    {
        if (text == "Start Game")
        {
            return ThemeButtonKind.Start;
        }

        return text == "-" ? ThemeButtonKind.Danger : ThemeButtonKind.Secondary;
    }

    private static TextMeshProUGUI Label(string name, Transform parent, int size, FontStyles style, TextAlignmentOptions anchor)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        text.font = TmpFontUtility.GetFont();
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = anchor;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Overflow;
        return text;
    }

    private static Canvas FindNamedCanvas(string targetName)
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i] != null && canvases[i].gameObject.name == targetName)
            {
                return canvases[i];
            }
        }

        return null;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void Place(RectTransform rt, Vector2 min, Vector2 max, Vector2 offMin, Vector2 offMax)
    {
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = offMin;
        rt.offsetMax = offMax;
    }
}
