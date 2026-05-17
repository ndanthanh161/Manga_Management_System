namespace MMS.Application.DTOs.Manuscript;

public record CreateManuscriptRequest(
    string Name,
    List<string> AssetUrls
);
