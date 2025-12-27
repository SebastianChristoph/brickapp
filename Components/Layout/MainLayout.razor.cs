using MudBlazor;

namespace brickapp.Components.Layout;

public partial class MainLayout
{
    private bool _drawerOpen = true;
    private MudTheme? _theme = null;
    private string? _username;
    private int _newNotificationCount = 0;

    protected override void OnInitialized()
    {
        base.OnInitialized();

        _theme = new()
        {
            PaletteLight = _lightPalette,
            PaletteDark = _darkPalette,
            LayoutProperties = new LayoutProperties(),
            Typography = new Typography()
            {
                Default = new DefaultTypography()
                {
                    FontFamily = new[] { "Poppins", "Roboto", "Helvetica", "Arial", "sans-serif" }
                },
                H1 = new H1Typography()
                {
                    FontFamily = new[] { "BBH Bogle", "Roboto", "Helvetica", "Arial", "sans-serif" },
                    FontWeight = "400"
                },
                H2 = new H2Typography()
                {
                    FontFamily = new[] { "BBH Bogle", "Roboto", "Helvetica", "Arial", "sans-serif" },
                    FontWeight = "400"
                },
                H3 = new H3Typography()
                {
                    FontFamily = new[] { "BBH Bogle", "Roboto", "Helvetica", "Arial", "sans-serif" },
                    FontWeight = "400"
                },
                H4 = new H4Typography()
                {
                    FontFamily = new[] { "BBH Bogle", "Roboto", "Helvetica", "Arial", "sans-serif" },
                    FontWeight = "400"
                },
                H5 = new H5Typography()
                {
                    FontFamily = new[] { "BBH Bogle", "Roboto", "Helvetica", "Arial", "sans-serif" },
                    FontWeight = "300"
                },
                H6 = new H6Typography()
                {
                    FontFamily = new[] { "BBH Bogle", "Roboto", "Helvetica", "Arial", "sans-serif" },
                    FontWeight = "300"
                }
            }
        };
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            Nav.LocationChanged += NavOnLocationChanged;
            await LoadUserAndNotificationsAsync();
        }
    }

    private async void NavOnLocationChanged(object? sender,
        Microsoft.AspNetCore.Components.Routing.LocationChangedEventArgs e)
    {
        await LoadUserAndNotificationsAsync();
        StateHasChanged();
    }

    private async Task LoadUserAndNotificationsAsync()
    {
        var isAuth = await UserService.IsAuthenticatedAsync();
        if (!isAuth)
        {
            _username = null;
            _newNotificationCount = 0;
        }
        else
        {
            var name = await UserService.GetUsernameAsync();
            if (name != _username)
            {
                _username = name;
            }

            await UpdateNotificationCountAsync();
        }

        StateHasChanged();
    }

    private async Task UpdateNotificationCountAsync()
    {
        var userUuid = await UserService.GetTokenAsync();
        if (!string.IsNullOrEmpty(userUuid))
        {
            var notifications = await NotificationService.GetNotificationsForUserAsync(userUuid);
            _newNotificationCount = notifications.Count(n => !n.IsRead);
        }
    }

    public void Dispose()
    {
        Nav.LocationChanged -= NavOnLocationChanged;
    }

    private void GoToNotifications()
    {
        Nav.NavigateTo("/my-notifications");
    }

    private void DrawerToggle()
    {
        _drawerOpen = !_drawerOpen;
    }

    private async Task Logout()
    {
        await TrackingService.TrackAsync("UserLogout", null, null);
        await UserService.LogoutAsync();
        _username = null;
        Nav.NavigateTo("/login", forceLoad: true);
    }

    private readonly PaletteLight _lightPalette = new()
    {
        // === Grundfarben ===
        Secondary = "#49a32b", // Senfgelb / warmes Gold (Hauptakzent, Buttons)
        Primary = "#594ae2", // Mintgrün / gedämpftes Türkis (Sekundärakzent)
        Background = "#fafafa", // Warmes Creme / Off-White (Seitenhintergrund)
        Surface = "#FFFFFF", // Reines Weiß (Cards, Dialoge)

        // === Textfarben ===
        TextPrimary = "#162A41", // Dunkles Anthrazit / Blau-Schwarz (Haupttext)
        TextSecondary = "#4F657A", // Gedämpftes Blau-Grau (Sekundärtext)
        TextDisabled = "#9FAFB5", // Helles Grau-Blau (deaktivierter Text)

        // === AppBar ===
        AppbarBackground = "#594ae2", // Warmes Creme (AppBar-Hintergrund)
        AppbarText = "#FFFFFF", // Dunkles Anthrazit (AppBar-Text & Icons)

        // === Navigation / Drawer ===
        DrawerBackground = "#FFFFFF", // Weiß (Navigation-Hintergrund)
        DrawerText = "#162A41", // Dunkles Anthrazit (Nav-Text)
        DrawerIcon = "#594ae2", // Dunkles Anthrazit (Nav-Icons)

        // === Grautöne ===
        GrayLight = "#E4E0D6", // Helles Warmgrau (Hover, leichte Hintergründe)
        GrayLighter = "#F4F1E9", // Sehr helles Creme-Grau (Sektionen, Flächen)

        // === Statusfarben ===
        Info = "#4A8DA8", // Ruhiges Blau (Info-Hinweise)
        Success = "#6FB98F", // Sanftes Grün (Erfolg / OK)
        Warning = "#EFBB3F", // Senfgelb (Warnungen)
        Error = "#D96C5F", // Warmes Ziegelrot (Fehler)

        // === Linien & Divider ===
        LinesDefault = "#DDD6C7", // Warmes Hellgrau (Tabellenlinien)
        Divider = "#DDD6C7", // Warmes Hellgrau (Trennlinien)

        // === Overlay ===
        OverlayLight = "#162A4114", // Sehr transparentes Anthrazit (Modals / Overlays)
    };

    private readonly PaletteDark _darkPalette = new()
    {
        Primary = "#7e6fff",
        Surface = "#1e1e2d",
        Background = "#1a1a27",
        BackgroundGray = "#151521",
        AppbarText = "#92929f",
        AppbarBackground = "rgba(26,26,39,0.8)",
        DrawerBackground = "#1a1a27",
        ActionDefault = "#74718e",
        ActionDisabled = "#9999994d",
        ActionDisabledBackground = "#605f6d4d",
        TextPrimary = "#b2b0bf",
        TextSecondary = "#92929f",
        TextDisabled = "#ffffff33",
        DrawerIcon = "#92929f",
        DrawerText = "#92929f",
        GrayLight = "#2a2833",
        GrayLighter = "#1e1e2d",
        Info = "#4a86ff",
        Success = "#3dcb6c",
        Warning = "#ffb545",
        Error = "#ff3f5f",
        LinesDefault = "#33323e",
        TableLines = "#33323e",
        Divider = "#292838",
        OverlayLight = "#1e1e2d80",
    };
}