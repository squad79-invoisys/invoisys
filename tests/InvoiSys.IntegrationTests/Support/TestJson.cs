using System.Text.Json;
using System.Text.Json.Serialization;

namespace InvoiSys.IntegrationTests.Support;

internal static class TestJson
{
    public static readonly JsonSerializerOptions Options = Criar();

    private static JsonSerializerOptions Criar()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
