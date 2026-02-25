using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace BattleshipMaui.ViewModels;

public static class ThemeTokenService
{
    public static void Apply(bool highContrast, bool largeText)
    {
        var resources = Application.Current?.Resources;
        if (resources is null)
            return;

        var palette = highContrast
            ? new ThemePalette(
                Background: "#000000",
                Surface: "#0f0f0f",
                SurfaceAlt: "#161616",
                Panel: "#1a1a1a",
                Border: "#ffffff",
                Accent: "#00d4ff",
                AccentSoft: "#00a0c1",
                TextPrimary: "#ffffff",
                TextSecondary: "#e8e8e8",
                TextMuted: "#b7b7b7",
                Success: "#59ffad",
                Warning: "#ffd66b",
                Danger: "#ff7f7f")
            : new ThemePalette(
                Background: "#0b1118",
                Surface: "#0f1823",
                SurfaceAlt: "#152334",
                Panel: "#13211b",
                Border: "#314052",
                Accent: "#4cb5ff",
                AccentSoft: "#2f84bf",
                TextPrimary: "#e5eef8",
                TextSecondary: "#ccd7e5",
                TextMuted: "#9fb6cc",
                Success: "#7fe3ab",
                Warning: "#f8dca1",
                Danger: "#ff8a6b");

        SetColor(resources, "GameColorBackground", palette.Background);
        SetColor(resources, "GameColorSurface", palette.Surface);
        SetColor(resources, "GameColorSurfaceAlt", palette.SurfaceAlt);
        SetColor(resources, "GameColorPanel", palette.Panel);
        SetColor(resources, "GameColorBorder", palette.Border);
        SetColor(resources, "GameColorAccent", palette.Accent);
        SetColor(resources, "GameColorAccentSoft", palette.AccentSoft);
        SetColor(resources, "GameColorTextPrimary", palette.TextPrimary);
        SetColor(resources, "GameColorTextSecondary", palette.TextSecondary);
        SetColor(resources, "GameColorTextMuted", palette.TextMuted);
        SetColor(resources, "GameColorSuccess", palette.Success);
        SetColor(resources, "GameColorWarning", palette.Warning);
        SetColor(resources, "GameColorDanger", palette.Danger);

        double scale = largeText ? 1.18 : 1.0;
        SetDouble(resources, "GameTypeDisplay", 30 * scale);
        SetDouble(resources, "GameTypeTitle", 18 * scale);
        SetDouble(resources, "GameTypeBody", 14 * scale);
        SetDouble(resources, "GameTypeCaption", 12 * scale);
    }

    private static void SetColor(ResourceDictionary resources, string key, string value)
    {
        resources[key] = Color.FromArgb(value);
    }

    private static void SetDouble(ResourceDictionary resources, string key, double value)
    {
        resources[key] = value;
    }
}

file sealed record ThemePalette(
    string Background,
    string Surface,
    string SurfaceAlt,
    string Panel,
    string Border,
    string Accent,
    string AccentSoft,
    string TextPrimary,
    string TextSecondary,
    string TextMuted,
    string Success,
    string Warning,
    string Danger);
