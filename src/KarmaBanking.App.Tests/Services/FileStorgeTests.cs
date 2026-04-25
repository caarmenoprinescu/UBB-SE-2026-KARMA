namespace KarmaBanking.App.Tests.Services
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using KarmaBanking.App.Services;
    using Xunit;
    public class FileStorageTests : IDisposable
    {
        private readonly FileStorage fileStorage;
        private readonly string testFilesDirectory;

        public FileStorageTests()
        {
            this.fileStorage = new FileStorage();

            this.testFilesDirectory = Path.Combine(Path.GetTempPath(), "KarmaBankingTestFiles_" + Guid.NewGuid());
            Directory.CreateDirectory(this.testFilesDirectory);
        }

        [Fact]
        public async Task UploadFileAsync_ThrowsArgumentException_WhenPathIsNullOrWhitespace()
        {
            await Assert.ThrowsAsync<ArgumentException>(() => this.fileStorage.UploadFileAsync(string.Empty));
            await Assert.ThrowsAsync<ArgumentException>(() => this.fileStorage.UploadFileAsync(null!));
        }

        [Fact]
        public async Task UploadFileAsync_ThrowsFileNotFoundException_WhenFileDoesNotExist()
        {
            string fakePath = Path.Combine(this.testFilesDirectory, "doesnotexist.pdf");

            await Assert.ThrowsAsync<FileNotFoundException>(() => this.fileStorage.UploadFileAsync(fakePath));
        }

        [Fact]
        public async Task UploadFileAsync_ThrowsInvalidOperationException_WhenExtensionIsInvalid()
        {
            string invalidExtensionPath = Path.Combine(this.testFilesDirectory, "testfile.txt");
            await File.WriteAllTextAsync(invalidExtensionPath, "Some content");

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => this.fileStorage.UploadFileAsync(invalidExtensionPath));
            Assert.Equal("Only PDF, PNG, JPG, and JPEG files are allowed.", ex.Message);
        }

        [Fact]
        public async Task UploadFileAsync_ThrowsInvalidOperationException_WhenFileIsTooLarge()
        {
            string largeFilePath = Path.Combine(this.testFilesDirectory, "toolarge.pdf");

            using (var fs = new FileStream(largeFilePath, FileMode.Create, FileAccess.Write))
            {
                fs.SetLength((10 * 1024 * 1024) + 1);
            }

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => this.fileStorage.UploadFileAsync(largeFilePath));
            Assert.Equal("File size must be 10 MB or less.", ex.Message);
        }

        [Fact]
        public async Task UploadFileAsync_SuccessfullyCopiesFile_WhenFileIsValid()
        {
            string validFilePath = Path.Combine(this.testFilesDirectory, "validimage.png");
            await File.WriteAllTextAsync(validFilePath, "fake image content");

            string destinationPath = await this.fileStorage.UploadFileAsync(validFilePath);

            Assert.NotNull(destinationPath);
            Assert.True(File.Exists(destinationPath), "The file should have been copied to the destination.");
            Assert.EndsWith(".png", destinationPath);

            if (File.Exists(destinationPath))
            {
                File.Delete(destinationPath);
            }
        }

        [Fact]
        public async Task DeleteUrl_RemovesFile_WhenFileExists()
        {
            string fileToDelete = Path.Combine(this.testFilesDirectory, "todelete.pdf");
            await File.WriteAllTextAsync(fileToDelete, "dummy content");

            Assert.True(File.Exists(fileToDelete));

            this.fileStorage.DeleteUrl(fileToDelete);

            Assert.False(File.Exists(fileToDelete), "The file should have been deleted.");
        }

        [Fact]
        public void DeleteUrl_DoesNothing_WhenUrlIsEmptyOrFileDoesNotExist()
        {
            string nonExistentPath = Path.Combine(this.testFilesDirectory, "ghost.pdf");

            var exceptionForEmpty = Record.Exception(() => this.fileStorage.DeleteUrl(string.Empty));
            var exceptionForMissing = Record.Exception(() => this.fileStorage.DeleteUrl(nonExistentPath));

            Assert.Null(exceptionForEmpty);
            Assert.Null(exceptionForMissing);
        }

        [Fact]
        public void GetSignedDownloadUrl_ReturnsSameUrl()
        {
            string inputUrl = "https://example.com/file.pdf";

            string result = this.fileStorage.GetSignedDownloadUrl(inputUrl);

            Assert.Equal(inputUrl, result);
        }

        public void Dispose()
        {
            if (Directory.Exists(this.testFilesDirectory))
            {
                Directory.Delete(this.testFilesDirectory, true);
            }
        }
    }
}