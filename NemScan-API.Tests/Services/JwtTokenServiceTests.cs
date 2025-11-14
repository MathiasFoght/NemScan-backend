using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NemScan_API.Config;
using NemScan_API.Models.Auth;
using NemScan_API.Services.Auth;
using NUnit.Framework;

namespace NemScan_API.Tests.Services
{
    public class JwtTokenServiceTests
    {
        private JwtTokenService _service = null!;
        private JwtOptions _options = null!;

        [SetUp]
        public void Setup()
        {
            _options = new JwtOptions
            {
                Key = "ThisIsASuperLongTestKey_ForJwtSigning_1234567890",
                Issuer = "TestIssuer",
                Audience = "TestAudience",
                Expire = 30
            };

            _service = new JwtTokenService(Options.Create(_options));
        }

        private JwtSecurityToken ReadToken(string jwt)
        {
            return new JwtSecurityTokenHandler().ReadJwtToken(jwt);
        }

        // ---------------- EMPLOYEE TOKEN TESTS ---------------- //

        [Test]
        public void GenerateEmployeeToken_ShouldContainExpectedClaims()
        {
            var employee = new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "E123",
                Name = "John Doe",
                Role = EmployeeRole.Admin
            };

            var jwt = _service.GenerateEmployeeToken(employee);
            var token = ReadToken(jwt);

            Assert.AreEqual(employee.Id.ToString(), token.Claims.First(c => c.Type == ClaimTypes.NameIdentifier || c.Type == JwtRegisteredClaimNames.Sub).Value);
            Assert.AreEqual("E123", token.Claims.First(c => c.Type == "employeeNumber").Value);
            Assert.AreEqual("John Doe", token.Claims.First(c => c.Type == "name").Value);
            Assert.AreEqual("Admin", token.Claims.First(c => c.Type == "role").Value);
            Assert.AreEqual("employee", token.Claims.First(c => c.Type == "userType").Value);
        }

        // ---------------- CUSTOMER TOKEN TESTS ---------------- //

        [Test]
        public void GenerateCustomerToken_ShouldContainExpectedClaims()
        {
            var customer = new Customer
            {
                Id = Guid.NewGuid(),
                DeviceId = "Device123"
            };

            var jwt = _service.GenerateCustomerToken(customer);
            var token = ReadToken(jwt);

            Assert.AreEqual(customer.Id.ToString(), token.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
            Assert.AreEqual("customer", token.Claims.First(c => c.Type == "userType").Value);
            Assert.AreEqual("barcode:read", token.Claims.First(c => c.Type == "scope").Value);
            Assert.AreEqual("Device123", token.Claims.First(c => c.Type == "deviceId").Value);
        }

        [Test]
        public void GenerateCustomerToken_ShouldExcludeDeviceId_WhenNull()
        {
            var customer = new Customer
            {
                Id = Guid.NewGuid(),
                DeviceId = null
            };

            var jwt = _service.GenerateCustomerToken(customer);
            var token = ReadToken(jwt);

            Assert.False(token.Claims.Any(c => c.Type == "deviceId"));
        }

        // ---------------- GENERAL TOKEN VALIDATION ---------------- //

        [Test]
        public void GeneratedToken_ShouldHaveCorrectIssuerAndAudience()
        {
            var employee = new Employee { Id = Guid.NewGuid() };

            var jwt = _service.GenerateEmployeeToken(employee);
            var token = ReadToken(jwt);

            Assert.AreEqual(_options.Issuer, token.Issuer);
            Assert.AreEqual(_options.Audience, token.Audiences.First());
        }

        [Test]
        public void GeneratedToken_ShouldHaveExpiration()
        {
            var employee = new Employee { Id = Guid.NewGuid() };

            var jwt = _service.GenerateEmployeeToken(employee);
            var token = ReadToken(jwt);

            Assert.That(token.ValidTo, Is.GreaterThan(DateTime.UtcNow));
        }

        [Test]
        public void GeneratedToken_ShouldBeValidSignature()
        {
            var employee = new Employee { Id = Guid.NewGuid() };
            var jwt = _service.GenerateEmployeeToken(employee);

            var handler = new JwtSecurityTokenHandler();
            var validationParams = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _options.Issuer,

                ValidateAudience = true,
                ValidAudience = _options.Audience,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key)),

                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            Assert.DoesNotThrow(() =>
            {
                handler.ValidateToken(jwt, validationParams, out _);
            });
        }
    }
}
