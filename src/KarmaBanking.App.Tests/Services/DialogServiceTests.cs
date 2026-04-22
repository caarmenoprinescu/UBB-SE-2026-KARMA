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
            var service = new DialogService();

            await Assert.ThrowsAnyAsync<Exception>(async () =>
                await service.ShowConfirmDialogAsync("Title", "Message", "Yes", "No", null));
        }

        [Fact]
        public async Task ShowErrorDialogAsync_NullXamlRoot_ThrowsException()
        {
            var service = new DialogService();

            await Assert.ThrowsAnyAsync<Exception>(async () =>
                await service.ShowErrorDialogAsync("Title", "Message", null));
        }

        [Fact]
        public async Task ShowInputDialogAsync_NullXamlRoot_ThrowsException()
        {
            var service = new DialogService();

            await Assert.ThrowsAnyAsync<Exception>(async () =>
                await service.ShowInputDialogAsync("Title", "Placeholder", "OK", "Cancel", null));
        }

        [Fact]
        public async Task ShowInfoDialogAsync_NullXamlRoot_ThrowsException()
        {
            var service = new DialogService();

            await Assert.ThrowsAnyAsync<Exception>(async () =>
                await service.ShowInfoDialogAsync("Title", "Message", null));
        }
    }
}