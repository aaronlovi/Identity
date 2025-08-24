using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Identity.Infrastructure.Firebase;

/// <summary>
/// Configuration options for EventPublisher.
/// </summary>
public class EventPublisherOptions {
    /// <summary>
    /// Google Cloud Project ID for Pub/Sub.
    /// </summary>
    public string? ProjectId { get; set; }

    /// <summary>
    /// Pub/Sub topic name for identity events.
    /// </summary>
    public string TopicName { get; set; } = "identity.events";

    /// <summary>
    /// Source identifier for CloudEvents.
    /// </summary>
    public string Source { get; set; } = "identity.grains";

    /// <summary>
    /// Pub/Sub emulator host for local development (e.g., "localhost:8085").
    /// When set, the EventPublisher will connect to the emulator instead of production Pub/Sub.
    /// </summary>
    public string? EmulatorHost { get; set; }
}
