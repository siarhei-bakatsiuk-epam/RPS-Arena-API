namespace RpsArena.Match.Application.Common.Abstractions;

/// <summary>
/// Publishes integration events. Backed by MassTransit's transactional outbox in
/// Infrastructure, so a publish enrolled before SaveChanges commits atomically
/// with the database change (no ghost events on rollback).
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : class;
}
