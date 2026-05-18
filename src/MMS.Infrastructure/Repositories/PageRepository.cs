using MMS.Application.Interfaces.Repositories;
using MMS.Domain.Entities;
using MMS.Infrastructure.Data;

namespace MMS.Infrastructure.Repositories;

public class PageRepository : GenericRepository<Page>, IPageRepository
{
    public PageRepository(MmsDbContext context) : base(context)
    {
    }
}
