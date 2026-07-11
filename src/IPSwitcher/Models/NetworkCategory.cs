using System.Text.Json.Serialization;

namespace IPSwitcher.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum NetworkCategory
{
    Public = 0,
    Private = 1,
}
