using Microsoft.AspNetCore.Mvc;
using PriceProof.Application.Abstractions.Services;
using PriceProof.Application.PaymentRecords;

namespace PriceProof.Api.Controllers;

[ApiController]
[Route("payment-records")]
public sealed class PaymentRecordsController : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(PaymentRecordDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<PaymentRecordDto>> CreateAsync(
        [FromBody] CreatePaymentRecordRequest request,
        [FromServices] IPaymentRecordService paymentRecordService,
        CancellationToken cancellationToken)
    {
        var result = await paymentRecordService.CreateAsync(request, cancellationToken);
        return Created($"/payment-records/{result.Id}", result);
    }
}
