using System;

namespace MMS.Application.DTOs.Series;

public record ReviewSeriesRequest(
    Guid SeriesId,
    bool IsApproved,
    Guid? TantouEditorId = null
);
