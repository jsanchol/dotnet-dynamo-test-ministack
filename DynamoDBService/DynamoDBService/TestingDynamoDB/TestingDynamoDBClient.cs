using System.Diagnostics;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;

internal class TestingDynamoDBClient // TODO: Implementing a base class for AmazonClient <T>
{
    internal AmazonDynamoDBClient client;
    internal string endpoint;
    private string AccessKey;
    private string SecretKey;

    internal TestingDynamoDBClient()
    {
        //TODO: move this to a config file and use dependency injection for better flexibility and testability, especially when we expand to other AWS services like Lambda and RDS.
        endpoint = Environment.GetEnvironmentVariable("DYNAMODB_ENDPOINT") ?? "http://localhost:4566";
        AccessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID") ?? "test";
        SecretKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY") ?? "test";
        client = SetUpClient();
    }

    //SetUp dynamo test service
    internal AmazonDynamoDBClient SetUpClient()
    {
        // Configure DynamoDB client for Ministack
        
        AmazonDynamoDBConfig dynamoDb = new()
        {
            ServiceURL = endpoint,
            AuthenticationRegion = "us-east-1"
        };
        
        AWSCredentials basicCredentials = new BasicAWSCredentials(AccessKey, SecretKey);
        return new AmazonDynamoDBClient(basicCredentials, dynamoDb);;
    }

    internal async Task WaitForTableActiveAsync(string tableName)
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

