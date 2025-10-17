using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NemScan_API.Interfaces;
using NemScan_API.Models.DTO;
using NemScan_API.Models.Events;
using NemScan_API.Utils;

namespace NemScan_API.Controllers;

[ApiController]
[Route("api/employee")]
public class EmployeeController : ControllerBase
{
    private readonly NemScanDbContext _db;
    private readonly IEmployeeService _employeeProfileService;
    private readonly ILogEventPublisher _logEventPublisher;

    public EmployeeController(NemScanDbContext db, IEmployeeService employeeProfileService, ILogEventPublisher logEventPublisher)
    {
        _db = db;
        _employeeProfileService = employeeProfileService;
        _logEventPublisher = logEventPublisher;
    }

    [HttpPost("upload-profile-image")]
    [Authorize(Policy = "EmployeeOnly")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadProfileImage([FromForm] ProfileImageUploadDTO form)
    {
        var employeeNumber = User.FindFirstValue("employeeNumber");
        if (string.IsNullOrEmpty(employeeNumber))
            return Unauthorized("Missing employeeNumber claim in token");

        var user = await _db.Users.SingleOrDefaultAsync(u => u.EmployeeNumber == employeeNumber);
        if (user is null)
            return NotFound("Employee not found");

        var file = form.File;
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded");
        }

        if (!string.IsNullOrEmpty(user.ProfileImageUrl))
            await _employeeProfileService.DeleteIfExistsAsync(user.ProfileImageUrl);

        var blobName = $"{employeeNumber}_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

        await using var stream = file.OpenReadStream();
        var imageUrl = await _employeeProfileService.UploadAsync(stream, blobName, file.ContentType);

        user.ProfileImageUrl = imageUrl;
        await _db.SaveChangesAsync();
        
        await _logEventPublisher.PublishAsync(new EmployeeLogEvent
        {
            EventType = "employee.profile.upload.success",
            EmployeeNumber = employeeNumber,
            Success = true,
            Message = $"Employee ({user.Name}) uploaded a new profile image.",
            Timestamp = DateTime.UtcNow
        }, "employee.profile.upload.success");

        return Ok(new { message = "Image uploaded successfully" });
    }
    
    [HttpGet("profile")]
    [Authorize(Policy = "EmployeeOnly")]
    public async Task<IActionResult> GetProfile()
    {
        var employeeNumber = User.FindFirstValue("employeeNumber");
        if (string.IsNullOrEmpty(employeeNumber))
            return Unauthorized("Missing employeeNumber claim in token");

        var user = await _db.Users.SingleOrDefaultAsync(u => u.EmployeeNumber == employeeNumber);
        if (user is null)
            return NotFound("User not found.");

        var profileDto = new EmployeeProfileDTO
        {
            EmployeeNumber = user.EmployeeNumber,
            Name = user.Name,
            Role = user.Role.ToString(),
            Position = user.Position.ToString(),
            StoreNumber = user.StoreNumber,
            ProfileImageUrl = user.ProfileImageUrl,
        };
        
        await _logEventPublisher.PublishAsync(new EmployeeLogEvent
        {
            EventType = "employee.profile.view",
            EmployeeNumber = employeeNumber,
            Success = true,
            Message = $"Employee ({user.Name}) viewed profile information.",
            Timestamp = DateTime.UtcNow
        }, "employee.profile.view");

        return Ok(profileDto);
    }
    
    [HttpDelete("profile-image")]
    [Authorize(Policy = "EmployeeOnly")]
    public async Task<IActionResult> DeleteProfileImage()
    {
        var employeeNumber = User.FindFirstValue("employeeNumber");
        if (string.IsNullOrEmpty(employeeNumber))
            return Unauthorized("Missing employeeNumber claim in token");

        var user = await _db.Users.SingleOrDefaultAsync(u => u.EmployeeNumber == employeeNumber);
        if (user is null)
            return NotFound("Employee not found");

        if (string.IsNullOrEmpty(user.ProfileImageUrl))
            return BadRequest("No profile image to delete.");

        await _employeeProfileService.DeleteIfExistsAsync(user.ProfileImageUrl);

        user.ProfileImageUrl = null;
        await _db.SaveChangesAsync();
        
        await _logEventPublisher.PublishAsync(new EmployeeLogEvent
        {
            EventType = "employee.profile.delete.success",
            EmployeeNumber = employeeNumber,
            Success = true,
            Message = $"Employee ({user.Name}) deleted their profile image.",
            Timestamp = DateTime.UtcNow
        }, "employee.profile.delete.success");


        return Ok(new { message = "Profile image removed successfully" });
    }

}