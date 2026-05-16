using MMS.Domain.Entities;

namespace MMS.Application.Interfaces;

public interface IJwtProvider
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
}
