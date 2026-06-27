using Microsoft.EntityFrameworkCore;
using StAmanuelBloodDonation.Api.Data;
using StAmanuelBloodDonation.Api.Models;

namespace StAmanuelBloodDonation.Api.Services;

public interface IHospitalService
{
    Task<IEnumerable<HospitalDto>> GetAllAsync(bool activeOnly = true);
    Task<HospitalDto?> GetByIdAsync(Guid id);
    Task<HospitalDto> CreateAsync(CreateHospitalRequest request, Guid? adminId, string? ip);
    Task<HospitalDto?> UpdateAsync(Guid id, UpdateHospitalRequest request, Guid? adminId, string? ip);
    Task<bool> DeleteAsync(Guid id, Guid? adminId, string? ip);
}

public class HospitalService(AppDbContext db, IAuditService audit) : IHospitalService
{
    public async Task<IEnumerable<HospitalDto>> GetAllAsync(bool activeOnly = true)
    {
        var query = db.HospitalPartners.AsQueryable();
        if (activeOnly) query = query.Where(h => h.IsActive);

        return await query
            .OrderBy(h => h.Name)
            .Select(h => new HospitalDto(h.Id, h.Name, h.ContactPerson, h.Phone, h.Email, h.Notes, h.IsActive))
            .ToListAsync();
    }

    public async Task<HospitalDto?> GetByIdAsync(Guid id)
    {
        var h = await db.HospitalPartners.FindAsync(id);
        return h == null ? null : Map(h);
    }

    public async Task<HospitalDto> CreateAsync(CreateHospitalRequest request, Guid? adminId, string? ip)
    {
        var hospital = new HospitalPartner
        {
            Name = request.Name.Trim(),
            ContactPerson = request.ContactPerson?.Trim(),
            Phone = request.Phone?.Trim(),
            Email = request.Email?.Trim(),
            Notes = request.Notes
        };

        db.HospitalPartners.Add(hospital);
        await db.SaveChangesAsync();
        await audit.LogAsync(adminId, "Create", "HospitalPartner", hospital.Id, $"Created hospital {hospital.Name}", ip);
        return Map(hospital);
    }

    public async Task<HospitalDto?> UpdateAsync(Guid id, UpdateHospitalRequest request, Guid? adminId, string? ip)
    {
        var hospital = await db.HospitalPartners.FindAsync(id);
        if (hospital == null) return null;

        hospital.Name = request.Name.Trim();
        hospital.ContactPerson = request.ContactPerson?.Trim();
        hospital.Phone = request.Phone?.Trim();
        hospital.Email = request.Email?.Trim();
        hospital.Notes = request.Notes;
        hospital.IsActive = request.IsActive;
        hospital.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        await audit.LogAsync(adminId, "Update", "HospitalPartner", hospital.Id, $"Updated hospital {hospital.Name}", ip);
        return Map(hospital);
    }

    public async Task<bool> DeleteAsync(Guid id, Guid? adminId, string? ip)
    {
        var hospital = await db.HospitalPartners.FindAsync(id);
        if (hospital == null) return false;

        hospital.IsActive = false;
        hospital.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await audit.LogAsync(adminId, "Delete", "HospitalPartner", id, $"Deactivated hospital {hospital.Name}", ip);
        return true;
    }

    private static HospitalDto Map(HospitalPartner h) =>
        new(h.Id, h.Name, h.ContactPerson, h.Phone, h.Email, h.Notes, h.IsActive);
}
