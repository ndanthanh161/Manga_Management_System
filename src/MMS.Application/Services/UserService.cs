using Mapster;
using MMS.Application.Interfaces;
using MMS.Application.Interfaces.Services;
using MMS.Application.Common;
using MMS.Domain.Entities;

namespace MMS.Application.Services;

public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;

    public UserService(IUnitOfWork unitOfWork, IPasswordHasher passwordHasher)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<IEnumerable<UserDto>>> GetAllUsersAsync()
    {
        var users = await _unitOfWork.Users.GetAllAsync();
        return Result<IEnumerable<UserDto>>.Success(users.Adapt<IEnumerable<UserDto>>());
    }

    public async Task<Result<UserDto>> GetUserByIdAsync(Guid id)
    {
        var user = await _unitOfWork.Users.GetUserWithRolesAsync(id);
        if (user == null) return Result<UserDto>.Failure("User not found");

        return Result<UserDto>.Success(user.Adapt<UserDto>());
    }

    public async Task<Result<UserDto>> CreateUserAsync(CreateUserRequest request)
    {
        var existing = await _unitOfWork.Users.GetByEmailAsync(request.Email);
        if (existing != null) return Result<UserDto>.Failure("Email already exists");

        // Sử dụng Adapt để Map tự động (Tuân thủ SOLID - OCP/SRP)
        var user = request.Adapt<User>();
        user.Id = Guid.NewGuid();
        user.PasswordHash = _passwordHasher.Hash(request.Password);

        if (request.Roles != null && request.Roles.Any())
        {
            var roles = await _unitOfWork.Roles.FindAsync(r => request.Roles.Contains(r.Name));
            user.Roles = roles.ToList();
        }

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return Result<UserDto>.Success(user.Adapt<UserDto>());
    }

    public async Task<Result<UserDto>> UpdateUserAsync(UpdateUserRequest request)
    {
        var user = await _unitOfWork.Users.GetUserWithRolesAsync(request.Id);
        if (user == null) return Result<UserDto>.Failure("User not found");

        // Đồng bộ dữ liệu từ request sang entity qua Mapster
        request.Adapt(user);
        
        if (request.Roles != null)
        {
            var roles = await _unitOfWork.Roles.FindAsync(r => request.Roles.Contains(r.Name));
            user.Roles = roles.ToList();
        }

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        return Result<UserDto>.Success(user.Adapt<UserDto>());
    }

    public async Task<Result<bool>> DeleteUserAsync(Guid id)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user == null) return Result<bool>.Failure("User not found");

        _unitOfWork.Users.Remove(user);
        await _unitOfWork.SaveChangesAsync();
        return Result<bool>.Success(true);
    }
}
