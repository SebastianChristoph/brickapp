using brickapp.Components.Shared.PartsListUpload;
using brickapp.Data;
using brickapp.Data.Entities;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;

namespace brickapp.Components.Pages;

public partial class MyMocks
{
    private readonly PartsUploadFormat[] _mocFormats = new[]
    {
        PartsUploadFormat.RebrickableCsv,
        PartsUploadFormat.RebrickableXml,
        PartsUploadFormat.BricklinkXml
    };

    private readonly string _uploadError = string.Empty;
    private string _currentSource = string.Empty;
    private string? _error;
    private string? _errorDetails;
    private bool _fileReadyForUpload;
    private bool _loading;
    private string _newMockComment = string.Empty;
    private string _newMockName = string.Empty;
    private string _newMockWebSource = string.Empty;
    private List<UnmappedRow> _unmappedRows = new();
    private IBrowserFile? _uploadedImage;
    private List<MockItem> _uploadedItems = new();

    private List<Mock>? _userMocks;
    public string ImagePreviewUrl = string.Empty;

    private string MocFormatLabel(PartsUploadFormat f)
    {
        return f switch
        {
            PartsUploadFormat.RebrickableCsv => "Rebrickable CSV (Part,Color,Quantity)",
            PartsUploadFormat.RebrickableXml => "Rebrickable XML",
            PartsUploadFormat.BricklinkXml => "BrickLink XML",
            _ => f.ToString()
        };
    }

