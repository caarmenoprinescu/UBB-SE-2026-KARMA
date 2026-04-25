// <copyright file="FileStorageTests.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

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
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => this.fileStorage.UploadFileAsync(string.Empty));
            await Assert.ThrowsAsync<ArgumentException>(() => this.fileStorage.UploadFileAsync(null!));
        }

        [Fact]
        public async Task UploadFileAsync_ThrowsFileNotFoundException_WhenFileDoesNotExist()
        {
            // Arrange
            string nonExistentPath = Path.Combine(this.testFilesDirectory, "doesnotexist.pdf");

            // Act & Assert
            await Assert.ThrowsAsync<FileNotFoundException>(() => this.fileStorage.UploadFileAsync(nonExistentPath));
        }

        [Fact]
        public async Task UploadFileAsync_ThrowsInvalidOperationException_WhenExtensionIsInvalid()
        {
            // Arrange
            string invalidExtensionPath = Path.Combine(this.testFilesDirectory, "testfile.txt");
            await File.WriteAllTextAsync(invalidExtensionPath, "Some content");

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                this.fileStorage.UploadFileAsync(invalidExtensionPath));
            Assert.Equal("Only PDF, PNG, JPG, and JPEG files are allowed.", exception.Message);
        }

        [Fact]
        public async Task UploadFileAsync_ThrowsInvalidOperationException_WhenFileIsTooLarge()
        {
            // Arrange
            string largeFilePath = Path.Combine(this.testFilesDirectory, "toolarge.pdf");

            using (var fileStream = new FileStream(largeFilePath, FileMode.Create, FileAccess.Write))
            {
                // Set length to 10MB + 1 byte
                fileStream.SetLength((10 * 1024 * 1024) + 1);
            }

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                this.fileStorage.UploadFileAsync(largeFilePath));
            Assert.Equal("File size must be 10 MB or less.", exception.Message);
        }

        [Fact]
        public async Task UploadFileAsync_SuccessfullyCopiesFile_WhenFileIsValid()
        {
            // Arrange
            string validFilePath = Path.Combine(this.testFilesDirectory, "validimage.png");
            await File.WriteAllTextAsync(validFilePath, "fake image content");

            // Act
            string destinationPath = await this.fileStorage.UploadFileAsync(validFilePath);

            // Assert
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
            // Arrange
            string fileToDeletePath = Path.Combine(this.testFilesDirectory, "todelete.pdf");
            await File.WriteAllTextAsync(fileToDeletePath, "dummy content");

            // Act
            this.fileStorage.DeleteUrl(fileToDeletePath);

            // Assert
            Assert.False(File.Exists(fileToDeletePath), "The file should have been deleted.");
        }

        [Fact]
        public void GetSignedDownloadUrl_ReturnsSameUrl()
        {
            // Arrange
            string inputUrl = "https://example.com/file.pdf";

            // Act
            string result = this.fileStorage.GetSignedDownloadUrl(inputUrl);

            // Assert
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