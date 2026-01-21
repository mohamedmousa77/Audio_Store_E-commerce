namespace AudioStore.Application.DTOs.Admin.CustomerManagement;

public record CustomerFilterDTO
{
    // Sorting
    public CustomerSortBy? SortBy { get; init; }

    // Filters
    public CustomerOrderFilter? OrderFilter { get; init; }
    public CustomerActivityFilter? ActivityFilter { get; init; }

    // Search
    public string? SearchTerm { get; init; }

    // Pagination
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}


public enum CustomerSortBy
{
    MostRecent,
    Oldest
}

public enum CustomerOrderFilter
{
    All,
    BigSpender,      // Spendaccione (>€500)
    Regular,         // Regolare (€100-€500)
    NoOrders         // Nessun ordine
}

public enum CustomerActivityFilter
{
    All,
    Recent,          // Ha fatto ordine entro 30 giorni
    Inactive         // Non ha fatto ordini da più di 6 mesi
}
