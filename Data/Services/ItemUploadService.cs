using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Forms;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace Data.Services
{
    public class ItemUploadService
    {
        private readonly string _wwwrootPath;
        public ItemUploadService(string wwwrootPath)
        {
            _wwwrootPath = wwwrootPath;
        }

        public async Task<string?> SaveResizedImageAsync(IBrowserFile file, string brand, string? itemName)
        {
            if (file == null || file.ContentType == null || !file.ContentType.StartsWith("image/"))
                return null;

            var safeBrand = brand.ToLower().Replace(" ", "_");
            string fileName;
            if (!string.IsNullOrWhiteSpace(itemName))
            {
                var safeName = itemName.ToLower().Replace(" ", "_");
                var ext = Path.GetExtension(file.Name);
                fileName = $"{safeName}{ext}";
            }
            else
            {
                // Generate unique filename if no itemName/ID is provided
                var ext = Path.GetExtension(file.Name);
                fileName = $"{Guid.NewGuid()}{ext}";
            }
            var folder = Path.Combine(_wwwrootPath, $"part_images_{safeBrand}");
            Directory.CreateDirectory(folder);
            var filePath = Path.Combine(folder, fileName);

            using var stream = file.OpenReadStream(10 * 1024 * 1024); // max 10MB
            using var image = await Image.LoadAsync(stream);
            if (image.Width > 700)
            {
                var ratio = 700f / image.Width;
                var newHeight = (int)(image.Height * ratio);
                image.Mutate(x => x.Resize(700, newHeight));
            }
            await image.SaveAsync(filePath);
            // Return relative path for DB
            return $"part_images_{safeBrand}/{fileName}";
        }
    }
}
