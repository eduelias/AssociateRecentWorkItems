using Microsoft.TeamFoundation.Controls;
using Microsoft.TeamFoundation.VersionControl.Controls.Extensibility;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TeamFoundation.WorkItemTracking;
using Osiris.TeamExplorer.Extensions.Common;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using ThomsonReuters.Services.ActiveWorkItems;

namespace ThomsonReuters.ActiveWorkItems
{
    /// <summary>
    /// Selected file info section.
    /// </summary>
    [TeamExplorerSection(SectionId, TeamExplorerPageIds.PendingChanges, 35)]
    public class ActiveWorkItemsSection : TeamExplorerBaseSection, INotifyPropertyChanged
    {
        public const string SectionId = "FA84FE92-6285-4F67-A11B-E27B5D1A2E77";
        private const string SectionTitle = "Active Work Items";

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

        private ObservableCollection<ActiveWorkItem> activeWorkItems = new ObservableCollection<ActiveWorkItem>();

        /// <summary>
        /// Constructor.
        /// </summary>
        public ActiveWorkItemsSection()
            : base()
        {            
            this.Title = SectionTitle;
            this.IsExpanded = true;
            this.IsBusy = false;
            this.SectionContent = new ActiveWorkItemsView();
            this.View.ParentSection = this;
        }

        /// <summary>
        /// Get the view.
        /// </summary>
        protected ActiveWorkItemsView View
        {
            get { return this.SectionContent as ActiveWorkItemsView; }
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
        public async System.Threading.Tasks.Task RefreshAsync()
        {
            try
            {                
                if (this.IsBusy)
                    return;

                // Set our busy flag and clear the previous data
                this.IsBusy = true;
                this.RecentWorkItems.Clear();

                Microsoft.TeamFoundation.Controls.ITeamExplorer teamExplorer;

                teamExplorer = GetService < Microsoft.TeamFoundation.Controls.ITeamExplorer>()
                                   as Microsoft.TeamFoundation.Controls.ITeamExplorer;

                if (teamExplorer.CurrentPage.GetId() != new Guid(Microsoft.TeamFoundation.Controls.TeamExplorerPageIds.PendingChanges))
                    return;

                var pc = GetService<IPendingChangesExt>();

                if (pc == null)
                    throw new Exception("please, confirm TeamFoundation.VersionControl dll version.");
                
                var currentlyAssociatedWorkItems = pc.WorkItems;

                var workItemsService = Package.GetGlobalService(typeof(SActiveWiService)) as IActiveWiService;
                var workItems = new ObservableCollection<ActiveWorkItem>();

                if (workItemsService != null)
                    workItems = workItemsService.GetActiveWorkItems(this.SearchTerm);                

                // Now back on the UI thread, update the bound collection and section title
                this.RecentWorkItems = new ObservableCollection<ActiveWorkItem>(workItems.Take(5));
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

        public ObservableCollection<ActiveWorkItem> RecentWorkItems
        {
            get { return activeWorkItems; }
            protected set { activeWorkItems = value; RaisePropertyChanged("RecentWorkItems"); }
        }

    }


}
