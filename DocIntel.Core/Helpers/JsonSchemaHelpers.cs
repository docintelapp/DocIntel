using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Schema;
using Json.Schema.Generation;

namespace DocIntel.Core.Helpers;

public static class JsonSchemaHelpers
{
    public static string ToJsonEditorSchema(JsonSchema schema)
    {
        var str = JsonSerializer.Serialize(schema);
        var json = JsonSerializer.Deserialize<JsonNode>(str);
        var properties = json["properties"];
        foreach (var prop in properties.AsObject())
        {
            if (prop.Value != null)
            {
                var propValue = prop.Value.AsObject();
                if (propValue != null && propValue.ContainsKey("type"))
                {
                    if (prop.Value["type"].GetValue<string>() == "boolean")
                    {
                        prop.Value["format"] = "checkbox";
                    }    
                }
            }
        }

        return JsonSerializer.Serialize(json);
    }

    public static string ToJsonEditorSchema(Type t)
    {
        var generator = new JsonSchemaBuilder();
        var schema = generator.FromType(t).Build();
        return ToJsonEditorSchema(schema);
    }
}