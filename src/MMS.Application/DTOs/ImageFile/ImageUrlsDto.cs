using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMS.Application.DTOs.ImageFile
{
    public record ImageUrlsDto(
    string PublicId,
    string Original,
    string Large,
    string Medium,
    string Thumbnail
);
}
