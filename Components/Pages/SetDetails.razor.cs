// ...using directives...
using brickisbrickapp.Components.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using brickisbrickapp.Data.Entities;
using brickisbrickapp.Helpers;
using brickisbrickapp.Services;
using MudBlazor;

namespace brickisbrickapp.Components.Pages
{
    public partial class SetDetails : ComponentBase
    {


    [Inject]
    public LoadingService? LoadingService { get; set; }

    [Inject]
    public RebrickablePartImageService RebrickablePartImageService { get; set; } = default!;


        private Dictionary<string, string> _partImageUrls = new();
        protected int TotalNeeded => _itemSet?.Bricks?.Sum(b => b.Quantity) ?? 0;
        protected int TotalOwned => _itemSet?.Bricks?.Sum(b => Math.Min(GetOwnedQuantity(b), b.Quantity)) ?? 0;
        protected int Percent => (TotalNeeded > 0 && TotalOwned == TotalNeeded)
            ? 100
            : (TotalNeeded > 0 ? (int)Math.Floor(100.0 * (double)TotalOwned / TotalNeeded) : 0);
        protected bool HasAll => TotalNeeded > 0 && TotalOwned == TotalNeeded;
        [Parameter]
        public int itemSetId { get; set; }

        private ItemSet? _itemSet;
        private UserItemSet? _userItemSet;
        private bool _loading = true;

        // Images temporarily disabled due to API ban

        // Menge, die der User pro (Brick, Farbe) besitzt
        // Für jede (Brick, Farbe): Dictionary<Brand, Menge>
        private Dictionary<(int BrickId, int ColorId), Dictionary<string, int>> _ownedByBrand = new();

        private enum BrickOwnershipState
        {
            None,
            Partial,
            Enough
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender) return;

            // Versuche das Set aus dem User-Inventory zu laden
            _userItemSet = await ItemSetService.GetCurrentUserItemSetAsync(itemSetId);

            // Falls nicht im User-Inventory, lade das Set generisch
            if (_userItemSet != null)
            {
                _itemSet = _userItemSet.ItemSet;
            }
            else
            {
                // Fallback: Lade das Set direkt
                _itemSet = await ItemSetService.GetItemSetByIdAsync(itemSetId);
            }

            // Inventory laden
            var inventory = await InventoryService.GetCurrentUserInventoryAsync();

            _ownedByBrand = inventory
                .GroupBy(i => (i.MappedBrickId, i.BrickColorId))
                .ToDictionary(
                    g => g.Key,
                    g => g.GroupBy(i => i.Brand)
                        .ToDictionary(
                            bg => bg.Key,
                            bg => bg.Sum(i => i.Quantity)
                        )
                );

            // Lade die Bild-URLs für die Bricks des Sets

            if (_itemSet?.Bricks != null && _itemSet.Bricks.Any())
            {
                var partNumsToLoad = _itemSet.Bricks
                    .Select(b => b.MappedBrick.LegoPartNum)
                    .Where(p => !string.IsNullOrEmpty(p) && !_partImageUrls.ContainsKey(p))
                    .Distinct()
                    .Select(p => p!)
                    .ToList();
                if (partNumsToLoad.Count > 0)
                {
                    // Bilder asynchron laden, aber Seite schon anzeigen (Lazy Loading)
                    _ = Task.Run(async () => {
                        var urls = await RebrickablePartImageService.GetPartImageUrlsBatchAsync(partNumsToLoad);
                        foreach (var kv in urls)
                        {
                            _partImageUrls[kv.Key] = kv.Value;
                        }
                        await InvokeAsync(StateHasChanged);
                    });
                }
            }

            _loading = false;

            StateHasChanged();
        }

        private void GoBack()
        {
            Nav.NavigateTo("/allsets");
        }

        private async Task OpenAddSetDialog()
        {
            if (_itemSet?.Bricks == null || !_itemSet.Bricks.Any())
                return;


            var itemCount = _itemSet.Bricks.Count;
            var parameters = new DialogParameters<AddSetToInventoryDialog>();
            parameters.Add(x => x.ItemCount, itemCount);
            parameters.Add(x => x.ItemSetId, itemSetId);

            var dialog = await DialogService.ShowAsync<AddSetToInventoryDialog>("Set zum Inventar hinzufügen", parameters);
            var result = await dialog.Result;

            if (result != null && !result.Canceled && result.Data is bool confirmed && confirmed)
            {
                // Spinner wurde bereits im Dialog angezeigt und wieder ausgeblendet
                // Optional: Erfolgs-Nachricht oder Daten neu laden
            }
        }

        private BrickOwnershipState GetOwnershipState(ItemSetBrick brick)
        {
            if (!_ownedByBrand.TryGetValue((brick.MappedBrickId, brick.BrickColorId), out var brandDict) || brandDict.Values.Sum() <= 0)
                return BrickOwnershipState.None;

            var total = brandDict.Values.Sum();
            if (total < brick.Quantity)
                return BrickOwnershipState.Partial;

            return BrickOwnershipState.Enough;
        }

        private int GetOwnedQuantity(ItemSetBrick brick)
        {
            return _ownedByBrand.TryGetValue((brick.MappedBrickId, brick.BrickColorId), out var brandDict)
                ? brandDict.Values.Sum()
                : 0;
        }



    }
}
