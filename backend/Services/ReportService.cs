using System.Globalization;
using System.Text;
using CsvHelper;
using Microsoft.EntityFrameworkCore;
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
    public async Task<(byte[] Data, string FileName, string ContentType)> GenerateReportAsync(ReportRequest request, Guid adminId)
    {
        QuestPDF.Settings.License = LicenseType.Community;

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
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontFamily("Nyala").Fallback(f => f.FontFamily("Arial")));
                page.Header().PaddingBottom(10).Text("St. Amanuel Church - Donor Directory").FontColor("#8b0000").FontSize(20).Bold();
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
                        h.Cell().Background("#8b0000").Padding(5).Text("Name").FontColor(Colors.White).Bold();
                        h.Cell().Background("#8b0000").Padding(5).Text("Christian Name").FontColor(Colors.White).Bold();
                        h.Cell().Background("#8b0000").Padding(5).Text("Phone").FontColor(Colors.White).Bold();
                        h.Cell().Background("#8b0000").Padding(5).Text("Blood Type").FontColor(Colors.White).Bold();
                        h.Cell().Background("#8b0000").Padding(5).Text("Sunday School").FontColor(Colors.White).Bold();
                    });
                    foreach (var d in donors)
                    {
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(d.FullName ?? string.Empty);
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(d.ChristianName ?? string.Empty);
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(d.Phone ?? string.Empty);
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(d.BloodType ?? string.Empty);
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(d.IsSundaySchoolMember ? "Yes" : "No");
                    }
                });
            });
        }).GeneratePdf();

        await SaveMetadataAsync(adminId, "DonorDirectory", fileName, "PDF", request, donors.Count);
        return (pdf, fileName, "application/pdf");
    }

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
                page.Size(PageSizes.A4.Landscape());
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontFamily("Nyala").Fallback(f => f.FontFamily("Arial")));
                page.Header().PaddingBottom(10).Text("St. Amanuel Church - Donation History").FontColor("#8b0000").FontSize(20).Bold();
                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(c => { c.RelativeColumn(2); c.RelativeColumn(1); c.RelativeColumn(2); c.RelativeColumn(2); });
                    table.Header(h =>
                    {
                        h.Cell().Background("#8b0000").Padding(5).Text("Donor").FontColor(Colors.White).Bold();
                        h.Cell().Background("#8b0000").Padding(5).Text("Date").FontColor(Colors.White).Bold();
                        h.Cell().Background("#8b0000").Padding(5).Text("Hospital").FontColor(Colors.White).Bold();
                        h.Cell().Background("#8b0000").Padding(5).Text("Notes").FontColor(Colors.White).Bold();
                    });
                    foreach (var d in donations)
                    {
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(d.Donor.FullName ?? string.Empty);
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(d.DonationDate.ToString("MM/dd/yyyy"));
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(d.HospitalPartner?.Name ?? "-");
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(d.Notes ?? "-");
                    }
                });
            });
        }).GeneratePdf();

        await SaveMetadataAsync(adminId, "DonationHistory", fileName, "PDF", request, donations.Count);
        return (pdf, fileName, "application/pdf");
    }

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
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontFamily("Nyala").Fallback(f => f.FontFamily("Arial")));
                page.Header().PaddingBottom(10).Text("St. Amanuel Church - SMS Campaigns").FontColor("#8b0000").FontSize(20).Bold();
                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(c => { c.RelativeColumn(1); c.RelativeColumn(1); c.RelativeColumn(3); c.RelativeColumn(1); });
                    table.Header(h =>
                    {
                        h.Cell().Background("#8b0000").Padding(5).Text("Type").FontColor(Colors.White).Bold();
                        h.Cell().Background("#8b0000").Padding(5).Text("Count").FontColor(Colors.White).Bold();
                        h.Cell().Background("#8b0000").Padding(5).Text("Message").FontColor(Colors.White).Bold();
                        h.Cell().Background("#8b0000").Padding(5).Text("Status").FontColor(Colors.White).Bold();
                    });
                    foreach (var s in logs)
                    {
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(s.RecipientType ?? string.Empty);
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(s.RecipientCount.ToString());
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(s.MessageContent.Length > 80 ? s.MessageContent[..80] + "..." : s.MessageContent);
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(s.Status ?? string.Empty);
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
