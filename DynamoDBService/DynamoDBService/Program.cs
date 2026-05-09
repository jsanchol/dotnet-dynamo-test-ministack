class Program
{
    static async Task Main(string[] args)
    {
        bool resetMinistack = Environment.GetEnvironmentVariable("DYNAMODB_RESET") == "true";

        Console.WriteLine(resetMinistack
            ? "DYNAMODB_RESET is set to true. Ministack will be reset before running tests."
            : "DYNAMODB_RESET is not set to true. Ministack will NOT be reset before running tests.");

        TestDynamoDBService testService = new();

        if(resetMinistack)
        {
            Console.WriteLine("Resetting Ministack...");
            await testService.ResetMinistackAsync();
        }

        int iteration = Environment.GetEnvironmentVariable("TEST_ITERATIONS") != null ? int.Parse(Environment.GetEnvironmentVariable("TEST_ITERATIONS")!) : 1;

        List<Task> setupTasks = [];

        while (iteration-- > 0)
        {
            setupTasks.AddRange(
            [
                testService.RunTestsTableGSIProvisioned(),
                testService.RunTestsTableGSIOnDemand()
            ]);
        }

        await Task.WhenAll(setupTasks);

        // Plan for more comprehensive testing
        Console.WriteLine("\nPerformance Testing Plan:");
        Console.WriteLine("2. Run multiple iterations (e.g., 10 runs at the same time) and calculate average, min, max times.");
        Console.WriteLine("3. Test with different RCUs (Read Capacity Units) to simulate throttling.");
        Console.WriteLine("4. Use Custom scripts for load testing IE docker compose services.");
        Console.WriteLine("5. Monitor CPU, memory usage during tests.");
        Console.WriteLine("6. Compare Query vs Scan vs GSI Query in terms of latency and throughput.");
        Console.WriteLine("7. Test with filters, projections to see impact.");
        Console.WriteLine("8. Add reset call of ministack to clear data between tests for consistency.");
        // adding size in KB being tested for more comprehensive performance testing.
        // using IDynamoDBContext
        // hot partition key vs???? cold partition key testing hash and range

        // refactoring code to be more modular and reusable for different test cases like other AWS services.
        //NEXT: lambda and RDS
    }
}
