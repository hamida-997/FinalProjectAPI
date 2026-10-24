namespace FinalProjectAPI.Services
{
    public class FileService : IFileService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly string[] _allowedExtensions;
        private readonly long _maxFileSize;

        public FileService(IWebHostEnvironment environment, IConfiguration configuration)
        {
            _environment = environment;

            var settings = configuration.GetSection("FileUploadSettings");

            // Load allowed extensions from config, use defaults if not configured
            _allowedExtensions = settings.GetSection("AllowedExtensions").Get<string[]>()
                ?? new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

            // Load max file size in MB from config, default to 5 MB
            var maxFileSizeInMB = settings.GetValue<int?>("MaxFileSizeInMB") ?? 5;
            _maxFileSize = maxFileSizeInMB * 1024L * 1024L;
        }

        public async Task<string> SaveFileAsync(IFormFile file, string folder)
        {
            // Validate file is not empty
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("File is empty or not provided.");
            }

            // Validate file size
            if (file.Length > _maxFileSize)
            {
                throw new ArgumentException($"File size exceeds the maximum allowed size of {_maxFileSize / 1024 / 1024} MB.");
            }

            // Validate file extension
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(extension))
            {
                throw new ArgumentException($"File type '{extension}' is not allowed. Allowed types: {string.Join(", ", _allowedExtensions)}");
            }

            // Build the full folder path inside wwwroot
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", folder);

            // Create directory if it doesn't exist
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            // Generate unique file name to avoid collisions
            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            // Save the file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Return the relative URL path
            return $"/uploads/{folder}/{uniqueFileName}";
        }

        public bool DeleteFile(string fileUrl)
        {
            if (string.IsNullOrEmpty(fileUrl))
            {
                return false;
            }

            try
            {
                // Convert URL to physical path
                var relativePath = fileUrl.TrimStart('/');
                var filePath = Path.Combine(_environment.WebRootPath, relativePath);

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}
