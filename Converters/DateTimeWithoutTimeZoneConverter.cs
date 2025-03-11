using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StoreIRPCAPI.Converters;

/// <summary>
/// Converter สำหรับแปลง DateTime ให้ไม่มี timezone
/// </summary>
public class DateTimeWithoutTimeZoneConverter : JsonConverter<DateTime?>
{
    private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";

    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        string? dateTimeString = reader.GetString();
        if (string.IsNullOrEmpty(dateTimeString))
        {
            return null;
        }

        if (DateTime.TryParse(dateTimeString, out DateTime dateTime))
        {
            return DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified);
        }

        return null;
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        // กำหนดให้ DateTime เป็น Unspecified เพื่อไม่ให้มี timezone
        DateTime dateTimeWithoutTimeZone = DateTime.SpecifyKind(value.Value, DateTimeKind.Unspecified);
        writer.WriteStringValue(dateTimeWithoutTimeZone.ToString(DateTimeFormat));
    }
} 