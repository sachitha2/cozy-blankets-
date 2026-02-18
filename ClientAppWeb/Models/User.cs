namespace ClientAppWeb.Models;

/// <summary>
/// User for login. Roles: Customer (can register), Manufacturer, Distributor, Seller (default logins).
/// </summary>
public class User
{
    public int Id { get; set; }
    public string UserName { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = "Customer"; // Customer, Manufacturer, Distributor, Seller
    public string? Email { get; set; }
}

public class LoginViewModel
{
    public string UserName { get; set; } = "";
    public string Password { get; set; } = "";
    public string? ReturnUrl { get; set; }
    public string? ErrorMessage { get; set; }
}

public class RegisterViewModel
{
    public string UserName { get; set; } = "";
    public string Password { get; set; } = "";
    public string ConfirmPassword { get; set; } = "";
    public string Email { get; set; } = "";
    public string? ErrorMessage { get; set; }
}
