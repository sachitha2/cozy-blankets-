using System.Security.Cryptography;
using System.Text;
using ClientAppWeb.Models;

namespace ClientAppWeb.Services;

/// <summary>
/// In-memory user store with default users and customer registration.
/// Default logins: manufacturer/manufacturer123, distributor/distributor123, seller/seller123, customer/customer123
/// </summary>
public class UserService : IUserService
{
    private readonly List<User> _users = new();
    private int _nextId = 1;
    private readonly object _lock = new();

    public UserService()
    {
        SeedDefaultUsers();
    }

    private void SeedDefaultUsers()
    {
        _users.AddRange(new[]
        {
            new User { Id = _nextId++, UserName = "manufacturer", PasswordHash = HashPassword("manufacturer123"), Role = "Manufacturer" },
            new User { Id = _nextId++, UserName = "distributor", PasswordHash = HashPassword("distributor123"), Role = "Distributor" },
            new User { Id = _nextId++, UserName = "seller", PasswordHash = HashPassword("seller123"), Role = "Seller" },
            new User { Id = _nextId++, UserName = "customer", PasswordHash = HashPassword("customer123"), Role = "Customer", Email = "customer@example.com" },
        });
    }

    private static string HashPassword(string password)
    {
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }

    private static bool VerifyPassword(string password, string storedHash)
    {
        var computed = HashPassword(password);
        return string.Equals(computed, storedHash, StringComparison.Ordinal);
    }

    public User? ValidateUser(string userName, string password)
    {
        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
            return null;
        lock (_lock)
        {
            var user = _users.FirstOrDefault(u => string.Equals(u.UserName, userName.Trim(), StringComparison.OrdinalIgnoreCase));
            return user != null && VerifyPassword(password, user.PasswordHash) ? user : null;
        }
    }

    public (bool success, string message) RegisterCustomer(string userName, string password, string email)
    {
        if (string.IsNullOrWhiteSpace(userName) || userName.Length < 3)
            return (false, "Username must be at least 3 characters.");
        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            return (false, "Password must be at least 6 characters.");
        lock (_lock)
        {
            if (_users.Any(u => string.Equals(u.UserName, userName.Trim(), StringComparison.OrdinalIgnoreCase)))
                return (false, "Username already taken.");
            _users.Add(new User
            {
                Id = _nextId++,
                UserName = userName.Trim(),
                PasswordHash = HashPassword(password),
                Role = "Customer",
                Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim()
            });
            return (true, "Registration successful. You can now log in.");
        }
    }

    public User? GetByUserName(string userName)
    {
        lock (_lock)
            return _users.FirstOrDefault(u => string.Equals(u.UserName, userName, StringComparison.OrdinalIgnoreCase));
    }
}
