using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.InputSystem;

namespace COM3D25_OpenXRHandsPOC2
{
    internal class ShortPressInputActionFilter : IInputActionFilter
    {
        InputAction action;
        float threshold;
        float lastPressedTime = 0;
        bool waitShortpress = false;
        bool triggerShortPressDown = false;
        bool triggerShortPressUp = false;
        float lastUpdateTime = 0;

        public ShortPressInputActionFilter(InputAction action, float threshold=0.2f)
        {
            this.action = action;
            this.threshold = threshold;
        }

        void CheckFrame()
        {
            if (lastUpdateTime == Time.time) return;
            lastUpdateTime = Time.time;

            if (action.WasPressedThisFrame())
            {
                lastPressedTime = Time.time;
                waitShortpress = true;
                triggerShortPressDown = false;
                triggerShortPressUp = false;
                return;
            }

            // pressDown > pressed > pressUp

            if (action.WasReleasedThisFrame() && waitShortpress)
            {
                waitShortpress = false;

                if (Time.time - lastPressedTime < threshold)
                {
                    triggerShortPressDown = true;
                }

                return;
            }

            if (triggerShortPressDown)
            {
                triggerShortPressDown = false;
                triggerShortPressUp = true;
                return;
            }

            if (triggerShortPressUp)
            {
                triggerShortPressUp = false;
                return;
            }
        }
       

        public bool IsPressed {
            get
            {
                CheckFrame();
                return triggerShortPressDown;
            }
        }

        public bool IsPressDown {
            get
            {
                CheckFrame();
                return triggerShortPressDown;
            }
        }

        public bool IsPressUp {
            get
            {
                CheckFrame();
                return triggerShortPressUp;
            }
        }
    }
}
