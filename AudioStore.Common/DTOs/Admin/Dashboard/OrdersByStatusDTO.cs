namespace AudioStore.Common.DTOs.Admin.Dashboard;

public record OrdersByStatusDTO
{
    public int Pending { get; init; }
    public int Processing { get; init; }
    public int Shipped { get; init; }
    public int Delivered { get; init; }
    public int Cancelled { get; init; }

}
