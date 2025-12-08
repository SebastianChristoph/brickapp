using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using brickisbrickapp.Data.Entities;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace brickisbrickapp.Data.Services
{
    public class NewSetRequestService
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;

        public NewSetRequestService(AppDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        public async Task<NewSetRequest> AddNewSetRequestAsync(NewSetRequest request, Stream? imageStream, string? imageFileName)
        {
            // Check for duplicate set name or setno for the same brand
            var exists = await _db.NewSetRequests.AnyAsync(r => r.Brand == request.Brand && (r.SetName == request.SetName || r.SetNo == request.SetNo));
            if (exists)
            {
                throw new InvalidOperationException("A set with the same name or set number already exists for this brand.");
            }
            if (imageStream != null && !string.IsNullOrWhiteSpace(request.Brand) && !string.IsNullOrWhiteSpace(imageFileName))
            {
                var brandFolder = Path.Combine(_env.WebRootPath, "set_images", request.Brand);
                Directory.CreateDirectory(brandFolder);
                var imagePath = Path.Combine(brandFolder, imageFileName);
                using (var fileStream = new FileStream(imagePath, FileMode.Create))
                {
                    await imageStream.CopyToAsync(fileStream);
                }
                request.ImagePath = $"set_images/{request.Brand}/{imageFileName}";
            }
            _db.NewSetRequests.Add(request);
            await _db.SaveChangesAsync();
            return request;
        }

        public async Task<List<NewSetRequest>> GetRequestsByUserAsync(string userId)
        {
            return await _db.NewSetRequests.Include(r => r.Items).Where(r => r.UserId == userId).ToListAsync();
        }

        public async Task<List<NewSetRequest>> GetAllRequestsAsync()
        {
            return await _db.NewSetRequests.Include(r => r.Items).ToListAsync();
        }

        public async Task ApproveRequestAsync(int requestId)
        {
            var req = await _db.NewSetRequests.FindAsync(requestId);
            if (req != null)
            {
                req.Status = NewSetRequestStatus.Approved;
                await _db.SaveChangesAsync();
                // Notify user
                var notificationService = (UserNotificationService)_db.GetService(typeof(UserNotificationService));
                if (notificationService != null)
                {
                    await notificationService.AddNotificationAsync(req.UserId, "Set Request Approved", $"Your set request '{req.SetName}' has been approved.", "NewSetRequest", req.Id);
                }
            }
        }

        public async Task RejectRequestAsync(int requestId, string reason)
        {
            var req = await _db.NewSetRequests.FindAsync(requestId);
            if (req != null)
            {
                req.Status = NewSetRequestStatus.Rejected;
                req.ReasonRejected = reason;
                await _db.SaveChangesAsync();
                // Notify user
                var notificationService = (UserNotificationService)_db.GetService(typeof(UserNotificationService));
                if (notificationService != null)
                {
                    await notificationService.AddNotificationAsync(req.UserId, "Set Request Rejected", $"Your set request '{req.SetName}' was rejected: {reason}", "NewSetRequest", req.Id);
                }
            }
        }
    }
}
