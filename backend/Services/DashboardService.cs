using Microsoft.EntityFrameworkCore;
using StAmanuelBloodDonation.Api.Data;
using StAmanuelBloodDonation.Api.Models;

namespace StAmanuelBloodDonation.Api.Services;

public interface IDashboardService
{
    Task<DashboardSummary> GetSummaryAsync();
    Task<IEnumerable<BloodTypeDistribution>> GetBloodTypeDistributionAsync();
    Task<IEnumerable<RecentDonorDto>> GetRecentDonorsAsync(int count);
    Task<IEnumerable<NotificationDto>> GetNotificationsAsync();
}

public class DashboardService(AppDbContext db) : IDashboardService
{
    public async Task<DashboardSummary> GetSummaryAsync()
    {
        var total = await db.Donors.CountAsync();
        var eligible = await db.Donors.CountAsync(d => d.IsEligible);
        var nonEligible = total - eligible;
        return new DashboardSummary(total, eligible, nonEligible);
    }

    public async Task<IEnumerable<BloodTypeDistribution>> GetBloodTypeDistributionAsync()
    {
        return await db.Donors
            .GroupBy(d => d.BloodType)
            .Select(g => new BloodTypeDistribution(g.Key, g.Count()))
            .OrderByDescending(x => x.Count)
            .ToListAsync();
    }

    public async Task<IEnumerable<RecentDonorDto>> GetRecentDonorsAsync(int count)
    {
        return await db.Donors
            .OrderByDescending(d => d.CreatedAt)
            .Take(count)
            .Select(d => new RecentDonorDto(d.Id, d.FullName, d.ChristianName, d.Phone, d.BloodType, d.CreatedAt))
            .ToListAsync();
    }

    public async Task<IEnumerable<NotificationDto>> GetNotificationsAsync()
    {
        return await db.SystemNotifications
            .Where(n => n.IsActive)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationDto(n.Id, n.TitleEn, n.TitleAm, n.MessageEn, n.MessageAm, n.NotificationType, n.EventDate))
            .ToListAsync();
    }
}
