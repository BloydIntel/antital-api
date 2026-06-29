using Antital.Application.DTOs.Investors;
using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Investors.GetWalletTransaction;

public record GetWalletTransactionQuery(int TransactionId) : ICommandQuery<WalletTransactionInvoiceResponse>;
