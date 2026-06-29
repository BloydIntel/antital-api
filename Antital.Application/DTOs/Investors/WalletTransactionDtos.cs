namespace Antital.Application.DTOs.Investors;

public record WalletTransactionItemDto(
    int Id,
    string Type,
    string Description,
    string SubDescription,
    decimal Amount,
    decimal? Fees,
    DateTime OccurredAt,
    string Status,
    int OrderId,
    string OfferingSlug);

public record WalletTransactionsResponse(
    IReadOnlyList<WalletTransactionItemDto> Items,
    int Page,
    int PageSize,
    int TotalCount);

public record WalletTransactionBillToDto(
    string Name,
    string Email,
    string? Phone);

public record WalletTransactionDetailsDto(
    string Type,
    string Status);

public record WalletTransactionBreakdownDto(
    string Description,
    string Company,
    string Sector,
    int Units,
    decimal PricePerUnit,
    decimal Subtotal,
    decimal FeePercentage,
    decimal Fees,
    decimal TotalAmount);

public record WalletTransactionInvoiceResponse(
    int InvoiceId,
    DateTime InvoiceDate,
    DateTime PaymentDate,
    string PaymentMethod,
    string? PaymentReference,
    WalletTransactionBillToDto BillTo,
    WalletTransactionDetailsDto TransactionDetails,
    WalletTransactionBreakdownDto Breakdown);
