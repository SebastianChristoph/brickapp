using brickapp.Data.Entities;

namespace brickapp.Components.Pages;

public partial class MyRequests
{
    private List<MappingRequest>? _mappingRequests;
    private List<NewItemRequest>? _itemRequests;
    private List<NewSetRequest>? _setRequests;

    protected override async Task OnInitializedAsync()
    {
        var userId = await UserService.GetTokenAsync();
        if (userId != null)
        {
            _mappingRequests = await RequestService.GetMappingRequestsByUserAsync(userId);
            _itemRequests = await RequestService.GetAllNewItemRequestsByUserAsync(userId);
            _setRequests = await RequestService.GetNewSetRequestsByUserAsync(userId);
        }
    }
}