using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace brickapp.Components.Dialogs;

public partial class ImagePreviewDialog
{
    [CascadingParameter] private IMudDialogInstance? MudDialog { get; set; }
    [Parameter] public string ImageUrl { get; set; } = string.Empty;

    private void Close()
    {
        MudDialog?.Close();
    }
}