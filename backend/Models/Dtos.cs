namespace StAmanuelBloodDonation.Api.Models;

public record LoginRequest(string Username, string Password);
public record LoginResponse(string Token, string Username, string FullName, string Role, DateTime ExpiresAt);

public record DashboardSummary(int TotalDonors, int EligibleDonors, int NonEligibleDonors);
public record BloodTypeDistribution(string BloodType, int Count);
public record RecentDonorDto(Guid Id, string FullName, string ChristianName, string Phone, string BloodType, DateTime CreatedAt);
public record NotificationDto(Guid Id, string TitleEn, string? TitleAm, string MessageEn, string? MessageAm, string NotificationType, DateOnly? EventDate);

public record CreateDonorRequest(
    string FullName,
    string ChristianName,
    string Phone,
    string? Email,
    string BloodType,
    string? Gender,
    bool IsSundaySchoolMember,
    bool IsFirstTimeDonor,
    DateOnly? LastDonationDate,
    int? PreviousDonationCount,
    string? HowHeardAboutUs
);

public record UpdateDonorRequest(
    string FullName,
    string ChristianName,
    string Phone,
    string? Email,
    string BloodType,
    string? Gender,
    bool IsSundaySchoolMember,
    bool IsFirstTimeDonor,
    DateOnly? LastDonationDate,
    int? PreviousDonationCount,
    string? HowHeardAboutUs
);

public record DonorListItemDto(
    Guid Id,
    string FullName,
    string ChristianName,
    string Phone,
    string BloodType,
    DateOnly? LastDonationDate,
    bool IsEligible,
    bool IsSundaySchoolMember,
    DateTime CreatedAt
);

public record DonorDetailDto(
    Guid Id,
    string FullName,
    string ChristianName,
    string Phone,
    string? Email,
    string BloodType,
    string? Gender,
    bool IsSundaySchoolMember,
    bool IsFirstTimeDonor,
    DateOnly? LastDonationDate,
    int PreviousDonationCount,
    string? HowHeardAboutUs,
    bool IsEligible,
    string? EligibilityNotes,
    DateTime CreatedAt,
    IEnumerable<DonationDto> Donations
);

public record DonationDto(Guid Id, DateOnly DonationDate, string? HospitalName, string? VerifiedBy, string? Notes);

public record DonorFilterParams(
    string? Search,
    string? BloodType,
    bool? IsEligible,
    bool? IsSundaySchoolMember,
    DateOnly? LastDonationFrom,
    DateOnly? LastDonationTo,
    int Page = 1,
    int PageSize = 10
);

public record PagedResult<T>(IEnumerable<T> Items, int TotalCount, int Page, int PageSize, int TotalPages);

public record SendSmsRequest(
    string RecipientType,
    string Message,
    List<Guid>? DonorIds,
    string? CustomGroupName
);

public record SmsBalanceDto(decimal BalanceEtb, int EstimatedMessages, decimal CostPerMessage);
public record SmsLogDto(Guid Id, string RecipientType, int RecipientCount, string MessageContent, string Status, string? DeliveryStatus, string? ErrorMessage, DateTime SentAt);

public record CreateHospitalRequest(string Name, string? ContactPerson, string? Phone, string? Email, string? Notes);
public record UpdateHospitalRequest(string Name, string? ContactPerson, string? Phone, string? Email, string? Notes, bool IsActive);
public record HospitalDto(Guid Id, string Name, string? ContactPerson, string? Phone, string? Email, string? Notes, bool IsActive);

public record ReportRequest(string ReportType, DateOnly? DateFrom, DateOnly? DateTo, string Format);
public record ReportMetadataDto(Guid Id, string ReportType, string FileName, string FileFormat, DateOnly? DateFrom, DateOnly? DateTo, int RecordCount, DateTime GeneratedAt);

public record MarkDonatedRequest(Guid? HospitalPartnerId, string? Notes);
public record ApiError(string MessageEn, string MessageAm, string? Details = null);
