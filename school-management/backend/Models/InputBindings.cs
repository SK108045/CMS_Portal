using System.Text.Json;
using System.Text.Json.Nodes;

namespace School.Api.Models;

// Accept scalar fields only: never attach a client-supplied EF navigation graph.
public static class InputBindings
{
    public static object? Read(JsonElement input, Type type)
    {
        var safe = new JsonObject();
        foreach (var property in type.GetProperties())
        {
            var t = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            if (
                !(
                    t.IsPrimitive
                    || t == typeof(string)
                    || t == typeof(decimal)
                    || t == typeof(DateOnly)
                )
            )
                continue;
            if (
                property.Name
                is "Id"
                    or "CreatedBy"
                    or "CreatedAt"
                    or "PasswordHash"
                    or "SessionVersion"
                    or "FailedLogins"
                    or "LockedUntil"
            )
                continue;
            var key = JsonNamingPolicy.CamelCase.ConvertName(property.Name);
            if (input.TryGetProperty(key, out var value))
                safe[key] = JsonNode.Parse(value.GetRawText());
        }
        if (type == typeof(PurchaseRequest) && input.TryGetProperty("items", out var items))
        {
            var array = new JsonArray();
            foreach (var item in items.EnumerateArray())
            {
                var row = new JsonObject();
                foreach (var key in new[] { "description", "quantity", "unitPrice" })
                    if (item.TryGetProperty(key, out var value))
                        row[key] = JsonNode.Parse(value.GetRawText());
                array.Add(row);
            }
            safe["items"] = array;
        }
        return safe.Deserialize(
            type,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );
    }
}
