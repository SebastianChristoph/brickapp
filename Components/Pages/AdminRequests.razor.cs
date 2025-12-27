using brickapp.Components.Dialogs;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace brickapp.Components.Pages;

public partial class AdminRequests
{
    [Inject] private Data.Services.RequestService RequestService { get; set; } = default!;
    [Inject] private brickapp.Data.Services.ImageService ImageService { get; set; } = default!;
    [Inject] private brickapp.Data.Services.NotificationService NotificationService { get; set; } = default!;

    [Inject]
    private Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider AuthenticationStateProvider
    {
        get;
        set;
    } = default!;

    [Inject] private brickapp.Data.Services.UserService UserService { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    private List<Data.Entities.MappingRequest>? openMappingRequests;
    private List<Data.Entities.NewItemRequest>? openItemRequests;
    private List<Data.Entities.NewSetRequest>? openSetRequests;
    private List<Data.Entities.ItemImageRequest>? openImageRequests;

    protected override async Task OnInitializedAsync()
    {
        await ReloadData();
    }

    private async Task ReloadData()
    {
        openMappingRequests = await RequestService.GetOpenMappingRequestsAsync();
        openItemRequests = await RequestService.GetOpenNewItemRequestsAsync();
        openSetRequests = await RequestService.GetOpenNewSetRequestsAsync();
        openImageRequests = await RequestService.GetOpenItemImageRequestsAsync();
        StateHasChanged();
    }

    private async Task<string?> OpenApproveNewItemDialogAsync(string currentName)
    {
        var parameters = new DialogParameters
        {
            ["CurrentName"] = currentName
        };

        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            MaxWidth = MaxWidth.Small,
            FullWidth = true
        };

        var dialogRef = await DialogService.ShowAsync<ApproveNewItemRequestDialog>(
            "Approve Item", parameters, options);

        if (dialogRef?.Result is null)
            return null;

        var result = await dialogRef.Result;
        if (result is null || result.Canceled)
            return null;

        var name = result.Data?.ToString();
        return string.IsNullOrWhiteSpace(name) ? null : name.Trim();
    }

    private async Task<ApproveItemDialogResult?> OpenApproveItemNameDialogAsync(string currentName,
        string? partNum = null, string? brand = null)
    {
        var parameters = new DialogParameters
        {
            ["CurrentName"] = currentName
        };

        if (!string.IsNullOrWhiteSpace(partNum))
            parameters.Add("PartNum", partNum);

        if (!string.IsNullOrWhiteSpace(brand))
            parameters.Add("Brand", brand);

        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            MaxWidth = MaxWidth.Small,
            FullWidth = true
        };

        var dialogRef = await DialogService.ShowAsync<ApproveNewItemRequestDialog>(
            "Approve Item", parameters, options);

        if (dialogRef?.Result is null)
            return null;

        var result = await dialogRef.Result;
        if (result is null || result.Canceled)
            return null;

        if (result.Data is ApproveItemDialogResult dialogResult)
            return dialogResult;

