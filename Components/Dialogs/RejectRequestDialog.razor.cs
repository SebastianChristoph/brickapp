using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace brickapp.Components.Dialogs;

public partial class RejectRequestDialog
{
    [CascadingParameter] private IMudDialogInstance? MudDialog { get; set; }
    [Parameter] public string? DefaultReason { get; set; }
    private string _reason = "";

    protected override void OnInitialized()
    {
        _reason = DefaultReason ?? "manual";
    }

    private void Cancel()
    {
        MudDialog?.Close(DialogResult.Cancel());
    }

    private void Reject()
    {
        MudDialog?.Close(DialogResult.Ok(new RejectDialogResult
        {
            Action = RejectAction.Reject,
            Reason = _reason.Trim()
        }));
    }

    private void RejectToPending()
    {
        MudDialog?.Close(DialogResult.Ok(new RejectDialogResult
        {
            Action = RejectAction.RejectToPending,
            Reason = _reason.Trim()
        }));
    }
}