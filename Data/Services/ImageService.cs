using brickapp.Data.Entities;
using Microsoft.AspNetCore.Components.Forms;
using brickapp.Data.Services;
using brickapp.Data.Services.Storage;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace brickapp.Data.Services
{
    public class ImageService
    {
        private readonly IImageStorage _storage;
        private readonly NotificationService _notificationService;
              private readonly ILogger<RequestService> _logger;

        public ImageService(IImageStorage storage, NotificationService notificationService,   ILogger<RequestService> logger)
        {
            _storage = storage;
            _notificationService = notificationService;
            _logger = logger;
        }

        // ITEM IMAGES
        public string GetPlaceHolder()
        {
            return "/placeholder-image.png";
        }

        public async Task<string?> SaveResizedItemImageAsync(IBrowserFile file, string brand, string? legoPartNum, string? uuid)
        {
             _logger.LogInformation($"🟡 [ImageService] Saving Image for brand {brand}");

            if (file == null || file.ContentType == null || !file.ContentType.StartsWith("image/"))
                return null;

            string relativePath;

            if (brand.Trim().ToLower() == "lego" && !string.IsNullOrWhiteSpace(legoPartNum))
            {
                // LEGO: part_images/<legoPartNum>.png
                var safeLegoPartNum = legoPartNum.Trim();
                relativePath = $"part_images/{safeLegoPartNum}.png";
            }
            else if (!string.IsNullOrWhiteSpace(uuid))
            {
                // Andere Brands: part_images/new/<uuid>.png
                relativePath = $"part_images/new/{uuid}.png";
            }
            else
            {
                // Fallback
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
            await _storage.SaveAsync(outStream, "image/png", relativePath);

            var webPath = BuildWebPath(relativePath);
            _notificationService.Success($"Item image saved at {webPath}");
            _logger.LogInformation($"🟢 [ImageService] Image saved at {webPath}");
            return webPath;
        }

        public string GetMappedBrickImagePath(MappedBrick mappedBrick)
        {
            if (mappedBrick != null)
            {
                // 1) part_images/<legoPartNum>.png
                if (!string.IsNullOrWhiteSpace(mappedBrick.LegoPartNum))
                {
                    var rel = $"part_images/{mappedBrick.LegoPartNum}.png";
                    if (_storage.Exists(rel))
                        return BuildWebPath(rel);
                }

                // 2) part_images/new/<uuid>.png
                if (!string.IsNullOrWhiteSpace(mappedBrick.Uuid))
                {
                    var rel = $"part_images/new/{mappedBrick.Uuid}.png";
                    if (_storage.Exists(rel))
                        return BuildWebPath(rel);
                }
            }

            return "/placeholder-image.png";
        }

        public string GetItemRequestImagePath(NewItemRequest newItemRequest)
        {
            if (newItemRequest != null)
            {
                // LEGO -> part_images/<PartNum>.png
                if (!string.IsNullOrWhiteSpace(newItemRequest.Brand) &&
                    newItemRequest.Brand.Trim().ToLower() == "lego" &&
                    !string.IsNullOrWhiteSpace(newItemRequest.PartNum))
                {
                    var rel = $"part_images/{newItemRequest.PartNum}.png";
                    if (_storage.Exists(rel))
                        return BuildWebPath(rel);
                }

                // sonst -> part_images/new/<uuid>.png
                if (!string.IsNullOrWhiteSpace(newItemRequest.Uuid))
                {
                    var rel = $"part_images/new/{newItemRequest.Uuid}.png";
                    if (_storage.Exists(rel))
                        return BuildWebPath(rel);
                }
            }

            return "/placeholder-image.png";
        }

        // SET IMAGES
        public async Task<string?> SaveSetImageAsync(IBrowserFile uploadedImage, string brand, string setId)
        {
            if (uploadedImage == null) return null;

            var ext = Path.GetExtension(uploadedImage.Name);
            if (string.IsNullOrWhiteSpace(ext)) ext = ".png";

            var safeBrand = brand.ToLower().Replace(" ", "_");
            var safeSetId = setId.ToLower().Replace(" ", "_");
            var fileName = safeSetId + ext;

            var relativePath = $"setimages/{safeBrand}/{fileName}";

            using var stream = uploadedImage.OpenReadStream(3 * 1024 * 1024);

            await _storage.SaveAsync(
                stream,
                uploadedImage.ContentType ?? "application/octet-stream",
                relativePath
            );

            return BuildWebPath(relativePath);
        }

        public string GetSetImagePath(ItemSet itemSet)
        {
            if (itemSet == null)
                return "/placeholder-image.png";

            // 1) Falls ImageUrl gesetzt ist, verwende diese
            if (!string.IsNullOrWhiteSpace(itemSet.ImageUrl))
                return itemSet.ImageUrl;

            // 2) lokales/blob Bild: setimages/<brand>/<setnum>.png
            if (!string.IsNullOrWhiteSpace(itemSet.SetNum) && !string.IsNullOrWhiteSpace(itemSet.Brand))
            {
                var safeBrand = itemSet.Brand.ToLower().Replace(" ", "_");
                var safeSetNum = itemSet.SetNum.ToLower().Replace(" ", "_");
                var rel = $"setimages/{safeBrand}/{safeSetNum}.png";

                if (_storage.Exists(rel))
                    return BuildWebPath(rel);
            }

            return "/placeholder-image.png";
        }

        public string GetNewSetRequestImagePath(NewSetRequest newSetRequest)
        {
            if (newSetRequest == null)
                return "/placeholder-image.png";

            if (!string.IsNullOrWhiteSpace(newSetRequest.SetNo) && !string.IsNullOrWhiteSpace(newSetRequest.Brand))
            {
                var safeBrand = newSetRequest.Brand.ToLower().Replace(" ", "_");
                var safeSetNum = newSetRequest.SetNo.ToLower().Replace(" ", "_");
                var rel = $"setimages/{safeBrand}/{safeSetNum}.png";

                if (_storage.Exists(rel))
                    return BuildWebPath(rel);
            }

            return "/placeholder-image.png";
        }

        // MOCK IMAGES
        public async Task<string?> SaveMockImageAsync(IBrowserFile uploadedImage, string userUuid, int mockId)
        {
            if (uploadedImage == null) return null;

            var ext = Path.GetExtension(uploadedImage.Name);
            if (string.IsNullOrWhiteSpace(ext)) ext = ".png";

            var fileName = mockId + ext;
            var relativePath = $"{userUuid}/{fileName}";

            using var stream = uploadedImage.OpenReadStream(3 * 1024 * 1024);

            await _storage.SaveAsync(
                stream,
                uploadedImage.ContentType ?? "application/octet-stream",
                relativePath
            );

            return BuildWebPath(relativePath);
        }

       public async Task DeleteMockImageAsync(Mock mock)
{
    if (mock == null || string.IsNullOrWhiteSpace(mock.UserUuid)) return;

    var usertoken = mock.UserUuid;
    var mockid = mock.Id;

    // Wir prüfen alle möglichen Endungen, die wir beim GetMockImagePath erlauben
    string[] extensions = new[] { ".png", ".jpg", ".jpeg" };
    
    foreach (var ext in extensions)
    {
        var rel = $"{usertoken}/{mockid}{ext}";
        if (_storage.Exists(rel))
        {
            try 
            {
                await _storage.DeleteAsync(rel);
                _logger.LogInformation($"[ImageService] Deleted image for Mock {mockid} at {rel}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[ImageService] Error deleting image for Mock {mockid} at {rel}");
            }
        }
    }
}

        public string GetMockImagePath(Mock mock)
        {
            if (mock == null)
                return "/placeholder-image.png";

            var usertoken = mock.UserUuid;
            var mockid = mock.Id;

            string[] extensions = new[] { ".png", ".jpg", ".jpeg" };
            foreach (var ext in extensions)
            {
                var rel = $"{usertoken}/{mockid}{ext}";
                if (_storage.Exists(rel))
                    return BuildWebPath(rel);
            }

            return "/placeholder-image.png";
        }

        private string BuildWebPath(string relativePath)
        {
            var baseUrl = _storage.BaseUrl ?? "/";

            // baseUrl sauber machen
            if (!baseUrl.EndsWith("/"))
                baseUrl += "/";

            // relativePath sauber machen
            relativePath = relativePath.Replace("\\", "/").TrimStart('/');

            return $"{baseUrl}{relativePath}";
        }
    }
}
