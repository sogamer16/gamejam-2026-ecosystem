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
    private class Def { public string Name; public string Tip; public Color Color; }
    private class DayEvent { public string Name; public string Summary; public int Light; public float Appetite = 1f; public float Nitrate; public int Temperature; public int AlgaeBoost; public int SnailBoost; }

    private readonly Dictionary<SpeciesType, Def> defs = new Dictionary<SpeciesType, Def>();
    private readonly List<OrganismView> organisms = new List<OrganismView>();
    private readonly Dictionary<SpeciesType, Text> counts = new Dictionary<SpeciesType, Text>();
    private readonly Dictionary<OrganismView, FishTrait> fishTraits = new Dictionary<OrganismView, FishTrait>();
    private readonly Queue<float> delayedNitrateQueue = new Queue<float>();

    private GameState state = GameState.Menu;
    private DifficultyMode difficulty = DifficultyMode.Normal;
    private StartingJar startingJar = StartingJar.Balanced;
    private Sprite whiteSprite;
    private Canvas canvas;
    private RectTransform jarArea;
    private Image water;
    private Text statsText;
    private Text warningText;
    private Text eventText;
    private Text countsText;
    private Text bannerText;
    private Text lightValueText;
    private Text fishFoodValueText;
    private Slider lightSlider;
    private Image lightGlow;
    private RectTransform lightTrack;
    private Image lightMover;
    private Text diagramText;
    private Text reportText;
    private Text tooltipText;
    private GameObject tooltipPanel;
    private GameObject menuPanel;
    private GameObject resultPanel;
    private Text resultText;
    private bool uiReady;

    private int day;
    private int stableDays;
    private int perfectDays;
    private int lightLevel;
    private int fishFoodLevel;
    private int temperatureLevel;
    private float nitrateLevel;
    private float stability;
    private float bloomThreshold;
    private int bloomRiskDays;
    private int highNitrateDays;
    private bool perfectUnlocked;
    private float bloomFlash;
    private string dayReport;
    private string diagramState;
    private string latestMilestone;
    private string jarName;
    private float algaeMemory;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        defs[SpeciesType.Algae] = new Def { Name = "Algae", Tip = "Light and nitrates grow algae. Too much causes bloom danger.", Color = new Color(0.43f, 0.79f, 0.36f) };
        defs[SpeciesType.Snail] = new Def { Name = "Snail", Tip = "Snails depend on algae and clean extra growth.", Color = new Color(0.92f, 0.73f, 0.45f) };
        defs[SpeciesType.Fish] = new Def { Name = "Fish", Tip = "Fish create nitrates. Low food makes them graze algae.", Color = new Color(0.42f, 0.72f, 0.96f) };
        defs[SpeciesType.Shrimp] = new Def { Name = "Shrimp", Tip = "Shrimp slowly clean algae but are sensitive to high nitrates.", Color = new Color(0.96f, 0.56f, 0.52f) };
        BuildVisuals();
        ShowMenu();
    }

    private void OnDestroy() { SceneManager.sceneLoaded -= OnSceneLoaded; }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) { if (canvas == null) BuildVisuals(); }
    private void Update() { if (state == GameState.Playing && Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) TickDay(); UpdateWater(); }

    private void BuildVisuals()
    {
        uiReady = false;
        whiteSprite = CreateWhiteSprite();
        if (FindFirstObjectByType<Canvas>() != null && FindFirstObjectByType<Canvas>().gameObject.name == "EcosystemCanvas") return;
        GameObject c = new GameObject("EcosystemCanvas");
        canvas = c.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = c.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920f, 1080f); c.AddComponent<GraphicRaycaster>();
        GameObject bg = Panel("BG", canvas.transform, new Color(0.93f, 0.96f, 0.98f)); Stretch(bg.GetComponent<RectTransform>());
        GameObject left = Panel("Left", canvas.transform, new Color(0.15f, 0.24f, 0.31f, 0.96f)); Place(left.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0.3f, 1f), Vector2.zero, Vector2.zero);
        Text title = Label("Title", left.transform, 38, FontStyle.Bold, TextAnchor.UpperLeft); Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -24f), new Vector2(-24f, -86f)); title.text = "Glass World"; title.color = Color.white;
        Text desc = Label("Desc", left.transform, 18, FontStyle.Normal, TextAnchor.UpperLeft); Place(desc.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -88f), new Vector2(-24f, -148f)); desc.text = "Adjust light and fish food each day. Keep algae, snails, fish, and nitrates in balance inside a living glass jar."; desc.color = new Color(0.88f, 0.93f, 0.97f);
        Control(left.transform, "Fish Food", "Less food makes fish eat algae. More food raises nitrates.", -164f, AdjustFishFood, out fishFoodValueText);
        if (fishFoodValueText != null) fishFoodValueText.text = "3 / 5";
        float top = -266f;
        foreach (SpeciesType type in new[] { SpeciesType.Algae, SpeciesType.Snail, SpeciesType.Fish, SpeciesType.Shrimp })
        {
            UnityEngine.UI.Button b = CreateUiButton(defs[type].Name, left.transform, defs[type].Color, delegate { AddOrganism(type); }); Place(b.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, top), new Vector2(-24f, top - 56f)); Tooltip(b.gameObject, defs[type].Tip);
            Text ct = Label(defs[type].Name + "Count", b.transform, 18, FontStyle.Bold, TextAnchor.MiddleRight); Place(ct.rectTransform, new Vector2(0.7f, 0f), new Vector2(0.95f, 1f), Vector2.zero, Vector2.zero); ct.color = new Color(0.1f, 0.14f, 0.19f); counts[type] = ct; top -= 62f;
        }
        diagramText = Label("Diagram", left.transform, 15, FontStyle.Bold, TextAnchor.UpperLeft); Place(diagramText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -484f), new Vector2(-24f, -630f)); diagramText.color = new Color(0.9f, 0.94f, 0.98f); Tooltip(diagramText.gameObject, "Whenever something changes, the active arrow is highlighted here.");
        UnityEngine.UI.Button next = CreateUiButton("Run Next Day", left.transform, new Color(0.38f, 0.78f, 0.55f), TickDay); Place(next.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(24f, 206f), new Vector2(-24f, 150f));
        UnityEngine.UI.Button restart = CreateUiButton("Restart", left.transform, new Color(0.91f, 0.43f, 0.35f), StartGame); Place(restart.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(24f, 140f), new Vector2(-24f, 84f));
        statsText = Label("Stats", left.transform, 18, FontStyle.Bold, TextAnchor.UpperLeft); Place(statsText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(24f, 168f), new Vector2(-24f, 92f)); statsText.color = Color.white;
        warningText = Label("Warnings", left.transform, 15, FontStyle.Bold, TextAnchor.UpperLeft); Place(warningText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(24f, 92f), new Vector2(-24f, 16f)); warningText.color = new Color(0.98f, 0.84f, 0.35f);
        eventText = Label("Event", left.transform, 14, FontStyle.Normal, TextAnchor.UpperLeft); Place(eventText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(24f, 16f), new Vector2(-24f, -40f)); eventText.color = new Color(0.78f, 0.86f, 0.92f);
        countsText = Label("Counts", left.transform, 15, FontStyle.Normal, TextAnchor.UpperLeft); Place(countsText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(24f, -38f), new Vector2(-24f, -124f)); countsText.color = new Color(0.87f, 0.92f, 0.97f);
        reportText = Label("Report", left.transform, 14, FontStyle.Normal, TextAnchor.UpperLeft); Place(reportText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(24f, -122f), new Vector2(-24f, -260f)); reportText.color = new Color(0.83f, 0.9f, 0.96f);
        GameObject jar = Panel("Jar", canvas.transform, new Color(0.81f, 0.92f, 0.98f, 0.2f)); Place(jar.GetComponent<RectTransform>(), new Vector2(0.33f, 0.08f), new Vector2(0.96f, 0.92f), Vector2.zero, Vector2.zero); Outline o = jar.AddComponent<Outline>(); o.effectColor = new Color(0.19f, 0.35f, 0.49f, 0.8f); o.effectDistance = new Vector2(4f, 4f);
        water = Panel("Water", jar.transform, new Color(0.62f, 0.86f, 0.96f, 0.5f)).GetComponent<Image>(); Place(water.rectTransform, new Vector2(0.04f, 0.12f), new Vector2(0.96f, 0.9f), Vector2.zero, Vector2.zero);
        lightGlow = Panel("LightGlow", jar.transform, new Color(1f, 0.97f, 0.74f, 0.18f)).GetComponent<Image>(); Place(lightGlow.rectTransform, new Vector2(0.08f, 0.72f), new Vector2(0.92f, 0.95f), Vector2.zero, Vector2.zero);
        CreateJarLightControl(jar.transform);
        bannerText = Label("Banner", jar.transform, 28, FontStyle.Bold, TextAnchor.UpperCenter); Place(bannerText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -20f), new Vector2(-24f, -72f)); bannerText.color = new Color(0.1f, 0.19f, 0.26f);
        Text jarHint = Label("JarHint", jar.transform, 20, FontStyle.Normal, TextAnchor.LowerCenter); Place(jarHint.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(24f, 24f), new Vector2(-24f, 82f)); jarHint.text = "Clear water is healthy. Green water means algae pressure. Murky water means nitrates are building up."; jarHint.color = new Color(0.13f, 0.2f, 0.27f);
        GameObject layer = new GameObject("Organisms"); jarArea = layer.AddComponent<RectTransform>(); jarArea.SetParent(jar.transform, false); Place(jarArea, new Vector2(0.07f, 0.14f), new Vector2(0.93f, 0.86f), Vector2.zero, Vector2.zero);
        tooltipPanel = Panel("Tooltip", canvas.transform, new Color(0.04f, 0.09f, 0.14f, 0.92f)); Place(tooltipPanel.GetComponent<RectTransform>(), new Vector2(0.68f, 0.02f), new Vector2(0.96f, 0.12f), Vector2.zero, Vector2.zero); tooltipText = Label("TooltipText", tooltipPanel.transform, 15, FontStyle.Normal, TextAnchor.MiddleCenter); Stretch(tooltipText.rectTransform); tooltipText.color = Color.white; tooltipPanel.SetActive(false);
        menuPanel = Panel("Menu", canvas.transform, new Color(0.04f, 0.09f, 0.14f, 0.84f)); Stretch(menuPanel.GetComponent<RectTransform>());
        Text mt = Label("MenuTitle", menuPanel.transform, 56, FontStyle.Bold, TextAnchor.MiddleCenter); Place(mt.rectTransform, new Vector2(0.2f, 0.56f), new Vector2(0.8f, 0.8f), Vector2.zero, Vector2.zero); mt.text = "Glass World"; mt.color = Color.white;
        Text mb = Label("MenuBody", menuPanel.transform, 25, FontStyle.Normal, TextAnchor.MiddleCenter); Place(mb.rectTransform, new Vector2(0.18f, 0.34f), new Vector2(0.82f, 0.58f), Vector2.zero, Vector2.zero); mb.text = "Quick runs, readable warnings, random events, and one ecosystem loop."; mb.color = new Color(0.88f, 0.93f, 0.97f);
        UnityEngine.UI.Button play = CreateUiButton("Start Prototype", menuPanel.transform, new Color(0.38f, 0.78f, 0.55f), StartGame); Place(play.GetComponent<RectTransform>(), new Vector2(0.39f, 0.22f), new Vector2(0.61f, 0.31f), Vector2.zero, Vector2.zero);
        resultPanel = Panel("Result", canvas.transform, new Color(0.04f, 0.09f, 0.14f, 0.84f)); Stretch(resultPanel.GetComponent<RectTransform>()); resultText = Label("ResultText", resultPanel.transform, 44, FontStyle.Bold, TextAnchor.MiddleCenter); Place(resultText.rectTransform, new Vector2(0.16f, 0.38f), new Vector2(0.84f, 0.7f), Vector2.zero, Vector2.zero); resultText.color = Color.white; UnityEngine.UI.Button again = CreateUiButton("Play Again", resultPanel.transform, new Color(0.4f, 0.75f, 0.95f), ReturnToStartScene); Place(again.GetComponent<RectTransform>(), new Vector2(0.4f, 0.22f), new Vector2(0.6f, 0.3f), Vector2.zero, Vector2.zero); resultPanel.SetActive(false);
        SyncLightSlider();
        uiReady = true;
    }

    private void ShowMenu() { state = GameState.Menu; menuPanel.SetActive(true); resultPanel.SetActive(false); bannerText.text = "Set your jar and begin"; dayReport = "Day report\nChoose a difficulty and starting jar, then begin."; diagramState = "neutral"; latestMilestone = "No milestones yet."; jarName = "Jar #00"; RefreshHud(new DayEvent { Name = "No event", Summary = "Use menu options to pick a challenge." }); if (GameSettingsStore.HasSelection) StartGame(); }
    private void StartGame() { menuPanel.SetActive(false); resultPanel.SetActive(false); state = GameState.Playing; day = 0; stableDays = 0; perfectDays = 0; bloomRiskDays = 0; highNitrateDays = 0; perfectUnlocked = false; algaeMemory = 0f; lightLevel = 3; fishFoodLevel = 3; temperatureLevel = GameSettingsStore.HasSelection ? GameSettingsStore.TemperatureLevel : 1; difficulty = GameSettingsStore.HasSelection ? (DifficultyMode)GameSettingsStore.DifficultyIndex : DifficultyMode.Normal; startingJar = GameSettingsStore.HasSelection ? (StartingJar)GameSettingsStore.StartingJarIndex : StartingJar.Balanced; nitrateLevel = 5f; stability = 72f; dayReport = "Day 0 Report\nThe ecosystem is slightly unstable but recoverable."; diagramState = "neutral"; latestMilestone = "No milestones yet."; jarName = "Jar #" + UnityEngine.Random.Range(10, 99); delayedNitrateQueue.Clear(); delayedNitrateQueue.Enqueue(0f); delayedNitrateQueue.Enqueue(0f); ClearOrganisms(); ApplyStartingJar(); SyncLightSlider(); bannerText.text = "Day 0. Adjust the controls, then run the next day."; RefreshHud(new DayEvent { Name = "Start", Summary = "Your jar is slightly unstable but recoverable." }); }
    private void ApplyStartingJar() { if (startingJar == StartingJar.Balanced) { nitrateLevel = 5f; AddSpecies(SpeciesType.Algae, 5); AddSpecies(SpeciesType.Snail, 2); AddSpecies(SpeciesType.Fish, 2); AddSpecies(SpeciesType.Shrimp, 1); } else if (startingJar == StartingJar.HighNitrates) { nitrateLevel = 8f; AddSpecies(SpeciesType.Algae, 6); AddSpecies(SpeciesType.Snail, 2); AddSpecies(SpeciesType.Fish, 2); } else if (startingJar == StartingJar.SnailHeavy) { nitrateLevel = 4f; AddSpecies(SpeciesType.Algae, 6); AddSpecies(SpeciesType.Snail, 4); AddSpecies(SpeciesType.Fish, 1); AddSpecies(SpeciesType.Shrimp, 1); } else if (startingJar == StartingJar.Overgrown) { nitrateLevel = 7f; AddSpecies(SpeciesType.Algae, 10); AddSpecies(SpeciesType.Snail, 2); AddSpecies(SpeciesType.Fish, 2); } else { nitrateLevel = 3f; AddSpecies(SpeciesType.Algae, 3); AddSpecies(SpeciesType.Snail, 1); AddSpecies(SpeciesType.Fish, 2); AddSpecies(SpeciesType.Shrimp, 1); } }
    private DayEvent RollEvent() { int roll = UnityEngine.Random.Range(0, 100); if (roll < 14) return new DayEvent { Name = "Cloudy day", Summary = "Cloud cover reduced light by 30 percent.", Light = -1 }; if (roll < 26) return new DayEvent { Name = "Algae spores", Summary = "Spores drifted in and algae jumped upward.", AlgaeBoost = 2 }; if (roll < 38) return new DayEvent { Name = "Sick fish", Summary = "Fish ate less today.", Appetite = 0.78f }; if (roll < 50) return new DayEvent { Name = "Dirty water", Summary = "The water turned dirtier and nitrates rose.", Nitrate = 2f }; if (roll < 60) return new DayEvent { Name = "Snail eggs", Summary = "A fresh clutch added one snail.", SnailBoost = 1 }; if (roll < 70) return new DayEvent { Name = "Heat wave", Summary = "The jar warmed up suddenly.", Temperature = 1 }; if (roll < 80) return new DayEvent { Name = "Cold snap", Summary = "The jar cooled down for the day.", Temperature = -1 }; return new DayEvent { Name = "Calm day", Summary = "No surprise event today." }; }

    private void TickDay()
    {
        if (state != GameState.Playing) return;
        day++;
        DayEvent e = RollEvent();
        int algae = CountSpecies(SpeciesType.Algae), snails = CountSpecies(SpeciesType.Snail), fish = CountSpecies(SpeciesType.Fish), shrimp = CountSpecies(SpeciesType.Shrimp);
        float algaeFactor = difficulty == DifficultyMode.Easy ? 0.82f : difficulty == DifficultyMode.Hard ? 1.18f : 1f;
        float nitrateFactor = difficulty == DifficultyMode.Easy ? 0.85f : difficulty == DifficultyMode.Hard ? 1.2f : 1f;
        float bloomBonus = difficulty == DifficultyMode.Easy ? 2f : difficulty == DifficultyMode.Hard ? -2f : 0f;
        int effectiveLight = Mathf.Clamp(lightLevel + e.Light, 0, 5);
        int effectiveTemp = Mathf.Clamp(temperatureLevel + e.Temperature, 0, 2);
        float tempAlgaeFactor = effectiveTemp == 0 ? 0.8f : effectiveTemp == 2 ? 1.2f : 1f;
        float tempFishStress = effectiveTemp == 2 ? -0.08f : 0f;
        float feedProvided = fishFoodLevel * 1.15f;
        float fishDemand = TotalFishDemand(e.Appetite);
        int fishGraze = Mathf.Min(algae, Mathf.Max(0, Mathf.RoundToInt(TotalFishGrazePotential())));
        int snailGraze = Mathf.Min(Mathf.Max(0, algae - fishGraze), Mathf.RoundToInt(snails * 0.7f));
        int shrimpGraze = Mathf.Min(Mathf.Max(0, algae - fishGraze - snailGraze), shrimp);
        int algaeGrowth = Mathf.Clamp(Mathf.RoundToInt((((effectiveLight * 0.75f) + (nitrateLevel * 0.18f) + Mathf.Min(algae * 0.08f, 1.6f) + algaeMemory) * algaeFactor) * tempAlgaeFactor), 0, 5);
        algaeGrowth += e.AlgaeBoost;
        int algaeDelta = Mathf.Clamp(algaeGrowth - Mathf.Min(algae, fishGraze + snailGraze + shrimpGraze + (effectiveLight == 0 ? Mathf.Min(algae, 1) : 0)), -3, 5);
        float leftoverFood = Mathf.Max(0f, feedProvided - fishDemand);
        float delayedSpike = delayedNitrateQueue.Count > 0 ? delayedNitrateQueue.Dequeue() : 0f;
        delayedNitrateQueue.Enqueue(leftoverFood * 0.55f);
        nitrateLevel += ((fish * (0.45f + (fishFoodLevel * 0.16f))) + (snails * 0.05f) + (shrimp * 0.03f) + delayedSpike + e.Nitrate) * nitrateFactor;
        nitrateLevel -= Mathf.Min(nitrateLevel, (algae + Mathf.Max(0, algaeDelta)) * 0.16f); nitrateLevel = Mathf.Clamp(nitrateLevel, 0f, 20f);
        if (snailGraze > 0 || shrimpGraze > 0) nitrateLevel = Mathf.Max(0f, nitrateLevel - 0.2f);
        ApplyAlgaeChange(algaeDelta);
        if (e.SnailBoost > 0) AddSpecies(SpeciesType.Snail, e.SnailBoost);
        algae = CountSpecies(SpeciesType.Algae); fish = CountSpecies(SpeciesType.Fish); snails = CountSpecies(SpeciesType.Snail);
        bloomThreshold = 14f + (snails * 1.6f) + bloomBonus;
        float fishIntake = fish > 0 ? (feedProvided + (fishGraze * 0.8f)) / fish : 0f;
        float snailFood = snails > 0 ? (snailGraze + Mathf.Max(0, algae - 2) * 0.08f) / snails : 0f;
        AdjustFishHealth((fishIntake >= 1.05f ? 0.07f : -0.18f) + tempFishStress);
        AdjustSpeciesHealth(SpeciesType.Snail, algae < 2 || snailFood < 0.45f ? -0.18f : (snails > Mathf.Max(4, algae / 2) ? -0.08f : 0.06f));
        AdjustSpeciesHealth(SpeciesType.Shrimp, nitrateLevel > 10f ? -0.16f : (algae >= 3 ? 0.05f : -0.1f));
        AdjustSpeciesHealth(SpeciesType.Algae, effectiveLight == 0 || nitrateLevel < 1f ? -0.12f : 0.04f);
        ReproduceFish(fishIntake); ReproduceSnails(algae, snails); ReproduceShrimp(algae, shrimp);
        UpdateEcosystemMemory();
        bool bloom = EvaluateBloom(algae); CleanupDead();
        string warnings = BuildWarnings(algae, fishIntake, snailFood, fish, snails);
        diagramState = DetermineDiagramState(fishGraze, snailGraze + shrimpGraze, effectiveLight, fish, warnings);
        dayReport = BuildDayReport(fishIntake, fishGraze, snailGraze + shrimpGraze, algaeGrowth, warnings, e);
        UpdateStability(algae, fish, snails + shrimp, warnings, bloom);
        UpdateMilestones(algae, fish, snails, shrimp);
        if (!bloom) bannerText.text = DailySummary(algae, fishGraze, snailGraze + shrimpGraze, effectiveLight, warnings);
        EvaluateState(algae, snails, fish, bloom);
        RefreshHud(e, warnings);
    }

    private void ReproduceFish(float fishIntake) { if (fishIntake < 1.35f || nitrateLevel > 11f) return; foreach (OrganismView fish in Species(SpeciesType.Fish)) { fish.ReproductionProgress += 0.35f; if (fish.ReproductionProgress >= 4f) { fish.ReproductionProgress = 0f; AddOrganism(SpeciesType.Fish); break; } } }
    private void ReproduceSnails(int algae, int snails) { if (algae < 5 || algae > 11 || nitrateLevel < 2f || nitrateLevel > 10f || snails >= 8) return; foreach (OrganismView snail in Species(SpeciesType.Snail)) { snail.ReproductionProgress += 0.45f; if (snail.ReproductionProgress >= 3.5f) { snail.ReproductionProgress = 0f; AddOrganism(SpeciesType.Snail); break; } } }
    private void ReproduceShrimp(int algae, int shrimp) { if (algae < 4 || nitrateLevel > 9f || shrimp >= 5) return; foreach (OrganismView cleaner in Species(SpeciesType.Shrimp)) { cleaner.ReproductionProgress += 0.3f; if (cleaner.ReproductionProgress >= 4f) { cleaner.ReproductionProgress = 0f; AddOrganism(SpeciesType.Shrimp); break; } } }
    private bool EvaluateBloom(int algae) { if (algae > bloomThreshold + 2f) { TriggerBloom(); return true; } if (algae > bloomThreshold) { bloomRiskDays++; if (bloomRiskDays >= 2) { TriggerBloom(); return true; } } else bloomRiskDays = 0; return false; }
    private void TriggerBloom() { bannerText.text = "Algae bloom. Oxygen crashed and the fish died."; int deadFish = CountSpecies(SpeciesType.Fish); RemoveSpecies(SpeciesType.Fish, deadFish); nitrateLevel = Mathf.Clamp(nitrateLevel + (deadFish * 1.4f), 0f, 20f); bloomFlash = 1.8f; }
    private string BuildWarnings(int algae, float fishIntake, float snailFood, int fish, int snails) { List<string> w = new List<string>(); if (algae >= bloomThreshold - 2f) w.Add("Algae bloom risk high"); if (fishIntake < 1.05f && fish > 0) w.Add("Fish underfed"); if (snailFood < 0.45f && snails > 0) w.Add("Snails starving"); if (nitrateLevel >= 10f) w.Add("High nitrates increased algae growth"); if (temperatureLevel == 2) w.Add("Hot water stresses fish"); if (algaeMemory > 0.15f) w.Add("Ecosystem memory is making algae rebound faster"); return string.Join("\n", w.ToArray()); }
    private void UpdateStability(int algae, int fish, int snails, string warnings, bool bloom) { float score = 100f; score -= Mathf.Abs(algae - 7) * 6f; score -= Mathf.Abs(nitrateLevel - 6f) * 4.5f; score -= Mathf.Abs(fish - 2) * 7f; score -= Mathf.Abs(snails - 3) * 5f; if (!string.IsNullOrEmpty(warnings)) score -= 14f; stability = Mathf.Clamp(score, 0f, 100f); if (bloom) { stableDays = 0; perfectDays = 0; return; } if (stability >= 72f) { stableDays++; perfectDays = string.IsNullOrEmpty(warnings) ? perfectDays + 1 : 0; } else { stableDays = 0; perfectDays = 0; } if (perfectDays >= 10) perfectUnlocked = true; }
    private string DailySummary(int algae, int fishGraze, int snailGraze, int effectiveLight, string warnings) { if (!string.IsNullOrEmpty(warnings)) return warnings.Split('\n')[0]; if (effectiveLight >= 4) return "Bright light is pushing algae growth upward."; if (fishFoodLevel <= 1 && fishGraze > 0) return "Fish are hungry, so they are eating algae today."; if (snailGraze > 0) return "Snails cleaned algae and helped stabilize the jar."; if (algae >= 6 && algae <= 9) return "The ecosystem looks healthy today."; return "The jar changed gradually today. Adjust again for tomorrow."; }
    private void EvaluateState(int algae, int snails, int fish, bool bloom) { if (bloom) { EndGame(false, "An algae bloom wiped out the fish on day " + day + "."); return; } if (algae <= 0 || snails <= 0 || fish <= 0) { EndGame(false, "A species collapsed on day " + day + ". Keep all three species alive."); return; } if (stableDays >= 12) EndGame(true, "You kept the jar stable for 12 straight days." + (perfectUnlocked ? "\n\nPerfect Ecosystem unlocked: 10 stable days with no warnings." : string.Empty)); }
    private void RefreshHud(DayEvent e, string warnings = "")
    {
        if (!uiReady || statsText == null || warningText == null || eventText == null || countsText == null || fishFoodValueText == null)
        {
            return;
        }

        int algae = CountSpecies(SpeciesType.Algae), snails = CountSpecies(SpeciesType.Snail), fish = CountSpecies(SpeciesType.Fish), shrimp = CountSpecies(SpeciesType.Shrimp);
        bloomThreshold = 14f + (snails * 1.6f) + (difficulty == DifficultyMode.Easy ? 2f : difficulty == DifficultyMode.Hard ? -2f : 0f);
        if (lightValueText != null) lightValueText.text = lightLevel + " / 5";
        fishFoodValueText.text = fishFoodLevel + " / 5";
        SyncLightSlider();
        statsText.text = jarName + "\nDay " + day + "\nStability " + Mathf.RoundToInt(stability) + "%\nStable Days " + stableDays + " / 12\nNitrates " + Mathf.RoundToInt(nitrateLevel) + "\nBloom Limit " + Mathf.RoundToInt(bloomThreshold) + "\nWater " + TemperatureLabel();
        warningText.text = string.IsNullOrEmpty(warnings) ? "Warnings\nNone" : "Warnings\n" + warnings;
        eventText.text = "Daily Event\n" + e.Name + ": " + e.Summary + "\nMilestone: " + latestMilestone;
        countsText.text = "Jar counts\nAlgae: " + algae + "\nSnails: " + snails + "\nFish: " + fish + "\nShrimp: " + shrimp + "\n\nTrade-offs\nMore light = more algae\nLess fish food = more grazing\nMore fish = more nitrates\nHot water = fish stress";

        if (reportText != null) reportText.text = dayReport;
        if (diagramText != null) diagramText.text = BuildDiagramText();

        foreach (SpeciesType type in new[] { SpeciesType.Algae, SpeciesType.Snail, SpeciesType.Fish, SpeciesType.Shrimp })
        {
            if (counts.ContainsKey(type) && counts[type] != null)
            {
                counts[type].text = CountSpecies(type).ToString();
            }
        }
    }
    private void UpdateWater() { if (water == null) return; int algae = CountSpecies(SpeciesType.Algae); int fish = CountSpecies(SpeciesType.Fish); float lightT = Mathf.InverseLerp(0f, 5f, lightLevel); Color healthy = new Color(0.62f, 0.86f, 0.96f, 0.42f + (lightT * 0.08f)), algaeTint = new Color(0.42f, 0.78f, 0.42f, 0.66f), nitrateTint = new Color(0.54f, 0.62f, 0.42f, 0.62f); Color target = Color.Lerp(healthy, algaeTint, Mathf.Clamp01(algae / 16f)); target = Color.Lerp(target, nitrateTint, Mathf.Clamp01(nitrateLevel / 14f) * 0.6f); target = Color.Lerp(target, new Color(0.76f, 0.9f, 1f, target.a), lightT * 0.22f); if (algae >= bloomThreshold - 2f) target = Color.Lerp(target, new Color(0.12f, 0.5f, 0.16f, 0.82f), 0.55f); if (fish == 0 && state == GameState.Result) target = Color.Lerp(target, new Color(0.34f, 0.42f, 0.34f, 0.88f), 0.7f); if (bloomFlash > 0f) { bloomFlash = Mathf.Max(0f, bloomFlash - Time.deltaTime); float pulse = 0.5f + (Mathf.Sin(Time.time * 12f) * 0.5f); target = Color.Lerp(target, new Color(0.2f, 0.85f, 0.2f, 0.85f), pulse); } water.color = Color.Lerp(water.color, target, Time.deltaTime * 3.5f); if (lightGlow != null) { Color glow = new Color(1f, 0.97f, 0.74f, Mathf.Lerp(0.05f, 0.55f, lightT)); lightGlow.color = Color.Lerp(lightGlow.color, glow, Time.deltaTime * 4f); } }
    private string DetermineDiagramState(int fishGraze, int snailGraze, int effectiveLight, int fish, string warnings) { if (warnings.Contains("Ecosystem memory")) return "memory"; if (warnings.Contains("High nitrates")) return "nitrates"; if (warnings.Contains("Algae bloom risk")) return "bloom"; if (fishGraze > 0 && fishFoodLevel <= 1) return "food"; if (effectiveLight >= 4) return "light"; if (snailGraze > 0) return "snails"; if (fish > 0) return "fish"; return "neutral"; }
    private string BuildDiagramText() { string fishNode = diagramState == "fish" ? "[Fish]" : " Fish "; string nitrateNode = diagramState == "nitrates" ? "[Nitrates]" : "Nitrates"; string algaeNode = diagramState == "light" || diagramState == "bloom" || diagramState == "memory" ? "[Algae]" : " Algae "; string snailNode = diagramState == "snails" ? "[Snails]" : "Snails"; string foodNode = diagramState == "food" ? "[Food]" : " Food "; string lightNode = diagramState == "light" ? "[Light]" : "Light"; string memoryNode = diagramState == "memory" ? "[Memory]" : "Memory"; return "Feedback Loop\n" + fishNode + " -> " + nitrateNode + "\n" + foodNode + " -> " + fishNode + "\n" + lightNode + " -> " + algaeNode + "\n" + nitrateNode + " -> " + algaeNode + "\n" + algaeNode + " -> " + snailNode + "\n" + memoryNode + " -> " + algaeNode; }
    private string BuildDayReport(float fishIntake, int fishGraze, int snailGraze, int algaeGrowth, string warnings, DayEvent e) { StringBuilder b = new StringBuilder(); b.AppendLine("Day " + day + " Report"); if (fishIntake < 1.05f) b.AppendLine("Fish were slightly underfed."); else b.AppendLine("Fish were fed well enough."); if (fishGraze > 0) b.AppendLine("Fish grazed on algae."); if (snailGraze > 0) b.AppendLine("Snails and shrimp cleaned extra algae."); if (algaeGrowth > 0) b.AppendLine("Light, temperature, and nitrates increased algae growth."); if (nitrateLevel >= 10f) b.AppendLine("Fish waste pushed nitrates high."); if (delayedNitrateQueue.Count > 0) b.AppendLine("Older feeding decisions are still affecting water quality."); if (algaeMemory > 0.15f) b.AppendLine("The jar remembers recent nitrate stress and rebounds faster."); if (!string.IsNullOrEmpty(latestMilestone) && latestMilestone != "No milestones yet.") b.AppendLine("Milestone: " + latestMilestone); if (!string.IsNullOrEmpty(warnings)) b.AppendLine("Warning: " + warnings.Replace("\n", ", ")); if (e.Name != "Calm day") b.AppendLine("Event: " + e.Summary); return b.ToString(); }
    private float TotalFishDemand(float appetiteModifier) { float demand = 0f; foreach (OrganismView fish in Species(SpeciesType.Fish)) demand += FishTraitAppetite(fish) * appetiteModifier; return demand; }
    private float TotalFishGrazePotential() { float graze = 0f; foreach (OrganismView fish in Species(SpeciesType.Fish)) graze += Mathf.Max(0f, FishTraitGraze(fish) - (fishFoodLevel * 0.42f)); return graze; }
    private float FishTraitAppetite(OrganismView fish) { FishTrait trait = fishTraits.ContainsKey(fish) ? fishTraits[fish] : FishTrait.Balanced; return trait == FishTrait.Hungry ? 1.7f : trait == FishTrait.Lazy ? 1.1f : 1.45f; }
    private float FishTraitGraze(OrganismView fish) { FishTrait trait = fishTraits.ContainsKey(fish) ? fishTraits[fish] : FishTrait.Balanced; return trait == FishTrait.Hungry ? 2.7f : trait == FishTrait.Lazy ? 1.6f : 2.2f; }
    private void UpdateEcosystemMemory() { if (nitrateLevel >= 10f) highNitrateDays++; else highNitrateDays = 0; if (highNitrateDays >= 3) algaeMemory = Mathf.Clamp(algaeMemory + 0.06f, 0f, 0.3f); else algaeMemory = Mathf.Max(0f, algaeMemory - 0.02f); }
    private void UpdateMilestones(int algae, int fish, int snails, int shrimp) { if (stableDays >= 5 && latestMilestone != "Balanced Ecosystem") latestMilestone = "Balanced Ecosystem"; if (snails >= 5 && latestMilestone != "Snail Paradise") latestMilestone = "Snail Paradise"; if (nitrateLevel <= 4f && algae >= 5 && algae <= 8 && latestMilestone != "Perfect Water") latestMilestone = "Perfect Water"; if (shrimp >= 3 && latestMilestone != "Cleaner Crew") latestMilestone = "Cleaner Crew"; }
    private string TemperatureLabel() { return temperatureLevel == 0 ? "Cold" : temperatureLevel == 2 ? "Hot" : "Warm"; }

    private void AddSpecies(SpeciesType type, int amount) { for (int i = 0; i < amount; i++) AddOrganism(type); }
    private void ApplyAlgaeChange(int delta) { if (delta > 0) AddSpecies(SpeciesType.Algae, delta); else if (delta < 0) RemoveSpecies(SpeciesType.Algae, -delta); }
    private void AddOrganism(SpeciesType type) { if (state != GameState.Playing || organisms.Count >= 24) return; Def d = defs[type]; GameObject go = new GameObject(d.Name); OrganismView view = go.AddComponent<OrganismView>(); view.Initialize(type, d.Name, d.Color, whiteSprite, jarArea, new Vector2(UnityEngine.Random.Range(-360f, 360f), UnityEngine.Random.Range(-240f, 210f))); organisms.Add(view); if (type == SpeciesType.Fish) fishTraits[view] = RandomFishTrait(); }
    private void RemoveSpecies(SpeciesType type, int amount) { for (int i = 0; i < amount; i++) { OrganismView view = First(type); if (view == null) return; if (fishTraits.ContainsKey(view)) fishTraits.Remove(view); organisms.Remove(view); Destroy(view.gameObject); } }
    private OrganismView First(SpeciesType type) { for (int i = organisms.Count - 1; i >= 0; i--) if (organisms[i] != null && organisms[i].Species == type) return organisms[i]; return null; }
    private List<OrganismView> Species(SpeciesType type) { List<OrganismView> matches = new List<OrganismView>(); for (int i = 0; i < organisms.Count; i++) if (organisms[i] != null && organisms[i].Species == type) matches.Add(organisms[i]); return matches; }
    private void AdjustSpeciesHealth(SpeciesType type, float delta) { foreach (OrganismView o in organisms) if (o != null && o.Species == type) o.AdjustHealth(delta); }
    private void AdjustFishHealth(float delta) { foreach (OrganismView fish in Species(SpeciesType.Fish)) { FishTrait trait = fishTraits.ContainsKey(fish) ? fishTraits[fish] : FishTrait.Balanced; float traitDelta = trait == FishTrait.Fragile ? delta - 0.05f : trait == FishTrait.Hungry ? delta + 0.02f : trait == FishTrait.Lazy ? delta - 0.01f : delta; fish.AdjustHealth(traitDelta); } }
    private void CleanupDead() { for (int i = organisms.Count - 1; i >= 0; i--) { if (organisms[i] == null) { organisms.RemoveAt(i); continue; } if (organisms[i].IsDead()) { if (fishTraits.ContainsKey(organisms[i])) fishTraits.Remove(organisms[i]); Destroy(organisms[i].gameObject); organisms.RemoveAt(i); } } }
    private int CountSpecies(SpeciesType type) { int count = 0; for (int i = 0; i < organisms.Count; i++) if (organisms[i] != null && organisms[i].Species == type) count++; return count; }
    private void ClearOrganisms() { for (int i = organisms.Count - 1; i >= 0; i--) if (organisms[i] != null) Destroy(organisms[i].gameObject); organisms.Clear(); fishTraits.Clear(); }
    private void AdjustLight(int delta) { if (state == GameState.Playing) { lightLevel = Mathf.Clamp(lightLevel + delta, 0, 5); RefreshHud(new DayEvent { Name = "Planning", Summary = "Light changed before the next day." }); } }
    private void AdjustFishFood(int delta) { if (state == GameState.Playing) { fishFoodLevel = Mathf.Clamp(fishFoodLevel + delta, 0, 5); RefreshHud(new DayEvent { Name = "Planning", Summary = "Fish food changed before the next day." }); } }
    private void AdjustTemperature(int delta) { if (state == GameState.Playing) { temperatureLevel = Mathf.Clamp(temperatureLevel + delta, 0, 2); RefreshHud(new DayEvent { Name = "Planning", Summary = "Temperature changed before the next day." }); } }
    private void ChangeDifficulty(int delta) { difficulty = (DifficultyMode)Mathf.Clamp((int)difficulty + delta, 0, Enum.GetValues(typeof(DifficultyMode)).Length - 1); RefreshHud(new DayEvent { Name = "Setup", Summary = "Difficulty changes algae speed and bloom forgiveness." }); }
    private void ChangeJar(int delta) { startingJar = (StartingJar)Mathf.Clamp((int)startingJar + delta, 0, Enum.GetValues(typeof(StartingJar)).Length - 1); RefreshHud(new DayEvent { Name = "Setup", Summary = "Starting jar changes the opening ecosystem state." }); }
    private void OnLightSliderChanged(float value) { lightLevel = Mathf.RoundToInt(value); if (!uiReady || state != GameState.Playing) return; RefreshHud(new DayEvent { Name = "Planning", Summary = "Light slider changed before the next day." }); }
    private void SyncLightSlider() { if (lightSlider != null && Mathf.Abs(lightSlider.value - lightLevel) > 0.01f) lightSlider.SetValueWithoutNotify(lightLevel); }
    private void ReturnToStartScene() { SceneManager.LoadScene("main"); }
    private void Control(Transform parent, string label, string hint, float top, Action<int> onAdjust, out Text valueText) { GameObject box = Panel(label + "Box", parent, new Color(0.1f, 0.16f, 0.2f, 0.68f)); Place(box.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, top), new Vector2(-24f, top - 90f)); Tooltip(box, hint); Text l = Label(label + "Label", box.transform, 20, FontStyle.Bold, TextAnchor.UpperLeft); Place(l.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, -12f), new Vector2(-16f, -34f)); l.text = label; l.color = Color.white; Text h = Label(label + "Hint", box.transform, 13, FontStyle.Normal, TextAnchor.UpperLeft); Place(h.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, -34f), new Vector2(-16f, -54f)); h.text = hint; h.color = new Color(0.78f, 0.86f, 0.92f); UnityEngine.UI.Button m = CreateUiButton("-", box.transform, new Color(0.88f, 0.58f, 0.36f), delegate { onAdjust(-1); }); Place(m.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(16f, 10f), new Vector2(76f, 46f)); valueText = Label(label + "Value", box.transform, 22, FontStyle.Bold, TextAnchor.MiddleCenter); Place(valueText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(82f, 8f), new Vector2(-82f, 48f)); valueText.color = Color.white; valueText.text = "3 / 5"; UnityEngine.UI.Button p = CreateUiButton("+", box.transform, new Color(0.4f, 0.75f, 0.95f), delegate { onAdjust(1); }); Place(p.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-76f, 10f), new Vector2(-16f, 46f)); }
    private void CreateSliderControl(Transform parent, string label, string hint, float top, out Text valueText, out Slider slider) { GameObject box = Panel(label + "SliderBox", parent, new Color(0.1f, 0.16f, 0.2f, 0.68f)); Place(box.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, top), new Vector2(-24f, top - 112f)); Tooltip(box, hint); Text l = Label(label + "Label", box.transform, 20, FontStyle.Bold, TextAnchor.UpperLeft); Place(l.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, -12f), new Vector2(-16f, -34f)); l.text = label; l.color = Color.white; Text h = Label(label + "Hint", box.transform, 13, FontStyle.Normal, TextAnchor.UpperLeft); Place(h.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, -34f), new Vector2(-16f, -56f)); h.text = hint; h.color = new Color(0.78f, 0.86f, 0.92f); GameObject sliderObject = new GameObject(label + "Slider"); RectTransform sliderRect = sliderObject.AddComponent<RectTransform>(); sliderRect.SetParent(box.transform, false); Place(sliderRect, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(18f, 18f), new Vector2(-90f, 46f)); slider = sliderObject.AddComponent<Slider>(); slider.minValue = 0f; slider.maxValue = 5f; slider.wholeNumbers = true; GameObject background = Panel(label + "SliderBg", sliderObject.transform, new Color(0.23f, 0.3f, 0.36f, 1f)); Stretch(background.GetComponent<RectTransform>()); slider.targetGraphic = background.GetComponent<Image>(); GameObject fillArea = new GameObject("Fill Area"); RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>(); fillAreaRect.SetParent(sliderObject.transform, false); Place(fillAreaRect, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(8f, 6f), new Vector2(-8f, -6f)); GameObject fill = Panel("Fill", fillArea.transform, new Color(1f, 0.9f, 0.45f, 1f)); slider.fillRect = fill.GetComponent<RectTransform>(); Stretch(fill.GetComponent<RectTransform>()); GameObject handleArea = new GameObject("Handle Area"); RectTransform handleAreaRect = handleArea.AddComponent<RectTransform>(); handleAreaRect.SetParent(sliderObject.transform, false); Stretch(handleAreaRect); GameObject handle = Panel("Handle", handleArea.transform, new Color(1f, 0.98f, 0.82f, 1f)); RectTransform handleRect = handle.GetComponent<RectTransform>(); handleRect.sizeDelta = new Vector2(26f, 34f); slider.handleRect = handleRect; slider.direction = Slider.Direction.LeftToRight; slider.onValueChanged.AddListener(OnLightSliderChanged); valueText = Label(label + "Value", box.transform, 22, FontStyle.Bold, TextAnchor.MiddleCenter); Place(valueText.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-82f, 10f), new Vector2(-16f, 48f)); valueText.color = Color.white; }
    private void CreateJarLightControl(Transform parent) { GameObject sliderObject = new GameObject("JarLightSlider"); RectTransform sliderRect = sliderObject.AddComponent<RectTransform>(); sliderRect.SetParent(parent, false); Place(sliderRect, new Vector2(0.9f, 0.18f), new Vector2(0.96f, 0.82f), Vector2.zero, Vector2.zero); lightTrack = sliderRect; lightSlider = sliderObject.AddComponent<Slider>(); lightSlider.minValue = 0f; lightSlider.maxValue = 5f; lightSlider.wholeNumbers = true; lightSlider.direction = Slider.Direction.BottomToTop; GameObject background = Panel("Track", sliderObject.transform, new Color(0.17f, 0.25f, 0.3f, 0.45f)); Stretch(background.GetComponent<RectTransform>()); lightSlider.targetGraphic = background.GetComponent<Image>(); GameObject fillArea = new GameObject("Fill Area"); RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>(); fillAreaRect.SetParent(sliderObject.transform, false); Place(fillAreaRect, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(10f, 10f), new Vector2(-10f, -10f)); GameObject fill = Panel("Fill", fillArea.transform, new Color(1f, 0.93f, 0.55f, 0.3f)); lightSlider.fillRect = fill.GetComponent<RectTransform>(); Stretch(fill.GetComponent<RectTransform>()); GameObject handleArea = new GameObject("Handle Area"); RectTransform handleAreaRect = handleArea.AddComponent<RectTransform>(); handleAreaRect.SetParent(sliderObject.transform, false); Stretch(handleAreaRect); GameObject handle = Panel("Handle", handleArea.transform, new Color(1f, 0.98f, 0.8f, 0.95f)); lightMover = handle.GetComponent<Image>(); RectTransform handleRect = handle.GetComponent<RectTransform>(); handleRect.sizeDelta = new Vector2(56f, 34f); lightSlider.handleRect = handleRect; lightSlider.onValueChanged.AddListener(OnLightSliderChanged); Tooltip(sliderObject, "Drag this light up and down inside the jar to change brightness."); }
    private void Tooltip(GameObject target, string message) { EventTrigger t = target.GetComponent<EventTrigger>() ?? target.AddComponent<EventTrigger>(); EventTrigger.Entry en = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter }; en.callback.AddListener(delegate { if (tooltipPanel != null) { tooltipText.text = message; tooltipPanel.SetActive(true); } }); t.triggers.Add(en); EventTrigger.Entry ex = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit }; ex.callback.AddListener(delegate { if (tooltipPanel != null) tooltipPanel.SetActive(false); }); t.triggers.Add(ex); }
    private FishTrait RandomFishTrait() { int roll = UnityEngine.Random.Range(0, 4); return roll == 0 ? FishTrait.Hungry : roll == 1 ? FishTrait.Lazy : roll == 2 ? FishTrait.Fragile : FishTrait.Balanced; }
    private Sprite CreateWhiteSprite() { Texture2D tex = new Texture2D(1, 1); tex.SetPixel(0, 0, Color.white); tex.Apply(); return Sprite.Create(tex, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f)); }
    private void EndGame(bool won, string message) { state = GameState.Result; resultPanel.SetActive(true); resultText.text = won ? "Jar Stabilized\n\n" + message : "Ecosystem Collapse\n\n" + message; bannerText.text = message; }
    private static GameObject Panel(string name, Transform parent, Color color) { GameObject go = new GameObject(name); RectTransform rt = go.AddComponent<RectTransform>(); rt.SetParent(parent, false); Image img = go.AddComponent<Image>(); img.color = color; return go; }
    private static UnityEngine.UI.Button CreateUiButton(string text, Transform parent, Color color, UnityEngine.Events.UnityAction onClick) { GameObject go = Panel(text + "Button", parent, color); UnityEngine.UI.Button b = go.AddComponent<UnityEngine.UI.Button>(); ColorBlock cb = b.colors; cb.highlightedColor = Color.Lerp(color, Color.white, 0.18f); cb.pressedColor = Color.Lerp(color, Color.black, 0.12f); b.colors = cb; b.onClick.AddListener(onClick); Text t = Label(text + "Text", go.transform, 20, FontStyle.Bold, TextAnchor.MiddleCenter); t.text = text; t.color = new Color(0.11f, 0.15f, 0.19f); Stretch(t.rectTransform); return b; }
    private static Text Label(string name, Transform parent, int size, FontStyle style, TextAnchor anchor) { GameObject go = new GameObject(name); RectTransform rt = go.AddComponent<RectTransform>(); rt.SetParent(parent, false); Text t = go.AddComponent<Text>(); t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); t.fontSize = size; t.fontStyle = style; t.alignment = anchor; t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow; return t; }
    private static void Stretch(RectTransform rt) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero; }
    private static void Place(RectTransform rt, Vector2 min, Vector2 max, Vector2 offMin, Vector2 offMax) { rt.anchorMin = min; rt.anchorMax = max; rt.offsetMin = offMin; rt.offsetMax = offMax; }
}
