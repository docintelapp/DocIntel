using System;
using System.Linq;

namespace DocIntel.Core.Helpers;

public static class StringHelpers
{
    public static string FromDashToLowerCamelCase(this string input)
    {
        switch (input)
        {
            case null:
                throw new ArgumentNullException(nameof(input));
            case "":
                return "";
            default:
                var input2 = input.FromDashToCamelCase(); 
                return string.Concat(input2[0].ToString().ToLower(), input2.AsSpan(1));
        }
    }

    public static string FromDashToCamelCase(this string input) =>
        input switch
        {
            null => throw new ArgumentNullException(nameof(input)),
            "" => "",
            _ => string.Join("", input.Split('-').Select(FirstCharToUpper))
        };
    public static string FirstCharToUpper(this string input) =>
        input switch
        {
            null => throw new ArgumentNullException(nameof(input)),
            "" => throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input)),
            _ => string.Concat(input[0].ToString().ToUpper(), input.AsSpan(1))
        };
}