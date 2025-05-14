# dotnet-release-helper

> [!CAUTION]
> This repo is archived and will not be maintained any longer. Please don't use it for anything important. Thanks.

## What is this?

This is a tool that I use to make releasing .NET container images easier.

## Setup

To avoid GitHub rate limiting, create GitHub token and store it using .NET user secrets:

```
dotnet user-secrets set "GitHub:Token" "$GITHUB_TOKEN"
```

## Usage

### List commits

```
Usage: commits [options...] [-h|--help] [--version]

List commits from a branch up to a specific commit sha.

Options:
  --to-sha <string>     (Required)
  --owner <string>      (Default: @"dotnet")
  --repo <string>       (Default: @"dotnet-docker")
  --branch <string>     (Default: @"nightly")
```

### Pick commits

```
Usage: pick-commits [options...] [-h|--help] [--version]

Interactively pick commits from a branch up to a specific commit sha. Prints a summary of the selected commits
     in an unordered markdown list as well as prints a git command to cherry-pick all of the selected commits.

Options:
  --to-sha <string>     (Required)
  --owner <string>      (Default: @"dotnet")
  --repo <string>       (Default: @"dotnet-docker")
  --branch <string>     (Default: @"nightly")
```
