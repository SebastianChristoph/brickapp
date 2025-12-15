using Data.Entities;
using Microsoft.AspNetCore.Components.Forms;
using Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace Data.Services
{
    public class ImageService
    {
        private readonly string _wwwrootPath;
        private readonly NotificationService _notificationService;

        public ImageService(string wwwrootPath, NotificationService notificationService)
        {
            _wwwrootPath = wwwrootPath;
            _notificationService = notificationService;
        }

        // ITEM IMAGES
        public string GetPlaceHolder()
        {
            return "/placeholder-image.png";
        }

        public async Task<string?> SaveResizedItemImageAsync(IBrowserFile file, string brand, string? legoPartNum, string? uuid)
        {
            if (file == null || file.ContentType == null || !file.ContentType.StartsWith("image/"))
                return null;

            var ext = Path.GetExtension(file.Name);
            string filePath;
            string relativePath;

            if (brand.Trim().ToLower() == "lego" && !string.IsNullOrWhiteSpace(legoPartNum))
            {
                // LEGO: Speichere als part_images/<legoPartNum>.png
                var safeLegoPartNum = legoPartNum.Trim();
                var folder = Path.Combine(_wwwrootPath, "part_images");
                Directory.CreateDirectory(folder);
                filePath = Path.Combine(folder, safeLegoPartNum + ".png");
                relativePath = $"/part_images/{safeLegoPartNum}.png";
            }
            else if (!string.IsNullOrWhiteSpace(uuid))
            {
                // Andere Brands: Speichere als part_images/new/<uuid>.png
                var folder = Path.Combine(_wwwrootPath, "part_images", "new");
                Directory.CreateDirectory(folder);
                filePath = Path.Combine(folder, uuid + ".png");
                relativePath = $"/part_images/new/{uuid}.png";
            }
            else
            {
                // Fallback: generiere zufälligen Namen in part_images/new
                var folder = Path.Combine(_wwwrootPath, "part_images", "new");
                Directory.CreateDirectory(folder);
                var randomName = Guid.NewGuid().ToString();
                filePath = Path.Combine(folder, randomName + ".png");
                relativePath = $"/part_images/new/{randomName}.png";
            }

            using var stream = file.OpenReadStream(10 * 1024 * 1024); // max 10MB
            using var image = await Image.LoadAsync(stream);
            if (image.Width > 700)
            {
                var ratio = 700f / image.Width;
                var newHeight = (int)(image.Height * ratio);
                image.Mutate(x => x.Resize(700, newHeight));
            }
            await image.SaveAsPngAsync(filePath);
            _notificationService.Success($"Item image saved at {relativePath}");
            return relativePath;
        }

        public string GetMappedBrickImagePath(MappedBrick mappedBrick)
        {
            if (mappedBrick != null)
            {

                // 1. Falls LegoPartNum vorhanden, prüfe auf PNG im part_images-Ordner
                if (!string.IsNullOrWhiteSpace(mappedBrick.LegoPartNum))
                {
                    var fileName = mappedBrick.LegoPartNum + ".png";
                    var filePath = Path.Combine(_wwwrootPath, "part_images", fileName);
                    if (File.Exists(filePath))
                        return $"/part_images/{mappedBrick.LegoPartNum}.png";
                }

                // 2. Falls Bild unter part_images/new/<uuid>.png existiert
                var newFileName = mappedBrick.Uuid + ".png";
                var newFilePath = Path.Combine(_wwwrootPath, "part_images", "new", newFileName);
                if (File.Exists(newFilePath))
                    return $"/part_images/new/{mappedBrick.Uuid}.png";
            }
            // Fallback: Platzhalter
            return "/placeholder-image.png";
        }

        public string GetItemRequestImagePath(NewItemRequest newItemRequest)
        {
            if (newItemRequest != null)
            {
                // Wenn Brand "lego" ist, prüfe auf PNG im part_images-Ordner mit PartNum
                if (!string.IsNullOrWhiteSpace(newItemRequest.Brand) && newItemRequest.Brand.Trim().ToLower() == "lego")
                {
                    if (!string.IsNullOrWhiteSpace(newItemRequest.PartNum))
                    {
                        var fileName = newItemRequest.PartNum + ".png";
                        var filePath = Path.Combine(_wwwrootPath, "part_images", fileName);
                        if (File.Exists(filePath))
                            return $"/part_images/{newItemRequest.PartNum}.png";
                    }
                }
                // Sonst prüfe auf PNG im part_images/new-Ordner mit uuid
                else if (!string.IsNullOrWhiteSpace(newItemRequest.Uuid))
                {
                    var newFileName = newItemRequest.Uuid + ".png";
                    var newFilePath = Path.Combine(_wwwrootPath, "part_images", "new", newFileName);
                    if (File.Exists(newFilePath))
                        return $"/part_images/new/{newItemRequest.Uuid}.png";
                }
            }
            // Fallback: Platzhalter
            return "/placeholder-image.png";
        }

        // SET IMAGES

        public string GetSetImagePath(ItemSet itemSet)
        {
            if (itemSet == null)
                return "/placeholder-image.png";

            // 1. Falls ImageUrl gesetzt ist, verwende diese
            if (!string.IsNullOrWhiteSpace(itemSet.ImageUrl))
                return itemSet.ImageUrl;

            // 2. Prüfe auf lokales Bild
            if (!string.IsNullOrWhiteSpace(itemSet.LegoSetNum) && !string.IsNullOrWhiteSpace(itemSet.Brand))
            {
                var safeBrand = itemSet.Brand.ToLower().Replace(" ", "_");
                var safeSetNum = itemSet.LegoSetNum.ToLower().Replace(" ", "_");
                var fileName = safeSetNum + ".png";
                var filePath = Path.Combine(_wwwrootPath, "setimages", safeBrand, fileName);
                if (File.Exists(filePath))
                    return $"/setimages/{safeBrand}/{fileName}";
            }

            // 3. Fallback: Platzhalter
            return "/placeholder-image.png";
        }

        public string GetNewSetRequestImagePath(NewSetRequest newSetRequest)
        {
            if (newSetRequest == null)
                return "/placeholder-image.png";


            // 2. Prüfe auf lokales Bild
            if (!string.IsNullOrWhiteSpace(newSetRequest.SetNo) && !string.IsNullOrWhiteSpace(newSetRequest.Brand))
            {
                var safeBrand = newSetRequest.Brand.ToLower().Replace(" ", "_");
                var safeSetNum = newSetRequest.SetNo.ToLower().Replace(" ", "_");
                var fileName = safeSetNum + ".png";
                var filePath = Path.Combine(_wwwrootPath, "setimages", safeBrand, fileName);
                if (File.Exists(filePath))
                    return $"/setimages/{safeBrand}/{fileName}";
            }

            // 3. Fallback: Platzhalter
            return "/placeholder-image.png";
        }

        // OTHER IMAGES

        // MOCK IMAGES
        public async Task<string?> SaveMockImageAsync(IBrowserFile uploadedImage, string userUuid, int mockId)
        {
            if (uploadedImage == null) return null;
            var ext = Path.GetExtension(uploadedImage.Name);
            var fileName = mockId.ToString() + ext;
            var userDir = Path.Combine(_wwwrootPath, userUuid);
            if (!Directory.Exists(userDir))
                Directory.CreateDirectory(userDir);
            var savePath = Path.Combine(userDir, fileName);
            using (var stream = uploadedImage.OpenReadStream(3 * 1024 * 1024))
            using (var fs = File.Create(savePath))
            {
                await stream.CopyToAsync(fs);
            }
            // Relativer Pfad für Webzugriff
            return $"/{userUuid}/{fileName}";
        }


        public void DeleteMockImage(string? imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath))
                return;
            // imagePath ist z.B. /useruuid/123.png
            var relativePath = imagePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var fullPath = Path.Combine(_wwwrootPath, relativePath);
            if (File.Exists(fullPath))
            {
                try { File.Delete(fullPath); } catch { /* ignore */ }
            }
        }

        public string GetMockImage(string? imagePath)
        {
            if (!string.IsNullOrWhiteSpace(imagePath))
                return imagePath;
            return "/placeholder-image.png";
        }

        // Gibt immer den Platzhalter für ein Item zurück

        public async Task<string?> SaveSetImageAsync(IBrowserFile uploadedImage, string brand, string setId)
        {
            if (uploadedImage == null) return null;
            var ext = Path.GetExtension(uploadedImage.Name);
            var safeBrand = brand.ToLower().Replace(" ", "_");
            var safeSetId = setId.ToLower().Replace(" ", "_");
            var fileName = safeSetId + ext;
            var setDir = Path.Combine(_wwwrootPath, "setimages", safeBrand);
            if (!Directory.Exists(setDir))
                Directory.CreateDirectory(setDir);
            var savePath = Path.Combine(setDir, fileName);
            using (var stream = uploadedImage.OpenReadStream(3 * 1024 * 1024))
            using (var fs = File.Create(savePath))
            {
                await stream.CopyToAsync(fs);
            }
            // Relativer Pfad für Webzugriff
            return $"/setimages/{safeBrand}/{fileName}";
        }
    }
}
