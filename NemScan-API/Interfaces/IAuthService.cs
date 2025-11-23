using NemScan_API.Models.Auth;

namespace NemScan_API.Interfaces;

public interface IAuthService
{
    Task<Employee?> AuthenticateEmployeeAsync(string employeeNumber, string hashedValue);
}