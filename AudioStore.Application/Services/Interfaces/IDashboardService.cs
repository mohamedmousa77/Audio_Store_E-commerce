using AudioStore.Application.DTOs.Admin.Dashboard;
using AudioStore.Common.Result;

namespace AudioStore.Application.Services.Interfaces;

public interface IDashboardService
{
    Task<Result<DashboardStatsDTO>> GetDashboardStatsAsync();
}
