using System;

namespace MMS.Application.DTOs.Series;

public record SeriesVoteDto(
    Guid Id,
    Guid SeriesId,
    Guid BoardMemberId,
    string BoardMemberName,
    string VoteType,
    Guid? ProposedTantouId,
    string? Comment,
    DateTime CreatedAt
);
