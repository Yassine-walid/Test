using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
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
    public SignalRMessageAction Run(
        [SignalRTrigger(
            "slv_hub",
            "messages",
            "ReceiveRfid",
            ConnectionStringSetting = "AzureSignalRConnectionString")]
        string message,
        FunctionContext context)
    {
        var logger = context.GetLogger("SignalRTrigger");

        logger.LogInformation("=== SIGNALR TRIGGER FIRED ===");
        logger.LogInformation($"Received: {message}");

        if (string.IsNullOrEmpty(message))
        {
            logger.LogWarning("Empty message received");
            return null;
        }

        try
        {
            // Parse the RFID message
            var rfidData = JsonSerializer.Deserialize<RfidMessage>(message);

            logger.LogInformation($"CarteSlv: {rfidData?.carteSlv}, Device: {rfidData?.deviceName}");

            // Forward to output_hub
            return new SignalRMessageAction("ReceiveRfid")
            {
                Arguments = new[] { message }
            };
        }
        catch (Exception ex)
        {
            logger.LogError($"Error: {ex.Message}");
            throw;
        }
    }
}

// RFID message model
public class RfidMessage
{
    public string carteSlv { get; set; }
    public string deviceId { get; set; }
    public string deviceName { get; set; }
    public string tsUtc { get; set; }
}