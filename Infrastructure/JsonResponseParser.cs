using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentOllamaPOC.Infrastructure;

public static class JsonResponseParser
{
    private static readonly JsonSerializerOptions Options =
        new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };

    public static T Parse<T>(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
            throw new InvalidOperationException("LLM returned an empty response.");

        // Handle string type directly without JSON deserialization
        if (typeof(T) == typeof(string))
            return (T)(object)response;

        // Strip Markdown code block formatting if present
        var cleanedResponse = response.Trim();

        // Remove markdown code block wrappers (```json ... ``` or ``` ... ```)
        if (cleanedResponse.StartsWith("```"))
        {
            // Find the end of the opening fence (skip optional language identifier like 'json')
            var firstNewline = cleanedResponse.IndexOf('\n');
            if (firstNewline > 0)
            {
                cleanedResponse = cleanedResponse.Substring(firstNewline + 1);
            }

            // Remove closing fence and everything after it
            var closingFence = cleanedResponse.LastIndexOf("```");
            if (closingFence > 0)
            {
                cleanedResponse = cleanedResponse.Substring(0, closingFence);
            }
        }

        cleanedResponse = cleanedResponse.Trim();

        // Extract JSON if response contains text before/after JSON
        cleanedResponse = ExtractJsonFromResponse(cleanedResponse);

        // Validate that the response appears to be JSON before attempting deserialization
        if (!IsValidJsonStart(cleanedResponse))
        {
            throw new InvalidOperationException(
                $"LLM response is not valid JSON format.{Environment.NewLine}Original: {response}{Environment.NewLine}Cleaned: {cleanedResponse}");
        }

        try
        {
            var result = JsonSerializer.Deserialize<T>(cleanedResponse, Options);

            if (result is null)
                throw new InvalidOperationException("Failed to deserialize LLM response.");

            return result;
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Invalid JSON returned by the LLM.{Environment.NewLine}{cleanedResponse}",
                ex);
        }
    }

    private static string ExtractJsonFromResponse(string response)
    {
        // Try to find JSON object or array in the response
        var openBrace = response.IndexOf('{');
        var openBracket = response.IndexOf('[');

        int jsonStart = -1;

        if (openBrace >= 0 && (openBracket < 0 || openBrace < openBracket))
        {
            jsonStart = openBrace;
        }
        else if (openBracket >= 0)
        {
            jsonStart = openBracket;
        }

        if (jsonStart > 0)
        {
            // Found JSON after some text, extract from there
            response = response.Substring(jsonStart);
        }

        // Also try to remove trailing text after JSON ends
        if (response.Contains('}'))
        {
            var lastBrace = response.LastIndexOf('}');
            if (lastBrace >= 0 && lastBrace < response.Length - 1)
            {
                response = response.Substring(0, lastBrace + 1);
            }
        }
        else if (response.Contains(']'))
        {
            var lastBracket = response.LastIndexOf(']');
            if (lastBracket >= 0 && lastBracket < response.Length - 1)
            {
                response = response.Substring(0, lastBracket + 1);
            }
        }

        return response.Trim();
    }

    private static bool IsValidJsonStart(string response)
    {
        if (string.IsNullOrEmpty(response))
            return false;

        var firstChar = response[0];
        return firstChar == '{' || firstChar == '[' || firstChar == '"' ||
               firstChar == 't' || firstChar == 'f' || firstChar == 'n' ||
               char.IsDigit(firstChar) || firstChar == '-';
    }
}