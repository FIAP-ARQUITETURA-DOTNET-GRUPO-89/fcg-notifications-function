namespace FcgNotifications.Bridge;

public class BridgeSettings
{
    public string LambdaEndpointUrl { get; set; } = "http://localstack:4566";

    public string LambdaFunctionName { get; set; } = "fcg-notifications-function";

    public string AwsRegion { get; set; } = "us-east-1";
}
