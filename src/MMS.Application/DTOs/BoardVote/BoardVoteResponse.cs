namespace MMS.Application.DTOs.BoardVote;

public record BoardVoteResponse(
    Guid Id,
    Guid BoardMemberId,
    string BoardMemberName,
    string VoteType,
    string? Comment
);
