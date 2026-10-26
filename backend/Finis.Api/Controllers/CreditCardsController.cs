using Finis.Api.DTOs;
using Finis.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Finis.Api.Controllers;

[Authorize]
public class CreditCardsController : BaseApiController
{
    private readonly ICreditCardService _cardService;

    public CreditCardsController(ICreditCardService cardService)
    {
        _cardService = cardService;
    }

    [HttpGet]
    public async Task<ActionResult<List<CreditCardResponseDto>>> GetCards()
    {
        var userId = GetUserId();
        var cards = await _cardService.GetCardsAsync(userId);
        return Ok(cards);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CreditCardResponseDto>> GetById(int id)
    {
        var userId = GetUserId();
        var card = await _cardService.GetCardByIdAsync(userId, id);
        if (card == null)
        {
            return NotFound(new { message = "Cartão de crédito não encontrado." });
        }
        return Ok(card);
    }

    [HttpPost]
    public async Task<ActionResult<CreditCardResponseDto>> Create([FromBody] CreditCardCreateUpdateDto dto)
    {
        var userId = GetUserId();
        var card = await _cardService.CreateCardAsync(userId, dto);
        return CreatedAtAction(nameof(GetById), new { id = card.Id }, card);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<CreditCardResponseDto>> Update(int id, [FromBody] CreditCardCreateUpdateDto dto)
    {
        var userId = GetUserId();
        var card = await _cardService.UpdateCardAsync(userId, id, dto);
        if (card == null)
        {
            return NotFound(new { message = "Cartão de crédito não encontrado." });
        }
        return Ok(card);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetUserId();
        var success = await _cardService.DeleteCardAsync(userId, id);
        if (!success)
        {
            return NotFound(new { message = "Cartão de crédito não encontrado." });
        }
        return NoContent();
    }

    [HttpGet("{id}/invoices/{year}/{month}")]
    public async Task<ActionResult<CreditCardInvoiceSummaryDto>> GetInvoice(int id, int year, int month)
    {
        var userId = GetUserId();
        var invoice = await _cardService.GetInvoiceSummaryAsync(userId, id, year, month);
        if (invoice == null)
        {
            return NotFound(new { message = "Cartão ou fatura não encontrada." });
        }
        return Ok(invoice);
    }

    [HttpGet("{id}/invoices/upcoming")]
    public async Task<ActionResult<List<CreditCardInvoiceSummaryDto>>> GetUpcomingInvoices(int id, [FromQuery] int count = 6)
    {
        var userId = GetUserId();
        var invoices = await _cardService.GetUpcomingInvoicesAsync(userId, id, count);
        return Ok(invoices);
    }

    [HttpPost("{id}/invoices/{year}/{month}/pay")]
    public async Task<ActionResult<TransactionResponseDto>> PayInvoice(int id, int year, int month, [FromBody] PayInvoiceRequestDto dto)
    {
        var userId = GetUserId();
        try
        {
            var transaction = await _cardService.PayInvoiceAsync(userId, id, year, month, dto);
            return Ok(transaction);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
