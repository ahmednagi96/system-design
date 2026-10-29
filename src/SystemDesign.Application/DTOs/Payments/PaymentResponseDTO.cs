namespace SystemDesign.Application.DTOs.Payments;

public record PaymentResponseDTO
{
    public int UserId { get; init; }
    public decimal Amount { get; init; }
    public string Status { get; init; } = "Success";
    public string TransactionId { get; init; } = Guid.NewGuid().ToString();
    public DateTime TransactionDate { get; init; } = DateTime.UtcNow;
}
