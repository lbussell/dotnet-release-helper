using Microsoft.Extensions.Configuration;
using Octokit;

namespace ReleaseHelper.Cli;

public record Commit(string Sha, string Message, Uri PullRequestUrl);

public class GitHubHelper
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
                var commitUri =
                    new Uri(commit.HtmlUrl ?? $"https://github.com/{_owner}/{_repo}/commit/{commit.Sha}");

                yield return new Commit(
                    Sha: commit.Sha,
                    Message: commit.Commit.Message.Split('\n').First(),
                    PullRequestUrl: commitUri);

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
}
