using System.Diagnostics;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using Amazon.Runtime;

internal class TestingCloudWatchClient
{
    internal AmazonCloudWatchClient client;
    internal string endpoint;
    private string AccessKey;
    private string SecretKey;

    internal TestingCloudWatchClient()
    {
        endpoint = Environment.GetEnvironmentVariable("CLOUDWATCH_ENDPOINT") ?? "http://localhost:4566";
        AccessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID") ?? "test";
        SecretKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY") ?? "test";
        client = SetUpClient();
    }

    internal AmazonCloudWatchClient SetUpClient()
    {
        AmazonCloudWatchConfig cloudWatchConfig = new()
        {
            ServiceURL = endpoint,
            AuthenticationRegion = "us-east-1"
        };

        AWSCredentials basicCredentials = new BasicAWSCredentials(AccessKey, SecretKey);
        return new AmazonCloudWatchClient(basicCredentials, cloudWatchConfig);
    }
}