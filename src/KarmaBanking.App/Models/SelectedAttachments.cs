namespace KarmaBanking.App.Models
{
    public class SelectedAttachment
    {
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }

        public string FileSizeDisplay
        {
            get
            {
                if (FileSizeBytes < 1024)
                {
                    return $"{FileSizeBytes} B";
                }

                if (FileSizeBytes < 1024 * 1024)
                {
                    return $"{FileSizeBytes / 1024.0:F2} KB";
                }

                return $"{FileSizeBytes / 1024.0 / 1024.0:F2} MB";
            }
        }
    }
}