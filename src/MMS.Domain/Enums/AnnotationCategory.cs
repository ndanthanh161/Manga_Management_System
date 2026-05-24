using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMS.Domain.Enums
{
    public enum AnnotationCategory
    {
        Story = 1,        // Góp ý về cốt truyện, plot hole
        Dialogue = 2,     // Lời thoại, câu chữ
        Pacing = 3,       // Nhịp truyện, chia panel
        Script = 4,       // Kịch bản tổng thể
        Artwork = 5       // Hình vẽ, phối cảnh, anatomy
    }
}
