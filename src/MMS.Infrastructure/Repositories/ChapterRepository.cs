using MMS.Application.Interfaces.Repositories;
using MMS.Domain.Entities;
using MMS.Infrastructure.Data;

namespace MMS.Infrastructure.Repositories;

public class ChapterRepository : GenericRepository<Chapter>, IChapterRepository
{
    public ChapterRepository(MmsDbContext context) : base(context)
    {
    }
}
