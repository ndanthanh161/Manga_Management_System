using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMS.Domain.Enums
{
    public enum AnnotationShape
    {
        Rectangle = 1,    // Hình chữ nhật — case phổ biến
        Polygon = 2,      // Đa giác — vẽ vùng phức tạp
        Freehand = 3      // Vẽ tay — lưu polyline trong StrokeData
    }
}
