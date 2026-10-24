namespace FinalProjectAPI.Services
{
    public interface IFileService
    {
        Task<string> SaveFileAsync(IFormFile file, string folder);
        bool DeleteFile(string fileUrl);
    }
}
