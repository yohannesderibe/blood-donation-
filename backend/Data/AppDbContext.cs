using Microsoft.EntityFrameworkCore;
using StAmanuelBloodDonation.Api.Models;

namespace StAmanuelBloodDonation.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Admin> Admins => Set<Admin>();
    public DbSet<Donor> Donors => Set<Donor>();
    public DbSet<Donation> Donations => Set<Donation>();
    public DbSet<HospitalPartner> HospitalPartners => Set<HospitalPartner>();
    public DbSet<SmsLog> SmsLogs => Set<SmsLog>();
    public DbSet<SmsRecipient> SmsRecipients => Set<SmsRecipient>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ReportMetadata> ReportsMetadata => Set<ReportMetadata>();
    public DbSet<SystemNotification> SystemNotifications => Set<SystemNotification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Admin>(e =>
        {
            e.ToTable("admins");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Username).HasColumnName("username").HasMaxLength(100);
            e.Property(x => x.Email).HasColumnName("email").HasMaxLength(255);
            e.Property(x => x.PasswordHash).HasColumnName("password_hash");
            e.Property(x => x.FullName).HasColumnName("full_name").HasMaxLength(200);
            e.Property(x => x.Role).HasColumnName("role").HasMaxLength(50);
            e.Property(x => x.IsActive).HasColumnName("is_active");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<Donor>(e =>
        {
            e.ToTable("donors");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.FullName).HasColumnName("full_name").HasMaxLength(200);
            e.Property(x => x.ChristianName).HasColumnName("christian_name").HasMaxLength(200);
            e.Property(x => x.Phone).HasColumnName("phone").HasMaxLength(50);
            e.Property(x => x.Email).HasColumnName("email").HasMaxLength(255);
            e.Property(x => x.BloodType).HasColumnName("blood_type").HasMaxLength(10);
            e.Property(x => x.Gender).HasColumnName("gender").HasMaxLength(20);
            e.Property(x => x.IsSundaySchoolMember).HasColumnName("is_sunday_school_member");
            e.Property(x => x.PasswordHash).HasColumnName("password_hash");
            e.Property(x => x.IsFirstTimeDonor).HasColumnName("is_first_time_donor");
            e.Property(x => x.LastDonationDate).HasColumnName("last_donation_date");
            e.Property(x => x.PreviousDonationCount).HasColumnName("previous_donation_count");
            e.Property(x => x.HowHeardAboutUs).HasColumnName("how_heard_about_us").HasMaxLength(500);
            e.Property(x => x.IsEligible).HasColumnName("is_eligible");
            e.Property(x => x.EligibilityNotes).HasColumnName("eligibility_notes");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<Donation>(e =>
        {
            e.ToTable("donations");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.DonorId).HasColumnName("donor_id");
            e.Property(x => x.HospitalPartnerId).HasColumnName("hospital_partner_id");
            e.Property(x => x.DonationDate).HasColumnName("donation_date");
            e.Property(x => x.VerifiedBy).HasColumnName("verified_by").HasMaxLength(200);
            e.Property(x => x.Notes).HasColumnName("notes");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.HasOne(x => x.Donor).WithMany(d => d.Donations).HasForeignKey(x => x.DonorId);
            e.HasOne(x => x.HospitalPartner).WithMany(h => h.Donations).HasForeignKey(x => x.HospitalPartnerId);
        });

        modelBuilder.Entity<HospitalPartner>(e =>
        {
            e.ToTable("hospital_partners");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Name).HasColumnName("name").HasMaxLength(255);
            e.Property(x => x.ContactPerson).HasColumnName("contact_person").HasMaxLength(200);
            e.Property(x => x.Phone).HasColumnName("phone").HasMaxLength(50);
            e.Property(x => x.Email).HasColumnName("email").HasMaxLength(255);
            e.Property(x => x.Notes).HasColumnName("notes");
            e.Property(x => x.IsActive).HasColumnName("is_active");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<SmsLog>(e =>
        {
            e.ToTable("sms_logs");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.AdminId).HasColumnName("admin_id");
            e.Property(x => x.RecipientType).HasColumnName("recipient_type").HasMaxLength(50);
            e.Property(x => x.RecipientCount).HasColumnName("recipient_count");
            e.Property(x => x.MessageContent).HasColumnName("message_content");
            e.Property(x => x.Status).HasColumnName("status").HasMaxLength(50);
            e.Property(x => x.DeliveryStatus).HasColumnName("delivery_status");
            e.Property(x => x.AfroMessageId).HasColumnName("afro_message_id").HasMaxLength(255);
            e.Property(x => x.CostEtb).HasColumnName("cost_etb").HasColumnType("decimal(10,4)");
            e.Property(x => x.ErrorMessage).HasColumnName("error_message");
            e.Property(x => x.RetryCount).HasColumnName("retry_count");
            e.Property(x => x.SentAt).HasColumnName("sent_at");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<SmsRecipient>(e =>
        {
            e.ToTable("sms_recipients");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.SmsLogId).HasColumnName("sms_log_id");
            e.Property(x => x.DonorId).HasColumnName("donor_id");
            e.Property(x => x.Phone).HasColumnName("phone").HasMaxLength(50);
            e.Property(x => x.DeliveryStatus).HasColumnName("delivery_status").HasMaxLength(50);
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<AuditLog>(e =>
        {
            e.ToTable("audit_logs");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.AdminId).HasColumnName("admin_id");
            e.Property(x => x.Action).HasColumnName("action").HasMaxLength(100);
            e.Property(x => x.EntityType).HasColumnName("entity_type").HasMaxLength(100);
            e.Property(x => x.EntityId).HasColumnName("entity_id");
            e.Property(x => x.Details).HasColumnName("details");
            e.Property(x => x.IpAddress).HasColumnName("ip_address").HasMaxLength(50);
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<ReportMetadata>(e =>
        {
            e.ToTable("reports_metadata");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.AdminId).HasColumnName("admin_id");
            e.Property(x => x.ReportType).HasColumnName("report_type").HasMaxLength(100);
            e.Property(x => x.FileName).HasColumnName("file_name").HasMaxLength(255);
            e.Property(x => x.FileFormat).HasColumnName("file_format").HasMaxLength(20);
            e.Property(x => x.DateFrom).HasColumnName("date_from");
            e.Property(x => x.DateTo).HasColumnName("date_to");
            e.Property(x => x.RecordCount).HasColumnName("record_count");
            e.Property(x => x.GeneratedAt).HasColumnName("generated_at");
        });

        modelBuilder.Entity<SystemNotification>(e =>
        {
            e.ToTable("system_notifications");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.TitleEn).HasColumnName("title_en").HasMaxLength(255);
            e.Property(x => x.TitleAm).HasColumnName("title_am").HasMaxLength(255);
            e.Property(x => x.MessageEn).HasColumnName("message_en");
            e.Property(x => x.MessageAm).HasColumnName("message_am");
            e.Property(x => x.NotificationType).HasColumnName("notification_type").HasMaxLength(50);
            e.Property(x => x.IsActive).HasColumnName("is_active");
            e.Property(x => x.EventDate).HasColumnName("event_date");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
        });
    }
}
