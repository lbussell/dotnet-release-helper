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

    /// <summary>
    /// Gets all commits from a branch since a specific date. Starts at the most recent commit and works backwards.
    /// </summary>
    /// <param name="since">Get commits back to this date.</param>
    /// <param name="untilSha">Stop looking for commits when this Sha is found.</param>
    /// <param name="branch">The branch to look for commits on.</param>
    /// <returns>List of commits.</returns>
    public async Task<IEnumerable<Commit>> GetCommits(DateTimeOffset since, string untilSha, string branch)
    {
        var commitRequest = new CommitRequest
        {
            Sha = branch,
            Since = since
        };

        var githubCommits = await _githubClient.Repository.Commit.GetAll(_owner, _repo, commitRequest);
        return CreateCommitList(githubCommits, untilSha);

        IEnumerable<Commit> CreateCommitList(IReadOnlyList<GitHubCommit> githubCommits, string untilSha)
        {
            foreach (var commit in githubCommits)
            {
                var commitMessage = commit.Commit.Message;
                var shortCommitMessage = commitMessage.Split('\n').FirstOrDefault()
                    ?? throw new InvalidOperationException("Commit message is empty");

                var pullRequestUri = GetPullRequestUri(commit);

                // Get author name - prefer the GitHub user login if available, fall back to commit author name
                string author = commit.Author?.Login ?? commit.Commit.Author.Name;

                yield return new Commit(
                    Sha: commit.Sha,
                    Message: shortCommitMessage,
                    PullRequestUrl: pullRequestUri,
                    Author: author);

                if (commit.Sha.StartsWith(untilSha))
                {
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Extract the GitHub pull request link from a GitHub commit.
    /// </summary>
    /// <param name="commit">
    /// GitHub commit. Assumes that the commit message includes the GitHub pull request number in the format (#1234).
    /// </param>
    /// <returns>
    /// Uri for the GitHub pull request. If the commit message does not include a pull request number, the GitHub
    /// commit URL is returned instead.
    /// </returns>
    private Uri GetPullRequestUri(GitHubCommit commit)
    {
        var matches = PullRequestNumberPattern.Matches(commit.Commit.Message);
        var prNumber = matches
            .FirstOrDefault()?
            .Groups[1]
            .Value;

        // Fall back to GitHub commit URL
        var pullRequestUrl = prNumber is null
            ? commit.HtmlUrl
            : $"https://github.com/{_owner}/{_repo}/pull/{prNumber}";

        return new Uri(pullRequestUrl);
    }

    /// <summary>
    /// Creates an authenticated GitHub client. Assumes that the GitHub token is stored in dotnet user secrets with the
    /// key "GitHub:Token".
    /// </summary>
    /// <returns>A new, authenticated GitHubClient instance</returns>
    private static GitHubClient CreateGitHubClient()
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
