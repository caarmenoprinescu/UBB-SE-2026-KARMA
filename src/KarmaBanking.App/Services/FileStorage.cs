// <copyright file="FileStorage.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Services;

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

public class FileStorage
{
    private const long MaxFileSizeBytes = 10 * 1024 * 1024;

    private static readonly string[] AllowedExtensions =
    {
        ".png",
        ".jpg",
        ".jpeg",
        ".pdf"
    };

    private readonly string attachmentsFolderPath;

    public FileStorage()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        this.attachmentsFolderPath = Path.Combine(localAppData, "KarmaBanking", "Attachments");

        if (!Directory.Exists(this.attachmentsFolderPath))
        {
            Directory.CreateDirectory(this.attachmentsFolderPath);
        }
    }

    public async Task<string> UploadFileAsync(string sourceFilePath)
    {
        this.ValidateFile(sourceFilePath);

        var extension = Path.GetExtension(sourceFilePath);
        var uniqueFileName = $"{Guid.NewGuid()}{extension}";
        var destinationPath = Path.Combine(this.attachmentsFolderPath, uniqueFileName);

        await using var sourceStream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        await using var destinationStream = new FileStream(
            destinationPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None);

        await sourceStream.CopyToAsync(destinationStream);

        return destinationPath;
    }

    public void DeleteUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        if (File.Exists(url))
        {
            File.Delete(url);
        }
    }

    public string GetSignedDownloadUrl(string url)
    {
        return url;
    }

    private void ValidateFile(string sourceFilePath)
    {
        if (string.IsNullOrWhiteSpace(sourceFilePath))
        {
            throw new ArgumentException("File path is required.");
        }

        if (!File.Exists(sourceFilePath))
        {
            throw new FileNotFoundException("The selected file does not exist.");
        }

        var extension = Path.GetExtension(sourceFilePath).ToLowerInvariant();

        if (!AllowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException("Only PDF, PNG, JPG, and JPEG files are allowed.");
        }

        var fileInfo = new FileInfo(sourceFilePath);

        if (fileInfo.Length > MaxFileSizeBytes)
        {
            throw new InvalidOperationException("File size must be 10 MB or less.");
        }
    }
}