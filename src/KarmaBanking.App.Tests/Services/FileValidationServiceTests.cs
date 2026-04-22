namespace KarmaBanking.App.Tests.Services
{
    using System;
    using System.Threading.Tasks;
    using KarmaBanking.App.Services;
    using Xunit;

    public class FileValidationServiceTests
    {
        [Fact]
        public void GetFileSizeDisplay_BytesSize_ReturnsBytesFormatted()
        {
            long size = 500;
            var result = FileValidationService.GetFileSizeDisplay(size);
            Assert.Equal("500 B", result);
        }

        [Fact]
        public void GetFileSizeDisplay_ExactKilobyte_ReturnsKilobytesFormatted()
        {
            long size = 1024;
            var result = FileValidationService.GetFileSizeDisplay(size);
            Assert.Equal("1 KB", result);
        }

        [Fact]
        public void GetFileSizeDisplay_KilobytesSize_ReturnsKilobytesFormatted()
        {
            long size = 1536;
            var result = FileValidationService.GetFileSizeDisplay(size);
            Assert.Equal("1.5 KB", result);
        }

        [Fact]
        public void GetFileSizeDisplay_ExactMegabyte_ReturnsMegabytesFormatted()
        {
            long size = 1048576;
            var result = FileValidationService.GetFileSizeDisplay(size);
            Assert.Equal("1 MB", result);
        }

        [Fact]
        public void GetFileSizeDisplay_MegabytesSize_ReturnsMegabytesFormatted()
        {
            long size = 2621440;
            var result = FileValidationService.GetFileSizeDisplay(size);
            Assert.Equal("2.5 MB", result);
        }

        [Fact]
        public async Task ValidateFileAsync_NullFile_ReturnsFalseAndErrorMessage()
        {
            var service = new FileValidationService();
            var (isValid, errorMessage) = await service.ValidateFileAsync(null);

            Assert.False(isValid);
            Assert.Equal("No file selected.", errorMessage);
        }

        [Fact]
        public async Task MapStorageFileToAttachmentAsync_NullFile_ThrowsInvalidOperationException()
        {
            var service = new FileValidationService();

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await service.MapStorageFileToAttachmentAsync(null));

            Assert.Contains("Failed to map file to attachment", exception.Message);
        }
    }
}