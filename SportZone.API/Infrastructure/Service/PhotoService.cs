using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using SportZone.Application.Dtos.Photo;
using SportZone.Application.Interfaces.IService;
// Nhớ using namespace chứa class PhotoUploadResult của bạn
// Ví dụ: using SportZone.Application.DTOs; hoặc SportZone.Application.Common; 

namespace SportZone.Infrastructure.Service;

public class PhotoService : IPhotoService
{
    private readonly Cloudinary _cloudinary;

    public PhotoService(IConfiguration config)
    {
        var acc = new Account(
            config["CloudinarySettings:CloudName"],
            config["CloudinarySettings:ApiKey"],
            config["CloudinarySettings:ApiSecret"]
        );
        _cloudinary = new Cloudinary(acc);
    }

    public async Task<PhotoUploadResult> AddPhotoAsync(IFormFile file)
    {
        var uploadResult = new ImageUploadResult();

        if (file.Length > 0)
        {
            // Mở luồng đọc file
            using var stream = file.OpenReadStream();
            
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                // Tùy chọn: Tự động crop ảnh vuông 500x500 để tối ưu hiển thị sản phẩm
                // Transformation = new Transformation().Height(500).Width(500).Crop("fill").Gravity("face")
            };

            // Thực hiện upload lên Cloudinary
            uploadResult = await _cloudinary.UploadAsync(uploadParams);
        }
        
        if (uploadResult.Error != null) 
    {
        return new PhotoUploadResult 
        { 
            Error = uploadResult.Error.Message // Lấy tin nhắn lỗi từ Cloudinary gán sang
        };
    }

        // Mapping: Chuyển dữ liệu từ Cloudinary sang class DTO của bạn
        // Để tầng Application không bị phụ thuộc vào thư viện CloudinaryDotNet
        return new PhotoUploadResult
        {
            Url = uploadResult.SecureUrl.AbsoluteUri,
            PublicId = uploadResult.PublicId
        };
    }

    public async Task<PhotoDeletionResult> DeletePhotoAsync(string publicId)
    {
        var deleteParams = new DeletionParams(publicId);
        
        // Gọi Cloudinary xóa
        var result = await _cloudinary.DestroyAsync(deleteParams);

        // Map kết quả sang class của mình
        return new PhotoDeletionResult
        {
            Result = result.Result,
            Error = result.Result == "Ok" ? null : "Lỗi khi xóa ảnh trên Cloudinary"
        };
    }
}