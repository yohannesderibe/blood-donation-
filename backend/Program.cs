using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StAmanuelBloodDonation.Api;
using StAmanuelBloodDonation.Api.Data;
using StAmanuelBloodDonation.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IDonorService, DonorService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<ISmsService, SmsService>();
builder.Services.AddScoped<IHospitalService, HospitalService>();
builder.Services.AddScoped<IReportService, ReportService>();

builder.Services.AddHttpClient("AfroMessaging");

var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:5173"];
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials());
});

// Bind to Render's dynamic PORT environment variable (falls back to 5000 locally)
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
    await DbSeeder.SeedAsync(db);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
