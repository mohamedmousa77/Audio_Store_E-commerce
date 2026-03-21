namespace AudioStore.Common.Constants;

public static class ErrorCode
{
    // Authentication & Authorization
    public const string InvalidCredentials = "AUTH001";
    public const string EmailAlreadyExists = "AUTH002";
    public const string UserNotFound = "AUTH003";
    public const string InvalidToken = "AUTH004";
    public const string Unauthorized = "AUTH005";

    // Validation
    public const string ValidationError = "VAL001";
    public const string RequiredField = "VAL002";
    public const string InvalidFormat = "VAL003";

    // Products
    public const string ProductNotFound = "PROD001";
    public const string InsufficientStock = "PROD002";
    public const string ProductNotAvailable = "PROD003";
    public const string SlugAlreadyExists = "PROD004";

    // Categories
    public const string CategoryNotFound = "CAT001";

    // Orders
    public const string OrderNotFound = "ORD001";
    public const string EmptyCart = "ORD002";
    public const string OrderCreationFailed = "ORD003";

    // Cart
    public const string CartNotFound = "CART001";
    public const string InvalidQuantity = "CART002";

    // PromoCode
    public const string PromoCodeNotFound = "PROMO001";
    public const string PromoCodeExpired = "PROMO002";
    public const string PromoCodeInactive = "PROMO003";
    public const string PromoCodeAlreadyUsed = "PROMO004";
    public const string PromoCodeMinAmountNotMet = "PROMO005";
    public const string PromoCodeMaxUsagesReached = "PROMO006";
    public const string PromoCodeNotAssigned = "PROMO007";
    public const string PromoCodeAlreadyExists = "PROMO008";

    // General
    public const string InternalServerError = "GEN001";
    public const string NotFound = "GEN002";
    public const string BadRequest = "GEN003";
}
