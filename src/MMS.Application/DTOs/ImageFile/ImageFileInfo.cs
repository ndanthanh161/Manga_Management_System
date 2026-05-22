using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMS.Application.DTOs.ImageFile
{
    public record ImageFileInfo(string DetectedFormat, long FileSizeBytes);
}
