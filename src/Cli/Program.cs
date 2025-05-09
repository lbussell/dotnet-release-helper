using ConsoleAppFramework;
using ReleaseHelper.Cli;

var app = ConsoleApp.Create();
app.Add<Commands>();
app.Run(args);

class Commands
{
    public async Task Commits(
        string toSha,
        string owner = "dotnet",
        string repo = "dotnet-docker",
        string branch = "nightly")
    {
        var gitHubHelper = new GitHubHelper(owner, repo);
        var twoMonthsAgo = DateTimeOffset.UtcNow.AddMonths(-2);

        var commits = await gitHubHelper.GetCommits(since: twoMonthsAgo, untilSha: toSha, branch: branch);

        Console.WriteLine();
        foreach (var commit in commits)
        {
            Console.WriteLine(
                $"""
                {commit.Message}
                Commit: {commit.Sha}
                Author: {commit.Author}
                Pull Request: {commit.PullRequestUrl}

                """);
        }
    }
}
