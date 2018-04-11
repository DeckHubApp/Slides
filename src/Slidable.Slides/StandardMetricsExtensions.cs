using System;
using App.Metrics;
using App.Metrics.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace SlidableLive
{
    public static class StandardMetricsExtensions
    {
        public static IWebHostBuilder UseStandardMetrics(this IWebHostBuilder webHostBuilder)
        {
            return webHostBuilder
                .ConfigureMetricsWithDefaults((webHostBuilderContext, metricsBuilder) =>
                {
                    var influxDb = webHostBuilderContext.Configuration["AppMetrics:InfluxDbServer"];
                    if (!string.IsNullOrWhiteSpace(influxDb))
                    {
                        var influxDatabase = webHostBuilderContext.Configuration["AppMetrics:InfluxDbDatabase"];
                        if (!string.IsNullOrWhiteSpace(influxDatabase))
                        {
                            Console.WriteLine($"Report To {influxDb}/{influxDatabase}");
                            metricsBuilder.Report.ToInfluxDb(o =>
                            {
                                o.InfluxDb.BaseUri = new Uri(influxDb);
                                o.InfluxDb.Database = influxDatabase;
                                o.FlushInterval = TimeSpan.FromSeconds(5);
                                o.InfluxDb.CreateDataBaseIfNotExists = true;
                            });
                        }
                    }
                })
                .UseMetrics();
        }
    }
}