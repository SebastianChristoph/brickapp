using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace brickapp.Components.Dialogs;

public partial class TokenDisplayDialog
{
    [CascadingParameter] private IMudDialogInstance? MudDialog { get; set; }

    [Parameter] public string Token { get; set; } = string.Empty;

    [Parameter] public string? Username { get; set; }

    private async Task CopyTokenToClipboard()
    {
        await JsRuntime.InvokeVoidAsync("navigator.clipboard.writeText", Token);
        Snackbar.Add("Token copied to clipboard!", Severity.Success);
    }

    private void CloseAndLogin()
    {
        MudDialog?.Close(DialogResult.Ok(true));
        Nav.NavigateTo("/login", true);
    }
}