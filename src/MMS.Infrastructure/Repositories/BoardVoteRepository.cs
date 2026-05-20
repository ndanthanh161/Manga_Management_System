using MMS.Application.Interfaces.Repositories;
using MMS.Domain.Entities;
using MMS.Infrastructure.Data;

namespace MMS.Infrastructure.Repositories;

public class BoardVoteRepository : GenericRepository<BoardVote>, IBoardVoteRepository
{
    public BoardVoteRepository(MmsDbContext context) : base(context)
    {
    }
}
