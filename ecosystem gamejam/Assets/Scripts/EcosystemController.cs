using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using NueGames.NueDeck.Scripts.Enums;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
public enum SpeciesType { Algae, Snail, Fish, Shrimp }

[ExecuteAlways]
public class EcosystemController : MonoBehaviour
{
    private enum GameState { Menu, Playing, Result }
    private enum DayPhase { AwaitingRoll, Rolling, AwaitingPlay, ResolvingTurn }
    private enum DifficultyMode { Easy, Medium, Hard }
    private enum StartingJar { Balanced, HighNitrates, SnailHeavy, Overgrown, Fragile }
    private enum FishTrait { Balanced, Hungry, Lazy, Fragile }
    private enum CardCategory { Fish, Snail, Algae, Light, Water, Risk }
    private enum CardTier { Common, Uncommon, Rare }

    private sealed class SpeciesDef { public string Name; public Color Color; }
    private sealed class CardDef { public string Name; public string Summary; public CardCategory Category; public CardTier Tier; public Color Color; public bool Risk; public Action<TurnState> Apply; }
    private sealed class UiSpark
    {
        public Image Image;
        public Vector2 Position;
        public Vector2 Velocity;
        public float Lifetime;
        public float MaxLifetime;
    }
    private sealed class BubbleFx
    {
        public Transform Transform;
        public float Speed;
        public float Drift;
        public float Phase;
        public float TopY;
        public float BottomY;
    }
    private sealed class TurnState
    {
        public float LightDelta;
        public bool FeedFishPlayed;
        public float FeedWasteMultiplier = 1f;
        public float NitrateBonus;
        public int AlgaeBonus;
        public int AddFish;
        public int RemoveFish;
        public int AddSnail;
        public int RemoveSnail;
        public bool TriggerRandomEvent;
        public readonly List<string> Notes = new List<string>();
    }
    private struct DifficultySettings
    {
        public int StartFish;
        public int StartSnails;
        public int StartAlgae;
        public float StartNitrates;
        public float StartLight;
        public int StartRerolls;
        public int StableDaysToWin;
        public float BaseAlgaeGrowth;
        public float LightGrowthPerPointAbove50;
        public float NitrateGrowthPerPoint;
        public float MemoryBleed;
        public int AlgaeSoftCapStart;
        public float AlgaeSoftCapMultiplier;
        public float NitrateWarning;
        public float NitrateCollapse;
        public int AlgaeWarning;
        public int AlgaeCollapse;
        public float StableNitrateMax;
        public int StableAlgaeMin;
        public int StableAlgaeMax;
        public int StableFishMin;
        public int StableSnailMin;
        public float FishWastePerTurn;
        public int FishHungryGraze;
        public int FishStarveTurns;
        public bool FishStarveLoseOne;
        public float FishReproductionChancePerFish;
        public int FishReproductionMinAlgae;
        public int SnailAlgaeEat;
        public int SnailStarveThreshold;
        public int SnailStarveTurns;
        public bool SnailStarveLoseOne;
        public float SnailReproductionChance;
        public int SnailReproductionMinAlgae;
        public float PassiveNitrateDecay;
        public float FeedFishNitrateMultiplier;
        public float BloomFeedbackNitrates;
        public int UncommonUnlockRoll;
        public int RareUnlockRoll;
    }

    private readonly Dictionary<SpeciesType, SpeciesDef> defs = new Dictionary<SpeciesType, SpeciesDef>();
    private readonly List<OrganismView> organisms = new List<OrganismView>();
    private readonly Dictionary<OrganismView, FishTrait> fishTraits = new Dictionary<OrganismView, FishTrait>();
    private readonly List<CardDef> deckTemplate = new List<CardDef>();
    private readonly Dictionary<string, CardDef> cardLibrary = new Dictionary<string, CardDef>();
    private readonly List<CardDef> drawPile = new List<CardDef>();
    private readonly List<CardDef> discardPile = new List<CardDef>();
    private readonly List<CardDef> hand = new List<CardDef>();
    private readonly List<CardDef> selected = new List<CardDef>();
    private readonly List<EcosystemCardPrefabView> cardViews = new List<EcosystemCardPrefabView>();
    private readonly List<Button> cardButtons = new List<Button>();
    private readonly List<CanvasGroup> cardCanvasGroups = new List<CanvasGroup>();
    private readonly List<RectTransform> cardRoots = new List<RectTransform>();
    private readonly List<Image> cardShines = new List<Image>();
    private readonly List<UiSpark> uiSparks = new List<UiSpark>();
    private readonly List<BubbleFx> bubbles = new List<BubbleFx>();

    private GameState state = GameState.Menu;
    private DifficultyMode difficulty = DifficultyMode.Medium;
    private StartingJar startingJar = StartingJar.Balanced;

    private Sprite whiteSprite;
    private Canvas canvas;
    private Camera sceneCamera;
    private Shader foilShader;
    private GameObject nueCardPrefab;
    private Sprite shirtFrontSprite;
    private Sprite shirtAccentSprite;
    private readonly List<GameObject> fishPrefabs = new List<GameObject>();
    private GameObject snailPrefab;
    private Transform jarWorldRoot;
    private Transform jarCreatureRoot;
    private Transform jarFxRoot;
    private Renderer jarWaterRenderer;
    private Light tableKeyLight;
    private Light jarFillLight;
    private RectTransform leftPanelRect;
    private RectTransform rightPanelRect;
    private RectTransform drawPileMarker;
    private RectTransform discardPileMarker;
    private Image water;
    private Image lightGlow;
    private Image playFlash;
    private GameObject menuPanel;
    private GameObject resultPanel;
    private GameObject pausePanel;
    private Button primaryActionButton;
    private Button rerollButton;
    private TextMeshProUGUI bannerText;
    private TextMeshProUGUI statsText;
    private TextMeshProUGUI warningText;
    private TextMeshProUGUI eventText;
    private TextMeshProUGUI reportText;
    private TextMeshProUGUI selectedText;
    private TextMeshProUGUI deckText;
    private TextMeshProUGUI speciesText;
    private TextMeshProUGUI resultText;
    private TextMeshProUGUI tooltipText;
    private GameObject tooltipPanel;
    private TextMeshProUGUI pauseStatusText;
    private Image nitrateBarFill;
    private Image stableProgressFill;
    private Image warningCardImage;
    private GameObject playButtonGlow;
    private float warningPulseTime;
    private DayPhase dayPhase = DayPhase.AwaitingRoll;

    private int day;
    private int stableDays;
    private int perfectDays;
    private int temperatureLevel;
    private int rerollTokens;
    private int currentDieRoll;
    private int fishHungryTurns;
    private int snailStarvingTurns;
    private int highNitrateDays;
    private float nitrateLevel;
    private float bloomThreshold;
    private float stability;
    private float algaeMemory;
    private float bloomFlash;
    private float displayedLightLevel = 50f;
    private float lightLevel = 50f;
    private int previousTurnAlgaeCount;
    private string lastRandomEventSummary;
    private string latestWarnings;
    private string dayReport;
    private string latestMilestone;
    private string jarName;
    private int hoveredCardIndex = -1;
    private bool isResolvingCard;
    private bool isPaused;
    private int animatingCardIndex = -1;
    private float playAnimTime;
    private float playAnimDuration = 0.22f;
    private Vector2 playAnimStart;
    private Vector2 playAnimTarget;
    private float playAnimStartRotation;
    private float playAnimTargetRotation;
    private float playAnimStartScale = 1f;
    private float playAnimTargetScale = 1.18f;
    private float playAnimStartAlpha = 1f;
    private float playAnimTargetAlpha = 1f;
    private int animatingDrawCardIndex = -1;
    private float drawAnimTime;
    private float drawAnimDuration = 0.26f;
    private float screenShakeTime;
    private bool isCameraAnimating;
    private GameObject dicePrefab;
    private Mesh diceMesh;
    private Transform diceStageRoot;
    private Transform activeDie;
    private Material diceDisplayMaterial;

    private void Awake()
    {
        if (!Application.isPlaying)
        {
            if (!CanBuildEditorArtifacts()) return;
            foilShader = Shader.Find("Custom/CardFoilUI");
            LoadCreaturePrefabs();
            LoadDicePrefab();
            EnsurePresentationWorld();
            BuildVisuals();
            return;
        }

        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        nueCardPrefab = Resources.Load<GameObject>("EcosystemCards/CardUI");
        shirtFrontSprite = Resources.Load<Sprite>("EcosystemCards/Shirts/Card_shirt_01");
        shirtAccentSprite = Resources.Load<Sprite>("EcosystemCards/Shirts/Card_shirt_04");
        LoadCreaturePrefabs();
        LoadDicePrefab();
        defs[SpeciesType.Algae] = new SpeciesDef { Name = "Algae", Color = new Color(0.43f, 0.79f, 0.36f) };
        defs[SpeciesType.Snail] = new SpeciesDef { Name = "Snail", Color = new Color(0.92f, 0.73f, 0.45f) };
        defs[SpeciesType.Fish] = new SpeciesDef { Name = "Fish", Color = new Color(0.42f, 0.72f, 0.96f) };
        defs[SpeciesType.Shrimp] = new SpeciesDef { Name = "Shrimp", Color = new Color(0.96f, 0.56f, 0.52f) };
        BuildDeckTemplate();
        EnsurePresentationWorld();
        BuildVisuals();
        ShowMenu();
    }

