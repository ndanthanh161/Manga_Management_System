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

    Task<int> SaveChangesAsync();
}
