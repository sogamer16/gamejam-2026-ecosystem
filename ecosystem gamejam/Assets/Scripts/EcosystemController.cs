using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum SpeciesType { Algae, Snail, Fish, Shrimp }

public class EcosystemController : MonoBehaviour
{
    private enum GameState { Menu, Playing, Result }
    private enum DifficultyMode { Easy, Normal, Hard }
    private enum StartingJar { Balanced, HighNitrates, SnailHeavy, Overgrown, Fragile }
    private enum FishTrait { Balanced, Hungry, Lazy, Fragile }
    private enum CardCategory { Fish, Snail, Algae, Light, Water, Risk }

    private sealed class SpeciesDef { public string Name; public Color Color; }
    private sealed class CardDef { public string Name; public string Summary; public CardCategory Category; public Color Color; public bool Risk; public Action<TurnState> Apply; }
    private sealed class EventDef { public string Name; public string Summary; public Action<TurnState> Apply; }
    private sealed class TurnState
    {
        public int LightBonus;
        public float FeedBonus;
        public float AppetiteMultiplier = 1f;
        public float NitrateBonus;
        public int AlgaeBonus;
        public int AddFish;
        public int RemoveFish;
        public int AddSnail;
        public int RemoveSnail;
        public float FishHealthBonus;
        public int ExtraFishGraze;
        public int FilterDays;
        public int WasteDays;
        public readonly List<string> Notes = new List<string>();
    }

    private readonly Dictionary<SpeciesType, SpeciesDef> defs = new Dictionary<SpeciesType, SpeciesDef>();
    private readonly List<OrganismView> organisms = new List<OrganismView>();
    private readonly Dictionary<OrganismView, FishTrait> fishTraits = new Dictionary<OrganismView, FishTrait>();
    private readonly List<CardDef> deckTemplate = new List<CardDef>();
    private readonly List<CardDef> drawPile = new List<CardDef>();
    private readonly List<CardDef> discardPile = new List<CardDef>();
    private readonly List<CardDef> hand = new List<CardDef>();
    private readonly List<CardDef> selected = new List<CardDef>();
    private readonly List<Button> cardButtons = new List<Button>();
    private readonly List<Text> cardTitles = new List<Text>();
    private readonly List<Text> cardBodies = new List<Text>();

    private GameState state = GameState.Menu;
    private DifficultyMode difficulty = DifficultyMode.Normal;
    private StartingJar startingJar = StartingJar.Balanced;

    private Sprite whiteSprite;
    private Canvas canvas;
    private RectTransform jarArea;
    private Image water;
    private Image lightGlow;
    private GameObject menuPanel;
    private GameObject resultPanel;
    private Text bannerText;
    private Text statsText;
    private Text warningText;
    private Text eventText;
    private Text reportText;
    private Text selectedText;
    private Text deckText;
    private Text speciesText;
    private Text resultText;
    private Text tooltipText;
    private GameObject tooltipPanel;