    private void OnDestroy() { SceneManager.sceneLoaded -= OnSceneLoaded; }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) { EnsurePresentationWorld(); if (canvas == null) BuildVisuals(); }

    private void OnEnable()
    {
        if (!Application.isPlaying)
        {
            if (!CanBuildEditorArtifacts()) return;
            foilShader = Shader.Find("Custom/CardFoilUI");
            nueCardPrefab = Resources.Load<GameObject>("EcosystemCards/CardUI");
            shirtFrontSprite = Resources.Load<Sprite>("EcosystemCards/Shirts/Card_shirt_01");
            shirtAccentSprite = Resources.Load<Sprite>("EcosystemCards/Shirts/Card_shirt_04");
            LoadCreaturePrefabs();
            LoadDicePrefab();
            EnsurePresentationWorld();
            BuildVisuals();
        }
    }

    [ContextMenu("Rebuild 3D Jar World")]
    private void RebuildJarWorldInEditor()
    {
        GameObject existing = GameObject.Find("EcosystemJarWorld");
        if (existing != null)
        {
            if (Application.isPlaying) Destroy(existing);
            else DestroyImmediate(existing);
        }

        jarWorldRoot = null;
        jarCreatureRoot = null;
        jarFxRoot = null;
        jarWaterRenderer = null;
        tableKeyLight = null;
        jarFillLight = null;
        bubbles.Clear();
        EnsurePresentationWorld();
    }

    [ContextMenu("Rebuild Gameplay Canvas")]
    private void RebuildGameplayCanvasInEditor()
    {
        GameObject existingCanvas = GameObject.Find("EcosystemCanvas");
        if (existingCanvas != null)
        {
            if (Application.isPlaying) Destroy(existingCanvas);
            else DestroyImmediate(existingCanvas);
        }

        ClearVisualCaches();
        canvas = null;
        BuildVisuals();
    }

    [ContextMenu("Rebuild Dice Stage")]
    private void RebuildDiceStageInEditor()
    {
        GameObject existing = GameObject.Find("DiceRollStage");
        if (existing != null)
        {
            if (Application.isPlaying) Destroy(existing);
            else DestroyImmediate(existing);
        }

        diceStageRoot = null;
        activeDie = null;
        EnsureDiceStage();
    }

    private bool CanBuildEditorArtifacts()
    {
        if (Application.isPlaying) return false;
        if (this == null || gameObject == null) return false;
#if UNITY_EDITOR
        if (EditorApplication.isPlayingOrWillChangePlaymode) return false;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return false;
#endif
        Scene scene = gameObject.scene;
        if (!scene.IsValid() || !scene.isLoaded) return false;
        if (string.IsNullOrEmpty(scene.path)) return false;
        return true;
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame && state == GameState.Playing)
        {
            TogglePause();
        }

        if (isPaused)
        {
            UpdateSceneLighting();
            UpdateWater();
            UpdateJarFx();
            return;
        }

        if (state == GameState.Playing && !isResolvingCard && Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) HandlePrimaryAction();
        UpdateCardPresentation();
        UpdateSparks();
        UpdateScreenShake();
        UpdateSceneLighting();
        UpdateWater();
        UpdateJarFx();
        UpdateWarningPulse();
        UpdatePlayButtonGlow();
    }

    private void BuildVisuals()
    {
        whiteSprite = CreateWhiteSprite();
        foilShader = Shader.Find("Custom/CardFoilUI");
        Canvas existingCanvas = FindNamedCanvas("EcosystemCanvas");
        if (existingCanvas != null)
        {
            if (BindExistingVisuals(existingCanvas.gameObject))
            {
                EnsureGameplayBrandLogo(leftPanelRect);
                return;
            }

            ClearVisualCaches();
            if (Application.isPlaying)
            {
                Destroy(existingCanvas.gameObject);
            }
            else
            {
                DestroyImmediate(existingCanvas.gameObject);
            }
        }

        GameObject c = new GameObject("EcosystemCanvas");
        canvas = c.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = sceneCamera;
        canvas.planeDistance = 1f;
        CanvasScaler scaler = c.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        c.AddComponent<GraphicRaycaster>();

        GameObject bg = Panel("BG", canvas.transform, new Color(0.02f, 0.03f, 0.03f, 0.02f));
        Stretch(bg.GetComponent<RectTransform>());
        GameObject felt = Panel("Felt", canvas.transform, new Color(0.17f, 0.28f, 0.2f, 0.02f));
        Place(felt.GetComponent<RectTransform>(), new Vector2(0.19f, 0.02f), new Vector2(0.995f, 0.99f), Vector2.zero, Vector2.zero);
        GameObject vignetteTop = Panel("VignetteTop", canvas.transform, new Color(0.02f, 0.03f, 0.03f, 0.04f));
        Place(vignetteTop.GetComponent<RectTransform>(), new Vector2(0f, 0.88f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        GameObject vignetteBottom = Panel("VignetteBottom", canvas.transform, new Color(0.02f, 0.03f, 0.03f, 0.06f));
        Place(vignetteBottom.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 0.16f), Vector2.zero, Vector2.zero);

        GameObject left = Panel("Left", canvas.transform, new Color(0.05f, 0.08f, 0.09f, 0.6f));
        Place(left.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0.15f, 1f), Vector2.zero, Vector2.zero);
        leftPanelRect = left.GetComponent<RectTransform>();
        RectTransform headerCardRect = EnsureLeftHudHeaderCard(left.transform);
        TextMeshProUGUI title = Label("Title", headerCardRect.transform, 30, FontStyles.Bold, TextAlignmentOptions.TopLeft);
        Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(18f, -18f), new Vector2(-18f, -58f));
        title.text = "Glass World";
        title.color = new Color(0.12f, 0.2f, 0.19f);
        TextMeshProUGUI desc = Label("Desc", headerCardRect.transform, 14, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        Place(desc.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(18f, -66f), new Vector2(-18f, -118f));
        desc.text = "Roll 1d6 each day to earn points. Play 1 unlocked card, then draw 1 replacement.";
        desc.color = new Color(0.29f, 0.38f, 0.37f);
        EnsureGameplayBrandLogo(headerCardRect);

        GameObject statsCard = Panel("StatsCard", left.transform, new Color(1f, 1f, 1f, 0.05f));
        Place(statsCard.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(14f, -208f), new Vector2(-14f, -414f));
        StyleLeftHudCard(statsCard, ThemePanelKind.Medium, new Color(0.98f, 0.99f, 0.97f, 0.96f), new Color(0.12f, 0.2f, 0.19f, 0.14f));
        statsText = Label("Stats", statsCard.transform, 15, FontStyles.Bold, TextAlignmentOptions.TopLeft);
        statsText.richText = true;
        Place(statsText.rectTransform, Vector2.zero, Vector2.one, new Vector2(16f, 14f), new Vector2(-16f, -42f));
        statsText.color = new Color(0.16f, 0.21f, 0.2f);

        // Stable days progress bar (inside stats card, pinned to bottom)
        GameObject stableBarTrack = Panel("StableBarTrack", statsCard.transform, new Color(0f, 0f, 0f, 0.28f));
        Place(stableBarTrack.GetComponent<RectTransform>(), Vector2.zero, new Vector2(1f, 0f), new Vector2(14f, 28f), new Vector2(-14f, 38f));
        stableBarTrack.GetComponent<Image>().raycastTarget = false;
        GameObject stableFillGo = Panel("StableFill", statsCard.transform, new Color(0.44f, 0.88f, 0.66f, 0.85f));
        Image stableFillImg = stableFillGo.GetComponent<Image>();
        stableFillImg.type = Image.Type.Filled;
        stableFillImg.fillMethod = Image.FillMethod.Horizontal;
        stableFillImg.fillAmount = 0f;
        stableFillImg.raycastTarget = false;
        Place(stableFillGo.GetComponent<RectTransform>(), Vector2.zero, new Vector2(1f, 0f), new Vector2(14f, 28f), new Vector2(-14f, 38f));
        stableProgressFill = stableFillImg;

        // Nitrate danger bar (inside stats card, just above stable bar)
        GameObject nitrateBarTrack = Panel("NitrateBarTrack", statsCard.transform, new Color(0f, 0f, 0f, 0.28f));
        Place(nitrateBarTrack.GetComponent<RectTransform>(), Vector2.zero, new Vector2(1f, 0f), new Vector2(14f, 14f), new Vector2(-14f, 24f));
        nitrateBarTrack.GetComponent<Image>().raycastTarget = false;
        GameObject nitrateFillGo = Panel("NitrateFill", statsCard.transform, new Color(0.42f, 0.88f, 0.58f, 0.85f));
        Image nitrateFillImg = nitrateFillGo.GetComponent<Image>();
        nitrateFillImg.type = Image.Type.Filled;
        nitrateFillImg.fillMethod = Image.FillMethod.Horizontal;
        nitrateFillImg.fillAmount = 0f;
        nitrateFillImg.raycastTarget = false;
        Place(nitrateFillGo.GetComponent<RectTransform>(), Vector2.zero, new Vector2(1f, 0f), new Vector2(14f, 14f), new Vector2(-14f, 24f));
        nitrateBarFill = nitrateFillImg;

        GameObject warningCard = Panel("WarningCard", left.transform, new Color(1f, 0.84f, 0.3f, 0.06f));
        Place(warningCard.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(14f, -426f), new Vector2(-14f, -536f));
        StyleLeftHudCard(warningCard, ThemePanelKind.Notice, new Color(0.99f, 0.97f, 0.92f, 0.98f), new Color(0.38f, 0.24f, 0.08f, 0.16f));
        warningCardImage = warningCard.GetComponent<Image>();
        warningText = Label("Warnings", warningCard.transform, 14, FontStyles.Bold, TextAlignmentOptions.TopLeft);
        Place(warningText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, -14f), new Vector2(-16f, -60f));
        warningText.color = new Color(0.46f, 0.26f, 0.1f);
        eventText = Label("Event", warningCard.transform, 12, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        Place(eventText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(16f, 28f), new Vector2(-16f, 50f));
        eventText.color = new Color(0.28f, 0.34f, 0.35f);
        selectedText = Label("Selected", warningCard.transform, 13, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        Place(selectedText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(16f, 8f), new Vector2(-16f, 26f));
        selectedText.color = new Color(0.22f, 0.32f, 0.29f);
        GameObject reportCard = Panel("ReportCard", left.transform, new Color(1f, 1f, 1f, 0.04f));
        Place(reportCard.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(14f, 14f), new Vector2(-14f, 172f));
        StyleLeftHudCard(reportCard, ThemePanelKind.Small, new Color(0.97f, 0.99f, 0.98f, 0.95f), new Color(0.12f, 0.2f, 0.19f, 0.12f));
        reportText = Label("Report", reportCard.transform, 13, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        Place(reportText.rectTransform, Vector2.zero, Vector2.one, new Vector2(16f, 12f), new Vector2(-16f, -12f));
        reportText.color = new Color(0.22f, 0.29f, 0.29f);

        playButtonGlow = Panel("PlayButtonGlow", left.transform, new Color(0.38f, 0.94f, 0.62f, 0f));
        Place(playButtonGlow.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(12f, 180f), new Vector2(-12f, 240f));
        playButtonGlow.GetComponent<Image>().raycastTarget = false;
        Button next = CreateUiButton("Play Selected Card", left.transform, new Color(0.38f, 0.74f, 0.51f), HandlePrimaryAction);
        Place(next.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(18f, 184f), new Vector2(-18f, 236f));
        primaryActionButton = next;
        Button reroll = CreateUiButton("Re-roll Die", left.transform, new Color(0.88f, 0.82f, 0.46f), RerollDie);
        Place(reroll.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(18f, 244f), new Vector2(-18f, 296f));
        rerollButton = reroll;

        GameObject right = Panel("Right", canvas.transform, new Color(0.2f, 0.3f, 0.24f, 0.005f));
        Place(right.GetComponent<RectTransform>(), new Vector2(0.15f, 0.015f), new Vector2(0.998f, 0.988f), new Vector2(8f, 0f), new Vector2(0f, 0f));
        rightPanelRect = right.GetComponent<RectTransform>();
        Outline o = right.AddComponent<Outline>();
        o.effectColor = new Color(0.16f, 0.28f, 0.22f, 0.8f);
        o.effectDistance = new Vector2(4f, 4f);

        GameObject bannerBg = Panel("BannerBg", right.transform, new Color(0.06f, 0.11f, 0.1f, 0.68f));
        Place(bannerBg.GetComponent<RectTransform>(), new Vector2(0.06f, 1f), new Vector2(0.94f, 1f), new Vector2(0f, -14f), new Vector2(0f, -58f));
        bannerBg.GetComponent<Image>().raycastTarget = false;
        bannerText = Label("Banner", right.transform, 22, FontStyles.Bold, TextAlignmentOptions.Center);
        Place(bannerText.rectTransform, new Vector2(0.06f, 1f), new Vector2(0.94f, 1f), new Vector2(0f, -18f), new Vector2(0f, -54f));
        bannerText.color = new Color(0.88f, 0.96f, 0.9f);
        deckText = Label("Deck", right.transform, 14, FontStyles.Bold, TextAlignmentOptions.TopLeft);
        Place(deckText.rectTransform, new Vector2(0f, 1f), new Vector2(0.24f, 1f), new Vector2(24f, -68f), new Vector2(-8f, -104f));
        deckText.color = new Color(0.14f, 0.2f, 0.17f);
        drawPileMarker = CreatePileMarker("DrawPileMarker", right.transform, new Vector2(110f, -126f), "Draw");
        discardPileMarker = CreatePileMarker("DiscardPileMarker", right.transform, new Vector2(218f, -126f), "Discard");
        speciesText = Label("Species", right.transform, 14, FontStyles.Bold, TextAlignmentOptions.TopRight);
        Place(speciesText.rectTransform, new Vector2(0.72f, 1f), new Vector2(1f, 1f), new Vector2(0f, -68f), new Vector2(-124f, -112f));
        speciesText.color = new Color(0.14f, 0.2f, 0.17f);
        Button pauseButton = CreateUiButton("Pause", right.transform, new Color(0.82f, 0.84f, 0.74f), TogglePause);
        Place(pauseButton.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-118f, -18f), new Vector2(-24f, -54f));
        water = null;
        lightGlow = null;
        playFlash = null;

        TextMeshProUGUI handLabel = Label("HandLabel", right.transform, 20, FontStyles.Bold, TextAlignmentOptions.TopLeft);
        Place(handLabel.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(120f, 136f), new Vector2(-120f, 174f));
        handLabel.text = "Today's Hand";
        handLabel.color = new Color(0.13f, 0.2f, 0.16f);
        CreateCardSlots(right.transform);

        tooltipPanel = Panel("Tooltip", canvas.transform, new Color(0.06f, 0.11f, 0.12f, 0.94f));
        Place(tooltipPanel.GetComponent<RectTransform>(), new Vector2(0.52f, 0.02f), new Vector2(0.985f, 0.195f), Vector2.zero, Vector2.zero);
        UiThemeStyler.ApplyPanel(tooltipPanel.GetComponent<Image>(), ThemePanelKind.Small, new Color(0.92f, 0.96f, 0.94f, 0.97f));
        tooltipText = Label("TooltipText", tooltipPanel.transform, 14, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        tooltipText.richText = true;
        Place(tooltipText.rectTransform, Vector2.zero, Vector2.one, new Vector2(18f, 10f), new Vector2(-18f, -10f));
        tooltipText.color = new Color(0.12f, 0.17f, 0.2f);
        tooltipText.textWrappingMode = TextWrappingModes.Normal;
        tooltipPanel.SetActive(false);
        menuPanel = null;

        resultPanel = Panel("Result", canvas.transform, new Color(0.03f, 0.05f, 0.07f, 0.76f));
        Stretch(resultPanel.GetComponent<RectTransform>());
        GameObject resultCard = Panel("ResultCard", resultPanel.transform, new Color(1f, 1f, 1f, 1f));
        Place(resultCard.GetComponent<RectTransform>(), new Vector2(0.26f, 0.16f), new Vector2(0.74f, 0.76f), Vector2.zero, Vector2.zero);
        UiThemeStyler.ApplyPanel(resultCard.GetComponent<Image>(), ThemePanelKind.Large, new Color(1f, 1f, 1f, 0.98f));
        resultText = Label("ResultText", resultCard.transform, 28, FontStyles.Bold, TextAlignmentOptions.Center);
        Place(resultText.rectTransform, new Vector2(0.06f, 0.3f), new Vector2(0.94f, 0.9f), Vector2.zero, Vector2.zero);
        resultText.color = new Color(0.21f, 0.15f, 0.09f);
        Button again = CreateUiButton("Play Again", resultCard.transform, new Color(0.42f, 0.75f, 0.94f), StartGame);
        Place(again.GetComponent<RectTransform>(), new Vector2(0.52f, 0.06f), new Vector2(0.9f, 0.22f), Vector2.zero, Vector2.zero);
        Button settings = CreateUiButton("Change Settings", resultCard.transform, new Color(0.82f, 0.84f, 0.74f), GoToSetup);
        Place(settings.GetComponent<RectTransform>(), new Vector2(0.1f, 0.06f), new Vector2(0.48f, 0.22f), Vector2.zero, Vector2.zero);
        resultPanel.SetActive(false);

        pausePanel = Panel("Pause", canvas.transform, new Color(0.03f, 0.05f, 0.07f, 0.82f));
        Stretch(pausePanel.GetComponent<RectTransform>());
        GameObject pauseCard = Panel("PauseCard", pausePanel.transform, new Color(1f, 1f, 1f, 1f));
        Place(pauseCard.GetComponent<RectTransform>(), new Vector2(0.30f, 0.15f), new Vector2(0.70f, 0.80f), Vector2.zero, Vector2.zero);
        UiThemeStyler.ApplyPanel(pauseCard.GetComponent<Image>(), ThemePanelKind.Large, new Color(1f, 1f, 1f, 0.98f));

        TextMeshProUGUI pauseTitle = Label("PauseTitle", pauseCard.transform, 42, FontStyles.Bold, TextAlignmentOptions.Center);
        Place(pauseTitle.rectTransform, new Vector2(0.1f, 1f), new Vector2(0.9f, 1f), new Vector2(0f, -52f), new Vector2(0f, -8f));
        pauseTitle.text = "Game Paused";
        pauseTitle.color = new Color(0.21f, 0.15f, 0.09f);

        pauseStatusText = Label("PauseStatus", pauseCard.transform, 17, FontStyles.Normal, TextAlignmentOptions.Center);
        Place(pauseStatusText.rectTransform, new Vector2(0.1f, 1f), new Vector2(0.9f, 1f), new Vector2(0f, -100f), new Vector2(0f, -58f));
        pauseStatusText.color = new Color(0.28f, 0.38f, 0.44f);

        Button resumeButton = CreateUiButton("Resume Game", pauseCard.transform, new Color(0.34f, 0.78f, 0.5f), TogglePause);
        Place(resumeButton.GetComponent<RectTransform>(), new Vector2(0.1f, 1f), new Vector2(0.9f, 1f), new Vector2(0f, -176f), new Vector2(0f, -116f));

        Button pauseRestartButton = CreateUiButton("Restart Run", pauseCard.transform, new Color(0.88f, 0.52f, 0.3f), RestartFromPause);
        Place(pauseRestartButton.GetComponent<RectTransform>(), new Vector2(0.1f, 1f), new Vector2(0.9f, 1f), new Vector2(0f, -244f), new Vector2(0f, -184f));

        TextMeshProUGUI restartWarning = Label("RestartWarning", pauseCard.transform, 13, FontStyles.Normal, TextAlignmentOptions.Center);
        Place(restartWarning.rectTransform, new Vector2(0.1f, 1f), new Vector2(0.9f, 1f), new Vector2(0f, -278f), new Vector2(0f, -248f));
        restartWarning.text = "Restarting will lose all current progress.";
        restartWarning.color = new Color(0.55f, 0.3f, 0.18f);

        Button quitButton = CreateUiButton("Quit to Desktop", pauseCard.transform, new Color(0.68f, 0.68f, 0.7f), QuitGame);
        Place(quitButton.GetComponent<RectTransform>(), new Vector2(0.2f, 0f), new Vector2(0.8f, 0f), new Vector2(0f, 32f), new Vector2(0f, 80f));
        pausePanel.SetActive(false);
    }

    private void TogglePause()
    {
        if (state != GameState.Playing || isResolvingCard || pausePanel == null)
        {
            return;
        }

        isPaused = !isPaused;
        pausePanel.SetActive(isPaused);
        if (isPaused && pauseStatusText != null)
        {
            DifficultySettings s = GetDifficultySettings();
            pauseStatusText.text = jarName + "  ·  " + difficulty + "  ·  Day " + day
                + "\nStable days: " + stableDays + " / " + s.StableDaysToWin
                + "   Nitrates: " + nitrateLevel.ToString("0") + " / 100";
        }
    }

    private void RestartFromPause()
    {
        isPaused = false;
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        StartGame();
    }

    private void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#endif
    }

    private void GoToSetup()
    {
        isPaused = false;
        state = GameState.Menu;
        SceneManager.LoadScene("main");
    }

    private bool BindExistingVisuals(GameObject canvasObject)
    {
        ClearVisualCaches();
        canvas = canvasObject.GetComponent<Canvas>();
        leftPanelRect = FindRect(canvasObject.transform, "Left");
        rightPanelRect = FindRect(canvasObject.transform, "Right");
        RectTransform legacyJar = FindRect(canvasObject.transform, "Jar");
        if (legacyJar != null)
        {
            CleanupLegacyUiObject(legacyJar.gameObject);
        }
        RectTransform legacyAnchor = FindRect(canvasObject.transform, "JarAnchor");
        if (legacyAnchor != null)
        {
            CleanupLegacyUiObject(legacyAnchor.gameObject);
        }
        drawPileMarker = FindRect(canvasObject.transform, "DrawPileMarker");
        discardPileMarker = FindRect(canvasObject.transform, "DiscardPileMarker");
        water = FindImage(canvasObject.transform, "Water");
        lightGlow = FindImage(canvasObject.transform, "LightGlow");
        playFlash = FindImage(canvasObject.transform, "PlayFlash");
        GameObject legacyMenu = FindObject(canvasObject.transform, "Menu");
        if (legacyMenu != null)
        {
            CleanupLegacyUiObject(legacyMenu);
        }
        GameObject legacyRestartButton = FindObject(canvasObject.transform, "Restart RunButton");
        if (legacyRestartButton != null)
        {
            CleanupLegacyUiObject(legacyRestartButton);
        }
        menuPanel = null;
        resultPanel = FindObject(canvasObject.transform, "Result");
        pausePanel = FindObject(canvasObject.transform, "Pause");
        tooltipPanel = FindObject(canvasObject.transform, "Tooltip");
        bannerText = FindText(canvasObject.transform, "Banner");
        statsText = FindText(canvasObject.transform, "Stats");
        warningText = FindText(canvasObject.transform, "Warnings");
        eventText = FindText(canvasObject.transform, "Event");
        reportText = FindText(canvasObject.transform, "Report");
        selectedText = FindText(canvasObject.transform, "Selected");
        deckText = FindText(canvasObject.transform, "Deck");
        speciesText = FindText(canvasObject.transform, "Species");
        resultText = FindText(canvasObject.transform, "ResultText");
        tooltipText = FindText(canvasObject.transform, "TooltipText");
        primaryActionButton = FindObject(canvasObject.transform, "Play Selected CardButton")?.GetComponent<Button>();
        rerollButton = FindObject(canvasObject.transform, "Re-roll DieButton")?.GetComponent<Button>();

        if (canvas == null || leftPanelRect == null || rightPanelRect == null || bannerText == null || resultPanel == null || pausePanel == null)
        {
            return false;
        }
        ApplyThemeToExistingCanvas(canvasObject.transform);
        Image bgImage = FindImage(canvasObject.transform, "BG");
        if (bgImage != null) bgImage.color = new Color(0.02f, 0.03f, 0.03f, 0.02f);
        Image feltImage = FindImage(canvasObject.transform, "Felt");
        if (feltImage != null) feltImage.color = new Color(0.17f, 0.28f, 0.2f, 0.02f);
        Image rightImage = FindImage(canvasObject.transform, "Right");
        if (rightImage != null) rightImage.color = new Color(0.2f, 0.3f, 0.24f, 0.005f);
        Image leftImage = FindImage(canvasObject.transform, "Left");
        if (leftImage != null) leftImage.color = new Color(0.06f, 0.1f, 0.1f, 0.78f);
        RectTransform headerCardRect = EnsureLeftHudHeaderCard(leftPanelRect);
        TextMeshProUGUI title = FindText(canvasObject.transform, "Title");
        if (title != null)
        {
            title.rectTransform.SetParent(headerCardRect, false);
            Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(18f, -18f), new Vector2(-18f, -58f));
            title.color = new Color(0.12f, 0.2f, 0.19f);
        }
        TextMeshProUGUI desc = FindText(canvasObject.transform, "Desc");
        if (desc != null)
        {
            desc.rectTransform.SetParent(headerCardRect, false);
            Place(desc.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(18f, -66f), new Vector2(-18f, -118f));
            desc.color = new Color(0.29f, 0.38f, 0.37f);
        }
        EnsureGameplayBrandLogo(headerCardRect);
        RectTransform statsCardRect = FindRect(canvasObject.transform, "StatsCard");
        if (statsCardRect != null)
        {
            Place(statsCardRect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(14f, -208f), new Vector2(-14f, -414f));
            StyleLeftHudCard(statsCardRect.gameObject, ThemePanelKind.Medium, new Color(0.98f, 0.99f, 0.97f, 0.96f), new Color(0.12f, 0.2f, 0.19f, 0.14f));
        }
        RectTransform statsRect = FindRect(canvasObject.transform, "Stats");
        if (statsRect != null)
        {
            if (statsCardRect != null) statsRect.SetParent(statsCardRect, false);
            Place(statsRect, Vector2.zero, Vector2.one, new Vector2(16f, 14f), new Vector2(-16f, -42f));
            TextMeshProUGUI stats = statsRect.GetComponent<TextMeshProUGUI>();
            if (stats != null) stats.color = new Color(0.16f, 0.21f, 0.2f);
        }
        RectTransform warningCardRect = FindRect(canvasObject.transform, "WarningCard");
        if (warningCardRect != null)
        {
            Place(warningCardRect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(14f, -426f), new Vector2(-14f, -536f));
            StyleLeftHudCard(warningCardRect.gameObject, ThemePanelKind.Notice, new Color(0.99f, 0.97f, 0.92f, 0.98f), new Color(0.38f, 0.24f, 0.08f, 0.16f));
        }
        RectTransform warningRect = FindRect(canvasObject.transform, "Warnings");
        if (warningRect != null)
        {
            if (warningCardRect != null) warningRect.SetParent(warningCardRect, false);
            Place(warningRect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, -14f), new Vector2(-16f, -60f));
        }
        RectTransform eventRect = FindRect(canvasObject.transform, "Event");
        if (eventRect != null)
        {
            if (warningCardRect != null) eventRect.SetParent(warningCardRect, false);
            Place(eventRect, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(16f, 28f), new Vector2(-16f, 50f));
            TextMeshProUGUI eventLabel = eventRect.GetComponent<TextMeshProUGUI>();
            if (eventLabel != null) eventLabel.color = new Color(0.28f, 0.34f, 0.35f);
        }
        RectTransform selectedRect = FindRect(canvasObject.transform, "Selected");
        if (selectedRect != null)
        {
            if (warningCardRect != null) selectedRect.SetParent(warningCardRect, false);
            Place(selectedRect, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(16f, 8f), new Vector2(-16f, 26f));
            TextMeshProUGUI selectedLabel = selectedRect.GetComponent<TextMeshProUGUI>();
            if (selectedLabel != null) selectedLabel.color = new Color(0.22f, 0.32f, 0.29f);
        }
        RectTransform reportCardRect = FindRect(canvasObject.transform, "ReportCard");
        if (reportCardRect != null)
        {
            StyleLeftHudCard(reportCardRect.gameObject, ThemePanelKind.Small, new Color(0.97f, 0.99f, 0.98f, 0.95f), new Color(0.12f, 0.2f, 0.19f, 0.12f));
        }
        RectTransform reportRect = FindRect(canvasObject.transform, "Report");
        if (reportRect != null)
        {
            if (reportCardRect != null) reportRect.SetParent(reportCardRect, false);
            Place(reportRect, Vector2.zero, Vector2.one, new Vector2(16f, 12f), new Vector2(-16f, -12f));
            TextMeshProUGUI reportLabel = reportRect.GetComponent<TextMeshProUGUI>();
            if (reportLabel != null) reportLabel.color = new Color(0.22f, 0.29f, 0.29f);
        }
        for (int i = 0; i < 3; i++)
        {
            RectTransform slotRect = FindRect(canvasObject.transform, "Card" + i);
            if (slotRect == null)
            {
                continue;
            }

            cardRoots.Add(slotRect);
            EcosystemCardPrefabView view = slotRect.GetComponent<EcosystemCardPrefabView>();
            if (view == null)
            {
                view = slotRect.gameObject.AddComponent<EcosystemCardPrefabView>();
            }

            view.Initialize();
            if (!view.HasTemplateCard)
            {
                return false;
            }
            cardViews.Add(view);
            Button cardButton = view.SelectButton;
            int index = cardButtons.Count;
            cardButton.onClick.RemoveAllListeners();
            cardButton.onClick.AddListener(delegate { ToggleCard(index); });
            cardButtons.Add(cardButton);
            cardShines.Add(view.ShineImage);
            CanvasGroup canvasGroup = slotRect.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = slotRect.gameObject.AddComponent<CanvasGroup>();
            }
            cardCanvasGroups.Add(canvasGroup);
        }

        if (cardRoots.Count < 3 || cardButtons.Count < 3)
        {
            return false;
        }

        RebindNamedButton(canvasObject.transform, "Play Selected CardButton", HandlePrimaryAction);
        RebindNamedButton(canvasObject.transform, "Re-roll DieButton", RerollDie);
        RebindNamedButton(canvasObject.transform, "PauseButton", TogglePause);
        RebindNamedButton(canvasObject.transform, "ResumeButton", TogglePause);
        RebindNamedButton(canvasObject.transform, "Restart RunButton", RestartFromPause);
        RebindNamedButton(canvasObject.transform, "QuitButton", QuitGame);
        RebindNamedButton(canvasObject.transform, "Start PrototypeButton", StartGame);
        RebindNamedButton(canvasObject.transform, "Play AgainButton", StartGame);
        return true;
    }

    private void CleanupLegacyUiObject(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(target);
            return;
        }

        target.SetActive(false);
        target.hideFlags = HideFlags.HideInHierarchy;
        target.name = "__LegacyHidden__" + target.name;
    }

    private RectTransform EnsureLeftHudHeaderCard(Transform leftRoot)
    {
        RectTransform headerCardRect = FindRect(leftRoot, "HeaderCard");
        if (headerCardRect == null)
        {
            GameObject headerCard = Panel("HeaderCard", leftRoot, new Color(1f, 1f, 1f, 0.08f));
            headerCardRect = headerCard.GetComponent<RectTransform>();
        }

        Place(headerCardRect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(14f, -14f), new Vector2(-14f, -194f));
        StyleLeftHudCard(headerCardRect.gameObject, ThemePanelKind.Medium, new Color(0.97f, 0.99f, 0.97f, 0.97f), new Color(0.12f, 0.2f, 0.19f, 0.14f));
        return headerCardRect;
    }

    private static void StyleLeftHudCard(GameObject card, ThemePanelKind kind, Color tint, Color outlineColor)
    {
        if (card == null)
        {
            return;
        }

        Image image = card.GetComponent<Image>();
        if (image != null)
        {
            UiThemeStyler.ApplyPanel(image, kind, tint);
        }

        Outline outline = card.GetComponent<Outline>();
        if (outline == null)
        {
            outline = card.AddComponent<Outline>();
        }

        outline.effectColor = outlineColor;
        outline.effectDistance = new Vector2(2f, -2f);
    }

    private void EnsureGameplayBrandLogo(RectTransform parent)
    {
        if (parent == null)
        {
            return;
        }

        Transform searchRoot = parent.parent != null ? parent.parent : parent;
        List<GameObject> existingPlates = new List<GameObject>();
        Transform[] existingTransforms = searchRoot.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < existingTransforms.Length; i++)
        {
            if (existingTransforms[i] != null && existingTransforms[i].name == "BrandLogoPlate")
            {
                existingPlates.Add(existingTransforms[i].gameObject);
            }
        }

        for (int i = 0; i < existingPlates.Count; i++)
        {
            if (Application.isPlaying)
            {
                Destroy(existingPlates[i]);
            }
            else
            {
                DestroyImmediate(existingPlates[i]);
            }
        }

        GameObject logoPlate = Panel("BrandLogoPlate", parent, new Color(0.94f, 0.98f, 0.96f, 0.9f));
        Place(logoPlate.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(14f, -126f), new Vector2(-14f, -194f));
        UiThemeStyler.ApplyPanel(logoPlate.GetComponent<Image>(), ThemePanelKind.Small, new Color(0.94f, 0.98f, 0.96f, 0.9f));

        Outline outline = logoPlate.AddComponent<Outline>();
        outline.effectColor = new Color(0.11f, 0.21f, 0.19f, 0.22f);
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

        Place(logoImage.rectTransform, Vector2.zero, Vector2.one, new Vector2(8f, 6f), new Vector2(-8f, -6f));
    }

    private void EnsurePresentationWorld()
    {
        bool createdCamera = false;
        if (sceneCamera == null)
        {
            Camera existing = Camera.main;
            if (existing != null)
            {
                sceneCamera = existing;
            }
            else
            {
                GameObject cameraObject = new GameObject("CardTableCamera");
                sceneCamera = cameraObject.AddComponent<Camera>();
                createdCamera = true;
            }
        }

        sceneCamera.orthographic = false;
        sceneCamera.fieldOfView = 34f;
        sceneCamera.nearClipPlane = 0.1f;
        sceneCamera.farClipPlane = 200f;
        sceneCamera.clearFlags = CameraClearFlags.SolidColor;
        sceneCamera.backgroundColor = new Color(0.08f, 0.11f, 0.1f);
        if (createdCamera)
        {
            MoveCameraImmediate(GetJarCameraPosition(), GetJarCameraRotation());
        }
        if (sceneCamera.GetComponent<UniversalAdditionalCameraData>() == null)
        {
            sceneCamera.gameObject.AddComponent<UniversalAdditionalCameraData>();
        }

        GameObject existingTableLight = GameObject.Find("CardTableLight");
        if (existingTableLight == null)
        {
            GameObject lightObject = new GameObject("CardTableLight");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.color = new Color(1f, 0.96f, 0.9f);
            light.shadows = LightShadows.Soft;
            lightObject.transform.rotation = Quaternion.Euler(44f, -26f, 0f);
            tableKeyLight = light;
        }
        else
        {
            tableKeyLight = existingTableLight.GetComponent<Light>();
        }

        GameObject existingFillLight = GameObject.Find("JarFillLight");
        if (existingFillLight == null)
        {
            GameObject fillLightObject = new GameObject("JarFillLight");
            Light fillLight = fillLightObject.AddComponent<Light>();
            fillLight.type = LightType.Point;
            fillLight.intensity = 4.5f;
            fillLight.range = 12f;
            fillLight.color = new Color(0.72f, 0.9f, 1f);
            fillLightObject.transform.position = new Vector3(4.4f, 1.8f, 5.2f);
            jarFillLight = fillLight;
        }
        else
        {
            jarFillLight = existingFillLight.GetComponent<Light>();
        }

        if (GameObject.Find("CardTableSurface") == null)
        {
            GameObject table = GameObject.CreatePrimitive(PrimitiveType.Plane);
            table.name = "CardTableSurface";
            table.transform.position = new Vector3(0f, -1.75f, 5.5f);
            table.transform.localScale = new Vector3(2f, 1f, 2f);
            Renderer renderer = table.GetComponent<Renderer>();
            if (renderer != null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader != null)
                {
                    Material material = new Material(shader);
                    material.color = new Color(0.19f, 0.31f, 0.24f);
                    renderer.sharedMaterial = material;
                }
            }
        }

        if (GameObject.Find("BackdropWall") == null)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Quad);
            wall.name = "BackdropWall";
            wall.transform.position = new Vector3(0f, 1.2f, 16f);
            wall.transform.localScale = new Vector3(18f, 10f, 1f);
            Renderer renderer = wall.GetComponent<Renderer>();
            if (renderer != null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader != null)
                {
                    Material material = new Material(shader);
                    material.color = new Color(0.11f, 0.14f, 0.14f);
                    renderer.sharedMaterial = material;
                }
            }
        }

        if (GameObject.Find("RuntimePostVolume") == null)
        {
            GameObject volumeObject = new GameObject("RuntimePostVolume");
            Volume volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 100f;
            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
            volume.sharedProfile = profile;

            Bloom bloom = profile.Add<Bloom>(true);
            bloom.active = true;
            bloom.intensity.Override(0.15f);
            bloom.threshold.Override(0.9f);
            bloom.scatter.Override(0.8f);

            Vignette vignette = profile.Add<Vignette>(true);
            vignette.active = true;
            vignette.intensity.Override(0.22f);
            vignette.smoothness.Override(0.65f);

            ColorAdjustments colorAdjustments = profile.Add<ColorAdjustments>(true);
            colorAdjustments.active = true;
            colorAdjustments.postExposure.Override(0.05f);
            colorAdjustments.contrast.Override(10f);
            colorAdjustments.saturation.Override(8f);
        }

        EnsureJarWorld();
        EnsureDiceStage();
        if (jarWorldRoot != null && createdCamera)
        {
            MoveCameraImmediate(GetJarCameraPosition(), GetJarCameraRotation());
        }
    }

    private void CreateCardSlots(Transform parent)
    {
        for (int i = 0; i < 3; i++)
        {
            GameObject slot = nueCardPrefab != null ? Instantiate(nueCardPrefab, parent) : Panel("Card" + i, parent, new Color(0.97f, 0.95f, 0.9f, 0.98f));
            slot.name = "Card" + i;
            RectTransform slotRect = slot.GetComponent<RectTransform>();
            if (slotRect == null)
            {
                slotRect = slot.AddComponent<RectTransform>();
            }

            slotRect.SetParent(parent, false);
            slotRect.anchorMin = new Vector2(0.5f, 0f);
            slotRect.anchorMax = new Vector2(0.5f, 0f);
            slotRect.pivot = new Vector2(0.5f, 0f);
            slotRect.sizeDelta = new Vector2(244f, 342f);
            slotRect.anchoredPosition = new Vector2((i - 1) * 236f, 24f);
            cardRoots.Add(slotRect);
            CanvasGroup canvasGroup = slot.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = slot.AddComponent<CanvasGroup>();
            }
            canvasGroup.alpha = 1f;
            cardCanvasGroups.Add(canvasGroup);

            EcosystemCardPrefabView view = slot.GetComponent<EcosystemCardPrefabView>();
            if (view == null)
            {
                view = slot.AddComponent<EcosystemCardPrefabView>();
            }

            view.Initialize();
            if (foilShader != null && view.ShineImage != null)
            {
                view.ShineImage.material = new Material(foilShader);
            }

            cardViews.Add(view);
            cardShines.Add(view.ShineImage);

            Button b = view.SelectButton;
            int index = i;
            b.onClick.RemoveAllListeners();
            b.onClick.AddListener(delegate { ToggleCard(index); });
            cardButtons.Add(b);

            EventTrigger trigger = slot.GetComponent<EventTrigger>() ?? slot.AddComponent<EventTrigger>();
            trigger.triggers.Clear();
            EventTrigger.Entry enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(delegate { hoveredCardIndex = index; ShowCardTooltip(index); });
            trigger.triggers.Add(enter);
            EventTrigger.Entry exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(delegate { if (hoveredCardIndex == index) hoveredCardIndex = -1; if (tooltipPanel != null) tooltipPanel.SetActive(false); });
            trigger.triggers.Add(exit);
        }
    }

    private void ShowMenu()
    {
        isPaused = false;
        if (pausePanel != null) pausePanel.SetActive(false);
        StartGame();
    }

    private void StartGame()
    {
        isPaused = false;
        if (menuPanel != null) menuPanel.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
        state = GameState.Playing;
        difficulty = GameSettingsStore.HasSelection ? (DifficultyMode)GameSettingsStore.DifficultyIndex : DifficultyMode.Medium;
        startingJar = GameSettingsStore.HasSelection ? (StartingJar)GameSettingsStore.StartingJarIndex : StartingJar.Balanced;
        temperatureLevel = GameSettingsStore.HasSelection ? GameSettingsStore.TemperatureLevel : 1;
        DifficultySettings settings = GetDifficultySettings();
        day = 1;
        stableDays = 0;
        perfectDays = 0;
        rerollTokens = settings.StartRerolls;
        currentDieRoll = 1;
        fishHungryTurns = 0;
        snailStarvingTurns = 0;
        nitrateLevel = settings.StartNitrates;
        lightLevel = settings.StartLight;
        displayedLightLevel = lightLevel;
        bloomThreshold = settings.AlgaeWarning;
        stability = 70f;
        highNitrateDays = 0;
        algaeMemory = 0f;
        bloomFlash = 0f;
        previousTurnAlgaeCount = settings.StartAlgae;
        lastRandomEventSummary = "No random event yet.";
        latestMilestone = "No milestones yet.";
        jarName = "Jar-" + UnityEngine.Random.Range(10, 99);
        ClearOrganisms();
        BuildDeck();
        ApplyStartingJar();
        MoveCameraImmediate(GetDiceCameraPosition(), GetDiceCameraRotation());
        BeginRollPhase();
    }

    private DifficultySettings GetDifficultySettings()
    {
        if (difficulty == DifficultyMode.Easy)
        {
            return new DifficultySettings
            {
                StartFish = 4,
                StartSnails = 3,
                StartAlgae = 6,
                StartNitrates = 10f,
                StartLight = 45f,
                StartRerolls = 5,
                StableDaysToWin = 4,
                BaseAlgaeGrowth = 0.6f,
                LightGrowthPerPointAbove50 = 0.010f,
                NitrateGrowthPerPoint = 0.008f,
                MemoryBleed = 0.15f,
                AlgaeSoftCapStart = 12,
                AlgaeSoftCapMultiplier = 0.5f,
                NitrateWarning = 70f,
                NitrateCollapse = 90f,
                AlgaeWarning = 14,
                AlgaeCollapse = 20,
                StableNitrateMax = 60f,
                StableAlgaeMin = 1,
                StableAlgaeMax = 11,
                StableFishMin = 1,
                StableSnailMin = 1,
                FishWastePerTurn = 1.0f,
                FishHungryGraze = 1,
                FishStarveTurns = 3,
                FishStarveLoseOne = true,
                FishReproductionChancePerFish = 0.20f,
                FishReproductionMinAlgae = 3,
                SnailAlgaeEat = 1,
                SnailStarveThreshold = 1,
                SnailStarveTurns = 3,
                SnailStarveLoseOne = true,
                SnailReproductionChance = 0.15f,
                SnailReproductionMinAlgae = 6,
                PassiveNitrateDecay = 0.8f,
                FeedFishNitrateMultiplier = 1.2f,
                BloomFeedbackNitrates = 1.5f,
                UncommonUnlockRoll = 2,
                RareUnlockRoll = 4
            };
        }

        if (difficulty == DifficultyMode.Hard)
        {
            return new DifficultySettings
            {
                StartFish = 3,
                StartSnails = 2,
                StartAlgae = 7,
                StartNitrates = 35f,
                StartLight = 60f,
                StartRerolls = 1,
                StableDaysToWin = 7,
                BaseAlgaeGrowth = 1.1f,
                LightGrowthPerPointAbove50 = 0.020f,
                NitrateGrowthPerPoint = 0.016f,
                MemoryBleed = 0.35f,
                AlgaeSoftCapStart = 8,
                AlgaeSoftCapMultiplier = 0.4f,
                NitrateWarning = 55f,
                NitrateCollapse = 75f,
                AlgaeWarning = 10,
                AlgaeCollapse = 15,
                StableNitrateMax = 45f,
                StableAlgaeMin = 3,
                StableAlgaeMax = 7,
                StableFishMin = 2,
                StableSnailMin = 1,
                FishWastePerTurn = 1.5f,
                FishHungryGraze = 2,
                FishStarveTurns = 2,
                FishStarveLoseOne = false,
                FishReproductionChancePerFish = 0.08f,
                FishReproductionMinAlgae = 6,
                SnailAlgaeEat = 1,
                SnailStarveThreshold = 3,
                SnailStarveTurns = 1,
                SnailStarveLoseOne = false,
                SnailReproductionChance = 0.07f,
                SnailReproductionMinAlgae = 9,
                PassiveNitrateDecay = 0.3f,
                FeedFishNitrateMultiplier = 1.8f,
                BloomFeedbackNitrates = 4.0f,
                UncommonUnlockRoll = 4,
                RareUnlockRoll = 6
            };
        }

        return new DifficultySettings
        {
            StartFish = 3,
            StartSnails = 2,
            StartAlgae = 5,
            StartNitrates = 20f,
            StartLight = 50f,
            StartRerolls = 3,
            StableDaysToWin = 5,
            BaseAlgaeGrowth = 0.8f,
            LightGrowthPerPointAbove50 = 0.015f,
            NitrateGrowthPerPoint = 0.012f,
            MemoryBleed = 0.25f,
            AlgaeSoftCapStart = 10,
            AlgaeSoftCapMultiplier = 0.4f,
            NitrateWarning = 65f,
            NitrateCollapse = 85f,
            AlgaeWarning = 12,
            AlgaeCollapse = 18,
            StableNitrateMax = 55f,
            StableAlgaeMin = 2,
            StableAlgaeMax = 10,
            StableFishMin = 1,
            StableSnailMin = 1,
            FishWastePerTurn = 1.2f,
            FishHungryGraze = 1,
            FishStarveTurns = 2,
            FishStarveLoseOne = false,
            FishReproductionChancePerFish = 0.15f,
            FishReproductionMinAlgae = 4,
            SnailAlgaeEat = 1,
            SnailStarveThreshold = 2,
            SnailStarveTurns = 2,
            SnailStarveLoseOne = false,
            SnailReproductionChance = 0.12f,
            SnailReproductionMinAlgae = 7,
            PassiveNitrateDecay = 0.5f,
            FeedFishNitrateMultiplier = 1.5f,
            BloomFeedbackNitrates = 2.5f,
            UncommonUnlockRoll = 3,
            RareUnlockRoll = 5
        };
    }

    private void BuildDeckTemplate()
    {
        deckTemplate.Clear();
        cardLibrary.Clear();
        AddCard("Feed Fish", "Reset hunger. Feeding adds nitrate pressure this turn.", CardCategory.Fish, CardTier.Common, new Color(0.58f, 0.83f, 0.95f), false, t => { t.FeedFishPlayed = true; t.Notes.Add("Feed Fish reset fish hunger."); });
        AddCard("Big Feed", "A strong feed. Powerful, but nitrate-heavy.", CardCategory.Risk, CardTier.Rare, new Color(0.97f, 0.66f, 0.43f), true, t => { t.FeedFishPlayed = true; t.FeedWasteMultiplier = 3f; t.Notes.Add("Big Feed pushed nitrate output hard."); });
        AddCard("Add Fish", "Add 1 fish.", CardCategory.Fish, CardTier.Uncommon, new Color(0.54f, 0.75f, 0.94f), false, t => { t.AddFish += 1; });
        AddCard("Remove Fish", "Remove 1 fish.", CardCategory.Fish, CardTier.Uncommon, new Color(0.72f, 0.83f, 0.96f), false, t => { t.RemoveFish += 1; });
        AddCard("Add Snail", "Add 1 snail.", CardCategory.Snail, CardTier.Common, new Color(0.93f, 0.79f, 0.49f), false, t => { t.AddSnail += 1; });
        AddCard("Trim Algae", "Remove 2 algae.", CardCategory.Algae, CardTier.Common, new Color(0.54f, 0.82f, 0.46f), false, t => { t.AlgaeBonus -= 2; });
        AddCard("Algae Bloom", "Add 3 algae.", CardCategory.Algae, CardTier.Uncommon, new Color(0.61f, 0.83f, 0.51f), false, t => { t.AlgaeBonus += 3; });
        AddCard("Algae Purge", "Remove 5 algae, but 1 snail dies too.", CardCategory.Risk, CardTier.Rare, new Color(0.41f, 0.76f, 0.33f), true, t => { t.AlgaeBonus -= 5; t.RemoveSnail += 1; });
        AddCard("Dim Light", "Lower light by 10.", CardCategory.Light, CardTier.Common, new Color(0.8f, 0.83f, 0.65f), false, t => { t.LightDelta -= 10f; });
        AddCard("Boost Light", "Raise light by 10.", CardCategory.Light, CardTier.Uncommon, new Color(0.98f, 0.89f, 0.55f), false, t => { t.LightDelta += 10f; });
        AddCard("Water Change", "Reduce nitrates by 16.", CardCategory.Water, CardTier.Common, new Color(0.66f, 0.92f, 0.95f), false, t => { t.NitrateBonus -= 16f; });
        AddCard("Big Water Change", "Reduce nitrates by 32.", CardCategory.Water, CardTier.Uncommon, new Color(0.72f, 0.94f, 0.93f), false, t => { t.NitrateBonus -= 32f; });
        AddCard("Deep Clean", "Reduce nitrates by 64.", CardCategory.Water, CardTier.Rare, new Color(0.62f, 0.88f, 0.88f), false, t => { t.NitrateBonus -= 64f; });
        AddCard("Fertilise", "Add 2 algae and 24 nitrates.", CardCategory.Risk, CardTier.Rare, new Color(0.88f, 0.62f, 0.5f), true, t => { t.AlgaeBonus += 2; t.NitrateBonus += 24f; });
        AddCard("Random Event", "Trigger a jar event.", CardCategory.Risk, CardTier.Common, new Color(0.77f, 0.81f, 0.83f), false, t => { t.TriggerRandomEvent = true; });
    }

    private void AddCard(string name, string summary, CardCategory category, CardTier tier, Color color, bool risk, Action<TurnState> apply)
    {
        CardDef card = new CardDef { Name = name, Summary = summary, Category = category, Tier = tier, Color = color, Risk = risk, Apply = apply };
        deckTemplate.Add(card);
        cardLibrary[name] = card;
    }

    private void BuildDeck()
    {
        drawPile.Clear();
        discardPile.Clear();
        hand.Clear();
        selected.Clear();
        AddCopiesToDeck("Feed Fish", difficulty == DifficultyMode.Easy ? 4 : difficulty == DifficultyMode.Hard ? 2 : 3);
        AddCopiesToDeck("Water Change", difficulty == DifficultyMode.Easy ? 3 : difficulty == DifficultyMode.Hard ? 2 : 2);
        AddCopiesToDeck("Big Water Change", difficulty == DifficultyMode.Easy ? 2 : 1);
        AddCopiesToDeck("Deep Clean", difficulty == DifficultyMode.Easy ? 2 : difficulty == DifficultyMode.Medium ? 1 : 0);
        AddCopiesToDeck("Random Event", difficulty == DifficultyMode.Easy ? 1 : difficulty == DifficultyMode.Medium ? 2 : 3);
        AddCopiesToDeck("Trim Algae", difficulty == DifficultyMode.Easy ? 3 : difficulty == DifficultyMode.Hard ? 2 : 2);
        AddCopiesToDeck("Algae Purge", 1);
        AddCopiesToDeck("Big Feed", difficulty == DifficultyMode.Hard ? 2 : 1);
        AddCopiesToDeck("Fertilise", difficulty == DifficultyMode.Hard ? 2 : 1);
        AddCopiesToDeck("Add Fish", difficulty == DifficultyMode.Hard ? 1 : 2);
        AddCopiesToDeck("Remove Fish", difficulty == DifficultyMode.Hard ? 1 : 2);
        AddCopiesToDeck("Add Snail", difficulty == DifficultyMode.Hard ? 1 : 2);
        AddCopiesToDeck("Dim Light", difficulty == DifficultyMode.Hard ? 1 : 2);
        AddCopiesToDeck("Boost Light", difficulty == DifficultyMode.Easy ? 2 : 1);
        AddCopiesToDeck("Algae Bloom", 1);
        Shuffle(drawPile);
    }

    private void AddCopiesToDeck(string cardName, int copies)
    {
        if (copies <= 0 || !cardLibrary.ContainsKey(cardName)) return;
        for (int i = 0; i < copies; i++) drawPile.Add(cardLibrary[cardName]);
    }

    private void RollDie()
    {
        currentDieRoll = UnityEngine.Random.Range(1, 7);
    }

    private void RerollDie()
    {
        if (state != GameState.Playing || isResolvingCard || isPaused || rerollTokens <= 0 || dayPhase != DayPhase.AwaitingPlay) return;
        rerollTokens--;
        selected.Clear();
        StartCoroutine(RollDayRoutine(true));
    }

    private void SetupDayPresentation()
    {
        DifficultySettings settings = GetDifficultySettings();
        bool uncommonUnlocked = currentDieRoll >= settings.UncommonUnlockRoll;
        bool rareUnlocked = currentDieRoll >= settings.RareUnlockRoll;

        string unlockLine;
        if (rareUnlocked)
            unlockLine = "Points " + currentDieRoll + " — Common, Uncommon & Rare cards available!";
        else if (uncommonUnlocked)
            unlockLine = "Points " + currentDieRoll + " — Common & Uncommon cards available  (need " + settings.RareUnlockRoll + " points for Rare)";
        else
            unlockLine = "Points " + currentDieRoll + " — Common cards only  (need " + settings.UncommonUnlockRoll + " for Uncommon, " + settings.RareUnlockRoll + " for Rare)";

        string rerollLine = rerollTokens > 0
            ? rerollTokens + " re-roll" + (rerollTokens > 1 ? "s" : "") + " remaining — re-roll to try for more points."
            : "No re-rolls left this turn.";

        int algae = CountSpecies(SpeciesType.Algae);
        int fish = CountSpecies(SpeciesType.Fish);
        int snails = CountSpecies(SpeciesType.Snail);
        string stateHint = "";
        if (algae >= settings.AlgaeWarning)
            stateHint = "Algae is at bloom risk — consider an Algae or Snail card.";
        else if (nitrateLevel >= settings.NitrateWarning)
            stateHint = "Nitrates are high — a Water card would help.";
        else if (fish < settings.StableFishMin)
            stateHint = "Fish count is low — you need " + settings.StableFishMin + " fish to win.";
        else if (fishHungryTurns > 0)
            stateHint = "Fish are hungry — play a Feed Fish card to avoid grazing.";
        else if (snails > 0 && algae <= settings.SnailStarveThreshold)
            stateHint = "Snails are starving — algae is too low, add more or lose a snail.";

        latestWarnings = string.IsNullOrEmpty(stateHint)
            ? "Jar looks stable. Choose a card to maintain it."
            : stateHint;

        dayReport = jarName + "  ·  Day " + day + "\n\n"
            + unlockLine + "\n"
            + rerollLine
            + (string.IsNullOrEmpty(stateHint) ? "" : "\n\nTip: " + stateHint);

        if (bannerText != null) bannerText.text = "Day " + day + "  ·  Points " + currentDieRoll + "  ·  Select a card to play";
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

    private void EnsurePlayableHandAfterRoll()
    {
        if (hand.Count == 0 || hand.Exists(IsCardPlayable))
        {
            return;
        }

        CardDef guaranteed = DrawGuaranteedPlayableCard();
        if (guaranteed == null)
        {
            return;
        }

        int replaceIndex = GetHighestRequirementCardIndex();
        if (replaceIndex < 0 || replaceIndex >= hand.Count)
        {
            replaceIndex = hand.Count - 1;
        }

        discardPile.Add(hand[replaceIndex]);
        hand[replaceIndex] = guaranteed;
        latestWarnings = "One card was swapped so you always have at least 1 playable option.";
    }

    private CardDef DrawGuaranteedPlayableCard()
    {
        int drawIndex = FindPlayableCardIndex(drawPile);
        if (drawIndex >= 0)
        {
            return RemoveCardAt(drawPile, drawIndex);
        }

        if (discardPile.Count > 0)
        {
            drawPile.AddRange(discardPile);
            discardPile.Clear();
            Shuffle(drawPile);
            drawIndex = FindPlayableCardIndex(drawPile);
            if (drawIndex >= 0)
            {
                return RemoveCardAt(drawPile, drawIndex);
            }
        }

        List<CardDef> fallbackCards = new List<CardDef>();
        for (int i = 0; i < deckTemplate.Count; i++)
        {
            if (GetRequiredRoll(deckTemplate[i]) <= currentDieRoll)
            {
                fallbackCards.Add(deckTemplate[i]);
            }
        }

        if (fallbackCards.Count == 0)
        {
            return null;
        }

        return fallbackCards[UnityEngine.Random.Range(0, fallbackCards.Count)];
    }

    private int FindPlayableCardIndex(List<CardDef> source)
    {
        for (int i = source.Count - 1; i >= 0; i--)
        {
            if (IsCardPlayable(source[i]))
            {
                return i;
            }
        }

        return -1;
    }

    private CardDef RemoveCardAt(List<CardDef> source, int index)
    {
        CardDef card = source[index];
        source.RemoveAt(index);
        return card;
    }

    private int GetHighestRequirementCardIndex()
    {
        int bestIndex = -1;
        int highestRequirement = int.MinValue;
        for (int i = 0; i < hand.Count; i++)
        {
            int requirement = GetRequiredRoll(hand[i]);
            if (requirement > highestRequirement)
            {
                highestRequirement = requirement;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private void ResolveRandomEvent(TurnState turn)
    {
        int roll = UnityEngine.Random.Range(1, 7);
        if (roll == 1) { turn.LightDelta += 20f; lastRandomEventSummary = "Heatwave: +20 light."; }
        else if (roll == 2) { turn.LightDelta -= 20f; lastRandomEventSummary = "Overcast: -20 light."; }
        else if (roll == 3) { turn.NitrateBonus += 10f; lastRandomEventSummary = "Overfeeding remnant: +10 nitrates."; }
        else if (roll == 4) { turn.AddSnail += 1; lastRandomEventSummary = "Surprise snail: +1 snail."; }
        else if (roll == 5) { turn.AlgaeBonus -= 2; lastRandomEventSummary = "Snail feast overnight: -2 algae."; }
        else { turn.RemoveFish += 1; lastRandomEventSummary = "One fish didn't make it: -1 fish."; }
        turn.Notes.Add("Random Event: " + lastRandomEventSummary);
    }

    private void ToggleCard(int index)
    {
        if (index < 0 || index >= hand.Count || state != GameState.Playing || dayPhase != DayPhase.AwaitingPlay) return;
        CardDef card = hand[index];
        if (!IsCardPlayable(card))
        {
            latestWarnings = card.Name + " is locked by your current points.";
            RefreshHud();
            return;
        }
        if (selected.Contains(card)) selected.Remove(card);
        else if (selected.Count < 1) selected.Add(card);
        RefreshHud();
    }

    private bool IsCardPlayable(CardDef card)
    {
        return currentDieRoll >= GetRequiredRoll(card);
    }

    private int GetRequiredRoll(CardDef card)
    {
        DifficultySettings settings = GetDifficultySettings();
        if (card.Tier == CardTier.Common) return 1;
        if (card.Tier == CardTier.Uncommon) return settings.UncommonUnlockRoll;
        return settings.RareUnlockRoll;
    }

    private static string GetTierRoman(CardTier tier)
    {
        if (tier == CardTier.Common) return "I";
        if (tier == CardTier.Uncommon) return "II";
        return "III";
    }

    private void TickDay()
    {
        if (state != GameState.Playing || isResolvingCard || isPaused || dayPhase != DayPhase.AwaitingPlay) return;
        if (selected.Count == 0) { latestWarnings = "Play 1 card before ending the day."; RefreshHud(); return; }
        StartCoroutine(ResolveSelectedCardRoutine(selected[0]));
    }

    private IEnumerator ResolveSelectedCardRoutine(CardDef playedCard)
    {
        isResolvingCard = true;
        dayPhase = DayPhase.ResolvingTurn;
        int playedIndex = hand.IndexOf(playedCard);
        if (playedIndex >= 0 && playedIndex < cardRoots.Count)
        {
            Vector2 jarTarget = GetJarScreenTarget();
            StartCardAnimation(playedIndex, jarTarget, 0f, 1.08f, 1.18f, 1f, 1f, 0.22f);
            yield return WaitForCardAnimation();
            CreateSparkBurst(playAnimTarget, playedCard.Color, 12);
            screenShakeTime = 0.15f;
            bloomFlash = Mathf.Max(bloomFlash, 0.55f);
            if (discardPileMarker != null)
            {
                Vector3 discardWorld = discardPileMarker.TransformPoint(discardPileMarker.rect.center);
                Vector2 discardTarget = (Vector2)rightPanelRect.InverseTransformPoint(discardWorld);
                StartCardAnimation(playedIndex, discardTarget, -14f, 1.18f, 0.38f, 1f, 0.18f, 0.24f);
                yield return WaitForCardAnimation();
            }
        }

        TurnState turn = new TurnState();
        playedCard.Apply(turn);
        if (turn.TriggerRandomEvent)
        {
            ResolveRandomEvent(turn);
        }

        RemoveSpecies(SpeciesType.Fish, turn.RemoveFish);
        RemoveSpecies(SpeciesType.Snail, turn.RemoveSnail);
        AddSpecies(SpeciesType.Fish, turn.AddFish);
        AddSpecies(SpeciesType.Snail, turn.AddSnail);
        ApplyAlgaeChange(turn.AlgaeBonus);

        ResolveSimulation(turn);
        hand.Remove(playedCard);
        discardPile.Add(playedCard);
        selected.Clear();
        hoveredCardIndex = -1;
        currentDieRoll = 0;
        RefreshHud();
        if (state != GameState.Playing)
        {
            isResolvingCard = false;
            yield break;
        }
        yield return new WaitForSecondsRealtime(0.14f);
        day++;
        yield return AnimateCameraTo(GetDiceCameraPosition(), GetDiceCameraRotation(), 0.6f);
        isResolvingCard = false;
        BeginRollPhase();
    }

    private void ResolveSimulation(TurnState turn)
    {
        DifficultySettings settings = GetDifficultySettings();
        lightLevel = Mathf.Clamp(lightLevel + turn.LightDelta, 0f, 100f);

        int startAlgae = CountSpecies(SpeciesType.Algae);
        int fish = CountSpecies(SpeciesType.Fish);
        int snails = CountSpecies(SpeciesType.Snail);

        float growth = settings.BaseAlgaeGrowth
            + (Mathf.Max(0f, lightLevel - 50f) * settings.LightGrowthPerPointAbove50 * GetTemperatureAlgaeGrowthMultiplier())
            + (nitrateLevel * settings.NitrateGrowthPerPoint)
            + (previousTurnAlgaeCount * settings.MemoryBleed)
            + (algaeMemory * 4f);
        if (startAlgae >= settings.AlgaeSoftCapStart)
        {
            growth *= settings.AlgaeSoftCapMultiplier;
        }

        int algaeGrowth = Mathf.Max(0, Mathf.RoundToInt(growth));
        if (algaeGrowth > 0)
        {
            ApplyAlgaeChange(algaeGrowth);
        }

        bool fedFish = turn.FeedFishPlayed;
        if (fedFish) fishHungryTurns = 0;
        else fishHungryTurns++;

        int fishGraze = 0;
        if (!fedFish && fish > 0)
        {
            fishGraze = Mathf.Min(CountSpecies(SpeciesType.Algae), fish * settings.FishHungryGraze);
            if (fishGraze > 0) ApplyAlgaeChange(-fishGraze);
        }

        int snailGraze = Mathf.Min(CountSpecies(SpeciesType.Algae), CountSpecies(SpeciesType.Snail) * settings.SnailAlgaeEat);
        if (snailGraze > 0)
        {
            ApplyAlgaeChange(-snailGraze);
        }

        int algaeAfterGraze = CountSpecies(SpeciesType.Algae);
        if (fishHungryTurns >= settings.FishStarveTurns && CountSpecies(SpeciesType.Fish) > 0)
        {
            int fishLoss = settings.FishStarveLoseOne ? 1 : Mathf.CeilToInt(CountSpecies(SpeciesType.Fish) * 0.5f);
            RemoveSpecies(SpeciesType.Fish, fishLoss);
            fishHungryTurns = 0;
            turn.Notes.Add("Fish starvation caused a loss.");
        }

        if (algaeAfterGraze <= settings.SnailStarveThreshold) snailStarvingTurns++;
        else snailStarvingTurns = 0;

        if (snailStarvingTurns >= settings.SnailStarveTurns && CountSpecies(SpeciesType.Snail) > 0)
        {
            int snailLoss = settings.SnailStarveLoseOne ? 1 : Mathf.CeilToInt(CountSpecies(SpeciesType.Snail) * 0.5f);
            RemoveSpecies(SpeciesType.Snail, snailLoss);
            snailStarvingTurns = 0;
            turn.Notes.Add("Snail starvation caused a loss.");
        }

        fish = CountSpecies(SpeciesType.Fish);
        snails = CountSpecies(SpeciesType.Snail);
        int algae = CountSpecies(SpeciesType.Algae);

        float wasteMultiplier = GetTemperatureWasteMultiplier();
        nitrateLevel -= settings.PassiveNitrateDecay;
        nitrateLevel += fish * settings.FishWastePerTurn * wasteMultiplier;
        if (fedFish && fish > 0)
        {
            nitrateLevel += fish * settings.FishWastePerTurn * settings.FeedFishNitrateMultiplier * turn.FeedWasteMultiplier * wasteMultiplier;
        }
        nitrateLevel += turn.NitrateBonus;
        if (algae >= settings.AlgaeWarning)
        {
            nitrateLevel += settings.BloomFeedbackNitrates;
        }
        nitrateLevel = Mathf.Clamp(nitrateLevel, 0f, 100f);
        UpdateEcosystemMemory();

        if (fedFish && algae >= settings.FishReproductionMinAlgae && fish > 0 && UnityEngine.Random.value < Mathf.Clamp01(fish * settings.FishReproductionChancePerFish))
        {
            AddSpecies(SpeciesType.Fish, 1);
            turn.Notes.Add("A fed fish reproduced.");
        }

        if (algae >= settings.SnailReproductionMinAlgae && snails > 0 && UnityEngine.Random.value < settings.SnailReproductionChance)
        {
            AddSpecies(SpeciesType.Snail, 1);
            turn.Notes.Add("Snails reproduced.");
        }

        int finalAlgae = CountSpecies(SpeciesType.Algae);
        int finalSnails = CountSpecies(SpeciesType.Snail);
        int finalFish = CountSpecies(SpeciesType.Fish);
        bloomThreshold = settings.AlgaeWarning;
        previousTurnAlgaeCount = finalAlgae;

        bool bloomWarning = finalAlgae >= settings.AlgaeWarning;
        latestWarnings = BuildWarnings(finalAlgae, finalSnails, finalFish, bloomWarning);
        bool stable = nitrateLevel < settings.StableNitrateMax
            && finalAlgae >= settings.StableAlgaeMin
            && finalAlgae <= settings.StableAlgaeMax
            && finalFish >= settings.StableFishMin
            && finalSnails >= settings.StableSnailMin;
        stableDays = stable ? stableDays + 1 : 0;
        perfectDays = stable ? perfectDays + 1 : 0;
        stability = Mathf.Clamp(100f - Mathf.Abs(((settings.StableAlgaeMin + settings.StableAlgaeMax) * 0.5f) - finalAlgae) * 8f - Mathf.Max(0f, nitrateLevel - settings.StableNitrateMax) * 1.3f, 0f, 100f);
        UpdateMilestones();
        dayReport = BuildReport(turn, fedFish, fishGraze, snailGraze, algaeGrowth);

        if (stableDays >= settings.StableDaysToWin) { EndGame(true, "You stabilized the jar for " + settings.StableDaysToWin + " straight days."); return; }
        if (finalFish <= 0) { EndGame(false, "The fish died out and the jar collapsed."); return; }
        if (finalAlgae <= 0) { EndGame(false, "The algae collapsed and the food web failed."); return; }
        if (nitrateLevel >= settings.NitrateCollapse) { EndGame(false, "Nitrates overwhelmed the jar."); return; }
        if (finalAlgae >= settings.AlgaeCollapse) { EndGame(false, "An algae bloom overwhelmed the jar."); return; }
        RefreshHud();
    }

    private string BuildWarnings(int algae, int snails, int fish, bool bloom)
    {
        DifficultySettings settings = GetDifficultySettings();
        List<string> warnings = new List<string>();
        if (bloom) warnings.Add("Algae bloom risk — at " + algae + ", blooms at " + settings.AlgaeWarning + "!");
        if (nitrateLevel >= settings.NitrateCollapse * 0.8f)
            warnings.Add("Nitrates critical! " + nitrateLevel.ToString("0") + " / " + settings.NitrateCollapse.ToString("0") + " (collapse imminent)");
        else if (nitrateLevel >= settings.NitrateWarning)
            warnings.Add("Nitrates high — " + nitrateLevel.ToString("0") + ", collapse at " + settings.NitrateCollapse.ToString("0"));
        if (fishHungryTurns > 0) warnings.Add("Fish hungry " + fishHungryTurns + " turn" + (fishHungryTurns > 1 ? "s" : "") + " — play a Feed Fish card soon");
        if (snails > 0 && algae <= settings.SnailStarveThreshold) warnings.Add("Snails starving — algae at " + algae + ", snails need " + (settings.SnailStarveThreshold + 1) + "+");
        if (fish < settings.StableFishMin) warnings.Add("Too few fish — have " + fish + ", need " + settings.StableFishMin + " to stabilise");
        return string.Join("\n", warnings.ToArray());
    }

    private string BuildReport(TurnState turn, bool fedFish, int fishGraze, int snailGraze, int algaeGrowth)
    {
        DifficultySettings settings = GetDifficultySettings();
        int algae = CountSpecies(SpeciesType.Algae);
        int fish = CountSpecies(SpeciesType.Fish);
        int snails = CountSpecies(SpeciesType.Snail);
        StringBuilder b = new StringBuilder();

        b.AppendLine("Day " + day + " Report");
        if (selected.Count > 0)
            b.AppendLine("Played: " + selected[0].Name + " (" + selected[0].Tier + ")");
        b.AppendLine("");

        // What actually happened this turn
        if (fedFish)
            b.AppendLine("Fish fed — waste raised nitrates");
        else
            b.AppendLine("Fish not fed — they may graze algae");
        if (fishGraze > 0)
            b.AppendLine("Fish ate " + fishGraze + " algae (were hungry)");
        if (snailGraze > 0)
            b.AppendLine("Snails ate " + snailGraze + " algae");
        if (algaeGrowth > 0)
            b.AppendLine("Algae grew +" + algaeGrowth + " from light & nitrates");
        else if (algaeGrowth < 0)
            b.AppendLine("Algae fell " + algaeGrowth + " this turn");
        if (selected.Count > 0 && selected[0].Name == "Random Event")
            b.AppendLine("Event: " + lastRandomEventSummary);
        for (int i = 0; i < turn.Notes.Count && i < 3; i++) b.AppendLine(turn.Notes[i]);

        b.AppendLine("");
        b.AppendLine("Jar now:");
        b.AppendLine("Fish " + fish + "  Snails " + snails + "  Algae " + algae + " / " + settings.AlgaeWarning + " max");
        b.AppendLine("Nitrates " + nitrateLevel.ToString("0") + " / " + settings.NitrateCollapse.ToString("0") + " (warn " + settings.NitrateWarning.ToString("0") + ")");
        b.AppendLine("Stable days " + stableDays + " / " + settings.StableDaysToWin + " needed to win");

        if (!string.IsNullOrEmpty(latestMilestone) && latestMilestone != "No milestones yet.")
            b.AppendLine("Milestone: " + latestMilestone);
        return b.ToString();
    }

    private void RefreshHud()
    {
        DifficultySettings settings = GetDifficultySettings();

        // Derive color hex codes based on current values
        string nitrateHex = nitrateLevel >= settings.NitrateCollapse * 0.8f ? "#FF5555"
            : nitrateLevel >= settings.NitrateWarning ? "#FFBB44" : "#88FFCC";
        string stabilityHex = stability >= 65f ? "#88FFCC" : stability >= 36f ? "#FFEE66" : "#FF6666";
        string stableHex = stableDays >= settings.StableDaysToWin * 0.7f ? "#66FF99"
            : stableDays > 0 ? "#FFEE77" : "#AACCFF";

        int algaeCount = CountSpecies(SpeciesType.Algae);
        int fishCount = CountSpecies(SpeciesType.Fish);
        int snailCount = CountSpecies(SpeciesType.Snail);
        string algaeHex = algaeCount >= settings.AlgaeWarning ? "#FF8844" : algaeCount >= settings.AlgaeWarning * 0.7f ? "#FFDD66" : "#88FFCC";
        string fishHex = fishCount < settings.StableFishMin ? "#FF8888" : "#88FFCC";
        string snailHex = snailCount == 0 ? "#AAAAAA" : (algaeCount <= settings.SnailStarveThreshold ? "#FF8844" : "#88FFCC");
        string lightHex = lightLevel >= 70f ? "#FFDD66" : lightLevel < 30f ? "#9999FF" : "#88FFCC";
        const string helperHex = "#5B635F";

        if (statsText != null) statsText.text =
            jarName + "  ·  " + difficulty + "\n\n"
            + "<color=" + stableHex + ">Day  " + day + "      Stable  " + stableDays + " / " + settings.StableDaysToWin + "</color>\n\n"
            + "<color=" + fishHex + ">Fish     " + fishCount + "</color>"
            + "  <size=11><color=" + helperHex + ">(need " + settings.StableFishMin + "+ to win)</color></size>\n"
            + "<color=" + snailHex + ">Snails  " + snailCount + "</color>"
            + (snailCount > 0 && algaeCount <= settings.SnailStarveThreshold
                ? "  <size=11><color=#FF8844>starving!</color></size>\n"
                : "\n")
            + "<color=" + algaeHex + ">Algae   " + algaeCount + "</color>"
            + "  <size=11><color=" + helperHex + ">(bloom at " + settings.AlgaeWarning + ")</color></size>\n\n"
            + "<color=" + lightHex + ">Light      " + lightLevel.ToString("0") + " / 100</color>"
            + "  <size=11><color=" + helperHex + ">(algae grows above 50)</color></size>\n"
            + "<color=" + nitrateHex + ">Nitrates  " + nitrateLevel.ToString("0.0") + "</color>"
            + "  <size=11><color=" + helperHex + ">(warn " + settings.NitrateWarning.ToString("0") + " / bad " + settings.NitrateCollapse.ToString("0") + ")</color></size>\n"
            + "<color=" + stabilityHex + ">Stability  " + stability.ToString("0") + "</color>"
            + "  <size=11><color=" + helperHex + ">(good above 65)</color></size>\n"
            + "<color=#233433>Points  " + (currentDieRoll > 0 ? currentDieRoll.ToString() : "-") + "</color>";

        // Update fill bars
        if (stableProgressFill != null)
        {
            float stableFrac = settings.StableDaysToWin > 0 ? Mathf.Clamp01((float)stableDays / settings.StableDaysToWin) : 0f;
            stableProgressFill.fillAmount = stableFrac;
            stableProgressFill.color = Color.Lerp(new Color(0.44f, 0.7f, 0.56f, 0.8f), new Color(0.36f, 0.98f, 0.62f, 0.95f), stableFrac);
        }
        if (nitrateBarFill != null)
        {
            float nitrateFrac = Mathf.Clamp01(nitrateLevel / 100f);
            nitrateBarFill.fillAmount = nitrateFrac;
            nitrateBarFill.color = Color.Lerp(new Color(0.38f, 0.88f, 0.58f, 0.85f), new Color(0.96f, 0.3f, 0.24f, 0.92f), Mathf.Clamp01(nitrateFrac * 1.35f));
        }
        if (warningText != null)
        {
            warningText.text = string.IsNullOrEmpty(latestWarnings) ? "Warnings\nStable for now." : "Warnings\n" + latestWarnings;
            warningText.color = string.IsNullOrEmpty(latestWarnings) ? new Color(0.19f, 0.42f, 0.29f) : new Color(0.53f, 0.31f, 0.1f);
        }
        if (eventText != null) eventText.text = "Common: any  ·  Uncommon: " + settings.UncommonUnlockRoll + "+  ·  Rare: " + settings.RareUnlockRoll + "+";
        if (selectedText != null) selectedText.text = BuildSelectedText();
        if (reportText != null) reportText.text = dayReport;
        if (deckText != null) deckText.text = "Draw " + drawPile.Count + "  ·  Discard " + discardPile.Count;
        if (speciesText != null) speciesText.text = "Fish " + CountSpecies(SpeciesType.Fish) + "  Snails " + CountSpecies(SpeciesType.Snail) + "  Algae " + CountSpecies(SpeciesType.Algae);
        RefreshActionButtons();
        if (bannerText != null && state == GameState.Playing)
        {
            bannerText.color = string.IsNullOrEmpty(latestWarnings) ? new Color(0.88f, 0.96f, 0.9f) : new Color(1f, 0.88f, 0.44f);
        }
        RefreshCards();
    }

    private string BuildSelectedText()
    {
        if (selected.Count == 0) return "No card selected";
        CardDef card = selected[0];
        return ">  " + card.Name + "  (" + card.Tier + ")";
    }

    private void RefreshCards()
    {
        for (int i = 0; i < cardButtons.Count; i++)
        {
            bool visible = i < hand.Count;
            cardRoots[i].gameObject.SetActive(visible);
            if (i < cardCanvasGroups.Count && cardCanvasGroups[i] != null && visible && animatingCardIndex != i && animatingDrawCardIndex != i)
            {
                cardCanvasGroups[i].alpha = 1f;
            }
            if (!visible)
            {
                if (i < cardCanvasGroups.Count && cardCanvasGroups[i] != null)
                {
                    cardCanvasGroups[i].alpha = 0f;
                }
                cardRoots[i].localScale = Vector3.one;
                cardRoots[i].localRotation = Quaternion.identity;
            }
            if (!visible) continue;
            CardDef card = hand[i];
            bool isSelected = selected.Contains(card);
            bool unlocked = IsCardPlayable(card);
            bool canInteract = dayPhase == DayPhase.AwaitingPlay && unlocked && (isSelected || selected.Count < 1);
            if (i < cardViews.Count && cardViews[i] != null)
            {
                string categoryText = card.Category + "  " + GetTierRoman(card.Tier) + (unlocked ? string.Empty : "  LOCKED");
                cardViews[i].SetCard(card.Name, card.Summary, categoryText, GetRequiredRoll(card), card.Risk, card.Color, GetCardArtSprite(card), GetCardFrameSprite(card), GetCardRarity(card), isSelected, canInteract);
            }
            else
            {
                TMP_Text label = cardButtons[i].GetComponentInChildren<TMP_Text>();
                if (label != null) label.text = !unlocked ? "Locked" : isSelected ? "Selected" : (selected.Count >= 1 ? "Pick 1 Only" : "Select");
                cardButtons[i].interactable = canInteract;
            }
        }
    }

    private void UpdateCardPresentation()
    {
        for (int i = 0; i < cardRoots.Count; i++)
        {
            if (cardRoots[i] == null)
            {
                continue;
            }

            if (i >= hand.Count)
            {
                cardRoots[i].localScale = Vector3.Lerp(cardRoots[i].localScale, Vector3.one * 0.96f, Time.unscaledDeltaTime * 10f);
                continue;
            }

            RectTransform rect = cardRoots[i];
            if (i == animatingCardIndex)
            {
                float eased = EaseOutCubic(Mathf.Clamp01(playAnimTime));
                rect.anchoredPosition = Vector2.Lerp(playAnimStart, playAnimTarget, eased);
                rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(playAnimStartRotation, playAnimTargetRotation, eased));
                rect.localScale = Vector3.Lerp(Vector3.one * playAnimStartScale, Vector3.one * playAnimTargetScale, eased);
                if (i < cardCanvasGroups.Count && cardCanvasGroups[i] != null)
                {
                    cardCanvasGroups[i].alpha = Mathf.Lerp(playAnimStartAlpha, playAnimTargetAlpha, eased);
                }
                continue;
            }
            if (i == animatingDrawCardIndex)
            {
                float eased = EaseOutCubic(Mathf.Clamp01(drawAnimTime));
                Vector2 drawTargetPos = GetHandCardTargetPosition(i);
                float drawTargetRot = GetHandCardTargetRotation(i);
                Vector2 startPos = GetPilePosition(drawPileMarker);
                rect.anchoredPosition = Vector2.Lerp(startPos, drawTargetPos, eased);
                rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(10f, drawTargetRot, eased));
                rect.localScale = Vector3.Lerp(Vector3.one * 0.72f, Vector3.one, eased);
                if (i < cardCanvasGroups.Count && cardCanvasGroups[i] != null)
                {
                    cardCanvasGroups[i].alpha = Mathf.Lerp(0.15f, 1f, eased);
                }
                continue;
            }

            Vector2 targetPos = GetHandCardTargetPosition(i);
            float targetRot = GetHandCardTargetRotation(i);
            bool hovered = hoveredCardIndex == i;
            bool picked = i < hand.Count && selected.Contains(hand[i]);
            if (hovered || picked)
            {
                targetPos += new Vector2(0f, picked ? 42f : 28f);
            }

            float targetScale = hovered ? 1.06f : picked ? 1.08f : 1f;
            rect.anchoredPosition = Vector2.Lerp(rect.anchoredPosition, targetPos, Time.unscaledDeltaTime * 10f);
            rect.localRotation = Quaternion.Lerp(rect.localRotation, Quaternion.Euler(0f, 0f, targetRot), Time.unscaledDeltaTime * 10f);
            rect.localScale = Vector3.Lerp(rect.localScale, Vector3.one * targetScale, Time.unscaledDeltaTime * 10f);

            if (i < cardShines.Count)
            {
                Image shine = cardShines[i];
                if (shine != null)
                {
                    RectTransform shineRect = shine.rectTransform;
                    float shineX = Mathf.PingPong((Time.unscaledTime * 80f) + (i * 70f), rect.rect.width + 120f) - 60f;
                    shineRect.anchoredPosition = Vector2.Lerp(shineRect.anchoredPosition, new Vector2(shineX, 0f), Time.unscaledDeltaTime * 5f);
                    Color shineColor = shine.color;
                    shineColor.a = hovered ? 0.18f : 0.08f;
                    shine.color = shineColor;
                }
            }
        }
    }

    private void ShowCardTooltip(int index)
    {
        if (tooltipPanel == null || index < 0 || index >= hand.Count)
        {
            return;
        }

        CardDef card = hand[index];
        bool unlocked = IsCardPlayable(card);
        string lockStr = unlocked ? "Unlocked  ·  " + GetRequiredRoll(card) + "+ points" : "<color=#FF8888>Locked  ·  Requires " + GetRequiredRoll(card) + "+ points</color>";
        tooltipText.text = "<b>" + card.Name + "</b>  [" + card.Tier + "]\n" + lockStr + "\n" + card.Summary;
        tooltipPanel.SetActive(true);
    }

    private void CreateSparkBurst(Vector2 center, Color color, int count)
    {
        for (int i = 0; i < count; i++)
        {
            GameObject dot = Panel("Spark", rightPanelRect, color);
            RectTransform rect = dot.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(UnityEngine.Random.Range(6f, 12f), UnityEngine.Random.Range(6f, 12f));
            UiSpark spark = new UiSpark
            {
                Image = dot.GetComponent<Image>(),
                Position = center,
                Velocity = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(180f, 320f),
                Lifetime = UnityEngine.Random.Range(0.32f, 0.55f),
                MaxLifetime = 0.55f
            };
            uiSparks.Add(spark);
        }
    }

    private void UpdateSparks()
    {
        for (int i = uiSparks.Count - 1; i >= 0; i--)
        {
            UiSpark spark = uiSparks[i];
            spark.Lifetime -= Time.unscaledDeltaTime;
            spark.Position += spark.Velocity * Time.unscaledDeltaTime;
            spark.Velocity *= 0.95f;
            if (spark.Image != null)
            {
                spark.Image.rectTransform.anchoredPosition = spark.Position;
                Color color = spark.Image.color;
                color.a = Mathf.Clamp01(spark.Lifetime / spark.MaxLifetime);
                spark.Image.color = color;
            }

            if (spark.Lifetime <= 0f)
            {
                if (spark.Image != null) Destroy(spark.Image.gameObject);
                uiSparks.RemoveAt(i);
            }
        }
    }

    private void UpdateScreenShake()
    {
        if (rightPanelRect == null)
        {
            return;
        }

        if (screenShakeTime > 0f)
        {
            screenShakeTime -= Time.unscaledDeltaTime;
            rightPanelRect.anchoredPosition = UnityEngine.Random.insideUnitCircle * 6f;
        }
        else
        {
            rightPanelRect.anchoredPosition = Vector2.Lerp(rightPanelRect.anchoredPosition, Vector2.zero, Time.unscaledDeltaTime * 12f);
        }
    }

    private static float EaseOutCubic(float t)
    {
        float inv = 1f - t;
        return 1f - (inv * inv * inv);
    }

    private void StartCardAnimation(int index, Vector2 target, float targetRotation, float startScale, float endScale, float startAlpha, float endAlpha, float duration)
    {
        if (index < 0 || index >= cardRoots.Count)
        {
            return;
        }

        animatingCardIndex = index;
        playAnimTime = 0f;
        playAnimDuration = Mathf.Max(0.01f, duration);
        playAnimStart = cardRoots[index].anchoredPosition;
        playAnimTarget = target;
        playAnimStartRotation = cardRoots[index].localEulerAngles.z;
        playAnimTargetRotation = targetRotation;
        playAnimStartScale = startScale;
        playAnimTargetScale = endScale;
        playAnimStartAlpha = startAlpha;
        playAnimTargetAlpha = endAlpha;
    }

    private IEnumerator WaitForCardAnimation()
    {
        while (animatingCardIndex >= 0)
        {
            playAnimTime += Time.unscaledDeltaTime / playAnimDuration;
            if (playAnimTime >= 1f)
            {
                int index = animatingCardIndex;
                if (index >= 0 && index < cardCanvasGroups.Count && cardCanvasGroups[index] != null)
                {
                    cardCanvasGroups[index].alpha = playAnimTargetAlpha;
                }
                animatingCardIndex = -1;
            }
            yield return null;
        }
    }

    private void StartDrawAnimation(int index)
    {
        if (index < 0 || index >= cardRoots.Count)
        {
            return;
        }

        animatingDrawCardIndex = index;
        drawAnimTime = 0f;
        drawAnimDuration = 0.26f;
        cardRoots[index].anchoredPosition = GetPilePosition(drawPileMarker);
        cardRoots[index].localRotation = Quaternion.Euler(0f, 0f, 10f);
        cardRoots[index].localScale = Vector3.one * 0.72f;
        if (index < cardCanvasGroups.Count && cardCanvasGroups[index] != null)
        {
            cardCanvasGroups[index].alpha = 0.15f;
        }
    }

    private IEnumerator WaitForDrawAnimation()
    {
        while (animatingDrawCardIndex >= 0)
        {
            drawAnimTime += Time.unscaledDeltaTime / drawAnimDuration;
            if (drawAnimTime >= 1f)
            {
                int index = animatingDrawCardIndex;
                if (index >= 0 && index < cardCanvasGroups.Count && cardCanvasGroups[index] != null)
                {
                    cardCanvasGroups[index].alpha = 1f;
                }
                animatingDrawCardIndex = -1;
            }
            yield return null;
        }
    }

    private Vector2 GetHandCardTargetPosition(int index)
    {
        float spread = hand.Count == 1 ? 0f : (index - ((hand.Count - 1) * 0.5f));
        return new Vector2(spread * 250f, 18f + (-Mathf.Abs(spread) * 14f));
    }

    private float GetHandCardTargetRotation(int index)
    {
        float spread = hand.Count == 1 ? 0f : (index - ((hand.Count - 1) * 0.5f));
        return -spread * 7f;
    }

    private Vector2 GetPilePosition(RectTransform pileMarker)
    {
        if (pileMarker == null || rightPanelRect == null)
        {
            return new Vector2(0f, 24f);
        }

        Vector3 world = pileMarker.TransformPoint(pileMarker.rect.center);
        return (Vector2)rightPanelRect.InverseTransformPoint(world);
    }

    private RectTransform CreatePileMarker(string name, Transform parent, Vector2 anchoredPosition, string label)
    {
        GameObject pile = Panel(name, parent, new Color(0.96f, 0.92f, 0.84f, 0.9f));
        RectTransform rect = pile.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(92f, 126f);
        rect.anchoredPosition = anchoredPosition;
        Shadow shadow = pile.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.18f);
        shadow.effectDistance = new Vector2(0f, -6f);
        if (shirtFrontSprite != null)
        {
            pile.GetComponent<Image>().sprite = shirtFrontSprite;
            pile.GetComponent<Image>().type = Image.Type.Sliced;
            pile.GetComponent<Image>().preserveAspect = false;
        }

        TextMeshProUGUI text = Label(name + "Label", pile.transform, 14, FontStyles.Bold, TextAlignmentOptions.Bottom);
        Stretch(text.rectTransform);
        text.margin = new Vector4(8f, 8f, 8f, 10f);
        text.text = label;
        text.color = new Color(0.18f, 0.14f, 0.1f);
        return rect;
    }

    private Vector2 GetJarScreenTarget()
    {
        if (rightPanelRect == null)
        {
            return Vector2.zero;
        }

        if (jarWorldRoot != null && sceneCamera != null)
        {
            Vector3 screenPoint = sceneCamera.WorldToScreenPoint(jarWorldRoot.position + new Vector3(0f, 0.8f, 0f));
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rightPanelRect, screenPoint, sceneCamera, out Vector2 localPoint);
            return localPoint;
        }

        return new Vector2(0f, 120f);
    }

    private void ClearVisualCaches()
    {
        cardButtons.Clear();
        cardViews.Clear();
        cardCanvasGroups.Clear();
        cardRoots.Clear();
        cardShines.Clear();
        bubbles.Clear();
        hoveredCardIndex = -1;
        animatingCardIndex = -1;
        animatingDrawCardIndex = -1;
        nitrateBarFill = null;
        stableProgressFill = null;
        warningCardImage = null;
        playButtonGlow = null;
        pauseStatusText = null;
        primaryActionButton = null;
        rerollButton = null;
        warningPulseTime = 0f;
    }

    private static Transform FindChildRecursive(Transform parent, string name)
    {
        if (parent == null)
        {
            return null;
        }

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

    private static RectTransform FindRect(Transform parent, string name)
    {
        Transform match = FindChildRecursive(parent, name);
        return match != null ? match.GetComponent<RectTransform>() : null;
    }

    private static TextMeshProUGUI FindText(Transform parent, string name)
    {
        Transform match = FindChildRecursive(parent, name);
        return match != null ? match.GetComponent<TextMeshProUGUI>() : null;
    }

    private static Image FindImage(Transform parent, string name)
    {
        Transform match = FindChildRecursive(parent, name);
        return match != null ? match.GetComponent<Image>() : null;
    }

    private static GameObject FindObject(Transform parent, string name)
    {
        Transform match = FindChildRecursive(parent, name);
        return match != null ? match.gameObject : null;
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

    private void RebindNamedButton(Transform root, string name, UnityEngine.Events.UnityAction action)
    {
        Transform match = FindChildRecursive(root, name);
        if (match == null)
        {
            return;
        }

        Button button = match.GetComponent<Button>();
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    private Sprite GetCardArtSprite(CardDef card)
    {
        if (card.Risk)
        {
            return shirtAccentSprite != null ? shirtAccentSprite : shirtFrontSprite;
        }

        return card.Category == CardCategory.Snail || card.Category == CardCategory.Water ? shirtAccentSprite : shirtFrontSprite;
    }

    private Sprite GetCardFrameSprite(CardDef card)
    {
        if (card.Category == CardCategory.Algae || card.Category == CardCategory.Light)
        {
            return shirtAccentSprite != null ? shirtAccentSprite : shirtFrontSprite;
        }

        return shirtFrontSprite != null ? shirtFrontSprite : shirtAccentSprite;
    }

    private static RarityType GetCardRarity(CardDef card)
    {
        if (card.Risk)
        {
            return RarityType.Legendary;
        }

        if (card.Tier == CardTier.Rare) return RarityType.Rare;
        if (card.Tier == CardTier.Uncommon) return RarityType.Rare;
        return RarityType.Common;
    }

    private void ApplyStartingJar()
    {
        DifficultySettings settings = GetDifficultySettings();
        nitrateLevel = settings.StartNitrates;
        lightLevel = settings.StartLight;
        int startAlgae = settings.StartAlgae;
        int startSnails = settings.StartSnails;
        int startFish = settings.StartFish;

        switch (startingJar)
        {
            case StartingJar.HighNitrates:
                nitrateLevel += 18f;
                startAlgae += 1;
                jarName = "High-Nitrate " + jarName;
                break;
            case StartingJar.SnailHeavy:
                startSnails += 2;
                startAlgae = Mathf.Max(1, startAlgae - 1);
                jarName = "Snail-Heavy " + jarName;
                break;
            case StartingJar.Overgrown:
                startAlgae += 4;
                nitrateLevel += 8f;
                jarName = "Overgrown " + jarName;
                break;
            case StartingJar.Fragile:
                startAlgae = Mathf.Max(1, startAlgae - 2);
                startFish = Mathf.Max(1, startFish - 1);
                nitrateLevel = Mathf.Max(0f, nitrateLevel - 4f);
                jarName = "Fragile " + jarName;
                break;
            default:
                jarName = "Balanced " + jarName;
                break;
        }

        if (temperatureLevel == 0)
        {
            lightLevel = Mathf.Max(0f, lightLevel - 8f);
            nitrateLevel = Mathf.Max(0f, nitrateLevel - 4f);
        }
        else if (temperatureLevel == 2)
        {
            lightLevel = Mathf.Min(100f, lightLevel + 10f);
            nitrateLevel = Mathf.Min(100f, nitrateLevel + 6f);
        }

        AddSpecies(SpeciesType.Algae, startAlgae);
        AddSpecies(SpeciesType.Snail, startSnails);
        AddSpecies(SpeciesType.Fish, startFish);
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
        DifficultySettings settings = GetDifficultySettings();
        if (nitrateLevel >= settings.NitrateWarning * 0.75f) highNitrateDays++;
        else highNitrateDays = 0;
        if (highNitrateDays >= 3) algaeMemory = Mathf.Clamp(algaeMemory + 0.08f, 0f, 0.4f);
        else algaeMemory = Mathf.Max(0f, algaeMemory - 0.03f);
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
        EnsureJarWorld();
        view.Initialize(type, d.Name, d.Color, jarCreatureRoot, RandomCreaturePosition(type), GetCreaturePrefab(type), GetCreatureScale(type));
        if (type == SpeciesType.Fish)
        {
            GetFishSwimArea(out Vector3 swimExtents, out float floorY, out float surfaceY);
            view.ConfigureFishSwimArea(swimExtents, floorY, surfaceY);
        }
        organisms.Add(view);
        if (type == SpeciesType.Fish) fishTraits[view] = RandomFishTrait();
        if (type == SpeciesType.Algae) UpdateAlgaeVisualScale();
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
            if (type == SpeciesType.Algae) UpdateAlgaeVisualScale();
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
        target.a = 0.1f;
        water.color = Color.Lerp(water.color, target, Time.deltaTime * 3.5f);
        float lightT = Mathf.InverseLerp(0f, 100f, displayedLightLevel);
        if (lightGlow != null)
        {
            Color glowColor = Color.Lerp(new Color(0.72f, 0.77f, 0.84f, 0.03f), new Color(1f, 0.97f, 0.74f, 0.16f), lightT);
            lightGlow.color = Color.Lerp(lightGlow.color, glowColor, Time.deltaTime * 4f);
        }
        if (jarWaterRenderer != null && jarWaterRenderer.material != null)
        {
            Color worldWater = Color.Lerp(
                new Color(target.r * 0.72f, target.g * 0.78f, target.b * 0.86f, 0.38f),
                new Color(Mathf.Min(1f, target.r + 0.08f), Mathf.Min(1f, target.g + 0.08f), Mathf.Min(1f, target.b + 0.04f), 0.46f),
                lightT);
            if (jarWaterRenderer.material.HasProperty("_BaseColor")) jarWaterRenderer.material.SetColor("_BaseColor", worldWater);
            else if (jarWaterRenderer.material.HasProperty("_Color")) jarWaterRenderer.material.color = worldWater;
        }
    }

    private void UpdateSceneLighting()
    {
        float targetLightLevel = GetDisplayedLightLevel();
        displayedLightLevel = Mathf.Lerp(displayedLightLevel, targetLightLevel, Time.deltaTime * 4f);
        float lightT = Mathf.InverseLerp(0f, 100f, displayedLightLevel);

        if (tableKeyLight != null)
        {
            tableKeyLight.intensity = Mathf.Lerp(0.45f, 1.7f, lightT);
            tableKeyLight.color = Color.Lerp(new Color(0.72f, 0.78f, 0.9f), new Color(1f, 0.95f, 0.82f), lightT);
            tableKeyLight.transform.rotation = Quaternion.Slerp(
                tableKeyLight.transform.rotation,
                Quaternion.Euler(Mathf.Lerp(60f, 38f, lightT), Mathf.Lerp(-12f, -30f, lightT), 0f),
                Time.deltaTime * 2.5f);
        }

        if (jarFillLight != null)
        {
            jarFillLight.intensity = Mathf.Lerp(1.4f, 6.8f, lightT);
            jarFillLight.range = Mathf.Lerp(8f, 13f, lightT);
            jarFillLight.color = Color.Lerp(new Color(0.66f, 0.76f, 0.88f), new Color(1f, 0.94f, 0.72f), lightT);
        }
    }

    private float GetDisplayedLightLevel()
    {
        if (state != GameState.Playing)
        {
            return 50f;
        }

        TurnState preview = new TurnState();
        for (int i = 0; i < selected.Count; i++)
        {
            selected[i].Apply(preview);
        }

        return Mathf.Clamp(lightLevel + preview.LightDelta, 0f, 100f);
    }

    private FishTrait RandomFishTrait() { int roll = UnityEngine.Random.Range(0, 4); return roll == 0 ? FishTrait.Hungry : roll == 1 ? FishTrait.Lazy : roll == 2 ? FishTrait.Fragile : FishTrait.Balanced; }
    private void LoadCreaturePrefabs()
    {
        if (fishPrefabs.Count > 0 || snailPrefab != null) return;
        fishPrefabs.Add(Resources.Load<GameObject>("EcosystemCreatures/Fish_B"));
        fishPrefabs.Add(Resources.Load<GameObject>("EcosystemCreatures/Fish_B"));
        fishPrefabs.Add(Resources.Load<GameObject>("EcosystemCreatures/Fish_B"));
        fishPrefabs.RemoveAll(prefab => prefab == null);
        snailPrefab = Resources.Load<GameObject>("EcosystemCreatures/Snail_Surrogate");
    }

    private void LoadDicePrefab()
    {
        if (dicePrefab != null && diceMesh != null)
        {
            return;
        }

#if UNITY_EDITOR
        dicePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Collection Dice set for role-playing games/Prefabs/Dice_d6.prefab");
        UnityEngine.Object[] meshAssets = AssetDatabase.LoadAllAssetsAtPath("Assets/Collection Dice set for role-playing games/Dice_d6/Meshes/Dice_d6.fbx");
        for (int i = 0; i < meshAssets.Length; i++)
        {
            if (meshAssets[i] is Mesh mesh && mesh.name.IndexOf("Dice_d6", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                diceMesh = mesh;
                break;
            }
        }
#endif
    }

    private void EnsureJarWorld()
    {
        if (jarWorldRoot != null) return;

        GameObject existing = GameObject.Find("EcosystemJarWorld");
        if (existing != null)
        {
            jarWorldRoot = existing.transform;
            jarWorldRoot.position = new Vector3(4.1f, 0.2f, 8.4f);
            jarWorldRoot.localScale = Vector3.one * 1.35f;
            Transform creatures = jarWorldRoot.Find("Creatures");
            if (creatures != null) jarCreatureRoot = creatures;
            Transform fx = jarWorldRoot.Find("FX");
            if (fx != null) jarFxRoot = fx;
            Transform waterRoot = jarWorldRoot.Find("WaterVolume");
            if (waterRoot != null) jarWaterRenderer = waterRoot.GetComponent<Renderer>();
            return;
        }

        GameObject root = new GameObject("EcosystemJarWorld");
        jarWorldRoot = root.transform;
        jarWorldRoot.position = new Vector3(4.1f, 0.2f, 8.4f);
        jarWorldRoot.localScale = Vector3.one * 1.35f;

        GameObject basePedestal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        basePedestal.name = "JarBase";
        basePedestal.transform.SetParent(jarWorldRoot, false);
        basePedestal.transform.localPosition = new Vector3(0f, -2.75f, 0f);
        basePedestal.transform.localScale = new Vector3(2.45f, 0.22f, 2.45f);
        Renderer baseRenderer = basePedestal.GetComponent<Renderer>();
        if (baseRenderer != null) baseRenderer.sharedMaterial = CreateWorldMaterial(new Color(0.17f, 0.14f, 0.11f, 1f), false);

        GameObject glass = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        glass.name = "GlassShell";
        glass.transform.SetParent(jarWorldRoot, false);
        glass.transform.localScale = new Vector3(2.2f, 2.7f, 2.2f);
        Renderer glassRenderer = glass.GetComponent<Renderer>();
        if (glassRenderer != null) glassRenderer.sharedMaterial = CreateWorldMaterial(new Color(0.82f, 0.94f, 0.98f, 0.12f), true);

        GameObject innerGlass = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        innerGlass.name = "InnerGlass";
        innerGlass.transform.SetParent(jarWorldRoot, false);
        innerGlass.transform.localScale = new Vector3(2.06f, 2.62f, 2.06f);
        Renderer innerGlassRenderer = innerGlass.GetComponent<Renderer>();
        if (innerGlassRenderer != null) innerGlassRenderer.sharedMaterial = CreateWorldMaterial(new Color(0.9f, 0.98f, 1f, 0.04f), true);

        GameObject rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        rim.name = "JarRim";
        rim.transform.SetParent(jarWorldRoot, false);
        rim.transform.localPosition = new Vector3(0f, 2.78f, 0f);
        rim.transform.localScale = new Vector3(2.28f, 0.08f, 2.28f);
        Renderer rimRenderer = rim.GetComponent<Renderer>();
        if (rimRenderer != null) rimRenderer.sharedMaterial = CreateWorldMaterial(new Color(0.9f, 0.95f, 0.98f, 0.32f), true);

        GameObject waterVolume = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        waterVolume.name = "WaterVolume";
        waterVolume.transform.SetParent(jarWorldRoot, false);
        waterVolume.transform.localPosition = new Vector3(0f, -0.2f, 0f);
        waterVolume.transform.localScale = new Vector3(2.0f, 2.1f, 2.0f);
        jarWaterRenderer = waterVolume.GetComponent<Renderer>();
        if (jarWaterRenderer != null) jarWaterRenderer.sharedMaterial = CreateWorldMaterial(new Color(0.62f, 0.86f, 0.96f, 0.42f), true);

        GameObject waterSurface = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        waterSurface.name = "WaterSurface";
        waterSurface.transform.SetParent(jarWorldRoot, false);
        waterSurface.transform.localPosition = new Vector3(0f, 1.9f, 0f);
        waterSurface.transform.localScale = new Vector3(1.96f, 0.02f, 1.96f);
        Renderer waterSurfaceRenderer = waterSurface.GetComponent<Renderer>();
        if (waterSurfaceRenderer != null) waterSurfaceRenderer.sharedMaterial = CreateWorldMaterial(new Color(0.84f, 0.96f, 1f, 0.22f), true);

        GameObject gravel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        gravel.name = "Gravel";
        gravel.transform.SetParent(jarWorldRoot, false);
        gravel.transform.localPosition = new Vector3(0f, -2.42f, 0f);
        gravel.transform.localScale = new Vector3(1.9f, 0.18f, 1.9f);
        Renderer gravelRenderer = gravel.GetComponent<Renderer>();
        if (gravelRenderer != null) gravelRenderer.sharedMaterial = CreateWorldMaterial(new Color(0.36f, 0.29f, 0.24f, 1f), false);

        GameObject moss = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        moss.name = "MossRing";
        moss.transform.SetParent(jarWorldRoot, false);
        moss.transform.localPosition = new Vector3(0f, -2.18f, 0f);
        moss.transform.localScale = new Vector3(1.82f, 0.03f, 1.82f);
        Renderer mossRenderer = moss.GetComponent<Renderer>();
        if (mossRenderer != null) mossRenderer.sharedMaterial = CreateWorldMaterial(new Color(0.2f, 0.36f, 0.18f, 1f), false);

        GameObject creaturesRoot = new GameObject("Creatures");
        jarCreatureRoot = creaturesRoot.transform;
        jarCreatureRoot.SetParent(jarWorldRoot, false);

        GameObject fxRoot = new GameObject("FX");
        jarFxRoot = fxRoot.transform;
        jarFxRoot.SetParent(jarWorldRoot, false);
        CreateJarPlants();
        CreateJarBubbles();
    }

    private void EnsureDiceStage()
    {
        if (diceStageRoot != null)
        {
            CacheExistingDie();
            return;
        }

        GameObject existing = GameObject.Find("DiceRollStage");
        if (existing != null)
        {
            diceStageRoot = existing.transform;
            CacheExistingDie();
            return;
        }

        GameObject stage = new GameObject("DiceRollStage");
        diceStageRoot = stage.transform;
        diceStageRoot.position = new Vector3(-0.3f, -1.24f, 4.1f);

        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        marker.name = "DiceRollMarker";
        marker.transform.SetParent(diceStageRoot, false);
        marker.transform.localScale = new Vector3(0.75f, 0.03f, 0.75f);
        Renderer renderer = marker.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = CreateWorldMaterial(new Color(0.26f, 0.18f, 0.12f, 1f), false);
        }

        EnsureDiceActor();
        MarkEditorSceneDirty();
    }

    private Material CreateWorldMaterial(Color color, bool transparent)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material material = new Material(shader);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        else if (material.HasProperty("_Color")) material.color = color;
        if (transparent)
        {
            material.renderQueue = 3000;
            if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
        }
        return material;
    }

    private Vector3 RandomCreaturePosition(SpeciesType type)
    {
        if (type == SpeciesType.Snail)
        {
            float side = UnityEngine.Random.value < 0.5f ? -1f : 1f;
            return new Vector3(UnityEngine.Random.Range(0.85f, 1.25f) * side, UnityEngine.Random.Range(-1.65f, -0.7f), UnityEngine.Random.Range(-0.95f, 0.95f));
        }
        if (type == SpeciesType.Algae)
        {
            float ring = UnityEngine.Random.Range(0.55f, 1.15f);
            float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            return new Vector3(Mathf.Cos(angle) * ring, UnityEngine.Random.Range(-1.7f, 0.5f), Mathf.Sin(angle) * ring);
        }

        GetFishSwimArea(out Vector3 swimExtents, out float floorY, out float surfaceY);
        Vector2 circle = UnityEngine.Random.insideUnitCircle * 0.82f;
        return new Vector3(
            circle.x * swimExtents.x,
            UnityEngine.Random.Range(floorY + 0.2f, surfaceY - 0.2f),
            circle.y * swimExtents.z);
    }

    private void GetFishSwimArea(out Vector3 swimExtents, out float floorY, out float surfaceY)
    {
        swimExtents = new Vector3(0.9f, 1.2f, 0.9f);
        floorY = -1.5f;
        surfaceY = 1.35f;

        if (jarWorldRoot == null) return;

        Transform water = jarWorldRoot.Find("WaterVolume");
        Transform glass = jarWorldRoot.Find("GlassShell");
        Transform rim = jarWorldRoot.Find("JarRim");
        Transform gravel = jarWorldRoot.Find("Gravel");

        if (water != null)
        {
            swimExtents.x = Mathf.Max(0.25f, (water.localScale.x * 0.5f) - 0.18f);
            swimExtents.z = Mathf.Max(0.25f, (water.localScale.z * 0.5f) - 0.18f);
            floorY = water.localPosition.y - water.localScale.y + 0.22f;
            surfaceY = water.localPosition.y + water.localScale.y - 0.3f;
        }

        if (glass != null)
        {
            float glassRadiusX = Mathf.Max(0.25f, (glass.localScale.x * 0.5f) - 0.22f);
            float glassRadiusZ = Mathf.Max(0.25f, (glass.localScale.z * 0.5f) - 0.22f);
            swimExtents.x = Mathf.Min(swimExtents.x, glassRadiusX);
            swimExtents.z = Mathf.Min(swimExtents.z, glassRadiusZ);
        }

        if (rim != null)
        {
            surfaceY = Mathf.Min(surfaceY, rim.localPosition.y - 0.45f);
        }

        if (gravel != null)
        {
            floorY = Mathf.Max(floorY, gravel.localPosition.y + gravel.localScale.y + 0.15f);
        }

        if (surfaceY <= floorY + 0.4f)
        {
            surfaceY = floorY + 0.4f;
        }
    }

    private GameObject GetCreaturePrefab(SpeciesType type)
    {
        if (type == SpeciesType.Fish && fishPrefabs.Count > 0) return fishPrefabs[UnityEngine.Random.Range(0, fishPrefabs.Count)];
        if (type == SpeciesType.Snail) return null;
        return null;
    }

    private float GetCreatureScale(SpeciesType type)
    {
        if (type == SpeciesType.Fish) return 10f;
        if (type == SpeciesType.Snail) return 0.18f;
        return 0.18f;
    }

    private void UpdateAlgaeVisualScale()
    {
        List<OrganismView> algae = Species(SpeciesType.Algae);
        if (algae.Count == 0)
        {
            return;
        }

        float scaleMultiplier = Mathf.Lerp(1f, 1.9f, Mathf.Clamp01(algae.Count / 18f));
        for (int i = 0; i < algae.Count; i++)
        {
            OrganismView view = algae[i];
            if (view == null)
            {
                continue;
            }

            view.SetVisualScaleMultiplier(view.GetBaseVisualScale() * scaleMultiplier);
        }
    }

    private void CreateJarPlants()
    {
        if (jarFxRoot == null) return;
        for (int i = 0; i < 6; i++)
        {
            GameObject plantRoot = new GameObject("Plant_" + i);
            plantRoot.transform.SetParent(jarFxRoot, false);
            float angle = (Mathf.PI * 2f * i) / 6f;
            float radius = UnityEngine.Random.Range(0.7f, 1.15f);
            plantRoot.transform.localPosition = new Vector3(Mathf.Cos(angle) * radius, -2.18f, Mathf.Sin(angle) * radius);
            plantRoot.transform.localRotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);

            int stalks = UnityEngine.Random.Range(3, 6);
            for (int s = 0; s < stalks; s++)
            {
                GameObject stalk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                stalk.transform.SetParent(plantRoot.transform, false);
                stalk.transform.localScale = new Vector3(0.06f, UnityEngine.Random.Range(0.35f, 0.8f), 0.06f);
                stalk.transform.localPosition = new Vector3(UnityEngine.Random.Range(-0.12f, 0.12f), stalk.transform.localScale.y * 0.5f, UnityEngine.Random.Range(-0.12f, 0.12f));
                stalk.transform.localRotation = Quaternion.Euler(UnityEngine.Random.Range(-10f, 10f), 0f, UnityEngine.Random.Range(-14f, 14f));
                Renderer renderer = stalk.GetComponent<Renderer>();
                if (renderer != null) renderer.sharedMaterial = CreateWorldMaterial(new Color(0.22f, 0.46f, 0.2f, 1f), false);
            }
        }
    }

    private void CreateJarBubbles()
    {
        bubbles.Clear();
        if (jarFxRoot == null) return;
        for (int i = 0; i < 16; i++)
        {
            GameObject bubble = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bubble.name = "Bubble_" + i;
            bubble.transform.SetParent(jarFxRoot, false);
            float scale = UnityEngine.Random.Range(0.05f, 0.12f);
            bubble.transform.localScale = Vector3.one * scale;
            bubble.transform.localPosition = new Vector3(UnityEngine.Random.Range(-1.1f, 1.1f), UnityEngine.Random.Range(-2.0f, 1.6f), UnityEngine.Random.Range(-1.1f, 1.1f));
            Renderer renderer = bubble.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = CreateWorldMaterial(new Color(0.92f, 0.98f, 1f, 0.18f), true);
            bubbles.Add(new BubbleFx
            {
                Transform = bubble.transform,
                Speed = UnityEngine.Random.Range(0.18f, 0.38f),
                Drift = UnityEngine.Random.Range(0.05f, 0.16f),
                Phase = UnityEngine.Random.Range(0f, 10f),
                BottomY = -2.05f,
                TopY = 1.95f
            });
        }
    }

    private void UpdateJarFx()
    {
        if (bubbles.Count == 0) return;
        float delta = Application.isPlaying ? Time.deltaTime : 0.016f;
        for (int i = 0; i < bubbles.Count; i++)
        {
            BubbleFx bubble = bubbles[i];
            if (bubble.Transform == null) continue;
            Vector3 position = bubble.Transform.localPosition;
            position.y += bubble.Speed * delta;
            position.x += Mathf.Sin((Time.time * 1.8f) + bubble.Phase) * bubble.Drift * delta;
            position.z += Mathf.Cos((Time.time * 1.4f) + bubble.Phase) * bubble.Drift * delta;
            if (position.y > bubble.TopY)
            {
                position.y = bubble.BottomY;
                position.x = UnityEngine.Random.Range(-1.05f, 1.05f);
                position.z = UnityEngine.Random.Range(-1.05f, 1.05f);
            }
            bubble.Transform.localPosition = position;
        }
    }

    private void UpdateWarningPulse()
    {
        if (warningCardImage == null || state != GameState.Playing) return;
        bool hasWarnings = !string.IsNullOrEmpty(latestWarnings)
            && latestWarnings != "Roll unlocks which cards can be played this turn."
            && latestWarnings != "You spent a re-roll token."
            && latestWarnings != "Play 1 card before ending the day.";
        warningPulseTime += Time.unscaledDeltaTime * (hasWarnings ? 2.4f : 0.8f);
        float pulse = 0.5f + Mathf.Sin(warningPulseTime) * 0.5f;
        float targetAlpha = hasWarnings ? Mathf.Lerp(0.82f, 1f, pulse) : 0.95f;
        Color c = warningCardImage.color;
        c.a = Mathf.Lerp(c.a, targetAlpha, Time.unscaledDeltaTime * 5f);
        warningCardImage.color = c;
    }

    private void UpdatePlayButtonGlow()
    {
        if (playButtonGlow == null || state != GameState.Playing) return;
        Image glowImage = playButtonGlow.GetComponent<Image>();
        if (glowImage == null) return;
        bool cardSelected = selected.Count > 0;
        bool shouldGlow = dayPhase == DayPhase.AwaitingRoll || cardSelected;
        float targetAlpha = shouldGlow ? 0.18f + Mathf.Sin(Time.unscaledTime * 3.8f) * 0.1f : 0f;
        Color c = glowImage.color;
        c.a = Mathf.Lerp(c.a, targetAlpha, Time.unscaledDeltaTime * 9f);
        glowImage.color = c;
    }

    private void HandlePrimaryAction()
    {
        if (state != GameState.Playing || isPaused || isResolvingCard || isCameraAnimating)
        {
            return;
        }

        if (dayPhase == DayPhase.AwaitingRoll)
        {
            StartCoroutine(RollDayRoutine(false));
            return;
        }

        if (dayPhase == DayPhase.AwaitingPlay)
        {
            TickDay();
        }
    }

    private void BeginRollPhase()
    {
        dayPhase = DayPhase.AwaitingRoll;
        currentDieRoll = 0;
        selected.Clear();
        hoveredCardIndex = -1;
        latestWarnings = "Look down and roll the die to earn today's card points.";
        dayReport = jarName + "  ·  Day " + day + "\n\nRoll the die to " + (day == 1 ? "draw your opening 3-card hand." : "replenish 1 card for the new day.") + "\nYour roll becomes the points used to unlock cards.\nTemperature: " + TemperatureLabel();
        if (bannerText != null)
        {
            bannerText.text = "Day " + day + "  ·  Roll the die for points";
        }
        RefreshHud();
    }

    private IEnumerator RollDayRoutine(bool isReroll)
    {
        if (dayPhase == DayPhase.Rolling)
        {
            yield break;
        }

        dayPhase = DayPhase.Rolling;
        latestWarnings = isReroll ? "Re-rolling the die..." : "Rolling the die...";
        RefreshHud();

        yield return AnimateCameraTo(GetDiceCameraPosition(), GetDiceCameraRotation(), 0.45f);
        EnsureDiceActor();
        RollDie();
        yield return AnimateDieRoll();
        SnapDieToResult(currentDieRoll);

        if (!isReroll)
        {
            yield return DrawCardsForDayRoutine();
        }

        EnsurePlayableHandAfterRoll();
        SetupDayPresentation();
        yield return new WaitForSecondsRealtime(0.2f);
        yield return AnimateCameraTo(GetJarCameraPosition(), GetJarCameraRotation(), 0.55f);
        dayPhase = DayPhase.AwaitingPlay;
        RefreshHud();
    }

    private void EnsureDiceActor()
    {
        EnsureDiceStage();
        if (diceStageRoot == null)
        {
            return;
        }

        if (activeDie != null && dicePrefab != null && IsFallbackDice(activeDie.gameObject))
        {
            GameObject oldDie = activeDie.gameObject;
            activeDie = null;
            if (Application.isPlaying) Destroy(oldDie);
            else DestroyImmediate(oldDie);
            MarkEditorSceneDirty();
        }

        if (activeDie == null)
        {
            GameObject dieObject = CreateEditorFriendlyDieObject();
            dieObject.name = "ActiveDayDie";
            activeDie = dieObject.transform;
            activeDie.SetParent(diceStageRoot, false);
            activeDie.localScale = Vector3.one * 0.55f;
            activeDie.localPosition = new Vector3(0f, 0.18f, 0f);
            activeDie.localRotation = GetDieFaceRotation(1);
            Rigidbody body = dieObject.GetComponent<Rigidbody>();
            if (body != null)
            {
                Destroy(body);
            }

            MarkEditorSceneDirty();
        }

        ApplyDiceGeometry(activeDie.gameObject);
        ApplyDiceMaterial(activeDie.gameObject);
    }

    private GameObject CreateEditorFriendlyDieObject()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying && dicePrefab != null)
        {
            GameObject prefabInstance = PrefabUtility.InstantiatePrefab(dicePrefab) as GameObject;
            if (prefabInstance != null)
            {
                return prefabInstance;
            }
        }
