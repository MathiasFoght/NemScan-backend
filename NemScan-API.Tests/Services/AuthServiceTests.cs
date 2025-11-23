using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using NemScan_API.Models.Auth;
using NemScan_API.Services.Auth;
using NemScan_API.Utils;
using NUnit.Framework;

namespace NemScan_API.Tests.Services
{
    public class AuthServiceTests
    {
        private NemScanDbContext _db = null!;
        private Mock<IPasswordHasher<Employee>> _hasherMock = null!;
        private AuthService _service = null!;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<NemScanDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _db = new NemScanDbContext(options);

            _hasherMock = new Mock<IPasswordHasher<Employee>>();

            _service = new AuthService(_db, _hasherMock.Object);
        }

        // -----------------------------
        // 1) Invalid input cases
        // -----------------------------

        [Test]
        public async Task AuthenticateEmployee_ReturnsNull_WhenEmployeeNumberIsEmpty()
        {
            var result = await _service.AuthenticateEmployeeAsync("", "pw");
            Assert.IsNull(result);
        }

        [Test]
        public async Task AuthenticateEmployee_ReturnsNull_WhenPasswordIsEmpty()
        {
            var result = await _service.AuthenticateEmployeeAsync("123", "");
            Assert.IsNull(result);
        }

        // -----------------------------
        // 2) User not found
        // -----------------------------
        [Test]
        public async Task AuthenticateEmployee_ReturnsNull_WhenUserNotFound()
        {
            var result = await _service.AuthenticateEmployeeAsync("123", "pw");
            Assert.IsNull(result);
        }

        // -----------------------------
        // 3) User exists but no PasswordHash
        // -----------------------------
        [Test]
        public async Task AuthenticateEmployee_ReturnsNull_WhenUserHasNoPasswordHash()
        {
            _db.Users.Add(new Employee
            {
                EmployeeNumber = "123",
                PasswordHash = null
            });
            await _db.SaveChangesAsync();

            var result = await _service.AuthenticateEmployeeAsync("123", "pw");
            Assert.IsNull(result);
        }

        // -----------------------------
        // 4) Password verification FAILED
        // -----------------------------
        [Test]
        public async Task AuthenticateEmployee_ReturnsNull_WhenPasswordVerificationFails()
        {
            var user = new Employee
            {
                EmployeeNumber = "123",
                PasswordHash = "HASHED"
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            _hasherMock
                .Setup(x => x.VerifyHashedPassword(user, "HASHED", "pw"))
                .Returns(PasswordVerificationResult.Failed);

            var result = await _service.AuthenticateEmployeeAsync("123", "pw");

            Assert.IsNull(result);
        }

        // -----------------------------
        // 5) Password verification SUCCESS
        // -----------------------------
        [Test]
        public async Task AuthenticateEmployee_ReturnsUser_WhenPasswordIsValid()
        {
            var user = new Employee
            {
                EmployeeNumber = "123",
                PasswordHash = "HASHED"
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            _hasherMock
                .Setup(x => x.VerifyHashedPassword(user, "HASHED", "pw"))
                .Returns(PasswordVerificationResult.Success);

            var result = await _service.AuthenticateEmployeeAsync("123", "pw");

            Assert.IsNotNull(result);
            Assert.AreEqual("123", result!.EmployeeNumber);
        }

        // -----------------------------
        // 6) Password verification SUCCESS-REHASH
        // -----------------------------
        [Test]
        public async Task AuthenticateEmployee_RehashesPassword_WhenSuccessRehashNeeded()
        {
            var user = new Employee
            {
                EmployeeNumber = "123",
                PasswordHash = "OLD_HASH"
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            _hasherMock
                .Setup(x => x.VerifyHashedPassword(user, "OLD_HASH", "pw"))
                .Returns(PasswordVerificationResult.SuccessRehashNeeded);

            _hasherMock
                .Setup(x => x.HashPassword(user, "pw"))
                .Returns("NEW_HASH");

            var result = await _service.AuthenticateEmployeeAsync("123", "pw");

            Assert.IsNotNull(result);
            Assert.AreEqual("NEW_HASH", user.PasswordHash);
        }
    }
}
