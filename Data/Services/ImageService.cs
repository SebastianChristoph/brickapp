using brickapp.Data.Entities;
using Microsoft.AspNetCore.Components.Forms;
using brickapp.Data.Services.Storage;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace brickapp.Data.Services
{
    public class ImageService(
        IImageStorage storage,
        NotificationService notificationService,
        ILogger<RequestService> logger)
    {
        // ITEM IMAGES
        public string GetPlaceHolder()
        {
            return "/placeholder-image.png";
        }

        public async Task<string?> SaveResizedItemImageAsync(IBrowserFile? file, string brand, string? legoPartNum, string? uuid)
        {
            logger.LogInformation($"🟡 [ImageService] Saving Image for brand {brand}");

            if (file == null || !file.ContentType.StartsWith("image/"))
                return null;

            string relativePath;

            if (brand.Trim().ToLower() == "lego" && !string.IsNullOrWhiteSpace(legoPartNum))
            {
                // LEGO: part_images/<legoPartNum>.png (nur für Seeding)
                var safeLegoPartNum = legoPartNum.Trim();
                relativePath = $"part_images/{safeLegoPartNum}.png";
            }
            else if (brand.Trim().ToLower() == "pending" && !string.IsNullOrWhiteSpace(uuid))
            {
                // Pending Images: part_images/pending/<uuid>.png
                relativePath = $"part_images/pending/{uuid}.png";
            }
            else if (brand.Trim().ToLower() == "new" && !string.IsNullOrWhiteSpace(uuid))
            {
                // Approved user images: part_images/new/<uuid>.png
                relativePath = $"part_images/new/{uuid}.png";
            }
            else if (!string.IsNullOrWhiteSpace(uuid))
            {
                // Fallback: part_images/new/<uuid>.png
                relativePath = $"part_images/new/{uuid}.png";
            }
            else
            {
                // Fallback ohne UUID
                var randomName = Guid.NewGuid().ToString();
                relativePath = $"part_images/new/{randomName}.png";
            }

            using var stream = file.OpenReadStream(10 * 1024 * 1024); // max 10MB
            using var image = await Image.LoadAsync(stream);

            if (image.Width > 700)
            {
                var ratio = 700f / image.Width;
                var newHeight = (int)(image.Height * ratio);
                image.Mutate(x => x.Resize(700, newHeight));
            }

            using var outStream = new MemoryStream();
            await image.SaveAsPngAsync(outStream);
            outStream.Position = 0;

            // Speichern (lokal: Datei, Azure: Blob)
            await storage.SaveAsync(outStream, "image/png", relativePath);

            var webPath = BuildWebPath(relativePath);
            notificationService.Success($"Item image saved at {webPath}");
            logger.LogInformation($"🟢 [ImageService] Image saved at {webPath}");
            return webPath;
        }

        public async Task<string?> MoveImageAsync(string currentWebPath, string targetFolder, string? legoPartNum, string? uuid)
        {
            try
            {
                // Extrahiere den relativen Pfad aus dem WebPath
                var baseUrl = storage.BaseUrl;
                var relativePath = currentWebPath.Replace(baseUrl, "").TrimStart('/');

                if (!storage.Exists(relativePath))
                {
                    logger.LogWarning($"[ImageService] Source image not found: {relativePath}");
                    return null;
                }

                // Bestimme den Zielpfad basierend auf targetFolder
                string targetRelativePath;
                if (targetFolder.ToLower() == "new" && !string.IsNullOrWhiteSpace(uuid))
                {
                    // pending → new: part_images/new/{uuid}.png
                    targetRelativePath = $"part_images/new/{uuid}.png";
                }
                else if (targetFolder.ToLower() == "lego" && !string.IsNullOrWhiteSpace(legoPartNum))
                {
                    // Seeding Lego: part_images/{legoPartNum}.png
                    targetRelativePath = $"part_images/{legoPartNum.Trim()}.png";
                }
                else
                {
                    logger.LogWarning($"[ImageService] Cannot determine target path for folder={targetFolder}, legoPartNum={legoPartNum}, uuid={uuid}");
                    return null;
                }

                // Kopiere die Datei an den neuen Ort
                await storage.CopyAsync(relativePath, targetRelativePath);

                // Lösche die alte Datei
                await storage.DeleteAsync(relativePath);

                logger.LogInformation($"[ImageService] Image moved from {relativePath} to {targetRelativePath}");
                return BuildWebPath(targetRelativePath);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"[ImageService] Error moving image from {currentWebPath}");
                return null;
            }
        }
        
        public async Task<string?> DownloadAndSaveItemImageAsync(string imageUrl, string brand, string? legoPartNum, string? uuid)
{
    try
    {
        using var client = new HttpClient();
        var response = await client.GetAsync(imageUrl);
        if (!response.IsSuccessStatusCode) return null;

        var imageData = await response.Content.ReadAsByteArrayAsync();
        
        // ImageSharp Logik (ähnlich wie in deiner SaveResizedItemImageAsync)
        using var image = Image.Load(imageData);

        if (image.Width > 700)
        {
            var ratio = 700f / image.Width;
            var newHeight = (int)(image.Height * ratio);
            image.Mutate(x => x.Resize(700, newHeight));
        }

        string relativePath;
        if (brand.Trim().ToLower() == "lego" && !string.IsNullOrWhiteSpace(legoPartNum))
        {
            relativePath = $"part_images/{legoPartNum.Trim()}.png";
        }
        else
        {
            relativePath = $"part_images/new/{uuid ?? Guid.NewGuid().ToString()}.png";
        }

        using var outStream = new MemoryStream();
        await image.SaveAsPngAsync(outStream);
        outStream.Position = 0;

        await storage.SaveAsync(outStream, "image/png", relativePath);
        return BuildWebPath(relativePath);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Fehler beim Download/Resize des Bildes von {Url}", imageUrl);
        return null;
    }
}

        public string GetMappedBrickImagePath(MappedBrick? mappedBrick)
        {
            if (mappedBrick != null)
            {
                // 1) part_images/<legoPartNum>.png
                if (!string.IsNullOrWhiteSpace(mappedBrick.LegoPartNum))
                {
                    var rel = $"part_images/{mappedBrick.LegoPartNum}.png";
                    if (storage.Exists(rel))
                        return BuildWebPath(rel);
                }

                // 2) part_images/new/<uuid>.png
                if (!string.IsNullOrWhiteSpace(mappedBrick.Uuid))
                {
                    var rel = $"part_images/new/{mappedBrick.Uuid}.png";
                    if (storage.Exists(rel))
                        return BuildWebPath(rel);
                }
            }

            return "/placeholder-image.png";
        }

        public string GetItemRequestImagePath(NewItemRequest? newItemRequest)
        {
            if (newItemRequest != null)
            {
                // LEGO -> part_images/<PartNum>.png
                if (!string.IsNullOrWhiteSpace(newItemRequest.Brand) &&
                    newItemRequest.Brand.Trim().ToLower() == "lego" &&
                    !string.IsNullOrWhiteSpace(newItemRequest.PartNum))
                {
                    var rel = $"part_images/{newItemRequest.PartNum}.png";
                    if (storage.Exists(rel))
                        return BuildWebPath(rel);
                }

                // sonst -> part_images/new/<uuid>.png
                if (!string.IsNullOrWhiteSpace(newItemRequest.Uuid))
                {
                    var rel = $"part_images/new/{newItemRequest.Uuid}.png";
                    if (storage.Exists(rel))
                        return BuildWebPath(rel);
                }
            }

            return "/placeholder-image.png";
        }

        // SET IMAGES
        public async Task<string?> SaveSetImageAsync(IBrowserFile? uploadedImage, string brand, string setId)
        {
            if (uploadedImage == null) return null;

            var ext = Path.GetExtension(uploadedImage.Name);
            if (string.IsNullOrWhiteSpace(ext)) ext = ".png";

            var safeBrand = brand.ToLower().Replace(" ", "_");
            var safeSetId = setId.ToLower().Replace(" ", "_");
            var fileName = safeSetId + ext;

            var relativePath = $"setimages/{safeBrand}/{fileName}";

            await using var stream = uploadedImage.OpenReadStream(3 * 1024 * 1024);

            await storage.SaveAsync(
                stream,
                uploadedImage.ContentType,
                relativePath
            );

            return BuildWebPath(relativePath);
        }

        public string GetSetImagePath(ItemSet? itemSet)
        {
            logger.LogInformation($"[ImageService] Get Set Image Path for ItemSet ID {itemSet?.Id}");
             
            if (itemSet == null)
                return "/placeholder-image.png";

            // 1) Falls ImageUrl gesetzt ist, verwende diese
            if (!string.IsNullOrWhiteSpace(itemSet.ImageUrl))
            {
                    logger.LogInformation($"[ImageService] Using ImageUrl: {itemSet.ImageUrl}");
                    return itemSet.ImageUrl;
            }
                

            // 2) lokales/blob Bild: setimages/<brand>/<setnum>.png
            if (!string.IsNullOrWhiteSpace(itemSet.SetNum) && !string.IsNullOrWhiteSpace(itemSet.Brand))
            {
                logger.LogInformation($"[ImageService] Checking local/blob storage for set image of brand {itemSet.Brand} and setnum {itemSet.SetNum}");
                var safeBrand = itemSet.Brand.ToLower().Replace(" ", "_");
                var safeSetNum = itemSet.SetNum.ToLower().Replace(" ", "_");
                var rel = $"setimages/{safeBrand}/{safeSetNum}.png";

                if (storage.Exists(rel))
                    return BuildWebPath(rel);
            }

            return "/placeholder-image.png";
        }

        public string GetNewSetRequestImagePath(NewSetRequest? newSetRequest)
        {
            if (newSetRequest == null)
                return "/placeholder-image.png";

            if (!string.IsNullOrWhiteSpace(newSetRequest.SetNo) && !string.IsNullOrWhiteSpace(newSetRequest.Brand))
            {
                var safeBrand = newSetRequest.Brand.ToLower().Replace(" ", "_");
                var safeSetNum = newSetRequest.SetNo.ToLower().Replace(" ", "_");
                var rel = $"setimages/{safeBrand}/{safeSetNum}.png";

                if (storage.Exists(rel))
                    return BuildWebPath(rel);
            }

            return "/placeholder-image.png";
        }

        // MOCK IMAGES
        public async Task<string?> SaveMockImageAsync(IBrowserFile? uploadedImage, string userUuid, int mockId)
        {
            if (uploadedImage == null) return null;

            var ext = Path.GetExtension(uploadedImage.Name);
            if (string.IsNullOrWhiteSpace(ext)) ext = ".png";

            var fileName = mockId + ext;
            var relativePath = $"{userUuid}/{fileName}";

            await using var stream = uploadedImage.OpenReadStream(3 * 1024 * 1024);

            await storage.SaveAsync(
                stream,
                uploadedImage.ContentType,
                relativePath
            );

            return BuildWebPath(relativePath);
        }
        public async Task<bool> DeleteImageAsync(string webPath)
        {
            try
            {
                var baseUrl = storage.BaseUrl;
                var relativePath = webPath.Replace(baseUrl, "").TrimStart('/');
                
                var deleted = await storage.DeleteAsync(relativePath);
                if (deleted)
                {
                    logger.LogInformation($"[ImageService] Deleted image at {relativePath}");
                }
                else
                {
                    logger.LogWarning($"[ImageService] Image not found or already deleted: {relativePath}");
                }
                return deleted;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"[ImageService] Error deleting image at {webPath}");
                return false;
            }
        }
       public async Task DeleteMockImageAsync(Mock? mock)
{
    if (mock == null || string.IsNullOrWhiteSpace(mock.UserUuid)) return;

    var mockUserUuid = mock.UserUuid;
    var mockid = mock.Id;

    // Wir prüfen alle möglichen Endungen, die wir beim GetMockImagePath erlauben
    string[] extensions = new[] { ".png", ".jpg", ".jpeg" };
    
    foreach (var ext in extensions)
    {
        var rel = $"{mockUserUuid}/{mockid}{ext}";
        if (storage.Exists(rel))
        {
            try 
            {
                await storage.DeleteAsync(rel);
                logger.LogInformation($"[ImageService] Deleted image for Mock {mockid} at {rel}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"[ImageService] Error deleting image for Mock {mockid} at {rel}");
            }
        }
    }
}

        public string GetMockImagePath(Mock? mock)
        {
            if (mock == null)
                return "/placeholder-image.png";

            var usertoken = mock.UserUuid;
            var mockid = mock.Id;

            string[] extensions = new[] { ".png", ".jpg", ".jpeg" };
            foreach (var ext in extensions)
            {
                var rel = $"{usertoken}/{mockid}{ext}";
                if (storage.Exists(rel))
                    return BuildWebPath(rel);
            }

            return "/placeholder-image.png";
        }

        private string BuildWebPath(string relativePath)
        {
            var baseUrl = storage.BaseUrl;

            // baseUrl sauber machen
            if (!baseUrl.EndsWith("/"))
                baseUrl += "/";

            // relativePath sauber machen
            relativePath = relativePath.Replace("\\", "/").TrimStart('/');

            return $"{baseUrl}{relativePath}";
        }
    }
}
