using AudioStore.Domain.Entities;
using AudioStore.Domain.Interfaces;
using AudioStore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AudioStore.Infrastructure.Repositories;

public class PromoCodeRepository : Repository<PromoCode>, IPromoCodeRepository
{
    public PromoCodeRepository(AppDbContext context) : base(context) { }

    public async Task<PromoCode?> GetByCodeAsync(string code)
    {
        return await _context.PromoCodes
            .Where(p => !p.IsDeleted && p.Code.ToLower() == code.ToLower())
            .FirstOrDefaultAsync();
    }

    public async Task<PromoCode?> GetWithStatsAsync(int id)
    {
        return await _context.PromoCodes
            .Include(p => p.UserPromoCodes)
            .Where(p => !p.IsDeleted && p.Id == id)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<PromoCode>> GetAllWithStatsAsync()
    {
        return await _context.PromoCodes
            .Include(p => p.UserPromoCodes)
            .Where(p => !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<UserPromoCode>> GetUserPromoCodesAsync(int userId)
    {
        return await _context.UserPromoCodes
            .Include(u => u.PromoCode)
            .Where(u => u.UserId == userId && !u.PromoCode.IsDeleted)
            .ToListAsync();
    }

    public async Task<bool> IsAssignedToUserAsync(int promoCodeId, int userId)
    {
        return await _context.UserPromoCodes
            .AnyAsync(u => u.PromoCodeId == promoCodeId && u.UserId == userId);
    }

    public async Task AssignToUserAsync(int promoCodeId, int userId)
    {
        var alreadyAssigned = await IsAssignedToUserAsync(promoCodeId, userId);
        if (alreadyAssigned) return;

        var userPromoCode = new UserPromoCode
        {
            PromoCodeId = promoCodeId,
            UserId = userId,
            IsUsed = false,
            AssignedAt = DateTime.UtcNow
        };

        await _context.UserPromoCodes.AddAsync(userPromoCode);
    }

    public async Task MarkAsUsedAsync(int promoCodeId, int userId)
    {
        // Aggiorna UserPromoCode
        var userPromoCode = await _context.UserPromoCodes
            .FirstOrDefaultAsync(u => u.PromoCodeId == promoCodeId && u.UserId == userId);

        if (userPromoCode != null)
        {
            userPromoCode.IsUsed = true;
            userPromoCode.UsedAt = DateTime.UtcNow;
        }

        // Incrementa CurrentUsages sul PromoCode
        var promo = await _context.PromoCodes.FindAsync(promoCodeId);
        if (promo != null)
        {
            promo.CurrentUsages++;
            promo.UpdatedAt = DateTime.UtcNow;
        }
    }
}
