using MMS.Application.Common;
using MMS.Application.DTOs.Annotation;
using MMS.Domain.Enums;
using System.Text.Json;

namespace MMS.Application.Validators;

public static class AnnotationValidator
{
    public static Result<bool> ValidateCreate(CreateAnnotationRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Content))
            return Result<bool>.Failure("InvalidContent", "Content cannot be empty.");
            
        if (req.Content.Length > 2000)
            return Result<bool>.Failure("InvalidContent", "Content cannot exceed 2000 characters.");

        if (req.Width <= 0 || req.Height <= 0)
            return Result<bool>.Failure("InvalidDimensions", "Width and Height must be greater than 0.");

        if (req.X < 0 || req.Y < 0)
            return Result<bool>.Failure("InvalidCoordinates", "X and Y must be greater than or equal to 0.");

        if (req.X + req.Width > 100)
            return Result<bool>.Failure("InvalidRegion", "X + Width cannot exceed 100%.");

        if (req.Y + req.Height > 100)
            return Result<bool>.Failure("InvalidRegion", "Y + Height cannot exceed 100%.");

        if (req.Shape != AnnotationShape.Freehand && !string.IsNullOrWhiteSpace(req.StrokeData))
            return Result<bool>.Failure("InvalidStrokeData", "StrokeData must be null for non-Freehand shapes.");

        if (req.Shape == AnnotationShape.Freehand)
        {
            if (string.IsNullOrWhiteSpace(req.StrokeData))
                return Result<bool>.Failure("InvalidStrokeData", "StrokeData is required for Freehand shape.");

            try
            {
                JsonDocument.Parse(req.StrokeData);
            }
            catch (JsonException)
            {
                return Result<bool>.Failure("InvalidStrokeData", "StrokeData must be valid JSON.");
            }
        }

        return Result<bool>.Success(true);
    }
}
