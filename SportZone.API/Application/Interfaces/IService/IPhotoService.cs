using Microsoft.AspNetCore.Http;
using SportZone.Application.Dtos.Photo;

namespace SportZone.Application.Interfaces.IService;
public interface IPhotoService {
    Task<PhotoUploadResult> AddPhotoAsync(IFormFile file);
    Task<PhotoDeletionResult> DeletePhotoAsync(string publicId);
}