using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using System.ComponentModel.Design;
using ThomsonReuters.Services.ActiveWorkItems;
using Microsoft.VisualStudio.OLE.Interop;

namespace ThomsonReuters.ActiveWorkItems
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.5", IconResourceID = 400)]
    [Guid(GuidList.guidOsirisAssociateRecentWorkItemPkgString)]
    [ProvideService(typeof(SActiveWiService))]
    public sealed class AssociateRecentWorkItemsPackage : Package, IServiceContainer, IServiceProvider
    {
        public AssociateRecentWorkItemsPackage()
        {
            IServiceContainer serviceContainer = this as IServiceContainer;
            ServiceCreatorCallback callback =
               new ServiceCreatorCallback(CreateService);
            serviceContainer.AddService(typeof(SActiveWiService), callback, true);            
        }

        private object CreateService(IServiceContainer container, System.Type serviceType)
        {
            if (typeof(SActiveWiService) == serviceType)
                return new ActiveWiService();

            return null;
        }

        protected override void Initialize()
        {
            base.Initialize();

            var service = GetService(typeof(SActiveWiService));
        }
    }
}
