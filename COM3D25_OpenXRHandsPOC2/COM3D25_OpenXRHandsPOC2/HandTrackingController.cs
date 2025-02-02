using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnuVRControl;
using UnityEngine;
using UnityEngine.InputSystem;

namespace COM3D25_OpenXRHandsPOC2
{
    public class HandTrackingController : HandControllerBase
    {
        public HandTrackingController(InputDevice inputDevice) : base(inputDevice)
        {
            Logger.LogInfo($"HandTrackingController created: {inputDevice.name}");
        }

        public override GameMain.VRDeviceType DeviceType => GameMain.VRDeviceType.RIFT;


        private string deviceHandedNessString => this.IsRightHand ? "Right" : "Left";

        private string deviceBindingPathString => $"<XRHandDevice>{{{this.deviceHandedNessString}Hand}}";

        private string metaHandAimPathString => $"<MetaAimHand>{{{this.deviceHandedNessString}Hand}}";
        private string handInteractionPathString => $"<HandInteraction>{{{this.deviceHandedNessString}Hand}}";


        //public override string defaultPositionBindingString => this.deviceBindingPathString + "/pinchPosition";
        //public override string defaultPositionBindingString => this.deviceBindingPathString + "/devicePosition";
        public override string defaultPositionBindingString => this.metaHandAimPathString + "/devicePosition";
        //public override string defaultRotationBindingString => this.deviceBindingPathString + "/deviceRotation";
        public override string defaultRotationBindingString => this.metaHandAimPathString + "/deviceRotation";

        OneEuroFilterVector3 OneEuroFilterVector3 = new OneEuroFilterVector3(Vector3.zero, 0.1f, 0.02f);

        public override Vector3 LocalPosition
        {
            get
            {
                var position = base.LocalPosition;
                return OneEuroFilterVector3.Filter(position, Time.deltaTime);
            }
        }

        public override Quaternion LocalRotation
        {
            get
            {
                var rotation = base.LocalRotation;
                // rotation points forward. rotate along this forward axis by 90 degrees counter clockwise
                return rotation * Quaternion.Euler(0, 0, 90);
                
                
            }
        }


        protected InputAction gripAction;
        protected InputAction GripAction
        {
            get
            {
                if (this.gripAction == null)
                {
                    // <XRHandDevice>{LeftHand}/{GraspFirm}
                    // <HandInteraction>{LeftHand}/graspValue

                    //this.gripAction = new InputAction(null, InputActionType.Value, this.deviceBindingPathString + "/{GraspFirm}", null, null, null);
                    this.gripAction = new InputAction(null, InputActionType.Value, this.handInteractionPathString + "/graspValue", null, null, null);
                }

                return gripAction;
            }
        }

        protected InputAction buttonActionPrimary;
        protected InputAction ButtonActionPrimary
        {
            get
            {
                if (this.buttonActionPrimary == null)
                {
                    // <MetaAimHand>{LeftHand}/indexPressed
                    this.buttonActionPrimary = new InputAction(null, InputActionType.Value, this.metaHandAimPathString + "/indexPressed", null, null, null);
                }
                return this.buttonActionPrimary;
            }
        }

        public override List<UnityEngine.InputSystem.InputAction> GetAllActionList()
        {
            return new List<UnityEngine.InputSystem.InputAction>()
            {
                this.GripAction, this.ButtonActionPrimary,
            };
        }

        public override Vector2 GetAxis()
        {
            return Vector2.zero;
        }

        public override float GetTriggerRate()
        {
            return 0;
        }

        public override void Haptic(float force, float time)
        {
            return;
        }

        protected InputAction[] GetButtonAction(AVRControllerButtons.BTN button)
        {
            InputAction[] result = new InputAction[] { };

            switch (button)
            {
                case AVRControllerButtons.BTN.VIRTUAL_L_CLICK:
                    result = new InputAction[] { this.ButtonActionPrimary };
                    break;

                case AVRControllerButtons.BTN.GRIP:
                    result = new InputAction[] { this.GripAction };
                    break;
            }

            return result;
        }

        public override bool IsPressDown(AVRControllerButtons.BTN button)
        {
            foreach (InputAction inputAction in this.GetButtonAction(button))
            {

                if (0.5f <= inputAction.ReadValue<float>())
                {
                    //Logger.LogInfo($"IsPressDown: {button}");
                    return true;
                }
            }
            return false;
        }

        public override bool IsPressed(AVRControllerButtons.BTN button)
        {
            foreach (InputAction inputAction in this.GetButtonAction(button))
            {

                if (inputAction.WasPressedThisFrame())
                {
                    Logger.LogInfo($"IsPressed: {button}");
                    return true;
                }
                //Logger.LogInfo($"Not IsPressed: {inputAction}");
            }
            return false;
        }

        public override bool IsPressUp(AVRControllerButtons.BTN button)
        {
            foreach (InputAction inputAction in this.GetButtonAction(button))
            {
                if (inputAction.WasReleasedThisFrame())
                {
                    Logger.LogInfo($"IsPressUp: {button}");
                    return true;
                }
                //Logger.LogInfo($"Not IsPressUp: {inputAction}");
            }
            return false;
        }

        public override bool IsTouch(AVRControllerButtons.TOUCH touchButton)
        {
            return false;
        }

        public override bool IsTouchDown(AVRControllerButtons.TOUCH touchButton)
        {
            return false;
        }

        public override bool IsTouchUp(AVRControllerButtons.TOUCH touchButton)
        {
            return false;
        }
    }
}
