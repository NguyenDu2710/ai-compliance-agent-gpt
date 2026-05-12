using System.Text.Json;

namespace AIComplianceAgent.Api.Extensions;

public static class JsonResultFactory
{
    public static IResult FromString(string json)
    {
        using var document = JsonDocument.Parse(json);
        return Results.Json(document.RootElement.Clone());
    }
}
