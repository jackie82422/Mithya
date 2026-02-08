using System.Text.RegularExpressions;

namespace MockServer.Infrastructure.MockEngine;

public static class PathMatcher
{
    public static bool IsMatch(string endpointPath, string requestPath)
    {
        // Exact match (case-insensitive)
        if (string.Equals(endpointPath, requestPath, StringComparison.OrdinalIgnoreCase))
            return true;

        // Check if endpoint path contains path parameters like {id}
        if (!endpointPath.Contains('{'))
            return false;

        var pattern = "^" + Regex.Replace(endpointPath, @"\{[^}]+\}", "([^/]+)") + "$";
        return Regex.IsMatch(requestPath, pattern, RegexOptions.IgnoreCase);
    }

    public static Dictionary<string, string> ExtractPathParams(string endpointPath, string requestPath)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (!endpointPath.Contains('{'))
            return result;

        // Extract parameter names
        var paramNames = Regex.Matches(endpointPath, @"\{([^}]+)\}")
            .Select(m => m.Groups[1].Value)
            .ToList();

        // Build regex to extract values
        var pattern = "^" + Regex.Replace(endpointPath, @"\{[^}]+\}", "([^/]+)") + "$";
        var match = Regex.Match(requestPath, pattern, RegexOptions.IgnoreCase);

        if (match.Success)
        {
            for (int i = 0; i < paramNames.Count && i + 1 < match.Groups.Count; i++)
            {
                result[paramNames[i]] = match.Groups[i + 1].Value;
            }
        }

        return result;
    }
}
