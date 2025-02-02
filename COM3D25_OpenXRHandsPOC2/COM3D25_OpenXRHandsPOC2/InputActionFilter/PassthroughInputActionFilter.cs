using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine.InputSystem;

namespace COM3D25_OpenXRHandsPOC2
{
    internal class PassthroughInputActionFilter : IInputActionFilter
    {
        InputAction action;

        public PassthroughInputActionFilter(InputAction a)
        {
            action = a;
        }

        public bool IsPressed => action.ReadValue<float>() > 0.5f;

        public bool IsPressDown => action.WasPressedThisFrame();

        public bool IsPressUp => action.WasReleasedThisFrame();
    }
}
