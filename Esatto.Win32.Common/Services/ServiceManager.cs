using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.ComponentModel;

namespace Esatto.Win32.Services
{
#if ESATTO_WIN32
    public
#else
    internal
#endif
        class ServiceManager : IDisposable
    {
        SafeServiceHandle hServiceManager;

        public ServiceManager()
        {
            hServiceManager = NativeMethods.OpenSCManager(null, null, NativeMethods.SCM_ACCESS.SC_MANAGER_ALL_ACCESS);
            if (hServiceManager.IsInvalid)
            {
                throw new Win32Exception();
            }
        }

        public void Dispose()
        {
            hServiceManager.Dispose();
        }

        public void CreateService(InteropService svc)
        {
            if (svc == null)
            {
                throw new ArgumentNullException(nameof(svc), "Contract assertion not met: svc != null");
            }
            if (String.IsNullOrEmpty(svc.ServiceName))
            {
                throw new ArgumentException("Contract assertion not met: !String.IsNullOrEmpty(svc.ServiceName)", nameof(svc));
            }
            if (String.IsNullOrEmpty(svc.Path))
            {
                throw new ArgumentException("Contract assertion not met: !String.IsNullOrEmpty(svc.Path)", nameof(svc));
            }
            if (!(svc.ServiceHandle == null))
            {
                throw new ArgumentException("service is already persisted", nameof(svc));
            }

            //create basic service
            var hService = NativeMethods.CreateService(
                this.hServiceManager,
                svc.ServiceName,
                svc.DisplayName,
                NativeMethods.SERVICE_ACCESS.SERVICE_ALL_ACCESS,
                NativeMethods.SERVICE_TYPE.SERVICE_WIN32_OWN_PROCESS,
                svc.StartType,
                NativeMethods.SERVICE_ERROR.SERVICE_ERROR_NORMAL,
                svc.Path,
                svc.LoadOrderGroup,
                IntPtr.Zero, //out tagid, //load order tag id
                NativeMethods.GetDoubleNullString(svc.Dependencies),
                svc.UserName,
                svc.Password);

            if (hService.IsInvalid)
            {
                throw new Win32Exception();
            }

            //update description
            if (!string.IsNullOrWhiteSpace(svc.Description))
            {
                var sd = new NativeMethods.SERVICE_DESCRIPTION()
                {
                    lpDescription = svc.Description
                };

                var pServiceDescription = Marshal.AllocHGlobal(Marshal.SizeOf(sd));
                Marshal.StructureToPtr(sd, pServiceDescription, false);

                if (!NativeMethods.ChangeServiceConfig2(hService, NativeMethods.SERVICE_CONFIG.SERVICE_CONFIG_DESCRIPTION, pServiceDescription))
                {
                    throw new Win32Exception();
                }

                Marshal.FreeHGlobal(pServiceDescription);
            }

            svc.ServiceHandle = hService;
        }

        public InteropService OpenService(string serviceName)
        {
            if (serviceName == null) throw new ArgumentNullException("serviceName");

            var hService = NativeMethods.OpenService(hServiceManager, serviceName, NativeMethods.SERVICE_ACCESS.SERVICE_ALL_ACCESS);
            if (hService.IsInvalid)
            {
                throw new Win32Exception();
            }

            return new InteropService() { ServiceHandle = hService, ServiceName = serviceName };
        }
    }
}
