using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Esatto.Win32.Com
{
    [Flags]
#if ESATTO_WIN32
    public
#else
    internal
#endif
        enum REGCLS : uint
    {
        SINGLEUSE = 0,
        MULTIPLEUSE = 1,
        MULTI_SEPARATE = 2,
        SUSPENDED = 4,
        SURROGATE = 8,
    }
}
