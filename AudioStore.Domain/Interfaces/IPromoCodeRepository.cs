using AudioStore.Domain.Entities;

namespace AudioStore.Domain.Interfaces;

public interface IPromoCodeRepository : IRepository<PromoCode>
{
    /// <summary>Cerca il PromoCode per stringa codice (case-insensitive).</summary>
    Task<PromoCode?> GetByCodeAsync(string code);

    /// <summary>Restituisce il PromoCode con la lista UserPromoCodes inclusa (per stats).</summary>
    Task<PromoCode?> GetWithStatsAsync(int id);

    /// <summary>Tutti i PromoCode con UserPromoCodes inclusi (per stats admin).</summary>
    Task<IEnumerable<PromoCode>> GetAllWithStatsAsync();

    /// <summary>Lista UserPromoCode assegnati a un utente specifico.</summary>
    Task<IEnumerable<UserPromoCode>> GetUserPromoCodesAsync(int userId);

    /// <summary>Controlla se il PromoCode è assegnato a un utente specifico.</summary>
    Task<bool> IsAssignedToUserAsync(int promoCodeId, int userId);

    /// <summary>Assegna un PromoCode esistente a un utente (inserisce UserPromoCode).</summary>
    Task AssignToUserAsync(int promoCodeId, int userId);

    /// <summary>Marca il PromoCode come usato per un utente + incrementa CurrentUsages.</summary>
    Task MarkAsUsedAsync(int promoCodeId, int userId);
}
