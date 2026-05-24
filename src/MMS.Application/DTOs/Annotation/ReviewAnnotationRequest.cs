using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMS.Application.DTOs.Annotation
{
    public record ReviewAnnotationRequest
    (
        bool IsApproved,
        string? Feedback
    );
}
