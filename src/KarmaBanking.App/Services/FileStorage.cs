using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace KarmaBanking.App.Services
{
    public class FileStorage
    {
        private const long MaxFileSizeBytes = 10 * 1024 * 1024;

        private static readonly string[] AllowedExtensions =
        {
            ".png",
            ".jpg",
            ".jpeg",
            ".pdf"
        };

        private readonly string attachmentsFolderPath;

        public FileStorage()
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            attachmentsFolderPath = Path.Combine(localAppData, "KarmaBanking", "Attachments");

            if (!Directory.Exists(attachmentsFolderPath))
            {
                Directory.CreateDirectory(attachmentsFolderPath);
            }
        }

        public async Task<string> UploadFileAsync(string sourceFilePath)
        {
            ValidateFile(sourceFilePath);

            string extension = Path.GetExtension(sourceFilePath);
            string uniqueFileName = $"{Guid.NewGuid()}{extension}";
            string destinationPath = Path.Combine(attachmentsFolderPath, uniqueFileName);

            await using FileStream sourceStream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            await using FileStream destinationStream = new FileStream(destinationPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);

            await sourceStream.CopyToAsync(destinationStream);

            return destinationPath;
        }

        public void DeleteUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return;
            }

            if (File.Exists(url))
            {
                File.Delete(url);
            }
        }

        public string GetSignedDownloadUrl(string url)
        {
            return url;
        }

        private void ValidateFile(string sourceFilePath)
        {
            if (string.IsNullOrWhiteSpace(sourceFilePath))
            {
                throw new ArgumentException("File path is required.");
            }

            if (!File.Exists(sourceFilePath))
            {
                throw new FileNotFoundException("The selected file does not exist.");
            }

            string extension = Path.GetExtension(sourceFilePath).ToLowerInvariant();

            if (!AllowedExtensions.Contains(extension))
            {
                throw new InvalidOperationException("Only PDF, PNG, JPG, and JPEG files are allowed.");
            }

            FileInfo fileInfo = new FileInfo(sourceFilePath);

            if (fileInfo.Length > MaxFileSizeBytes)
            {
                throw new InvalidOperationException("File size must be 10 MB or less.");
            }
        }
    }
}