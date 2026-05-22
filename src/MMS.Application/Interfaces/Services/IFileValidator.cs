using MMS.Application.Common;
using MMS.Application.DTOs.ImageFile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMS.Application.Interfaces.Services
{
    public interface IFileValidator
    {
        Task<Result<ImageFileInfo>> ValidateImageAsync(Stream stream, string fileName, int maxSizeMb);
    }
}
