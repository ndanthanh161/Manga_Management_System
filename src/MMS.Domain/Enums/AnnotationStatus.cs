using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMS.Domain.Enums
{
    public enum AnnotationStatus
    {
        Open = 1,         // Đang chờ sửa
        Resolved = 2,     // Mangaka đánh dấu đã sửa
        Closed = 3        // Tantou đã nghiệm thu
    }
}
