using MMS.Domain.Exceptions;
using HotChocolate;
using HotChocolate.Execution;

namespace MMS.API.GraphQL;

public class GraphQLErrorFilter : IErrorFilter
{
    public IError OnError(IError error)
    {
        if (error.Exception is null) return error;

        var code = error.Exception is DomainException de ? de.ErrorCode : 
                   (error.Exception is InfrastructureException ie ? ie.ErrorCode : "INTERNAL_ERROR");
                   
        var message = error.Exception is DomainException ? error.Exception.Message : "An internal error occurred";

        // Ghi log ra console thay vì dùng ILogger để tránh lỗi DI từ HotChocolate
        Console.WriteLine($"[GraphQL Error] Code: {code} | Exception: {error.Exception.Message}");

        return ErrorBuilder.FromError(error)
            .SetMessage(message)
            .SetCode(code)
            .SetException(null)
            .Build();
    }
}
