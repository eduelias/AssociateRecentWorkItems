using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Controls;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.VersionControl.Controls.Extensibility;
using Microsoft.VisualStudio.TeamFoundation.WorkItemTracking;
using Osiris.TeamExplorer.Extensions.Common;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using ThomsonReuters.ActiveWorkItems;
using ThomsonReuters.Services.ActiveWorkItems;

namespace ThomsonReuters.RecentWorkItems
{
    /// <summary>
    /// Selected file info section.
    /// </summary>
    [TeamExplorerSection(SectionId, TeamExplorerPageIds.PendingChanges, 35)]
    public class RecentWorkItemsSection : TeamExplorerBaseSection, INotifyPropertyChanged
    {
        public const string SectionId = "D79C3F43-7C5B-4429-824C-AA7771D19B20";
        private const string SectionTitle = "Recent Work Items";

        private string _searchTerm = "";
        public string SearchTerm {
            get
            {
                return _searchTerm;
            }
            set
            {
                _searchTerm = value;
                this.RaisePropertyChanged("SearchTerm");
            }
        }

        private ObservableCollection<RecentWorkItem> recentWorkItems = new ObservableCollection<RecentWorkItem>();

        /// <summary>
        /// Constructor.
        /// </summary>
        public RecentWorkItemsSection()
            : base()
        {            
            this.Title = SectionTitle;
            this.IsExpanded = true;
            this.IsBusy = false;
            this.SectionContent = new RecentWorkItemsView();
            this.View.ParentSection = this;
        }

        /// <summary>
        /// Get the view.
        /// </summary>
        protected RecentWorkItemsView View
        {
            get { return this.SectionContent as RecentWorkItemsView; }
        }

        /// <summary>
        /// Initialize override.
        /// </summary>
        public override void Initialize(object sender, SectionInitializeEventArgs e)
        {
            base.Initialize(sender, e);

            // Find the Pending Changes extensibility service and sign up for
            // property change notifications
            var pcExt = this.GetService<IPendingChangesExt>();
            if (pcExt != null)
            {
                pcExt.PropertyChanged += pcExt_PropertyChanged;
            }

            var ds = this.GetService<DocumentService>();
            this.View.Context = this.CurrentContext;
            this.View.DocumentService = ds;

            var aTimer = new System.Timers.Timer(10000);

            // Hook up the Elapsed event for the timer.
            //aTimer.Elapsed += new ElapsedEventHandler((object source, ElapsedEventArgs ev) => {
            //    this.View.Dispatcher.Invoke(() => this.RefreshAsync());
            //});

            // Set the Interval to 2 seconds (2000 milliseconds).
            aTimer.Interval = 5000;
            aTimer.Enabled = true;

            this.RefreshAsync();
        }

        /// <summary>
        /// Dispose override.
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();

            var pcExt = this.GetService<IPendingChangesExt>();
            if (pcExt != null)
            {
                pcExt.PropertyChanged -= pcExt_PropertyChanged;
            }
        }

        /// <summary>
        /// Pending Changes Extensibility PropertyChanged event handler.
        /// </summary>
        private void pcExt_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "WorkItems":
                    Refresh();
                    break;
            }
        }

        /// <summary>
        /// Refresh override.
        /// </summary>
        public async override void Refresh()
        {
            base.Refresh();
            await this.RefreshAsync();
        }

        /// <summary>
        /// Refresh the changeset data asynchronously.
        /// </summary>
        public async Task RefreshAsync()
        {
            try
            {
                var pc = GetService<IPendingChangesExt>();
                var currentlyAssociatedWorkItems = pc.WorkItems;

                // Set our busy flag and clear the previous data
                this.IsBusy = true;
                this.RecentWorkItems.Clear();

                var workItems = new ObservableCollection<RecentWorkItem>();

                var activeWorkItems = new ObservableCollection<ActiveWorkItem>();

                try
                {
                    var service = GetService<SActiveWiService>() as IActiveWiService;
                    if (service != null)
                        activeWorkItems = service.GetActiveWorkItems(string.Empty);
                }
                catch
                {
                }

                // Make the server call asynchronously to avoid blocking the UI
                await Task.Run(() =>
                {
                    ITeamFoundationContext context = this.CurrentContext;
                    if (context != null && context.HasCollection && context.HasTeamProject)
                    {
                        var vcs = context.TeamProjectCollection.GetService<VersionControlServer>();
                        if (vcs != null)
                        {
                            string path = "$/" + context.TeamProjectName;
                            foreach (Changeset changeset in vcs.QueryHistory(path, VersionSpec.Latest, 0, RecursionType.Full,
                                                                             vcs.AuthorizedUser, null, null, 10, false, true))
                            {
                                foreach (AssociatedWorkItemInfo wi in changeset.AssociatedWorkItems.Where(i => string.IsNullOrEmpty(this.SearchTerm) ||
                                            i.State.ToLower().Contains(this.SearchTerm.ToLower()) ||
                                            i.Id.ToString().ToLower().Contains(this.SearchTerm.ToLower()) ||
                                            i.Title.ToLower().Contains(this.SearchTerm.ToLower()) ||
                                            i.AssignedTo.ToLower().Contains(this.SearchTerm.ToLower()) ||
                                            i.WorkItemType.ToLower().Contains(this.SearchTerm.ToLower())))
                                {
                                    if (
                                        workItems.All(w => w.Id != wi.Id) && 
                                        currentlyAssociatedWorkItems.All(w => w.WorkItem.Id != wi.Id) &&
                                        activeWorkItems.All(w => w.Id != wi.Id)
                                    )
                                    {
                                        workItems.Add(new RecentWorkItem(wi));
                                    }
                                }
                            }
                        }
                    }
                });

                // Now back on the UI thread, update the bound collection and section title
                this.RecentWorkItems = new ObservableCollection<RecentWorkItem>(workItems.Take(5));
                this.Title = this.RecentWorkItems.Count > 0 ? String.Format("{0} ({1})", SectionTitle, this.RecentWorkItems.Count)
                                                       : SectionTitle;
            }
            catch (Exception ex)
            {
                ShowNotification(ex.Message, NotificationType.Error);
            }
            finally
            {
                // Always clear our busy flag when done
                this.IsBusy = false;
            }
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

        public ObservableCollection<RecentWorkItem> RecentWorkItems
        {
            get { return recentWorkItems; }
            protected set { recentWorkItems = value; RaisePropertyChanged("RecentWorkItems"); }
        }

    }


}
