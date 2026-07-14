using System;

namespace MMS.Application.DTOs.CandidateApplication;

public record CandidateApplicationVoteDto(
    Guid Id,
    Guid BoardMemberId,
    string BoardMemberName,
    string VoteType,
    Guid? ProposedTantouId,
    string? Comment,
    DateTime CreatedAt
);
