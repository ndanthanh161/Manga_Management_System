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
    private ISubmissionTransitionRepository? _submissionTransitions;
    private IRegionRepository? _regions;
    private ITaskRepository? _tasks;
    private IAnnotationRepository? _annotations;
    private IChapterRepository? _chapters;
    private IPageRepository? _pages;
    private IManuscriptRepository? _manuscripts;
    private IManuscriptPageRefRepository? _manuscriptPageRefs;
    private IAuditLogRepository? _auditLogs;
    private IAssistantSalaryRepository? _assistantSalaries;
    private IAssistantIncomeRepository? _assistantIncomes;
    private IBoardVoteRepository? _boardVotes;
    private INotificationRepository? _notifications;

    public UnitOfWork(MmsDbContext context)
    {
        _context = context;
    }

    public IUserRepository Users => _users ??= new UserRepository(_context);
    public IRoleRepository Roles => _roles ??= new RoleRepository(_context);
    public IInviteRepository Invites => _invites ??= new InviteRepository(_context);
    public ISeriesRepository Series => _series ??= new SeriesRepository(_context);
    public ISubmissionRepository Submissions => _submissions ??= new SubmissionRepository(_context);
    public ISubmissionTransitionRepository SubmissionTransitions => _submissionTransitions ??= new SubmissionTransitionRepository(_context);
    public IRegionRepository Regions => _regions ??= new RegionRepository(_context);
    public ITaskRepository Tasks => _tasks ??= new TaskRepository(_context);
    public IAnnotationRepository Annotations => _annotations ??= new AnnotationRepository(_context);
    public IChapterRepository Chapters => _chapters ??= new ChapterRepository(_context);
    public IPageRepository Pages => _pages ??= new PageRepository(_context);
    public IManuscriptRepository Manuscripts => _manuscripts ??= new ManuscriptRepository(_context);
    public IManuscriptPageRefRepository ManuscriptPageRefs => _manuscriptPageRefs ??= new ManuscriptPageRefRepository(_context);
    public IAuditLogRepository AuditLogs => _auditLogs ??= new AuditLogRepository(_context);
    public IAssistantSalaryRepository AssistantSalaries => _assistantSalaries ??= new AssistantSalaryRepository(_context);
    public IAssistantIncomeRepository AssistantIncomes => _assistantIncomes ??= new AssistantIncomeRepository(_context);
    public IBoardVoteRepository BoardVotes => _boardVotes ??= new BoardVoteRepository(_context);
    public INotificationRepository Notifications => _notifications ??= new NotificationRepository(_context);


    public async Task<int> SaveChangesAsync() => await _context.SaveChangesAsync();

    public void Dispose() => _context.Dispose();
}
