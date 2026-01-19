namespace AudioStore.Domain.Enums;

public enum OrderStatus
{
    Pending = 0,           // In attesa di conferma
    Processing = 1,        // In elaborazione
    Shipped = 2,           // Spedito
    Delivered = 3,         // Consegnato
    Cancelled = 4          // Annullato
}
