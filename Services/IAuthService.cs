using System.Threading.Tasks;

namespace LabelStudio.Services;

public interface IAuthService
{
    Task<bool> LoginAsync(string email, string password);
    void Logout();
}
