using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NemScan_API.Interfaces;
using NemScan_API.Utils;

namespace NemScan_API.Services.Auth;

public class AuthService : IAuthService
{
    private readonly NemScanDbContext _db;
    private readonly IPasswordHasher<Models.Auth.Employee> _passwordHasher;

    public AuthService(NemScanDbContext db, IPasswordHasher<Models.Auth.Employee> passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    public async Task<Models.Auth.Employee?> AuthenticateEmployeeAsync(string employeeNumber, string hashedValue)
    {
        if (string.IsNullOrWhiteSpace(employeeNumber) || string.IsNullOrWhiteSpace(hashedValue))
            return null;

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.EmployeeNumber == employeeNumber.Trim());

        if (user == null || string.IsNullOrWhiteSpace(user.PasswordHash))
            return null;

        if (!VerifyPassword(user, hashedValue))
            return null;

        return user;
    }

    private bool VerifyPassword(Models.Auth.Employee user, string password)
    {
        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash!, password);

        if (result == PasswordVerificationResult.Failed)
            return false;

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _passwordHasher.HashPassword(user, password);
            _db.SaveChanges();
        }

        return true;
    }
}