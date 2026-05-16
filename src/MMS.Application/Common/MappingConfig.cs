using Mapster;
using System.Reflection;

namespace MMS.Application.Common;

public static class MappingConfig
{
    public static void Configure()
    {
        var config = TypeAdapterConfig.GlobalSettings;
        
        // Quét toàn bộ Assembly hiện tại để tìm các lớp implement IRegister
        config.Scan(Assembly.GetExecutingAssembly());
    }
}
