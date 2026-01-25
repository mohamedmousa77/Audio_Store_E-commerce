using AudioStore.Common.DTOs.Admin.Dashboard;

namespace AudioStore.Common.Services.Interfaces;

public interface IDashboardService
{
    Task<Result<DashboardStatsDTO>> GetDashboardStatsAsync();
}
