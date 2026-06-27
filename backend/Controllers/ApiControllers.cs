using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StAmanuelBloodDonation.Api.Models;
using StAmanuelBloodDonation.Api.Services;

namespace StAmanuelBloodDonation.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await authService.LoginAsync(request);
        if (result == null)
            return Unauthorized(new ApiError("Invalid username or password", "የተሳሳተ የተጠቃሚ ስም ወይም የይለፍ ቃል"));

        return Ok(result);
    }
}

[ApiController]
[Route("api/dashboard")]
[Authorize(Roles = "Admin")]
public class DashboardController(IDashboardService dashboard) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary() => Ok(await dashboard.GetSummaryAsync());

    [HttpGet("blood-types")]
    public async Task<IActionResult> GetBloodTypes() => Ok(await dashboard.GetBloodTypeDistributionAsync());

    [HttpGet("recent-donors")]
    public async Task<IActionResult> GetRecentDonors([FromQuery] int count = 10) =>
        Ok(await dashboard.GetRecentDonorsAsync(count));

    [HttpGet("notifications")]
    public async Task<IActionResult> GetNotifications() => Ok(await dashboard.GetNotificationsAsync());
}

[ApiController]
[Route("api/donors")]
[Authorize(Roles = "Admin")]
public class DonorsController(IDonorService donorService) : ControllerBase
{
    private Guid? AdminId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    private string? Ip => HttpContext.Connection.RemoteIpAddress?.ToString();

    [HttpGet]
    public async Task<IActionResult> GetDonors([FromQuery] DonorFilterParams filter) =>
        Ok(await donorService.GetDonorsAsync(filter));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetDonor(Guid id)
    {
        var donor = await donorService.GetDonorAsync(id);
        return donor == null ? NotFound(new ApiError("Donor not found", "Contributor አልተገኘም")) : Ok(donor);
    }

    [HttpPost]
    public async Task<IActionResult> CreateDonor([FromBody] CreateDonorRequest request)
    {
        try
        {
            var donor = await donorService.CreateDonorAsync(request, AdminId, Ip);
            return CreatedAtAction(nameof(GetDonor), new { id = donor.Id }, donor);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiError(ex.Message, "ስልክ ቁጥሩ ቀድሞ ተመዝግቧል"));
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateDonor(Guid id, [FromBody] UpdateDonorRequest request)
    {
        try
        {
            var donor = await donorService.UpdateDonorAsync(id, request, AdminId, Ip);
            return donor == null ? NotFound(new ApiError("Donor not found", "Contributor አልተገኘም")) : Ok(donor);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiError(ex.Message, "ስልክ ቁጥሩ ቀድሞ ተመዝግቧል"));
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteDonor(Guid id)
    {
        var deleted = await donorService.DeleteDonorAsync(id, AdminId, Ip);
        return deleted ? NoContent() : NotFound(new ApiError("Donor not found", "Contributor አልተገኘም"));
    }

    [HttpPost("{id:guid}/donate-today")]
    public async Task<IActionResult> MarkDonatedToday(Guid id, [FromBody] MarkDonatedRequest request)
    {
        var donor = await donorService.MarkDonatedTodayAsync(id, request, AdminId, Ip);
        return donor == null ? NotFound(new ApiError("Donor not found", "Contributor አልተገኘም")) : Ok(donor);
    }
}

[ApiController]
[Route("api/sms")]
[Authorize(Roles = "Admin")]
public class SmsController(ISmsService smsService) : ControllerBase
{
    private Guid AdminId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string? Ip => HttpContext.Connection.RemoteIpAddress?.ToString();

    [HttpGet("balance")]
    public async Task<IActionResult> GetBalance() => Ok(await smsService.GetBalanceAsync());

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 20) =>
        Ok(await smsService.GetHistoryAsync(page, pageSize));

    [HttpPost("send")]
    public async Task<IActionResult> SendSms([FromBody] SendSmsRequest request)
    {
        try
        {
            var result = await smsService.SendSmsAsync(request, AdminId, Ip);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiError(ex.Message, "ተቀባዮች አልተገኙም"));
        }
    }

    [HttpPost("{id:guid}/retry")]
    public async Task<IActionResult> Retry(Guid id)
    {
        try
        {
            await smsService.RetryFailedAsync(id, AdminId);
            return Ok(new { message = "Retry initiated" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiError(ex.Message, "ከፍተኛ የመሞከር ሙከራ ተደርጓል"));
        }
    }
}

[ApiController]
[Route("api/hospitals")]
[Authorize(Roles = "Admin")]
public class HospitalsController(IHospitalService hospitalService) : ControllerBase
{
    private Guid? AdminId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    private string? Ip => HttpContext.Connection.RemoteIpAddress?.ToString();

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool activeOnly = true) =>
        Ok(await hospitalService.GetAllAsync(activeOnly));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var hospital = await hospitalService.GetByIdAsync(id);
        return hospital == null ? NotFound(new ApiError("Hospital not found", "ሆስፒታል አልተገኘም")) : Ok(hospital);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateHospitalRequest request)
    {
        var hospital = await hospitalService.CreateAsync(request, AdminId, Ip);
        return CreatedAtAction(nameof(GetById), new { id = hospital.Id }, hospital);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateHospitalRequest request)
    {
        var hospital = await hospitalService.UpdateAsync(id, request, AdminId, Ip);
        return hospital == null ? NotFound(new ApiError("Hospital not found", "ሆስፒታል አልተገኘም")) : Ok(hospital);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await hospitalService.DeleteAsync(id, AdminId, Ip);
        return deleted ? NoContent() : NotFound(new ApiError("Hospital not found", "ሆስፒታል አልተገኘም"));
    }
}

[ApiController]
[Route("api/reports")]
[Authorize(Roles = "Admin")]
public class ReportsController(IReportService reportService) : ControllerBase
{
    private Guid AdminId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory() => Ok(await reportService.GetReportHistoryAsync());

    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] ReportRequest request)
    {
        try
        {
            var (data, fileName, contentType) = await reportService.GenerateReportAsync(request, AdminId);
            return File(data, contentType, fileName);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiError(ex.Message, "ያልታወቀ የሪፖርት አይነት"));
        }
    }
}
