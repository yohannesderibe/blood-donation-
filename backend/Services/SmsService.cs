using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StAmanuelBloodDonation.Api.Data;
using StAmanuelBloodDonation.Api.Models;

namespace StAmanuelBloodDonation.Api.Services;

public interface ISmsService
{
    Task<SmsBalanceDto> GetBalanceAsync();
    Task<SmsLogDto> SendSmsAsync(SendSmsRequest request, Guid adminId, string? ip);
    Task<IEnumerable<SmsLogDto>> GetHistoryAsync(int page = 1, int pageSize = 20);
    Task RetryFailedAsync(Guid smsLogId, Guid adminId);
}

public class SmsService(AppDbContext db, IHttpClientFactory httpFactory, IConfiguration config, IAuditService audit) : ISmsService
{
    private const decimal CostPerSms = 0.1725m;
    private const decimal KnownBalance = 16.905m;

    private string ApiKey => config["AfroMessaging:ApiKey"] ?? "";
    private string IdentifierId => config["AfroMessaging:IdentifierId"] ?? "";
    private string BaseUrl => config["AfroMessaging:BaseUrl"] ?? "https://api.afromessage.com/api";

    public Task<SmsBalanceDto> GetBalanceAsync()
    {
        var estimated = (int)(KnownBalance / CostPerSms);
        return Task.FromResult(new SmsBalanceDto(KnownBalance, estimated, CostPerSms));
    }

    public async Task<SmsLogDto> SendSmsAsync(SendSmsRequest request, Guid adminId, string? ip)
    {
        var phones = await ResolveRecipientsAsync(request);
        if (phones.Count == 0)
            throw new InvalidOperationException("No recipients found");

        var smsLog = new SmsLog
        {
            AdminId = adminId,
            RecipientType = request.RecipientType,
            RecipientCount = phones.Count,
            MessageContent = request.Message,
            Status = "Pending",
            CostEtb = phones.Count * CostPerSms
        };

        db.SmsLogs.Add(smsLog);
        await db.SaveChangesAsync();

        foreach (var (phone, donorId) in phones)
        {
            db.SmsRecipients.Add(new SmsRecipient
            {
                SmsLogId = smsLog.Id,
                DonorId = donorId,
                Phone = phone
            });
        }
        await db.SaveChangesAsync();

        var success = await SendViaAfroAsync(smsLog, phones.Select(p => p.Phone).ToList());

        smsLog.Status = success ? "Sent" : "Failed";
        smsLog.DeliveryStatus = success ? "Delivered" : "Failed";
        if (!success && string.IsNullOrEmpty(smsLog.ErrorMessage)) 
        {
            smsLog.ErrorMessage = "Failed to send via Afro Messaging API";
        }
        smsLog.SentAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        await audit.LogAsync(adminId, "SendSms", "SmsLog", smsLog.Id,
            $"Sent SMS to {phones.Count} recipients", ip);

        return MapToDto(smsLog);
    }

    public async Task<IEnumerable<SmsLogDto>> GetHistoryAsync(int page = 1, int pageSize = 20)
    {
        return await db.SmsLogs
            .OrderByDescending(s => s.SentAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new SmsLogDto(s.Id, s.RecipientType, s.RecipientCount, s.MessageContent, s.Status, s.DeliveryStatus, s.ErrorMessage, s.SentAt))
            .ToListAsync();
    }

    public async Task RetryFailedAsync(Guid smsLogId, Guid adminId)
    {
        var smsLog = await db.SmsLogs
            .Include(s => s.Recipients)
            .FirstOrDefaultAsync(s => s.Id == smsLogId);

        if (smsLog == null || smsLog.Status == "Sent")
            return;

        if (smsLog.RetryCount >= 3)
            throw new InvalidOperationException("Maximum retry attempts reached");

        smsLog.RetryCount++;
        var phones = smsLog.Recipients.Select(r => r.Phone).ToList();
        var success = await SendViaAfroAsync(smsLog, phones);

        smsLog.Status = success ? "Sent" : "Failed";
        smsLog.DeliveryStatus = success ? "Delivered" : "Failed";
        smsLog.SentAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    private async Task<List<(string Phone, Guid? DonorId)>> ResolveRecipientsAsync(SendSmsRequest request)
    {
        return request.RecipientType switch
        {
            "All" => await db.Donors
                .Select(d => new ValueTuple<string, Guid?>(d.Phone, d.Id))
                .ToListAsync(),
            "Selected" when request.DonorIds?.Count > 0 => await db.Donors
                .Where(d => request.DonorIds.Contains(d.Id))
                .Select(d => new ValueTuple<string, Guid?>(d.Phone, d.Id))
                .ToListAsync(),
            "SundaySchool" => await db.Donors
                .Where(d => d.IsSundaySchoolMember)
                .Select(d => new ValueTuple<string, Guid?>(d.Phone, d.Id))
                .ToListAsync(),
            "Eligible" => await db.Donors
                .Where(d => d.IsEligible)
                .Select(d => new ValueTuple<string, Guid?>(d.Phone, d.Id))
                .ToListAsync(),
            _ => []
        };
    }

    private async Task<bool> SendViaAfroAsync(SmsLog smsLog, List<string> phones)
    {
        try
        {
            var client = httpFactory.CreateClient("AfroMessaging");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);

            var successCount = 0;
            foreach (var phone in phones)
            {
                var payload = new
                {
                    to = NormalizePhone(phone),
                    message = smsLog.MessageContent
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{BaseUrl}/send", content);
                var body = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    // AfroMessage can return HTTP 200 with acknowledge=error
                    bool apiSuccess = true;
                    try
                    {
                        using var doc = JsonDocument.Parse(body);
                        if (doc.RootElement.TryGetProperty("acknowledge", out var ack) &&
                            ack.GetString() != "success")
                        {
                            apiSuccess = false;
                            // Extract error message from response
                            if (doc.RootElement.TryGetProperty("response", out var resp) &&
                                resp.TryGetProperty("errors", out var errs) &&
                                errs.ValueKind == JsonValueKind.Array &&
                                errs.GetArrayLength() > 0)
                            {
                                smsLog.ErrorMessage = errs[0].GetString();
                            }
                            else
                            {
                                smsLog.ErrorMessage = $"API Error: {body}";
                            }
                        }
                        else if (smsLog.AfroMessageId == null &&
                                 doc.RootElement.TryGetProperty("id", out var idProp))
                        {
                            smsLog.AfroMessageId = idProp.GetString();
                        }
                    }
                    catch { /* ignore parse errors */ }

                    if (apiSuccess)
                        successCount++;
                }
                else
                {
                    smsLog.ErrorMessage = $"HTTP {response.StatusCode}: {body}";
                }
            }

            return successCount > 0;
        }
        catch (Exception ex)
        {
            smsLog.ErrorMessage = ex.Message;
            return false;
        }
    }

    private static string NormalizePhone(string phone)
    {
        phone = phone.Trim().Replace(" ", "").Replace("-", "");
        if (phone.StartsWith('0')) phone = "+251" + phone[1..];
        if (!phone.StartsWith('+')) phone = "+251" + phone;
        return phone;
    }

    private static SmsLogDto MapToDto(SmsLog s) =>
        new(s.Id, s.RecipientType, s.RecipientCount, s.MessageContent, s.Status, s.DeliveryStatus, s.ErrorMessage, s.SentAt);
}
