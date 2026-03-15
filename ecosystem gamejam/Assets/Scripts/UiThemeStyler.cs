using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum ThemePanelKind
{
    Large,
    Medium,
    Small,
    Notice
}

public enum ThemeButtonKind
{
    Primary,
    Secondary,
    Danger,
    Start
}

public static class UiThemeStyler
{
    private static Sprite boardLarge;
    private static Sprite boardMedium;
    private static Sprite boardSmall;
    private static Sprite notice;
    private static Sprite buttonBig;
    private static Sprite buttonBigPressed;
    private static Sprite buttonMedium;
    private static Sprite buttonMediumPressed;
    private static Sprite buttonCancel;
    private static Sprite buttonCancelPressed;
    private static Sprite buttonStart;
    private static Sprite buttonStartPressed;

    public static void ApplyPanel(Image image, ThemePanelKind kind, Color tint)
    {
        if (image == null)
        {
            return;
        }

        EnsureLoaded();
        image.sprite = kind switch
        {
            ThemePanelKind.Large => boardLarge,
            ThemePanelKind.Medium => boardMedium,
            ThemePanelKind.Small => boardSmall,
            ThemePanelKind.Notice => notice,
            _ => boardMedium
        };
        image.type = Image.Type.Simple;
        image.color = tint;
        image.raycastTarget = false;
    }

    public static void ApplyButton(Button button, ThemeButtonKind kind, TMP_Text label = null)
    {
        if (button == null)
        {
            return;
        }

        EnsureLoaded();
        Image image = button.GetComponent<Image>();
        if (image == null)
        {
            return;
        }

        Sprite normal;
        Sprite pressed;
        Color labelColor;
        switch (kind)
        {
            case ThemeButtonKind.Start:
                normal = buttonStart != null ? buttonStart : buttonBig;
                pressed = buttonStartPressed != null ? buttonStartPressed : buttonBigPressed;
                labelColor = new Color(0.17f, 0.12f, 0.08f);
                break;
            case ThemeButtonKind.Danger:
                normal = buttonCancel != null ? buttonCancel : buttonMedium;
                pressed = buttonCancelPressed != null ? buttonCancelPressed : buttonMediumPressed;
                labelColor = new Color(0.22f, 0.08f, 0.06f);
                break;
            case ThemeButtonKind.Secondary:
                normal = buttonMedium;
                pressed = buttonMediumPressed;
                labelColor = new Color(0.19f, 0.13f, 0.08f);
                break;
            default:
                normal = buttonBig;
                pressed = buttonBigPressed;
                labelColor = new Color(0.17f, 0.12f, 0.08f);
                break;
        }

        image.sprite = normal;
        image.type = Image.Type.Simple;
        image.color = Color.white;

        SpriteState state = button.spriteState;
        state.pressedSprite = pressed;
        state.highlightedSprite = pressed != null ? pressed : normal;
        state.selectedSprite = normal;
        button.spriteState = state;
        button.transition = Selectable.Transition.SpriteSwap;

        if (label != null)
        {
            label.color = labelColor;
            label.fontStyle = FontStyles.Bold;
        }
    }

    public static void ApplyTitle(TMP_Text text)
    {
        if (text == null)
        {
            return;
        }

        text.color = new Color(0.2f, 0.14f, 0.08f);
        text.fontStyle = FontStyles.Bold;
    }

    private static void EnsureLoaded()
    {
        if (boardLarge != null)
        {
            return;
        }

        boardLarge = Resources.Load<Sprite>("UiTheme/Wood/board_large_parchment");
        boardMedium = Resources.Load<Sprite>("UiTheme/Wood/board_medium_parchment");
        boardSmall = Resources.Load<Sprite>("UiTheme/Wood/board_small_parchment");
        notice = Resources.Load<Sprite>("UiTheme/Wood/notice");
        buttonBig = Resources.Load<Sprite>("UiTheme/Wood/button_big");
        buttonBigPressed = Resources.Load<Sprite>("UiTheme/Wood/button_big_pressed");
        buttonMedium = Resources.Load<Sprite>("UiTheme/Wood/button_medium");
        buttonMediumPressed = Resources.Load<Sprite>("UiTheme/Wood/button_medium_pressed");
        buttonCancel = Resources.Load<Sprite>("UiTheme/Wood/button_cancel");
        buttonCancelPressed = Resources.Load<Sprite>("UiTheme/Wood/button_cancel_pressed");
        buttonStart = Resources.Load<Sprite>("UiTheme/Wood/button_start");
        buttonStartPressed = Resources.Load<Sprite>("UiTheme/Wood/button_start_pressed");
    }
}
