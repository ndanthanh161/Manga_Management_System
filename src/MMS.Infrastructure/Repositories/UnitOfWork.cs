using MMS.Application.Interfaces;
using MMS.Application.Interfaces.Repositories;
using MMS.Infrastructure.Data;
using MMS.Infrastructure.Repositories;

namespace MMS.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly MmsDbContext _context;
    private IUserRepository? _users;
    private IRoleRepository? _roles;
    private IInviteRepository? _invites;
    private ISeriesRepository? _series;
    private ISubmissionRepository? _submissions;
    private IRegionRepository? _regions;
    private ITaskRepository? _tasks;
    private IAnnotationRepository? _annotations;
    private IChapterRepository? _chapters;
    private IPageRepository? _pages;
    private IAuditLogRepository? _auditLogs;

    public UnitOfWork(MmsDbContext context)
    {
        _context = context;
    }

    public IUserRepository Users => _users ??= new UserRepository(_context);
    public IRoleRepository Roles => _roles ??= new RoleRepository(_context);
    public IInviteRepository Invites => _invites ??= new InviteRepository(_context);
    public ISeriesRepository Series => _series ??= new SeriesRepository(_context);
    public ISubmissionRepository Submissions => _submissions ??= new SubmissionRepository(_context);
    public IRegionRepository Regions => _regions ??= new RegionRepository(_context);
    public ITaskRepository Tasks => _tasks ??= new TaskRepository(_context);
    public IAnnotationRepository Annotations => _annotations ??= new AnnotationRepository(_context);
    public IChapterRepository Chapters => _chapters ??= new ChapterRepository(_context);
    public IPageRepository Pages => _pages ??= new PageRepository(_context);
    public IAuditLogRepository AuditLogs => _auditLogs ??= new AuditLogRepository(_context);

    public async Task<int> SaveChangesAsync() => await _context.SaveChangesAsync();

    public void Dispose() => _context.Dispose();
}
