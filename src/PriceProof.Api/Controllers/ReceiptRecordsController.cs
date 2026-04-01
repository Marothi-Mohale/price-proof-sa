using Microsoft.AspNetCore.Mvc;
using PriceProof.Application.Abstractions.Services;
using PriceProof.Application.ReceiptRecords;

namespace PriceProof.Api.Controllers;

[ApiController]
[Route("receipt-records")]
public sealed class ReceiptRecordsController : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(ReceiptRecordDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ReceiptRecordDto>> CreateAsync(
        [FromBody] CreateReceiptRecordRequest request,
        [FromServices] IReceiptRecordService receiptRecordService,
        CancellationToken cancellationToken)
    {
        var result = await receiptRecordService.CreateAsync(request, cancellationToken);
        return Created($"/receipt-records/{result.Id}", result);
    }

    [HttpPost("{id:guid}/run-ocr")]
    [ProducesResponseType(typeof(RunReceiptOcrResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<RunReceiptOcrResultDto>> RunOcrAsync(
        Guid id,
        [FromServices] IReceiptRecordService receiptRecordService,
        CancellationToken cancellationToken)
    {
        var result = await receiptRecordService.RunOcrAsync(id, cancellationToken);
        return Ok(result);
    }
}
