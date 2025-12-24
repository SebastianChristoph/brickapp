using brickapp.Data.Entities;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace brickapp.Components.Dialogs;

public partial class UploadItemImageDialog
{
    [CascadingParameter] private IMudDialogInstance? MudDialog { get; set; }
    [Parameter] public MappedBrick? Brick { get; set; }
    private IBrowserFile? _file;
    private string? _imagePreviewUrl;
    private string _errorMessage = string.Empty;
    private bool _loading;

    private async Task OnInputFileChange(InputFileChangeEventArgs e)
    {
        _file = e.File;
        _errorMessage = string.Empty;

        if (_file != null)
        {
            // Validierung
            if (_file.Size > 10 * 1024 * 1024) // 10MB
            {
                _errorMessage = "Image file is too large. Maximum size is 10MB.";
                _file = null;
                return;
            }

            if (!_file.ContentType.StartsWith("image/"))
            {
                _errorMessage = "Please select a valid image file.";
                _file = null;
                return;
            }

            // Preview generieren
            try
            {
                await using var stream = _file.OpenReadStream(10 * 1024 * 1024);
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                var buffer = memoryStream.ToArray();
                _imagePreviewUrl = $"data:{_file.ContentType};base64,{Convert.ToBase64String(buffer)}";
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error generating image preview");
                _errorMessage = "Could not generate image preview.";
            }
        }
        else
        {
            _imagePreviewUrl = null;
        }
    }

    private async Task OnSave()
    {
        if (_loading || _file == null || Brick == null) return;

        _errorMessage = string.Empty;
        _loading = true;
        LoadingService.Show();

        try
        {
            var userId = await UserService.GetTokenAsync();
            if (userId == null)
            {
                _errorMessage = "User not authenticated.";
                return;
            }

            await RequestService.CreateItemImageRequestAsync(Brick.Id, userId, _file);

            NotificationService.Success(
                "Image uploaded successfully! An admin will review it shortly. You will be notified once it's approved."
            );

            MudDialog?.Close(DialogResult.Ok(true));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error uploading item image");
            _errorMessage = $"Failed to upload image: {ex.Message}";
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