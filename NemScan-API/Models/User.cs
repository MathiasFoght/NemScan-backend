using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace NemScan_API.Models;

public enum UserRole
{
    Admin,
    Basic
}

public class User
{
    public Guid Id { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Basic;
}