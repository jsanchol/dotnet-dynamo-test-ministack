using System.Diagnostics;
using Amazon.DynamoDBv2.Model;

internal class TestDynamoDBService : TestingDynamoDBClient
{
    internal const string NameTestTableGSIProvisioned = "TestTableGSIProvisioned";
    internal const string NameTestTableGSIOnDemand = "TestTableGSIOnDemand";

    //Given a containerized environment with Ministack running
    public TestDynamoDBService()
    {
    }

    //Then Runnning tests against Provisioned the DynamoDB service table
    internal async Task RunTestsTableGSIProvisioned(string tableName = NameTestTableGSIProvisioned)
    {
        // Create table
        await CreateGSIProvisionedTableAsync(tableName);

        await MonitorTableAsync(tableName);

        // GSI+Provisioned throughput with partition key only (no sort key) to test performance of queries on GSI without sort key
        await RunTestGSIAsync(tableName);
    }

    internal async Task MonitorTableAsync(string tableName)
    {
        Console.WriteLine($"Monitoring table {tableName} for RCUs and WCUs...");

        // This is a placeholder for monitoring logic. In a real implementation, you would use CloudWatch metrics or DynamoDB's DescribeTable API to monitor RCUs and WCUs.
        // For example, you could periodically call DescribeTable and log the ProvisionedThroughput and ConsumedCapacity.

        // Example of using DescribeTable to get current provisioned throughput
        var describeResponse = await client.DescribeTableAsync(new DescribeTableRequest
        {
            TableName = tableName
        });

        Console.WriteLine($"Initial provisioned RCUs: {describeResponse.Table.ProvisionedThroughput.ReadCapacityUnits}");
        Console.WriteLine($"Initial provisioned WCUs: {describeResponse.Table.ProvisionedThroughput.WriteCapacityUnits}");
    }

    //Then Runnning tests against On-Demand the DynamoDB service table
    internal async Task RunTestsTableGSIOnDemand(string tableName = NameTestTableGSIOnDemand)
    {
        // Create table
        await CreateGSIOnDemandTableAsync(tableName);

        // GSI+Provisioned throughput with partition key only (no sort key) to test performance of queries on GSI without sort key
        await RunTestGSIAsync(tableName);
    }

    internal async Task RunTestGSIAsync(string tableName)
    {
        // Put sample items small test data
        Console.WriteLine($"Inserting small test data for table {tableName}.");

        //sampleFactor is used to increase the number of items inserted for testing performance with larger datasets. Adjust as needed.
        int sampleFactor = 1000; // Start with 1000 items, increase to 10,000 or more for more comprehensive testing
        await PutSmallItemTestDataAsync(tableName, sampleFactor);

        // Performance tests
        await RunQueryTestsAsync(tableName);
        await RunPaginationQueryTestsAsync(tableName);
        await RunScanTestsAsync(tableName);

        // // Then 10k items or more for more comprehensive testing
        // await PutSmallItemTestDataAsync(tableName, sampleFactor * 10);
        // //Then with 100k items or more for more comprehensive testing
        // await PutSmallItemTestDataAsync(tableName, sampleFactor * 100);
        // //Then with 1M items or more for more comprehensive testing
        // await PutSmallItemTestDataAsync(tableName, sampleFactor * 1000);
    }

    internal async Task RunQueryTestsAsync(string tableName)
    {
        Console.WriteLine("Running performance tests...");

        // Query on partition key
        var queryRequest = new QueryRequest
        {
            TableName = tableName,
            KeyConditionExpression = "PK = :pk",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":pk"] = new AttributeValue { S = "User0" }
            }
        };

        var stopwatch = Stopwatch.StartNew();
        var queryResponse = await client.QueryAsync(queryRequest);
        stopwatch.Stop();
        Console.WriteLine($"PK Query time: {stopwatch.ElapsedMilliseconds} ms, Items: {queryResponse.Items.Count}");

        // Query on GSI
        var gsiQueryRequest = new QueryRequest
        {
            TableName = tableName,
            IndexName = "GSI1",
            KeyConditionExpression = "GSI_PK = :gsi_pk",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":gsi_pk"] = new AttributeValue { S = "Category0" }
            }
        };

        stopwatch.Restart();
        var gsiQueryResponse = await client.QueryAsync(gsiQueryRequest);
        stopwatch.Stop();
        Console.WriteLine($"GSI Query time: {stopwatch.ElapsedMilliseconds} ms, Items: {gsiQueryResponse.Items.Count}");
    }

    internal async Task RunScanTestsAsync(string tableName)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        // Scan
        var scanRequest = new ScanRequest
        {
            TableName = tableName
        };

        stopwatch.Restart();
        var scanResponse = await client.ScanAsync(scanRequest);
        stopwatch.Stop();
        Console.WriteLine($"Scan time: {stopwatch.ElapsedMilliseconds} ms, Items: {scanResponse.Items.Count}");
    }

    internal async Task RunPaginationQueryTestsAsync(string tableName)
    {
        Console.WriteLine("Running pagination query tests...");

        var queryRequest = new QueryRequest
        {
            TableName = tableName,
            KeyConditionExpression = "PK = :pk",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":pk"] = new AttributeValue { S = "User0" }
            },
            Limit = 100 // Adjust page size as needed
        };

        string? lastEvaluatedKey = null;
        int totalItems = 0;
        var stopwatch = Stopwatch.StartNew();

        do
        {
            if (lastEvaluatedKey != null)
            {
                queryRequest.ExclusiveStartKey = new Dictionary<string, AttributeValue>
                {
                    ["PK"] = new AttributeValue { S = "User0" },
                    ["SK"] = new AttributeValue { S = lastEvaluatedKey }
                };
            }

            var queryResponse = await client.QueryAsync(queryRequest);
            totalItems += queryResponse.Items.Count;
            lastEvaluatedKey = queryResponse.LastEvaluatedKey.ContainsKey("SK") ? queryResponse.LastEvaluatedKey["SK"].S : null;
        } while (lastEvaluatedKey != null);

        stopwatch.Stop();
        Console.WriteLine($"Total items retrieved: {totalItems}, Total time: {stopwatch.ElapsedMilliseconds} ms");
    }
}