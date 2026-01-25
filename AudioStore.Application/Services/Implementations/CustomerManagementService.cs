using AudioStore.Common;
using AudioStore.Common.Constants;
using AudioStore.Common.DTOs.Admin.CustomerManagement;
using AudioStore.Common.DTOs.Orders;
using AudioStore.Common.Services.Interfaces;
using AudioStore.Domain.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AudioStore.Application.Services.Implementations;

public class CustomerManagementService : ICustomerManagementService
{
    private readonly ILogger<CustomerManagementService> _logger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CustomerManagementService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<CustomerManagementService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<CustomerSummaryDTO>> GetCustomerSummaryAsync()
    {
        try
        {
            // Total customers (role = Customer)
            var totalCustomersTask = _unitOfWork.Users
                .GetTotalCustomersCountAsync();

            // Active customers this month (made at least one order)
            var activeCustomersTask = _unitOfWork.Users
                .GetActiveCustomersThisMonthAsync();

            // Total orders
            var totalOrdersTask = _unitOfWork.Orders
                .CountAsync();

            // Top customer (highest total spent)
            var topCustomerTask = _unitOfWork.Users.GetTopCustomerAsync();

            await Task.WhenAll(totalCustomersTask, activeCustomersTask, totalOrdersTask, topCustomerTask);

            var totalCustomers = await totalCustomersTask;
            var activeCustomersThisMonth = await activeCustomersTask;
            var totalOrders = await totalOrdersTask;
            var topCustomerEntity = await topCustomerTask;


            TopCustomerDTO? topCustomer = null;
            if (topCustomerEntity != null)
            {
                topCustomer = new TopCustomerDTO
                {
                    UserId = topCustomerEntity.Id,
                    FirstName = topCustomerEntity.FirstName,
                    LastName = topCustomerEntity.LastName,
                    Email = topCustomerEntity.Email!,
                    TotalSpent = topCustomerEntity.Orders.Sum(o => o.TotalAmount),
                    TotalOrders = topCustomerEntity.Orders.Count
                };
            }

            var summary = new CustomerSummaryDTO
            {
                TotalCustomers = totalCustomers,
                ActiveCustomersThisMonth = activeCustomersThisMonth,
                TotalOrders = totalOrders,
                TopCustomer = topCustomer
            };

            return Result.Success(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customer summary");
            return Result.Failure<CustomerSummaryDTO>(
                "Errore recupero riepilogo clienti",
                ErrorCode.InternalServerError);
        }
    }

    public async Task<Result<PaginatedResult<CustomerListItemDTO>>> GetCustomersAsync(CustomerFilterDTO filter)
    {
        try
        {
            var customers = _unitOfWork.Users.GetCustomersWithOrdersAsync();

            var customerDtos = _mapper.Map<IEnumerable<CustomerListItemDTO>>(customers).AsQueryable();

            // Search by name or email
            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var searchLower = filter.SearchTerm.ToLower();
                customerDtos = customerDtos.Where(u =>
                    u.FirstName.ToLower().Contains(searchLower) ||
                    u.LastName.ToLower().Contains(searchLower) ||
                    u.Email!.ToLower().Contains(searchLower));
            }

            // Apply filters
            if (filter.OrderFilter.HasValue)
            {
                customerDtos = filter.OrderFilter.Value switch
                {
                    CustomerOrderFilter.BigSpender => customerDtos.Where(c => c.TotalSpent > 500),
                    CustomerOrderFilter.Regular => customerDtos.Where(c => c.TotalSpent >= 100 && c.TotalSpent <= 500),
                    CustomerOrderFilter.NoOrders => customerDtos.Where(c => c.TotalOrders == 0),
                    _ => customerDtos
                };
            }
            if (filter.ActivityFilter.HasValue)
            {
                var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
                var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);

                customerDtos = filter.ActivityFilter.Value switch
                {
                    CustomerActivityFilter.Recent => customerDtos.Where(c => c.LastOrderDate >= thirtyDaysAgo),
                    CustomerActivityFilter.Inactive => customerDtos.Where(c => c.LastOrderDate < sixMonthsAgo || c.LastOrderDate == null),
                    _ => customerDtos
                };
            }

            // Apply sorting
            customerDtos = filter.SortBy switch
            {
                CustomerSortBy.MostRecent => customerDtos.OrderByDescending(c => c.RegistrationDate),
                CustomerSortBy.Oldest => customerDtos.OrderBy(c => c.RegistrationDate),
                _ => customerDtos.OrderByDescending(c => c.RegistrationDate)
            };

            var totalCount = customerDtos.Count();

            var pagedCustomers = customerDtos
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToList();

            var result = new PaginatedResult<CustomerListItemDTO>
            {
                Items = pagedCustomers,
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            };

            return Result.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customers");
            return Result.Failure<PaginatedResult<CustomerListItemDTO>>(
                "Errore recupero clienti",
                ErrorCode.InternalServerError);
        }
    }

    public async Task<Result<CustomerDetailDTO>> GetCustomerDetailAsync(int userId)
    {
        try
        {
            var user = await _unitOfWork.Users.GetUserWithOrdersAsync(userId);

            if (user == null)
            {
                return Result.Failure<CustomerDetailDTO>(
                    "Cliente non trovato",
                    ErrorCode.UserNotFound);
            }

            var orders = user.Orders.OrderByDescending(o => o.OrderDate).ToList();
            var totalOrders = orders.Count;
            var totalSpent = orders.Sum(o => o.TotalAmount);
            var averageOrderAmount = totalOrders > 0 ? totalSpent / totalOrders : 0;
            var lastOrderDate = orders.Any() ? orders.First().OrderDate : (DateTime?)null;

            // Get recent orders (last 5)
            var recentOrders = _mapper.Map<List<OrderDTO>>(orders.Take(5).ToList());

            var customerDetail = new CustomerDetailDTO
            {
                UserId = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email!,
                PhoneNumber = user.PhoneNumber,
                RegistrationDate = user.CreatedAt,
                TotalOrders = totalOrders,
                TotalSpent = totalSpent,
                AverageOrderAmount = averageOrderAmount,
                LastOrderDate = lastOrderDate,
                RecentOrders = recentOrders
            };

            return Result.Success(customerDetail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customer detail {UserId}", userId);
            return Result.Failure<CustomerDetailDTO>(
                "Errore recupero dettagli cliente",
                ErrorCode.InternalServerError);
        }
    }

    public async Task<Result<IEnumerable<OrderDTO>>> GetCustomerOrderHistoryAsync(int userId)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null)
            {
                return Result.Failure<IEnumerable<OrderDTO>>(
                    "Cliente non trovato",
                    ErrorCode.UserNotFound);
            }

            var orders = await _unitOfWork.Orders.GetUserOrdersAsync(userId);

            var orderDtos = _mapper.Map<IEnumerable<OrderDTO>>(orders);
            return Result.Success(orderDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customer order history {UserId}", userId);
            return Result.Failure<IEnumerable<OrderDTO>>(
                "Errore recupero storico ordini",
                ErrorCode.InternalServerError);
        }
    }


}
