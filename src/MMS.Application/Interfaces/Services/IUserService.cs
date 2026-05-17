using MMS.Application.Common;

namespace MMS.Application.Interfaces.Services;

public interface IUserService
{
    Task<Result<IEnumerable<UserDto>>> GetAllUsersAsync();
    Task<Result<UserDto>> GetUserByIdAsync(Guid id);
    Task<Result<UserDto>> CreateUserAsync(CreateUserRequest request);
    Task<Result<UserDto>> UpdateUserAsync(UpdateUserRequest request);
    Task<Result<bool>> DeleteUserAsync(Guid id);
}
