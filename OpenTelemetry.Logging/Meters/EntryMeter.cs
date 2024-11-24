using System.Diagnostics.Metrics;

namespace OpenTelemetry.Logging.Meters;

public class EntryMeter
{
    private readonly Counter<long> _deniedCreditCounter;
    private readonly Counter<long> _ordersCreatedCounter;
    private readonly Histogram<double> _ordersAmountHistogram;
    private readonly UpDownCounter<double> _ordersAmountUpDownCounter;

    public static readonly string MeterName = "OpenTelemetry.Learning.Meters";

    public EntryMeter(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);

        _deniedCreditCounter = meter.CreateCounter<long>("otel.credits.denied",
            description: "Counts the number of denied credits.", unit: "counter",
            tags: ApplicationDiagnostics.DefaultTags);

        _ordersCreatedCounter = meter.CreateCounter<long>("otel.orders.created",
            description: "Counts the number of orders created.", unit: "counter",
            tags: ApplicationDiagnostics.DefaultTags);

        _ordersAmountHistogram = meter.CreateHistogram("otel.orders.amount.histogram",
            description: "Tracks the orders amount", unit: "usd", tags: ApplicationDiagnostics.DefaultTags,
            advice: new InstrumentAdvice<double>
            {
                // Define the buckets for the histogram (e.g., orders between $0–$50, $51–$100, etc.)
                HistogramBucketBoundaries = [0, 50, 100, 200, 500, 1000, 2000]
            });

        _ordersAmountUpDownCounter = meter.CreateUpDownCounter<double>("otel.orders.amount.counter",
            description: "Tracks the orders amount", unit: "usd");
    }

    public void IncreaseOrderCreatedCounter()
    {
        _ordersCreatedCounter.Add(1);
    }

    public void IncreaseCreditDeniedCounter()
    {
        _deniedCreditCounter.Add(1);
    }

    public void RecordOrderAmountHistogram(double amount, string country, string city)
    {
        _ordersAmountHistogram.Record(amount, new("country", country), new("city", city));
    }

    public void RecordOrderAmountCounter(double amount, string country, string city)
    {
        _ordersAmountUpDownCounter.Add(amount, new("country", country), new("city", city));
    }
}