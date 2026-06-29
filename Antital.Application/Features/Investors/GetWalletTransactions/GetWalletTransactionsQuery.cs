using Antital.Application.DTOs.Investors;
using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Investors.GetWalletTransactions;

public record GetWalletTransactionsQuery(
    int Page = 1,
    int PageSize = 20,
    string? Type = null,
    string? Status = null,
    DateTime? From = null,
    DateTime? To = null) : ICommandQuery<WalletTransactionsResponse>;
