using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.DynamoDBv2.DocumentModel;
using System.Diagnostics;

class Program
{
    static async Task Main(string[] args)
    {
        // Configure DynamoDB client for Ministack
        string endpoint = Environment.GetEnvironmentVariable("DYNAMODB_ENDPOINT") ?? "http://localhost:4566";
        var client = new AmazonDynamoDBClient(new AmazonDynamoDBConfig
        {
            ServiceURL = endpoint,
            AuthenticationRegion = "us-east-1"
        });

        string tableName = "TestTable";

        // Create table
        await CreateTableAsync(client, tableName);

        // Wait for table to be active
        await WaitForTableActiveAsync(client, tableName);

        // Put some test data
        await PutTestDataAsync(client, tableName);

        // Performance tests
        await RunPerformanceTestsAsync(client, tableName);
    }

    static async Task CreateTableAsync(AmazonDynamoDBClient client, string tableName)
    {
        var request = new CreateTableRequest
        {
            TableName = tableName,
            KeySchema = new List<KeySchemaElement>
            {
                new KeySchemaElement { AttributeName = "PK", KeyType = "HASH" },
                new KeySchemaElement { AttributeName = "SK", KeyType = "RANGE" }
            },
            AttributeDefinitions = new List<AttributeDefinition>
            {
                new AttributeDefinition { AttributeName = "PK", AttributeType = "S" },
                new AttributeDefinition { AttributeName = "SK", AttributeType = "S" },
                new AttributeDefinition { AttributeName = "GSI_PK", AttributeType = "S" }
            },
            GlobalSecondaryIndexes = new List<GlobalSecondaryIndex>
            {
                new GlobalSecondaryIndex
                {
                    IndexName = "GSI1",
                    KeySchema = new List<KeySchemaElement>
                    {
                        new KeySchemaElement { AttributeName = "GSI_PK", KeyType = "HASH" }
                    },
                    Projection = new Projection { ProjectionType = "ALL" },
                    ProvisionedThroughput = new ProvisionedThroughput { ReadCapacityUnits = 5, WriteCapacityUnits = 5 }
                }
            },
            ProvisionedThroughput = new ProvisionedThroughput { ReadCapacityUnits = 5, WriteCapacityUnits = 5 }
        };

        try
        {
            await client.CreateTableAsync(request);
            Console.WriteLine("Table created successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Table creation failed: {ex.Message}");
        }
    }

    static async Task WaitForTableActiveAsync(AmazonDynamoDBClient client, string tableName)
    {
        var request = new DescribeTableRequest { TableName = tableName };
        while (true)
        {
            var response = await client.DescribeTableAsync(request);
            if (response.Table.TableStatus == TableStatus.ACTIVE)
                break;
            await Task.Delay(1000);
        }
        Console.WriteLine("Table is active.");
    }

    static async Task PutTestDataAsync(AmazonDynamoDBClient client, string tableName)
    {
        for (int i = 0; i < 100; i++)
        {
            var item = new Dictionary<string, AttributeValue>
            {
                ["PK"] = new AttributeValue { S = $"User{i % 10}" },
                ["SK"] = new AttributeValue { S = $"Item{i}" },
                ["GSI_PK"] = new AttributeValue { S = $"Category{i % 5}" },
                ["Data"] = new AttributeValue { S = $"Data for item {i}" }
            };

            await client.PutItemAsync(tableName, item);
        }
        Console.WriteLine("Test data inserted.");
    }

    static async Task RunPerformanceTestsAsync(AmazonDynamoDBClient client, string tableName)
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
        Console.WriteLine($"Query time: {stopwatch.ElapsedMilliseconds} ms, Items: {queryResponse.Items.Count}");

        // Scan
        var scanRequest = new ScanRequest
        {
            TableName = tableName
        };

        stopwatch.Restart();
        var scanResponse = await client.ScanAsync(scanRequest);
        stopwatch.Stop();
        Console.WriteLine($"Scan time: {stopwatch.ElapsedMilliseconds} ms, Items: {scanResponse.Items.Count}");

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

        // Plan for more comprehensive testing
        Console.WriteLine("\nPerformance Testing Plan:");
        Console.WriteLine("1. Increase dataset size to 10,000+ items for more realistic testing.");
        Console.WriteLine("2. Run multiple iterations (e.g., 10 runs) and calculate average, min, max times.");
        Console.WriteLine("3. Test with different RCUs (Read Capacity Units) to simulate throttling.");
        Console.WriteLine("4. Use tools like JMeter or custom scripts for load testing.");
        Console.WriteLine("5. Monitor CPU, memory usage during tests.");
        Console.WriteLine("6. Compare Query vs Scan vs GSI Query in terms of latency and throughput.");
        Console.WriteLine("7. Test with filters, projections to see impact.");
        Console.WriteLine("8. Use AWS X-Ray or custom logging for detailed tracing.");
    }
}
