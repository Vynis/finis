using Finis.Api.Entities;

namespace Finis.Api.Services;

public interface ITokenService
{
    string GenerateToken(User user);
}
