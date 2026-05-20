using MMS.Domain.Exceptions;
using HotChocolate;
using HotChocolate.Execution;

namespace MMS.API.GraphQL;

public class GraphQLErrorFilter : IErrorFilter
{
    private readonly ILogger<GraphQLErrorFilter> _logger;

    public GraphQLErrorFilter(ILogger<GraphQLErrorFilter> logger) => _logger = logger;

    public IError OnError(IError error)
    {
        if (error.Exception is null) return error;

        var code = error.Exception is DomainException de ? de.ErrorCode : 
                   (error.Exception is InfrastructureException ie ? ie.ErrorCode : "INTERNAL_ERROR");
                   
        var message = error.Exception is DomainException ? error.Exception.Message : "An internal error occurred";

        _logger.LogError(error.Exception, "GraphQL error: {Code}", code);

        return ErrorBuilder.FromError(error)
            .SetMessage(message)
            .SetCode(code)
            .SetException(null)
            .Build();
    }
}
