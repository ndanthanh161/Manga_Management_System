using Mapster;
using MMS.Domain.Entities;
using MMS.Application.DTOs.CandidateApplication;

namespace MMS.Application.Mappings;

public class CandidateApplicationMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Asset, CandidateApplicationAssetDto>();

        config.NewConfig<ApplicationVote, CandidateApplicationVoteDto>()
            .Map(dest => dest.BoardMemberName, src => src.BoardMember != null ? src.BoardMember.Username : string.Empty)
            .Map(dest => dest.VoteType, src => src.VoteType.ToString());

        config.NewConfig<CandidateApplication, CandidateApplicationDto>()
            .Map(dest => dest.Email, src => src.User != null ? src.User.Email : string.Empty)
            .Map(dest => dest.SeriesTitle, src => src.Series != null ? src.Series.Title : string.Empty)
            .Map(dest => dest.Status, src => src.Status.ToString())
            .Map(dest => dest.Assets, src => src.Assets)
            .Map(dest => dest.Votes, src => src.ApplicationVotes);

        // SubmitApplicationRequest -> User
        config.NewConfig<SubmitApplicationRequest, User>()
            .Map(dest => dest.Id, src => Guid.NewGuid())
            .Map(dest => dest.Username, src => src.FullName)
            .Map(dest => dest.Email, src => src.Email)
            .Map(dest => dest.IsActive, src => false)
            .Ignore(dest => dest.PasswordHash)
            .Ignore(dest => dest.Roles);

        // SubmitApplicationRequest -> Series
        config.NewConfig<SubmitApplicationRequest, Series>()
            .Map(dest => dest.Id, src => Guid.NewGuid())
            .Map(dest => dest.Title, src => src.SeriesTitle)
            .Map(dest => dest.Description, src => $"Bản thảo ứng tuyển của {src.FullName}")
            .Map(dest => dest.Status, src => MMS.Domain.Enums.SeriesStatus.Draft)
            .Map(dest => dest.AuthorEmail, src => src.Email)
            .Map(dest => dest.AuthorName, src => src.FullName)
            .Ignore(dest => dest.Author!)
            .Ignore(dest => dest.Chapters)
            .Ignore(dest => dest.ReaderVotes)
            .Ignore(dest => dest.Rankings);

        // SubmitApplicationRequest -> CandidateApplication
        config.NewConfig<SubmitApplicationRequest, CandidateApplication>()
            .Map(dest => dest.Id, src => Guid.NewGuid())
            .Map(dest => dest.FullName, src => src.FullName)
            .Map(dest => dest.Status, src => MMS.Domain.Enums.CandidateApplicationStatus.Pending)
            .Ignore(dest => dest.UserId)
            .Ignore(dest => dest.SeriesId)
            .Ignore(dest => dest.User)
            .Ignore(dest => dest.Series)
            .Ignore(dest => dest.ApprovedByUserId!)
            .Ignore(dest => dest.ApprovedByUser!);
    }
}
