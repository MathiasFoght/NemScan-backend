using NemScan_API.Models;
using NemScan_API.Models.Auth;

namespace NemScan_API.Interfaces;

public interface IJwtTokenService
{
    string GenerateEmployeeToken(Employee employee);
    string GenerateCustomerToken(Customer customer); 
}