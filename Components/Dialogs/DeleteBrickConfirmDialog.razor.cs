using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace brickapp.Components.Dialogs;

public partial class DeleteBrickConfirmDialog
{
    [CascadingParameter] private IMudDialogInstance? MudDialog { get; set; }

    [Parameter] public string Message { get; set; } = "Do you really want to delete this item?";

    private void Cancel()
    {
        MudDialog?.Cancel();
    }

    private void Confirm()
    {
        MudDialog?.Close(true);
    }
}