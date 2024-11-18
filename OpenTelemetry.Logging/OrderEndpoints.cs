using System.Text.Json;
using OpenTelemetry.Logging.Meters;

namespace OpenTelemetry.Logging;

public static class OrderEndpoints
{
    public static WebApplication MapOrderEndpoints(this WebApplication app)
    {
       
        //var eventTelemetry = new EventTelemetry("orderCreatedEvent");
        app.MapPost("/order", (EntryMeter entryMeter, OrderCommand orderCommand) =>
        {
            entryMeter.IncreaseOrderCreatedCounter();

            entryMeter.RecordOrderAmount(orderCommand.Amount, orderCommand.Country, orderCommand.City);

            return orderCommand;
        });

        return app;
    }
}

internal record OrderCommand(double Amount, int TotalItems, string Country, string City);