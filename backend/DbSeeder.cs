using Microsoft.EntityFrameworkCore;
using StAmanuelBloodDonation.Api.Data;
using StAmanuelBloodDonation.Api.Models;

namespace StAmanuelBloodDonation.Api;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (!await db.Admins.AnyAsync())
        {
            db.Admins.Add(new Admin
            {
                Username = "admin",
                Email = "admin@stamanuel.org",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                FullName = "System Administrator",
                Role = "Admin"
            });
        }

        if (!await db.SystemNotifications.AnyAsync())
        {
            db.SystemNotifications.AddRange(
                new SystemNotification
                {
                    TitleEn = "Upcoming Blood Drive",
                    TitleAm = "የሚመጣ የደም ስጦታ",
                    MessageEn = "Annual blood donation drive scheduled for next month at St. Amanuel Church.",
                    MessageAm = "በሚቀጥለው ወር በቅዱስ አmanuel ቤተክርስቲያን የዓመታዊ የደም ስጦታ ይplanned ነው.",
                    NotificationType = "Drive",
                    EventDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30))
                },
                new SystemNotification
                {
                    TitleEn = "Hospital Request",
                    TitleAm = "የሆስፒታል ጥያቄ",
                    MessageEn = "Black Lion Hospital requests O+ donors urgently.",
                    MessageAm = "ብላክ ላዮን ሆስፒታል O+ ደም Contributors በአስቸኳይ ይፈልጋል.",
                    NotificationType = "Request",
                    EventDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7))
                });
        }

        await db.SaveChangesAsync();
    }
}
