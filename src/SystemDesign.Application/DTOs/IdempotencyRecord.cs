namespace SystemDesign.Application.DTOs;

public sealed class IdempotencyRecord
{
    public string Status { get; set; } = string.Empty; // "InFlight" or "Completed"
    public string RequestHash { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public string? ResponseBody { get; set; }
}