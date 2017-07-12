using Microsoft.TeamFoundation.WorkItemTracking.Client;
using System.Windows;
using System.Windows.Media;

namespace ThomsonReuters.ActiveWorkItems
{
    public class ActiveWorkItem 
    {
        public ActiveWorkItem()
        {
        }

        public ActiveWorkItem(WorkItem workitem)
        {
            this.Id = workitem.Id;
            this.State = workitem.State;
            this.Title = workitem.Title;
            this.WorkItemType = workitem.Type.Name;
            this.AssignedTo = workitem["Assigned To"] as string;
            this.IterationPath = workitem["Iteration Path"] as string;

        }

        public int Id { get; set; }
        public string State { get; set; }
        public string Title { get; set; }
        public string WorkItemType { get; set; }
        public string AssignedTo { get; set; }
        public string IterationPath { get; set; }
        public object WorkItemTypeColor
        {
            get
            {
                BrushConverter bc = new BrushConverter();
                switch (WorkItemType)
                {
                    case "Task":
                        {
                            Brush brush = (Brush)bc.ConvertFrom("#F2CB1D");
                            brush.Freeze();
                            return brush;
                        }
                    case "User Story":
                        {
                            Brush brush = (Brush)bc.ConvertFrom("#009CCC");
                            brush.Freeze();
                            return brush;
                        }
                    case "Bug":
                        {
                            Brush brush = (Brush)bc.ConvertFrom("#CC293D");
                            brush.Freeze();
                            return brush;
                        }
                    case "Code Review Response":
                    case "Code Review Request":
                        {
                            Brush brush = (Brush)bc.ConvertFrom("#FF9D00");
                            brush.Freeze();
                            return brush;
                        }
                    case "Feature":
                        {
                            Brush brush = (Brush)bc.ConvertFrom("#773B93");
                            brush.Freeze();
                            return brush;
                        }
                    default:
                        {
                            Brush brush = (Brush)bc.ConvertFrom("#AAAAAA");
                            brush.Freeze();
                            return brush;
                        }
                }
            }
        }
    }
}
