using NemScan_API.Models;

namespace NemScan_API.Interfaces;

public interface IAuthService
{
    Task<User?> AuthenticateEmployeeAsync(string employeeNumber);
}