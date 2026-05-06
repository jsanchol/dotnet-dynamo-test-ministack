using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using System.Diagnostics;
using Amazon.Runtime;

class Program
{
    static async Task Main(string[] args)
    {
        AmazonDynamoDBClient client = SetUpDynamoClient();

        string tableName = "TestTableGSIProvisioned";

        // Create table
        await CreateGSIProvisionedTableAsync(client, tableName);

        // GSI+Provisioned throughput with partition key only (no sort key) to test performance of queries on GSI without sort key
        await RunTestGSIProvisionedAsync(client, tableName);

        tableName = "TestTableGSIOnDemand";

        // Create table
        await CreateGSIOnDemandTableAsync(client, tableName);

        // GSI+Provisioned throughput with partition key only (no sort key) to test performance of queries on GSI without sort key
        await RunTestGSIProvisionedAsync(client, tableName);

        // Plan for more comprehensive testing
        Console.WriteLine("\nPerformance Testing Plan:");
        Console.WriteLine("1. Increase dataset size to 1,000-10,000+ items for more realistic testing.");
        Console.WriteLine("2. Run multiple iterations (e.g., 10 runs) and calculate average, min, max times.");
        Console.WriteLine("3. Test with different RCUs (Read Capacity Units) to simulate throttling.");
        Console.WriteLine("4. Use Custom scripts for load testing IE docker compose services.");
        Console.WriteLine("5. Monitor CPU, memory usage during tests.");
        Console.WriteLine("6. Compare Query vs Scan vs GSI Query in terms of latency and throughput.");
        Console.WriteLine("7. Test with filters, projections to see impact.");
        Console.WriteLine("8. Add reset call of ministack to clear data between tests for consistency.");
    }

    private static AmazonDynamoDBClient SetUpDynamoClient()
    {
        // Configure DynamoDB client for Ministack
        string endpoint = Environment.GetEnvironmentVariable("DYNAMODB_ENDPOINT") ?? "http://localhost:4566";
        AmazonDynamoDBConfig dynamoDb = new()
        {
            ServiceURL = endpoint,
            AuthenticationRegion = "us-east-1"
        };
        string AccessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID") ?? "test";
        string SecretKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY") ?? "test";
        AWSCredentials basicCredentials = new BasicAWSCredentials(AccessKey, SecretKey);
        return new AmazonDynamoDBClient(basicCredentials, dynamoDb);
    }

    private static async Task RunTestGSIProvisionedAsync(AmazonDynamoDBClient client, string tableName)
    {
        // Put sample items small test data
        Console.WriteLine($"Inserting small test data for table {tableName}.");
        
        //sampleFactor is used to increase the number of items inserted for testing performance with larger datasets. Adjust as needed.
        int sampleFactor = 1000; // Start with 1000 items, increase to 10,000 or more for more comprehensive testing
        await PutTestDataAsync(client, tableName, sampleFactor);

        // Performance tests
        await RunPerformanceTestsAsync(client, tableName);

        // Then 10k items or more for more comprehensive testing
        await PutTestDataAsync(client, tableName, sampleFactor * 10);

        // Performance tests
        await RunPerformanceTestsAsync(client, tableName);

        //Then with 100k items or more for more comprehensive testing
        await PutTestDataAsync(client, tableName, sampleFactor * 100);

        // Performance tests
        await RunPerformanceTestsAsync(client, tableName);

        //Then with 1M items or more for more comprehensive testing
        await PutTestDataAsync(client, tableName, sampleFactor * 1000);

        // Performance tests
        await RunPerformanceTestsAsync(client, tableName);
    }

    static async Task CreateGSIProvisionedTableAsync(AmazonDynamoDBClient client, string tableName)
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

    static async Task CreateGSIOnDemandTableAsync(AmazonDynamoDBClient client, string tableName)
    {
        var request = new CreateTableRequest
        {
            TableName = tableName,
            BillingMode = BillingMode.PAY_PER_REQUEST,
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
                    Projection = new Projection { ProjectionType = "ALL" }
                    // No ProvisionedThroughput needed for PAY_PER_REQUEST
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

    static async Task PutTestDataAsync(AmazonDynamoDBClient client, string tableName, int sampleNumber)
    {
        for (int i = 0; i < sampleNumber; i++)
        {
            var item = new Dictionary<string, AttributeValue>
            {
                ["PK"] = new AttributeValue { S = $"User{i % 10}" },
                ["SK"] = new AttributeValue { S = $"Item{i}" },
                ["GSI_PK"] = new AttributeValue { S = $"Category{i % 5}" },
                ["Data"] = new AttributeValue { S = $"Data for item {i}" }
            };

            await client.PutItemAsync(tableName, item);
            //Console.WriteLine("TODO: Implement metrics on client.");
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
        Console.WriteLine($"PK Query time: {stopwatch.ElapsedMilliseconds} ms, Items: {queryResponse.Items.Count}");

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
    }
}
