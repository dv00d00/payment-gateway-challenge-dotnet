namespace PaymentGateway.Api.Models.Responses;

public class ClientErrorResponse
{
    public required IReadOnlyCollection<Issue> Issues { get; init; }
    
    public class Issue
    {
        public required string ErrorCode { get; init; }
        public required string ErrorMessage { get; init; }
    }
}