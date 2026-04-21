// <copyright file="FileValidationService.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Services;

using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

using KarmaBanking.App.Models;
public class FileValidationService
{
    private const long MaxFileSize = 10 * 1024 * 1024;

    public async Task<(bool IsValid, string ErrorMessage)> ValidateFileAsync(StorageFile file)
    {
        try
        {
            if (file == null)
            {
                return (false, "No file selected.");
            }

            var properties = await file.GetBasicPropertiesAsync();
            if (properties.Size > MaxFileSize)
            {
                return (false, "File size must be 10 MB or less.");
            }

            var fileExtension = Path.GetExtension(file.Name).ToLowerInvariant();
            string[] allowedExtensions = [".pdf", ".png", ".jpg", ".jpeg"];
            var isAllowedType = false;

            foreach (var ext in allowedExtensions)
            {
                if (fileExtension == ext)
                {
                    isAllowedType = true;
                    break;
                }
            }

            if (!isAllowedType)
            {
                return (false, $"File type '{fileExtension}' is not allowed. Allowed types: PDF, PNG, JPG, JPEG.");
            }

            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            return (false, $"Error validating file: {ex.Message}");
        }
    }

    public async Task<SelectedAttachment> MapStorageFileToAttachmentAsync(StorageFile file)
    {
        try
        {
            var properties = await file.GetBasicPropertiesAsync();

            return new SelectedAttachment
            {
                FileName = file.Name,
                FilePath = file.Path,
                FileType = Path.GetExtension(file.Name).ToLowerInvariant(),
                FileSizeBytes = (long)properties.Size,
            };
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to map file to attachment: {ex.Message}", ex);
        }
    }

    public static string GetFileSizeDisplay(long sizeInBytes)
    {
        const long kb = 1024;
        const long mb = kb * 1024;

        if (sizeInBytes >= mb)
        {
            return $"{sizeInBytes / (double)mb:0.##} MB";
        }

        if (sizeInBytes >= kb)
        {
            return $"{sizeInBytes / (double)kb:0.##} KB";
        }

        return $"{sizeInBytes} B";
    }
}