using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace brickapp.Components.Dialogs;

public partial class RejectMappingRequestDialog
{
    private string _reason = string.Empty;
    [Parameter] public Data.Entities.MappingRequest? Request { get; set; }

    [CascadingParameter] private IMudDialogInstance? MudDialog { get; set; }
}