    internal async Task CreateGSIProvisionedTableAsync(string tableName)
    {
        var request = new CreateTableRequest
        {
            TableName = tableName,
            KeySchema = new List<KeySchemaElement>
            {
                new KeySchemaElement { AttributeName = "PK", KeyType = KeyType.HASH },
                new KeySchemaElement { AttributeName = "SK", KeyType = KeyType.RANGE }
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
                        new KeySchemaElement { AttributeName = "GSI_PK", KeyType = KeyType.HASH }
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

        // Wait for table to be active
        await WaitForTableActiveAsync(tableName);
    }

    //When populated the DynamoDB table with test data and running performance tests
    internal async Task PutSmallItemTestDataAsync(string tableName, int sampleNumber)
    {
        var stopwatch = Stopwatch.StartNew();
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
        }
        stopwatch.Stop();
        Console.WriteLine($"Test data inserted: {sampleNumber} items in {stopwatch.ElapsedMilliseconds} ms.");
    }

    internal async Task PutLargeItemTestDataAsync(string tableName, int sampleNumber)
    {
        var stopwatch = Stopwatch.StartNew();
        for (int i = 0; i < sampleNumber; i++)
        {
            var item = new Dictionary<string, AttributeValue>
            {
                ["PK"] = new AttributeValue { S = $"User{i % 10}" },
                ["SK"] = new AttributeValue { S = $"Item{i}" },
                ["GSI_PK"] = new AttributeValue { S = $"Category{i % 5}" },
                ["Data"] = new AttributeValue { S = $"Data for item {i}" },
                ["LargeData"] = new AttributeValue { S = $"Large data for item {i} NsQlhbisDW5JVlLSaZVtCLSUUrkBijbkc5f9gFFscDkoGnN0J6GgIFqdCLyhbdWLHxRVY8IwDCrWF555JeY0yD0GtgH21NotZAEeiWJR1A4 bxqq9VKKAzMJ0tW7TCOqNtMzVtPB6NrtCIg8NSmhrO7QjNcOzi4N b VGc0HB5HMNXdyEoWroU464ChM5R Lqdsm3iPo1mz0cPKqobhjDYkvRs5LZO8n92GxEKGeCtt oX53Qu6T7O2E9nJLKoUeJI6Ul7keLsNGI2BC55qs7fhqW8eFDsGsLPaImF7kFJizNsQlhbisDW5JVlLSaZVtCLSUUrkBijbkc5f9gFFscDkoGnN0J6GgIFqdCLyhbdWLHxRVY8IwDCrWF555JeY0yD0GtgH21NotZAEeiWJR1A4 bxqq9VKKAzMJ0tW7TCOqNtMzVtPB6NrtCIg8NSmhrO7QjNcOzi4N b VGc0HB5HMNXdyEoWroU464ChM5R Lqdsm3iPo1mz0cPKqobhjDYkvRs5LZO8n92GxEKGeCtt oX53Qu6T7O2E9nJLKoUeJI6Ul7keLsNGI2BC55qs7fhqW8eFDsGsLPaImF7kFJizNsQlhbisDW5JVlLSaZVtCLSUUrkBijbkc5f9gFFscDkoGnN0J6GgIFqdCLyhbdWLHxRVY8IwDCrWF555JeY0yD0GtgH21NotZAEeiWJR1A4 bxqq9VKKAzMJ0tW7TCOqNtMzVtPB6NrtCIg8NSmhrO7QjNcOzi4N b VGc0HB5HMNXdyEoWroU464ChM5R Lqdsm3iPo1mz0cPKqobhjDYkvRs5LZO8n92GxEKGeCtt oX53Qu6T7O2E9nJLKoUeJI6Ul7keLsNGI2BC55qs7fhqW8eFDsGsLPaImF7kFJizNsQlhbisDW5JVlLSaZVtCLSUUrkBijbkc5f9gFFscDkoGnN0J6GgIFqdCLyhbdWLHxRVY8IwDCrWF555JeY0yD0GtgH21NotZAEeiWJR1A4 bxqq9VKKAzMJ0tW7TCOqNtMzVtPB6NrtCIg8NSmhrO7QjNcOzi4N b VGc0HB5HMNXdyEoWroU464ChM5R Lqdsm3iPo1mz0cPKqobhjDYkvRs5LZO8n92GxEKGeCtt oX53Qu6T7O2E9nJLKoUeJI6Ul7keLsNGI2BC55qs7fhqW8eFDsGsLPaImF7kFJizNsQlhbisDW5JVlLSaZVtCLSUUrkBijbkc5f9gFFscDkoGnN0J6GgIFqdCLyhbdWLHxRVY8IwDCrWF555JeY0yD0GtgH21NotZAEeiWJR1A4 bxqq9VKKAzMJ0tW7TCOqNtMzVtPB6NrtCIg8NSmhrO7QjNcOzi4N b VGc0HB5HMNXdyEoWroU464ChM5R Lqdsm3iPo1mz0cPKqobhjDYkvRs5LZO8n92GxEKGeCtt oX53Qu6T7O2E9nJLKoUeJI6Ul7keLsNGI2BC55qs7fhqW8eFDsGsLPaImF7kFJizNsQlhbisDW5JVlLSaZVtCLSUUrkBijbkc5f9gFFscDkoGnN0J6GgIFqdCLyhbdWLHxRVY8IwDCrWF555JeY0yD0GtgH21NotZAEeiWJR1A4 bxqq9VKKAzMJ0tW7TCOqNtMzVtPB6NrtCIg8NSmhrO7QjNcOzi4N b VGc0HB5HMNXdyEoWroU464ChM5R Lqdsm3iPo1mz0cPKqobhjDYkvRs5LZO8n92GxEKGeCtt oX53Qu6T7O2E9nJLKoUeJI6Ul7keLsNGI2BC55qs7fhqW8eFDsGsLPaImF7kFJizNsQlhbisDW5JVlLSaZVtCLSUUrkBijbkc5f9gFFscDkoGnN0J6GgIFqdCLyhbdWLHxRVY8IwDCrWF555JeY0yD0GtgH21NotZAEeiWJR1A4 bxqq9VKKAzMJ0tW7TCOqNtMzVtPB6NrtCIg8NSmhrO7QjNcOzi4N b VGc0HB5HMNXdyEoWroU464ChM5R Lqdsm3iPo1mz0cPKqobhjDYkvRs5LZO8n92GxEKGeCtt oX53Qu6T7O2E9nJLKoUeJI6Ul7keLsNGI2BC55qs7fhqW8eFDsGsLPaImF7kFJizNsQlhbisDW5JVlLSaZVtCLSUUrkBijbkc5f9gFFscDkoGnN0J6GgIFqdCLyhbdWLHxRVY8IwDCrWF555JeY0yD0GtgH21NotZAEeiWJR1A4 bxqq9VKKAzMJ0tW7TCOqNtMzVtPB6NrtCIg8NSmhrO7QjNcOzi4N b VGc0HB5HMNXdyEoWroU464ChM5R Lqdsm3iPo1mz0cPKqobhjDYkvRs5LZO8n92GxEKGeCtt oX53Qu6T7O2E9nJLKoUeJI6Ul7keLsNGI2BC55qs7fhqW8eFDsGsLPaImF7kFJizNsQlhbisDW5JVlLSaZVtCLSUUrkBijbkc5f9gFFscDkoGnN0J6GgIFqdCLyhbdWLHxRVY8IwDCrWF555JeY0yD0GtgH21NotZAEeiWJR1A4 bxqq9VKKAzMJ0tW7TCOqNtMzVtPB6NrtCIg8NSmhrO7QjNcOzi4N b VGc0HB5HMNXdyEoWroU464ChM5R Lqdsm3iPo1mz0cPKqobhjDYkvRs5LZO8n92GxEKGeCtt oX53Qu6T7O2E9nJLKoUeJI6Ul7keLsNGI2BC55qs7fhqW8eFDsGsLPaImF7kFJizNsQlhbisDW5JVlLSaZVtCLSUUrkBijbkc5f9gFFscDkoGnN0J6GgIFqdCLyhbdWLHxRVY8IwDCrWF555JeY0yD0GtgH21NotZAEeiWJR1A4 bxqq9VKKAzMJ0tW7TCOqNtMzVtPB6NrtCIg8NSmhrO7QjNcOzi4N b VGc0HB5HMNXdyEoWroU464ChM5R Lqdsm3iPo1mz0cPKqobhjDYkvRs5LZO8n92GxEKGeCtt oX53Qu6T7O2E9nJLKoUeJI6Ul7keLsNGI2BC55qs7fhqW8eFDsGsLPaImF7kFJizNsQlhbisDW5JVlLSaZVtCLSUUrkBijbkc5f9gFFscDkoGnN0J6GgIFqdCLyhbdWLHxRVY8IwDCrWF555JeY0yD0GtgH21NotZAEeiWJR1A4 bxqq9VKKAzMJ0tW7TCOqNtMzVtPB6NrtCIg8NSmhrO7QjNcOzi4N b VGc0HB5HMNXdyEoWroU464ChM5R Lqdsm3iPo1mz0cPKqobhjDYkvRs5LZO8n92GxEKGeCtt oX53Qu6T7O2E9nJLKoUeJI6Ul7keLsNGI2BC55qs7fhqW8eFDsGsLPaImF7kFJizNsQlhbisDW5JVlLSaZVtCLSUUrkBijbkc5f9gFFscDkoGnN0J6GgIFqdCLyhbdWLHxRVY8IwDCrWF555JeY0yD0GtgH21NotZAEeiWJR1A4 bxqq9VKKAzMJ0tW7TCOqNtMzVtPB6NrtCIg8NSmhrO7QjNcOzi4N b VGc0HB5HMNXdyEoWroU464ChM5R Lqdsm3iPo1mz0cPKqobhjDYkvRs5LZO8n92GxEKGeCtt oX53Qu6T7O2E9nJLKoUeJI6Ul7keLsNGI2BC55qs7fhqW8eFDsGsLPaImF7kFJizNsQlhbisDW5JVlLSaZVtCLSUUrkBijbkc5f9gFFscDkoGnN0J6GgIFqdCLyhbdWLHxRVY8IwDCrWF555JeY0yD0GtgH21NotZAEeiWJR1A4 bxqq9VKKAzMJ0tW7TCOqNtMzVtPB6NrtCIg8NSmhrO7QjNcOzi4N b VGc0HB5HMNXdyEoWroU464ChM5R Lqdsm3iPo1mz0cPKqobhjDYkvRs5LZO8n92GxEKGeCtt oX53Qu6T7O2E9nJLKoUeJI6Ul7keLsNGI2BC55qs7fhqW8eFDsGsLPaImF7kFJizNsQlhbisDW5JVlLSaZVtCLSUUrkBijbkc5f9gFFscDkoGnN0J6GgIFqdCLyhbdWLHxRVY8IwDCrWF555JeY0yD0GtgH21NotZAEeiWJR1A4 bxqq9VKKAzMJ0tW7TCOqNtMzVtPB6NrtCIg8NSmhrO7QjNcOzi4N b VGc0HB5HMNXdyEoWroU464ChM5R Lqdsm3iPo1mz0cPKqobhjDYkvRs5LZO8n92GxEKGeCtt oX53Qu6T7O2E9nJLKoUeJI6Ul7keLsNGI2BC55qs7fhqW8eFDsGsLPaImF7kFJizNsQlhbisDW5JVlLSaZVtCLSUUrkBijbkc5f9gFFscDkoGnN0J6GgIFqdCLyhbdWLHxRVY8IwDCrWF555JeY0yD0GtgH21NotZAEeiWJR1A4 bxqq9VKKAzMJ0tW7TCOqNtMzVtPB6NrtCIg8NSmhrO7QjNcOzi4N b VGc0HB5HMNXdyEoWroU464ChM5R Lqdsm3iPo1mz0cPKqobhjDYkvRs5LZO8n92GxEKGeCtt oX53Qu6T7O2E9nJLKoUeJI6Ul7keLsNGI2BC55qs7fhqW8eFDsGsLPaImF7kFJizNsQlhbisDW5JVlLSaZVtCLSUUrkBijbkc5f9gFFscDkoGnN0J6GgIFqdCLyhbdWLHxRVY8IwDCrWF555JeY0yD0GtgH21NotZAEeiWJR1A4 " }
            };
            //LargeData is more than 4KB
            //TODO: create function to generate large data string based on specified size in KB for more comprehensive testing instead of hardcoding it.

            await client.PutItemAsync(tableName, item);
        }
        stopwatch.Stop();
        Console.WriteLine($"Test data inserted: {sampleNumber} items in {stopwatch.ElapsedMilliseconds} ms.");
    }

    internal async Task CreateGSIOnDemandTableAsync(string tableName)
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
}
