namespace SportZone.Application.Dtos.Photo;
public class PhotoDeletionResult
{
    public string? Error { get; set; } // Nếu null nghĩa là xóa thành công
    public string? Result { get; set; } // "ok" hoặc "not found"
}