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

public enum SpeciesType { Algae, Snail, Fish, Shrimp }

[ExecuteAlways]
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
    private sealed class UiSpark
    {
        public Image Image;
        public Vector2 Position;
        public Vector2 Velocity;
        public float Lifetime;
        public float MaxLifetime;
    }
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
    private readonly List<EcosystemCardPrefabView> cardViews = new List<EcosystemCardPrefabView>();
    private readonly List<Button> cardButtons = new List<Button>();
    private readonly List<CanvasGroup> cardCanvasGroups = new List<CanvasGroup>();
    private readonly List<RectTransform> cardRoots = new List<RectTransform>();
    private readonly List<Image> cardShines = new List<Image>();
    private readonly List<UiSpark> uiSparks = new List<UiSpark>();

    private GameState state = GameState.Menu;
    private DifficultyMode difficulty = DifficultyMode.Normal;
    private StartingJar startingJar = StartingJar.Balanced;

    private Sprite whiteSprite;
    private Canvas canvas;
    private Camera sceneCamera;
    private Shader foilShader;
    private GameObject nueCardPrefab;
    private Sprite shirtFrontSprite;
    private Sprite shirtAccentSprite;
    private RectTransform jarArea;
    private RectTransform rightPanelRect;
    private RectTransform jarRect;
    private RectTransform drawPileMarker;
    private RectTransform discardPileMarker;
    private Image water;
    private Image lightGlow;
    private Image playFlash;
    private GameObject menuPanel;
    private GameObject resultPanel;
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
    private int hoveredCardIndex = -1;
    private bool isResolvingCard;
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

    private void Awake()
    {
        if (!Application.isPlaying)
        {
            foilShader = Shader.Find("Custom/CardFoilUI");
            BuildVisuals();
            return;
        }

        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        nueCardPrefab = Resources.Load<GameObject>("EcosystemCards/CardUI");
        shirtFrontSprite = Resources.Load<Sprite>("EcosystemCards/Shirts/Card_shirt_01");
        shirtAccentSprite = Resources.Load<Sprite>("EcosystemCards/Shirts/Card_shirt_04");
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
            foilShader = Shader.Find("Custom/CardFoilUI");
            nueCardPrefab = Resources.Load<GameObject>("EcosystemCards/CardUI");
            shirtFrontSprite = Resources.Load<Sprite>("EcosystemCards/Shirts/Card_shirt_01");
            shirtAccentSprite = Resources.Load<Sprite>("EcosystemCards/Shirts/Card_shirt_04");
            BuildVisuals();
        }
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (state == GameState.Playing && !isResolvingCard && Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) TickDay();
        UpdateCardPresentation();
        UpdateSparks();
        UpdateScreenShake();
        UpdateWater();
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

        GameObject bg = Panel("BG", canvas.transform, new Color(0.31f, 0.25f, 0.19f));
        Stretch(bg.GetComponent<RectTransform>());
        GameObject felt = Panel("Felt", canvas.transform, new Color(0.17f, 0.28f, 0.2f, 0.9f));
        Place(felt.GetComponent<RectTransform>(), new Vector2(0.24f, 0.02f), new Vector2(0.99f, 0.99f), Vector2.zero, Vector2.zero);
        GameObject vignetteTop = Panel("VignetteTop", canvas.transform, new Color(0.02f, 0.03f, 0.03f, 0.16f));
        Place(vignetteTop.GetComponent<RectTransform>(), new Vector2(0f, 0.88f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        GameObject vignetteBottom = Panel("VignetteBottom", canvas.transform, new Color(0.02f, 0.03f, 0.03f, 0.2f));
        Place(vignetteBottom.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 0.16f), Vector2.zero, Vector2.zero);

        GameObject left = Panel("Left", canvas.transform, new Color(0.1f, 0.17f, 0.15f, 0.97f));
        Place(left.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0.28f, 1f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI title = Label("Title", left.transform, 38, FontStyles.Bold, TextAlignmentOptions.TopLeft);
        Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -24f), new Vector2(-24f, -82f));
        title.text = "Glass World";
        title.color = Color.white;
        TextMeshProUGUI desc = Label("Desc", left.transform, 17, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        Place(desc.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -84f), new Vector2(-24f, -144f));
        desc.text = "Keep 3 cards in hand. Play 1 each day, then draw 1 replacement.";
        desc.color = new Color(0.83f, 0.91f, 0.87f);

        statsText = Label("Stats", left.transform, 18, FontStyles.Bold, TextAlignmentOptions.TopLeft);
        Place(statsText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -156f), new Vector2(-24f, -260f));
        statsText.color = Color.white;
        warningText = Label("Warnings", left.transform, 15, FontStyles.Bold, TextAlignmentOptions.TopLeft);
        Place(warningText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -264f), new Vector2(-24f, -342f));
        warningText.color = new Color(0.98f, 0.84f, 0.4f);
        eventText = Label("Event", left.transform, 14, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        Place(eventText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -346f), new Vector2(-24f, -410f));
        eventText.color = new Color(0.84f, 0.9f, 0.95f);
        selectedText = Label("Selected", left.transform, 14, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        Place(selectedText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -414f), new Vector2(-24f, -492f));
        selectedText.color = new Color(0.88f, 0.95f, 0.9f);
        reportText = Label("Report", left.transform, 14, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        Place(reportText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(24f, 180f), new Vector2(-24f, 24f));
        reportText.color = new Color(0.84f, 0.92f, 0.88f);

        Button next = CreateUiButton("Play Selected Card", left.transform, new Color(0.38f, 0.74f, 0.51f), TickDay);
        Place(next.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(24f, 230f), new Vector2(-24f, 290f));
        Button restart = CreateUiButton("Restart Run", left.transform, new Color(0.86f, 0.42f, 0.34f), StartGame);
        Place(restart.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(24f, 302f), new Vector2(-24f, 362f));

        GameObject right = Panel("Right", canvas.transform, new Color(0.2f, 0.3f, 0.24f, 0.08f));
        Place(right.GetComponent<RectTransform>(), new Vector2(0.3f, 0.04f), new Vector2(0.98f, 0.97f), Vector2.zero, Vector2.zero);
        rightPanelRect = right.GetComponent<RectTransform>();
        Outline o = right.AddComponent<Outline>();
        o.effectColor = new Color(0.16f, 0.28f, 0.22f, 0.8f);
        o.effectDistance = new Vector2(4f, 4f);

        bannerText = Label("Banner", right.transform, 28, FontStyles.Bold, TextAlignmentOptions.Top);
        Place(bannerText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -18f), new Vector2(-24f, -70f));
        bannerText.color = new Color(0.13f, 0.2f, 0.16f);
        deckText = Label("Deck", right.transform, 17, FontStyles.Bold, TextAlignmentOptions.TopLeft);
        Place(deckText.rectTransform, new Vector2(0f, 1f), new Vector2(0.35f, 1f), new Vector2(24f, -76f), new Vector2(-6f, -128f));
        deckText.color = new Color(0.14f, 0.2f, 0.17f);
        drawPileMarker = CreatePileMarker("DrawPileMarker", right.transform, new Vector2(122f, -152f), "Draw");
        discardPileMarker = CreatePileMarker("DiscardPileMarker", right.transform, new Vector2(248f, -152f), "Discard");
        speciesText = Label("Species", right.transform, 17, FontStyles.Bold, TextAlignmentOptions.TopRight);
        Place(speciesText.rectTransform, new Vector2(0.45f, 1f), new Vector2(1f, 1f), new Vector2(6f, -76f), new Vector2(-24f, -154f));
        speciesText.color = new Color(0.14f, 0.2f, 0.17f);

        GameObject jar = Panel("Jar", right.transform, new Color(0.81f, 0.92f, 0.95f, 0.18f));
        Place(jar.GetComponent<RectTransform>(), new Vector2(0.08f, 0.22f), new Vector2(0.92f, 0.78f), Vector2.zero, Vector2.zero);
        jarRect = jar.GetComponent<RectTransform>();
        Outline jarOutline = jar.AddComponent<Outline>();
        jarOutline.effectColor = new Color(0.18f, 0.32f, 0.27f, 0.8f);
        jarOutline.effectDistance = new Vector2(4f, 4f);
        Shadow jarShadow = jar.AddComponent<Shadow>();
        jarShadow.effectColor = new Color(0f, 0f, 0f, 0.22f);
        jarShadow.effectDistance = new Vector2(0f, -12f);
        water = Panel("Water", jar.transform, new Color(0.62f, 0.86f, 0.96f, 0.5f)).GetComponent<Image>();
        Place(water.rectTransform, new Vector2(0.05f, 0.12f), new Vector2(0.95f, 0.9f), Vector2.zero, Vector2.zero);
        lightGlow = Panel("LightGlow", jar.transform, new Color(1f, 0.97f, 0.74f, 0.16f)).GetComponent<Image>();
        Place(lightGlow.rectTransform, new Vector2(0.08f, 0.72f), new Vector2(0.92f, 0.95f), Vector2.zero, Vector2.zero);
        playFlash = Panel("PlayFlash", jar.transform, new Color(1f, 1f, 1f, 0f)).GetComponent<Image>();
        Stretch(playFlash.rectTransform);
        GameObject layer = new GameObject("Organisms");
        jarArea = layer.AddComponent<RectTransform>();
        jarArea.SetParent(jar.transform, false);
        Place(jarArea, new Vector2(0.08f, 0.14f), new Vector2(0.92f, 0.88f), Vector2.zero, Vector2.zero);

        TextMeshProUGUI handLabel = Label("HandLabel", right.transform, 24, FontStyles.Bold, TextAlignmentOptions.TopLeft);
        Place(handLabel.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(24f, 210f), new Vector2(-24f, 248f));
        handLabel.text = "Today's Hand";
        handLabel.color = new Color(0.13f, 0.2f, 0.16f);
        CreateCardSlots(right.transform);

        tooltipPanel = Panel("Tooltip", canvas.transform, new Color(0.05f, 0.09f, 0.1f, 0.92f));
        Place(tooltipPanel.GetComponent<RectTransform>(), new Vector2(0.68f, 0.02f), new Vector2(0.96f, 0.12f), Vector2.zero, Vector2.zero);
        tooltipText = Label("TooltipText", tooltipPanel.transform, 15, FontStyles.Normal, TextAlignmentOptions.Center);
        Stretch(tooltipText.rectTransform);
        tooltipText.color = Color.white;
        tooltipPanel.SetActive(false);

        menuPanel = Panel("Menu", canvas.transform, new Color(0.05f, 0.09f, 0.1f, 0.85f));
        Stretch(menuPanel.GetComponent<RectTransform>());
        TextMeshProUGUI mt = Label("MenuTitle", menuPanel.transform, 56, FontStyles.Bold, TextAlignmentOptions.Center);
        Place(mt.rectTransform, new Vector2(0.18f, 0.56f), new Vector2(0.82f, 0.8f), Vector2.zero, Vector2.zero);
        mt.text = "Glass World";
        mt.color = Color.white;
        TextMeshProUGUI mb = Label("MenuBody", menuPanel.transform, 24, FontStyles.Normal, TextAlignmentOptions.Center);
        Place(mb.rectTransform, new Vector2(0.16f, 0.36f), new Vector2(0.84f, 0.56f), Vector2.zero, Vector2.zero);
        mb.text = "Keep a hand of 3 cards. Play 1 each day, draw 1 replacement, and adapt to the jar.";
        mb.color = new Color(0.87f, 0.93f, 0.9f);
        Button play = CreateUiButton("Start Prototype", menuPanel.transform, new Color(0.39f, 0.78f, 0.54f), StartGame);
        Place(play.GetComponent<RectTransform>(), new Vector2(0.39f, 0.22f), new Vector2(0.61f, 0.31f), Vector2.zero, Vector2.zero);

        resultPanel = Panel("Result", canvas.transform, new Color(0.05f, 0.09f, 0.1f, 0.85f));
        Stretch(resultPanel.GetComponent<RectTransform>());
        resultText = Label("ResultText", resultPanel.transform, 44, FontStyles.Bold, TextAlignmentOptions.Center);
        Place(resultText.rectTransform, new Vector2(0.14f, 0.38f), new Vector2(0.86f, 0.7f), Vector2.zero, Vector2.zero);
        resultText.color = Color.white;
        Button again = CreateUiButton("Play Again", resultPanel.transform, new Color(0.42f, 0.75f, 0.94f), StartGame);
        Place(again.GetComponent<RectTransform>(), new Vector2(0.4f, 0.22f), new Vector2(0.6f, 0.3f), Vector2.zero, Vector2.zero);
        resultPanel.SetActive(false);
    }

    private bool BindExistingVisuals(GameObject canvasObject)
    {
        ClearVisualCaches();
        canvas = canvasObject.GetComponent<Canvas>();
        rightPanelRect = FindRect(canvasObject.transform, "Right");
        jarRect = FindRect(canvasObject.transform, "Jar");
        jarArea = FindRect(canvasObject.transform, "Organisms");
        drawPileMarker = FindRect(canvasObject.transform, "DrawPileMarker");
        discardPileMarker = FindRect(canvasObject.transform, "DiscardPileMarker");
        water = FindImage(canvasObject.transform, "Water");
        lightGlow = FindImage(canvasObject.transform, "LightGlow");
        playFlash = FindImage(canvasObject.transform, "PlayFlash");
        menuPanel = FindObject(canvasObject.transform, "Menu");
        resultPanel = FindObject(canvasObject.transform, "Result");
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

        if (canvas == null || rightPanelRect == null || jarRect == null || jarArea == null || bannerText == null || menuPanel == null || resultPanel == null)
        {
            return false;
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

        RebindNamedButton(canvasObject.transform, "Play Selected CardButton", TickDay);
        RebindNamedButton(canvasObject.transform, "Restart RunButton", StartGame);
        RebindNamedButton(canvasObject.transform, "Start PrototypeButton", StartGame);
        RebindNamedButton(canvasObject.transform, "Play AgainButton", StartGame);
        return true;
    }

    private void EnsurePresentationWorld()
    {
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
            }
        }

        sceneCamera.orthographic = false;
        sceneCamera.fieldOfView = 34f;
        sceneCamera.nearClipPlane = 0.1f;
        sceneCamera.farClipPlane = 200f;
        sceneCamera.clearFlags = CameraClearFlags.SolidColor;
        sceneCamera.backgroundColor = new Color(0.08f, 0.11f, 0.1f);
        sceneCamera.transform.position = new Vector3(0f, 2.7f, -9.4f);
        sceneCamera.transform.rotation = Quaternion.Euler(14f, 0f, 0f);
        if (sceneCamera.GetComponent<UniversalAdditionalCameraData>() == null)
        {
            sceneCamera.gameObject.AddComponent<UniversalAdditionalCameraData>();
        }

        if (GameObject.Find("CardTableLight") == null)
        {
            GameObject lightObject = new GameObject("CardTableLight");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.color = new Color(1f, 0.96f, 0.9f);
            light.shadows = LightShadows.Soft;
            lightObject.transform.rotation = Quaternion.Euler(44f, -26f, 0f);
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
            slotRect.sizeDelta = new Vector2(270f, 370f);
            slotRect.anchoredPosition = new Vector2((i - 1) * 250f, 18f);
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
        if (Application.isPlaying && SceneManager.GetActiveScene().path == "Assets/Scenes/SampleScene.unity")
        {
            StartGame();
            return;
        }

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
        SetupDayPresentation();
    }

    private void SetupDayPresentation()
    {
        latestWarnings = "Choose 1 card to shape the jar.";
        dayReport = jarName + "\nDay " + day + "\nPlan carefully before resolving the ecosystem.";
        if (bannerText != null) bannerText.text = jarName + "  Day " + day + " - Play 1 card, then draw 1.";
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
        if (state != GameState.Playing || isResolvingCard) return;
        if (selected.Count == 0) { latestWarnings = "Play 1 card before ending the day."; RefreshHud(); return; }
        StartCoroutine(ResolveSelectedCardRoutine(selected[0]));
    }

    private IEnumerator ResolveSelectedCardRoutine(CardDef playedCard)
    {
        isResolvingCard = true;
        int playedIndex = hand.IndexOf(playedCard);
        if (playedIndex >= 0 && playedIndex < cardRoots.Count && jarRect != null)
        {
            Vector3 worldTarget = jarRect.TransformPoint(jarRect.rect.center);
            Vector2 jarTarget = (Vector2)rightPanelRect.InverseTransformPoint(worldTarget);
            StartCardAnimation(playedIndex, jarTarget, 0f, 1.08f, 1.18f, 1f, 1f, 0.22f);
            if (playFlash != null) playFlash.color = new Color(1f, 1f, 1f, 0.22f);
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
        currentEvent.Apply(turn);
        playedCard.Apply(turn);

        RemoveSpecies(SpeciesType.Fish, turn.RemoveFish);
        RemoveSpecies(SpeciesType.Snail, turn.RemoveSnail);
        AddSpecies(SpeciesType.Fish, turn.AddFish);
        AddSpecies(SpeciesType.Snail, turn.AddSnail);
        ApplyAlgaeChange(turn.AlgaeBonus);
        if (turn.FilterDays > 0) filterDaysRemaining = Mathf.Max(filterDaysRemaining, turn.FilterDays);
        if (turn.WasteDays > 0) wasteDaysRemaining = Mathf.Max(wasteDaysRemaining, turn.WasteDays);

        ResolveSimulation(turn);
        hand.Remove(playedCard);
        discardPile.Add(playedCard);
        selected.Clear();
        hoveredCardIndex = -1;
        RefreshHud();
        if (state != GameState.Playing)
        {
            isResolvingCard = false;
            yield break;
        }
        yield return new WaitForSecondsRealtime(0.14f);
        day++;
        int handCountBeforeDraw = hand.Count;
        DrawToFullHand();
        currentEvent = RollEvent();
        SetupDayPresentation();
        if (hand.Count > handCountBeforeDraw)
        {
            int drawnIndex = hand.Count - 1;
            StartDrawAnimation(drawnIndex);
            yield return WaitForDrawAnimation();
        }
        isResolvingCard = false;
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
            bool canInteract = isSelected || selected.Count < 1;
            if (i < cardViews.Count && cardViews[i] != null)
            {
                cardViews[i].SetCard(card.Name, card.Summary, card.Category.ToString(), card.Risk, card.Color, GetCardArtSprite(card), GetCardFrameSprite(card), GetCardRarity(card), isSelected, canInteract);
            }
            else
            {
                TMP_Text label = cardButtons[i].GetComponentInChildren<TMP_Text>();
                if (label != null) label.text = isSelected ? "Selected" : (selected.Count >= 1 ? "Pick 1 Only" : "Select");
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
                if (playFlash != null)
                {
                    Color flash = playFlash.color;
                    flash.a = Mathf.Lerp(0.22f, 0f, eased);
                    playFlash.color = flash;
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

        tooltipText.text = hand[index].Name + "\n" + hand[index].Summary;
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

    private void ClearVisualCaches()
    {
        cardButtons.Clear();
        cardViews.Clear();
        cardCanvasGroups.Clear();
        cardRoots.Clear();
        cardShines.Clear();
        hoveredCardIndex = -1;
        animatingCardIndex = -1;
        animatingDrawCardIndex = -1;
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

        return card.Category == CardCategory.Water || card.Category == CardCategory.Light ? RarityType.Rare : RarityType.Common;
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
    private static Button CreateUiButton(string text, Transform parent, Color color, UnityEngine.Events.UnityAction onClick) { GameObject go = Panel(text + "Button", parent, color); Button b = go.AddComponent<Button>(); ColorBlock cb = b.colors; cb.highlightedColor = Color.Lerp(color, Color.white, 0.18f); cb.pressedColor = Color.Lerp(color, Color.black, 0.12f); b.colors = cb; b.onClick.AddListener(onClick); TextMeshProUGUI t = Label(text + "Text", go.transform, 18, FontStyles.Bold, TextAlignmentOptions.Center); t.text = text; t.color = new Color(0.11f, 0.15f, 0.19f); Stretch(t.rectTransform); return b; }
    private static TextMeshProUGUI Label(string name, Transform parent, int size, FontStyles style, TextAlignmentOptions alignment) { GameObject go = new GameObject(name); RectTransform rt = go.AddComponent<RectTransform>(); rt.SetParent(parent, false); TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>(); t.font = TmpFontUtility.GetFont(); t.fontSize = size; t.fontStyle = style; t.alignment = alignment; t.enableWordWrapping = true; t.overflowMode = TextOverflowModes.Overflow; return t; }
    private static void Stretch(RectTransform rt) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero; }
    private static void Place(RectTransform rt, Vector2 min, Vector2 max, Vector2 offMin, Vector2 offMax) { rt.anchorMin = min; rt.anchorMax = max; rt.offsetMin = offMin; rt.offsetMax = offMax; }
}
