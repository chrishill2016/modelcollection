using MudBlazor;

namespace ModelCollection.Shared;

public static class AppTheme
{
    public static readonly MudTheme Theme = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#c62828",
            Secondary = "#455a64",
            Background = "#f5f6f8",
            Surface = "#ffffff",
            AppbarBackground = "#ffffff",
            AppbarText = "#1f2328",
            DrawerBackground = "#ffffff",
            DrawerText = "#1f2328",
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#ef5350",
            Secondary = "#90a4ae",
            Background = "#121417",
            Surface = "#1c1f24",
            AppbarBackground = "#1c1f24",
            DrawerBackground = "#16191d",
        },
    };
}
