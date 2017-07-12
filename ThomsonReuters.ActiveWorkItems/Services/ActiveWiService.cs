using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.VersionControl.Controls.Extensibility;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using Osiris.TeamExplorer.Extensions.Common;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using ThomsonReuters.ActiveWorkItems;

namespace ThomsonReuters.Services.ActiveWorkItems
{
    [Guid("9D99F268-576B-4EB5-B0E4-62599F3B332E")]    
    public interface SActiveWiService
    {
    }

    [Guid("B03D8F1A-D657-4D08-A4B2-3D7354AB7153")]    
    [ComVisible(true)]
    public interface IActiveWiService
    {
        ObservableCollection<ActiveWorkItem> GetActiveWorkItems(string searchTerm, IPendingChangesExt pc,  ITeamFoundationContext context);
    }
        
    public class ActiveWiService : TeamExplorerBase, IActiveWiService, SActiveWiService
    {
        public ObservableCollection<ActiveWorkItem> GetActiveWorkItems(string searchTerm, IPendingChangesExt pc, ITeamFoundationContext context)
        {            
            var currentlyAssociatedWorkItems = pc.WorkItems;

            var workItems = new ObservableCollection<ActiveWorkItem>();

            // Make the server call asynchronously to avoid blocking the UI                            
            if (context != null && context.HasCollection && context.HasTeamProject)
            {
                var vcs = context.TeamProjectCollection.GetService<VersionControlServer>();
                if (vcs != null)
                {

                    WorkItemStore workItemStore = (WorkItemStore)context.TeamProjectCollection.GetService(typeof(WorkItemStore));

                    var project = workItemStore.Projects[context.TeamProjectName];

                    var query = FindQueryItem("Active Work Items Query", project.QueryHierarchy);

                    string queryString = "Select * "
                           + "From WorkItems "
                           + "Where [Work Item Type] IN ('User Story','Bug','Task') "
                           + "AND [Assigned to] = @Me "
                           + "AND [State] = 'Active' "
                           + "AND [Area Path] Under '" + context.TeamProjectName + "' "
                           + "Order By [Changed Date] Desc ";


                    if (query != null)
                        queryString = query.QueryText.ToString().Replace("@project", "'" + context.TeamProjectName + "'");

                    // Run a query.
                    WorkItemCollection queryResults = workItemStore.Query(queryString);
                    foreach (WorkItem wi in queryResults.Cast<WorkItem>()
                            .Where(i => currentlyAssociatedWorkItems
                                .All(a => a.WorkItem.Id != i.Id) && (
                                    string.IsNullOrEmpty(searchTerm) ||
                                    i.Type.Name.ToLower().Contains(searchTerm.ToLower()) ||
                                    i.Id.ToString().ToLower().Contains(searchTerm.ToLower()) ||
                                    i.Title.ToLower().Contains(searchTerm.ToLower()) ||
                                    i.Description.ToLower().Contains(searchTerm.ToLower()))))
                    {
                        workItems.Add(new ActiveWorkItem(wi));
                    }
                }
            }

            return workItems;
        }

        ///<summary>
        /// Recursively find the QueryDefinition based on the requested queryName.
        ///<para>Note that if multiple queries match the requested queryName
        /// only the first will be used</para>
        ///</summary>
        ///<param name=”queryName”>Name of Stored Query. Note if multiple exist
        /// the first found will be used</param>
        ///<param name=”currentNode”>Pointer to the current node in the recursive search</param>
        ///<returns>QueryDefinition</returns>
        private static QueryDefinition FindQueryItem(string queryName, QueryItem currentNode)

        {
            // Attempt to cast to a QueryDefinition
            QueryDefinition queryDefinition = currentNode as QueryDefinition;

            // Check if we’ve found a match
            if (queryDefinition != null && queryDefinition.Name == queryName)

                return queryDefinition;

            // Attempt to cast the current node to a QueryFolder
            QueryFolder queryFolder = currentNode as QueryFolder;

            // All further checks are for child nodes so if this is not an
            // instance of QueryFolder then no further processing is required.
            if (queryFolder == null)

                return null;

            // Loop through all the child query item
            foreach (QueryItem qi in queryFolder)
            {

                // Recursively call FindQueryItem
                QueryDefinition ret = FindQueryItem(queryName, qi);

                // If a match is found no further checks are required
                if (ret != null)

                    return ret;

            }

            return null;

        }
    }
}
