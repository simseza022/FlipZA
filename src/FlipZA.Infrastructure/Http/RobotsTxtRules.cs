using System.Text;
using System.Text.RegularExpressions;

namespace FlipZA.Infrastructure.Http;

/// <summary>
/// Minimal robots.txt evaluator following RFC 9309: picks the group matching our product token
/// (falling back to "*"), supports * and $ wildcards, longest match wins, ties go to Allow.
/// </summary>
public sealed class RobotsTxtRules
{
    private readonly List<(Regex Pattern, int Length, bool Allow)> _rules;

    private RobotsTxtRules(List<(Regex, int, bool)> rules) => _rules = rules;

    public static RobotsTxtRules AllowAll { get; } = new([]);

    public static RobotsTxtRules DisallowAll { get; } = new([(ToRegex("/"), 1, false)]);

    public bool IsAllowed(string pathAndQuery)
    {
        (int Length, bool Allow)? best = null;

        foreach (var rule in _rules)
        {
            if (!rule.Pattern.IsMatch(pathAndQuery))
                continue;

            if (best is null
                || rule.Length > best.Value.Length
                || (rule.Length == best.Value.Length && rule.Allow))
            {
                best = (rule.Length, rule.Allow);
            }
        }

        return best?.Allow ?? true;
    }

    public static RobotsTxtRules Parse(string content, string productToken)
    {
        var groups = new List<(List<string> Agents, List<(string Directive, string Value)> Rules)>();
        (List<string> Agents, List<(string, string)> Rules)? current = null;
        var lastWasAgent = false;

        foreach (var rawLine in content.Split('\n'))
        {
            var line = StripComment(rawLine).Trim();
            if (line.Length == 0)
                continue;

            var separator = line.IndexOf(':');
            if (separator < 0)
                continue;

            var directive = line[..separator].Trim().ToLowerInvariant();
            var value = line[(separator + 1)..].Trim();

            if (directive == "user-agent")
            {
                if (current is null || !lastWasAgent)
                {
                    current = (new List<string>(), new List<(string, string)>());
                    groups.Add(current.Value);
                }

                current.Value.Agents.Add(value.ToLowerInvariant());
                lastWasAgent = true;
            }
            else if (directive is "allow" or "disallow" && current is not null)
            {
                current.Value.Rules.Add((directive, value));
                lastWasAgent = false;
            }
            else
            {
                lastWasAgent = false;
            }
        }

        var token = productToken.ToLowerInvariant();
        var matching = groups.Where(g => g.Agents.Contains(token)).ToList();
        if (matching.Count == 0)
            matching = groups.Where(g => g.Agents.Contains("*")).ToList();

        var rules = matching
            .SelectMany(g => g.Rules)
            .Where(r => r.Value.Length > 0) // an empty Disallow means "allow everything"
            .Select(r => (ToRegex(r.Value), r.Value.Length, r.Directive == "allow"))
            .ToList();

        return new RobotsTxtRules(rules);
    }

    private static string StripComment(string line)
    {
        var hash = line.IndexOf('#');
        return hash >= 0 ? line[..hash] : line;
    }

    private static Regex ToRegex(string pattern)
    {
        var anchoredEnd = pattern.EndsWith('$');
        if (anchoredEnd)
            pattern = pattern[..^1];

        var regex = new StringBuilder("^");
        foreach (var ch in pattern)
            regex.Append(ch == '*' ? ".*" : Regex.Escape(ch.ToString()));
        if (anchoredEnd)
            regex.Append('$');

        return new Regex(regex.ToString(), RegexOptions.CultureInvariant);
    }
}
