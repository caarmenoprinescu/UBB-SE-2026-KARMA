// <copyright file="FileValidationService.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Services;

using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using KarmaBanking.App.Models;
using Windows.Storage;

/// <summary>
/// Provides services for validating files and mapping them to attachment models.
/// </summary>
public class FileValidationService
{
    /// <summary>
    /// The maximum allowed file size in bytes (10 MB).
    /// </summary>
    private const long MaxFileSize = 10 * 1024 * 1024;

    /// <summary>
    /// Converts a file size in bytes to a human-readable formatted string (e.g., KB or MB).
    /// </summary>
    /// <param name="sizeInBytes">The file size in bytes.</param>
    /// <returns>A formatted string representing the file size.</returns>
    public static string GetFileSizeDisplay(long sizeInBytes)
    {
        const long kb = 1024;
        const long mb = kb * 1024;

        if (sizeInBytes >= mb)
        {
            // 2. Use .ToString with InvariantCulture
            return (sizeInBytes / (double)mb).ToString("0.##", CultureInfo.InvariantCulture) + " MB";
        }

        if (sizeInBytes >= kb)
        {
            // 3. Use .ToString with InvariantCulture
            return (sizeInBytes / (double)kb).ToString("0.##", CultureInfo.InvariantCulture) + " KB";
        }

        return $"{sizeInBytes} B";
    }

    /// <summary>
    /// Validates a storage file based on its size and file extension.
    /// </summary>
    /// <param name="file">The storage file to validate.</param>
    /// <returns>A tuple containing a boolean indicating if the file is valid, and an error message if it is not.</returns>
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

    /// <summary>
    /// Maps a <see cref="StorageFile"/> to a <see cref="SelectedAttachment"/> model containing its metadata.
    /// </summary>
    /// <param name="file">The storage file to map.</param>
    /// <returns>A <see cref="SelectedAttachment"/> populated with the file's details.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the file mapping process fails.</exception>
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
}