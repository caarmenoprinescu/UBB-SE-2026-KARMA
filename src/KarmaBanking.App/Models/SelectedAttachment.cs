// <copyright file="SelectedAttachment.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Models;

/// <summary>
/// Represents a locally selected file before chat attachment upload.
/// </summary>
public class SelectedAttachment
{
    /// <summary>
    /// Gets or sets the original file name.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the absolute local file path.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the content/MIME type.
    /// </summary>
    public string FileType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the file size in bytes.
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// Gets a human-readable size label derived from <see cref="FileSizeBytes"/>.
    /// </summary>
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