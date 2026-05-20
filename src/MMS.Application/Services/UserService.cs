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
        var dtos = users.Adapt<List<UserDto>>();

        var salaries = await _unitOfWork.AssistantSalaries.GetAllAsync();
        var salaryMap = salaries.ToDictionary(s => s.AssistantId, s => s.BaseSalary);

        var updatedDtos = dtos.Select(d => d with { BaseSalary = salaryMap.TryGetValue(d.Id, out var val) ? val : 0 }).ToList();
        return Result<IEnumerable<UserDto>>.Success(updatedDtos);
    }

    public async Task<Result<UserDto>> GetUserByIdAsync(Guid id)
    {
        var user = await _unitOfWork.Users.GetUserWithRolesAsync(id);
        if (user == null) return Result<UserDto>.NotFound("USER_NOT_FOUND", "User not found");

        var salary = (await _unitOfWork.AssistantSalaries.FindAsync(s => s.AssistantId == id)).FirstOrDefault();
        var dto = user.Adapt<UserDto>() with { BaseSalary = salary?.BaseSalary ?? 0 };

        return Result<UserDto>.Success(dto);
    }

    public async Task<Result<UserDto>> CreateUserAsync(CreateUserRequest request)
    {
        var existing = await _unitOfWork.Users.GetByEmailAsync(request.Email);
        if (existing != null) return Result<UserDto>.Failure("EMAIL_EXISTS", "Email already exists");

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
        if (user == null) return Result<UserDto>.NotFound("USER_NOT_FOUND", "User not found");

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
        if (user == null) return Result<bool>.NotFound("USER_NOT_FOUND", "User not found");

        _unitOfWork.Users.Remove(user);
        await _unitOfWork.SaveChangesAsync();
        return Result<bool>.Success(true);
    }

    public async Task<Result<IEnumerable<UserDto>>> GetAssistantsByMangakaAsync(Guid mangakaId)
    {
        var assistants = await _unitOfWork.Users.GetAssistantsByMangakaAsync(mangakaId);
        var dtos = assistants.Adapt<List<UserDto>>();

        var salaries = await _unitOfWork.AssistantSalaries.GetAllAsync();
        var salaryMap = salaries.ToDictionary(s => s.AssistantId, s => s.BaseSalary);

        var updatedDtos = dtos.Select(d => d with { BaseSalary = salaryMap.TryGetValue(d.Id, out var val) ? val : 0 }).ToList();
        return Result<IEnumerable<UserDto>>.Success(updatedDtos);
    }

    public async Task<Result<UserDto>> SetAssistantBaseSalaryAsync(Guid mangakaId, Guid assistantId, decimal baseSalary)
    {
        var assistant = await _unitOfWork.Users.GetUserWithRolesAsync(assistantId);
        if (assistant == null) return Result<UserDto>.NotFound("USER_NOT_FOUND", "Trợ lý không tồn tại.");

        if (assistant.MangakaId != mangakaId)
        {
            return Result<UserDto>.Forbidden("FORBIDDEN", "Bạn chỉ có quyền thiết lập lương cho Trợ lý trực thuộc quản lý của mình.");
        }

        if (baseSalary < 0)
        {
            return Result<UserDto>.Failure("INVALID_SALARY", "Lương cơ bản không được nhỏ hơn 0.");
        }

        var salary = (await _unitOfWork.AssistantSalaries.FindAsync(s => s.AssistantId == assistantId)).FirstOrDefault();
        if (salary == null)
        {
            salary = new AssistantSalary
            {
                Id = Guid.NewGuid(),
                AssistantId = assistantId,
                BaseSalary = baseSalary,
                UpdatedAt = DateTime.UtcNow
            };
            await _unitOfWork.AssistantSalaries.AddAsync(salary);
        }
        else
        {
            salary.BaseSalary = baseSalary;
            salary.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.AssistantSalaries.Update(salary);
        }
        await _unitOfWork.SaveChangesAsync();

        var dto = assistant.Adapt<UserDto>() with { BaseSalary = baseSalary };
        return Result<UserDto>.Success(dto);
    }
}
