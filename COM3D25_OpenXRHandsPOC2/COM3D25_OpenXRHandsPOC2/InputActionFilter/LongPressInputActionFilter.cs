using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.InputSystem;

namespace COM3D25_OpenXRHandsPOC2
{
    internal class LongPressInputActionFilter : IInputActionFilter
    {
        InputAction action;
        
        // time in seconds to wait to consider a press as a long press
        float threshold;
        
        // the last time the action was actually pressed
        float lastPressedTime = 0;

        // if true, the action was previously pressed and we are waiting for the threshold to pass before considering it a long press
        bool waitLongPress = false;

        // if true, trigger the long press event this frame
        bool triggerLongPressed = false;
        bool triggerLongPressDown = false;
        bool triggerLongPressUp = false;

        // last time the filter was updated
        float lastUpdateTime;

        public LongPressInputActionFilter(InputAction action, float threshold=0.2f)
        {
            this.action = action;
            this.threshold = threshold;
        }

        void CheckFrame()
        {
            // don't run if we already ran this frame
            if (Time.time == lastUpdateTime)
            {
                return;
            }

            // pressDown > pressed > pressUp

            lastUpdateTime = Time.time;

            if (action.WasPressedThisFrame())
            {
                lastPressedTime = Time.time;
                waitLongPress = true;
                triggerLongPressDown = false;
                triggerLongPressed = false;
                triggerLongPressUp = false;
                return;
            }

            if (action.ReadValue<float>() > 0.5f)
            {
                if (waitLongPress)
                {
                    if (Time.time - lastPressedTime > threshold)
                    {
                        waitLongPress = false;
                        triggerLongPressDown = true;
                        triggerLongPressed = true;
                    }

                    return;
                }

                if (triggerLongPressDown)
                {
                    triggerLongPressDown = false;
                }

                if (triggerLongPressed) return;
            }


            if (action.WasReleasedThisFrame() && triggerLongPressed)
            {
                triggerLongPressDown = false;
                triggerLongPressed = false;
                triggerLongPressUp = true;
                return;
            }

            Reset();
        }

        void Reset()
        {
            waitLongPress = false;
            triggerLongPressed = false;
            triggerLongPressDown = false;
            triggerLongPressUp = false;
        }

        public bool IsPressed {
            get
            {
                CheckFrame();
                return triggerLongPressed;
            }
        }

        public bool IsPressDown
        {
            get
            {
                CheckFrame();
                return triggerLongPressDown;
            }
        }

        public bool IsPressUp
        {
            get
            {
                CheckFrame();
                return triggerLongPressUp;
            }
        }
    }
}
