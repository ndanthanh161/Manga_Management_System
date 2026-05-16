using Mapster;
using MMS.Application.DTOs;
using MMS.Domain.Entities;

namespace MMS.Application.Mappings;

public class AuthMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // User -> AuthResponse
        config.NewConfig<User, AuthResponse>()
            .Map(dest => dest.Roles, src => src.Roles.Select(r => r.Name));
    }
}
