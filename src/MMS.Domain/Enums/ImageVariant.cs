using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMS.Domain.Enums
{
    public enum ImageVariant
    {
        Original,       // No transform — download
        Large1024,      // w=1024, auto height — editor canvas
        Medium512,      // w=512, auto height — grid view
        Thumbnail256    // w=256, h=256, fill — list thumbnail
    }
}
