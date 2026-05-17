using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using diplomaProject.Interfaces;

namespace diplomaProject.Services;

public class CloudinaryService : ICloudinaryService
{
    private readonly Cloudinary _cloudinary;

    public CloudinaryService(Cloudinary cloudinary)
    {
        _cloudinary = cloudinary;
    }

    public async Task<string> UploadToCloudinary(IFormFile file)
    {
        using var stream = file.OpenReadStream();
        var extension = Path.GetExtension(file.FileName).ToLower();
        var isImage = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" }.Contains(extension);

        var uploadParams = isImage
            ? new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Transformation = new Transformation().Width(1200).Crop("limit").Quality("auto")
            }
            : (object)new RawUploadParams
            {
                File = new FileDescription(file.FileName, stream)
            };
        var uploadResult = await _cloudinary.UploadAsync((dynamic)uploadParams);

        if (uploadResult.Error != null)
            throw new Exception($"Cloudinary Error: {uploadResult.Error.Message}");

        return uploadResult.SecureUrl.ToString();
    }

    public string GetPublicIdFromUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            // Отримуємо останній сегмент шляху, наприклад "v12345/public_id.jpg"
            var segments = uri.Segments;
            var fileNameWithExtension = segments.Last();

            // Видаляємо розширення (.jpg, .png тощо), щоб отримати чистий PublicId
            var publicId = Path.GetFileNameWithoutExtension(fileNameWithExtension);

            return publicId;
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }

    public async Task<bool> DeleteFromCloudinary(string publicId)
    {
        var deletionParams = new DeletionParams(publicId);
        var result = await _cloudinary.DestroyAsync(deletionParams);
        return result.Result == "ok";
    }
}