using System.Diagnostics;
using Amazon.CloudWatch.Model;

internal class TestCloudWatchService : TestingCloudWatchClient
{
    public TestCloudWatchService()
    {
    }

    internal async Task ReadDynamoDBMetricsAsync(string tableName)
    {
        Console.WriteLine($"Fetching CloudWatch metrics for table: {tableName}");
        Console.WriteLine("=".PadRight(80, '='));

        // Get metrics for last 1 hour
        DateTime endTime = DateTime.UtcNow;
        DateTime startTime = endTime.AddHours(-1);

        // Read Capacity Units (RCU)
        await GetMetricAsync(tableName, "ConsumedReadCapacityUnits", "AWS/DynamoDB", startTime, endTime);

        // Write Capacity Units (WCU)
        await GetMetricAsync(tableName, "ConsumedWriteCapacityUnits", "AWS/DynamoDB", startTime, endTime);

        // User Errors
        await GetMetricAsync(tableName, "UserErrors", "AWS/DynamoDB", startTime, endTime);

        // System Errors
        await GetMetricAsync(tableName, "SystemErrors", "AWS/DynamoDB", startTime, endTime);

        // Latency metrics
        await GetMetricAsync(tableName, "SuccessfulRequestLatency", "AWS/DynamoDB", startTime, endTime);

        // Query count
        await GetMetricAsync(tableName, "Query", "AWS/DynamoDB", startTime, endTime);

        // Scan count
        await GetMetricAsync(tableName, "Scan", "AWS/DynamoDB", startTime, endTime);
    }

    private async Task GetMetricAsync(string tableName, string metricName, string namespaceName, DateTime startTime, DateTime endTime)
    {
        try
        {
            var request = new GetMetricStatisticsRequest
            {
                Namespace = namespaceName,
                MetricName = metricName,
                Dimensions = new List<Dimension>
                {
                    new Dimension { Name = "TableName", Value = tableName }
                },
                StartTime = startTime,
                EndTime = endTime,
                Period = 300, // 5-minute intervals
                Statistics = new List<string> { "Sum", "Average", "Maximum" }
            };

            var response = await client.GetMetricStatisticsAsync(request);

            if (response.Datapoints.Count > 0)
            {
                Console.WriteLine($"\n{metricName}:");
                foreach (var datapoint in response.Datapoints.OrderBy(d => d.Timestamp))
                {
                    Console.WriteLine($"  Timestamp: {datapoint.Timestamp:yyyy-MM-dd HH:mm:ss}");
                    if (datapoint.Sum.HasValue)
                        Console.WriteLine($"    Sum: {datapoint.Sum:N2}");
                    if (datapoint.Average.HasValue)
                        Console.WriteLine($"    Average: {datapoint.Average:N2}");
                    if (datapoint.Maximum.HasValue)
                        Console.WriteLine($"    Maximum: {datapoint.Maximum:N2}");
                }
            }
            else
            {
                Console.WriteLine($"\n{metricName}: No data available");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n{metricName}: Error retrieving metrics - {ex.Message}");
        }
    }
}