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
            var totalSales = await _unitOfWork.Orders.GetTotalSalesAsync();

            // Total Orders
            var totalOrders = await _unitOfWork.Orders.CountAsync();

            // Orders by Status
            var ordersByStatus = await _unitOfWork.Orders.GetOrdersByStatusAsync();
            //.Query()
            //.GroupBy(o => o.Status)
            //.Select(g => new { Status = g.Key, Count = g.Count() })
            //.ToListAsync();

            // Total Customers
            var totalCustomers = await _unitOfWork.Users.GetTotalCustomersCountAsync();
               //.Query()
               //.Where(u => u.Role == UserRole.Customer)
               //.CountAsync();

            var ordersStats = new OrdersByStatusDTO
            {
                Pending = ordersByStatus.FirstOrDefault(x => x.Status == OrderStatus.Pending)?.Count ?? 0,
                Processing = ordersByStatus.FirstOrDefault(x => x.Status == OrderStatus.Processing)?.Count ?? 0,
                Shipped = ordersByStatus.FirstOrDefault(x => x.Status == OrderStatus.Shipped)?.Count ?? 0,
                Delivered = ordersByStatus.FirstOrDefault(x => x.Status == OrderStatus.Delivered)?.Count ?? 0,
                Cancelled = ordersByStatus.FirstOrDefault(x => x.Status == OrderStatus.Cancelled)?.Count ?? 0
            };

            // Top 5 Products (by quantity sold)
            var topProductsData = await _unitOfWork.Orders.GetTopProductsAsync(5);
            var topProducts = _mapper.Map<List<TopProductDTO>>(topProductsData);

            //var topProducts = await _unitOfWork.OrderItems
            //    .Query()
            //    .Include(oi => oi.Product)
            //    .Include(oi => oi.Order)
            //    .Where(oi => oi.Order.Status != OrderStatus.Cancelled)
            //    .GroupBy(oi => new { oi.ProductId, oi.Product.Name, oi.Product.MainImage })
            //    .Select(g => new TopProductDTO
            //    {
            //        ProductId = g.Key.ProductId,
            //        ProductName = g.Key.Name,
            //        ProductImage = g.Key.MainImage,
            //        TotalQuantitySold = g.Sum(oi => oi.Quantity),
            //        TotalRevenue = g.Sum(oi => oi.Subtotal)
            //    })
            //    .OrderByDescending(x => x.TotalQuantitySold)
            //    .Take(5)
            //    .ToListAsync();

            // Top 3 Categories (by quantity sold)
            var topCategoriesData = await _unitOfWork.Orders.GetTopCategoriesAsync(3);
            var topCategories = _mapper.Map<List<TopCategoryDTO>>(topCategoriesData);

            //var topCategories = await _unitOfWork.OrderItems
            //    .Query()
            //    .Include(oi => oi.Product)
            //    .ThenInclude(p => p.Category)
            //    .Include(oi => oi.Order)
            //    .Where(oi => oi.Order.Status != OrderStatus.Cancelled)
            //    .GroupBy(oi => new { oi.Product.CategoryId, oi.Product.Category.Name })
            //    .Select(g => new TopCategoryDTO
            //    {
            //        CategoryId = g.Key.CategoryId,
            //        CategoryName = g.Key.Name,
            //        TotalQuantitySold = g.Sum(oi => oi.Quantity),
            //        TotalRevenue = g.Sum(oi => oi.Subtotal)
            //    })
            //    .OrderByDescending(x => x.TotalQuantitySold)
            //    .Take(3)
            //    .ToListAsync();

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
