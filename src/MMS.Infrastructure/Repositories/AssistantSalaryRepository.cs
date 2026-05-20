using MMS.Application.Interfaces.Repositories;
using MMS.Domain.Entities;
using MMS.Infrastructure.Data;

namespace MMS.Infrastructure.Repositories;

public class AssistantSalaryRepository : GenericRepository<AssistantSalary>, IAssistantSalaryRepository
{
    public AssistantSalaryRepository(MmsDbContext context) : base(context)
    {
    }
}