    private int day;
    private int stableDays;
    private int perfectDays;
    private int temperatureLevel;
    private int filterDaysRemaining;
    private int wasteDaysRemaining;
    private int highNitrateDays;
    private float nitrateLevel;
    private float bloomThreshold;
    private float stability;
    private float algaeMemory;
    private float bloomFlash;
    private string latestWarnings;
    private string dayReport;
    private string latestMilestone;
    private string jarName;
    private EventDef currentEvent;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        defs[SpeciesType.Algae] = new SpeciesDef { Name = "Algae", Color = new Color(0.43f, 0.79f, 0.36f) };
        defs[SpeciesType.Snail] = new SpeciesDef { Name = "Snail", Color = new Color(0.92f, 0.73f, 0.45f) };
        defs[SpeciesType.Fish] = new SpeciesDef { Name = "Fish", Color = new Color(0.42f, 0.72f, 0.96f) };
        defs[SpeciesType.Shrimp] = new SpeciesDef { Name = "Shrimp", Color = new Color(0.96f, 0.56f, 0.52f) };
        BuildDeckTemplate();
        BuildVisuals();
        ShowMenu();
    }

    private void OnDestroy() { SceneManager.sceneLoaded -= OnSceneLoaded; }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) { if (canvas == null) BuildVisuals(); }

    private void Update()
    {
        if (state == GameState.Playing && Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) TickDay();
        UpdateWater();
    }

    private void BuildVisuals()
    {
        whiteSprite = CreateWhiteSprite();
        if (FindFirstObjectByType<Canvas>() != null && FindFirstObjectByType<Canvas>().gameObject.name == "EcosystemCanvas") return;

        GameObject c = new GameObject("EcosystemCanvas");
        canvas = c.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = c.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        c.AddComponent<GraphicRaycaster>();

        GameObject bg = Panel("BG", canvas.transform, new Color(0.94f, 0.95f, 0.9f));
        Stretch(bg.GetComponent<RectTransform>());

        GameObject left = Panel("Left", canvas.transform, new Color(0.1f, 0.17f, 0.15f, 0.97f));
        Place(left.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0.28f, 1f), Vector2.zero, Vector2.zero);
        Text title = Label("Title", left.transform, 38, FontStyle.Bold, TextAnchor.UpperLeft);
        Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -24f), new Vector2(-24f, -82f));
        title.text = "Glass World";
        title.color = Color.white;
        Text desc = Label("Desc", left.transform, 17, FontStyle.Normal, TextAnchor.UpperLeft);
        Place(desc.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -84f), new Vector2(-24f, -144f));
        desc.text = "Keep 3 cards in hand. Play 1 each day, then draw 1 replacement.";
        desc.color = new Color(0.83f, 0.91f, 0.87f);

        statsText = Label("Stats", left.transform, 18, FontStyle.Bold, TextAnchor.UpperLeft);
        Place(statsText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -156f), new Vector2(-24f, -260f));
        statsText.color = Color.white;
        warningText = Label("Warnings", left.transform, 15, FontStyle.Bold, TextAnchor.UpperLeft);
        Place(warningText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -264f), new Vector2(-24f, -342f));
        warningText.color = new Color(0.98f, 0.84f, 0.4f);
        eventText = Label("Event", left.transform, 14, FontStyle.Normal, TextAnchor.UpperLeft);
        Place(eventText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -346f), new Vector2(-24f, -410f));
        eventText.color = new Color(0.84f, 0.9f, 0.95f);
        selectedText = Label("Selected", left.transform, 14, FontStyle.Normal, TextAnchor.UpperLeft);
        Place(selectedText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -414f), new Vector2(-24f, -492f));
        selectedText.color = new Color(0.88f, 0.95f, 0.9f);
        reportText = Label("Report", left.transform, 14, FontStyle.Normal, TextAnchor.UpperLeft);
        Place(reportText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(24f, 180f), new Vector2(-24f, 24f));
        reportText.color = new Color(0.84f, 0.92f, 0.88f);

        Button next = CreateUiButton("Play Selected Card", left.transform, new Color(0.38f, 0.74f, 0.51f), TickDay);
        Place(next.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(24f, 230f), new Vector2(-24f, 290f));
        Button restart = CreateUiButton("Restart Run", left.transform, new Color(0.86f, 0.42f, 0.34f), StartGame);
        Place(restart.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(24f, 302f), new Vector2(-24f, 362f));

        GameObject right = Panel("Right", canvas.transform, new Color(0.2f, 0.3f, 0.24f, 0.08f));
        Place(right.GetComponent<RectTransform>(), new Vector2(0.3f, 0.04f), new Vector2(0.98f, 0.97f), Vector2.zero, Vector2.zero);
        Outline o = right.AddComponent<Outline>();
        o.effectColor = new Color(0.16f, 0.28f, 0.22f, 0.8f);
        o.effectDistance = new Vector2(4f, 4f);

        bannerText = Label("Banner", right.transform, 28, FontStyle.Bold, TextAnchor.UpperCenter);
        Place(bannerText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -18f), new Vector2(-24f, -70f));
        bannerText.color = new Color(0.13f, 0.2f, 0.16f);
        deckText = Label("Deck", right.transform, 17, FontStyle.Bold, TextAnchor.UpperLeft);
        Place(deckText.rectTransform, new Vector2(0f, 1f), new Vector2(0.35f, 1f), new Vector2(24f, -76f), new Vector2(-6f, -128f));
        deckText.color = new Color(0.14f, 0.2f, 0.17f);
        speciesText = Label("Species", right.transform, 17, FontStyle.Bold, TextAnchor.UpperRight);
        Place(speciesText.rectTransform, new Vector2(0.45f, 1f), new Vector2(1f, 1f), new Vector2(6f, -76f), new Vector2(-24f, -154f));
        speciesText.color = new Color(0.14f, 0.2f, 0.17f);

        GameObject jar = Panel("Jar", right.transform, new Color(0.81f, 0.92f, 0.95f, 0.18f));
        Place(jar.GetComponent<RectTransform>(), new Vector2(0.08f, 0.22f), new Vector2(0.92f, 0.78f), Vector2.zero, Vector2.zero);
        Outline jarOutline = jar.AddComponent<Outline>();
        jarOutline.effectColor = new Color(0.18f, 0.32f, 0.27f, 0.8f);
        jarOutline.effectDistance = new Vector2(4f, 4f);
        water = Panel("Water", jar.transform, new Color(0.62f, 0.86f, 0.96f, 0.5f)).GetComponent<Image>();
        Place(water.rectTransform, new Vector2(0.05f, 0.12f), new Vector2(0.95f, 0.9f), Vector2.zero, Vector2.zero);
        lightGlow = Panel("LightGlow", jar.transform, new Color(1f, 0.97f, 0.74f, 0.16f)).GetComponent<Image>();
        Place(lightGlow.rectTransform, new Vector2(0.08f, 0.72f), new Vector2(0.92f, 0.95f), Vector2.zero, Vector2.zero);
        GameObject layer = new GameObject("Organisms");
        jarArea = layer.AddComponent<RectTransform>();
        jarArea.SetParent(jar.transform, false);
        Place(jarArea, new Vector2(0.08f, 0.14f), new Vector2(0.92f, 0.88f), Vector2.zero, Vector2.zero);

        Text handLabel = Label("HandLabel", right.transform, 24, FontStyle.Bold, TextAnchor.UpperLeft);
        Place(handLabel.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(24f, 210f), new Vector2(-24f, 248f));
        handLabel.text = "Today's Hand";
        handLabel.color = new Color(0.13f, 0.2f, 0.16f);
        CreateCardSlots(right.transform);

        tooltipPanel = Panel("Tooltip", canvas.transform, new Color(0.05f, 0.09f, 0.1f, 0.92f));
        Place(tooltipPanel.GetComponent<RectTransform>(), new Vector2(0.68f, 0.02f), new Vector2(0.96f, 0.12f), Vector2.zero, Vector2.zero);
        tooltipText = Label("TooltipText", tooltipPanel.transform, 15, FontStyle.Normal, TextAnchor.MiddleCenter);
        Stretch(tooltipText.rectTransform);
        tooltipText.color = Color.white;
        tooltipPanel.SetActive(false);

        menuPanel = Panel("Menu", canvas.transform, new Color(0.05f, 0.09f, 0.1f, 0.85f));
        Stretch(menuPanel.GetComponent<RectTransform>());
        Text mt = Label("MenuTitle", menuPanel.transform, 56, FontStyle.Bold, TextAnchor.MiddleCenter);
        Place(mt.rectTransform, new Vector2(0.18f, 0.56f), new Vector2(0.82f, 0.8f), Vector2.zero, Vector2.zero);
        mt.text = "Glass World";
        mt.color = Color.white;
        Text mb = Label("MenuBody", menuPanel.transform, 24, FontStyle.Normal, TextAnchor.MiddleCenter);
        Place(mb.rectTransform, new Vector2(0.16f, 0.36f), new Vector2(0.84f, 0.56f), Vector2.zero, Vector2.zero);
        mb.text = "Keep a hand of 3 cards. Play 1 each day, draw 1 replacement, and adapt to the jar.";
        mb.color = new Color(0.87f, 0.93f, 0.9f);
        Button play = CreateUiButton("Start Prototype", menuPanel.transform, new Color(0.39f, 0.78f, 0.54f), StartGame);
        Place(play.GetComponent<RectTransform>(), new Vector2(0.39f, 0.22f), new Vector2(0.61f, 0.31f), Vector2.zero, Vector2.zero);

        resultPanel = Panel("Result", canvas.transform, new Color(0.05f, 0.09f, 0.1f, 0.85f));
        Stretch(resultPanel.GetComponent<RectTransform>());
        resultText = Label("ResultText", resultPanel.transform, 44, FontStyle.Bold, TextAnchor.MiddleCenter);
        Place(resultText.rectTransform, new Vector2(0.14f, 0.38f), new Vector2(0.86f, 0.7f), Vector2.zero, Vector2.zero);
        resultText.color = Color.white;
        Button again = CreateUiButton("Play Again", resultPanel.transform, new Color(0.42f, 0.75f, 0.94f), StartGame);
        Place(again.GetComponent<RectTransform>(), new Vector2(0.4f, 0.22f), new Vector2(0.6f, 0.3f), Vector2.zero, Vector2.zero);
        resultPanel.SetActive(false);
    }

    private void CreateCardSlots(Transform parent)
    {
        for (int i = 0; i < 3; i++)
        {
            float start = 0.04f + (i * 0.31f);
            GameObject slot = Panel("Card" + i, parent, new Color(0.97f, 0.95f, 0.9f, 0.98f));
            Place(slot.GetComponent<RectTransform>(), new Vector2(start, 0f), new Vector2(start + 0.28f, 0f), new Vector2(0f, 26f), new Vector2(0f, 194f));
            Outline outline = slot.AddComponent<Outline>();
            outline.effectColor = new Color(0.13f, 0.22f, 0.18f, 0.8f);
            outline.effectDistance = new Vector2(2f, 2f);

            Text t = Label("Title", slot.transform, 21, FontStyle.Bold, TextAnchor.UpperLeft);
            Place(t.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, -14f), new Vector2(-16f, -44f));
            t.color = new Color(0.12f, 0.18f, 0.15f);
            cardTitles.Add(t);

            Text body = Label("Body", slot.transform, 14, FontStyle.Normal, TextAnchor.UpperLeft);
            Place(body.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, -50f), new Vector2(-16f, -156f));
            body.color = new Color(0.2f, 0.28f, 0.24f);
            cardBodies.Add(body);

            Button b = CreateUiButton("Select", slot.transform, new Color(0.38f, 0.74f, 0.51f), delegate { });
            Place(b.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(16f, 14f), new Vector2(-16f, 56f));
            int index = i;
            b.onClick.RemoveAllListeners();
            b.onClick.AddListener(delegate { ToggleCard(index); });
            cardButtons.Add(b);
        }
    }

    private void ShowMenu()
    {
        state = GameState.Menu;
        menuPanel.SetActive(true);
        resultPanel.SetActive(false);
        jarName = "Your Jar";
        latestWarnings = "No warnings yet.";
        dayReport = "Draw cards, commit to a plan, and let the ecosystem react.";
        currentEvent = null;
        RefreshHud();
        if (GameSettingsStore.HasSelection) StartGame();
    }

    private void StartGame()
    {
        menuPanel.SetActive(false);
        resultPanel.SetActive(false);
        state = GameState.Playing;
        difficulty = GameSettingsStore.HasSelection ? (DifficultyMode)GameSettingsStore.DifficultyIndex : DifficultyMode.Normal;
        startingJar = GameSettingsStore.HasSelection ? (StartingJar)GameSettingsStore.StartingJarIndex : StartingJar.Balanced;
        temperatureLevel = GameSettingsStore.HasSelection ? GameSettingsStore.TemperatureLevel : 1;
        day = 1;
        stableDays = 0;
        perfectDays = 0;
        nitrateLevel = 5f;
        bloomThreshold = 14f;
        stability = 70f;
        filterDaysRemaining = 0;
        wasteDaysRemaining = 0;
        highNitrateDays = 0;
        algaeMemory = 0f;
        bloomFlash = 0f;
        latestMilestone = "No milestones yet.";
        jarName = "Jar-" + UnityEngine.Random.Range(10, 99);
        ClearOrganisms();
        BuildDeck();
        ApplyStartingJar();
        PrepareDay();
    }

    private void BuildDeckTemplate()
    {
        deckTemplate.Clear();
        AddCard("Feed Fish", "Fish are fully fed today.\nSmall nitrate increase.", CardCategory.Fish, new Color(0.58f, 0.83f, 0.95f), false, t => { t.FeedBonus += 1.4f; t.NitrateBonus += 1.2f; t.Notes.Add("Feed Fish kept the fish satisfied."); });
        AddCard("Underfeed Fish", "Fish eat algae today.\nRepeated use is risky.", CardCategory.Fish, new Color(0.63f, 0.8f, 0.92f), false, t => { t.FeedBonus -= 0.9f; t.ExtraFishGraze += 2; t.FishHealthBonus -= 0.06f; t.Notes.Add("Underfeeding pushed fish to graze algae."); });
        AddCard("Overfeed Fish", "Fish health improves.\nHuge nitrate increase.", CardCategory.Risk, new Color(0.97f, 0.66f, 0.43f), true, t => { t.FeedBonus += 2.2f; t.NitrateBonus += 3.6f; t.FishHealthBonus += 0.14f; t.Notes.Add("Overfeeding created a heavy nitrate spike."); });
        AddCard("Add Fish", "Add 1 fish.\nRaises long-term nitrates.", CardCategory.Fish, new Color(0.54f, 0.75f, 0.94f), false, t => { t.AddFish += 1; });
        AddCard("Remove Fish", "Remove 1 fish.\nLowers waste output.", CardCategory.Fish, new Color(0.72f, 0.83f, 0.96f), false, t => { t.RemoveFish += 1; });
        AddCard("Add Snail", "Add 1 snail.", CardCategory.Snail, new Color(0.93f, 0.79f, 0.49f), false, t => { t.AddSnail += 1; });
        AddCard("Snail Eggs", "Add 2 snails.", CardCategory.Snail, new Color(0.95f, 0.83f, 0.54f), false, t => { t.AddSnail += 2; });
        AddCard("Snail Loss", "Remove 1 snail.", CardCategory.Snail, new Color(0.84f, 0.68f, 0.45f), false, t => { t.RemoveSnail += 1; });
        AddCard("Remove Algae", "Remove 20 algae.", CardCategory.Algae, new Color(0.54f, 0.82f, 0.46f), false, t => { t.AlgaeBonus -= 20; });
        AddCard("Algae Growth", "Increase algae moderately.", CardCategory.Algae, new Color(0.61f, 0.83f, 0.51f), false, t => { t.AlgaeBonus += 3; });
        AddCard("Algae Surge", "Large algae increase.", CardCategory.Risk, new Color(0.41f, 0.76f, 0.33f), true, t => { t.AlgaeBonus += 6; t.LightBonus += 1; });
        AddCard("Algae Die-Off", "Large algae decrease.", CardCategory.Algae, new Color(0.72f, 0.89f, 0.66f), false, t => { t.AlgaeBonus -= 5; });
        AddCard("Balanced Growth", "Algae grows slowly and safely.", CardCategory.Algae, new Color(0.7f, 0.88f, 0.62f), false, t => { t.AlgaeBonus += 1; t.NitrateBonus -= 0.6f; });
        AddCard("Increase Light", "Boost algae growth.", CardCategory.Light, new Color(0.98f, 0.89f, 0.55f), false, t => { t.LightBonus += 1; });
        AddCard("Dim Light", "Reduce algae growth.", CardCategory.Light, new Color(0.8f, 0.83f, 0.65f), false, t => { t.LightBonus -= 1; });
        AddCard("Sunny Day", "Strong algae growth this turn.", CardCategory.Light, new Color(0.99f, 0.83f, 0.39f), false, t => { t.LightBonus += 2; t.AlgaeBonus += 2; });
        AddCard("Cloudy Day", "Algae growth is reduced this turn.", CardCategory.Light, new Color(0.77f, 0.81f, 0.83f), false, t => { t.LightBonus -= 2; });
        AddCard("Clean Water", "Reduce nitrates significantly.", CardCategory.Water, new Color(0.66f, 0.92f, 0.95f), false, t => { t.NitrateBonus -= 4.5f; });
        AddCard("Partial Water Change", "Reduce nitrates and algae slightly.", CardCategory.Water, new Color(0.72f, 0.94f, 0.93f), false, t => { t.NitrateBonus -= 2.3f; t.AlgaeBonus -= 2; });
        AddCard("Nutrient Spike", "Increase nitrates significantly.", CardCategory.Risk, new Color(0.93f, 0.6f, 0.44f), true, t => { t.NitrateBonus += 4.5f; });
        AddCard("Filter System", "Reduce nitrates for the next 2 days.", CardCategory.Water, new Color(0.62f, 0.88f, 0.88f), false, t => { t.FilterDays = Mathf.Max(t.FilterDays, 2); });
        AddCard("Waste Build-Up", "Nitrates rise slowly for 2 days.", CardCategory.Risk, new Color(0.88f, 0.62f, 0.5f), true, t => { t.WasteDays = Mathf.Max(t.WasteDays, 2); });
    }

    private void AddCard(string name, string summary, CardCategory category, Color color, bool risk, Action<TurnState> apply)
    {
        deckTemplate.Add(new CardDef { Name = name, Summary = summary, Category = category, Color = color, Risk = risk, Apply = apply });
    }

    private void BuildDeck()
    {
        drawPile.Clear();
        discardPile.Clear();
        hand.Clear();
        selected.Clear();
        foreach (CardDef card in deckTemplate) drawPile.Add(card);
        Shuffle(drawPile);
    }

    private void PrepareDay()
    {
        selected.Clear();
        DrawToFullHand();
        currentEvent = RollEvent();
        latestWarnings = "Choose 1 card to shape the jar.";
        dayReport = jarName + "\nDay " + day + "\nPlan carefully before resolving the ecosystem.";
        bannerText.text = jarName + "  Day " + day + " - Play 1 card, then draw 1.";
        RefreshHud();
    }

    private void DrawToFullHand()
    {
        while (hand.Count < 3)
        {
            if (drawPile.Count == 0)
            {
                drawPile.AddRange(discardPile);
                discardPile.Clear();
                Shuffle(drawPile);
            }

            if (drawPile.Count == 0) break;
            CardDef next = drawPile[drawPile.Count - 1];
            drawPile.RemoveAt(drawPile.Count - 1);
            hand.Add(next);
        }
    }

    private EventDef RollEvent()
    {
        int roll = UnityEngine.Random.Range(0, 100);
        if (roll < 18) return new EventDef { Name = "Snail Eggs", Summary = "+2 snails appear automatically.", Apply = t => { t.AddSnail += 2; t.Notes.Add("Random event: snail eggs hatched."); } };
        if (roll < 34) return new EventDef { Name = "Fish Disease", Summary = "Fish eat less this turn.", Apply = t => { t.AppetiteMultiplier *= 0.75f; t.FishHealthBonus -= 0.04f; t.Notes.Add("Random event: fish disease reduced appetite."); } };
        if (roll < 50) return new EventDef { Name = "Nutrient Spike", Summary = "Nitrates rise by 15.", Apply = t => { t.NitrateBonus += 15f; t.Notes.Add("Random event: a nutrient spike hit the jar."); } };
        if (roll < 62) return new EventDef { Name = "Cloudy Weather", Summary = "Light drops this turn.", Apply = t => { t.LightBonus -= 1; } };
        return new EventDef { Name = "Calm Day", Summary = "No random event today.", Apply = t => { } };
    }

    private void ToggleCard(int index)
    {
        if (index < 0 || index >= hand.Count || state != GameState.Playing) return;
        CardDef card = hand[index];
        if (selected.Contains(card)) selected.Remove(card);
        else if (selected.Count < 1) selected.Add(card);
        RefreshHud();
    }

    private void TickDay()
    {
        if (state != GameState.Playing) return;
        if (selected.Count == 0) { latestWarnings = "Play 1 card before ending the day."; RefreshHud(); return; }

        TurnState turn = new TurnState();
        currentEvent.Apply(turn);
        foreach (CardDef card in selected) card.Apply(turn);

        RemoveSpecies(SpeciesType.Fish, turn.RemoveFish);
        RemoveSpecies(SpeciesType.Snail, turn.RemoveSnail);
        AddSpecies(SpeciesType.Fish, turn.AddFish);
        AddSpecies(SpeciesType.Snail, turn.AddSnail);
        ApplyAlgaeChange(turn.AlgaeBonus);
        if (turn.FilterDays > 0) filterDaysRemaining = Mathf.Max(filterDaysRemaining, turn.FilterDays);
        if (turn.WasteDays > 0) wasteDaysRemaining = Mathf.Max(wasteDaysRemaining, turn.WasteDays);

        ResolveSimulation(turn);
        foreach (CardDef playedCard in selected)
        {
            hand.Remove(playedCard);
            discardPile.Add(playedCard);
        }
        selected.Clear();
        if (state != GameState.Playing) return;
        day++;
        PrepareDay();
    }

    private void ResolveSimulation(TurnState turn)
    {
        int algae = CountSpecies(SpeciesType.Algae);
        int snails = CountSpecies(SpeciesType.Snail);
        int fish = CountSpecies(SpeciesType.Fish);
        float algaeFactor = difficulty == DifficultyMode.Easy ? 0.86f : difficulty == DifficultyMode.Hard ? 1.18f : 1f;
        float nitrateFactor = difficulty == DifficultyMode.Easy ? 0.88f : difficulty == DifficultyMode.Hard ? 1.18f : 1f;
        float bloomBonus = difficulty == DifficultyMode.Easy ? 2f : difficulty == DifficultyMode.Hard ? -2f : 0f;
        float tempAlgaeFactor = temperatureLevel == 0 ? 0.84f : temperatureLevel == 2 ? 1.18f : 1f;
        float tempFishStress = temperatureLevel == 2 ? -0.08f : temperatureLevel == 0 ? -0.02f : 0f;
        int effectiveLight = Mathf.Clamp(3 + turn.LightBonus, 0, 5);
        float feedProvided = Mathf.Max(0f, 3f + turn.FeedBonus);
        float fishIntake;
        float fishDemand = TotalFishDemand(turn.AppetiteMultiplier);
        int fishGraze = Mathf.Min(algae, Mathf.Max(0, Mathf.RoundToInt(TotalFishGrazePotential(feedProvided) + turn.ExtraFishGraze)));
        int snailGraze = Mathf.Min(Mathf.Max(0, algae - fishGraze), Mathf.RoundToInt(snails * 0.7f));
        int algaeGrowth = Mathf.Clamp(Mathf.RoundToInt((((effectiveLight * 0.8f) + (nitrateLevel * 0.17f) + Mathf.Min(algae * 0.08f, 1.6f) + algaeMemory) * algaeFactor) * tempAlgaeFactor), 0, 6);
        int algaeDelta = Mathf.Clamp(algaeGrowth - Mathf.Min(algae, fishGraze + snailGraze + (effectiveLight == 0 ? 1 : 0)), -4, 6);
        nitrateLevel += ((fish * (0.5f + (feedProvided * 0.18f))) + (snails * 0.05f)) * nitrateFactor;
        nitrateLevel += turn.NitrateBonus;
        nitrateLevel -= Mathf.Min(nitrateLevel, (algae + Mathf.Max(0, algaeDelta)) * 0.14f);
        if (filterDaysRemaining > 0) { nitrateLevel = Mathf.Max(0f, nitrateLevel - 2.4f); filterDaysRemaining--; }
        if (wasteDaysRemaining > 0) { nitrateLevel += 1.8f; wasteDaysRemaining--; }
        nitrateLevel = Mathf.Clamp(nitrateLevel, 0f, 30f);
        ApplyAlgaeChange(algaeDelta);

        algae = CountSpecies(SpeciesType.Algae);
        fish = CountSpecies(SpeciesType.Fish);
        snails = CountSpecies(SpeciesType.Snail);
        bloomThreshold = 14f + (snails * 1.4f) + bloomBonus;
        fishIntake = fish > 0 ? (feedProvided + (fishGraze * 0.8f)) / fish : 0f;
        float snailFood = snails > 0 ? (snailGraze + Mathf.Max(0, algae - 2) * 0.08f) / snails : 0f;
        AdjustFishHealth((fishIntake >= 1.05f ? 0.07f : -0.2f) + tempFishStress + turn.FishHealthBonus);
        AdjustSpeciesHealth(SpeciesType.Snail, algae < 2 || snailFood < 0.45f ? -0.18f : (snails > Mathf.Max(4, algae / 2) ? -0.06f : 0.06f));
        AdjustSpeciesHealth(SpeciesType.Algae, effectiveLight == 0 || nitrateLevel < 1f ? -0.14f : 0.04f);
        ReproduceFish(fishIntake);
        ReproduceSnails(algae, snails);
        CleanupDead();
        UpdateEcosystemMemory();
        UpdateMilestones();
        bool bloom = CountSpecies(SpeciesType.Algae) >= bloomThreshold;
        if (bloom) { bloomFlash = 1.5f; AdjustFishHealth(-0.45f); CleanupDead(); }

        int finalAlgae = CountSpecies(SpeciesType.Algae);
        int finalSnails = CountSpecies(SpeciesType.Snail);
        int finalFish = CountSpecies(SpeciesType.Fish);
        latestWarnings = BuildWarnings(finalAlgae, finalSnails, finalFish, bloom);
        bool stable = finalFish > 0 && finalSnails > 0 && finalAlgae > 0 && nitrateLevel <= 10f && finalAlgae < bloomThreshold - 2f;
        stableDays = stable ? stableDays + 1 : 0;
        perfectDays = stable && string.IsNullOrEmpty(latestWarnings) ? perfectDays + 1 : 0;
        stability = Mathf.Clamp((finalFish * 14f) + (finalSnails * 10f) + (finalAlgae * 4f) - (nitrateLevel * 3.2f) - (bloom ? 18f : 0f), 0f, 100f);
        dayReport = BuildReport(turn, fishIntake, fishGraze, snailGraze, algaeGrowth, fishDemand);

        if (stableDays >= 12) { EndGame(true, "You stabilized the jar for 12 straight days."); return; }
        if (finalFish <= 0) { EndGame(false, "The fish died out and the jar collapsed."); return; }
        if (finalSnails <= 0) { EndGame(false, "The snails died out and algae control failed."); return; }
        if (finalAlgae <= 0) { EndGame(false, "The algae collapsed and the food web failed."); return; }
        if (nitrateLevel >= 24f) { EndGame(false, "Nitrates overwhelmed the jar."); return; }
        RefreshHud();
    }

    private string BuildWarnings(int algae, int snails, int fish, bool bloom)
    {
        List<string> warnings = new List<string>();
        if (bloom) warnings.Add("Algae bloom risk is active.");
        if (nitrateLevel >= 10f) warnings.Add("High nitrates are stressing the ecosystem.");
        if (fish > 0 && AverageFishHealth() < 0.4f) warnings.Add("Fish are weakened.");
        if (snails > 0 && algae < snails) warnings.Add("Snails may starve if algae stays low.");
        if (filterDaysRemaining > 0) warnings.Add("Filter System is still active.");
        if (wasteDaysRemaining > 0) warnings.Add("Waste Build-Up is still active.");
        if (algaeMemory > 0.15f) warnings.Add("Ecosystem memory is making algae rebound faster.");
        return string.Join("\n", warnings.ToArray());
    }

    private string BuildReport(TurnState turn, float fishIntake, int fishGraze, int snailGraze, int algaeGrowth, float fishDemand)
    {
        StringBuilder b = new StringBuilder();
        b.AppendLine(jarName + " - Day " + day + " Report");
        b.AppendLine("You played 1 card.");
        b.AppendLine("Cause and effect:");
        foreach (CardDef card in selected) b.AppendLine("- " + card.Name);
        b.AppendLine(fishIntake < 1.05f ? "Fish were underfed and leaned on algae." : "Fish feeding was adequate.");
        if (fishGraze > 0) b.AppendLine("Fish grazed on algae.");
        if (snailGraze > 0) b.AppendLine("Snails cleaned extra algae.");
        if (algaeGrowth > 0) b.AppendLine("Light and nitrates pushed algae growth.");
        if (fishDemand > 0f && fishIntake < 0.85f) b.AppendLine("Fish demand was hard to satisfy this turn.");
        if (currentEvent != null && currentEvent.Name != "Calm Day") b.AppendLine("Event: " + currentEvent.Summary);
        for (int i = 0; i < turn.Notes.Count && i < 3; i++) b.AppendLine(turn.Notes[i]);
        if (!string.IsNullOrEmpty(latestMilestone) && latestMilestone != "No milestones yet.") b.AppendLine("Milestone: " + latestMilestone);
        if (!string.IsNullOrEmpty(latestWarnings)) b.AppendLine("Warnings: " + latestWarnings.Replace("\n", ", "));
        return b.ToString();
    }

    private void RefreshHud()
    {
        if (statsText != null) statsText.text = jarName + "\nDay: " + day + "\nStable Days: " + stableDays + " / 12\nPerfect Days: " + perfectDays + " / 10\nNitrates: " + nitrateLevel.ToString("0.0") + "\nBloom Threshold: " + bloomThreshold.ToString("0.0") + "\nStability: " + stability.ToString("0") + "\nTemperature: " + TemperatureLabel();
        if (warningText != null)
        {
            warningText.text = string.IsNullOrEmpty(latestWarnings) ? "Warnings\nStable for now." : "Warnings\n" + latestWarnings;
            warningText.color = string.IsNullOrEmpty(latestWarnings) ? new Color(0.72f, 0.9f, 0.78f) : new Color(0.98f, 0.84f, 0.4f);
        }
        if (eventText != null) eventText.text = currentEvent == null ? "Event\nNo event" : "Event\n" + currentEvent.Name + "\n" + currentEvent.Summary;
        if (selectedText != null) selectedText.text = BuildSelectedText();
        if (reportText != null) reportText.text = dayReport;
        if (deckText != null) deckText.text = "Draw: " + drawPile.Count + "\nDiscard: " + discardPile.Count;
        if (speciesText != null) speciesText.text = "Algae: " + CountSpecies(SpeciesType.Algae) + "\nSnails: " + CountSpecies(SpeciesType.Snail) + "\nFish: " + CountSpecies(SpeciesType.Fish);
        if (bannerText != null && state == GameState.Playing)
        {
            bannerText.color = string.IsNullOrEmpty(latestWarnings) ? new Color(0.13f, 0.2f, 0.16f) : new Color(0.3f, 0.25f, 0.08f);
        }
        RefreshCards();
    }

    private string BuildSelectedText()
    {
        if (selected.Count == 0) return "Selected Card\nPick 1 card.";
        StringBuilder b = new StringBuilder();
        b.AppendLine("Selected Card");
        foreach (CardDef card in selected) b.AppendLine("- " + card.Name);
        return b.ToString();
    }

    private void RefreshCards()
    {
        for (int i = 0; i < cardButtons.Count; i++)
        {
            bool visible = i < hand.Count;
            cardButtons[i].transform.parent.gameObject.SetActive(visible);
            if (!visible) continue;
            CardDef card = hand[i];
            cardTitles[i].text = card.Name + "\n" + card.Category + (card.Risk ? " Risk" : string.Empty);
            cardBodies[i].text = card.Summary;
            cardButtons[i].GetComponent<Image>().color = selected.Contains(card) ? Color.Lerp(card.Color, Color.white, 0.2f) : card.Color;
            Text label = cardButtons[i].GetComponentInChildren<Text>();
            if (label != null) label.text = selected.Contains(card) ? "Selected" : (selected.Count >= 1 ? "Pick 1 Only" : "Select");
            cardButtons[i].interactable = selected.Contains(card) || selected.Count < 1;
            Tooltip(cardButtons[i].transform.parent.gameObject, card.Name + "\n" + card.Summary);
        }
    }

    private void ApplyStartingJar()
    {
        if (startingJar == StartingJar.Balanced) { nitrateLevel = 5f; AddSpecies(SpeciesType.Algae, 7); AddSpecies(SpeciesType.Snail, 2); AddSpecies(SpeciesType.Fish, 2); }
        else if (startingJar == StartingJar.HighNitrates) { nitrateLevel = 9f; AddSpecies(SpeciesType.Algae, 8); AddSpecies(SpeciesType.Snail, 2); AddSpecies(SpeciesType.Fish, 2); }
        else if (startingJar == StartingJar.SnailHeavy) { nitrateLevel = 4f; AddSpecies(SpeciesType.Algae, 7); AddSpecies(SpeciesType.Snail, 4); AddSpecies(SpeciesType.Fish, 1); }
        else if (startingJar == StartingJar.Overgrown) { nitrateLevel = 7f; AddSpecies(SpeciesType.Algae, 11); AddSpecies(SpeciesType.Snail, 2); AddSpecies(SpeciesType.Fish, 2); }
        else { nitrateLevel = 3f; AddSpecies(SpeciesType.Algae, 4); AddSpecies(SpeciesType.Snail, 1); AddSpecies(SpeciesType.Fish, 2); }
    }

    private float TotalFishDemand(float appetiteModifier)
    {
        float demand = 0f;
        foreach (OrganismView fish in Species(SpeciesType.Fish)) demand += FishTraitAppetite(fish) * appetiteModifier;
        return demand;
    }

    private float TotalFishGrazePotential(float feedProvided)
    {
        float graze = 0f;
        foreach (OrganismView fish in Species(SpeciesType.Fish)) graze += Mathf.Max(0f, FishTraitGraze(fish) - (feedProvided * 0.5f));
        return graze;
    }

    private float FishTraitAppetite(OrganismView fish) { FishTrait trait = fishTraits.ContainsKey(fish) ? fishTraits[fish] : FishTrait.Balanced; return trait == FishTrait.Hungry ? 1.7f : trait == FishTrait.Lazy ? 1.1f : 1.45f; }
    private float FishTraitGraze(OrganismView fish) { FishTrait trait = fishTraits.ContainsKey(fish) ? fishTraits[fish] : FishTrait.Balanced; return trait == FishTrait.Hungry ? 2.7f : trait == FishTrait.Lazy ? 1.6f : 2.2f; }

    private void UpdateEcosystemMemory()
    {
        if (nitrateLevel >= 10f) highNitrateDays++;
        else highNitrateDays = 0;
        if (highNitrateDays >= 3) algaeMemory = Mathf.Clamp(algaeMemory + 0.06f, 0f, 0.3f);
        else algaeMemory = Mathf.Max(0f, algaeMemory - 0.02f);
    }

    private void UpdateMilestones()
    {
        int algae = CountSpecies(SpeciesType.Algae);
        int snails = CountSpecies(SpeciesType.Snail);
        int fish = CountSpecies(SpeciesType.Fish);
        if (stableDays >= 5) latestMilestone = "Balanced Ecosystem";
        else if (snails >= 5) latestMilestone = "Snail Paradise";
        else if (nitrateLevel <= 4f && algae >= 5 && algae <= 8) latestMilestone = "Perfect Water";
        else if (fish >= 4) latestMilestone = "Busy Tank";
    }

    private string TemperatureLabel() { return temperatureLevel == 0 ? "Cold" : temperatureLevel == 2 ? "Hot" : "Warm"; }
    private float AverageFishHealth() { List<OrganismView> fish = Species(SpeciesType.Fish); if (fish.Count == 0) return 0f; float total = 0f; foreach (OrganismView f in fish) total += f.Health; return total / fish.Count; }

    private void ReproduceFish(float fishIntake)
    {
        if (fishIntake < 1.15f || nitrateLevel >= 9f) return;
        foreach (OrganismView fish in Species(SpeciesType.Fish))
        {
            fish.ReproductionProgress += 0.35f;
            if (fish.ReproductionProgress >= 1f) { fish.ReproductionProgress = 0f; AddSpecies(SpeciesType.Fish, 1); break; }
        }
    }

    private void ReproduceSnails(int algae, int snails)
    {
        if (algae < 6 || snails <= 0 || snails >= 6) return;
        foreach (OrganismView snail in Species(SpeciesType.Snail))
        {
            snail.ReproductionProgress += 0.45f;
            if (snail.ReproductionProgress >= 1f) { snail.ReproductionProgress = 0f; AddSpecies(SpeciesType.Snail, 1); break; }
        }
    }

    private void AddSpecies(SpeciesType type, int amount) { for (int i = 0; i < amount; i++) AddOrganism(type); }
    private void ApplyAlgaeChange(int delta) { if (delta > 0) AddSpecies(SpeciesType.Algae, delta); else if (delta < 0) RemoveSpecies(SpeciesType.Algae, -delta); }

    private void AddOrganism(SpeciesType type)
    {
        if (state != GameState.Playing || organisms.Count >= 32) return;
        SpeciesDef d = defs[type];
        GameObject go = new GameObject(d.Name);
        OrganismView view = go.AddComponent<OrganismView>();
        view.Initialize(type, d.Name, d.Color, whiteSprite, jarArea, new Vector2(UnityEngine.Random.Range(-360f, 360f), UnityEngine.Random.Range(-220f, 220f)));
        organisms.Add(view);
        if (type == SpeciesType.Fish) fishTraits[view] = RandomFishTrait();
    }

    private void RemoveSpecies(SpeciesType type, int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            OrganismView view = First(type);
            if (view == null) return;
            if (fishTraits.ContainsKey(view)) fishTraits.Remove(view);
            organisms.Remove(view);
            Destroy(view.gameObject);
        }
    }

    private OrganismView First(SpeciesType type) { for (int i = organisms.Count - 1; i >= 0; i--) if (organisms[i] != null && organisms[i].Species == type) return organisms[i]; return null; }
    private List<OrganismView> Species(SpeciesType type) { List<OrganismView> matches = new List<OrganismView>(); for (int i = 0; i < organisms.Count; i++) if (organisms[i] != null && organisms[i].Species == type) matches.Add(organisms[i]); return matches; }
    private void AdjustSpeciesHealth(SpeciesType type, float delta) { foreach (OrganismView o in organisms) if (o != null && o.Species == type) o.AdjustHealth(delta); }
    private void AdjustFishHealth(float delta) { foreach (OrganismView fish in Species(SpeciesType.Fish)) { FishTrait trait = fishTraits.ContainsKey(fish) ? fishTraits[fish] : FishTrait.Balanced; float traitDelta = trait == FishTrait.Fragile ? delta - 0.05f : trait == FishTrait.Hungry ? delta + 0.02f : trait == FishTrait.Lazy ? delta - 0.01f : delta; fish.AdjustHealth(traitDelta); } }

    private void CleanupDead()
    {
        for (int i = organisms.Count - 1; i >= 0; i--)
        {
            if (organisms[i] == null) { organisms.RemoveAt(i); continue; }
            if (!organisms[i].IsDead()) continue;
            if (fishTraits.ContainsKey(organisms[i])) fishTraits.Remove(organisms[i]);
            Destroy(organisms[i].gameObject);
            organisms.RemoveAt(i);
        }
    }

    private int CountSpecies(SpeciesType type) { int count = 0; for (int i = 0; i < organisms.Count; i++) if (organisms[i] != null && organisms[i].Species == type) count++; return count; }
    private void ClearOrganisms() { for (int i = organisms.Count - 1; i >= 0; i--) if (organisms[i] != null) Destroy(organisms[i].gameObject); organisms.Clear(); fishTraits.Clear(); }

    private void UpdateWater()
    {
        if (water == null) return;
        int algae = CountSpecies(SpeciesType.Algae);
        int fish = CountSpecies(SpeciesType.Fish);
        Color healthy = new Color(0.62f, 0.86f, 0.96f, 0.46f);
        Color algaeTint = new Color(0.42f, 0.78f, 0.42f, 0.66f);
        Color nitrateTint = new Color(0.54f, 0.62f, 0.42f, 0.62f);
        Color target = Color.Lerp(healthy, algaeTint, Mathf.Clamp01(algae / 16f));
        target = Color.Lerp(target, nitrateTint, Mathf.Clamp01(nitrateLevel / 18f) * 0.6f);
        if (algae >= bloomThreshold - 2f) target = Color.Lerp(target, new Color(0.12f, 0.5f, 0.16f, 0.82f), 0.55f);
        if (fish == 0 && state == GameState.Result) target = Color.Lerp(target, new Color(0.34f, 0.42f, 0.34f, 0.88f), 0.7f);
        if (bloomFlash > 0f) { bloomFlash = Mathf.Max(0f, bloomFlash - Time.deltaTime); float pulse = 0.5f + (Mathf.Sin(Time.time * 12f) * 0.5f); target = Color.Lerp(target, new Color(0.2f, 0.85f, 0.2f, 0.85f), pulse); }
        water.color = Color.Lerp(water.color, target, Time.deltaTime * 3.5f);
        if (lightGlow != null) lightGlow.color = Color.Lerp(lightGlow.color, new Color(1f, 0.97f, 0.74f, 0.22f), Time.deltaTime * 4f);
    }

    private FishTrait RandomFishTrait() { int roll = UnityEngine.Random.Range(0, 4); return roll == 0 ? FishTrait.Hungry : roll == 1 ? FishTrait.Lazy : roll == 2 ? FishTrait.Fragile : FishTrait.Balanced; }
    private void EndGame(bool won, string message)
    {
        state = GameState.Result;
        resultPanel.SetActive(true);
        resultText.text = won
            ? "Jar Stabilized\n\n" + jarName + "\nFinal Day: " + day + "\n" + message
            : "Ecosystem Collapse\n\n" + jarName + "\nFinal Day: " + day + "\n" + message;
        bannerText.text = message;
        RefreshHud();
    }
    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int swapIndex = UnityEngine.Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[swapIndex];
            list[swapIndex] = temp;
        }
    }

    private void Tooltip(GameObject target, string message)
    {
        EventTrigger t = target.GetComponent<EventTrigger>() ?? target.AddComponent<EventTrigger>();
        t.triggers.Clear();
        EventTrigger.Entry en = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        en.callback.AddListener(delegate { if (tooltipPanel != null) { tooltipText.text = message; tooltipPanel.SetActive(true); } });
        t.triggers.Add(en);
        EventTrigger.Entry ex = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        ex.callback.AddListener(delegate { if (tooltipPanel != null) tooltipPanel.SetActive(false); });
        t.triggers.Add(ex);
    }

    private Sprite CreateWhiteSprite()
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
    }

    private static GameObject Panel(string name, Transform parent, Color color) { GameObject go = new GameObject(name); RectTransform rt = go.AddComponent<RectTransform>(); rt.SetParent(parent, false); Image img = go.AddComponent<Image>(); img.color = color; return go; }
    private static Button CreateUiButton(string text, Transform parent, Color color, UnityEngine.Events.UnityAction onClick) { GameObject go = Panel(text + "Button", parent, color); Button b = go.AddComponent<Button>(); ColorBlock cb = b.colors; cb.highlightedColor = Color.Lerp(color, Color.white, 0.18f); cb.pressedColor = Color.Lerp(color, Color.black, 0.12f); b.colors = cb; b.onClick.AddListener(onClick); Text t = Label(text + "Text", go.transform, 18, FontStyle.Bold, TextAnchor.MiddleCenter); t.text = text; t.color = new Color(0.11f, 0.15f, 0.19f); Stretch(t.rectTransform); return b; }
    private static Text Label(string name, Transform parent, int size, FontStyle style, TextAnchor anchor) { GameObject go = new GameObject(name); RectTransform rt = go.AddComponent<RectTransform>(); rt.SetParent(parent, false); Text t = go.AddComponent<Text>(); t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); t.fontSize = size; t.fontStyle = style; t.alignment = anchor; t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow; return t; }
    private static void Stretch(RectTransform rt) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero; }
    private static void Place(RectTransform rt, Vector2 min, Vector2 max, Vector2 offMin, Vector2 offMax) { rt.anchorMin = min; rt.anchorMax = max; rt.offsetMin = offMin; rt.offsetMax = offMax; }
}
