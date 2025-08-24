using System.Threading;
using System.Threading.Tasks;

namespace Identity.Infrastructure.Firebase;

/// <summary>
/// Service for publishing domain events to Pub/Sub.
/// </summary>
public interface IEventPublisher {
    /// <summary>
    /// Publishes a domain event to the identity.events topic.
    /// </summary>
    /// <param name="eventType">CloudEvents event type (e.g., "UserStatusChanged")</param>
    /// <param name="data">Event payload</param>
    /// <param name="userId">User ID for event attribution</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task PublishAsync(
        string eventType,
        object data,
        long userId,
        CancellationToken cancellationToken = default);
}
