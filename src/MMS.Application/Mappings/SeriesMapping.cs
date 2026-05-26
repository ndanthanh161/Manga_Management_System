using Mapster;
using MMS.Application.DTOs.ImageFile;
using MMS.Application.DTOs.Series;
using MMS.Domain.Entities;
using MMS.Domain.Enums;
using System;

namespace MMS.Application.Mappings;

public class SeriesMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // Series -> SeriesResponse
        config.NewConfig<Series, SeriesResponse>()
            .Map(dest => dest.AuthorName, src => src.Author != null ? src.Author.Username : (src.AuthorName ?? string.Empty))
            .Map(dest => dest.Status, src => src.Status.ToString());

        // CreateSeriesRequest -> Series
        config.NewConfig<CreateSeriesRequest, Series>()
            .Map(dest => dest.Id, src => Guid.NewGuid())
            .Map(dest => dest.Status, src => SeriesStatus.Draft)
            .Map(dest => dest.CreatedAt, src => DateTime.UtcNow)
            .Ignore(dest => dest.Author!)
            .Ignore(dest => dest.Chapters)
            .Ignore(dest => dest.ReaderVotes)
            .Ignore(dest => dest.Rankings);


        // UpdateSeriesRequest -> Series (partial update - bỏ qua null)
        config.NewConfig<UpdateSeriesRequest, Series>()
            .IgnoreNullValues(true)
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.Author!)
            .Ignore(dest => dest.Chapters)
            .Ignore(dest => dest.ReaderVotes)
            .Ignore(dest => dest.Rankings)
            .Ignore(dest => dest.AuthorId!)
            .Map(dest => dest.UpdatedAt, src => DateTime.UtcNow);

        // UploadedImageDto -> Series (Cập nhật ảnh bìa mới và thời gian)
        config.NewConfig<UploadedImageDto, Series>()
            .Map(dest => dest.CoverImageUrl, src => src.SecureUrl)
            .Map(dest => dest.UpdatedAt, src => DateTime.UtcNow)
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.Title)
            .Ignore(dest => dest.AlternativeTitle!)
            .Ignore(dest => dest.Description)
            .Ignore(dest => dest.Author!)
            .Ignore(dest => dest.Chapters)
            .Ignore(dest => dest.ReaderVotes)
            .Ignore(dest => dest.Rankings)
            .Ignore(dest => dest.AuthorId!);

        // Ranking -> SeriesRankingResponse
        config.NewConfig<Ranking, SeriesRankingResponse>()
            .Map(dest => dest.SeriesTitle, src => src.Series != null ? src.Series.Title : string.Empty);
    }
}
