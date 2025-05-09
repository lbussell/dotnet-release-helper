using ConsoleAppFramework;
using ReleaseHelper.Cli;

var app = ConsoleApp.Create();
app.Add<Commands>();
app.Run(args);

class Commands
{
    /// <summary>
    /// List commits from a branch up to a specific commit sha.
    /// </summary>
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

    /// <summary>
    /// Interactively pick commits from a branch up to a specific commit sha. Prints a summary of selected commits in
    /// markdown format as well as the git cherry-pick command to run.
    /// </summary>
    public async Task PickCommits(
        string toSha,
        string owner = "dotnet",
        string repo = "dotnet-docker",
        string branch = "nightly")
    {
        var gitHubHelper = new GitHubHelper(owner, repo);
        var twoMonthsAgo = DateTimeOffset.UtcNow.AddMonths(-2);
        var commits = await gitHubHelper.GetCommits(since: twoMonthsAgo, untilSha: toSha, branch: branch);

        // Reverse the order of commits to show the oldest first
        commits = commits.Reverse();

        Console.WriteLine("\nSelect commits to include.\n");
        var selectedCommits = new List<Commit>();

        foreach (var commit in commits)
        {
            Console.WriteLine(
                $"""
                Message: {commit.Message}
                Commit: {commit.Sha}
                Author: {commit.Author}
                Pull Request: {commit.PullRequestUrl}
                Include this commit? (y/n):
                """);

            var accepted = AskForUserAcceptance();
            if (accepted)
            {
                selectedCommits.Add(commit);
                Console.WriteLine($"Selecting commit {commit.Sha}.\n");
            }
            else
            {
                Console.WriteLine($"Skipping commit {commit.Sha}.\n");
            }

            Console.WriteLine();
        }

        // Display summary of selected commits in markdown to put on GitHub
        Console.WriteLine($"\nMarkdown summary:");
        foreach (var commit in selectedCommits)
        {
            Console.WriteLine($" - {commit.PullRequestUrl} - {commit.Sha}");
        }

        var gitCherryPickCommand = "git cherry-pick";
        var commitList = string.Join(" ", selectedCommits.Select(c => c.Sha));
        Console.WriteLine(
            $"""

            To cherry-pick the selected commits, run the following command:
            {gitCherryPickCommand} {commitList}

            """);

    }

    private static bool AskForUserAcceptance()
    {
        var accepted = Console.ReadKey().KeyChar.ToString().Equals("y", StringComparison.OrdinalIgnoreCase);
        Console.WriteLine();
        return accepted;
    }
}
