namespace NemScan_API.Interfaces;

public interface IAmeroAuthService
{
    Task<string> GetAccessTokenAsync();
}