#endif

        return dicePrefab != null ? Instantiate(dicePrefab) : GameObject.CreatePrimitive(PrimitiveType.Cube);
    }

    private void ApplyDiceMaterial(GameObject dieObject)
    {
        if (dieObject == null)
        {
            return;
        }

        Renderer renderer = dieObject.GetComponent<Renderer>();
        if (renderer == null)
        {
            return;
        }

        if (diceDisplayMaterial == null)
        {
            diceDisplayMaterial = BuildDiceDisplayMaterial();
        }

        if (diceDisplayMaterial != null)
        {
            renderer.sharedMaterial = diceDisplayMaterial;
        }
    }

    private void ApplyDiceGeometry(GameObject dieObject)
    {
        if (dieObject == null || diceMesh == null)
        {
            return;
        }

        MeshFilter meshFilter = dieObject.GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            meshFilter = dieObject.AddComponent<MeshFilter>();
        }

        MeshRenderer meshRenderer = dieObject.GetComponent<MeshRenderer>();
        if (meshRenderer == null)
        {
            meshRenderer = dieObject.AddComponent<MeshRenderer>();
        }

        MeshCollider meshCollider = dieObject.GetComponent<MeshCollider>();
        if (meshCollider == null)
        {
            meshCollider = dieObject.AddComponent<MeshCollider>();
            meshCollider.convex = true;
        }

        meshFilter.sharedMesh = diceMesh;
        meshCollider.sharedMesh = diceMesh;
    }

    private Material BuildDiceDisplayMaterial()
    {
#if UNITY_EDITOR
        Texture2D albedo = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Collection Dice set for role-playing games/Dice_d6/Textures/2k/Plastic Glossy Pure write/Dice_d6_Albedo.png");
        Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Collection Dice set for role-playing games/Dice_d6/Textures/2k/Plastic Glossy Pure write/Dice_d6_Normal.png");
        Texture2D metallic = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Collection Dice set for role-playing games/Dice_d6/Textures/2k/Plastic Glossy Pure write/Dice_d6_Metallic.png");
        Texture2D occlusion = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Collection Dice set for role-playing games/Dice_d6/Textures/2k/Plastic Glossy Pure write/Dice_d6_AO.png");

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        if (shader == null || albedo == null)
        {
            return null;
        }

        Material material = new Material(shader);
        material.name = "RuntimeDiceDisplayMaterial";

        if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", albedo);
        if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", albedo);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
        if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);

        if (normal != null && material.HasProperty("_BumpMap"))
        {
            material.EnableKeyword("_NORMALMAP");
            material.SetTexture("_BumpMap", normal);
            if (material.HasProperty("_BumpScale")) material.SetFloat("_BumpScale", 1f);
        }

        if (metallic != null && material.HasProperty("_MetallicGlossMap"))
        {
            material.SetTexture("_MetallicGlossMap", metallic);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0.2f);
        }

        if (occlusion != null && material.HasProperty("_OcclusionMap"))
        {
            material.SetTexture("_OcclusionMap", occlusion);
        }

        return material;
