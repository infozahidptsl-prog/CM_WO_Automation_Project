using System;
using System.IO;
using System.Threading.Tasks;

namespace WO_CM_Review_Automation.Application.Interfaces;

public interface IImageStorageService
{
    Task<string> SaveImageAsync(string base64OrBytes, string application, int year, int month, Guid documentId, string folder, string extension);
    Task DeleteImageAsync(string storageReference);
    Task<string?> GetImagePathAsync(string storageReference);
}
