using Services.Abstraction.Contract;

namespace GMS.MVC.Services {
    /// <summary>
    /// Saves Uploaded Profile Photos Under <c>wwwroot/uploads/{folder}</c>. Uploads Are Always
    /// Stored Under A Freshly Generated Name So A Caller Can Never Steer The Write With A Crafted
    /// File Name, And Only A Short Allow-List Of Image Extensions Is Accepted.
    /// </summary>
    public class AttachmentService(IWebHostEnvironment _environment, ILogger<AttachmentService> _logger) : IAttachmentService {

        private const long MaxBytes = 2 * 1024 * 1024; // 2 MB
        private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];
        private const string UploadRoot = "uploads";

        public bool IsAllowed(string originalFileName, long lengthInBytes) {
            if (lengthInBytes <= 0 || lengthInBytes > MaxBytes) return false;
            var extension = Path.GetExtension(originalFileName);
            return !string.IsNullOrWhiteSpace(extension)
                && AllowedExtensions.Contains(extension.ToLowerInvariant());
        }

        public async Task<string?> UploadAsync(Stream content, string originalFileName, string folderName) {
            try {
                var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
                if (!AllowedExtensions.Contains(extension)) return null;

                var folderPath = GetFolderPath(folderName);
                Directory.CreateDirectory(folderPath);

                // The Stored Name Is Generated, Never Taken From The Upload.
                var storedName = $"{Guid.NewGuid():N}{extension}";
                var fullPath = Path.Combine(folderPath, storedName);

                await using var fileStream = new FileStream(fullPath, FileMode.Create);
                await content.CopyToAsync(fileStream);

                return storedName;
            } catch (Exception ex) {
                _logger.LogError(ex, "Uploading An Attachment To {Folder} Failed.", folderName);
                return null;
            }
        }

        public void Delete(string? fileName, string folderName) {
            if (string.IsNullOrWhiteSpace(fileName)) return;
            try {
                // Guard Against A Stored Value That Tries To Walk Out Of The Upload Folder.
                var safeName = Path.GetFileName(fileName);
                if (string.IsNullOrWhiteSpace(safeName)) return;

                var fullPath = Path.Combine(GetFolderPath(folderName), safeName);
                if (File.Exists(fullPath)) File.Delete(fullPath);
            } catch (Exception ex) {
                _logger.LogError(ex, "Deleting The Attachment {File} From {Folder} Failed.", fileName, folderName);
            }
        }

        private string GetFolderPath(string folderName) {
            var webRoot = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            return Path.Combine(webRoot, UploadRoot, Path.GetFileName(folderName));
        }
    }
}
