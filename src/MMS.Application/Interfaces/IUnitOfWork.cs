using MMS.Application.Interfaces.Repositories;

namespace MMS.Application.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    IRoleRepository Roles { get; }
    IInviteRepository Invites { get; }
    ISeriesRepository Series { get; }
    ISubmissionRepository Submissions { get; }
    IRegionRepository Regions { get; }
    ITaskRepository Tasks { get; }
    IAnnotationRepository Annotations { get; }
    IChapterRepository Chapters { get; }
    IPageRepository Pages { get; }
    IAuditLogRepository AuditLogs { get; }
    IAssistantSalaryRepository AssistantSalaries { get; }
    IAssistantIncomeRepository AssistantIncomes { get; }
    IBoardVoteRepository BoardVotes { get; }
    INotificationRepository Notifications { get; }

    Task<int> SaveChangesAsync();
}
