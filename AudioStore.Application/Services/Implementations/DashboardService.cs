using AudioStore.Application.DTOs.Admin.Dashboard;
using AudioStore.Application.Services.Interfaces;
using AudioStore.Common.Constants;
using AudioStore.Common.Result;
using AudioStore.Domain.Enums;
using AudioStore.Domain.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AudioStore.Application.Services.Implementations;

public class DashboardService : IDashboardService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DashboardService> _logger;
    private readonly IMapper _mapper;


    public DashboardService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<DashboardService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _mapper = mapper;
    }

    public async Task<Result<DashboardStatsDTO>> GetDashboardStatsAsync()
    {
        try
        {
            // Total Sales
            var totalSales = await _unitOfWork.Dashboard.GetTotalSalesAsync();
            // Total Orders
            var totalOrders = await _unitOfWork.Orders.CountAsync();
            // Orders by Status
            var ordersByStatus = await _unitOfWork.Dashboard.GetOrdersByStatusAsync();
            // Total Customers
            var totalCustomers = await _unitOfWork.Users.GetTotalCustomersCountAsync();

            var ordersStats = new OrdersByStatusDTO
            {
                Pending = ordersByStatus.GetValueOrDefault(OrderStatus.Pending, 0),
                Processing = ordersByStatus.GetValueOrDefault(OrderStatus.Processing, 0),
                Shipped = ordersByStatus.GetValueOrDefault(OrderStatus.Shipped, 0),
                Delivered = ordersByStatus.GetValueOrDefault(OrderStatus.Delivered, 0),
                Cancelled = ordersByStatus.GetValueOrDefault(OrderStatus.Cancelled, 0)
            };

            // Top 5 Products (by quantity sold)
            var topProductsData = await _unitOfWork.Dashboard.GetTopProductsAsync(5);
            var topProducts = _mapper.Map<List<TopProductDTO>>(topProductsData);

            // Top 3 Categories (by quantity sold)
            var topCategoriesData = await _unitOfWork.Dashboard.GetTopCategoriesAsync(3);
            var topCategories = _mapper.Map<List<TopCategoryDTO>>(topCategoriesData);

            var stats = new DashboardStatsDTO
            {
                TotalSales = totalSales,
                TotalOrders = totalOrders,
                OrdersByStatus = ordersStats,
                TotalCustomers = totalCustomers,
                TopProducts = topProducts,
                TopCategories = topCategories
            };

            return Result.Success(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard stats");
            return Result.Failure<DashboardStatsDTO>(
                "Errore recupero statistiche dashboard",
                ErrorCode.InternalServerError);
        }
    }

}