#else
        return null;
#endif
    }

    private void CacheExistingDie()
    {
        if (diceStageRoot == null || activeDie != null)
        {
            return;
        }

        Transform existingDie = diceStageRoot.Find("ActiveDayDie");
        if (existingDie != null)
        {
            activeDie = existingDie;
        }
    }

    private bool IsFallbackDice(GameObject dieObject)
    {
        if (dieObject == null)
        {
            return false;
        }

        MeshFilter meshFilter = dieObject.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            return true;
        }

        string meshName = meshFilter.sharedMesh.name;
        return meshName.IndexOf("Cube", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void MarkEditorSceneDirty()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
#endif
    }

    private IEnumerator AnimateDieRoll()
    {
        if (activeDie == null)
        {
            yield break;
        }

        Vector3 startPos = new Vector3(-0.5f, 0.65f, 0f);
        Vector3 endPos = new Vector3(0f, 0.18f, 0f);
        Quaternion startRot = UnityEngine.Random.rotation;
        float duration = 0.95f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float arc = Mathf.Sin(t * Mathf.PI) * 0.45f;
            activeDie.localPosition = Vector3.Lerp(startPos, endPos, EaseOutCubic(t)) + new Vector3(0f, arc, 0f);
            Quaternion spin = Quaternion.Euler(720f * t, 960f * t, 840f * t);
            activeDie.localRotation = startRot * spin;
            yield return null;
        }
    }

    private void SnapDieToResult(int result)
    {
        if (activeDie == null)
        {
            return;
        }

        activeDie.localPosition = new Vector3(0f, 0.18f, 0f);
        activeDie.localRotation = GetDieFaceRotation(result);
    }

    private Quaternion GetDieFaceRotation(int result)
    {
        switch (Mathf.Clamp(result, 1, 6))
        {
            case 1: return Quaternion.identity;
            case 2: return Quaternion.Euler(0f, 0f, -90f);
            case 3: return Quaternion.Euler(90f, 0f, 0f);
            case 4: return Quaternion.Euler(-90f, 0f, 0f);
            case 5: return Quaternion.Euler(0f, 0f, 90f);
            default: return Quaternion.Euler(180f, 0f, 0f);
        }
    }

    private Vector3 GetJarCameraPosition()
    {
        return new Vector3(0.7f, 2.0f, -5.6f);
    }

    private Quaternion GetJarCameraRotation()
    {
        Vector3 target = jarWorldRoot != null ? jarWorldRoot.position + new Vector3(0f, 0.7f, 0f) : new Vector3(4.1f, 0.9f, 8.4f);
        return Quaternion.LookRotation((target - GetJarCameraPosition()).normalized, Vector3.up);
    }

    private Vector3 GetDiceCameraPosition()
    {
        Vector3 focus = diceStageRoot != null ? diceStageRoot.position : new Vector3(-0.3f, -1.24f, 4.1f);
        return focus + new Vector3(0.1f, 3.0f, -2.3f);
    }

    private Quaternion GetDiceCameraRotation()
    {
        Vector3 focus = diceStageRoot != null ? diceStageRoot.position + new Vector3(0f, 0.15f, 0f) : new Vector3(-0.3f, -1.1f, 4.1f);
        return Quaternion.LookRotation((focus - GetDiceCameraPosition()).normalized, Vector3.up);
    }

    private void MoveCameraImmediate(Vector3 position, Quaternion rotation)
    {
        if (sceneCamera == null)
        {
            return;
        }

        sceneCamera.transform.position = position;
        sceneCamera.transform.rotation = rotation;
    }

    private IEnumerator AnimateCameraTo(Vector3 position, Quaternion rotation, float duration)
    {
        if (sceneCamera == null)
        {
            yield break;
        }

        isCameraAnimating = true;
        Vector3 startPos = sceneCamera.transform.position;
        Quaternion startRot = sceneCamera.transform.rotation;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = EaseOutCubic(Mathf.Clamp01(elapsed / duration));
            sceneCamera.transform.position = Vector3.Lerp(startPos, position, t);
            sceneCamera.transform.rotation = Quaternion.Slerp(startRot, rotation, t);
            yield return null;
        }

        sceneCamera.transform.position = position;
        sceneCamera.transform.rotation = rotation;
        isCameraAnimating = false;
    }

    private float GetTemperatureAlgaeGrowthMultiplier()
    {
        if (temperatureLevel <= 0) return 0.82f;
        if (temperatureLevel >= 2) return 1.22f;
        return 1f;
    }

    private float GetTemperatureWasteMultiplier()
    {
        if (temperatureLevel <= 0) return 0.9f;
        if (temperatureLevel >= 2) return 1.15f;
        return 1f;
    }

    private void RefreshActionButtons()
    {
        if (primaryActionButton != null)
        {
            TMP_Text label = primaryActionButton.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.text = dayPhase == DayPhase.AwaitingRoll ? "Roll For Points" : "Play Selected Card";
            }

            primaryActionButton.interactable = state == GameState.Playing
                && !isPaused
                && !isResolvingCard
                && !isCameraAnimating
                && (dayPhase == DayPhase.AwaitingRoll || dayPhase == DayPhase.AwaitingPlay);
        }

        if (rerollButton != null)
        {
            rerollButton.interactable = state == GameState.Playing
                && !isPaused
                && !isResolvingCard
                && !isCameraAnimating
                && dayPhase == DayPhase.AwaitingPlay
                && rerollTokens > 0;
        }
    }

    private IEnumerator DrawCardsForDayRoutine()
    {
        int cardsToDraw = day == 1 ? Mathf.Max(0, 3 - hand.Count) : Mathf.Min(1, Mathf.Max(0, 3 - hand.Count));
        for (int i = 0; i < cardsToDraw; i++)
        {
            int previousCount = hand.Count;
            DrawSingleCard();
            if (hand.Count > previousCount)
            {
                StartDrawAnimation(hand.Count - 1);
                yield return WaitForDrawAnimation();
            }
        }
    }

    private void DrawSingleCard()
    {
        if (drawPile.Count == 0)
        {
            drawPile.AddRange(discardPile);
            discardPile.Clear();
            Shuffle(drawPile);
        }

        if (drawPile.Count == 0)
        {
            return;
        }

        CardDef next = drawPile[drawPile.Count - 1];
        drawPile.RemoveAt(drawPile.Count - 1);
        hand.Add(next);
    }

    private void EndGame(bool won, string message)
    {
        isPaused = false;
        if (pausePanel != null) pausePanel.SetActive(false);
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
    private static Button CreateUiButton(string text, Transform parent, Color color, UnityEngine.Events.UnityAction onClick) { GameObject go = CreateButtonObject(text, parent, color); Button b = go.GetComponent<Button>() ?? go.AddComponent<Button>(); ColorBlock cb = b.colors; cb.highlightedColor = Color.white; cb.pressedColor = Color.white; b.colors = cb; b.onClick.AddListener(onClick); TextMeshProUGUI t = Label(text + "Text", go.transform, 18, FontStyles.Bold, TextAlignmentOptions.Center); t.text = text; t.color = new Color(0.11f, 0.15f, 0.19f); Stretch(t.rectTransform); UiThemeStyler.ApplyButton(b, GetButtonKind(text), t); return b; }
    private static TextMeshProUGUI Label(string name, Transform parent, int size, FontStyles style, TextAlignmentOptions alignment) { GameObject go = new GameObject(name); RectTransform rt = go.AddComponent<RectTransform>(); rt.SetParent(parent, false); TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>(); t.font = TmpFontUtility.GetFont(); t.fontSize = size; t.fontStyle = style; t.alignment = alignment; t.textWrappingMode = TextWrappingModes.Normal; t.overflowMode = TextOverflowModes.Overflow; return t; }
    private static void Stretch(RectTransform rt) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero; }
    private static void Place(RectTransform rt, Vector2 min, Vector2 max, Vector2 offMin, Vector2 offMax) { rt.anchorMin = min; rt.anchorMax = max; rt.offsetMin = offMin; rt.offsetMax = offMax; }
    private static GameObject CreateButtonObject(string text, Transform parent, Color color)
    {
        return Panel(text + "Button", parent, color);
    }
    private static ThemeButtonKind GetButtonKind(string text)
    {
        return text switch
        {
            "Restart Run" => ThemeButtonKind.Danger,
            "Quit" => ThemeButtonKind.Danger,
            "Pause" => ThemeButtonKind.Secondary,
            "Re-roll Die" => ThemeButtonKind.Secondary,
            "Play Again" => ThemeButtonKind.Start,
            _ => ThemeButtonKind.Primary
        };
    }

    private static void ApplyThemeToExistingCanvas(Transform root)
    {
        ApplyPanelTheme(root, "StatsCard", ThemePanelKind.Medium, new Color(1f, 1f, 1f, 0.92f));
        ApplyPanelTheme(root, "WarningCard", ThemePanelKind.Notice, new Color(1f, 1f, 1f, 0.95f));
        ApplyPanelTheme(root, "ReportCard", ThemePanelKind.Small, new Color(1f, 1f, 1f, 0.9f));
        ApplyPanelTheme(root, "PauseCard", ThemePanelKind.Large, new Color(1f, 1f, 1f, 0.98f));
        ApplyPanelTheme(root, "ResultCard", ThemePanelKind.Large, new Color(1f, 1f, 1f, 0.98f));
        ApplyButtonTheme(root, "Play Selected CardButton", ThemeButtonKind.Primary);
        ApplyButtonTheme(root, "Re-roll DieButton", ThemeButtonKind.Secondary);
        ApplyButtonTheme(root, "PauseButton", ThemeButtonKind.Secondary);
        ApplyButtonTheme(root, "ResumeButton", ThemeButtonKind.Primary);
        ApplyButtonTheme(root, "Restart RunButton", ThemeButtonKind.Danger);
        ApplyButtonTheme(root, "QuitButton", ThemeButtonKind.Danger);
        ApplyButtonTheme(root, "Play AgainButton", ThemeButtonKind.Start);
    }

    private static void ApplyPanelTheme(Transform root, string name, ThemePanelKind kind, Color tint)
    {
        Image image = FindImage(root, name);
        if (image != null)
        {
            UiThemeStyler.ApplyPanel(image, kind, tint);
        }
    }

    private static void ApplyButtonTheme(Transform root, string name, ThemeButtonKind kind)
    {
        Transform match = FindChildRecursive(root, name);
        if (match == null)
        {
            return;
        }

        Button button = match.GetComponent<Button>();
        if (button == null)
        {
            return;
        }

        TMP_Text label = match.GetComponentInChildren<TMP_Text>(true);
        UiThemeStyler.ApplyButton(button, kind, label);
    }
}
