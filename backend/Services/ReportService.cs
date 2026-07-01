using System.Globalization;
using System.Text;
using CsvHelper;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using StAmanuelBloodDonation.Api.Data;
using StAmanuelBloodDonation.Api.Models;

namespace StAmanuelBloodDonation.Api.Services;

public interface IReportService
{
    Task<(byte[] Data, string FileName, string ContentType)> GenerateReportAsync(ReportRequest request, Guid adminId);
    Task<IEnumerable<ReportMetadataDto>> GetReportHistoryAsync();
}

public class ReportService(AppDbContext db) : IReportService
{
    // Cache the font bytes so we only download once per app lifetime
    private static byte[]? _notoSansEthiopicBytes;
    private static readonly SemaphoreSlim _fontLock = new(1, 1);
    private static bool _fontRegistered;

    private static async Task EnsureFontRegisteredAsync()
    {
        if (_fontRegistered) return;

        await _fontLock.WaitAsync();
        try
        {
            if (_fontRegistered) return;

            // Try to load Noto Sans Ethiopic from the bundled Fonts folder first
            var fontsDir = Path.Combine(AppContext.BaseDirectory, "Fonts");
            var localFontPath = Path.Combine(fontsDir, "NotoSansEthiopic-Regular.ttf");

            if (File.Exists(localFontPath))
            {
                _notoSansEthiopicBytes = await File.ReadAllBytesAsync(localFontPath);
            }
            else
            {
                // Download from Google Fonts CDN
                using var http = new HttpClient();
                _notoSansEthiopicBytes = await http.GetByteArrayAsync(
                    "https://github.com/google/fonts/raw/main/ofl/notosansethiopic/NotoSansEthiopic%5Bwdth%2Cwght%5D.ttf"
                );

                // Save locally for future use
                Directory.CreateDirectory(fontsDir);
                await File.WriteAllBytesAsync(localFontPath, _notoSansEthiopicBytes);
            }

            using var stream = new MemoryStream(_notoSansEthiopicBytes);
            FontManager.RegisterFontWithCustomName("NotoEthiopic", stream);

            _fontRegistered = true;
        }
        finally
        {
            _fontLock.Release();
        }
    }

    public async Task<(byte[] Data, string FileName, string ContentType)> GenerateReportAsync(ReportRequest request, Guid adminId)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        // Make sure our Ethiopic font is registered before generating any PDF
        await EnsureFontRegisteredAsync();

