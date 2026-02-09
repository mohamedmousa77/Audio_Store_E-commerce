//using AudioStore.Common;
//using AudioStore.Common.Constants;
//using AudioStore.Common.DTOs.Orders;
//using AudioStore.Common.Enums;
//using AudioStore.Common.Services.Interfaces;
//using AudioStore.Domain.Entities;
//using AudioStore.Domain.Interfaces;
//using AutoMapper;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Logging;

//namespace AudioStore.Application.Services.Implementations;

//public class OrderService : IOrderService
//{
//    private readonly IUnitOfWork _unitOfWork;
//    private readonly IMapper _mapper;
//    private readonly ILogger<OrderService> _logger;

//    public OrderService(
//        IUnitOfWork unitOfWork,
//        IMapper mapper,
//        ILogger<OrderService> logger)
//    {
//        _unitOfWork = unitOfWork;
//        _mapper = mapper;
//        _logger = logger;
//    }

//    // ============ CREATE ORDER ============
//    public async Task<Result<OrderConfirmationDTO>> CreateOrderAsync(CreateOrderDTO dto)
//    {

//        try
//        {
//            _logger.LogInformation("🔵 Starting order creation - UserId: {UserId}, Items count: {ItemCount}",
//                dto.UserId, dto.Items?.Count ?? 0);

//            //  Validazione items
//            if (dto.Items == null || !dto.Items.Any())
//            {
//                _logger.LogWarning("❌ Order creation failed: Empty cart");
//                return Result.Failure<OrderConfirmationDTO>("Il carrello è vuoto",
//                    ErrorCode.EmptyCart);
//            }

//            _logger.LogInformation("✅ Items validation passed - {ItemCount} items", dto.Items.Count);

//            // Log DTO details for debugging
//            _logger.LogInformation("📦 Order DTO Details:");
//            _logger.LogInformation("  - CustomerFirstName: {FirstName}", dto.CustomerFirstName ?? "NULL");
//            _logger.LogInformation("  - CustomerLastName: {LastName}", dto.CustomerLastName ?? "NULL");
//            _logger.LogInformation("  - CustomerEmail: {Email}", dto.CustomerEmail ?? "NULL");
//            _logger.LogInformation("  - CustomerPhone: {Phone}", dto.CustomerPhone ?? "NULL");
//            _logger.LogInformation("  - ShippingStreet: {Street}", dto.ShippingStreet ?? "NULL");
//            _logger.LogInformation("  - ShippingCity: {City}", dto.ShippingCity ?? "NULL");
//            _logger.LogInformation("  - ShippingPostalCode: {PostalCode}", dto.ShippingPostalCode ?? "NULL");
//            _logger.LogInformation("  - ShippingCountry: {Country}", dto.ShippingCountry ?? "NULL");

//            // Inizia transazione
//            _logger.LogInformation("🔄 Beginning database transaction");
//            await _unitOfWork.BeginTransactionAsync();

//            //  Genera numero ordine unico
//            _logger.LogInformation("🔢 Generating order number");
//            var orderNumber = await GenerateOrderNumberAsync();
//            _logger.LogInformation("✅ Order number generated: {OrderNumber}", orderNumber);

//            //  Ottieni info cliente (da User o da DTO)
//            _logger.LogInformation("👤 Retrieving customer info - IsAuthenticated: {IsAuth}", dto.UserId.HasValue);
//            var customerInfo = await GetCustomerInfoAsync(dto);
//            if (customerInfo.IsFailure)
//            {
//                _logger.LogError("❌ Customer info retrieval failed: {Error} (ErrorCode: {ErrorCode})",
//                    customerInfo.Error, customerInfo.ErrorCode);
//                await _unitOfWork.RollbackTransactionAsync();
//                return Result.Failure<OrderConfirmationDTO>(
//                    customerInfo.Error!,
//                    customerInfo.ErrorCode!);
//            }

//            var customer = customerInfo.Value!;
//            _logger.LogInformation("✅ Customer info retrieved: {FirstName} {LastName} ({Email})",
//                customer.FirstName, customer.LastName, customer.Email);

//            //  Crea ordine
//            _logger.LogInformation("📝 Creating order entity");
//            var order = new Order
//            {
//                OrderNumber = orderNumber,
//                OrderDate = DateTime.UtcNow,
//                UserId = dto.UserId,

//                // Customer Info
//                CustomerFirstName = customer.FirstName,
//                CustomerLastName = customer.LastName,
//                CustomerEmail = customer.Email,
//                CustomerPhone = customer.Phone,

//                // Shipping Address
//                ShippingStreet = dto.ShippingStreet,
//                ShippingCity = dto.ShippingCity,
//                ShippingPostalCode = dto.ShippingPostalCode,
//                ShippingCountry = dto.ShippingCountry,

//                Status = OrderStatus.Processing,
//                PaymentMethod = "Cash on Delivery",
//                Notes = dto.Notes,
//                CreatedAt = DateTime.UtcNow
//            };

//            //  Processa order items e aggiorna stock
//            _logger.LogInformation("📦 Processing {ItemCount} order items", dto.Items.Count);
//            decimal subtotal = 0;

//            foreach (var itemDto in dto.Items)
//            {
//                _logger.LogInformation("  - Processing item: ProductId={ProductId}, Quantity={Quantity}, UnitPrice={UnitPrice}",
//                    itemDto.ProductId, itemDto.Quantity, itemDto.UnitPrice);

