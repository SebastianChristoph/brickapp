using System.Collections.Generic;
using System.Threading.Tasks;
using Data.Entities;
using Services;

namespace Helpers;

public static class InventorySetHelper
{
    // Gibt alle Sets zurück, die mit dem aktuellen Inventory gebaut werden können
    public static async Task<List<ItemSet>> GetBuildableSetsAsync(
        List<ItemSet> allSets,
        List<InventoryItem> inventory)
    {
        var inventoryDict = new Dictionary<(int BrickId, int ColorId), int>();
        foreach (var item in inventory)
        {
            var key = (item.MappedBrickId, item.BrickColorId);
            if (!inventoryDict.ContainsKey(key))
                inventoryDict[key] = 0;
            inventoryDict[key] += item.Quantity;
        }

        var buildable = new List<ItemSet>();
        foreach (var set in allSets)
        {
            if (set.Bricks == null || set.Bricks.Count == 0)
                continue;
            bool canBuild = true;
            foreach (var brick in set.Bricks)
            {
                var key = (brick.MappedBrickId, brick.BrickColorId);
                if (!inventoryDict.TryGetValue(key, out var owned) || owned < brick.Quantity)
                {
                    canBuild = false;
                    break;
                }
            }
            if (canBuild)
                buildable.Add(set);
        }
        return buildable;
    }
}
