using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using TfsCommitSearcher;

public class AzureDevopsCommitLister
{
    //Collection array if more than one collection is using
    static readonly string[] collectionNames = new string[]{
        "Collection1","Collection2" };

    //Date Constraints for git project commits
    static DateTime gitFromDate = new DateTime(2021, 02, 01);
    static DateTime gitToDate = new DateTime(2022, 01, 01);

    //Date Constraints for tfvc commits
    static string tfvcFromDate = "2021.02.01";
    static string tfvcToDate = "2022.01.01";

    //Personel Access token with full access privileges
    const string pat = ""; 

    static async Task Main(string[] args)
    {
        foreach (var coll in collectionNames)
        {
            await MainGit(coll, gitFromDate, gitToDate);
            await MainTfvc(coll, tfvcFromDate, tfvcToDate);
        }
    }

    static async Task MainGit(string collectionName, DateTime fromDate, DateTime toDate)
    {
        var creds = new VssBasicCredential(
            userName: string.Empty,
            password: pat
        );

        var connection = new VssConnection(new Uri("https://tfs.tarim.gov.tr/tfs/" + collectionName), creds);

        var gitClient = connection.GetClient<GitHttpClient>();

        var gitRepos = await gitClient.GetRepositoriesAsync();

        var records = new List<DevOpsChangelog>();

        foreach (var gitRep in gitRepos)
        {
            var pushes = await gitClient.GetPushesAsync(gitRep.Id,
                searchCriteria: new GitPushSearchCriteria()
                {
                    FromDate = fromDate,
                    ToDate = toDate
                });


            foreach (var push in pushes)
            {
                var commits = await gitClient.GetPushCommitsAsync(gitRep.Id, push.PushId);

                var comments = new StringBuilder();
                foreach (var commit in commits)
                {
                    comments.Append(commit.Comment);
                    comments.Append(" ||| ");
                }

                records.Add(new DevOpsChangelog
                {
                    CollectionName = collectionName,
                    ProjectName = gitRep.ProjectReference.Name,
                    ChangesetId = push.PushId,
                    Git = true,
                    Date = push.Date,
                    UniqueName = push.PushedBy.UniqueName,
                    DisplayName = push.PushedBy.DisplayName,
                    Comments = comments.ToString()
                });
            }
        }


        SaveRecords(records);

    }
    static async Task MainTfvc(string collectionName, string fromDate, string toDate)
    {
        var creds = new VssBasicCredential(
            userName: string.Empty,
            password: pat
        );

        var connection = new VssConnection(new Uri("https://tfs.tarim.gov.tr/tfs/" + collectionName), creds);

        var tfvcClient = connection.GetClient<TfvcHttpClient>();

        var changesets = await tfvcClient.GetChangesetsAsync(searchCriteria: new TfvcChangesetSearchCriteria()
        {
            FromDate = fromDate,
            ToDate = toDate
        }, top: 100000);

        var records = new List<DevOpsChangelog>();

        foreach (var changeset in changesets)
        {
            HashSet<string> projectNames = new HashSet<string>();

            var changes = await tfvcClient.GetChangesetChangesAsync(changeset.ChangesetId);
            foreach (var change in changes)
            {
                var path = change.Item.Path;
                var indexOfSecondSlash = path.IndexOf('/', 2);

                string pname;
                if (indexOfSecondSlash == -1)
                    pname = path.Substring(2);
                else
                    pname = path.Substring(2, indexOfSecondSlash - 2);

                projectNames.Add(pname);
            }

            var projectName = string.Join("|||", projectNames);

            records.Add(new DevOpsChangelog
            {
                CollectionName = collectionName,
                ProjectName = projectName,
                Git = false,
                ChangesetId = changeset.ChangesetId,
                Date = changeset.CreatedDate,
                UniqueName = changeset.CheckedInBy.UniqueName,
                DisplayName = changeset.CheckedInBy.DisplayName,
                Comments = changeset.Comment
            });
        }

        SaveRecords(records);
    }

    static SqlDbContext ctx = new SqlDbContext();

    private static void SaveRecords(List<DevOpsChangelog> records)
    {        
        foreach (var item in records)
        {
            var existng = ctx.TfsYazilimChangelogs.Find(item.ProjectName, item.Git, item.ChangesetId);
            if (existng == null)
            {
                ctx.TfsYazilimChangelogs.Add(item);
            }
            else
            {
                ctx.Entry(existng).CurrentValues.SetValues(item);
                
            }            
        }
        ctx.SaveChanges();
    }
}