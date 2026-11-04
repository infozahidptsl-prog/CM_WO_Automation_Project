using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using WO_CM_Review_Automation.Application.Interfaces;

namespace WO_CM_Review_Automation.Infrastructure.Services.CMReview;

public class ImageStorageService : IImageStorageService
{
    private readonly IWebHostEnvironment _env;

    public ImageStorageService(IWebHostEnvironment env)
    {
        _env = env;
    }

    public async Task<string> SaveImageAsync(string base64OrBytes, string application, int year, int month, Guid documentId, string folder, string extension)
    {
        // CMReviewEvidence/2026/6/LEA/{DocumentId}/server/{Guid}.png
        var relativePath = Path.Combine("CMReviewEvidence", year.ToString(), month.ToString(), application, documentId.ToString(), folder);
        var storageDir = Path.Combine(_env.ContentRootPath, relativePath);

        if (!Directory.Exists(storageDir))
        {
            Directory.CreateDirectory(storageDir);
        }

        var fileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(storageDir, fileName);

        if (base64OrBytes.StartsWith("data:image"))
        {
            var base64Data = base64OrBytes.Substring(base64OrBytes.IndexOf(",") + 1);
            var bytes = Convert.FromBase64String(base64Data);
            await File.WriteAllBytesAsync(filePath, bytes);
        }
        else
        {
            var bytes = Convert.FromBase64String(base64OrBytes);
            await File.WriteAllBytesAsync(filePath, bytes);
        }

        // Return relative reference
        return Path.Combine(relativePath, fileName).Replace("\\", "/");
    }

    public Task DeleteImageAsync(string storageReference)
    {
        if (string.IsNullOrWhiteSpace(storageReference)) return Task.CompletedTask;

        var fullPath = Path.Combine(_env.ContentRootPath, storageReference.Replace("/", Path.DirectorySeparatorChar.ToString()));
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    public Task<string?> GetImagePathAsync(string storageReference)
    {
        if (string.IsNullOrWhiteSpace(storageReference)) return Task.FromResult<string?>(null);

        var fullPath = Path.Combine(_env.ContentRootPath, storageReference.Replace("/", Path.DirectorySeparatorChar.ToString()));
        if (File.Exists(fullPath))
        {
            return Task.FromResult<string?>(fullPath);
        }

        return Task.FromResult<string?>(null);
    }
}
