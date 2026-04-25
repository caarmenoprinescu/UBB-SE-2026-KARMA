// <copyright file="FileValidationServiceTests.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

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
            // Arrange
            long sizeInBytes = 500;

            // Act
            string formattedSizeDisplay = FileValidationService.GetFileSizeDisplay(sizeInBytes);

            // Assert
            Assert.Equal("500 B", formattedSizeDisplay);
        }

        [Fact]
        public void GetFileSizeDisplay_ExactKilobyte_ReturnsKilobytesFormatted()
        {
            // Arrange
            long sizeInBytes = 1024;

            // Act
            string formattedSizeDisplay = FileValidationService.GetFileSizeDisplay(sizeInBytes);

            // Assert
            Assert.Equal("1 KB", formattedSizeDisplay);
        }

        [Fact]
        public void GetFileSizeDisplay_ExactMegabyte_ReturnsMegabytesFormatted()
        {
            // Arrange
            long sizeInBytes = 1048576;

            // Act
            string formattedSizeDisplay = FileValidationService.GetFileSizeDisplay(sizeInBytes);

            // Assert
            Assert.Equal("1 MB", formattedSizeDisplay);
        }

        [Fact]
        public async Task ValidateFileAsync_NullFile_ReturnsFalseAndErrorMessage()
        {
            // Arrange
            var fileValidationService = new FileValidationService();

            // Act
            var (isValid, errorMessage) = await fileValidationService.ValidateFileAsync(null);

            // Assert
            Assert.False(isValid);
            Assert.Equal("No file selected.", errorMessage);
        }

        [Fact]
        public async Task MapStorageFileToAttachmentAsync_NullFile_ThrowsInvalidOperationException()
        {
            // Arrange
            var fileValidationService = new FileValidationService();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await fileValidationService.MapStorageFileToAttachmentAsync(null));

            Assert.Contains("Failed to map file to attachment", exception.Message);
        }
    }
}