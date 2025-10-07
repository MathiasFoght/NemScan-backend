using NemScan_API.Models;

namespace NemScan_API.Interfaces;

public interface IJwtTokenService
{
    string GenerateEmployeeToken(Employee employee);
    string GenerateCustomerToken(Customer customer); 
}