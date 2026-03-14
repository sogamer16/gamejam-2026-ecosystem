using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SetupSceneController : MonoBehaviour
{
    private readonly string[] difficulties = { "Easy", "Normal", "Hard" };
    private readonly string[] jars = { "Balanced", "HighNitrates", "SnailHeavy", "Overgrown", "Fragile" };
    private readonly string[] temperatures = { "Cold", "Warm", "Hot" };

    private int difficultyIndex = 1;
    private int jarIndex = 0;
    private int temperatureLevel = 1;

    private Text difficultyValue;
    private Text jarValue;
    private Text temperatureValue;

    private void Awake()
    {
        BuildUi();
        RefreshValues();
    }

    private void BuildUi()
    {
        GameObject canvasObject = new GameObject("SetupCanvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject bg = Panel("Background", canvas.transform, new Color(0.06f, 0.11f, 0.14f, 1f));
        Stretch(bg.GetComponent<RectTransform>());

        GameObject card = Panel("Card", canvas.transform, new Color(0.9f, 0.95f, 0.97f, 0.97f));
        Place(card.GetComponent<RectTransform>(), new Vector2(0.2f, 0.1f), new Vector2(0.8f, 0.9f), Vector2.zero, Vector2.zero);

        Text title = Label("Title", card.transform, 54, FontStyle.Bold, TextAnchor.UpperCenter);
        Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(40f, -40f), new Vector2(-40f, -110f));
        title.text = "Glass World";
        title.color = new Color(0.12f, 0.18f, 0.22f);

        Text subtitle = Label("Subtitle", card.transform, 22, FontStyle.Normal, TextAnchor.UpperCenter);
        Place(subtitle.rectTransform, new Vector2(0.1f, 1f), new Vector2(0.9f, 1f), new Vector2(0f, -118f), new Vector2(0f, -176f));
        subtitle.text = "Choose your starting settings before entering the card-driven jar.";
        subtitle.color = new Color(0.23f, 0.33f, 0.4f);

        float top = -220f;
        CreateControl(card.transform, "Difficulty", "How forgiving the ecosystem is.", top, ChangeDifficulty, out difficultyValue);
        top -= 110f;
        CreateControl(card.transform, "Starting Jar", "Pick the opening ecosystem condition.", top, ChangeJar, out jarValue);
        top -= 110f;
        CreateControl(card.transform, "Starting Temperature", "Initial water temperature.", top, ChangeTemperature, out temperatureValue);

        Text hint = Label("Hint", card.transform, 18, FontStyle.Normal, TextAnchor.MiddleCenter);
        Place(hint.rectTransform, new Vector2(0.1f, 0f), new Vector2(0.9f, 0f), new Vector2(0f, 134f), new Vector2(0f, 198f));
        hint.text = "Balanced + Normal is the recommended first card run.";
        hint.color = new Color(0.22f, 0.32f, 0.39f);

        Button playButton = CreateUiButton("Start Game", card.transform, new Color(0.36f, 0.78f, 0.54f), StartGame);
        Place(playButton.GetComponent<RectTransform>(), new Vector2(0.35f, 0f), new Vector2(0.65f, 0f), new Vector2(0f, 44f), new Vector2(0f, 108f));
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

    private void CreateControl(Transform parent, string label, string hint, float top, Action<int> onAdjust, out Text valueText)
    {
        GameObject box = Panel(label + "Box", parent, new Color(0.79f, 0.87f, 0.91f, 0.75f));
        Place(box.GetComponent<RectTransform>(), new Vector2(0.1f, 1f), new Vector2(0.9f, 1f), new Vector2(0f, top), new Vector2(0f, top - 90f));

        Text labelText = Label(label + "Label", box.transform, 22, FontStyle.Bold, TextAnchor.UpperLeft);
        Place(labelText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(18f, -10f), new Vector2(-18f, -36f));
        labelText.text = label;
        labelText.color = new Color(0.11f, 0.17f, 0.22f);

        Text hintText = Label(label + "Hint", box.transform, 14, FontStyle.Normal, TextAnchor.UpperLeft);
        Place(hintText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(18f, -34f), new Vector2(-18f, -54f));
        hintText.text = hint;
        hintText.color = new Color(0.24f, 0.33f, 0.39f);

        Button minus = CreateUiButton("-", box.transform, new Color(0.91f, 0.61f, 0.4f), delegate { onAdjust(-1); });
        Place(minus.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(18f, 10f), new Vector2(86f, 48f));

        valueText = Label(label + "Value", box.transform, 22, FontStyle.Bold, TextAnchor.MiddleCenter);
        Place(valueText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(96f, 8f), new Vector2(-96f, 50f));
        valueText.color = new Color(0.11f, 0.17f, 0.22f);

        Button plus = CreateUiButton("+", box.transform, new Color(0.42f, 0.76f, 0.95f), delegate { onAdjust(1); });
        Place(plus.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-86f, 10f), new Vector2(-18f, 48f));
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
        GameObject go = Panel(text + "Button", parent, color);
        Button button = go.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = Color.Lerp(color, Color.white, 0.18f);
        colors.pressedColor = Color.Lerp(color, Color.black, 0.12f);
        button.colors = colors;
        button.onClick.AddListener(onClick);
        Text label = Label(text + "Text", go.transform, 20, FontStyle.Bold, TextAnchor.MiddleCenter);
        label.text = text;
        label.color = new Color(0.11f, 0.15f, 0.19f);
        Stretch(label.rectTransform);
        return button;
    }

    private static Text Label(string name, Transform parent, int size, FontStyle style, TextAnchor anchor)
    {
        GameObject go = new GameObject(name);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        Text text = go.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = anchor;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
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
