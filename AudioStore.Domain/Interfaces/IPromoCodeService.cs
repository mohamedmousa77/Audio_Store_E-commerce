using AudioStore.Common.DTOs.PromoCode;

namespace AudioStore.Domain.Interfaces;

public interface IPromoCodeService
{
    /// <summary>
    /// Valida il codice promo per l'utente loggato.
    /// Verifica: esiste, è attivo, non scaduto, assegnato all'utente, non già usato, subtotal >= minimo.
    /// </summary>
    Task<PromoCodeValidationResultDTO> ValidateAsync(string code, decimal subtotal, int userId);

    /// <summary>
    /// Crea un nuovo PromoCode generico (usato dall'admin).
    /// </summary>
    Task<PromoCodeResponseDTO> CreateAsync(CreatePromoCodeDTO dto);

    /// <summary>
    /// Crea un nuovo PromoCode e lo assegna immediatamente a un utente specifico in un'unica operazione.
    /// </summary>
    Task<PromoCodeResponseDTO> CreateAndAssignAsync(CreateAndAssignPromoCodeDTO dto);

    /// <summary>
    /// Assegna un PromoCode esistente a un utente specifico.
    /// Chiamato dall'admin dalla sezione Clienti.
    /// </summary>
    Task AssignToUserAsync(int promoCodeId, int userId);

    /// <summary>
    /// Segna il PromoCode come usato dopo la conferma dell'ordine.
    /// </summary>
    Task MarkAsUsedAsync(int promoCodeId, int userId);

    /// <summary>
    /// Lista tutti i PromoCode (admin). Filtro opzionale per codice/nome/email utente.
    /// </summary>
    Task<IEnumerable<PromoCodeResponseDTO>> GetAllAsync(string? search = null);

    /// <summary>
    /// Lista i PromoCode assegnati a un utente specifico (admin → scheda cliente).
    /// </summary>
    Task<IEnumerable<UserPromoCodeDTO>> GetByUserIdAsync(int userId);

    /// <summary>
    /// Disattiva un PromoCode.
    /// </summary>
    Task DeactivateAsync(int promoCodeId);

    /// <summary>
    /// Riattiva un PromoCode disattivato.
    /// </summary>
    Task ActivateAsync(int promoCodeId);
}
