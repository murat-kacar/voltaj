using Voltflow.Domain.Identity;

namespace Voltflow.Application.Interfaces;

public interface ITokenService
{
    string CreateToken(AppUser user, IReadOnlyCollection<string> roles);
}
