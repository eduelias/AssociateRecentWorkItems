using System;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Controls;
using Microsoft.TeamFoundation.VersionControl.Controls.Extensibility;
using Microsoft.VisualStudio.TeamFoundation.WorkItemTracking;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Collections;
using System.Collections.ObjectModel;
using Microsoft.TeamFoundation.WorkItemTracking.WpfControls;
using Microsoft.VisualStudio.Shell;
using EnvDTE;
using EnvDTE80;

namespace ThomsonReuters.RecentWorkItems
{     
    public partial class RecentWorkItemsView : UserControl, INotifyPropertyChanged
    {
        public RecentWorkItemsView()
        {
            InitializeComponent();
        }

        private string _searchTerm = "";
        public string SearchTerm
        {
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
        /// <summary>
        /// Parent section.
        /// </summary>
        public RecentWorkItemsSection ParentSection
        {
            get { return (RecentWorkItemsSection)GetValue(ParentSectionProperty); }
            set { SetValue(ParentSectionProperty, value); }
        }

        public ITeamFoundationContext Context { get; set; }

        public DocumentService DocumentService { get; set; }

        public static readonly DependencyProperty ParentSectionProperty =
            DependencyProperty.Register("ParentSection", typeof(RecentWorkItemsSection), typeof(RecentWorkItemsView));

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raise the PropertyChanged event for the specified property.
        /// </summary>
        /// <param name="propertyName">Property name</param>
        protected void RaisePropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void workItemList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            OnAssociateWorkItem(sender, e);
        }

        private void OnOpenWorkItem(object sender, RoutedEventArgs e)
        {
            var item = this.workItemList.SelectedItem;
            if (item == null)
                return;

            var propertyId = item.GetType().GetProperty("Id");
            if (propertyId == null)
                return;

            int selectedWorkItemId = (int)propertyId.GetValue(item);

            IWorkItemDocument widoc = null;
            try
            {

                widoc = DocumentService.GetWorkItem(Context.TeamProjectCollection, selectedWorkItemId, this);
                DocumentService.ShowWorkItem(widoc);
            }
            finally
            {
                if( widoc != null )
                    widoc.Release(this);
            }
        }

        private void AddWorkItem(bool associate = false)
        {            
            try
            {
                var item = this.workItemList.SelectedItem;
                if (item == null)
                    return;

                var propertyId = item.GetType().GetProperty("Id");
                if (propertyId == null)
                    return;

                int selectedWorkItemId = (int)propertyId.GetValue(item);
                
                IPendingChangesExt pendingChangesExt = ParentSection.GetService<IPendingChangesExt>();

                var workItemSection = pendingChangesExt.GetType().GetField("m_workItemsSection", BindingFlags.NonPublic | BindingFlags.Instance);

                var modelType = workItemSection.FieldType;
                var model = workItemSection.GetValue(pendingChangesExt);


                var m = modelType.GetMethod("AddWorkItemById", BindingFlags.NonPublic | BindingFlags.Instance);                

                m.Invoke(model, new object[] { selectedWorkItemId });

                if (associate)
                    AssociateIt(selectedWorkItemId, model);

                var workItem = ParentSection.RecentWorkItems.FirstOrDefault(wi => wi.Id == selectedWorkItemId);
                if (workItem != null)
                {
                    ParentSection.RecentWorkItems.Remove(workItem);
                }
            }
            catch (Exception ex)
            {
                ParentSection.ShowNotification(ex.ToString(), NotificationType.Error);                
            }            
        }

        public async void AssociateIt(int selectedWorkItemId, object mm)
        {
            await ParentSection.RefreshAsync();

            IPendingChangesExt pendingChangesExt = ParentSection.GetService<IPendingChangesExt>();

            var model = pendingChangesExt.GetType().GetField("m_workItemsSection", BindingFlags.NonPublic | BindingFlags.Instance);

            var modelType = model.FieldType;
            var workItemSection = model.GetValue(pendingChangesExt);

            var selectedWil = workItemSection.GetType().GetProperty("SelectedWorkItems").GetValue(workItemSection) as ObservableCollection<WorkItemValueProvider>;
            var availablWil = workItemSection.GetType().GetProperty("WorkItemsListProvider").GetValue(workItemSection) as WorkItemsListProvider;

            try
            {
                while (!availablWil.WorkItems.Where(x => x.Id == selectedWorkItemId).Any())
                {                    
                    await System.Threading.Tasks.Task.Delay(25);
                }

                selectedWil.Clear();
                selectedWil.Add(availablWil.WorkItems.Where(x => x.Id == selectedWorkItemId).First());

                EnvDTE80.DTE2 dte2 = Package.GetGlobalService(typeof(DTE)) as DTE2;
                dte2.ExecuteCommand("TeamFoundationContextMenus.WorkItemActionLink.TfsContextPendingChangesPageWorkItemActionLinkAssociate");
                selectedWil.Clear();
            }
            catch (Exception ex)
            {
                ParentSection.ShowNotification(ex.ToString(), NotificationType.Error);
            }
        }

        private void OnAssociateWorkItem(object sender, RoutedEventArgs e)
        {
            AddWorkItem(true);  
        }

        private void OnResolveWorkItem(object sender, RoutedEventArgs e)
        {
            AddWorkItem();
        }

        private void TermBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var caller = ((TextBox)sender);

            if (ParentSection == null)  // avoid running on initialize
                return;

            if (!string.IsNullOrEmpty(caller.Text) &&
                caller.Text.Length < 3)
                return;

            ParentSection.SearchTerm = caller.Text;

            ParentSection.RefreshAsync();
        }
    }
}
