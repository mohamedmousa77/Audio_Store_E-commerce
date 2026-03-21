using AudioStore.Common;
using AudioStore.Common.Constants;
using AudioStore.Common.DTOs.PromoCode;
using AudioStore.Common.Enums;
using AudioStore.Domain.Entities;
using AudioStore.Domain.Interfaces;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace AudioStore.Application.Services.Implementations;

public class PromoCodeService : IPromoCodeService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<PromoCodeService> _logger;

    public PromoCodeService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<PromoCodeService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    // ============ VALIDATE ============

    public async Task<PromoCodeValidationResultDTO> ValidateAsync(string code, decimal subtotal, int userId)
    {
        _logger.LogInformation("Validating promo code '{Code}' for user {UserId}, subtotal: {Subtotal}", code, userId, subtotal);

        var promo = await _unitOfWork.PromoCodes.GetByCodeAsync(code);

        if (promo == null)
        {
            _logger.LogWarning("Promo code '{Code}' not found", code);
            return PromoCodeValidationResultDTO.Invalid("Codice promo non trovato.");
        }

        if (!promo.IsActive)
        {
            _logger.LogWarning("Promo code '{Code}' is inactive", code);
            return PromoCodeValidationResultDTO.Invalid("Il codice promo non è attivo.");
        }

        if (promo.ExpiresAt.HasValue && promo.ExpiresAt.Value < DateTime.UtcNow)
        {
            _logger.LogWarning("Promo code '{Code}' is expired (expiry: {Expiry})", code, promo.ExpiresAt);
            return PromoCodeValidationResultDTO.Invalid("Il codice promo è scaduto.");
        }

        if (promo.MaxUsages.HasValue && promo.CurrentUsages >= promo.MaxUsages.Value)
        {
            _logger.LogWarning("Promo code '{Code}' has reached max usages ({Max})", code, promo.MaxUsages);
            return PromoCodeValidationResultDTO.Invalid("Il codice promo ha raggiunto il numero massimo di utilizzi.");
        }

        if (promo.MinOrderAmount.HasValue && subtotal < promo.MinOrderAmount.Value)
        {
            _logger.LogWarning("Promo code '{Code}' min amount not met: required {Min}, got {Subtotal}", code, promo.MinOrderAmount, subtotal);
            return PromoCodeValidationResultDTO.Invalid($"Il codice promo richiede un importo minimo di {promo.MinOrderAmount.Value:C}.");
        }

        // Se il PromoCode è stato assegnato a specifici utenti, verifica che l'utente corrente lo abbia
        var isAssigned = await _unitOfWork.PromoCodes.IsAssignedToUserAsync(promo.Id, userId);
        var hasUserAssignments = promo.UserPromoCodes.Any();

        // Ricarica il promo con le UserPromoCodes per verificare le assegnazioni
        var promoWithStats = await _unitOfWork.PromoCodes.GetWithStatsAsync(promo.Id);
        var isPersonalized = promoWithStats?.UserPromoCodes.Any() == true;

        if (isPersonalized && !isAssigned)
        {
            _logger.LogWarning("Promo code '{Code}' is not assigned to user {UserId}", code, userId);
            return PromoCodeValidationResultDTO.Invalid("Questo codice promo non è assegnato al tuo account.");
        }

        if (isAssigned)
        {
            // Verifica che non sia già stato usato dall'utente
            var userPromoCode = promoWithStats!.UserPromoCodes.FirstOrDefault(u => u.UserId == userId);
            if (userPromoCode?.IsUsed == true)
            {
                _logger.LogWarning("Promo code '{Code}' already used by user {UserId}", code, userId);
                return PromoCodeValidationResultDTO.Invalid("Hai già utilizzato questo codice promo.");
            }
        }

        // Calcola lo sconto
        var discount = CalculateDiscount(promo, subtotal);

        _logger.LogInformation("Promo code '{Code}' valid — discount: {Discount}", code, discount);
        return PromoCodeValidationResultDTO.Valid(discount, subtotal, promo.Id);
    }

    // ============ CREATE (generico) ============

    public async Task<PromoCodeResponseDTO> CreateAsync(CreatePromoCodeDTO dto)
    {
        _logger.LogInformation("Creating promo code '{Code}'", dto.Code);

        // Verifica duplicato
        var existing = await _unitOfWork.PromoCodes.GetByCodeAsync(dto.Code);
        if (existing != null)
            throw new InvalidOperationException($"Il codice '{dto.Code}' esiste già.");

        var promoCode = _mapper.Map<PromoCode>(dto);
        await _unitOfWork.PromoCodes.AddAsync(promoCode);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Promo code '{Code}' created with Id={Id}", promoCode.Code, promoCode.Id);

        var response = _mapper.Map<PromoCodeResponseDTO>(promoCode);
        response.TotalAssigned = 0;
        response.TotalUsed = 0;
        return response;
    }

    // ============ CREATE + ASSIGN (in un'unica operazione) ============

    public async Task<PromoCodeResponseDTO> CreateAndAssignAsync(CreateAndAssignPromoCodeDTO dto)
    {
        _logger.LogInformation("Creating and assigning promo code '{Code}' to user {UserId}", dto.Code, dto.UserId);

        // Verifica duplicato
        var existing = await _unitOfWork.PromoCodes.GetByCodeAsync(dto.Code);
        if (existing != null)
            throw new InvalidOperationException($"Il codice '{dto.Code}' esiste già.");

        // Verifica che l'utente esista
        var user = await _unitOfWork.Users.GetByIdAsync(dto.UserId);
        if (user == null)
            throw new InvalidOperationException($"Utente con Id={dto.UserId} non trovato.");

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var promoCode = _mapper.Map<PromoCode>(dto);
            await _unitOfWork.PromoCodes.AddAsync(promoCode);
            await _unitOfWork.SaveChangesAsync(); // genera l'Id

            // Assegna all'utente
            await _unitOfWork.PromoCodes.AssignToUserAsync(promoCode.Id, dto.UserId);
            await _unitOfWork.SaveChangesAsync();

            await _unitOfWork.CommitTransactionAsync();

            _logger.LogInformation("Promo code '{Code}' (Id={Id}) created and assigned to user {UserId}", promoCode.Code, promoCode.Id, dto.UserId);

            var response = _mapper.Map<PromoCodeResponseDTO>(promoCode);
            response.TotalAssigned = 1;
            response.TotalUsed = 0;
            return response;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    // ============ ASSIGN ============

    public async Task AssignToUserAsync(int promoCodeId, int userId)
    {
        _logger.LogInformation("Assigning promo code {PromoCodeId} to user {UserId}", promoCodeId, userId);

        var promo = await _unitOfWork.PromoCodes.GetByIdAsync(promoCodeId);
        if (promo == null)
            throw new InvalidOperationException($"PromoCode con Id={promoCodeId} non trovato.");

        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
            throw new InvalidOperationException($"Utente con Id={userId} non trovato.");

        await _unitOfWork.PromoCodes.AssignToUserAsync(promoCodeId, userId);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Promo code {PromoCodeId} assigned to user {UserId}", promoCodeId, userId);
    }

    // ============ MARK AS USED ============

    public async Task MarkAsUsedAsync(int promoCodeId, int userId)
    {
        _logger.LogInformation("Marking promo code {PromoCodeId} as used for user {UserId}", promoCodeId, userId);
        await _unitOfWork.PromoCodes.MarkAsUsedAsync(promoCodeId, userId);
        await _unitOfWork.SaveChangesAsync();
    }

    // ============ GET ALL ============

    public async Task<IEnumerable<PromoCodeResponseDTO>> GetAllAsync()
    {
        var promoCodes = await _unitOfWork.PromoCodes.GetAllWithStatsAsync();

        return promoCodes.Select(p =>
        {
            var dto = _mapper.Map<PromoCodeResponseDTO>(p);
            dto.TotalAssigned = p.UserPromoCodes.Count;
            dto.TotalUsed = p.UserPromoCodes.Count(u => u.IsUsed);
            return dto;
        }).ToList();
    }

    // ============ GET BY USER ============

    public async Task<IEnumerable<UserPromoCodeDTO>> GetByUserIdAsync(int userId)
    {
        var userPromoCodes = await _unitOfWork.PromoCodes.GetUserPromoCodesAsync(userId);
        return _mapper.Map<IEnumerable<UserPromoCodeDTO>>(userPromoCodes);
    }

    // ============ DEACTIVATE ============

    public async Task DeactivateAsync(int promoCodeId)
    {
        _logger.LogInformation("Deactivating promo code {PromoCodeId}", promoCodeId);

        var promo = await _unitOfWork.PromoCodes.GetByIdAsync(promoCodeId);
        if (promo == null)
            throw new InvalidOperationException($"PromoCode con Id={promoCodeId} non trovato.");

        promo.IsActive = false;
        _unitOfWork.PromoCodes.Update(promo);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Promo code {PromoCodeId} deactivated", promoCodeId);
    }

    // ============ PRIVATE HELPERS ============

    private static decimal CalculateDiscount(PromoCode promo, decimal subtotal)
    {
        return promo.DiscountType switch
        {
            DiscountType.Percentage => Math.Round(subtotal * (promo.DiscountValue / 100m), 2),
            DiscountType.FixedAmount => Math.Min(promo.DiscountValue, subtotal),
            _ => 0m
        };
    }
}
