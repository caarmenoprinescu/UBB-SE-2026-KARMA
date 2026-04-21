// <copyright file="SelectedAttachments.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Models;

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
            if (this.FileSizeBytes < 1024)
            {
                return $"{this.FileSizeBytes} B";
            }

            if (this.FileSizeBytes < 1024 * 1024)
            {
                return $"{this.FileSizeBytes / 1024.0:F2} KB";
            }

            return $"{this.FileSizeBytes / 1024.0 / 1024.0:F2} MB";
        }
    }
}