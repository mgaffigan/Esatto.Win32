using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.ComponentModel;
using Microsoft.Win32.SafeHandles;

namespace Esatto.Win32.CommonControls.PnP
{
    static class NativeMethods
    {
        [DllImport("setupapi.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr SetupDiGetClassDevs(ref Guid ClassGuid, IntPtr Enumerator, IntPtr hwndParent, DiGetClassFlags Flags);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiDestroyDeviceInfoList(IntPtr DeviceInfoSet);

        [DllImport("setupapi.dll", SetLastError = true)]
        static extern bool SetupDiEnumDeviceInfo(IntPtr DeviceInfoSet, int MemberIndex, ref SP_DEVINFO_DATA DeviceInfoData);

        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern Boolean SetupDiGetDeviceInterfaceDetail(
           IntPtr DeviceInfoSet,
           ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData,
           ref SP_DEVICE_INTERFACE_DETAIL_DATA DeviceInterfaceDetailData,
           UInt32 DeviceInterfaceDetailDataSize,
           out UInt32 requiredSize,
           ref SP_DEVINFO_DATA deviceInfoData
        );

        /// <summary>
        /// The SetupDiGetDeviceRegistryProperty function retrieves the specified device property.
        /// This handle is typically returned by the SetupDiGetClassDevs or SetupDiGetClassDevsEx function.
        /// </summary>
        /// <param Name="DeviceInfoSet">Handle to the device information set that contains the interface and its underlying device.</param>
        /// <param Name="DeviceInfoData">Pointer to an SP_DEVINFO_DATA structure that defines the device instance.</param>
        /// <param Name="Property">Device property to be retrieved. SEE MSDN</param>
        /// <param Name="PropertyRegDataType">Pointer to a variable that receives the registry data Type. This parameter can be NULL.</param>
        /// <param Name="PropertyBuffer">Pointer to a buffer that receives the requested device property.</param>
        /// <param Name="PropertyBufferSize">Size of the buffer, in bytes.</param>
        /// <param Name="RequiredSize">Pointer to a variable that receives the required buffer size, in bytes. This parameter can be NULL.</param>
        /// <returns>If the function succeeds, the return value is nonzero.</returns>
        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SetupDiGetDeviceRegistryProperty(
            IntPtr DeviceInfoSet,
            ref SP_DEVINFO_DATA DeviceInfoData,
            SetupDiGetDeviceRegistryPropertyEnum Property,
            out int PropertyRegDataType,
            IntPtr PropertyBuffer,
            int PropertyBufferSize,
            out int RequiredSize
        );

        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern SafeRegistryHandle SetupDiOpenDevRegKey(
            IntPtr DeviceInfoSet,
            ref SP_DEVINFO_DATA DeviceInfoData,
            uint Scope,
            uint HwProfile,
            uint KeyType,
            uint samDesired
        );

        private const uint KEY_QUERY_VALUE = 1;
        private const uint DICS_FLAG_GLOBAL = 0x00000001;
        private const uint DIREG_DEV = 0x00000001;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SP_DEVICE_INTERFACE_DETAIL_DATA
        {
            public UInt32 cbSize;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string DevicePath;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SP_DEVINFO_DATA
        {
            public int cbSize;
            public Guid ClassGuid;
            public uint DevInst;
            public IntPtr Reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SP_DEVICE_INTERFACE_DATA
        {
            public int cbSize;
            public Guid InterfaceClassGuid;
            public uint Flags;
            public IntPtr Reserved;
        }

        [Flags]
        private enum DiGetClassFlags : uint
        {
            // only valid with DIGCF_DEVICEINTERFACE
            DIGCF_DEFAULT = 0x00000001,
            DIGCF_PRESENT = 0x00000002,
            DIGCF_ALLCLASSES = 0x00000004,
            DIGCF_PROFILE = 0x00000008,
            DIGCF_DEVICEINTERFACE = 0x00000010,
        }

        enum SetupDiGetDeviceRegistryPropertyEnum : uint
        {
            SPDRP_DEVICEDESC = 0x00000000, // DeviceDesc (R/W)
            SPDRP_HARDWAREID = 0x00000001, // HardwareID (R/W)
            SPDRP_COMPATIBLEIDS = 0x00000002, // CompatibleIDs (R/W)
            SPDRP_UNUSED0 = 0x00000003, // unused
            SPDRP_SERVICE = 0x00000004, // Service (R/W)
            SPDRP_UNUSED1 = 0x00000005, // unused
            SPDRP_UNUSED2 = 0x00000006, // unused
            SPDRP_CLASS = 0x00000007, // Class (R--tied to ClassGUID)
            SPDRP_CLASSGUID = 0x00000008, // ClassGUID (R/W)
            SPDRP_DRIVER = 0x00000009, // Driver (R/W)
            SPDRP_CONFIGFLAGS = 0x0000000A, // ConfigFlags (R/W)
            SPDRP_MFG = 0x0000000B, // Mfg (R/W)
            SPDRP_FRIENDLYNAME = 0x0000000C, // FriendlyName (R/W)
            SPDRP_LOCATION_INFORMATION = 0x0000000D, // LocationInformation (R/W)
            SPDRP_PHYSICAL_DEVICE_OBJECT_NAME = 0x0000000E, // PhysicalDeviceObjectName (R)
            SPDRP_CAPABILITIES = 0x0000000F, // Capabilities (R)
            SPDRP_UI_NUMBER = 0x00000010, // UiNumber (R)
            SPDRP_UPPERFILTERS = 0x00000011, // UpperFilters (R/W)
            SPDRP_LOWERFILTERS = 0x00000012, // LowerFilters (R/W)
            SPDRP_BUSTYPEGUID = 0x00000013, // BusTypeGUID (R)
            SPDRP_LEGACYBUSTYPE = 0x00000014, // LegacyBusType (R)
            SPDRP_BUSNUMBER = 0x00000015, // BusNumber (R)
            SPDRP_ENUMERATOR_NAME = 0x00000016, // Enumerator Name (R)
            SPDRP_SECURITY = 0x00000017, // Security (R/W, binary form)
            SPDRP_SECURITY_SDS = 0x00000018, // Security (W, SDS form)
            SPDRP_DEVTYPE = 0x00000019, // Device Type (R/W)
            SPDRP_EXCLUSIVE = 0x0000001A, // Device is exclusive-access (R/W)
            SPDRP_CHARACTERISTICS = 0x0000001B, // Device Characteristics (R/W)
            SPDRP_ADDRESS = 0x0000001C, // Device Address (R)
            SPDRP_UI_NUMBER_DESC_FORMAT = 0X0000001D, // UiNumberDescFormat (R/W)
            SPDRP_DEVICE_POWER_DATA = 0x0000001E, // Device Power Data (R)
            SPDRP_REMOVAL_POLICY = 0x0000001F, // Removal Policy (R)
            SPDRP_REMOVAL_POLICY_HW_DEFAULT = 0x00000020, // Hardware Removal Policy (R)
            SPDRP_REMOVAL_POLICY_OVERRIDE = 0x00000021, // Removal Policy Override (RW)
            SPDRP_INSTALL_STATE = 0x00000022, // Device Install State (R)
            SPDRP_LOCATION_PATHS = 0x00000023, // Device Location Paths (R)
            SPDRP_BASE_CONTAINERID = 0x00000024  // Base ContainerID (R)
        }

        private static IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);
        private const int ERROR_NO_MORE_ITEMS = 259;
        private const int ERROR_INSUFFICIENT_BUFFER = 122;

        internal static IEnumerable<DetectedPort> EnumeratePorts()
        {
            Guid GUID_DEVCLASS_PORTS = new Guid("{4d36e978-e325-11ce-bfc1-08002be10318}");
            //IntPtr pPorts = Marshal.StringToHGlobalUni("Ports");

            IntPtr hDevInfo = SetupDiGetClassDevs(ref GUID_DEVCLASS_PORTS,
                IntPtr.Zero, IntPtr.Zero,
                DiGetClassFlags.DIGCF_PRESENT);

            if (hDevInfo == INVALID_HANDLE_VALUE)
                throw new Exception("Unable to obtain handle to ports device enumerator");

            try
            {
                // enum
                var cDevInfo = new SP_DEVINFO_DATA();
                cDevInfo.cbSize = Marshal.SizeOf(typeof(SP_DEVINFO_DATA));
                for (int devNo = 0; enumDevice(hDevInfo, devNo, ref cDevInfo); devNo++)
                {
                    // get a property
                    var hRegKey = SetupDiOpenDevRegKey(hDevInfo, ref cDevInfo, DICS_FLAG_GLOBAL, 0, DIREG_DEV, KEY_QUERY_VALUE);

                    using (var rkDev = Microsoft.Win32.RegistryKey.FromHandle(hRegKey))
                    {
                        yield return new DetectedPort()
                        {
                            DeviceName = getStrProp(hDevInfo, cDevInfo, SetupDiGetDeviceRegistryPropertyEnum.SPDRP_DEVICEDESC),
                            HardwareID = getStrProp(hDevInfo, cDevInfo, SetupDiGetDeviceRegistryPropertyEnum.SPDRP_HARDWAREID),
                            PortName = rkDev.GetValue("PortName") as string
                        };
                    }
                }
            }
            finally
            {
                SetupDiDestroyDeviceInfoList(hDevInfo);
            }
        }

        private static string getStrProp(IntPtr hDevInfo, SP_DEVINFO_DATA cDevInfo, SetupDiGetDeviceRegistryPropertyEnum p)
        {
            string result = null;
            int buffSize = 256;

            while (true)
            {
                int cbBuff = buffSize;
                IntPtr pBuff = Marshal.AllocHGlobal(cbBuff);
                try
                {
                    int unused_propType = 0;

                    if (!SetupDiGetDeviceRegistryProperty(hDevInfo, ref cDevInfo, p, out unused_propType, pBuff, cbBuff, out cbBuff))
                    {
                        var lastError = Marshal.GetLastWin32Error();
                        if (lastError != ERROR_INSUFFICIENT_BUFFER)
                        {
                            throw new Win32Exception(lastError);
                        }

                        buffSize = buffSize * 2;
                    }
                    else
                    {
                        result = Marshal.PtrToStringUni(pBuff);
                        break;
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(pBuff);
                }
            }

            return result;
        }

        private static bool enumDevice(IntPtr hDevInfo, int devNo, ref SP_DEVINFO_DATA cDevInfo)
        {
            var result = SetupDiEnumDeviceInfo(hDevInfo, devNo, ref cDevInfo);

            if (!result)
            {
                var lastError = Marshal.GetLastWin32Error();

                if (lastError != ERROR_NO_MORE_ITEMS)
                    throw new Win32Exception(lastError);
            }

            return result;
        }
    }
}
