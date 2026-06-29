using FluentValidation;

namespace Antital.Application.Features.Investors.AddToWatchlist;

public class AddToWatchlistCommandValidator : AbstractValidator<AddToWatchlistCommand>
{
    public AddToWatchlistCommandValidator()
    {
        RuleFor(x => x.OfferingId).GreaterThan(0);
    }
}
