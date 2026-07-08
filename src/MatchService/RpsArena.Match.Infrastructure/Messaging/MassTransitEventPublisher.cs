using MassTransit;
using RpsArena.Match.Application.Common.Abstractions;

namespace RpsArena.Match.Infrastructure.Messaging;

/// <summary>
/// Publishes through MassTransit. When the entity-framework bus outbox is
/// enabled the publish is captured into the outbox within the current DbContext,
/// so it is delivered only after (and if) SaveChanges commits.
/// </summary>
public sealed class MassTransitEventPublisher(IPublishEndpoint publishEndpoint) : IEventPublisher
{
    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : class =>
        publishEndpoint.Publish(@event, cancellationToken);
}
