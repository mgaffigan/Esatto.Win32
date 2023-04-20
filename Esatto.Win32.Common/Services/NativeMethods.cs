using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Esatto.Win32.Services
{
    internal static class NativeMethods
    {
        private const string advapi32 = "advapi32.dll";

        [DllImport(advapi32, SetLastError = true, CharSet = CharSet.Auto)]
        public static extern SafeServiceHandle OpenSCManager(string machineName,
            string databaseName, SCM_ACCESS dwAccess);

        [DllImport(advapi32, SetLastError = true, CharSet = CharSet.Auto)]
        public static extern SafeServiceHandle CreateService(SafeServiceHandle hSCManager,
            string lpServiceName, string lpDisplayName, SERVICE_ACCESS dwDesiredAccess,
            SERVICE_TYPE dwServiceType, ServiceStartType dwStartType, SERVICE_ERROR dwErrorControl,
            string lpBinaryPathName, string lpLoadOrderGroup, IntPtr lpdwTagId,
            char[] lpDependencies, string lpServiceStartName, string lpPassword);

        [DllImport(advapi32, SetLastError = true, CharSet = CharSet.Auto)]
        public static extern SafeServiceHandle OpenService(SafeServiceHandle hSCManager,
            string lpServiceName, SERVICE_ACCESS dwDesiredAccess);

        [DllImport(advapi32, SetLastError = true, ExactSpelling = true)]
        public static extern bool CloseServiceHandle(IntPtr hSCObject);

        [DllImport(advapi32, SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool ChangeServiceConfig2(SafeServiceHandle hService,
            SERVICE_CONFIG dwInfoLevel, IntPtr lpInfo);

        [DllImport(advapi32, SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool StartService(SafeServiceHandle hService, int dwNumServiceArgs,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] string[] lpServiceArgVectors);

        [DllImport(advapi32, SetLastError = true, ExactSpelling = true)]
        public static extern bool DeleteService(SafeServiceHandle hService);

        [DllImport(advapi32, SetLastError = true, ExactSpelling = true)]
        public static extern bool QueryServiceStatus(SafeServiceHandle hService, out SERVICE_STATUS dwServiceStatus);

        #region Structs

        [StructLayout(LayoutKind.Sequential)]
        public struct SERVICE_STATUS
        {
            public SERVICE_TYPE dwServiceType;
            public SERVICE_STATE dwCurrentState;
            public int dwControlsAccepted;
            public int dwWin32ExitCode;
            public int dwServiceSpecificExitCode;
            public int dwCheckPoint;
            public int dwWaitHint;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct SERVICE_DESCRIPTION
        {
            [MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPTStr)]
            public string lpDescription;
        }

        #endregion Structs

        #region Constants

        public enum SERVICE_CONFIG
        {
            SERVICE_CONFIG_DESCRIPTION = 1,
            SERVICE_CONFIG_FAILURE_ACTIONS = 2,
            SERVICE_CONFIG_DELAYED_AUTO_START_INFO = 3,
            SERVICE_CONFIG_FAILURE_ACTIONS_FLAG = 4,
            SERVICE_CONFIG_SERVICE_SID_INFO = 5,
            SERVICE_CONFIG_REQUIRED_PRIVILEGES_INFO = 6,
            SERVICE_CONFIG_PRESHUTDOWN_INFO = 7,
            SERVICE_CONFIG_TRIGGER_INFO = 8,
            SERVICE_CONFIG_PREFERRED_NODE = 9
        }

        [Flags]
        private enum ACCESS_MASK : uint
        {
            DELETE = 0x00010000,
            READ_CONTROL = 0x00020000,
            WRITE_DAC = 0x00040000,
            WRITE_OWNER = 0x00080000,
            SYNCHRONIZE = 0x00100000,

            STANDARD_RIGHTS_REQUIRED = 0x000f0000,

            STANDARD_RIGHTS_READ = 0x00020000,
            STANDARD_RIGHTS_WRITE = 0x00020000,
            STANDARD_RIGHTS_EXECUTE = 0x00020000,

            STANDARD_RIGHTS_ALL = 0x001f0000,

            SPECIFIC_RIGHTS_ALL = 0x0000ffff,

            ACCESS_SYSTEM_SECURITY = 0x01000000,

            MAXIMUM_ALLOWED = 0x02000000,

            GENERIC_READ = 0x80000000,
            GENERIC_WRITE = 0x40000000,
            GENERIC_EXECUTE = 0x20000000,
            GENERIC_ALL = 0x10000000,

            DESKTOP_READOBJECTS = 0x00000001,
            DESKTOP_CREATEWINDOW = 0x00000002,
            DESKTOP_CREATEMENU = 0x00000004,
            DESKTOP_HOOKCONTROL = 0x00000008,
            DESKTOP_JOURNALRECORD = 0x00000010,
            DESKTOP_JOURNALPLAYBACK = 0x00000020,
            DESKTOP_ENUMERATE = 0x00000040,
            DESKTOP_WRITEOBJECTS = 0x00000080,
            DESKTOP_SWITCHDESKTOP = 0x00000100,

            WINSTA_ENUMDESKTOPS = 0x00000001,
            WINSTA_READATTRIBUTES = 0x00000002,
            WINSTA_ACCESSCLIPBOARD = 0x00000004,
            WINSTA_CREATEDESKTOP = 0x00000008,
            WINSTA_WRITEATTRIBUTES = 0x00000010,
            WINSTA_ACCESSGLOBALATOMS = 0x00000020,
            WINSTA_EXITWINDOWS = 0x00000040,
            WINSTA_ENUMERATE = 0x00000100,
            WINSTA_READSCREEN = 0x00000200,

            WINSTA_ALL_ACCESS = 0x0000037f
        }

        public enum SERVICE_STATE
        {
            SERVICE_STOPPED = 0x00000001,
            SERVICE_START_PENDING = 0x00000002,
            SERVICE_STOP_PENDING = 0x00000003,
            SERVICE_RUNNING = 0x00000004,
            SERVICE_CONTINUE_PENDING = 0x00000005,
            SERVICE_PAUSE_PENDING = 0x00000006,
            SERVICE_PAUSED = 0x00000007
        }

        /// <summary>
        /// Access to the service. Before granting the requested access, the
        /// system checks the access token of the calling process.
        /// </summary>
        [Flags]
        public enum SERVICE_ACCESS : uint
        {
            /// <summary>
            /// Required to call the QueryServiceConfig and
            /// QueryServiceConfig2 functions to query the service configuration.
            /// </summary>
            SERVICE_QUERY_CONFIG = 0x00001,

            /// <summary>
            /// Required to call the ChangeServiceConfig or ChangeServiceConfig2 function
            /// to change the service configuration. Because this grants the caller
            /// the right to change the executable file that the system runs,
            /// it should be granted only to administrators.
            /// </summary>
            SERVICE_CHANGE_CONFIG = 0x00002,

            /// <summary>
            /// Required to call the QueryServiceStatusEx function to ask the service
            /// control manager about the status of the service.
            /// </summary>
            SERVICE_QUERY_STATUS = 0x00004,

            /// <summary>
            /// Required to call the EnumDependentServices function to enumerate all
            /// the services dependent on the service.
            /// </summary>
            SERVICE_ENUMERATE_DEPENDENTS = 0x00008,

            /// <summary>
            /// Required to call the StartService function to start the service.
            /// </summary>
            SERVICE_START = 0x00010,

            /// <summary>
            ///     Required to call the ControlService function to stop the service.
            /// </summary>
            SERVICE_STOP = 0x00020,

            /// <summary>
            /// Required to call the ControlService function to pause or continue
            /// the service.
            /// </summary>
            SERVICE_PAUSE_CONTINUE = 0x00040,

            /// <summary>
            /// Required to call the EnumDependentServices function to enumerate all
            /// the services dependent on the service.
            /// </summary>
            SERVICE_INTERROGATE = 0x00080,

            /// <summary>
            /// Required to call the ControlService function to specify a user-defined
            /// control code.
            /// </summary>
            SERVICE_USER_DEFINED_CONTROL = 0x00100,

            /// <summary>
            /// Includes STANDARD_RIGHTS_REQUIRED in addition to all access rights in this table.
            /// </summary>
            SERVICE_ALL_ACCESS = (ACCESS_MASK.STANDARD_RIGHTS_REQUIRED |
                SERVICE_QUERY_CONFIG |
                SERVICE_CHANGE_CONFIG |
                SERVICE_QUERY_STATUS |
                SERVICE_ENUMERATE_DEPENDENTS |
                SERVICE_START |
                SERVICE_STOP |
                SERVICE_PAUSE_CONTINUE |
                SERVICE_INTERROGATE |
                SERVICE_USER_DEFINED_CONTROL),

            GENERIC_READ = ACCESS_MASK.STANDARD_RIGHTS_READ |
                SERVICE_QUERY_CONFIG |
                SERVICE_QUERY_STATUS |
                SERVICE_INTERROGATE |
                SERVICE_ENUMERATE_DEPENDENTS,

            GENERIC_WRITE = ACCESS_MASK.STANDARD_RIGHTS_WRITE |
                SERVICE_CHANGE_CONFIG,

            GENERIC_EXECUTE = ACCESS_MASK.STANDARD_RIGHTS_EXECUTE |
                SERVICE_START |
                SERVICE_STOP |
                SERVICE_PAUSE_CONTINUE |
                SERVICE_USER_DEFINED_CONTROL,

            /// <summary>
            /// Required to call the QueryServiceObjectSecurity or
            /// SetServiceObjectSecurity function to access the SACL. The proper
            /// way to obtain this access is to enable the SE_SECURITY_NAME
            /// privilege in the caller's current access token, open the handle
            /// for ACCESS_SYSTEM_SECURITY access, and then disable the privilege.
            /// </summary>
            ACCESS_SYSTEM_SECURITY = ACCESS_MASK.ACCESS_SYSTEM_SECURITY,

            /// <summary>
            /// Required to call the DeleteService function to delete the service.
            /// </summary>
            DELETE = ACCESS_MASK.DELETE,

            /// <summary>
            /// Required to call the QueryServiceObjectSecurity function to query
            /// the security descriptor of the service object.
            /// </summary>
            READ_CONTROL = ACCESS_MASK.READ_CONTROL,

            /// <summary>
            /// Required to call the SetServiceObjectSecurity function to modify
            /// the Dacl member of the service object's security descriptor.
            /// </summary>
            WRITE_DAC = ACCESS_MASK.WRITE_DAC,

            /// <summary>
            /// Required to call the SetServiceObjectSecurity function to modify
            /// the Owner and Group members of the service object's security
            /// descriptor.
            /// </summary>
            WRITE_OWNER = ACCESS_MASK.WRITE_OWNER,
        }

        /// <summary>
        /// Service types.
        /// </summary>
        [Flags]
        public enum SERVICE_TYPE
        {
            /// <summary>
            /// Driver service.
            /// </summary>
            SERVICE_KERNEL_DRIVER = 0x00000001,

            /// <summary>
            /// File system driver service.
            /// </summary>
            SERVICE_FILE_SYSTEM_DRIVER = 0x00000002,

            /// <summary>
            /// Service that runs in its own process.
            /// </summary>
            SERVICE_WIN32_OWN_PROCESS = 0x00000010,

            /// <summary>
            /// Service that shares a process with one or more other services.
            /// </summary>
            SERVICE_WIN32_SHARE_PROCESS = 0x00000020,

            /// <summary>
            /// The service can interact with the desktop.
            /// </summary>
            SERVICE_INTERACTIVE_PROCESS = 0x00000100,
        }
        
        /// <summary>
        /// Severity of the error, and action taken, if this service fails
        /// to start.
        /// </summary>
        public enum SERVICE_ERROR
        {
            /// <summary>
            /// The startup program ignores the error and continues the startup
            /// operation.
            /// </summary>
            SERVICE_ERROR_IGNORE = 0x00000000,

            /// <summary>
            /// The startup program logs the error in the event log but continues
            /// the startup operation.
            /// </summary>
            SERVICE_ERROR_NORMAL = 0x00000001,

            /// <summary>
            /// The startup program logs the error in the event log. If the
            /// last-known-good configuration is being started, the startup
            /// operation continues. Otherwise, the system is restarted with
            /// the last-known-good configuration.
            /// </summary>
            SERVICE_ERROR_SEVERE = 0x00000002,

            /// <summary>
            /// The startup program logs the error in the event log, if possible.
            /// If the last-known-good configuration is being started, the startup
            /// operation fails. Otherwise, the system is restarted with the
            /// last-known good configuration.
            /// </summary>
            SERVICE_ERROR_CRITICAL = 0x00000003,
        }

        [Flags]
        public enum SCM_ACCESS : uint
        {
            /// <summary>
            /// Required to connect to the service control manager.
            /// </summary>
            SC_MANAGER_CONNECT = 0x00001,

            /// <summary>
            /// Required to call the CreateService function to create a service
            /// object and add it to the database.
            /// </summary>
            SC_MANAGER_CREATE_SERVICE = 0x00002,

            /// <summary>
            /// Required to call the EnumServicesStatusEx function to list the
            /// services that are in the database.
            /// </summary>
            SC_MANAGER_ENUMERATE_SERVICE = 0x00004,

            /// <summary>
            /// Required to call the LockServiceDatabase function to acquire a
            /// lock on the database.
            /// </summary>
            SC_MANAGER_LOCK = 0x00008,

            /// <summary>
            /// Required to call the QueryServiceLockStatus function to retrieve
            /// the lock status information for the database.
            /// </summary>
            SC_MANAGER_QUERY_LOCK_STATUS = 0x00010,

            /// <summary>
            /// Required to call the NotifyBootConfigStatus function.
            /// </summary>
            SC_MANAGER_MODIFY_BOOT_CONFIG = 0x00020,

            /// <summary>
            /// Includes STANDARD_RIGHTS_REQUIRED, in addition to all access
            /// rights in this table.
            /// </summary>
            SC_MANAGER_ALL_ACCESS = ACCESS_MASK.STANDARD_RIGHTS_REQUIRED |
                SC_MANAGER_CONNECT |
                SC_MANAGER_CREATE_SERVICE |
                SC_MANAGER_ENUMERATE_SERVICE |
                SC_MANAGER_LOCK |
                SC_MANAGER_QUERY_LOCK_STATUS |
                SC_MANAGER_MODIFY_BOOT_CONFIG,

            GENERIC_READ = ACCESS_MASK.STANDARD_RIGHTS_READ |
                SC_MANAGER_ENUMERATE_SERVICE |
                SC_MANAGER_QUERY_LOCK_STATUS,

            GENERIC_WRITE = ACCESS_MASK.STANDARD_RIGHTS_WRITE |
                SC_MANAGER_CREATE_SERVICE |
                SC_MANAGER_MODIFY_BOOT_CONFIG,

            GENERIC_EXECUTE = ACCESS_MASK.STANDARD_RIGHTS_EXECUTE |
                SC_MANAGER_CONNECT | SC_MANAGER_LOCK,

            GENERIC_ALL = SC_MANAGER_ALL_ACCESS,
        }

        #endregion Constants

        public static char[] GetDoubleNullString(string[] input)
        {
            if (input == null || input.Length == 0
                || input.Any(i => i == null || i.Length == 0))
            {
                // this should be \0, rather than \0\0
                return new char[] { '\0', '\0' };
            }

            // useful reference: http://blogs.msdn.com/b/oldnewthing/archive/2009/10/08/9904646.aspx
            char[] data = new char[
                input.Sum(s => s.Length) /* character data */
                + input.Length /* string nulls */
                + 1 /* end of list */];

            int cpos = 0;
            for (int i = 0; i < input.Length; i++)
            {
                input[i].CopyTo(0, data, cpos, input[i].Length);
                cpos += input[i].Length;
                data[cpos] = '\0';
                cpos++;
            }

            // add the zero length string at the end
            data[cpos] = '\0';
            cpos++;

            if (!(cpos == data.Length))
            {
                throw new ArgumentException("Contract assertion not met: cpos == data.Length", "value");
            }
            return data; //final null will be inserted by marshaler
        }
    }
}