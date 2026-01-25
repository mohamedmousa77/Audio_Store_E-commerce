namespace AudioStore.Common.Constants;

/// <summary>
/// Costanti per i ruoli utente del sistema.
/// IMPORTANTE: Questi valori devono corrispondere ai ruoli in Identity.
/// </summary>
public static class UserRole
{
    /// <summary>
    /// Amministratore con accesso completo al sistema
    /// </summary>
    public const string Admin = "Administrator";

    /// <summary>
    /// Cliente con accesso limitato alle funzionalità e-commerce
    /// </summary>
    public const string Customer = "Customer";

    /// <summary>
    /// Tutti i ruoli disponibili nel sistema
    /// </summary>
    public static readonly string[] AllRoles = { Admin, Customer };

    /// <summary>
    /// Verifica se un ruolo è valido
    /// </summary>
    public static bool IsValidRole(string role) => AllRoles.Contains(role);
}
