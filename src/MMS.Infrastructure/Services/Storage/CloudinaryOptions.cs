using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMS.Infrastructure.Services.Storage
{
    public class CloudinaryOptions
    {
        public const string SectionName = "Cloudinary";
        public string CloudName { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string ApiSecret { get; set; } = string.Empty;
        public string DefaultFolder { get; set; } = "mms";
        public int MaxFileSizeMb { get; set; } = 20;
    }
}
