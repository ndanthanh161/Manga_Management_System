using Mapster;
using MMS.Domain.Entities;

namespace MMS.Application.Mappings;

public class SubmissionMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // Submission -> SubmissionResponse
        config.NewConfig<Submission, SubmissionResponse>()
            .Map(dest => dest.SeriesTitle, src => src.Series != null ? src.Series.Title : string.Empty)
            .Map(dest => dest.MangakaName, src => src.Mangaka != null ? src.Mangaka.Username : string.Empty)
            .Map(dest => dest.Status, src => src.Status.ToString())
            .Map(dest => dest.Manuscripts, src => src.Manuscripts.Adapt<List<ManuscriptResponse>>())
            .Map(dest => dest.Votes, src => src.Votes.Adapt<List<BoardVoteResponse>>());

        // Manuscript -> ManuscriptResponse
        config.NewConfig<Manuscript, ManuscriptResponse>()
            .Map(dest => dest.Status, src => src.Status.ToString());

        // BoardVote -> BoardVoteResponse
        config.NewConfig<BoardVote, BoardVoteResponse>()
            .Map(dest => dest.BoardMemberName, src => src.BoardMember != null ? src.BoardMember.Username : string.Empty)
            .Map(dest => dest.VoteType, src => src.VoteType.ToString());
    }
}
