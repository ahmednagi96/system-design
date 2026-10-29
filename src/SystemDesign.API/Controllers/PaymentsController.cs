using Microsoft.AspNetCore.Mvc;
using SystemDesign.API.Filters;
using SystemDesign.Application.DTOs.Payments;
namespace SystemDesign.API.Controllers;


[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    [HttpPost("charge")]
    [Idempotent(cacheHours: 24)] // <-- السحر كله هنا
    public  IActionResult ChargeCard([FromBody] PaymentRequestDTO request)
    {
        var payment = new PaymentResponseDTO { UserId = request.UserId, Amount = request.Amount };
        return Ok(payment);
    }
}