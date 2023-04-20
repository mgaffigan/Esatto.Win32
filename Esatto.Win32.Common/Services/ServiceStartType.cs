using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Esatto.Win32.Services
{
    /// <summary>
    /// Service start options
    /// </summary>
#if ESATTO_WIN32
    public
#else
    internal
#endif
        enum ServiceStartType
    {
        /// <summary>
        /// A device driver started by the system loader. This value is valid
        /// only for driver services.
        /// </summary>
        Boot = 0x00000000,

        /// <summary>
        /// A device driver started by the IoInitSystem function. This value
        /// is valid only for driver services.
        /// </summary>
        System = 0x00000001,

        /// <summary>
        /// A service started automatically by the service control manager
        /// during system startup. For more information, see Automatically
        /// Starting Services.
        /// </summary>
        Automatic = 0x00000002,

        /// <summary>
        /// A service started by the service control manager when a process
        /// calls the StartService function. For more information, see
        /// Starting Services on Demand.
        /// </summary>
        Manually = 0x00000003,

        /// <summary>
        /// A service that cannot be started. Attempts to start the service
        /// result in the error code ERROR_SERVICE_DISABLED.
        /// </summary>
        Disabled = 0x00000004,
    }
}
