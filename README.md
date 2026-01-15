### Audio Store E-commerce project 
---

## 📁 Backend Project Structure

```
AudioStore.Solution/
│
├── AudioStore.Domain/                    # CORE - Nessuna dipendenza
│   ├── Entities/                         # User, Product, Order, Category, etc.
│   ├── Enums/                           # OrderStatus, UserRole, etc.
│   ├── Exceptions/                      # Domain-specific exceptions
│   └── Interfaces/                      # IRepository<T>, IUnitOfWork
│
├── AudioStore.Application/              # Business Logic
│   ├── DTOs/                           # Request/Response DTOs
│   │   ├── Auth/                       # LoginDTO, RegisterDTO
│   │   ├── Products/                   # ProductDTO, CreateProductDTO
│   │   ├── Orders/                     # OrderDTO, CreateOrderDTO
│   │   └── Customers/                  # CustomerDTO, CustomerFilterDTO
│   ├── Services/                       # Business Services
│   │   ├── Interfaces/                 # IAuthService, IProductService
│   │   └── Implementations/            # AuthService, ProductService
│   ├── Mappings/                       # AutoMapper Profiles
│   ├── Validators/                     # FluentValidation
│   └── Common/                         # Result<T>, PaginatedResult<T>
│
├── AudioStore.Infrastructure/           # Data Access & External Services
│   ├── Data/
│   │   ├── AppDbContext.cs
│   │   └── Configurations/             # EF Core Entity Configurations
│   ├── Repositories/                   # Generic & Specific Repositories
│   ├── Identity/                       # Identity configurations
│   ├── Caching/                        # Redis/Memory Cache
│   └── Migrations/
│
├── AudioStore.API/                      # Presentation Layer
│   ├── Controllers/
│   │   ├── AuthController.cs
│   │   ├── ProductsController.cs
│   │   ├── OrdersController.cs
│   │   ├── Admin/
│   │   │   ├── AdminProductsController.cs
│   │   │   ├── AdminOrdersController.cs
│   │   │   ├── AdminCustomersController.cs
│   │   │   └── AdminStatisticsController.cs
│   ├── Middleware/                     # Exception handling, Logging
│   ├── Filters/                        # Authorization, Validation
│   └── Extensions/                     # Service registration
│
├── AudioStore.Shared/                   # Shared across all layers
│   ├── Constants/
│   │   ├── AuthConstants.cs
│   │   ├── CacheConstants.cs
│   │   ├── ErrorCodes.cs
│   │   └── RoleNames.cs
│   └── Extensions/                     # Helper extensions
│
└── AudioStore.Tests/
    ├── UnitTests/
    ├── IntegrationTests/
    └── Helpers/
```
