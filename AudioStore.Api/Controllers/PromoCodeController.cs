using Asp.Versioning;
using AudioStore.Common.Constants;
using AudioStore.Common.DTOs.PromoCode;
using AudioStore.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AudioStore.Api.Controllers;

/// <summary>
/// PromoCode management endpoints
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class PromoCodeController : ControllerBase
{
    private readonly IPromoCodeService _promoCodeService;
    private readonly ILogger<PromoCodeController> _logger;

    public PromoCodeController(
        IPromoCodeService promoCodeService,
        ILogger<PromoCodeController> logger)
    {
        _promoCodeService = promoCodeService;
        _logger = logger;
    }

    // ============ ADMIN ENDPOINTS ============

    /// <summary>
    /// [Admin] Lista tutti i PromoCode con stats di utilizzo
    /// </summary>
    [HttpGet]
    [Authorize(Roles = UserRole.Admin)]
    [ProducesResponseType(typeof(IEnumerable<PromoCodeResponseDTO>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        _logger.LogInformation("Admin getting all promo codes");
        try
        {
            var result = await _promoCodeService.GetAllAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all promo codes");
            return StatusCode(500, new { error = "Errore nel recupero dei codici promo" });
        }
    }

    /// <summary>
    /// [Admin] Crea un nuovo PromoCode generico (non assegnato a nessun utente specifico;
    /// chiunque lo inserisce lo potrà usare, nel rispetto dei limiti MaxUsages).
    /// </summary>
    [HttpPost]
    [Authorize(Roles = UserRole.Admin)]
    [ProducesResponseType(typeof(PromoCodeResponseDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreatePromoCodeDTO dto)
    {
        _logger.LogInformation("Admin creating promo code '{Code}'", dto.Code);
        try
        {
            var result = await _promoCodeService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetAll), result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Conflict creating promo code '{Code}': {Message}", dto.Code, ex.Message);
            return Conflict(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating promo code '{Code}'", dto.Code);
            return StatusCode(500, new { error = "Errore nella creazione del codice promo" });
        }
    }

    /// <summary>
    /// [Admin] Crea un nuovo PromoCode e lo assegna immediatamente a un cliente specifico
    /// in un'unica operazione. Non è necessario che il PromoCode esista già.
    /// </summary>
    [HttpPost("create-for-user")]
    [Authorize(Roles = UserRole.Admin)]
    [ProducesResponseType(typeof(PromoCodeResponseDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateAndAssign([FromBody] CreateAndAssignPromoCodeDTO dto)
    {
        _logger.LogInformation("Admin creating and assigning promo code '{Code}' to user {UserId}", dto.Code, dto.UserId);
        try
        {
            var result = await _promoCodeService.CreateAndAssignAsync(dto);
            return CreatedAtAction(nameof(GetAll), result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Error in CreateAndAssign for '{Code}': {Message}", dto.Code, ex.Message);

            // Distingui conflict (code duplicato) da not found (user inesistente)
            if (ex.Message.Contains("esiste già"))
                return Conflict(new { error = ex.Message });

            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating and assigning promo code '{Code}'", dto.Code);
            return StatusCode(500, new { error = "Errore nella creazione e assegnazione del codice promo" });
        }
    }

    /// <summary>
    /// [Admin] Assegna un PromoCode già esistente a un utente specifico
    /// </summary>
    [HttpPost("{id:int}/assign/{userId:int}")]
    [Authorize(Roles = UserRole.Admin)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignToUser(int id, int userId)
    {
        _logger.LogInformation("Admin assigning promo code {Id} to user {UserId}", id, userId);
        try
        {
            await _promoCodeService.AssignToUserAsync(id, userId);
            return Ok(new { message = $"PromoCode assegnato all'utente {userId} con successo." });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Error assigning promo code {Id} to user {UserId}: {Message}", id, userId, ex.Message);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning promo code {Id} to user {UserId}", id, userId);
            return StatusCode(500, new { error = "Errore nell'assegnazione del codice promo" });
        }
    }

    /// <summary>
    /// [Admin] Disattiva un PromoCode
    /// </summary>
    [HttpPost("{id:int}/deactivate")]
    [Authorize(Roles = UserRole.Admin)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(int id)
    {
        _logger.LogInformation("Admin deactivating promo code {Id}", id);
        try
        {
            await _promoCodeService.DeactivateAsync(id);
            return Ok(new { message = "Codice promo disattivato con successo." });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating promo code {Id}", id);
            return StatusCode(500, new { error = "Errore nella disattivazione del codice promo" });
        }
    }

    /// <summary>
    /// [Admin] Lista i PromoCode assegnati a un utente specifico
    /// </summary>
    [HttpGet("user/{userId:int}")]
    [Authorize(Roles = UserRole.Admin)]
    [ProducesResponseType(typeof(IEnumerable<UserPromoCodeDTO>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByUser(int userId)
    {
        _logger.LogInformation("Admin getting promo codes for user {UserId}", userId);
        try
        {
            var result = await _promoCodeService.GetByUserIdAsync(userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting promo codes for user {UserId}", userId);
            return StatusCode(500, new { error = "Errore nel recupero dei codici promo utente" });
        }
    }

    // ============ USER ENDPOINTS ============

    /// <summary>
    /// [Authenticated] Valida un codice promo per l'utente loggato durante il checkout.
    /// Restituisce il DiscountAmount e il FinalAmount se il codice è valido.
    /// </summary>
    [HttpPost("validate")]
    [ProducesResponseType(typeof(PromoCodeValidationResultDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Validate([FromBody] ValidatePromoCodeRequestDTO dto)
    {
        var userId = GetUserId();
        //if (!userId.HasValue)
        //    return Unauthorized(new { error = "Devi essere autenticato per usare un codice promo." });

        _logger.LogInformation("User {UserId} validating promo code '{Code}' with subtotal {Subtotal}",
            userId, dto.Code, dto.Subtotal);

        try
        {
            var result = await _promoCodeService.ValidateAsync(dto.Code, dto.Subtotal, userId.Value);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating promo code '{Code}' for user {UserId}", dto.Code, userId);
            return StatusCode(500, new { error = "Errore nella validazione del codice promo" });
        }
    }

    #region Helper Methods

    private int? GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    #endregion
}
