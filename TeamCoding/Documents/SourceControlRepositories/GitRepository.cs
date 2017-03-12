﻿using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TeamCoding.VisualStudio;

namespace TeamCoding.Documents.SourceControlRepositories
{
    /// <summary>
    /// Provides methods to get information about a file in a Git repository
    /// </summary>
    public class GitRepository : ISourceControlRepository
    {
        private string GetRepoPath(string fullFilePath)
        {
            var repoPath = Repository.Discover(fullFilePath);

            if (repoPath == null) return null; // No repository for file

            return fullFilePath.Substring(new DirectoryInfo(repoPath).Parent.FullName.Length).TrimStart('\\');
        }
        public DocumentRepoMetaData GetRepoDocInfo(string fullFilePath)
        {
            var relativePath = GetRepoPath(fullFilePath);

            // It's ok to return null here since calling methods will handle it and it allows us to not have some global "is this a repository setting"
            // Another reason it's better to do it this way is there's no "before loading a solution" event, meaning lots of listeners get the IsEnabled setting change too late (after docs are loaded)
            if (relativePath == null) return null;

            var repo = new Repository(Repository.Discover(fullFilePath));

            if (repo.Ignore.IsPathIgnored(relativePath)) return null;

            var repoHeadTree = repo.Head.Tip.Tree;
            var remoteMasterTree = repo.Head.TrackedBranch.Tip.Tree;

            // Check for local changes, then server changes.
            // It's possible there is a local change that actually makes it the same as the remote, but I think that's ok to say the user is editing anyway
            var isEdited = repo.Diff.Compare<TreeChanges>(new[] { fullFilePath }).Any() ||
                           repo.Diff.Compare<TreeChanges>(remoteMasterTree, repoHeadTree, new[] { fullFilePath }).Any();

            return new DocumentRepoMetaData()
            {
                RepoProvider = nameof(GitRepository),
                RepoUrl = repo.Head.TrackedBranch.Remote.Url,
                RepoBranch = repo.Head.TrackedBranch.CanonicalName,
                RelativePath = relativePath,
                BeingEdited = isEdited,
                LastActioned = DateTime.UtcNow
            };
        }

        public (int[] LineAdditions, int[] LineDeletions)? GetDiffWithServer(string fullFilePath)
        {
            var relativePath = GetRepoPath(fullFilePath);

            // It's ok to return null here since calling methods will handle it and it allows us to not have some global "is this a repository setting"
            // Another reason it's better to do it this way is there's no "before loading a solution" event, meaning lots of listeners get the IsEnabled setting change too late (after docs are loaded)
            if (relativePath == null) return null;

            var repo = new Repository(Repository.Discover(fullFilePath));

            if (repo.Ignore.IsPathIgnored(relativePath)) return null;

            var repoHeadTree = repo.Head.Tip.Tree;
            var remoteMasterTree = repo.Head.TrackedBranch.Tip.Tree;

            var change = repo.Diff.Compare<Patch>(new[] { fullFilePath }).SingleOrDefault();

            if(change == null)
            {
                return null;
            }

            return ParsePatch(change.Patch);
        }

        private (int[] LineAdditions, int[] LineDeletions)? ParsePatch(string patch)
        {
            var lineAdditions = new List<int>();
            var lineDeletions = new List<int>();
            int? currentLine = null;

            foreach(var line in patch.Split('\n'))
            {
                if (line.StartsWith("@@"))
                {
                    currentLine = int.Parse(line.Substring(4, line.IndexOf(',') - 4));
                }
                else if(line.StartsWith(" "))
                {
                    currentLine++;
                }
                else if(line.StartsWith("+") && currentLine != null)
                {
                    lineAdditions.Add(currentLine.Value);
                }
                else if (line.StartsWith("-") && currentLine != null)
                {
                    lineDeletions.Add(currentLine.Value);
                }
            }

            return (lineAdditions.ToArray(), lineDeletions.ToArray());
        }
    }
}