//                var product = await _unitOfWork.Products.GetByIdAsync(itemDto.ProductId);

//                if (product == null)
//                {
//                    _logger.LogError("❌ Product not found: ProductId={ProductId}", itemDto.ProductId);
//                    await _unitOfWork.RollbackTransactionAsync();
//                    return Result.Failure<OrderConfirmationDTO>(
//                        $"Prodotto {itemDto.ProductId} non trovato",
//                        ErrorCode.ProductNotFound);
//                }

//                _logger.LogInformation("  ✅ Product found: {ProductName} (Stock: {Stock}, Available: {IsAvailable})",
//                    product.Name, product.StockQuantity, product.IsAvailable);

//                if (!product.IsAvailable)
//                {
//                    _logger.LogError("❌ Product not available: {ProductName}", product.Name);
//                    await _unitOfWork.RollbackTransactionAsync();
//                    return Result.Failure<OrderConfirmationDTO>(
//                        $"Prodotto '{product.Name}' non disponibile",
//                        ErrorCode.ProductNotAvailable);
//                }

//                if (product.StockQuantity < itemDto.Quantity)
//                {
//                    _logger.LogError("❌ Insufficient stock for {ProductName}: Requested={Requested}, Available={Available}",
//                        product.Name, itemDto.Quantity, product.StockQuantity);
//                    await _unitOfWork.RollbackTransactionAsync();
//                    return Result.Failure<OrderConfirmationDTO>(
//                        $"Stock insufficiente per '{product.Name}'. Disponibili: {product.StockQuantity}",
//                        ErrorCode.InsufficientStock);
//                }

//                //  Aggiorna stock
//                product.StockQuantity -= itemDto.Quantity;
//                product.UpdatedAt = DateTime.UtcNow;
//                _unitOfWork.Products.Update(product);
//                _logger.LogInformation("  ✅ Stock updated for {ProductName}: New stock={NewStock}",
//                    product.Name, product.StockQuantity);

//                //  Crea order item
//                var orderItem = new OrderItem
//                {
//                    ProductId = itemDto.ProductId,
//                    Quantity = itemDto.Quantity,
//                    UnitPrice = itemDto.UnitPrice,
//                    CreatedAt = DateTime.UtcNow
//                };

//                order.OrderItems.Add(orderItem);
//                subtotal += orderItem.Subtotal;
//                _logger.LogInformation("  ✅ Order item created: Subtotal={Subtotal}", orderItem.Subtotal);
//            }

//            //  Calcola totali
//            _logger.LogInformation("💰 Calculating order totals");
//            order.Subtotal = subtotal;
//            order.ShippingCost = CalculateShippingCost(subtotal);
//            order.Tax = CalculateTax(subtotal);
//            order.TotalAmount = order.Subtotal + order.ShippingCost + order.Tax;
//            _logger.LogInformation("  - Subtotal: {Subtotal}", order.Subtotal);
//            _logger.LogInformation("  - Shipping: {Shipping}", order.ShippingCost);
//            _logger.LogInformation("  - Tax: {Tax}", order.Tax);
//            _logger.LogInformation("  - Total: {Total}", order.TotalAmount);

//            //  Salva ordine
//            _logger.LogInformation("💾 Saving order to database");
//            await _unitOfWork.Orders.AddAsync(order);
//            await _unitOfWork.SaveChangesAsync();
//            _logger.LogInformation("✅ Order saved to database");

//            await _unitOfWork.CommitTransactionAsync();
//            _logger.LogInformation("✅ Transaction committed");

//            _logger.LogInformation(
//                "🎉 Order {OrderNumber} created successfully - Total: {Total}",
//                order.OrderNumber,
//                order.TotalAmount);

//            // Ricarica order con Include per AutoMapper
//            _logger.LogInformation("🔄 Reloading order with includes for mapping");
//            var savedOrder = await _unitOfWork.Orders
//                .Query()
//                .Include(o => o.OrderItems)
//                .ThenInclude(oi => oi.Product)
//                .FirstOrDefaultAsync(o => o.Id == order.Id);

//            //  Mappa a DTO
//            var orderDto = _mapper.Map<OrderDTO>(savedOrder);

//            var confirmation = new OrderConfirmationDTO
//            {
//                OrderNumber = order.OrderNumber,
//                OrderDate = order.OrderDate,
//                CustomerEmail = order.CustomerEmail,
//                TotalAmount = order.TotalAmount,
//                Message = "Grazie per il tuo ordine! Riceverai una conferma via email.",
//                OrderDetails = orderDto
//            };

//            return Result.Success(confirmation);
//        }
//        catch (Exception ex)
//        {
//            await _unitOfWork.RollbackTransactionAsync();
//            _logger.LogError(ex, "💥 CRITICAL ERROR creating order - Exception Type: {ExceptionType}, Message: {Message}, StackTrace: {StackTrace}",
//                ex.GetType().Name, ex.Message, ex.StackTrace);

//            // Log inner exception if exists
//            if (ex.InnerException != null)
//            {
//                _logger.LogError("  Inner Exception: {InnerExceptionType} - {InnerMessage}",
//                    ex.InnerException.GetType().Name, ex.InnerException.Message);
//            }

//            return Result.Failure<OrderConfirmationDTO>(
//                $"Errore durante la creazione dell'ordine: {ex.Message}",
//                ErrorCode.OrderCreationFailed);
//        }
//    }
//}
