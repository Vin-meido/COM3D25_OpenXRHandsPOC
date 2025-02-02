using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace COM3D25_OpenXRHandsPOC2
{
    internal interface IInputActionFilter
    {
        bool IsPressed { get; }
        bool IsPressDown { get; }
        bool IsPressUp { get; }
    }
}
