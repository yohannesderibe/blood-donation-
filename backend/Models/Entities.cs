namespace StAmanuelBloodDonation.Api.Models;

public class Admin
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = "Admin";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class Donor
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string ChristianName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string BloodType { get; set; } = "Unknown";
    public string? Gender { get; set; }
    public bool IsSundaySchoolMember { get; set; }
    public string? PasswordHash { get; set; }
    public bool IsFirstTimeDonor { get; set; } = true;
    public DateOnly? LastDonationDate { get; set; }
    public int PreviousDonationCount { get; set; }
    public string? HowHeardAboutUs { get; set; }
    public bool IsEligible { get; set; } = true;
    public string? EligibilityNotes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Donation> Donations { get; set; } = [];
}

public class Donation
{
    public Guid Id { get; set; }
    public Guid DonorId { get; set; }
    public Guid? HospitalPartnerId { get; set; }
    public DateOnly DonationDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public string? VerifiedBy { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Donor Donor { get; set; } = null!;
    public HospitalPartner? HospitalPartner { get; set; }
}

public class HospitalPartner
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Donation> Donations { get; set; } = [];
}

public class SmsLog
{
    public Guid Id { get; set; }
    public Guid? AdminId { get; set; }
    public string RecipientType { get; set; } = string.Empty;
    public int RecipientCount { get; set; }
    public string MessageContent { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public string? DeliveryStatus { get; set; }
    public string? AfroMessageId { get; set; }
    public decimal? CostEtb { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Admin? Admin { get; set; }
    public ICollection<SmsRecipient> Recipients { get; set; } = [];
}

public class SmsRecipient
{
    public Guid Id { get; set; }
    public Guid SmsLogId { get; set; }
    public Guid? DonorId { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string DeliveryStatus { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public SmsLog SmsLog { get; set; } = null!;
    public Donor? Donor { get; set; }
}

public class AuditLog
{
    public Guid Id { get; set; }
    public Guid? AdminId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Admin? Admin { get; set; }
}

public class ReportMetadata
{
    public Guid Id { get; set; }
    public Guid? AdminId { get; set; }
    public string ReportType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FileFormat { get; set; } = string.Empty;
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
    public int RecordCount { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    public Admin? Admin { get; set; }
}

public class SystemNotification
{
    public Guid Id { get; set; }
    public string TitleEn { get; set; } = string.Empty;
    public string? TitleAm { get; set; }
    public string MessageEn { get; set; } = string.Empty;
    public string? MessageAm { get; set; }
    public string NotificationType { get; set; } = "Info";
    public bool IsActive { get; set; } = true;
    public DateOnly? EventDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
