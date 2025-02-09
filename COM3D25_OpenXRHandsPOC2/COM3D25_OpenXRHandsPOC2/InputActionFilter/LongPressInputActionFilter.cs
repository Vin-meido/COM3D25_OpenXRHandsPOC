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

        // last frame the filter was updated
        int lastUpdateFrame;

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
            if (lastUpdateFrame == Time.frameCount) return;
            lastUpdateFrame = Time.frameCount;

            // pressDown > pressed > pressUp

            if (action.WasPressedThisFrame())
            {
                // regardless of last state, if the action was pressed this frame, we reset the timer

                lastPressedTime = Time.time;
                state = State.Waiting;
                return;
            }
            
            if (state == State.Waiting)
            {
                // we are waiting for time to pass to consider the press as a long press

                if (action.ReadValue<float>() > 0.5f)
                {
                    if (Time.time - lastPressedTime > threshold)
                    {
                        state = State.PressDown;
                    }

                    return;
                }
                else
                {
                    // no longer being pressed, reset to initial
                    state = State.Initial;
                    return;
                }
            }

            if (state == State.PressDown)
            {
                if (action.WasReleasedThisFrame())
                {
                    state = State.PressUp;
                    return;
                }

                if (action.ReadValue<float>() > 0.5f)
                {
                    state = State.Pressed;
                    return;
                }
                
                state = State.Initial;
                return;
            }

            if (state == State.Pressed)
            {
                if (action.WasReleasedThisFrame())
                {
                    state = State.PressUp;
                    return;
                }

                if (action.ReadValue<float>() > 0.5f)
                {
                    return;
                }

                state = State.Initial;
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
