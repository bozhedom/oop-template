using System.Text.Json;

namespace Chat.Common;

public record ChatMessage(
    string Type,
    string Nick = "",
    string Text = "",
    string To = "",
    DateTime Time = default,
    string[]? Users = null)
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public string ToJson() => JsonSerializer.Serialize(this, Options);

    public static ChatMessage? FromJson(string json)
    {
        try { return JsonSerializer.Deserialize<ChatMessage>(json, Options); }
        catch (JsonException) { return null; }
    }
}
