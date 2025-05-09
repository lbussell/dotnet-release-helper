using Microsoft.Extensions.Configuration;
using Octokit;
using System.Text.RegularExpressions;

namespace ReleaseHelper.Cli;

public record Commit(string Sha, string Message, Uri? PullRequestUrl, string Author);

public partial class GitHubHelper
{
    private readonly GitHubClient _githubClient;
    private readonly string _owner;
    private readonly string _repo;

    public GitHubHelper(string owner, string repo)
    {
        _owner = owner;
        _repo = repo;
        _githubClient = CreateGitHubClient();
    }

    public async Task<IEnumerable<Commit>> GetCommits(DateTimeOffset since, string untilSha, string branch)
    {
        var commitRequest = new CommitRequest
        {
            Sha = branch,
            Since = since
        };

        var githubCommits = await _githubClient.Repository.Commit.GetAll(_owner, _repo, commitRequest);
        return ProcessCommits(githubCommits, untilSha);

        IEnumerable<Commit> ProcessCommits(IReadOnlyList<GitHubCommit> githubCommits, string untilSha)
        {
            foreach (var commit in githubCommits)
            {
                var commitMessage = commit.Commit.Message;
                var firstLine = commitMessage.Split('\n').First();

                // Extract PR number from commit message and create PR URL
                var prNumber =
                    PullRequestNumberPattern
                        .Matches(commitMessage)
                        .FirstOrDefault()?
                        .Value;

                var pullRequestUrl =
                    prNumber is null ?  commit.HtmlUrl
                        : $"https://github.com/{_owner}/{_repo}/pull/{prNumber}";

                // Get author name - prefer the GitHub user login if available, fall back to commit author name
                string author = commit.Author?.Login ?? commit.Commit.Author.Name;

                yield return new Commit(
                    Sha: commit.Sha,
                    Message: firstLine,
                    PullRequestUrl: new Uri(pullRequestUrl),
                    Author: author);

                if (commit.Sha.StartsWith(untilSha))
                {
                    break;
                }
            }
        }
    }

    private GitHubClient CreateGitHubClient()
    {
        var productHeaderValue = new ProductHeaderValue("ReleaseHelper");
        var client = new GitHubClient(productHeaderValue);

        // Get GitHub token from user secret
        var configurationBuilder = new ConfigurationBuilder()
            .AddUserSecrets<Program>();
        var userSecretsConfiguration = configurationBuilder.Build();
        var gitHubToken = userSecretsConfiguration["GitHub:Token"];
        var credentials = new Credentials(gitHubToken);
        client.Credentials = credentials;

        return client;
    }

    [GeneratedRegex(@"\(#(\d+)\)", RegexOptions.Compiled)]
    private static partial Regex PullRequestNumberPattern { get; }
}
