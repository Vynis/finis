using Finis.Api.DTOs;

namespace Finis.Api.Services;

public interface ICreditCardService
{
    Task<List<CreditCardResponseDto>> GetCardsAsync(int userId);
    Task<CreditCardResponseDto?> GetCardByIdAsync(int userId, int cardId);
    Task<CreditCardResponseDto> CreateCardAsync(int userId, CreditCardCreateUpdateDto dto);
    Task<CreditCardResponseDto?> UpdateCardAsync(int userId, int cardId, CreditCardCreateUpdateDto dto);
    Task<bool> DeleteCardAsync(int userId, int cardId);

    Task<CreditCardInvoiceSummaryDto?> GetInvoiceSummaryAsync(int userId, int cardId, int year, int month);
    Task<List<CreditCardInvoiceSummaryDto>> GetUpcomingInvoicesAsync(int userId, int cardId, int count = 6);
    Task<TransactionResponseDto> PayInvoiceAsync(int userId, int cardId, int year, int month, PayInvoiceRequestDto dto);

    (int Year, int Month, DateTime DueDate) CalculateInvoicePeriod(DateTime purchaseDate, int closingDay, int dueDay, int installmentOffset = 0);
}
