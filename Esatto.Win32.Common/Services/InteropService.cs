using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Esatto.Win32.Services
{
#if ESATTO_WIN32
    public
#else
    internal
#endif
        class InteropService : IDisposable
    {
        public SafeServiceHandle ServiceHandle { get; set; }
        public string ServiceName { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string Path { get; set; }
        public string LoadOrderGroup { get; set; }
        public string[] Dependencies { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public ServiceStartType StartType { get; set; }

        public InteropService()
        {
            StartType = ServiceStartType.Automatic;
        }

        public void Dispose()
        {
            if (ServiceHandle != null)
            {
                ServiceHandle.Dispose();
                ServiceHandle = null;
            }
        }

        public void Start()
        {
            AssertOpened();

            if (!NativeMethods.StartService(this.ServiceHandle, 0, null))
            {
                throw new Win32Exception();
            }
        }

        public void Uninstall()
        {
            AssertOpened();

            // verify that the service is stopped
            if (IsStarted())
            {
                throw new InvalidOperationException("Service must be stopped");
            }

            if (!NativeMethods.DeleteService(ServiceHandle))
            {
                throw new Win32Exception();
            }
        }

        public void Stop()
        {
            AssertOpened();

            if (!IsStarted())
            {
                return;
            }

            throw new NotImplementedException();
        }

        public bool IsStarted()
        {
            AssertOpened();

            NativeMethods.SERVICE_STATUS status;
            if (!NativeMethods.QueryServiceStatus(this.ServiceHandle, out status))
            {
                throw new Win32Exception();
            }

            return status.dwCurrentState != NativeMethods.SERVICE_STATE.SERVICE_STOPPED;
        }

        private void AssertOpened()
        {
            if (ServiceHandle == null || ServiceHandle.IsInvalid)
            {
                throw new InvalidOperationException("Service has not been opened.");
            }
        }
    }
}
