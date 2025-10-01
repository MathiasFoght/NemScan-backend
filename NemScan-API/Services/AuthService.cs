using Microsoft.EntityFrameworkCore;
using NemScan_API.Interfaces;
using NemScan_API.Models;
using NemScan_API.Utils;

namespace NemScan_API.Services;

public class AuthService : IAuthService
{
    private readonly NemScanDbContext _db;

    public AuthService(NemScanDbContext db)
    {
        _db = db;
    }

    public async Task<User?> AuthenticateEmployeeAsync(string employeeNumber)
    {
        return await _db.Users.FirstOrDefaultAsync(u => u.EmployeeNumber == employeeNumber);
    }
}