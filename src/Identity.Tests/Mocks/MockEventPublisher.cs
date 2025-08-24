using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Identity.Grains.Tests.Models;
using Identity.Infrastructure.Firebase;
using Microsoft.Extensions.Logging;

namespace Identity.Grains.Tests.Mocks;

/// <summary>
/// Mock implementation of IEventPublisher for unit tests.
/// Captures published events for verification without requiring actual Pub/Sub infrastructure.
/// </summary>
public class MockEventPublisher : IEventPublisher, IAsyncDisposable {
    private readonly ILogger<MockEventPublisher> _logger;
    private readonly List<PublishedEvent> _publishedEvents = [];

    public MockEventPublisher(ILogger<MockEventPublisher> logger) {
        _logger = logger;
    }

    public IReadOnlyList<PublishedEvent> PublishedEvents => _publishedEvents.AsReadOnly();

    public Task PublishAsync(string eventType, object data, long userId, CancellationToken cancellationToken = default) {
        var publishedEvent = new PublishedEvent(
            EventType: eventType,
            Data: data,
            UserId: userId,
            PublishedAt: DateTime.UtcNow
        );

        _publishedEvents.Add(publishedEvent);

        _logger.LogInformation("Mock published event {EventType} for user {UserId}", eventType, userId);

        return Task.CompletedTask;
    }

    public void ClearEvents() {
        _publishedEvents.Clear();
    }

    public ValueTask DisposeAsync() {
        _publishedEvents.Clear();
        return ValueTask.CompletedTask;
    }
}

