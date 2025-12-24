using Microsoft.AspNetCore.Components;

namespace brickapp.Components.Shared;

public partial class OwnershipProgress
{
    [Parameter] public int OwnedCount { get; set; }
    [Parameter] public int TotalCount { get; set; }

    private int Percent => (TotalCount > 0 && OwnedCount >= TotalCount)
        ? 100
        : (TotalCount > 0 ? (int)Math.Floor(100.0 * OwnedCount / TotalCount) : 0);

    private string GetColorHex()
    {
        if (Percent >= 100) return "#00ff18"; // Green [cite: 114]
        if (Percent >= 25) return "#ffeb3b"; // Yellow
        return "#a6a6a6"; // Black
    }
}