using brickapp.Data.Entities;

namespace brickapp.Components.Pages;

public partial class MyNotifications
{
    private List<UserNotification>? _notifications;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await LoadNotificationsAsync();
            StateHasChanged();
        }
    }

    private async Task LoadNotificationsAsync()
    {
        var userUuid = await UserService.GetTokenAsync();
        if (!string.IsNullOrEmpty(userUuid))
        {
            _notifications = await NotificationService.GetNotificationsForUserAsync(userUuid);
        }
        else
        {
            _notifications = new();
        }
    }

    private async Task DeleteNotification(UserNotification notification)
    {
        await NotificationService.DeleteNotificationAsync(notification.Id);
        await LoadNotificationsAsync();
        StateHasChanged();
    }

    private async Task DeleteAllNotifications()
    {
        var userUuid = await UserService.GetTokenAsync();
        if (!string.IsNullOrEmpty(userUuid))
        {
            await NotificationService.DeleteAllNotificationsAsync(userUuid);
            await LoadNotificationsAsync();
            StateHasChanged();
        }
    }

    private async Task MarkAsRead(UserNotification notification)
    {
        await NotificationService.MarkAsReadAsync(notification.Id);
        await LoadNotificationsAsync();
        StateHasChanged();
        // dieses forceLoad brauchst du meist nicht:
        // NavigationManager.NavigateTo(NavigationManager.Uri, forceLoad: true);
    }

    private async Task MarkAllAsRead()
    {
        var userUuid = await UserService.GetTokenAsync();
        if (!string.IsNullOrEmpty(userUuid))
        {
            await NotificationService.MarkAllAsReadAsync(userUuid);
            await LoadNotificationsAsync();
            StateHasChanged();
            // NavigationManager.NavigateTo(NavigationManager.Uri, forceLoad: true);
        }
    }
}