using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Google.Cloud.PubSub.V1;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Identity.Infrastructure.Firebase;

/// <summary>
/// Publishes domain events to Google Cloud Pub/Sub using CloudEvents format.
/// </summary>
public class EventPublisher : IEventPublisher, IAsyncDisposable {
    private readonly PublisherClient _publisherClient;
    private readonly EventPublisherOptions _options;
    private readonly ILogger<EventPublisher> _logger;
    private readonly TopicName _topicName;

    public EventPublisher(
        IOptions<EventPublisherOptions> options,
        ILogger<EventPublisher> logger) {
        _options = options.Value;
        _logger = logger;

        if (string.IsNullOrEmpty(_options.ProjectId)) {
            throw new InvalidOperationException("EventPublisher requires ProjectId to be configured");
        }

        // Configure for emulator if EmulatorHost is set
        if (!string.IsNullOrEmpty(_options.EmulatorHost)) {
            Environment.SetEnvironmentVariable("PUBSUB_EMULATOR_HOST", _options.EmulatorHost);
            _logger.LogInformation("EventPublisher configured for emulator at: {EmulatorHost}", _options.EmulatorHost);
        }

        _topicName = TopicName.FromProjectTopic(_options.ProjectId, _options.TopicName);
        _publisherClient = PublisherClient.Create(_topicName);

        _logger.LogInformation("EventPublisher initialized for topic: {TopicName}", _topicName);
    }

    public async Task PublishAsync(string eventType, object data, long userId, CancellationToken cancellationToken = default) {
        try {
            var cloudEvent = new {
                specversion = "1.0",
                type = eventType,
                source = _options.Source,
                id = Guid.NewGuid().ToString(),
                time = DateTime.UtcNow.ToString("O"),
                datacontenttype = "application/json",
                data = data
            };

            var json = JsonSerializer.Serialize(cloudEvent, new JsonSerializerOptions {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var pubsubMessage = new PubsubMessage {
                Data = ByteString.CopyFromUtf8(json),
                Attributes =
                {
                    ["ce-specversion"] = "1.0",
                    ["ce-type"] = eventType,
                    ["ce-source"] = _options.Source,
                    ["ce-id"] = cloudEvent.id,
                    ["user_id"] = userId.ToString()
                }
            };

            string messageId = await _publisherClient.PublishAsync(pubsubMessage);

            _logger.LogInformation("Published event {EventType} for user {UserId} with message ID: {MessageId}",
                eventType, userId, messageId);
        } catch (Exception ex) {
            _logger.LogError(ex, "Failed to publish event {EventType} for user {UserId}: {Error}",
                eventType, userId, ex.Message);
            throw;
        }
    }

    public async ValueTask DisposeAsync() {
        if (_publisherClient != null) {
            await _publisherClient.DisposeAsync();
        }
    }
}
