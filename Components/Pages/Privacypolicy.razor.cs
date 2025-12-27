namespace brickapp.Components.Pages;

public partial class Privacypolicy
{
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;
        await TrackingService.TrackAsync("ViewPrivacyPolicy", null, "/privacypolicy");
    }
}