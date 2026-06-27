using Microsoft.EntityFrameworkCore;
using StAmanuelBloodDonation.Api.Data;
using StAmanuelBloodDonation.Api.Models;

namespace StAmanuelBloodDonation.Api.Services;

public interface IDonorService
{
    Task<PagedResult<DonorListItemDto>> GetDonorsAsync(DonorFilterParams filter);
    Task<DonorDetailDto?> GetDonorAsync(Guid id);
    Task<DonorDetailDto> CreateDonorAsync(CreateDonorRequest request, Guid? adminId, string? ip);
    Task<DonorDetailDto?> UpdateDonorAsync(Guid id, UpdateDonorRequest request, Guid? adminId, string? ip);
    Task<bool> DeleteDonorAsync(Guid id, Guid? adminId, string? ip);
    Task<DonorDetailDto?> MarkDonatedTodayAsync(Guid id, MarkDonatedRequest request, Guid? adminId, string? ip);
}

public class DonorService(AppDbContext db, IAuditService audit) : IDonorService
{
    public async Task<PagedResult<DonorListItemDto>> GetDonorsAsync(DonorFilterParams filter)
    {
        var query = db.Donors.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.ToLower();
            query = query.Where(d =>
                d.FullName.ToLower().Contains(search) ||
                d.ChristianName.ToLower().Contains(search) ||
                d.Phone.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(filter.BloodType) && filter.BloodType != "All")
            query = query.Where(d => d.BloodType == filter.BloodType);

        if (filter.IsEligible.HasValue)
            query = query.Where(d => d.IsEligible == filter.IsEligible.Value);

        if (filter.IsSundaySchoolMember.HasValue)
            query = query.Where(d => d.IsSundaySchoolMember == filter.IsSundaySchoolMember.Value);

        if (filter.LastDonationFrom.HasValue)
            query = query.Where(d => d.LastDonationDate >= filter.LastDonationFrom.Value);

        if (filter.LastDonationTo.HasValue)
            query = query.Where(d => d.LastDonationDate <= filter.LastDonationTo.Value);

        var total = await query.CountAsync();
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);
        var page = Math.Max(filter.Page, 1);
        var totalPages = (int)Math.Ceiling(total / (double)pageSize);

        var items = await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(d => new DonorListItemDto(
                d.Id, d.FullName, d.ChristianName, d.Phone, d.BloodType,
                d.LastDonationDate, d.IsEligible, d.IsSundaySchoolMember, d.CreatedAt))
            .ToListAsync();

