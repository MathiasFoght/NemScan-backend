using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NemScan_API.Models.DTO;
using NemScan_API.Services;
using NemScan_API.Utils;

namespace NemScan_API.Controllers;

[ApiController]
[Route("api/employee")]
public class EmployeeController : ControllerBase
{
    private readonly NemScanDbContext _db;
    private readonly EmployeeProfileService _employeeProfileService;

    public EmployeeController(NemScanDbContext db, EmployeeProfileService employeeProfileService)
    {
        _db = db;
        _employeeProfileService = employeeProfileService;
    }

    [HttpPost("upload-profile-image")]
    [Authorize(Policy = "EmployeeOnly")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadProfileImage([FromForm] ProfileImageUploadDTO form)
    {
        var file = form.File;

        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        var employeeNumber = User.FindFirstValue("employeeNumber");
        if (string.IsNullOrEmpty(employeeNumber))
            return Unauthorized();

        var user = await _db.Users.SingleOrDefaultAsync(u => u.EmployeeNumber == employeeNumber);
        if (user == null)
            return NotFound("User not found.");

        if (!string.IsNullOrEmpty(user.ProfileImageUrl))
            await _employeeProfileService.DeleteIfExistsAsync(user.ProfileImageUrl);

        var blobName = $"{employeeNumber}_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

        await using var stream = file.OpenReadStream();
        var imageUrl = await _employeeProfileService.UploadAsync(stream, blobName, file.ContentType);

        user.ProfileImageUrl = imageUrl;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Image uploaded successfully" });
    }
    
    [HttpGet("profile")]
    [Authorize(Policy = "EmployeeOnly")]
    public async Task<IActionResult> GetProfile()
    {
        var employeeNumber = User.FindFirstValue("employeeNumber");
        if (string.IsNullOrEmpty(employeeNumber))
            return Unauthorized("Missing employeeNumber claim.");

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

        return Ok(profileDto);
    }
    
    [HttpDelete("profile-image")]
    [Authorize(Policy = "EmployeeOnly")]
    public async Task<IActionResult> DeleteProfileImage()
    {
        var employeeNumber = User.FindFirstValue("employeeNumber");
        if (string.IsNullOrEmpty(employeeNumber))
            return Unauthorized("Missing employeeNumber claim.");

        var user = await _db.Users.SingleOrDefaultAsync(u => u.EmployeeNumber == employeeNumber);
        if (user is null)
            return NotFound("User not found.");

        if (string.IsNullOrEmpty(user.ProfileImageUrl))
            return BadRequest("No profile image to delete.");

        await _employeeProfileService.DeleteIfExistsAsync(user.ProfileImageUrl);

        user.ProfileImageUrl = null;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Profile image removed successfully." });
    }

}