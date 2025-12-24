using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace brickapp.Components.Dialogs;

public partial class DeleteConfirmDialog
{
    [CascadingParameter] private IMudDialogInstance? MudDialog { get; set; }

    private void Cancel()
    {
        MudDialog?.Cancel();
    }

    private void Confirm()
    {
        MudDialog?.Close(true);
    }
}