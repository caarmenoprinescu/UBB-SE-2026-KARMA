using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

public class PdfExporter
{
    private const float PageWidth = 595f;
    private const float PageHeight = 842f;
    private const float LeftMargin = 48f;
    private const float TopMargin = 54f;
    private const float RowHeight = 18f;

    public byte[] exportAmortization(IEnumerable<AmortizationRow> rows)
    {
        List<AmortizationRow> amortizationRows = rows?.ToList() ?? [];
        List<string> pageContents = BuildAmortizationPages(amortizationRows);

        return BuildPdfDocument(pageContents);
    }

    public byte[] exportTransactions(IEnumerable<object> rows)
    {
        throw new NotImplementedException("Transaction PDF export is not implemented yet.");
    }

    private static List<string> BuildAmortizationPages(IReadOnlyList<AmortizationRow> rows)
    {
        List<string> pages = [];
        StringBuilder currentPage = new StringBuilder();

        WriteTitle(currentPage, "Amortisation schedule", PageHeight - TopMargin);
        WriteHeader(currentPage, PageHeight - TopMargin - 34f);

        float yPosition = PageHeight - TopMargin - 58f;

        foreach (AmortizationRow row in rows)
        {
            if (yPosition < 68f)
            {
                pages.Add(currentPage.ToString());
                currentPage = new StringBuilder();
                WriteTitle(currentPage, "Amortisation schedule", PageHeight - TopMargin);
                WriteHeader(currentPage, PageHeight - TopMargin - 34f);
                yPosition = PageHeight - TopMargin - 58f;
            }

            WriteRow(currentPage, row, yPosition);
            yPosition -= RowHeight;
        }

        pages.Add(currentPage.ToString());
        return pages;
    }

    private static void WriteTitle(StringBuilder builder, string title, float y)
    {
        AppendText(builder, 18, LeftMargin, y, title);
    }

    private static void WriteHeader(StringBuilder builder, float y)
    {
        AppendText(builder, 11, LeftMargin, y, "#");
        AppendText(builder, 11, LeftMargin + 36f, y, "Due date");
        AppendText(builder, 11, LeftMargin + 130f, y, "Principal");
        AppendText(builder, 11, LeftMargin + 255f, y, "Interest");
        AppendText(builder, 11, LeftMargin + 360f, y, "Balance");
    }

    private static void WriteRow(StringBuilder builder, AmortizationRow row, float y)
    {
        AppendText(builder, 10, LeftMargin, y, row.InstallmentNumber.ToString(CultureInfo.InvariantCulture));
        AppendText(builder, 10, LeftMargin + 36f, y, row.DueDate.ToString("MMM ''yy", CultureInfo.InvariantCulture));
        AppendText(builder, 10, LeftMargin + 130f, y, FormatCurrency(row.PrincipalPortion));
        AppendText(builder, 10, LeftMargin + 255f, y, FormatCurrency(row.InterestPortion));
        AppendText(builder, 10, LeftMargin + 360f, y, FormatCurrency(row.RemainingBalance));
    }

    private static string FormatCurrency(decimal value)
    {
        return value.ToString("C2", CultureInfo.CurrentCulture);
    }

    private static void AppendText(StringBuilder builder, int fontSize, float x, float y, string text)
    {
        builder.AppendLine("BT");
        builder.AppendLine($"/F1 {fontSize} Tf");
        builder.AppendLine($"{x.ToString("0.##", CultureInfo.InvariantCulture)} {y.ToString("0.##", CultureInfo.InvariantCulture)} Td");
        builder.AppendLine($"({EscapePdfText(text)}) Tj");
        builder.AppendLine("ET");
    }

    private static string EscapePdfText(string value)
    {
        return (value ?? string.Empty)
            .Replace("\\", "\\\\")
            .Replace("(", "\\(")
            .Replace(")", "\\)");
    }

    private static byte[] BuildPdfDocument(IReadOnlyList<string> pageContents)
    {
        List<byte[]> objects = [];
        int pageCount = pageContents.Count;
        int fontObjectId = 3 + pageCount * 2;
        StringBuilder pageReferences = new StringBuilder();

        for (int i = 0; i < pageCount; i++)
        {
            int pageObjectId = 3 + (i * 2);
            pageReferences.Append($"{pageObjectId} 0 R ");
        }

        objects.Add(ToPdfBytes("<< /Type /Catalog /Pages 2 0 R >>"));
        objects.Add(ToPdfBytes($"<< /Type /Pages /Kids [{pageReferences.ToString().TrimEnd()}] /Count {pageCount} >>"));

        for (int i = 0; i < pageCount; i++)
        {
            int contentObjectId = 4 + (i * 2);
            string pageObject =
                $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {PageWidth.ToString("0", CultureInfo.InvariantCulture)} {PageHeight.ToString("0", CultureInfo.InvariantCulture)}] " +
                $"/Resources << /Font << /F1 {fontObjectId} 0 R >> >> /Contents {contentObjectId} 0 R >>";

            byte[] contentBytes = Encoding.ASCII.GetBytes(pageContents[i]);
            string streamHeader = $"<< /Length {contentBytes.Length} >>\nstream\n";
            string streamFooter = "endstream";

            objects.Add(ToPdfBytes(pageObject));
            objects.Add(CombineBytes(
                Encoding.ASCII.GetBytes(streamHeader),
                contentBytes,
                Encoding.ASCII.GetBytes("\n" + streamFooter)));
        }

        objects.Add(ToPdfBytes("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"));

        using MemoryStream stream = new MemoryStream();
        using StreamWriter writer = new StreamWriter(stream, Encoding.ASCII, leaveOpen: true);

        writer.Write("%PDF-1.4\n");
        writer.Flush();

        List<long> offsets = [0];

        for (int i = 0; i < objects.Count; i++)
        {
            offsets.Add(stream.Position);
            writer.Write($"{i + 1} 0 obj\n");
            writer.Flush();
            stream.Write(objects[i], 0, objects[i].Length);
            writer.Write("\nendobj\n");
            writer.Flush();
        }

        long xrefPosition = stream.Position;
        writer.Write($"xref\n0 {objects.Count + 1}\n");
        writer.Write("0000000000 65535 f \n");

        for (int i = 1; i < offsets.Count; i++)
        {
            writer.Write($"{offsets[i]:D10} 00000 n \n");
        }

        writer.Write("trailer\n");
        writer.Write($"<< /Size {objects.Count + 1} /Root 1 0 R >>\n");
        writer.Write("startxref\n");
        writer.Write($"{xrefPosition}\n");
        writer.Write("%%EOF");
        writer.Flush();

        return stream.ToArray();
    }

    private static byte[] ToPdfBytes(string value)
    {
        return Encoding.ASCII.GetBytes(value);
    }

    private static byte[] CombineBytes(params byte[][] parts)
    {
        int totalLength = parts.Sum(part => part.Length);
        byte[] result = new byte[totalLength];
        int offset = 0;

        foreach (byte[] part in parts)
        {
            Buffer.BlockCopy(part, 0, result, offset, part.Length);
            offset += part.Length;
        }

        return result;
    }
}
