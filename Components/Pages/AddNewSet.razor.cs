using brickapp.Data.DTOs;
using brickapp.Data.Entities;
using Microsoft.AspNetCore.Components.Forms;

namespace brickapp.Components.Pages;

public partial class AddNewSet(string? imageError)
{
    private string? _imagePreviewUrl;
    private string? _imageError = imageError;
    private IBrowserFile? _uploadedImageFile;
    private bool _loading;

    private async Task OnImageSelected(InputFileChangeEventArgs e)
    {
        _imageError = null;
        _imagePreviewUrl = null;
        _uploadedImageFile = null;

        var file = e.File;

        if (file.Size > 3 * 1024 * 1024)
        {
            _imageError = "The image must be max 3MB.";
            StateHasChanged();
            return;
        }

        try
        {
            using var stream = file.OpenReadStream(3 * 1024 * 1024);
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            var buffer = ms.ToArray();
            var base64 = Convert.ToBase64String(buffer);
            _imagePreviewUrl = $"data:{file.ContentType};base64,{base64}";
            _uploadedImageFile = file;
        }
        catch (Exception ex)
        {
            _imageError = $"Error loading image: {ex.Message}";
        }

        StateHasChanged();
    }

    private bool _hasLoaded;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!_hasLoaded && firstRender)
        {
            _hasLoaded = true;
            await LoadDraftsAndData();
        }
    }

    private List<NewSetRequest>? _draftSets;
    private NewSetModel _newSetModel = new();
    private int? _editingDraftId;
    private string? _errorMessage;
    private List<MappedBrick>? _allBricksCache;
    private List<BrickColor> _brickColors = new();
    private readonly Dictionary<int, MappedBrick> _brickCache = new();

    private async Task EditDraftAsync(int draftId)
    {
        var draft = _draftSets?.FirstOrDefault(d => d.Id == draftId);
        if (draft == null) return;

        LoadingService.Show("Loading draft...");
        await Task.Delay(100); // allow UI to update

        _editingDraftId = draft.Id;
        var bricks = new List<NewSetBrickModel>();
        var allBricks = await MappedBrickService.GetAllMappedBricksAsync();

        foreach (var item in draft.Items)
        {
            int brickId = 0;
            string brickName = item.ItemIdOrName;

            if (int.TryParse(item.ItemIdOrName, out var id))
            {
                brickId = id;
                var brick = allBricks.FirstOrDefault(b => b.Id == id);
                if (brick != null)
                    brickName = brick.LegoName ?? brick.LegoPartNum ?? item.ItemIdOrName;
            }

            var mappedBrick = allBricks.FirstOrDefault(b => b.Id == brickId);

            bricks.Add(new NewSetBrickModel
            {
                BrickId = brickId,
                BrickName = brickName,
                LegoPartNum = mappedBrick?.LegoPartNum,
                Uuid = mappedBrick?.Uuid,
                Color = item.Color,
                Quantity = item.Quantity
            });
        }

        _newSetModel = new NewSetModel
        {
            SetName = draft.SetName,
            SetNo = draft.SetNo,
            Brand = draft.Brand,
            Bricks = bricks
        };

        _uploadedImageFile = null;
        _imagePreviewUrl = ImageService.GetNewSetRequestImagePath(draft);
        LoadingService.Hide();
        StateHasChanged();
    }

    private async Task LoadDraftsAndData()
    {
        var user = await UserService.GetCurrentUserAsync();
        if (user == null)
        {
            _draftSets = new List<NewSetRequest>();
            return;
        }

        var allSets = await RequestService.GetNewSetRequestsByUserAsync(user.Uuid);
        _draftSets = allSets.Where(s => s.Status == NewSetRequestStatus.Draft).ToList();

        if (_allBricksCache == null)
        {
            _allBricksCache = await MappedBrickService.GetAllMappedBricksAsync();
        }

        await GetAllBrandsAsync();
        _brickColors = await MappedBrickService.GetAllColorsAsync();

        StateHasChanged();
    }

    private void RemoveBrick(NewSetBrickModel brick)
    {
        _newSetModel.Bricks.Remove(brick);
        StateHasChanged();
    }

    private void HandleBrickAdded(BrickSelectionDto? selection)
    {
        if (selection?.Brick == null) return;

        // Cache für spätere Verwendung
        _brickCache[selection.Brick.Id] = selection.Brick;

        // Hole Farbnamen
        var colorName = _brickColors.FirstOrDefault(c => c.Id == selection.BrickColorId)?.Name ?? "Unknown";

        string displayName = selection.Brick.LegoName
                             ?? selection.Brick.BluebrixxName
                             ?? selection.Brick.CadaName
                             ?? selection.Brick.Name;

        _newSetModel.Bricks.Add(new NewSetBrickModel
        {
            BrickId = selection.Brick.Id,
            BrickName = displayName,
            LegoPartNum = selection.Brick.LegoPartNum,
            Uuid = selection.Brick.Uuid,
            Color = colorName,
            Quantity = selection.Quantity
        });

        NotificationService.Success("Item added to the set.");
        StateHasChanged();
    }

    private async Task GetAllBrandsAsync()
    {
        await ItemSetService.GetPaginatedItemSetsAsync(1, 1000);
    }

    private async Task HandleDraftSubmit()
    {
        if (_loading) return;
        _errorMessage = null;

        _loading = true;
        StateHasChanged(); // UI sofort sperren

        try
        {
            // Validierung
            if (string.IsNullOrWhiteSpace(_newSetModel.Brand) || string.IsNullOrWhiteSpace(_newSetModel.SetNo) ||
                string.IsNullOrWhiteSpace(_newSetModel.SetName))
            {
                _errorMessage = "Brand, Set Number and Set Name are required.";
                return;
            }

            // Prüfe, ob das Set bereits existiert (in veröffentlichten Sets oder pending Requests)
            // ABER: Wenn wir einen Draft bearbeiten, dürfen wir den eigenen Draft ignorieren
            if (!_editingDraftId.HasValue)
            {
                var setExists =
                    await RequestService.DoesSetExistAsync(_newSetModel.Brand, _newSetModel.SetNo, _newSetModel.SetName);
                if (setExists)
                {
                    _errorMessage =
                        $"A set with Brand '{_newSetModel.Brand}' and Set Number '{_newSetModel.SetNo}' or Name '{_newSetModel.SetName}' already exists or has a pending request.";
                    return;
                }
            }

            // BILD-LOGIK: Beim Zwischenspeichern (Draft) drücken wir ein Auge zu
            string? imagePath = null;
            if (_uploadedImageFile != null)
            {
                imagePath = await ImageService.SaveSetImageAsync(_uploadedImageFile, _newSetModel.Brand,
                    _newSetModel.SetNo);
            }


            // SPEICHERN / UPDATE
            var items = _newSetModel.Bricks.Select(b => new NewSetRequestItem
            {
                ItemIdOrName = b.BrickId != 0 ? b.BrickId.ToString() : b.BrickName,
                Quantity = b.Quantity,
                Color = b.Color
            }).ToList();

            if (_editingDraftId.HasValue)
            {
                // Hier wird der bestehende Draft einfach AKTUALISIERT
                await RequestService.UpdateNewSetRequestAsync(_editingDraftId.Value, _newSetModel.Brand,
                    _newSetModel.SetNo,
                    _newSetModel.SetName, imagePath, items, NewSetRequestStatus.Draft);
                NotificationService.Success("Draft updated.");
            }
            else
            {
                // Neuen Draft anlegen
                var user = await UserService.GetCurrentUserAsync();
                if (user == null)
                {
                    _errorMessage = "User not found.";
                    return;
                }

                await RequestService.CreateNewSetRequestAsync(_newSetModel.Brand, _newSetModel.SetNo, _newSetModel.SetName,
                    imagePath,
                    user.Uuid, items, NewSetRequestStatus.Draft);
                NotificationService.Success("Draft created.");
            }

            // ALLES RESETTEN für die nächste Eingabe
            await LoadDraftsAndData();
            _newSetModel = new();
            _editingDraftId = null;
            _uploadedImageFile = null;
            _imagePreviewUrl = null;
        }
        catch (Exception ex)
        {
            _errorMessage = "Error: " + ex.Message;
        }
        finally
        {
            // DAS HIER IST DER WICHTIGSTE TEIL: 
            // Egal was passiert, wir machen die Buttons wieder frei!
            _loading = false;
            StateHasChanged();
        }
    }

    private async Task HandlePublish()
    {
        if (_loading) return;

        _errorMessage = null;
        var user = await UserService.GetCurrentUserAsync();
        if (user == null) return;

        // Validierung
        if (string.IsNullOrWhiteSpace(_newSetModel.Brand) || string.IsNullOrWhiteSpace(_newSetModel.SetNo) ||
            string.IsNullOrWhiteSpace(_newSetModel.SetName))
        {
            _errorMessage = "Brand, Set Number and Set Name are required.";
            StateHasChanged();
            return;
        }

        // Prüfe ob das Set bereits existiert (in veröffentlichten Sets oder pending Requests)
        var setExists =
            await RequestService.DoesSetExistAsync(_newSetModel.Brand, _newSetModel.SetNo, _newSetModel.SetName);
        if (setExists)
        {
            _errorMessage =
                $"A set with Brand '{_newSetModel.Brand}' and Set Number '{_newSetModel.SetNo}' or Name '{_newSetModel.SetName}' already exists or has a pending request.";
            StateHasChanged();
            return;
        }

        string? imagePath = null;

        _loading = true;
        await InvokeAsync(StateHasChanged);
        LoadingService.Show(message: "Publishing set...");

        try
        {
            if (_uploadedImageFile != null)
            {
                imagePath = await ImageService.SaveSetImageAsync(_uploadedImageFile, _newSetModel.Brand,
                    _newSetModel.SetNo);
            }
            else if (_editingDraftId.HasValue)
            {
                // kein neues Bild: ok (du hattest hier Logik auskommentiert)
            }
            else
            {
                _errorMessage = "Please upload an image for the set.";
                StateHasChanged();
                return;
            }

            var items = _newSetModel.Bricks.Select(b => new NewSetRequestItem
            {
                ItemIdOrName = b.BrickId != 0 ? b.BrickId.ToString() : b.BrickName,
                Quantity = b.Quantity,
                Color = b.Color
            }).ToList();

            if (_editingDraftId.HasValue)
            {
                var draft = _draftSets?.FirstOrDefault(d => d.Id == _editingDraftId.Value);
                if (draft != null)
                    await RequestService.DeleteNewSetRequestAsync(draft.Id);
            }

            await RequestService.CreateNewSetRequestAsync(_newSetModel.Brand, _newSetModel.SetNo, _newSetModel.SetName,
                imagePath,
                user.Uuid, items, NewSetRequestStatus.Pending);

            await LoadDraftsAndData();
            _newSetModel = new();
            _editingDraftId = null;
            _uploadedImageFile = null;
            _imagePreviewUrl = null;

            NotificationService.Success("Set successfully submitted for review.");
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error while publishing: {ex.Message}";
            NotificationService.Error("Publishing failed.");
        }
        finally
        {
            LoadingService.Hide();
            _loading = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task DeleteDraftAsync(int id)
    {
        await RequestService.DeleteNewSetRequestAsync(id);

        var user = await UserService.GetCurrentUserAsync();
        if (user != null)
        {
            await LoadDraftsAndData();
            StateHasChanged();
        }
    }

    public class NewSetModel
    {
        public string SetName { get; set; } = string.Empty;
        public string SetNo { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public List<NewSetBrickModel> Bricks { get; set; } = new();
    }

    public class NewSetBrickModel
    {
        public int BrickId { get; set; }
        public string BrickName { get; set; } = string.Empty;
        public string? LegoPartNum { get; set; }
        public string? Uuid { get; set; }
        public string Color { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }
}