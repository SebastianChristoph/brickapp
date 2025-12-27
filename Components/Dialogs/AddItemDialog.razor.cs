using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace brickapp.Components.Dialogs;

public partial class AddItemDialog
{
    [CascadingParameter] private IMudDialogInstance? MudDialog { get; set; }
    private string Brand { get; set; } = string.Empty;
    private string Name { get; set; } = string.Empty;
    private string PartNum { get; set; } = string.Empty;
    private IBrowserFile? _file;
    private string? _imagePreviewUrl;
    private string _errorMessage = string.Empty;
    private bool _loading;

    private async Task OnInputFileChange(InputFileChangeEventArgs e)
    {
        _file = e.File;
        if (_file != null)
        {
            var buffer = new byte[_file.Size];
            await using var stream = _file.OpenReadStream();
            var totalRead = 0;
            while (totalRead < buffer.Length)
            {
                int bytesRead = await stream.ReadAsync(buffer, totalRead, buffer.Length - totalRead);
                if (bytesRead == 0)
                    break;
                totalRead += bytesRead;
            }

            _imagePreviewUrl = $"data:{_file.ContentType};base64,{Convert.ToBase64String(buffer)}";
            Logger.LogInformation(
                "🟡 [AddItemDialog] File selected: Name={Name}, Size={Size}, ContentType={ContentType}",
                _file.Name, _file.Size, _file.ContentType);
        }
    }

    private async Task OnSave()
    {
        if (_loading) return;

        _errorMessage = string.Empty;
        Logger.LogInformation("🟡 [AddItemDialog] OnSave called: Brand={Brand}, Name={Name}, PartNum={PartNum}", Brand,
            Name,
            PartNum);

        // Validierung VOR Spinner (damit kein unnötiges Flackern)
        if (string.IsNullOrWhiteSpace(Brand) || string.IsNullOrWhiteSpace(Name))
        {
            _errorMessage = "Brand and Name are required.";
            Logger.LogWarning("🟡 [AddItemDialog] Validation failed: Brand or Name missing.");
            return;
        }

        if (Brand == "Lego" && string.IsNullOrWhiteSpace(PartNum))
        {
            _errorMessage = "For LEGO, the part number is required.";
            Logger.LogWarning("🟡 [AddItemDialog] LEGO PartNum required but missing.");
            return;
        }

        // Prüfe ob Item bereits existiert (mit effizienter DB-Abfrage)
        var itemExists = await RequestService.DoesItemExistAsync(Brand, PartNum, Name);
        if (itemExists)
        {
            var identifier = !string.IsNullOrWhiteSpace(PartNum) ? $"Part Number '{PartNum}'" : $"Name '{Name}'";
            _errorMessage =
                $"An item with {identifier} already exists for brand '{Brand}'. This item is already in our database.";
            Logger.LogWarning("🟡 [AddItemDialog] Duplicate item: Brand={Brand}, Name={Name}, PartNum={PartNum}", Brand,
                Name,
                PartNum);
            return;
        }

        var requestBlocked = await RequestService.IsNewItemRequestBlockedAsync(Name, Brand);
        if (requestBlocked)
        {
            _errorMessage = "There is already a pending request for this item. Please wait for the admin to review it.";
            Logger.LogWarning("🟡 [AddItemDialog] Request already pending: Brand={Brand}, Name={Name}", Brand, Name);
            return;
        }

        var userId = await UserService.GetTokenAsync();
        if (userId is null)
        {
            _errorMessage = "User not authenticated.";
            Logger.LogWarning("🟡 [AddItemDialog] User not authenticated.");
            return;
        }

        _loading = true;
        await InvokeAsync(StateHasChanged);
        await Task.Delay(100); // optional, damit UI sicher rendert

        LoadingService.Show();
        try
        {
            Logger.LogInformation(
                "🟡 [AddItemDialog] Creating new item request: Brand={Brand}, Name={Name}, PartNum={PartNum}, UserId={UserId}",
                Brand, Name, PartNum, userId);

            await RequestService.CreateNewItemRequestAsync(Brand, Name, userId, PartNum, _file);

            Logger.LogInformation(
                "🟡 [AddItemDialog] Item request created successfully for Brand={Brand}, Name={Name}, PartNum={PartNum}",
                Brand, Name, PartNum);

            // Spinner soll bis HIER laufen -> Success kommt noch im try
            NotificationService.Success(
                $"Item '{Name}/{PartNum}' added successfully as a request! An admin has to approve it. You will be notified in your notification area once it's approved."
            );

            MudDialog?.Close(DialogResult.Ok(true));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "🔴 [AddItemDialog] Error while creating new item request.");
            _errorMessage = "Something went wrong while saving. Please try again.";
        }
        finally
        {
            LoadingService.Hide();
            _loading = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private void Cancel()
    {
        if (_loading) return;
        MudDialog?.Cancel();
    }
}