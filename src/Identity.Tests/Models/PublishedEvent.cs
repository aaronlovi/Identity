using System;

namespace Identity.Grains.Tests.Models;

/// <summary>
/// Represents an event published by the MockEventPublisher.
/// </summary>
public record PublishedEvent(
    string EventType,
    object Data,
    long UserId,
    DateTime PublishedAt);
