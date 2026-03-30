using System.Text.Json.Serialization;

namespace AudioStore.Common.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DiscountType
{
    Percentage,
    FixedAmount
}