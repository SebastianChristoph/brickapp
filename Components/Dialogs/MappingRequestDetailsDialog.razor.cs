using brickapp.Data.Entities;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace brickapp.Components.Dialogs;

public partial class MappingRequestDetailsDialog
{
    [Parameter] public MappingRequest? Request { get; set; }
    [Parameter] public string? PartImageUrl { get; set; }
    [CascadingParameter] private IMudDialogInstance? MudDialog { get; set; }
    private void Close() => MudDialog?.Close();
}