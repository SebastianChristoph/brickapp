using brickapp.Data.Entities;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using MudBlazor;

namespace brickapp.Components.Dialogs;

public partial class EditItemDialog
{
    [CascadingParameter] private IMudDialogInstance? MudDialog { get; set; }

    [Parameter] public int CurrentQuantity { get; set; }

    [Parameter] public int CurrentColorId { get; set; }

    private int _quantity = 1;
    private int _selectedColorId;
    private List<BrickColor>? _colors;

    protected override async Task OnInitializedAsync()
    {
        _quantity = CurrentQuantity;
        _selectedColorId = CurrentColorId;

        // Lade alle verfügbaren Farben
        await using var db = await DbFactory.CreateDbContextAsync();
        _colors = await db.BrickColors
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    private void Cancel()
    {
        MudDialog?.Cancel();
    }

    private void Save()
    {
        var result = new EditItemDialogResult
        {
            Quantity = _quantity,
            ColorId = _selectedColorId
        };
        MudDialog?.Close(result);
    }
}