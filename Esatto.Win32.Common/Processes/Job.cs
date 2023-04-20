using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Esatto.Win32.Processes
{
    using static NativeMethods;

    public sealed class Job : IDisposable
    {
        private SafeJobHandle handle;
        private bool disposed;

        public Job()
        {
            handle = CreateJobObject(IntPtr.Zero, null);
            if (handle.IsInvalid)
            {
                throw new Win32Exception();
            }

            var extendedInfo = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION
            {
                BasicLimitInformation = new JOBOBJECT_BASIC_LIMIT_INFORMATION
                {
                    LimitFlags = JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE
                }
            };

            int cbJobObjectInfo = Marshal.SizeOf(typeof(JOBOBJECT_EXTENDED_LIMIT_INFORMATION));
            IntPtr lpJobObjectInfo = Marshal.AllocHGlobal(cbJobObjectInfo);
            try
            {
                Marshal.StructureToPtr(extendedInfo, lpJobObjectInfo, false);

                if (!SetInformationJobObject(handle,
                    JobObjectInfoType.ExtendedLimitInformation,
                    ref extendedInfo, cbJobObjectInfo))
                {
                    throw new Win32Exception();
                }
            }
            finally
            {
                Marshal.FreeHGlobal(lpJobObjectInfo);
            }
        }

        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;

            this.handle.Dispose();
        }

        private void AddProcess(IntPtr processHandle)
        {
            if (!AssignProcessToJobObject(handle, processHandle))
            {
                throw new Win32Exception();
            }
        }

        public void AddProcess(Process process)
        {
            if (process == null)
            {
                throw new ArgumentNullException("process");
            }

            AddProcess(process.Handle);
        }
    }
}
