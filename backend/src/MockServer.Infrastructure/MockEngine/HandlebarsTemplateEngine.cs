using System.Text.RegularExpressions;
using HandlebarsDotNet;
using MockServer.Core.Interfaces;
using Newtonsoft.Json.Linq;

namespace MockServer.Infrastructure.MockEngine;

public class HandlebarsTemplateEngine : ITemplateEngine
{
    private readonly IHandlebars _handlebars;

    public HandlebarsTemplateEngine()
    {
        _handlebars = Handlebars.Create();
        RegisterHelpers();
    }

    public string Render(string template, TemplateContext context)
    {
        // BE-03: Validate block helpers have required parameters
        ValidateTemplate(template);

        // BE-02: Fix triple-brace ambiguity (e.g., {"num":{{randomInt 1 100}}})
        // HandlebarsDotNet can't distinguish }}+} from }}} (triple-stache close).
        // Replace }}} with a marker before compilation, then restore after rendering.
        const string marker = "__HBS_BRACE__";
        var processed = template.Replace("}}}", "}}" + marker);

        var compiled = _handlebars.Compile(processed);
        var result = compiled(context);

        return result.Replace(marker, "}");
    }

    private static void ValidateTemplate(string template)
    {
        if (Regex.IsMatch(template, @"\{\{#(if|unless)\s*\}\}"))
        {
            throw new HandlebarsException("Block helper requires a parameter: found {{#if}} or {{#unless}} without condition");
        }
    }

    private void RegisterHelpers()
    {
        // {{jsonPath body "$.user.name"}}
        _handlebars.RegisterHelper("jsonPath", (output, _, arguments) =>
        {
            if (arguments.Length < 2) return;
            var json = arguments[0]?.ToString();
            var path = arguments[1]?.ToString();
            if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(path)) return;

            try
            {
                var token = JToken.Parse(json);
                var selected = token.SelectToken(path);
                output.WriteSafeString(selected?.ToString() ?? "");
            }
            catch
            {
                // Silently fail for invalid JSON/path
            }
        });

        // {{now "yyyy-MM-dd"}} or {{now}}
        _handlebars.RegisterHelper("now", (output, _, arguments) =>
        {
            var format = arguments.Length > 0 ? arguments[0]?.ToString() : "yyyy-MM-ddTHH:mm:ss";
            output.WriteSafeString(DateTime.UtcNow.ToString(format ?? "yyyy-MM-ddTHH:mm:ss"));
        });

        // {{uuid}}
        _handlebars.RegisterHelper("uuid", (output, _, _) =>
        {
            output.WriteSafeString(Guid.NewGuid().ToString());
        });

        // {{randomInt 1 100}}
        _handlebars.RegisterHelper("randomInt", (output, _, arguments) =>
        {
            if (arguments.Length < 2) return;
            if (int.TryParse(arguments[0]?.ToString(), out var min) &&
                int.TryParse(arguments[1]?.ToString(), out var max))
            {
                output.WriteSafeString(Random.Shared.Next(min, max + 1).ToString());
            }
        });

        // {{math 5 "+" 3}}
        _handlebars.RegisterHelper("math", (output, _, arguments) =>
        {
            if (arguments.Length < 3) return;
            if (!decimal.TryParse(arguments[0]?.ToString(), out var a)) return;
            var op = arguments[1]?.ToString();
            if (!decimal.TryParse(arguments[2]?.ToString(), out var b)) return;

            var result = op switch
            {
                "+" => a + b,
                "-" => a - b,
                "*" => a * b,
                "/" when b != 0 => a / b,
                "%" when b != 0 => a % b,
                _ => 0m
            };
            output.WriteSafeString(result.ToString());
        });

        // {{eq a b}} -> boolean for conditionals
        _handlebars.RegisterHelper("eq", (output, _, arguments) =>
        {
            if (arguments.Length < 2) return;
            var result = string.Equals(arguments[0]?.ToString(), arguments[1]?.ToString(), StringComparison.OrdinalIgnoreCase);
            output.WriteSafeString(result ? "true" : "false");
        });

        // {{ne a b}}
        _handlebars.RegisterHelper("ne", (output, _, arguments) =>
        {
            if (arguments.Length < 2) return;
            var result = !string.Equals(arguments[0]?.ToString(), arguments[1]?.ToString(), StringComparison.OrdinalIgnoreCase);
            output.WriteSafeString(result ? "true" : "false");
        });

        // {{gt a b}}
        _handlebars.RegisterHelper("gt", (output, _, arguments) =>
        {
            if (arguments.Length < 2) return;
            if (decimal.TryParse(arguments[0]?.ToString(), out var a) &&
                decimal.TryParse(arguments[1]?.ToString(), out var b))
            {
                output.WriteSafeString(a > b ? "true" : "false");
            }
        });

        // {{lt a b}}
        _handlebars.RegisterHelper("lt", (output, _, arguments) =>
        {
            if (arguments.Length < 2) return;
            if (decimal.TryParse(arguments[0]?.ToString(), out var a) &&
                decimal.TryParse(arguments[1]?.ToString(), out var b))
            {
                output.WriteSafeString(a < b ? "true" : "false");
            }
        });

        // {{stringify obj}} -> JSON.serialize
        _handlebars.RegisterHelper("stringify", (output, _, arguments) =>
        {
            if (arguments.Length < 1) return;
            try
            {
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(arguments[0]);
                output.WriteSafeString(json);
            }
            catch
            {
                output.WriteSafeString(arguments[0]?.ToString() ?? "");
            }
        });
    }
}
