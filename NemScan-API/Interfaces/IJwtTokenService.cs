using NemScan_API.Models;

namespace NemScan_API.Interfaces;

public interface IJwtTokenService
{
    string GenerateEmployeeToken(User user);
    string GenerateCustomerToken(Customer customer); 
}