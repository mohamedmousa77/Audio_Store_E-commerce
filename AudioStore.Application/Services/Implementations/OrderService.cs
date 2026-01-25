using AudioStore.Common;
using AudioStore.Common.Constants;
using AudioStore.Common.DTOs.Orders;
using AudioStore.Common.Enums;
using AudioStore.Common.Services.Interfaces;
using AudioStore.Domain.Entities;
using AudioStore.Domain.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AudioStore.Application.Services.Implementations;

public class OrderService : IOrderService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<OrderService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    // ============ CREATE ORDER ============
    public async Task<Result<OrderConfirmationDTO>> CreateOrderAsync(CreateOrderDTO dto)
    {

        try
        {
            //  Validazione items
            if (dto.Items == null || !dto.Items.Any())
            {
                return Result.Failure<OrderConfirmationDTO>("Il carrello è vuoto",
                    ErrorCode.EmptyCart);
            }
            // Inizia transazione
            await _unitOfWork.BeginTransactionAsync();

            //  Genera numero ordine unico
            var orderNumber = await GenerateOrderNumberAsync();

            //  Ottieni info cliente (da User o da DTO)
            var customerInfo = await GetCustomerInfoAsync(dto);
            if (customerInfo.IsFailure)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return Result.Failure<OrderConfirmationDTO>(
                    customerInfo.Error!,
                    customerInfo.ErrorCode!);
            }

            var customer = customerInfo.Value!;

            //  Crea ordine
            var order = new Order
            {
                OrderNumber = orderNumber,
                OrderDate = DateTime.UtcNow,
                UserId = dto.UserId,

                // Customer Info
                CustomerFirstName = customer.FirstName,
                CustomerLastName = customer.LastName,
                CustomerEmail = customer.Email,
                CustomerPhone = customer.Phone,

                // Shipping Address
                ShippingStreet = dto.ShippingStreet,
                ShippingCity = dto.ShippingCity,
                ShippingPostalCode = dto.ShippingPostalCode,
                ShippingCountry = dto.ShippingCountry,

                Status = OrderStatus.Processing,
                PaymentMethod = "Cash on Delivery",
                Notes = dto.Notes,
                CreatedAt = DateTime.UtcNow
            };

            //  Processa order items e aggiorna stock
            decimal subtotal = 0;

            foreach (var itemDto in dto.Items)
            {
                var product = await _unitOfWork.Products.GetByIdAsync(itemDto.ProductId);

                if (product == null)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return Result.Failure<OrderConfirmationDTO>(
                        $"Prodotto {itemDto.ProductId} non trovato",
                        ErrorCode.ProductNotFound);
                }

                if (!product.IsAvailable)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return Result.Failure<OrderConfirmationDTO>(
                        $"Prodotto '{product.Name}' non disponibile",
                        ErrorCode.ProductNotAvailable);
                }

                if (product.StockQuantity < itemDto.Quantity)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return Result.Failure<OrderConfirmationDTO>(
                        $"Stock insufficiente per '{product.Name}'. Disponibili: {product.StockQuantity}",
                        ErrorCode.InsufficientStock);
                }

                //  Aggiorna stock
                product.StockQuantity -= itemDto.Quantity;
                product.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Products.Update(product);

                //  Crea order item
                var orderItem = new OrderItem
                {
                    ProductId = itemDto.ProductId,
                    Quantity = itemDto.Quantity,
                    UnitPrice = itemDto.UnitPrice,
                    CreatedAt = DateTime.UtcNow
                };

                order.OrderItems.Add(orderItem);
                subtotal += orderItem.Subtotal;
            }

            //  Calcola totali
            order.Subtotal = subtotal;
            order.ShippingCost = CalculateShippingCost(subtotal);
            order.Tax = CalculateTax(subtotal);
            order.TotalAmount = order.Subtotal + order.ShippingCost + order.Tax;

            //  Salva ordine
            await _unitOfWork.Orders.AddAsync(order);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            _logger.LogInformation(
                "Order {OrderNumber} created successfully - Total: {Total}",
                order.OrderNumber,
                order.TotalAmount);

            // Ricarica order con Include per AutoMapper
            var savedOrder = await _unitOfWork.Orders
                .Query()
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == order.Id);

            //  Mappa a DTO
            var orderDto = _mapper.Map<OrderDTO>(savedOrder);

            var confirmation = new OrderConfirmationDTO
            {
                OrderNumber = order.OrderNumber,
                OrderDate = order.OrderDate,
                CustomerEmail = order.CustomerEmail,
                TotalAmount = order.TotalAmount,
                Message = "Grazie per il tuo ordine! Riceverai una conferma via email.",
                OrderDetails = orderDto
            };

            return Result.Success(confirmation);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Error creating order");
            return Result.Failure<OrderConfirmationDTO>(
                "Errore durante la creazione dell'ordine",
                ErrorCode.OrderCreationFailed);
        }
    }
    // ============ GET ORDERS ============

    public async Task<Result<OrderDTO>> GetOrderByIdAsync(int orderId)
    {
        try
        {
            var order = await _unitOfWork.Orders.GetOrderWithItemsAsync(orderId);

            if (order == null)
            {
                return Result.Failure<OrderDTO>(
                    "Ordine non trovato",
                    ErrorCode.OrderNotFound);
            }

            var orderDto = _mapper.Map<OrderDTO>(order);
            return Result.Success(orderDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order {OrderId}", orderId);
            return Result.Failure<OrderDTO>(
                "Errore recupero ordine",
                ErrorCode.InternalServerError);
        }
    }

    public async Task<Result<OrderDTO>> GetOrderByNumberAsync(string orderNumber)
    {
        try
        {
            var order = await _unitOfWork.Orders.GetOrderByNumberAsync(orderNumber);

            if (order == null)
            {
                return Result.Failure<OrderDTO>(
                    "Ordine non trovato",
                    ErrorCode.OrderNotFound);
            }

            var orderDto = _mapper.Map<OrderDTO>(order);
            return Result.Success(orderDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order {OrderNumber}", orderNumber);
            return Result.Failure<OrderDTO>(
                "Errore recupero ordine",
                ErrorCode.InternalServerError);
        }
    }

    public async Task<Result<IEnumerable<OrderDTO>>> GetUserOrdersAsync(int userId)
    {
        try
        {
            var orders = await _unitOfWork.Orders.GetUserOrdersAsync(userId);

            var orderDtos = _mapper.Map<IEnumerable<OrderDTO>>(orders);

            return Result.Success(orderDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting orders for user {UserId}", userId);
            return Result.Failure<IEnumerable<OrderDTO>>(
                "Errore recupero ordini",
                ErrorCode.InternalServerError);
        }
    }

    public async Task<Result<PaginatedResult<OrderDTO>>> GetAllOrdersAsync(OrderFilterDTO filter)
    {
        try
        {
            var query = _unitOfWork.Orders
                .Query()
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .AsQueryable();

            //  Apply filters
            if (filter.UserId.HasValue)
            {
                query = query.Where(o => o.UserId == filter.UserId.Value);
            }

            if (filter.Status.HasValue)
            {
                query = query.Where(o => o.Status == filter.Status.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.CustomerSearch))
            {
                var searchLower = filter.CustomerSearch.ToLower();
                query = query.Where(o =>
                    o.CustomerFirstName.ToLower().Contains(searchLower) ||
                    o.CustomerLastName.ToLower().Contains(searchLower) ||
                    o.CustomerEmail.ToLower().Contains(searchLower));
            }

            if (filter.FromDate.HasValue)
            {
                query = query.Where(o => o.OrderDate >= filter.FromDate.Value);
            }

            if (filter.ToDate.HasValue)
            {
                query = query.Where(o => o.OrderDate <= filter.ToDate.Value);
            }

            //  Get total count
            var totalCount = await query.CountAsync();

            //  Apply sorting and pagination
            var orders = await query
                .OrderByDescending(o => o.OrderDate)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            var orderDtos = _mapper.Map<List<OrderDTO>>(orders);

            var result = new PaginatedResult<OrderDTO>
            {
                Items = orderDtos,
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            };

            return Result.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting orders with filter");
            return Result.Failure<PaginatedResult<OrderDTO>>(
                "Errore recupero ordini",
                ErrorCode.InternalServerError);
        }
    }
    // ============ UPDATE ORDER STATUS ============

    public async Task<Result<OrderDTO>> UpdateOrderStatusAsync(UpdateOrderStatusDTO dto)
    {
        try
        {
            var order = await _unitOfWork.Orders.GetOrderById(dto.OrderId);

            if (order == null)
            {
                return Result.Failure<OrderDTO>(
                    "Ordine non trovato",
                    ErrorCode.OrderNotFound);
            }

            //  Validazione transizione stato
            if (!IsValidStatusTransition(order.Status, dto.NewStatus))
            {
                return Result.Failure<OrderDTO>(
                    $"Transizione di stato non valida da {order.Status} a {dto.NewStatus}",
                    ErrorCode.BadRequest);
            }

            order.Status = dto.NewStatus;
            order.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Orders.Update(order);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Order {OrderNumber} status updated to {Status}",
                order.OrderNumber,
                dto.NewStatus);

            var orderDto = _mapper.Map<OrderDTO>(order);
            return Result.Success(orderDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating order status");
            return Result.Failure<OrderDTO>(
                "Errore aggiornamento stato ordine",
                ErrorCode.InternalServerError);
        }
    }

    // ============ CANCEL ORDER ============

    public async Task<Result> CancelOrderAsync(int orderId, int? userId = null)
    {
        try
        {
            var order = await _unitOfWork.Orders.GetOrderById(orderId);

            if (order == null)
            {
                return Result.Failure(
                    "Ordine non trovato",
                    ErrorCode.OrderNotFound);
            }

            //  Se userId è specificato, verifica che l'ordine appartenga all'utente
            if (userId.HasValue && order.UserId != userId.Value)
            {
                return Result.Failure(
                    "Non autorizzato a cancellare questo ordine",
                    ErrorCode.Unauthorized);
            }

            //  Verifica se l'ordine può essere cancellato
            if (order.Status == OrderStatus.Shipped || order.Status == OrderStatus.Delivered)
            {
                return Result.Failure(
                    "Impossibile cancellare un ordine già spedito o consegnato",
                    ErrorCode.BadRequest);
            }

            if (order.Status == OrderStatus.Cancelled)
            {
                return Result.Failure(
                    "L'ordine è già stato cancellato",
                    ErrorCode.BadRequest);
            }

            await _unitOfWork.BeginTransactionAsync();

            //  Ripristina stock prodotti
            foreach (var item in order.OrderItems)
            {
                var product = item.Product;
                product.StockQuantity += item.Quantity;
                product.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Products.Update(product);
            }

            //  Aggiorna stato ordine
            order.Status = OrderStatus.Cancelled;
            order.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Orders.Update(order);

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            _logger.LogInformation("Order {OrderNumber} cancelled", order.OrderNumber);

            return Result.Success();
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Error cancelling order {OrderId}", orderId);
            return Result.Failure(
                "Errore cancellazione ordine",
                ErrorCode.InternalServerError);
        }
    }

    // ============ PRIVATE HELPERS ============

    private async Task<string> GenerateOrderNumberAsync()
    {
        // Formato: ORD-YYYYMMDD-XXXXX (es: ORD-20260119-00001)
        var date = DateTime.UtcNow.ToString("yyyyMMdd");
        var lastOrder = await _unitOfWork.Orders
            .Query()
            .Where(o => o.OrderNumber.StartsWith($"ORD-{date}"))
            .OrderByDescending(o => o.OrderNumber)
            .FirstOrDefaultAsync();

        int sequence = 1;
        if (lastOrder != null)
        {
            var lastSequence = lastOrder.OrderNumber.Split('-').Last();
            if (int.TryParse(lastSequence, out int lastNum))
            {
                sequence = lastNum + 1;
            }
        }

        return $"ORD-{date}-{sequence:D5}";
    }

    private async Task<Result<CustomerInfo>> GetCustomerInfoAsync(CreateOrderDTO dto)
    {
        // Se è un utente autenticato, prendi i dati dal database
        if (dto.UserId.HasValue)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(dto.UserId.Value);
            if (user == null)
            {
                return Result.Failure<CustomerInfo>(
                    "Utente non trovato",
                    ErrorCode.UserNotFound);
            }

            return Result.Success(new CustomerInfo
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email!,
                Phone = user.PhoneNumber ?? ""
            });
        }

        // Guest user - usa i dati dal DTO
        if (string.IsNullOrWhiteSpace(dto.CustomerFirstName) ||
            string.IsNullOrWhiteSpace(dto.CustomerLastName) ||
            string.IsNullOrWhiteSpace(dto.CustomerEmail) ||
            string.IsNullOrWhiteSpace(dto.CustomerPhone))
        {
            return Result.Failure<CustomerInfo>(
                "Informazioni cliente mancanti per guest checkout",
                ErrorCode.ValidationError);
        }

        return Result.Success(new CustomerInfo
        {
            FirstName = dto.CustomerFirstName!,
            LastName = dto.CustomerLastName!,
            Email = dto.CustomerEmail!,
            Phone = dto.CustomerPhone!
        });
    }

    private decimal CalculateShippingCost(decimal subtotal)
    {
        //  Spedizione gratuita sopra €50
        if (subtotal >= 50)
            return 0m;

        //  Altrimenti €5 fissi
        return 5.00m;
    }

    private decimal CalculateTax(decimal subtotal)
    {
        //  IVA 22% (Italia)
        return subtotal * 0.22m;
    }

    private bool IsValidStatusTransition(OrderStatus currentStatus, OrderStatus newStatus)
    {
        //  Definisci transizioni valide
        return (currentStatus, newStatus) switch
        {
            (OrderStatus.Pending, OrderStatus.Processing) => true,
            (OrderStatus.Pending, OrderStatus.Cancelled) => true,
            (OrderStatus.Processing, OrderStatus.Shipped) => true,
            (OrderStatus.Processing, OrderStatus.Cancelled) => true,
            (OrderStatus.Shipped, OrderStatus.Delivered) => true,
            _ => false
        };
    }
    private class CustomerInfo
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
    }
}
