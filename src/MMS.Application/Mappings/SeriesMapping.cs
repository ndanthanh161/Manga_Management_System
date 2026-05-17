using Mapster;
using MMS.Domain.Entities;
using MMS.Domain.Enums;

namespace MMS.Application.Mappings;

public class SeriesMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // Series -> SeriesResponse
        config.NewConfig<Series, SeriesResponse>()
            .Map(dest => dest.AuthorName, src => src.Author != null ? src.Author.Username : string.Empty)
            .Map(dest => dest.Status, src => src.Status.ToString());

        // CreateSeriesRequest -> Series
        config.NewConfig<CreateSeriesRequest, Series>()
            .Map(dest => dest.Id, src => Guid.NewGuid())
            .Map(dest => dest.Status, src => SeriesStatus.Ongoing)
            .Map(dest => dest.CreatedAt, src => DateTime.UtcNow)
            .Ignore(dest => dest.Author)
            .Ignore(dest => dest.Chapters)
            .Ignore(dest => dest.ReaderVotes)
            .Ignore(dest => dest.Rankings);
            // AuthorId KHÔNG map được — là runtime value từ JWT token, phải set ở service

        // UpdateSeriesRequest -> Series (partial update - bỏ qua null)
        config.NewConfig<UpdateSeriesRequest, Series>()
            .IgnoreNullValues(true)
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.Author)
            .Ignore(dest => dest.Chapters)
            .Ignore(dest => dest.ReaderVotes)
            .Ignore(dest => dest.Rankings)
            .Ignore(dest => dest.AuthorId)
            .Map(dest => dest.UpdatedAt, src => DateTime.UtcNow);

        // Ranking -> SeriesRankingResponse
        config.NewConfig<Ranking, SeriesRankingResponse>()
            .Map(dest => dest.SeriesTitle, src => src.Series != null ? src.Series.Title : string.Empty);
    }
}
