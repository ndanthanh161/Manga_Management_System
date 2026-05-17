using Mapster;
using MMS.Domain.Entities;

namespace MMS.Application.Mappings;

public class UserMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // User -> UserDto
        config.NewConfig<User, UserDto>()
            .Map(dest => dest.Roles, src => src.Roles.Select(r => r.Name));

        // CreateUserRequest -> User
        config.NewConfig<CreateUserRequest, User>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.PasswordHash)
            .Map(dest => dest.CreatedAt, src => DateTime.UtcNow)
            .Map(dest => dest.IsActive, src => true);

        // UpdateUserRequest -> User
        config.NewConfig<UpdateUserRequest, User>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.Roles)
            .Map(dest => dest.UpdatedAt, src => DateTime.UtcNow);
    }
}
