// <copyright file="DialogServiceTests.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Tests.Services
{
    using System;
    using System.Threading.Tasks;
    using KarmaBanking.App.Services;
    using Xunit;

    public class DialogServiceTests
    {
        [Fact]
        public async Task ShowConfirmDialogAsync_NullXamlRoot_ThrowsException()
        {
            // Arrange
            var dialogService = new DialogService();

            // Act & Assert
            await Assert.ThrowsAnyAsync<Exception>(async () =>
                await dialogService.ShowConfirmDialogAsync("Title", "Message", "Yes", "No", null));
        }

        [Theory]
        [InlineData("", "", "", "")]
        [InlineData(null, null, null, null)]
        public async Task ShowConfirmDialogAsync_NullOrEmptyStrings_ThrowsException(
            string title,
            string message,
            string primaryButtonText,
            string closeButtonText)
        {
            // Arrange
            var dialogService = new DialogService();

            // Act & Assert
            await Assert.ThrowsAnyAsync<Exception>(async () =>
                await dialogService.ShowConfirmDialogAsync(title, message, primaryButtonText, closeButtonText, null));
        }

        [Fact]
        public async Task ShowErrorDialogAsync_NullXamlRoot_ThrowsException()
        {
            // Arrange
            var dialogService = new DialogService();

            // Act & Assert
            await Assert.ThrowsAnyAsync<Exception>(async () =>
                await dialogService.ShowErrorDialogAsync("Title", "Message", null));
        }

        [Fact]
        public async Task ShowInputDialogAsync_NullXamlRoot_ThrowsException()
        {
            // Arrange
            var dialogService = new DialogService();

            // Act & Assert
            await Assert.ThrowsAnyAsync<Exception>(async () =>
                await dialogService.ShowInputDialogAsync("Title", "Placeholder", "OK", "Cancel", null));
        }

        [Fact]
        public async Task ShowInfoDialogAsync_NullXamlRoot_ThrowsException()
        {
            // Arrange
            var dialogService = new DialogService();

            // Act & Assert
            await Assert.ThrowsAnyAsync<Exception>(async () =>
                await dialogService.ShowInfoDialogAsync("Title", "Message", null));
        }
    }
}