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

        // last time the filter was updated
        float lastUpdateTime;

        enum State
        {
            Initial,
            Waiting,
            PressDown,
            Pressed,
            PressUp,
        }

        State state = State.Initial;

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
                state = State.Waiting;
                return;
            }

            if (action.ReadValue<float>() > 0.5f)
            {
                if (state == State.Waiting)
                {
                    if (Time.time - lastPressedTime > threshold)
                    {
                        state = State.PressDown;
                    }

                    return;
                }

                if (state == State.PressDown)
                {
                    state = State.Pressed;
                    return;
                }

                if (state == State.Pressed) return;
            }


            if (action.WasReleasedThisFrame() && (state == State.Pressed || state == State.PressDown))
            {
                state = State.PressUp;
                return;
            }

            state = State.Initial;
        }

        public bool IsPressed {
            get
            {
                CheckFrame();
                return state == State.Pressed || state == State.PressDown;
            }
        }

        public bool IsPressDown
        {
            get
            {
                CheckFrame();
                return state == State.PressDown;
            }
        }

        public bool IsPressUp
        {
            get
            {
                CheckFrame();
                return state == State.PressUp;
            }
        }
    }
}
