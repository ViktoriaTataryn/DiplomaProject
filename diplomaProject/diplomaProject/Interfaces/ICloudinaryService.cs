namespace diplomaProject.Interfaces
{
    public interface ICloudinaryService
    {
         Task<string> UploadToCloudinary(IFormFile file);
         string GetPublicIdFromUrl(string url);
        Task<bool> DeleteFromCloudinary(string publicId);
    }
}
