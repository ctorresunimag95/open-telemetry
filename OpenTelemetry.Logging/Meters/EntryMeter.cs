using System.Diagnostics.Metrics;

namespace OpenTelemetry.Logging.Meters;

public class EntryMeter
{
    private readonly Counter<long> _deniedCreditCounter;
    private readonly Counter<long> _ordersCreatedCounter;
    private readonly Histogram<double> _ordersAmountHistogram;

    public static readonly string MeterName = "OpenTelemetry.Learning.Meters";

    public EntryMeter(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);

        _deniedCreditCounter = meter.CreateCounter<long>("otel.credits.denied",
            description: "Counts the number of denied credits.", unit: "counter", tags: ApplicationDiagnostics.DefaultTags);

        _ordersCreatedCounter = meter.CreateCounter<long>("otel.orders.created",
            description: "Counts the number of orders created.", unit: "counter", tags: ApplicationDiagnostics.DefaultTags);

        _ordersAmountHistogram = meter.CreateHistogram<double>("otel.orders.amount",
            description: "Tracks the orders amount", unit: "usd", tags: ApplicationDiagnostics.DefaultTags);
    }

    public void IncreaseOrderCreatedCounter()
    {
        _ordersCreatedCounter.Add(1);
    }

    public void IncreaseCreditDeniedCounter()
    {
        _deniedCreditCounter.Add(1);
    }

    public void RecordOrderAmount(double amount, string country, string city)
    {
        _ordersAmountHistogram.Record(amount, new("country", country), new("city", city));
    }
}