        return new PagedResult<DonorListItemDto>(items, total, page, pageSize, totalPages);
    }

    public async Task<DonorDetailDto?> GetDonorAsync(Guid id)
    {
        var donor = await db.Donors
            .Include(d => d.Donations).ThenInclude(dn => dn.HospitalPartner)
            .FirstOrDefaultAsync(d => d.Id == id);

        return donor == null ? null : MapToDetail(donor);
    }

    public async Task<DonorDetailDto> CreateDonorAsync(CreateDonorRequest request, Guid? adminId, string? ip)
    {
        if (await db.Donors.AnyAsync(d => d.Phone == request.Phone))
            throw new InvalidOperationException("Phone number already registered");

        var isEligible = DonorEligibility.CalculateEligibility(request.LastDonationDate);

        var donor = new Donor
        {
            FullName = request.FullName.Trim(),
            ChristianName = request.ChristianName.Trim(),
            Phone = request.Phone.Trim(),
            Email = request.Email?.Trim(),
            BloodType = request.BloodType,
            Gender = request.Gender,
            IsSundaySchoolMember = request.IsSundaySchoolMember,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            IsFirstTimeDonor = request.IsFirstTimeDonor,
            LastDonationDate = request.IsFirstTimeDonor ? null : request.LastDonationDate,
            PreviousDonationCount = request.IsFirstTimeDonor ? 0 : (request.PreviousDonationCount ?? 0),
            HowHeardAboutUs = request.HowHeardAboutUs,
            IsEligible = isEligible,
            EligibilityNotes = DonorEligibility.GetEligibilityNotes(isEligible, request.LastDonationDate)
        };

        db.Donors.Add(donor);
        await db.SaveChangesAsync();
        await audit.LogAsync(adminId, "Create", "Donor", donor.Id, $"Created donor {donor.FullName}", ip);

        return MapToDetail(donor);
    }

    public async Task<DonorDetailDto?> UpdateDonorAsync(Guid id, UpdateDonorRequest request, Guid? adminId, string? ip)
    {
        var donor = await db.Donors.FindAsync(id);
        if (donor == null) return null;

        if (await db.Donors.AnyAsync(d => d.Phone == request.Phone && d.Id != id))
            throw new InvalidOperationException("Phone number already registered");

        donor.FullName = request.FullName.Trim();
        donor.ChristianName = request.ChristianName.Trim();
        donor.Phone = request.Phone.Trim();
        donor.Email = request.Email?.Trim();
        donor.BloodType = request.BloodType;
        donor.Gender = request.Gender;
        donor.IsSundaySchoolMember = request.IsSundaySchoolMember;
        donor.IsFirstTimeDonor = request.IsFirstTimeDonor;
        donor.LastDonationDate = request.IsFirstTimeDonor ? null : request.LastDonationDate;
        donor.PreviousDonationCount = request.IsFirstTimeDonor ? 0 : (request.PreviousDonationCount ?? donor.PreviousDonationCount);
        donor.HowHeardAboutUs = request.HowHeardAboutUs;
        donor.IsEligible = DonorEligibility.CalculateEligibility(donor.LastDonationDate);
        donor.EligibilityNotes = DonorEligibility.GetEligibilityNotes(donor.IsEligible, donor.LastDonationDate);
        donor.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        await audit.LogAsync(adminId, "Update", "Donor", donor.Id, $"Updated donor {donor.FullName}", ip);

        return await GetDonorAsync(id);
    }

    public async Task<bool> DeleteDonorAsync(Guid id, Guid? adminId, string? ip)
    {
        var donor = await db.Donors.FindAsync(id);
        if (donor == null) return false;

        var name = donor.FullName;
        db.Donors.Remove(donor);
        await db.SaveChangesAsync();
        await audit.LogAsync(adminId, "Delete", "Donor", id, $"Deleted donor {name}", ip);
        return true;
    }

    public async Task<DonorDetailDto?> MarkDonatedTodayAsync(Guid id, MarkDonatedRequest request, Guid? adminId, string? ip)
    {
        var donor = await db.Donors.FindAsync(id);
        if (donor == null) return null;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var donation = new Donation
        {
            DonorId = id,
            HospitalPartnerId = request.HospitalPartnerId,
            DonationDate = today,
            Notes = request.Notes
        };

        db.Donations.Add(donation);
        donor.LastDonationDate = today;
        donor.PreviousDonationCount++;
        donor.IsFirstTimeDonor = false;
        donor.IsEligible = false;
        donor.EligibilityNotes = DonorEligibility.GetEligibilityNotes(false, today);
        donor.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        await audit.LogAsync(adminId, "MarkDonated", "Donor", id, $"Marked {donor.FullName} as donated today", ip);

        return await GetDonorAsync(id);
    }

    private static DonorDetailDto MapToDetail(Donor donor) => new(
        donor.Id, donor.FullName, donor.ChristianName, donor.Phone, donor.Email,
        donor.BloodType, donor.Gender, donor.IsSundaySchoolMember, donor.IsFirstTimeDonor,
        donor.LastDonationDate, donor.PreviousDonationCount, donor.HowHeardAboutUs,
        donor.IsEligible, donor.EligibilityNotes, donor.CreatedAt,
        donor.Donations.OrderByDescending(d => d.DonationDate).Select(d => new DonationDto(
            d.Id, d.DonationDate, d.HospitalPartner?.Name, d.VerifiedBy, d.Notes)));
}