    private void SetError(Exception ex, string context = "")
    {
        _error = string.IsNullOrWhiteSpace(context) ? $"Error: {ex.Message}" : $"Error ({context}): {ex.Message}";
        _errorDetails = ex.ToString();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await LoadMocks();
            StateHasChanged();
        }
    }

    private async Task LoadMocks()
    {
        using var scope = ServiceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userUuid = await UserService.GetTokenAsync();
        if (string.IsNullOrEmpty(userUuid)) return;
        _userMocks = await db.Mocks.Where(m => m.UserUuid == userUuid).Include(m => m.Items).ToListAsync();

        // Track page visit
        await TrackingService.TrackAsync("ViewMyMocks", $"Mocks: {_userMocks?.Count ?? 0}", "/mymocs");
    }

    private Task OnMocParsed(ParseResult<ParsedPart> result)
    {
        // 1. Mapped Items wie bisher
        _uploadedItems = result.MappedItems
            .Select(p => new MockItem
            {
                MappedBrickId = p.MappedBrickId,
                BrickColorId = p.BrickColorId,
                Quantity = p.Quantity,
                ExternalPartNum = p.ExternalPartNum
            })
            .ToList();

        // 2. Unmapped Rows speichern
        _unmappedRows = result.Unmapped;

        // 3. Source merken
        _currentSource = result.AppliedFormat.ToString();

        // --- FIX: Button aktivieren ---
        // Wir aktivieren den Button, wenn entweder gemappte ODER ungemappte Teile gefunden wurden
        _fileReadyForUpload = _uploadedItems.Any() || _unmappedRows.Any();

        return Task.CompletedTask;
    }

    private async Task CreateMockFromUpload()
    {
        if (_loading || _uploadedItems.Count == 0) return;
        _loading = true;
        LoadingService.Show("Creating MOC...");
        try
        {
            using var scope = ServiceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var userUuid = await UserService.GetTokenAsync();

            if (string.IsNullOrEmpty(userUuid))
            {
                NotificationService.Success("User not authenticated.");
                return;
            }

            var mock = new Mock
            {
                Name = _newMockName,
                Comment = _newMockComment,
                WebSource = _newMockWebSource,
                UserUuid = userUuid,
                MockType = _currentSource.ToLower()
                    .Replace("csv", "")
                    .Replace("xml", "")
                    .Trim()
                // WICHTIG: Source speichern (musst du evtl. in Mock.cs Entity noch hinzufügen!)
                // MockSource = currentSource 
            };
            db.Mocks.Add(mock);
            await db.SaveChangesAsync();

            // 1. Mapped Items hinzufügen
            foreach (var item in _uploadedItems)
            {
                item.MockId = mock.Id; // Wird von EF gehandelt
                mock.Items.Add(item);
            }

            // 2. NEU: Missing Items transformieren und hinzufügen
            if (_unmappedRows.Any())
            {
                // A) MissingItems für das Mock erstellen (für die UI Anzeige)
                mock.MissingItems = _unmappedRows
                    .GroupBy(u => new { u.PartNum, u.ColorId })
                    .Select(g => new MissingItem
                    {
                        ExternalPartNum = g.Key.PartNum,
                        ExternalColorId = g.Key.ColorId,
                        Quantity = g.Sum(x => x.Quantity)
                    }).ToList();

                // B) NewItemRequests & Bilder für Lego Teile erstellen
                var distinctPartNums = _unmappedRows
                    .Where(r => !string.IsNullOrEmpty(r.PartNum))
                    .Select(r => r.PartNum!)
                    .Distinct()
                    .ToList();

                foreach (var partNum in distinctPartNums)
                {
                    // Checken, ob wir für dieses Teil schon einen gültigen Request haben
                    var requestExists = await db.NewItemRequests
                        .AnyAsync(r => r.PartNum == partNum
                                       && r.Brand == "Lego"
                                       && r.Status != NewItemRequestStatus.Rejected
                                       && r.Status != NewItemRequestStatus.Pending);

                    if (!requestExists)
                    {
                        // API fragen nach Name und Bild-URL (nutzt das neue DTO)
                        var partInfo = await RebrickableApi.GetLegoItemNameByPartNumber(partNum);

                        if (partInfo != null && !string.IsNullOrWhiteSpace(partInfo.Name))
                        {
                            var newRequestUuid = Guid.NewGuid().ToString();
                            var newRequest = new NewItemRequest
                            {
                                Uuid = newRequestUuid,
                                Brand = "Lego",
                                PartNum = partNum,
                                Name = partInfo.Name,
                                RequestedByUserId = userUuid,
                                CreatedAt = DateTime.UtcNow,
                                Status = NewItemRequestStatus.Pending
                            };

                            db.NewItemRequests.Add(newRequest);

                            // Bild asynchron herunterladen und speichern
                            if (!string.IsNullOrWhiteSpace(partInfo.ImageUrl))
                                // Wir warten hier kurz, damit das Bild beim nächsten Laden 
                                // der Seite evtl. schon da ist.
                                await ImageService.DownloadAndSaveItemImageAsync(
                                    partInfo.ImageUrl,
                                    "Lego",
                                    partNum,
                                    newRequestUuid);

                            Logger.LogInformation("✅ NewItemRequest via MOC-Upload für {PartNum} erstellt.", partNum);
                        }
                    }
                }
            }

            if (_uploadedImage != null) await ImageService.SaveMockImageAsync(_uploadedImage, userUuid, mock.Id);

            await db.SaveChangesAsync();
            NotificationService.Success("MOC created!");
            // --- NAVIGATION HINZUFÜGEN ---
            // Wir leiten direkt auf die Detailseite weiter
            Nav.NavigateTo($"/mockdetail/{mock.Id}");
        }
        catch (Exception ex)
        {
            SetError(ex, "Create");
        }
        finally
        {
            LoadingService.Hide();
            _loading = false;
        }
    }

    private async Task OnImageSelected(InputFileChangeEventArgs e)
    {
        _uploadedImage = e.File;
        var stream = _uploadedImage.OpenReadStream(3 * 1024 * 1024);
        var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        ImagePreviewUrl = $"data:{e.File.ContentType};base64,{Convert.ToBase64String(ms.ToArray())}";
    }

    private async Task DeleteMock(int mockId)
    {
        if (_loading) return;

        try
        {
            var userUuid = await UserService.GetTokenAsync();
            if (string.IsNullOrEmpty(userUuid))
                return;

            using var scope = ServiceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var mock = db.Mocks.FirstOrDefault(m => m.Id == mockId && m.UserUuid == userUuid);
            if (mock != null)
            {
                await ImageService.DeleteMockImageAsync(mock);
                db.Mocks.Remove(mock);
                db.SaveChanges();
            }

            await LoadMocks();
        }
        catch (Exception ex)
        {
            SetError(ex, "DeleteMock");
            NotificationService.Error(_error ?? "DeleteMock failed.");
        }
    }

    private async Task DeleteAllMocks()
    {
        if (_loading) return;

        try
        {
            var userUuid = await UserService.GetTokenAsync();
            if (string.IsNullOrEmpty(userUuid))
                return;

            using var scope = ServiceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var mocks = db.Mocks.Where(m => m.UserUuid == userUuid).ToList();
            if (mocks.Any())
            {
                foreach (var mock in mocks)
                    await ImageService.DeleteMockImageAsync(mock);

                db.Mocks.RemoveRange(mocks);
                db.SaveChanges();
            }

            await LoadMocks();
        }
        catch (Exception ex)
        {
            SetError(ex, "DeleteAllMocks");
            NotificationService.Error(_error ?? "DeleteAllMocks failed.");
        }
    }
}