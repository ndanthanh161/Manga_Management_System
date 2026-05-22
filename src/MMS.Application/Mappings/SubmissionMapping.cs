using Mapster;
using MMS.Domain.Entities;
using MMS.Application.DTOs.Submission;
using MMS.Application.DTOs.Manuscript;
using MMS.Application.DTOs.BoardVote;

namespace MMS.Application.Mappings;

public class SubmissionMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // Submission -> SubmissionResponse
        config.NewConfig<Submission, SubmissionResponse>()
            .Map(dest => dest.SeriesTitle, src => src.Series != null ? src.Series.Title : string.Empty)
            .Map(dest => dest.MangakaName, src => src.Mangaka != null ? src.Mangaka.Username : string.Empty)
            .Map(dest => dest.AssignedTantouName, src => src.AssignedTantou != null ? src.AssignedTantou.Username : string.Empty)
            .Map(dest => dest.Status, src => src.Status.ToString())
            .Map(dest => dest.Manuscripts, src => src.Manuscripts.Adapt<List<ManuscriptResponse>>())
            .Map(dest => dest.Votes, src => src.Votes.Adapt<List<BoardVoteResponse>>())
            .Map(dest => dest.Transitions, src => src.Transitions.Adapt<List<SubmissionTransitionResponse>>());

        // Manuscript -> ManuscriptResponse
        config.NewConfig<Manuscript, ManuscriptResponse>()
            .Map(dest => dest.Status, src => src.Status.ToString());

        // BoardVote -> BoardVoteResponse
        config.NewConfig<BoardVote, BoardVoteResponse>()
            .Map(dest => dest.BoardMemberName, src => src.BoardMember != null ? src.BoardMember.Username : string.Empty)
            .Map(dest => dest.VoteType, src => src.VoteType.ToString());

        // SubmissionTransition -> SubmissionTransitionResponse
        config.NewConfig<SubmissionTransition, SubmissionTransitionResponse>()
            .Map(dest => dest.FromStatus, src => src.FromStatus.ToString())
            .Map(dest => dest.ToStatus, src => src.ToStatus.ToString())
            .Map(dest => dest.ActorName, src => src.ActorUser != null ? src.ActorUser.Username : string.Empty);
    }
}
