using MMS.Application.Common;
using MMS.Application.DTOs.CandidateApplication;
using MMS.Application.Interfaces;
using MMS.Application.Interfaces.Services;
using MMS.Domain.Entities;
using MMS.Domain.Enums;
using Mapster;

namespace MMS.Application.Services;

public class CandidateApplicationService : ICandidateApplicationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IImageStorageService _imageStorageService;
    private readonly IEmailService _emailService;

    public CandidateApplicationService(
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IImageStorageService imageStorageService,
        IEmailService emailService)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _imageStorageService = imageStorageService;
        _emailService = emailService;
    }

    public async Task<Result<CandidateApplicationDto>> SubmitApplicationAsync(
        SubmitApplicationRequest request, CancellationToken ct = default)
    {
        // 1. Validate email chưa tồn tại
        var existingUser = await _unitOfWork.Users.GetByEmailAsync(request.Email);
        if (existingUser != null)
            return Result<CandidateApplicationDto>.Failure("EMAIL_EXISTS", "Email này đã được đăng ký trong hệ thống.");

        // 2. Upload các file lên Cloudinary
        var assets = new List<Asset>();
        if (request.Files != null && request.Files.Count > 0)
        {
            foreach (var file in request.Files)
            {
                if (file.Length > 0)
                {
                    using var stream = file.OpenReadStream();
                    var folder = "mms/manuscripts/applications";
                    var hint = $"app_{Guid.NewGuid().ToString("N")[..8]}";
                    var uploadResult = await _imageStorageService.UploadImageAsync(stream, folder, hint, ct);
                    if (!uploadResult.Succeeded)
                        return Result<CandidateApplicationDto>.Failure("UPLOAD_FAILED", uploadResult.Message ?? $"Upload file {file.FileName} thất bại.", 500);

                    assets.Add(new Asset
                    {
                        Id = Guid.NewGuid(),
                        Name = file.FileName,
                        Type = file.ContentType,
                        FileUrl = uploadResult.Data!.SecureUrl,
                        PublicId = uploadResult.Data.PublicId,
                        Size = file.Length,
                        UploadedAt = DateTime.UtcNow
                    });
                }
            }
        }

        // 3. Hash password
        var passwordHash = _passwordHasher.Hash(request.Password);

        // 4. Tạo User với IsActive = false, không có Role
        var user = request.Adapt<User>();
        user.PasswordHash = passwordHash;
        await _unitOfWork.Users.AddAsync(user);

        // 5. Tạo Series draft
        var series = request.Adapt<Series>();
        series.AuthorId = user.Id;
        await _unitOfWork.Series.AddAsync(series);

        // 6. Tạo CandidateApplication
        var application = request.Adapt<CandidateApplication>();
        application.UserId = user.Id;
        application.SeriesId = series.Id;
        application.User = user;
        application.Series = series;
        application.Assets = assets;
        await _unitOfWork.CandidateApplications.AddAsync(application);

        await _unitOfWork.SaveChangesAsync();

        // 7. Return DTO
        var dto = application.Adapt<CandidateApplicationDto>();

        return Result<CandidateApplicationDto>.Success(dto, "Đơn ứng tuyển đã được gửi thành công. Vui lòng chờ phản hồi qua email.");
    }

    public async Task<Result<CandidateApplicationDto>> VoteApplicationAsync(
        Guid applicationId, Guid boardMemberId, VoteType vote, Guid? proposedTantouId, string? comment)
    {
        // 1. Kiểm tra EB role
        var boardMember = await _unitOfWork.Users.GetUserWithRolesAsync(boardMemberId);
        if (boardMember == null || !boardMember.Roles.Any(r => r.Name == "Editorial Board"))
            return Result<CandidateApplicationDto>.Failure("FORBIDDEN", "Chỉ thành viên Editorial Board mới có thể bỏ phiếu.", 403);

        // 2. Load application kèm ApplicationVotes + User + Series
        var application = await _unitOfWork.CandidateApplications.GetWithDetailsAsync(applicationId);
        if (application == null)
            return Result<CandidateApplicationDto>.NotFound("NOT_FOUND", "Đơn ứng tuyển không tồn tại.");

        if (application.Status != CandidateApplicationStatus.Pending)
            return Result<CandidateApplicationDto>.Failure("ALREADY_REVIEWED", "Đơn ứng tuyển này đã được xử lý.");

        // 3. Kiểm tra chưa vote (prevent duplicate)
        var existingVotes = await _unitOfWork.ApplicationVotes.FindAsync(v => v.ApplicationId == applicationId);
        if (existingVotes.Any(v => v.BoardMemberId == boardMemberId))
            return Result<CandidateApplicationDto>.Failure("ALREADY_VOTED", "Bạn đã bỏ phiếu cho đơn ứng tuyển này rồi.");

        // 4. Tạo phiếu bầu mới
        var newVote = new ApplicationVote
        {
            Id = Guid.NewGuid(),
            ApplicationId = applicationId,
            BoardMemberId = boardMemberId,
            VoteType = vote,
            ProposedTantouId = (vote == VoteType.Approve) ? proposedTantouId : null,
            Comment = comment,
            CreatedAt = DateTime.UtcNow
        };
        await _unitOfWork.ApplicationVotes.AddAsync(newVote);
        await _unitOfWork.SaveChangesAsync();

        // 5. Tính tổng số EB members trong hệ thống
        var allEbMembers = await _unitOfWork.Users.FindAsync(u =>
            u.IsActive && u.Roles.Any(r => r.Name == "Editorial Board"));
        int totalEb = allEbMembers.Count();

        // 6. Đếm phiếu hiện tại
        var allVotes = (await _unitOfWork.ApplicationVotes.FindAsync(v => v.ApplicationId == applicationId)).ToList();
        int approveCount = allVotes.Count(v => v.VoteType == VoteType.Approve);
        int rejectCount = allVotes.Count(v => v.VoteType == VoteType.Reject);

        // 7. Kiểm tra đạt ngưỡng
        if (approveCount > totalEb / 2.0)
        {
            var selectedTantouId = ResolveTantouByConsensus(allVotes.Where(v => v.VoteType == VoteType.Approve));
            application.ApprovedByUserId = boardMemberId;
            application.ReviewedAt = DateTime.UtcNow;
            return await ApproveApplicationAsync(application, selectedTantouId);
        }

        if (rejectCount > totalEb / 2.0)
        {
            application.ReviewedAt = DateTime.UtcNow;
            return await RejectApplicationAsync(application);
        }

        return Result<CandidateApplicationDto>.Success(
            application.Adapt<CandidateApplicationDto>(),
            $"Phiếu bầu đã được ghi nhận. Hiện tại: {approveCount} Approve / {rejectCount} Reject / {totalEb} EB members.");
    }

    // Thuật toán đồng thuận: plurality vote + tie-breaker theo CreatedAt sớm nhất
    private static Guid? ResolveTantouByConsensus(IEnumerable<ApplicationVote> approveVotes)
    {
        var grouped = approveVotes
            .Where(v => v.ProposedTantouId.HasValue)
            .GroupBy(v => v.ProposedTantouId!.Value)
            .Select(g => new { TantouId = g.Key, Count = g.Count(), EarliestVote = g.Min(v => v.CreatedAt) })
            .OrderByDescending(g => g.Count)
            .ThenBy(g => g.EarliestVote)
            .FirstOrDefault();

        return grouped?.TantouId;
    }

    private async Task<Result<CandidateApplicationDto>> ApproveApplicationAsync(
        CandidateApplication application, Guid? tantouEditorId)
    {
        if (tantouEditorId == null)
            return Result<CandidateApplicationDto>.Failure("EDITOR_REQUIRED", "Vui lòng đề xuất Biên tập viên (Tantou Editor) khi bỏ phiếu duyệt đơn.");

        // Guard: không cho phép gán lại Tantou nếu series đã active
        var series = application.Series;
        if (series.Status != SeriesStatus.Draft)
            return Result<CandidateApplicationDto>.Failure("CANNOT_REASSIGN_TANTOU", "Tác phẩm đã hoạt động. Không thể thay đổi Tantou phụ trách.");

        // Validate tantou editor tồn tại
        var editor = await _unitOfWork.Users.GetUserWithRolesAsync(tantouEditorId.Value);
        if (editor == null || !editor.Roles.Any(r => r.Name == "Tantou Editor"))
            return Result<CandidateApplicationDto>.Failure("INVALID_EDITOR", "Biên tập viên không hợp lệ.");

        var user = application.User;

        // 1. Kích hoạt User
        user.IsActive = true;
        user.UpdatedAt = DateTime.UtcNow;

        // 2. Gán Role Mangaka
        var mangakaRole = (await _unitOfWork.Roles.FindAsync(r => r.Name == "Mangaka")).FirstOrDefault();
        if (mangakaRole != null)
        {
            var userWithRoles = await _unitOfWork.Users.GetUserWithRolesAsync(user.Id);
            if (userWithRoles != null && !userWithRoles.Roles.Any(r => r.Name == "Mangaka"))
            {
                userWithRoles.Roles.Add(mangakaRole);
                _unitOfWork.Users.Update(userWithRoles);
            }
        }

        // 3. Cập nhật Series → Ongoing + gán TantouEditorId (khóa cứng)
        series.Status = SeriesStatus.Ongoing;
        series.TantouEditorId = tantouEditorId.Value;
        series.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Series.Update(series);

        // 4. Tạo Chapter 1 cho Series
        var chapter = new Chapter
        {
            Id = Guid.NewGuid(),
            Title = "Chương 1: Bản thảo ứng tuyển",
            ChapterNumber = 1,
            IsPublished = false,
            SeriesId = series.Id,
            CreatedAt = DateTime.UtcNow
        };
        await _unitOfWork.Chapters.AddAsync(chapter);

        // 5. Tạo Manuscript chính thức cho Chapter 1
        var manuscript = new Manuscript
        {
            Id = Guid.NewGuid(),
            Name = "Bản thảo ứng tuyển",
            Status = ManuscriptStatus.Draft,
            Version = 1,
            IsCurrentVersion = true,
            ChapterId = chapter.Id,
            CreatedAt = DateTime.UtcNow
        };
        await _unitOfWork.Manuscripts.AddAsync(manuscript);

        // 6. Chuyển các Asset của candidate thành của Manuscript này
        if (application.Assets != null && application.Assets.Count > 0)
        {
            foreach (var asset in application.Assets)
            {
                asset.ManuscriptId = manuscript.Id;
            }
        }

        // 7. Cập nhật trạng thái application
        application.Status = CandidateApplicationStatus.Approved;
        _unitOfWork.CandidateApplications.Update(application);

        await _unitOfWork.SaveChangesAsync();

        await _emailService.SendApplicationApprovedAsync(user.Email, application.FullName, series.Title);

        return Result<CandidateApplicationDto>.Success(application.Adapt<CandidateApplicationDto>(),
            "Đơn ứng tuyển đã được chấp nhận. Email thông báo đã được gửi.");
    }

    private async Task<Result<CandidateApplicationDto>> RejectApplicationAsync(
        CandidateApplication application)
    {
        var user = application.User;
        var series = application.Series;
        var email = user.Email;
        var fullName = application.FullName;
        var seriesTitle = series.Title;
        var reviewNote = application.ReviewNote;

        // 1. Cập nhật trạng thái application
        application.Status = CandidateApplicationStatus.Rejected;
        _unitOfWork.CandidateApplications.Update(application);

        // 2. Xóa Series draft
        _unitOfWork.Series.Remove(series);

        // 3. Xóa User pending
        _unitOfWork.Users.Remove(user);

        // 4. Xóa manuscript khỏi Cloudinary
        if (application.Assets != null && application.Assets.Count > 0)
        {
            foreach (var asset in application.Assets)
            {
                if (!string.IsNullOrEmpty(asset.PublicId))
                {
                    await _imageStorageService.DeleteImageAsync(asset.PublicId);
                }
            }
        }

        await _unitOfWork.SaveChangesAsync();

        await _emailService.SendApplicationRejectedAsync(email, fullName, seriesTitle, reviewNote);

        return Result<CandidateApplicationDto>.Success(application.Adapt<CandidateApplicationDto>(),
            "Đơn ứng tuyển đã bị từ chối. Email thông báo đã được gửi.");
    }

    public async Task<Result<IEnumerable<CandidateApplicationDto>>> GetPendingApplicationsAsync()
    {
        var applications = await _unitOfWork.CandidateApplications.GetAllPendingAsync();
        var dtos = applications.Adapt<IEnumerable<CandidateApplicationDto>>();
        return Result<IEnumerable<CandidateApplicationDto>>.Success(dtos);
    }

    public async Task<Result<CandidateApplicationDto>> GetApplicationByIdAsync(Guid id)
    {
        var application = await _unitOfWork.CandidateApplications.GetWithDetailsAsync(id);
        if (application == null)
            return Result<CandidateApplicationDto>.NotFound("NOT_FOUND", "Đơn ứng tuyển không tồn tại.");

        return Result<CandidateApplicationDto>.Success(application.Adapt<CandidateApplicationDto>());
    }
}
