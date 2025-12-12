
using System;
using System.IO;
using System.Threading.Tasks;
using Data.Entities;
using Microsoft.AspNetCore.Components.Forms;

namespace Data.Services
{
    public class ImageService
    {
        private readonly string _wwwrootPath;
        public ImageService(string wwwrootPath)
        {
            _wwwrootPath = wwwrootPath;
        }

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
    }
}
