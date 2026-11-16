using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Prismbar.Core.GitHub;

public sealed class GitHubAuthException() : Exception("GitHub token rejected");
public sealed class GitHubRateLimitException() : Exception("GitHub rate limit reached");

public sealed record GitHubNotification(string Title, string Repository);
public sealed record GitHubPullRequest(string Title, string HtmlUrl, string Repository);

public sealed class GitHubClient(HttpClient http, string token)
{
    public async Task<string> GetLoginAsync(CancellationToken ct = default)
    {
        using var doc = await GetAsync("https://api.github.com/user", ct);
        return doc.RootElement.GetProperty("login").GetString() ?? "";
    }

    public async Task<IReadOnlyList<GitHubNotification>> GetUnreadNotificationsAsync(CancellationToken ct = default)
    {
        using var doc = await GetAsync("https://api.github.com/notifications?per_page=50", ct);
        return doc.RootElement.EnumerateArray().Select(n => new GitHubNotification(
            n.GetProperty("subject").GetProperty("title").GetString() ?? "",
            n.GetProperty("repository").GetProperty("full_name").GetString() ?? "")).ToList();
    }

    public async Task<(int Total, IReadOnlyList<GitHubPullRequest> Items)> GetReviewRequestsAsync(CancellationToken ct = default)
    {
        using var doc = await GetAsync("https://api.github.com/search/issues?q=is%3Aopen+is%3Apr+review-requested%3A%40me&per_page=10", ct);
        var items = doc.RootElement.GetProperty("items").EnumerateArray().Select(i =>
        {
            var repoUrl = i.GetProperty("repository_url").GetString() ?? "";
            var repo = repoUrl.Replace("https://api.github.com/repos/", "");
            return new GitHubPullRequest(i.GetProperty("title").GetString() ?? "", i.GetProperty("html_url").GetString() ?? "", repo);
        }).ToList();
        return (doc.RootElement.GetProperty("total_count").GetInt32(), items);
    }

    private async Task<JsonDocument> GetAsync(string url, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        req.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
        using var res = await http.SendAsync(req, ct);
        if (res.StatusCode == HttpStatusCode.Unauthorized) throw new GitHubAuthException();
        if (res.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.TooManyRequests) throw new GitHubRateLimitException();
        res.EnsureSuccessStatusCode();
        return JsonDocument.Parse(await res.Content.ReadAsStringAsync(ct));
    }
}
