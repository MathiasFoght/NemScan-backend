using Microsoft.AspNetCore.Mvc;

namespace NemScan_API.Models.DTO;

public class ProfileImageUploadDTO
{
    [FromForm(Name = "file")]
    public IFormFile File { get; set; } = null!;
}