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
        int lastUpdateFrame = 0;

        enum State
        {
            Initial,
            Waiting,
            PressDown,
            Pressed,
            PressUp,
        }

        State state = State.Initial;

        public ShortPressInputActionFilter(InputAction action, float threshold=0.2f)
        {
            this.action = action;
            this.threshold = threshold;
        }

        void CheckFrame()
        {
            if (lastUpdateFrame == Time.frameCount) return;
            lastUpdateFrame = Time.frameCount;

            if (action.WasPressedThisFrame())
            {
                lastPressedTime = Time.time;
                state = State.Waiting;
                return;
            }

            // pressDown > pressed > pressUp

            if (state == State.Waiting)
            {
                if (action.WasReleasedThisFrame())
                {
                    if (Time.time - lastPressedTime < threshold)
                    {
                        state = State.PressDown;
                        return;
                    } else
                    {
                        state = State.Initial;
                        return;
                    }
                } 

                if (action.ReadValue<float>() > 0.5f)
                {
                    // continue wating
                    return;
                }

                state = State.Initial;
                return;
            }

            if (state == State.PressDown)
            {
                state = State.PressUp;
                return;
            }

            if (state == State.PressUp)
            {
                state = State.Initial;
                return;
            }
        }

        public bool IsPressed {
            get
            {
                CheckFrame();
                return state == State.Pressed || state == State.PressDown;
            }
        }

        public bool IsPressDown {
            get
            {
                CheckFrame();
                return state == State.PressDown;
            }
        }

        public bool IsPressUp {
            get
            {
                CheckFrame();
                return state == State.PressUp;
            }
        }
    }
}
