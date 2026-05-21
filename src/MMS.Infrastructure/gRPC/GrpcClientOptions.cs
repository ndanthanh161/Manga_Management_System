namespace MMS.Infrastructure.gRPC;

public class GrpcClientOptions
{
    public string ImageProcessorEndpoint { get; set; } = "https://localhost:5101";
    public string NotificationEndpoint { get; set; } = "https://localhost:5102";
    public string AuditLogEndpoint { get; set; } = "https://localhost:5103";
    public int MaxReceiveMessageSizeMb { get; set; } = 64;  // 64MB cho image stream
    public int RetryCount { get; set; } = 3;
    public int DefaultDeadlineSeconds { get; set; } = 30;
}
