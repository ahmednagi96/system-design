namespace SystemDesign.Application.DTOs.Payments;

public class PaymentRequestDTO
{
    public int UserId { get; set; }
    public decimal Amount { get; set; }
}
