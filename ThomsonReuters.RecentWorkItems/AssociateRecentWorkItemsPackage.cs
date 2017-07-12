using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace ThomsonReuters.ActiveWorkItems
{
 [PackageRegistration(UseManagedResourcesOnly = true)]
 [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [Guid(GuidList.guidOsirisAssociateRecentWorkItemPkgString)]
    public sealed class AssociateRecentWorkItemsPackage : Package
    {
    }
}
