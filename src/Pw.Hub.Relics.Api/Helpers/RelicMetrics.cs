using Prometheus;

namespace Pw.Hub.Relics.Api.Helpers;

public static class RelicMetrics
{
    // Счетчики (Counters) - только увеличиваются
    public static readonly Counter RelicsProcessedTotal = Metrics.CreateCounter(
        "relics_processed_total", 
        "Total number of relic lots processed", 
        new CounterConfiguration { LabelNames = new[] { "status" } }); // created, updated, skipped, failed

    public static readonly Counter BatchesProcessedTotal = Metrics.CreateCounter(
        "relics_batches_processed_total", 
        "Total number of batches processed");

    // Измерители (Gauges) - могут увеличиваться и уменьшаться
    public static readonly Gauge ParseQueueLength = Metrics.CreateGauge(
        "relics_parse_queue_length", 
        "Current number of items in the parsing queue");

    // Гистограммы (Histograms) - для измерения длительности и распределения
    public static readonly Histogram BatchProcessingDuration = Metrics.CreateHistogram(
        "relics_batch_processing_duration_seconds", 
        "Time taken to process a single batch of relics",
        new HistogramConfiguration
        {
            Buckets = new[] { 0.01, 0.05, 0.1, 0.5, 1.0, 2.5, 5.0, 10.0 }
        });

    public static readonly Histogram BatchSize = Metrics.CreateHistogram(
        "relics_batch_size", 
        "Number of items in a processed batch",
        new HistogramConfiguration
        {
            Buckets = new double[] { 1, 5, 10, 25, 50, 75, 100 }
        });
}
