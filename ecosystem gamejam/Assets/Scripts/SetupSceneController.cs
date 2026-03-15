using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class SetupSceneController : MonoBehaviour
{
    private readonly string[] difficulties = { "Easy", "Medium", "Hard" };
    private readonly string[] jars = { "Balanced", "HighNitrates", "SnailHeavy", "Overgrown", "Fragile" };
    private readonly string[] temperatures = { "Cold", "Warm", "Hot" };

    private int difficultyIndex = 1;
    private int jarIndex = 0;
    private int temperatureLevel = 1;

    private TextMeshProUGUI difficultyValue;
    private TextMeshProUGUI jarValue;
    private TextMeshProUGUI temperatureValue;

    private void Awake()
    {
        if (!Application.isPlaying)
        {
            if (!CanBuildEditorUi()) return;
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
            if (!CanBuildEditorUi()) return;
            BuildUi();
        }
    }

    private void BuildUi()
    {
        Canvas existingCanvas = FindNamedCanvas("SetupCanvas");
        if (existingCanvas != null)
        {
            BindExistingUi(existingCanvas.gameObject);
            return;
        }

        GameObject canvasObject = new GameObject("SetupCanvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject bg = Panel("Background", canvas.transform, new Color(0.06f, 0.11f, 0.14f, 1f));
        Stretch(bg.GetComponent<RectTransform>());

        GameObject card = Panel("Card", canvas.transform, new Color(1f, 1f, 1f, 1f));
        Place(card.GetComponent<RectTransform>(), new Vector2(0.28f, 0.1f), new Vector2(0.72f, 0.9f), Vector2.zero, Vector2.zero);
        UiThemeStyler.ApplyPanel(card.GetComponent<Image>(), ThemePanelKind.Large, new Color(1f, 1f, 1f, 0.98f));

        TextMeshProUGUI title = Label("Title", card.transform, 48, FontStyles.Bold, TextAlignmentOptions.Top);
        Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(32f, -36f), new Vector2(-32f, -94f));
        title.text = "Glass World";
        UiThemeStyler.ApplyTitle(title);

        TextMeshProUGUI subtitle = Label("Subtitle", card.transform, 18, FontStyles.Normal, TextAlignmentOptions.Top);
        Place(subtitle.rectTransform, new Vector2(0.08f, 1f), new Vector2(0.92f, 1f), new Vector2(0f, -104f), new Vector2(0f, -150f));
        subtitle.text = "Choose your starting settings before entering the card-driven jar.";
        subtitle.color = new Color(0.23f, 0.33f, 0.4f);

        float top = -188f;
        CreateControl(card.transform, "Difficulty", "How forgiving the ecosystem is.", top, ChangeDifficulty, out difficultyValue);
        top -= 126f;
        CreateControl(card.transform, "Starting Jar", "Pick the opening ecosystem condition.", top, ChangeJar, out jarValue);
        top -= 126f;
        CreateControl(card.transform, "Starting Temperature", "Initial water temperature.", top, ChangeTemperature, out temperatureValue);

        TextMeshProUGUI hint = Label("Hint", card.transform, 18, FontStyles.Normal, TextAlignmentOptions.Center);
        Place(hint.rectTransform, new Vector2(0.1f, 0f), new Vector2(0.9f, 0f), new Vector2(0f, 132f), new Vector2(0f, 178f));
        hint.text = "Medium is the intended default experience.";
        hint.color = new Color(0.22f, 0.32f, 0.39f);

        Button playButton = CreateUiButton("Start Game", card.transform, new Color(0.36f, 0.78f, 0.54f), StartGame);
        Place(playButton.GetComponent<RectTransform>(), new Vector2(0.28f, 0f), new Vector2(0.72f, 0f), new Vector2(0f, 40f), new Vector2(0f, 104f));
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

    private void BindExistingUi(GameObject canvasObject)
    {
        ApplyThemeToExistingUi(canvasObject.transform);
        difficultyValue = FindText(canvasObject.transform, "DifficultyValue");
        jarValue = FindText(canvasObject.transform, "Starting JarValue");
        if (jarValue == null) jarValue = FindText(canvasObject.transform, "StartingJarValue");
        temperatureValue = FindText(canvasObject.transform, "Starting TemperatureValue");
        if (temperatureValue == null) temperatureValue = FindText(canvasObject.transform, "StartingTemperatureValue");
        RebindButton(canvasObject.transform, "-Button", () => ChangeDifficulty(-1), 0);
        RebindButton(canvasObject.transform, "+Button", () => ChangeDifficulty(1), 0);
        RebindButton(canvasObject.transform, "-Button", () => ChangeJar(-1), 1);
        RebindButton(canvasObject.transform, "+Button", () => ChangeJar(1), 1);
        RebindButton(canvasObject.transform, "-Button", () => ChangeTemperature(-1), 2);
        RebindButton(canvasObject.transform, "+Button", () => ChangeTemperature(1), 2);
        RebindButton(canvasObject.transform, "Start GameButton", StartGame);
        if (difficultyValue != null && jarValue != null && temperatureValue != null)
        {
            RefreshValues();
        }
    }

    private void ChangeDifficulty(int delta) { difficultyIndex = Mathf.Clamp(difficultyIndex + delta, 0, difficulties.Length - 1); RefreshValues(); }
    private void ChangeJar(int delta) { jarIndex = Mathf.Clamp(jarIndex + delta, 0, jars.Length - 1); RefreshValues(); }
    private void ChangeTemperature(int delta) { temperatureLevel = Mathf.Clamp(temperatureLevel + delta, 0, temperatures.Length - 1); RefreshValues(); }

    private void RefreshValues()
    {
        difficultyValue.text = difficulties[difficultyIndex];
        jarValue.text = jars[jarIndex];
        temperatureValue.text = temperatures[temperatureLevel];
    }

    private void StartGame()
    {
        GameSettingsStore.Save(difficultyIndex, jarIndex, temperatureLevel);
        SceneManager.LoadScene("SampleScene");
    }

    private void CreateControl(Transform parent, string label, string hint, float top, Action<int> onAdjust, out TextMeshProUGUI valueText)
    {
        GameObject box = Panel(label + "Box", parent, new Color(1f, 1f, 1f, 1f));
        Place(box.GetComponent<RectTransform>(), new Vector2(0.08f, 1f), new Vector2(0.92f, 1f), new Vector2(0f, top), new Vector2(0f, top - 104f));
        UiThemeStyler.ApplyPanel(box.GetComponent<Image>(), ThemePanelKind.Medium, new Color(1f, 1f, 1f, 0.96f));

        TextMeshProUGUI labelText = Label(label + "Label", box.transform, 22, FontStyles.Bold, TextAlignmentOptions.TopLeft);
        Place(labelText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(18f, -12f), new Vector2(-18f, -40f));
        labelText.text = label;
        labelText.color = new Color(0.11f, 0.17f, 0.22f);

        TextMeshProUGUI hintText = Label(label + "Hint", box.transform, 14, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        Place(hintText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(18f, -40f), new Vector2(-18f, -62f));
        hintText.text = hint;
        hintText.color = new Color(0.24f, 0.33f, 0.39f);

        Button minus = CreateUiButton("-", box.transform, new Color(0.91f, 0.61f, 0.4f), delegate { onAdjust(-1); });
        Place(minus.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(18f, 14f), new Vector2(78f, 56f));

        valueText = Label(label + "Value", box.transform, 22, FontStyles.Bold, TextAlignmentOptions.Center);
        Place(valueText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(92f, 8f), new Vector2(-92f, 58f));
        valueText.color = new Color(0.11f, 0.17f, 0.22f);

        Button plus = CreateUiButton("+", box.transform, new Color(0.42f, 0.76f, 0.95f), delegate { onAdjust(1); });
        Place(plus.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-78f, 14f), new Vector2(-18f, 56f));
    }

    private static GameObject Panel(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.color = color;
        return go;
    }

    private static Button CreateUiButton(string text, Transform parent, Color color, UnityEngine.Events.UnityAction onClick)
    {
        GameObject go = CreateButtonObject(text, parent, color);
        Button button = go.GetComponent<Button>() ?? go.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = Color.Lerp(color, Color.white, 0.18f);
        colors.pressedColor = Color.Lerp(color, Color.black, 0.12f);
        button.colors = colors;
        button.onClick.AddListener(onClick);
        TextMeshProUGUI label = Label(text + "Text", go.transform, 20, FontStyles.Bold, TextAlignmentOptions.Center);
        label.text = text;
        label.color = new Color(0.11f, 0.15f, 0.19f);
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

    private static void ApplyThemeToExistingUi(Transform root)
    {
        ApplyPanelTheme(root, "Card", ThemePanelKind.Large, new Color(1f, 1f, 1f, 0.98f));
        ApplyPanelTheme(root, "DifficultyBox", ThemePanelKind.Medium, new Color(1f, 1f, 1f, 0.96f));
        ApplyPanelTheme(root, "Starting JarBox", ThemePanelKind.Medium, new Color(1f, 1f, 1f, 0.96f));
        ApplyPanelTheme(root, "Starting TemperatureBox", ThemePanelKind.Medium, new Color(1f, 1f, 1f, 0.96f));
        ApplyButtonTheme(root, "Start GameButton", ThemeButtonKind.Start);
        ApplyButtonTheme(root, "-Button", ThemeButtonKind.Danger, 0);
        ApplyButtonTheme(root, "+Button", ThemeButtonKind.Secondary, 0);
        ApplyButtonTheme(root, "-Button", ThemeButtonKind.Danger, 1);
        ApplyButtonTheme(root, "+Button", ThemeButtonKind.Secondary, 1);
        ApplyButtonTheme(root, "-Button", ThemeButtonKind.Danger, 2);
        ApplyButtonTheme(root, "+Button", ThemeButtonKind.Secondary, 2);
    }

    private static void ApplyPanelTheme(Transform root, string name, ThemePanelKind kind, Color tint)
    {
        Transform match = FindChildRecursive(root, name);
        if (match == null)
        {
            return;
        }

        Image image = match.GetComponent<Image>();
        if (image != null)
        {
            UiThemeStyler.ApplyPanel(image, kind, tint);
        }
    }

    private static void ApplyButtonTheme(Transform root, string name, ThemeButtonKind kind, int matchIndex = -1)
    {
        Button[] buttons = root.GetComponentsInChildren<Button>(true);
        int seen = 0;
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i].name != name)
            {
                continue;
            }

            if (matchIndex >= 0 && seen++ != matchIndex)
            {
                continue;
            }

            TMP_Text label = buttons[i].GetComponentInChildren<TMP_Text>(true);
            UiThemeStyler.ApplyButton(buttons[i], kind, label);
            if (matchIndex >= 0)
            {
                return;
            }
        }
    }

    private static TextMeshProUGUI Label(string name, Transform parent, int size, FontStyles style, TextAlignmentOptions anchor)
    {
        GameObject go = new GameObject(name);
        RectTransform rt = go.AddComponent<RectTransform>();
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

    private static TextMeshProUGUI FindText(Transform parent, string name)
    {
        Transform match = FindChildRecursive(parent, name);
        return match != null ? match.GetComponent<TextMeshProUGUI>() : null;
    }

    private static Transform FindChildRecursive(Transform parent, string name)
    {
        if (parent.name == name)
        {
            return parent;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform match = FindChildRecursive(parent.GetChild(i), name);
            if (match != null)
            {
                return match;
            }
        }

        return null;
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

    private void RebindButton(Transform root, string name, UnityEngine.Events.UnityAction action, int occurrence = 0)
    {
        Button button = FindButton(root, name, occurrence);
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    private static Button FindButton(Transform root, string name, int occurrence = 0)
    {
        int seen = 0;
        return FindButtonRecursive(root, name, occurrence, ref seen);
    }

    private static Button FindButtonRecursive(Transform parent, string name, int occurrence, ref int seen)
    {
        if (parent.name == name)
        {
            Button button = parent.GetComponent<Button>();
            if (button != null)
            {
                if (seen == occurrence)
                {
                    return button;
                }

                seen++;
            }
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Button result = FindButtonRecursive(parent.GetChild(i), name, occurrence, ref seen);
            if (result != null)
            {
                return result;
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
