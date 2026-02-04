using Antital.Domain.Enums;
using Antital.Domain.Interfaces;
using Antital.Domain.Models;
using BuildingBlocks.Domain.Interfaces;
using BuildingBlocks.Infrastructure.Implementations;
using Microsoft.EntityFrameworkCore;

namespace Antital.Infrastructure.Repositories;

public class UserOnboardingRepository(
    DBContext dbContext,
    ICurrentUser currentUser
) : Repository<UserOnboarding>(dbContext, currentUser), IUserOnboardingRepository
{
    public async Task<UserOnboarding?> GetByUserIdAsync(int userId, CancellationToken cancellationToken)
    {
        return await SetAsNoTracking
            .FirstOrDefaultAsync(e => e.UserId == userId && !e.IsDeleted, cancellationToken);
    }

    public async Task<UserOnboarding> GetOrCreateForUserAsync(int userId, CancellationToken cancellationToken)
    {
        var existing = await GetByUserIdAsync(userId, cancellationToken);
        if (existing != null)
            return existing;

        var entity = new UserOnboarding
        {
            UserId = userId,
            FlowType = OnboardingFlowType.IndividualInvestor,
            CurrentStep = OnboardingStep.InvestorCategory,
            Status = OnboardingStatus.Draft
        };
        await AddAsync(entity, cancellationToken);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return entity;
        }
        catch (DbUpdateException)
        {
            _dbContext.Entry(entity).State = EntityState.Detached;
            var createdByOther = await GetByUserIdAsync(userId, cancellationToken);
            if (createdByOther != null)
                return createdByOther;
            throw;
        }
    }
}
