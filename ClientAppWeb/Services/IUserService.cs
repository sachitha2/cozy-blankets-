using ClientAppWeb.Models;

namespace ClientAppWeb.Services;

public interface IUserService
{
    User? ValidateUser(string userName, string password);
    (bool success, string message) RegisterCustomer(string userName, string password, string email);
    User? GetByUserName(string userName);
}
