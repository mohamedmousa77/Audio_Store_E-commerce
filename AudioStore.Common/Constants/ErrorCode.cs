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

    // Orders
    public const string OrderNotFound = "ORD001";
    public const string EmptyCart = "ORD002";
    public const string OrderCreationFailed = "ORD003";

    // Cart
    public const string CartNotFound = "CART001";
    public const string InvalidQuantity = "CART002";

    // General
    public const string InternalServerError = "GEN001";
    public const string NotFound = "GEN002";
    public const string BadRequest = "GEN003";
}