        return null;
    }

    private async Task ApproveNewItemRequestWithDialog(Data.Entities.NewItemRequest req)
    {
        var dialogResult = await OpenApproveItemNameDialogAsync(req.Name ?? "", req.PartNum, req.Brand);
        if (dialogResult is null) return; // cancel

        await ApproveNewItemRequest(req.Id, dialogResult.Name, dialogResult.NameChanged);
    }

    private async Task<RejectDialogResult?> OpenRejectReasonDialogAsync(string? defaultReason = null)
    {
        var parameters = new DialogParameters();

        if (!string.IsNullOrWhiteSpace(defaultReason))
            parameters.Add("DefaultReason", defaultReason);

        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            MaxWidth = MaxWidth.Small,
            FullWidth = true
        };

        var dialogReference = await DialogService.ShowAsync<RejectRequestDialog>(
            "Reject Request", parameters, options);

        if (dialogReference is null)
            return null;

        // 👇 WICHTIG: Result kann (selten/versionsabhängig) null sein
        if (dialogReference.Result is null)
            return null;

        var result = await dialogReference.Result;

        if (result is null || result.Canceled)
            return null;

        if (result.Data is RejectDialogResult dialogResult)
            return dialogResult;

        return null;
    }

    private async Task RejectAllItemRequests()
    {
        if (openItemRequests == null || !openItemRequests.Any())
            return;

        try
        {
            var token = await UserService.GetTokenAsync();
            if (token == null)
            {
                NotificationService.Error("User token not found.");
                return;
            }

            foreach (var request in openItemRequests)
            {
                await RequestService.RejectNewItemRequestAsync(
                    request.Id,
                    token,
                    "rejected by admin"
                );
            }

            openItemRequests = await RequestService.GetOpenNewItemRequestsAsync();
            NotificationService.Success("Alle Item Requests wurden abgelehnt.");
        }
        catch (Exception ex)
        {
            NotificationService.Error($"Fehler beim Ablehnen aller Item Requests: {ex.Message}");
        }
    }

    private async Task RejectMappingRequestWithDialog(int requestId)
    {
        var dialogResult = await OpenRejectReasonDialogAsync();
        if (dialogResult is null) return;

        if (dialogResult.Action == RejectAction.Reject)
            await RejectMappingRequest(requestId, dialogResult.Reason);
        else if (dialogResult.Action == RejectAction.RejectToPending)
            await RejectMappingRequestToPending(requestId, dialogResult.Reason);
    }

    private async Task RejectNewSetRequestWithDialog(int requestId)
    {
        var dialogResult = await OpenRejectReasonDialogAsync();
        if (dialogResult is null) return;

        if (dialogResult.Action == RejectAction.Reject)
            await RejectNewSetRequest(requestId, dialogResult.Reason);
        else if (dialogResult.Action == RejectAction.RejectToPending)
            await RejectNewSetRequestToPending(requestId, dialogResult.Reason);
    }

    private async Task RejectNewSetRequest(int requestId, string reason)
    {
        try
        {
            await RequestService.RejectNewSetRequestAsync(requestId, reason);
            openSetRequests = await RequestService.GetOpenNewSetRequestsAsync();
            NotificationService.Success("New set request erfolgreich abgelehnt.");
        }
        catch (Exception ex)
        {
            NotificationService.Error($"Fehler beim Ablehnen: {ex.Message}");
        }
    }

    private async Task RejectNewSetRequestToPending(int requestId, string reason)
    {
        try
        {
            await RequestService.RejectNewSetRequestToPendingAsync(requestId, reason);
            openSetRequests = await RequestService.GetOpenNewSetRequestsAsync();
            NotificationService.Success("New set request zurück auf Pending gesetzt.");
        }
        catch (Exception ex)
        {
            NotificationService.Error($"Fehler: {ex.Message}");
        }
    }

    private async Task RejectMappingRequest(int requestId, string reason)
    {
        try
        {
            var token = await UserService.GetTokenAsync();
            if (token == null)
            {
                NotificationService.Error("User token not found.");
                return;
            }

            await RequestService.RejectMappingRequestAsync(requestId, token, reason);
            openMappingRequests = await RequestService.GetOpenMappingRequestsAsync();
            NotificationService.Success("Mapping-Request erfolgreich abgelehnt.");
        }
        catch (Exception ex)
        {
            NotificationService.Error($"Fehler beim Ablehnen: {ex.Message}");
        }
    }

    private async Task RejectMappingRequestToPending(int requestId, string reason)
    {
        try
        {
            var token = await UserService.GetTokenAsync();
            if (token == null)
            {
                NotificationService.Error("User token not found.");
                return;
            }

            await RequestService.RejectMappingRequestToPendingAsync(requestId, token, reason);
            openMappingRequests = await RequestService.GetOpenMappingRequestsAsync();
            NotificationService.Success("Mapping-Request zurück auf Pending gesetzt.");
        }
        catch (Exception ex)
        {
            NotificationService.Error($"Fehler: {ex.Message}");
        }
    }

    private async Task ApproveMappingRequest(int requestId)
    {
        try
        {
            var token = await UserService.GetTokenAsync();
            if (token == null)
            {
                NotificationService.Error("User token not found.");
                return;
            }

            await RequestService.ApproveMappingRequestAsync(requestId, token);
            NotificationService.Success("Mapping request approved successfully.");
            openMappingRequests = await RequestService.GetOpenMappingRequestsAsync();
        }
        catch (Exception ex)
        {
            NotificationService.Error($"Error approving request: {ex.Message}");
        }
    }

    private async Task RejectMappingRequest(int requestId)
    {
        try
        {
            var token = await UserService.GetTokenAsync();
            if (token == null)
            {
                NotificationService.Error("User token not found.");
                return;
            }

            string reason = "abgelehnt durch Admin"; // Optional: Dialog für Grund
            await RequestService.RejectMappingRequestAsync(requestId, token, reason);
            openMappingRequests = await RequestService.GetOpenMappingRequestsAsync();
            NotificationService.Success("Mapping-Request erfolgreich abgelehnt.");
        }
        catch (Exception ex)
        {
            NotificationService.Error($"Fehler beim Ablehnen: {ex.Message}");
        }
    }

    private async Task ApproveNewItemRequest(int requestId, string newName, bool nameChanged)
    {
        try
        {
            var token = await UserService.GetTokenAsync();
            if (token == null)
            {
                NotificationService.Error("User token not found.");
                return;
            }

            // token ist bei dir offenbar adminUserId
            await RequestService.ApproveNewItemRequestAsync(requestId, token, newName, nameChanged);

            NotificationService.Success("New item request approved successfully.");
            openItemRequests = await RequestService.GetOpenNewItemRequestsAsync();
        }
        catch (Exception ex)
        {
            NotificationService.Error($"Error approving request: {ex.Message}");
        }
    }

    private async Task RejectNewItemRequestWithDialog(int requestId)
    {
        var dialogResult = await OpenRejectReasonDialogAsync();
        if (dialogResult is null) return;

        if (dialogResult.Action == RejectAction.Reject)
            await RejectNewItemRequest(requestId, dialogResult.Reason);
        else if (dialogResult.Action == RejectAction.RejectToPending)
            await RejectNewItemRequestToPending(requestId, dialogResult.Reason);
    }

    private async Task RejectNewItemRequest(int requestId, string reason)
    {
        try
        {
            var token = await UserService.GetTokenAsync();
            if (token == null)
            {
                NotificationService.Error("User token not found.");
                return;
            }

            await RequestService.RejectNewItemRequestAsync(requestId, token, reason);
            openItemRequests = await RequestService.GetOpenNewItemRequestsAsync();
            NotificationService.Success("New item request erfolgreich abgelehnt.");
        }
        catch (Exception ex)
        {
            NotificationService.Error($"Fehler beim Ablehnen: {ex.Message}");
        }
    }

    private async Task RejectNewItemRequestToPending(int requestId, string reason)
    {
        try
        {
            var token = await UserService.GetTokenAsync();
            if (token == null)
            {
                NotificationService.Error("User token not found.");
                return;
            }

            await RequestService.RejectNewItemRequestToPendingAsync(requestId, token, reason);
            openItemRequests = await RequestService.GetOpenNewItemRequestsAsync();
            NotificationService.Success("New item request zurück auf Pending gesetzt.");
        }
        catch (Exception ex)
        {
            NotificationService.Error($"Fehler: {ex.Message}");
        }
    }

    private async Task RejectNewItemRequest(int requestId)
    {
        try
        {
            var token = await UserService.GetTokenAsync();
            if (token == null)
            {
                NotificationService.Error("User token not found.");
                return;
            }

            string reason = "abgelehnt durch Admin"; // Optional: Dialog für Grund
            await RequestService.RejectNewItemRequestAsync(requestId, token, reason);
            openItemRequests = await RequestService.GetOpenNewItemRequestsAsync();
            NotificationService.Success("New item request erfolgreich abgelehnt.");
        }
        catch (Exception ex)
        {
            NotificationService.Error($"Fehler beim Ablehnen: {ex.Message}");
        }
    }

    private async Task ApproveNewSetRequest(int requestId)
    {
        try
        {
            var token = await UserService.GetTokenAsync();
            if (token == null)
            {
                NotificationService.Error("User token not found.");
                return;
            }

            await RequestService.ApproveNewSetRequestAsync(requestId);
            NotificationService.Success("New set request approved successfully.");
            openSetRequests = await RequestService.GetOpenNewSetRequestsAsync();
        }
        catch (Exception ex)
        {
            NotificationService.Error($"Error approving request: {ex.Message}");
        }
    }

    private async Task RejectNewSetRequest(int requestId)
    {
        try
        {
            string reason = "abgelehnt durch Admin"; // Optional: Dialog für Grund
            await RequestService.RejectNewSetRequestAsync(requestId, reason);
            openSetRequests = await RequestService.GetOpenNewSetRequestsAsync();
            NotificationService.Success("New set request erfolgreich abgelehnt.");
        }
        catch (Exception ex)
        {
            NotificationService.Error($"Fehler beim Ablehnen: {ex.Message}");
        }
    }

    private async Task ApproveImageRequest(int requestId)
    {
        try
        {
            var token = await UserService.GetTokenAsync();
            if (token == null)
            {
                NotificationService.Error("User token not found.");
                return;
            }

            await RequestService.ApproveItemImageRequestAsync(requestId, token);
            NotificationService.Success("Image request approved successfully!");
            openImageRequests = await RequestService.GetOpenItemImageRequestsAsync();
        }
        catch (Exception ex)
        {
            NotificationService.Error($"Error approving image request: {ex.Message}");
        }
    }

    private async Task RejectImageRequestWithDialog(int requestId)
    {
        var dialogResult = await OpenRejectReasonDialogAsync();
        if (dialogResult is null) return;

        if (dialogResult.Action == RejectAction.Reject)
            await RejectImageRequest(requestId, dialogResult.Reason);
        else if (dialogResult.Action == RejectAction.RejectToPending)
            await RejectImageRequestToPending(requestId, dialogResult.Reason);
    }

    private async Task RejectImageRequest(int requestId, string reason)
    {
        try
        {
            var token = await UserService.GetTokenAsync();
            if (token == null)
            {
                NotificationService.Error("User token not found.");
                return;
            }

            await RequestService.RejectItemImageRequestAsync(requestId, token, reason);
            NotificationService.Success("Image request rejected.");
            openImageRequests = await RequestService.GetOpenItemImageRequestsAsync();
        }
        catch (Exception ex)
        {
            NotificationService.Error($"Error rejecting image request: {ex.Message}");
        }
    }

    private async Task RejectImageRequestToPending(int requestId, string reason)
    {
        try
        {
            var token = await UserService.GetTokenAsync();
            if (token == null)
            {
                NotificationService.Error("User token not found.");
                return;
            }

            await RequestService.RejectItemImageRequestToPendingAsync(requestId, token, reason);
            NotificationService.Success("Image request set back to pending.");
            openImageRequests = await RequestService.GetOpenItemImageRequestsAsync();
        }
        catch (Exception ex)
        {
            NotificationService.Error($"Error setting request to pending: {ex.Message}");
        }
    }

    private async Task ShowImagePreview(string imageUrl)
    {
        var parameters = new DialogParameters
        {
            ["ImageUrl"] = imageUrl
        };
        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Large,
            CloseButton = true,
            CloseOnEscapeKey = true
        };
        await DialogService.ShowAsync<ImagePreviewDialog>("Image Preview", parameters, options);
    }
}