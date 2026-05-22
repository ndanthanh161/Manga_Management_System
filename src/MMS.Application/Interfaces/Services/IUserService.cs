using MMS.Application.Common;
using MMS.Application.DTOs.User;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MMS.Application.Interfaces.Services;

public interface IUserService
{
    Task<Result<IEnumerable<UserDto>>> GetAllUsersAsync();
    Task<Result<UserDto>> GetUserByIdAsync(Guid id);
    Task<Result<UserDto>> CreateUserAsync(CreateUserRequest request);
    Task<Result<UserDto>> UpdateUserAsync(UpdateUserRequest request);
    Task<Result<bool>> DeleteUserAsync(Guid id);
    Task<Result<IEnumerable<UserDto>>> GetAssistantsByMangakaAsync(Guid mangakaId);
    Task<Result<UserDto>> SetAssistantBaseSalaryAsync(Guid mangakaId, Guid assistantId, decimal baseSalary);

    Task<Result<UploadAvatarResponse>> UploadAvatarAsync(Guid userId, UploadAvatarRequest request, CancellationToken ct = default);
}
