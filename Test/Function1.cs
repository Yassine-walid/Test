using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.SignalRService;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Test;

public class Function1
{
    private readonly ILogger _logger;

    public Function1(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<Function1>();
    }

    // SignalR Trigger - receives messages from slv_hub and forwards to output_hub
    [Function("SignalRTrigger")]
    [SignalROutput(HubName = "output_hub", ConnectionStringSetting = "AzureSignalRConnectionString")]
    public IEnumerable<SignalRMessageAction> Run(
        [SignalRTrigger(
            hubName: "slv_hub",
            category: "messages",
            @event: "ReceiveRfid",
            ConnectionStringSetting = "AzureSignalRConnectionString")]
        SignalRInvocationContext invocationContext,
        string message,
        FunctionContext context)
    {
        var logger = context.GetLogger("SignalRTrigger");

        logger.LogInformation("=== SIGNALR TRIGGER FIRED ===");
        logger.LogInformation(
            "Invocation from hub {HubName} with event {EventName} and connection {ConnectionId}",
            invocationContext.Hub, invocationContext.Event, invocationContext.ConnectionId);
        logger.LogInformation("Received payload: {Payload}", message);

        if (string.IsNullOrEmpty(message))
        {
            logger.LogWarning("Empty message received");
            return Array.Empty<SignalRMessageAction>();
        }

        try
        {
            // Parse the RFID message
            var rfidData = JsonSerializer.Deserialize<RfidMessage>(message);

            logger.LogInformation(
                "CarteSlv: {CarteSlv}, Device: {Device}",
                rfidData?.carteSlv,
                rfidData?.deviceName);

            // Forward to output_hub
            return new[]
            {
                new SignalRMessageAction("ReceiveRfid")
                {
                    Arguments = new[] { message }
                }
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error forwarding message to SignalR");
            throw;
        }
    }
}

// RFID message model
public class RfidMessage
{
    public string? carteSlv { get; set; }
    public string? deviceId { get; set; }
    public string? deviceName { get; set; }
    public string? tsUtc { get; set; }
}
