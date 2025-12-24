using brickapp.Components.Dialogs;
using brickapp.Data.Entities;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace brickapp.Components.Pages;

public partial class PreviewNewSetRequest
{
    private bool _isAdmin;
    private bool _loading = true;

    private NewSetRequest? _setRequest;
    [Parameter] public int RequestId { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            _loading = true;
            _isAdmin = await UserService.IsAdminAsync();

            if (!_isAdmin)
            {
                _loading = false;
                return;
            }

            _setRequest = await RequestService.GetNewSetRequestByIdAsync(RequestId);
        }
        finally
        {
            _loading = false;
        }
    }

    private void GoBack()
    {
        Nav.NavigateTo("/admin-requests");
    }

    private Color GetStatusColor(NewSetRequestStatus status)
    {
        return status switch
        {
            NewSetRequestStatus.Draft => Color.Default,
            NewSetRequestStatus.Pending => Color.Warning,
            NewSetRequestStatus.Approved => Color.Success,
            NewSetRequestStatus.Rejected => Color.Error,
            _ => Color.Default
        };
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
            Nav.NavigateTo("/admin-requests");
        }
        catch (Exception ex)
        {
            NotificationService.Error($"Error approving request: {ex.Message}");
        }
    }

    private async Task RejectNewSetRequestWithDialog(int requestId)
    {
        var reason = await OpenRejectReasonDialogAsync();
        if (reason is null) return;

        await RejectNewSetRequest(requestId, reason);
    }

    private async Task RejectNewSetRequest(int requestId, string reason)
    {
        try
        {
            await RequestService.RejectNewSetRequestAsync(requestId, reason);
            NotificationService.Success("New set request rejected successfully.");
            Nav.NavigateTo("/admin-requests");
        }
        catch (Exception ex)
        {
            NotificationService.Error($"Error rejecting request: {ex.Message}");
        }
    }

    private async Task<string?> OpenRejectReasonDialogAsync(string? defaultReason = null)
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

        var result = await dialogReference.Result;

        if (result is null || result.Canceled)
            return null;

        var reason = result.Data?.ToString();
        return string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
    }
}