using Antital.Application.DTOs.Investors;
using Antital.Domain.Interfaces;
using Antital.Application.Features.Investors;
using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Investors.GetWalletTransactions;

public class GetWalletTransactionsQueryHandler(
    IInvestorUserAccess investorUserAccess,
    IInvestmentOrderRepository orderRepository
) : ICommandQueryHandler<GetWalletTransactionsQuery, WalletTransactionsResponse>
{
    private const int MaxPageSize = 50;
    private const string InvestmentType = "Investment";

    public async Task<Result<WalletTransactionsResponse>> Handle(
        GetWalletTransactionsQuery request,
        CancellationToken cancellationToken)
    {
        if (!WalletTransactionFilters.TryNormalizeType(request.Type, out var typeError))
        {
            var invalidType = new Result<WalletTransactionsResponse>();
            invalidType.BadRequest(
                "Invalid transaction type.",
                new Dictionary<string, string[]> { ["type"] = [typeError!] });
            return invalidType;
        }

        if (!WalletTransactionFilters.TryNormalizeStatus(request.Status, out var normalizedStatus, out var statusError))
        {
            var invalidStatus = new Result<WalletTransactionsResponse>();
            invalidStatus.BadRequest(
                "Invalid transaction status.",
                new Dictionary<string, string[]> { ["status"] = [statusError!] });
            return invalidStatus;
        }

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, MaxPageSize);

        var (userId, _) = await investorUserAccess.RequireAuthenticatedUserAsync(cancellationToken);

        if (!WalletTransactionFilters.IncludesInvestmentRows(request.Type)
            || !WalletTransactionFilters.IncludesCompletedInvestmentRows(normalizedStatus))
        {
            return Success(page, pageSize, [], 0);
        }

        var (orders, totalCount) = await orderRepository.ListPaidByUserAsync(
            userId,
            page,
            pageSize,
            request.From,
            request.To,
            cancellationToken);

        var items = orders.Select(WalletTransactionMapper.ToInvestmentItem).ToList();
        return Success(page, pageSize, items, totalCount);
    }

    private static Result<WalletTransactionsResponse> Success(
        int page,
        int pageSize,
        IReadOnlyList<WalletTransactionItemDto> items,
        int totalCount)
    {
        var response = new WalletTransactionsResponse(items, page, pageSize, totalCount);
        var result = new Result<WalletTransactionsResponse>();
        result.AddValue(response);
        result.OK();
        return result;
    }
}

internal static class WalletTransactionFilters
{
    private static readonly HashSet<string> SupportedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Investment",
        "Deposit",
        "Withdrawal",
        "Fee",
    };

    private static readonly HashSet<string> UnsupportedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Buy",
        "Sell",
    };

    private static readonly HashSet<string> SupportedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Completed",
        "Pending",
        "Failed",
    };

    public static bool TryNormalizeType(string? type, out string? error)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            error = null;
            return true;
        }

        if (UnsupportedTypes.Contains(type))
        {
            error = "Secondary market transaction types are not supported.";
            return false;
        }

        if (!SupportedTypes.Contains(type))
        {
            error = "Type must be one of: Investment, Deposit, Withdrawal, Fee.";
            return false;
        }

        error = null;
        return true;
    }

    public static bool IncludesInvestmentRows(string? type) =>
        string.IsNullOrWhiteSpace(type)
        || type.Equals("Investment", StringComparison.OrdinalIgnoreCase);

    public static bool TryNormalizeStatus(string? status, out string? normalizedStatus, out string? error)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            normalizedStatus = null;
            error = null;
            return true;
        }

        if (!SupportedStatuses.Contains(status))
        {
            normalizedStatus = null;
            error = "Status must be one of: Completed, Pending, Failed.";
            return false;
        }

        normalizedStatus = SupportedStatuses.First(s => s.Equals(status, StringComparison.OrdinalIgnoreCase));
        error = null;
        return true;
    }

    public static bool IncludesCompletedInvestmentRows(string? normalizedStatus) =>
        normalizedStatus is null
        || normalizedStatus.Equals("Completed", StringComparison.OrdinalIgnoreCase);
}