        return request.ReportType switch
        {
            "DonorDirectory" => await GenerateDonorDirectoryAsync(request, adminId),
            "DonationHistory" => await GenerateDonationHistoryAsync(request, adminId),
            "SmsCampaigns" => await GenerateSmsCampaignsAsync(request, adminId),
            _ => throw new ArgumentException($"Unknown report type: {request.ReportType}")
        };
    }

    public async Task<IEnumerable<ReportMetadataDto>> GetReportHistoryAsync()
    {
        return await db.ReportsMetadata
            .OrderByDescending(r => r.GeneratedAt)
            .Take(50)
            .Select(r => new ReportMetadataDto(r.Id, r.ReportType, r.FileName, r.FileFormat, r.DateFrom, r.DateTo, r.RecordCount, r.GeneratedAt))
            .ToListAsync();
    }

    // ── Shared styling helpers ──────────────────────────────────────────

    private static void ConfigurePage(PageDescriptor page, string title, bool landscape = false)
    {
        page.Size(landscape ? PageSizes.A4.Landscape() : PageSizes.A4);
        page.Margin(30);
        page.DefaultTextStyle(x => x
            .FontFamily("NotoEthiopic")
            .Fallback(f => f.FontFamily("Arial"))
            .FontSize(9));
        page.Header().PaddingBottom(10).Text(title).FontColor("#8b0000").FontSize(16).Bold();
    }

    private static void DataCell(TableDescriptor table, string text, bool shaded)
    {
        var cell = table.Cell()
            .Border(0.5f).BorderColor(Colors.Grey.Lighten2);

        if (shaded)
            cell = cell.Background("#f5f5f5");

        cell.Padding(5).Text(text ?? string.Empty).FontSize(9);
    }

    // ── Donor Directory ─────────────────────────────────────────────────

    private async Task<(byte[], string, string)> GenerateDonorDirectoryAsync(ReportRequest request, Guid adminId)
    {
        var donors = await db.Donors.OrderBy(d => d.FullName).ToListAsync();
        var ext = request.Format.ToLower() == "pdf" ? "pdf" : "csv";
        var fileName = $"donor_directory_{DateTime.UtcNow:yyyyMMdd}.{ext}";

        if (ext == "csv")
        {
            using var ms = new MemoryStream();
            using var writer = new StreamWriter(ms, new UTF8Encoding(true));
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            csv.WriteRecords(donors.Select(d => new
            {
                d.FullName,
                d.ChristianName,
                d.Phone,
                d.Email,
                d.BloodType,
                d.Gender,
                SundaySchool = d.IsSundaySchoolMember ? "Yes" : "No",
                d.IsEligible,
                d.LastDonationDate,
                d.PreviousDonationCount
            }));
            await writer.FlushAsync();
            await SaveMetadataAsync(adminId, "DonorDirectory", fileName, "CSV", request, donors.Count);
            return (ms.ToArray(), fileName, "text/csv");
        }

        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                ConfigurePage(page, "St. Amanuel Church - Donor Directory");
                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(2);
                        c.RelativeColumn(2);
                        c.RelativeColumn(1);
                        c.RelativeColumn(1);
                        c.RelativeColumn(1);
                    });
                    table.Header(h =>
                    {
                        foreach (var title in new[] { "Name", "Christian Name", "Phone", "Blood Type", "Sunday School" })
                            h.Cell().Background("#8b0000").Border(0.5f).BorderColor(Colors.White).Padding(5).Text(title).FontColor(Colors.White).Bold().FontSize(9);
                    });
                    var rowIndex = 0;
                    foreach (var d in donors)
                    {
                        var shaded = rowIndex % 2 == 1;
                        DataCell(table, d.FullName ?? string.Empty, shaded);
                        DataCell(table, d.ChristianName ?? string.Empty, shaded);
                        DataCell(table, d.Phone ?? string.Empty, shaded);
                        DataCell(table, d.BloodType ?? string.Empty, shaded);
                        DataCell(table, d.IsSundaySchoolMember ? "Yes" : "No", shaded);
                        rowIndex++;
                    }
                });
            });
        }).GeneratePdf();

        await SaveMetadataAsync(adminId, "DonorDirectory", fileName, "PDF", request, donors.Count);
        return (pdf, fileName, "application/pdf");
    }

    // ── Donation History ────────────────────────────────────────────────

    private async Task<(byte[], string, string)> GenerateDonationHistoryAsync(ReportRequest request, Guid adminId)
    {
        var query = db.Donations.Include(d => d.Donor).Include(d => d.HospitalPartner).AsQueryable();

        if (request.DateFrom.HasValue)
            query = query.Where(d => d.DonationDate >= request.DateFrom.Value);
        if (request.DateTo.HasValue)
            query = query.Where(d => d.DonationDate <= request.DateTo.Value);

        var donations = await query.OrderByDescending(d => d.DonationDate).ToListAsync();
        var ext = request.Format.ToLower() == "pdf" ? "pdf" : "csv";
        var fileName = $"donation_history_{DateTime.UtcNow:yyyyMMdd}.{ext}";

        if (ext == "csv")
        {
            using var ms = new MemoryStream();
            using var writer = new StreamWriter(ms, new UTF8Encoding(true));
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            csv.WriteRecords(donations.Select(d => new
            {
                DonorName = d.Donor.FullName,
                d.DonationDate,
                Hospital = d.HospitalPartner?.Name ?? "",
                d.VerifiedBy,
                d.Notes
            }));
            await writer.FlushAsync();
            await SaveMetadataAsync(adminId, "DonationHistory", fileName, "CSV", request, donations.Count);
            return (ms.ToArray(), fileName, "text/csv");
        }

        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                ConfigurePage(page, "St. Amanuel Church - Donation History", landscape: true);
                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(c => { c.RelativeColumn(2); c.RelativeColumn(1); c.RelativeColumn(2); c.RelativeColumn(2); });
                    table.Header(h =>
                    {
                        foreach (var title in new[] { "Donor", "Date", "Hospital", "Notes" })
                            h.Cell().Background("#8b0000").Border(0.5f).BorderColor(Colors.White).Padding(5).Text(title).FontColor(Colors.White).Bold().FontSize(9);
                    });
                    var rowIndex = 0;
                    foreach (var d in donations)
                    {
                        var shaded = rowIndex % 2 == 1;
                        DataCell(table, d.Donor.FullName ?? string.Empty, shaded);
                        DataCell(table, d.DonationDate.ToString("MM/dd/yyyy"), shaded);
                        DataCell(table, d.HospitalPartner?.Name ?? "-", shaded);
                        DataCell(table, d.Notes ?? "-", shaded);
                        rowIndex++;
                    }
                });
            });
        }).GeneratePdf();

        await SaveMetadataAsync(adminId, "DonationHistory", fileName, "PDF", request, donations.Count);
        return (pdf, fileName, "application/pdf");
    }

    // ── SMS Campaigns ───────────────────────────────────────────────────

    private async Task<(byte[], string, string)> GenerateSmsCampaignsAsync(ReportRequest request, Guid adminId)
    {
        var query = db.SmsLogs.AsQueryable();
        if (request.DateFrom.HasValue)
            query = query.Where(s => DateOnly.FromDateTime(s.SentAt) >= request.DateFrom.Value);
        if (request.DateTo.HasValue)
            query = query.Where(s => DateOnly.FromDateTime(s.SentAt) <= request.DateTo.Value);

        var logs = await query.OrderByDescending(s => s.SentAt).ToListAsync();
        var ext = request.Format.ToLower() == "pdf" ? "pdf" : "csv";
        var fileName = $"sms_campaigns_{DateTime.UtcNow:yyyyMMdd}.{ext}";

        if (ext == "csv")
        {
            using var ms = new MemoryStream();
            using var writer = new StreamWriter(ms, new UTF8Encoding(true));
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            csv.WriteRecords(logs.Select(s => new
            {
                s.RecipientType,
                s.RecipientCount,
                s.MessageContent,
                s.Status,
                s.DeliveryStatus,
                SentAt = s.SentAt.ToString("yyyy-MM-dd HH:mm")
            }));
            await writer.FlushAsync();
            await SaveMetadataAsync(adminId, "SmsCampaigns", fileName, "CSV", request, logs.Count);
            return (ms.ToArray(), fileName, "text/csv");
        }

        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                ConfigurePage(page, "St. Amanuel Church - SMS Campaigns");
                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(c => { c.RelativeColumn(1); c.RelativeColumn(1); c.RelativeColumn(3); c.RelativeColumn(1); });
                    table.Header(h =>
                    {
                        foreach (var title in new[] { "Type", "Count", "Message", "Status" })
                            h.Cell().Background("#8b0000").Border(0.5f).BorderColor(Colors.White).Padding(5).Text(title).FontColor(Colors.White).Bold().FontSize(9);
                    });
                    var rowIndex = 0;
                    foreach (var s in logs)
                    {
                        var shaded = rowIndex % 2 == 1;
                        DataCell(table, s.RecipientType ?? string.Empty, shaded);
                        DataCell(table, s.RecipientCount.ToString(), shaded);
                        DataCell(table, s.MessageContent.Length > 80 ? s.MessageContent[..80] + "..." : s.MessageContent, shaded);
                        DataCell(table, s.Status ?? string.Empty, shaded);
                        rowIndex++;
                    }
                });
            });
        }).GeneratePdf();

        await SaveMetadataAsync(adminId, "SmsCampaigns", fileName, "PDF", request, logs.Count);
        return (pdf, fileName, "application/pdf");
    }

    private async Task SaveMetadataAsync(Guid adminId, string reportType, string fileName, string format, ReportRequest request, int count)
    {
        db.ReportsMetadata.Add(new ReportMetadata
        {
            AdminId = adminId,
            ReportType = reportType,
            FileName = fileName,
            FileFormat = format,
            DateFrom = request.DateFrom,
            DateTo = request.DateTo,
            RecordCount = count
        });
        await db.SaveChangesAsync();
    }
}
