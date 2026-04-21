// <copyright file="ChatAttachment.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Models;

public class ChatAttachment
{
    public int Id { get; set; }

    public int MessageId { get; set; }

    public string AttachmentName { get; set; } = string.Empty;

    public string FileType { get; set; } = string.Empty;

    public int FileSizeBytes { get; set; }

    public string StorageUrl { get; set; } = string.Empty;
}