using MediatR;
using MMS.Application.Interfaces;
using MMS.Domain.Events;

namespace MMS.API.Services;

public class DomainEventPublisher : IDomainEventPublisher
{
    private readonly IMediator _mediator;

    public DomainEventPublisher(IMediator mediator)
    {
        _mediator = mediator;
    }

    public Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken ct = default)
        where TEvent : DomainEvent
    {
        return _mediator.Publish(domainEvent, ct);
    }
}
