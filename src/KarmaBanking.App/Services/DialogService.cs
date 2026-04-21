namespace KarmaBanking.App.Services
{
    using Microsoft.UI.Xaml;
    using Microsoft.UI.Xaml.Controls;
    using System.Threading.Tasks;

    public class DialogService
    {
        public async Task<ContentDialogResult> ShowConfirmDialogAsync(
            string title,
            string message,
            string primaryButtonText,
            string closeButtonText,
            XamlRoot xamlRoot)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                PrimaryButtonText = primaryButtonText,
                CloseButtonText = closeButtonText,
                XamlRoot = xamlRoot,
            };

            var result = await System.WindowsRuntimeSystemExtensions.AsTask(dialog.ShowAsync());
            return result;
        }

        public async Task ShowErrorDialogAsync(
            string title,
            string message,
            XamlRoot xamlRoot)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = xamlRoot,
            };

            await System.WindowsRuntimeSystemExtensions.AsTask(dialog.ShowAsync());
        }

        public async Task<(ContentDialogResult Result, string InputText)> ShowInputDialogAsync(
            string title,
            string placeholder,
            string primaryButtonText,
            string closeButtonText,
            XamlRoot xamlRoot)
        {
            var inputTextBox = new TextBox
            {
                PlaceholderText = placeholder,
            };

            var dialog = new ContentDialog
            {
                Title = title,
                Content = inputTextBox,
                PrimaryButtonText = primaryButtonText,
                CloseButtonText = closeButtonText,
                XamlRoot = xamlRoot,
            };

            var result = await System.WindowsRuntimeSystemExtensions.AsTask(dialog.ShowAsync());
            return (result, inputTextBox.Text);
        }

        public async Task ShowInfoDialogAsync(
            string title,
            string message,
            XamlRoot xamlRoot)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = xamlRoot,
            };

            await System.WindowsRuntimeSystemExtensions.AsTask(dialog.ShowAsync());
        }
    }
}