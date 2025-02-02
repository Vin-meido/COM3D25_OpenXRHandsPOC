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

        public override GameMain.VRDeviceType DeviceType => GameMain.VRDeviceType.RIFT_TOUCH;


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

        public virtual string buttonActionPrimaryBindingString => this.metaHandAimPathString + "/indexPressed";
        public virtual string gripActionBindingString => this.metaHandAimPathString + "/middlePressed";
        public virtual string gripActionValueBindingString => this.metaHandAimPathString + "/pinchStrengthMiddle";

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


        protected InputAction buttonActionPrimary;
        protected InputAction ButtonActionPrimary
        {
            get
            {
                if (this.buttonActionPrimary == null)
                {
                    this.buttonActionPrimary = new InputAction(null, InputActionType.Value, buttonActionPrimaryBindingString, null, null, null);
                }
                return this.buttonActionPrimary;
            }
        }

        IInputActionFilter _primaryActionFilter;
        IInputActionFilter PrimaryActionFilter => _primaryActionFilter ?? (_primaryActionFilter = new ShortPressInputActionFilter(this.ButtonActionPrimary));

        IInputActionFilter _gripActionFilter;
        IInputActionFilter GripActionFilter => _gripActionFilter ?? (_gripActionFilter = new LongPressInputActionFilter(this.ButtonActionPrimary));
        //IInputActionFilter GripActionFilter => _gripActionFilter ?? (_gripActionFilter = new PassthroughInputActionFilter(this.ButtonActionPrimary));




        public override List<UnityEngine.InputSystem.InputAction> GetAllActionList()
        {
            return new List<UnityEngine.InputSystem.InputAction>()
            {
                this.ButtonActionPrimary,
            };
        }

        public override Vector2 GetAxis()
        {
            return Vector2.zero;
        }

        public override float GetTriggerRate()
        {
            return GripActionFilter.IsPressDown ? 1 : 0;
        }

        public override void Haptic(float force, float time)
        {
            Logger.LogInfo($"Haptic: {force} {time}");
            return;
        }

        IInputActionFilter[] GetButtonAction(AVRControllerButtons.BTN button)
        {
            IInputActionFilter[] result = new IInputActionFilter[] { };

            switch (button)
            {
                case AVRControllerButtons.BTN.VIRTUAL_L_CLICK:
                    result = new IInputActionFilter[] { this.PrimaryActionFilter };
                    break;

                case AVRControllerButtons.BTN.VIRTUAL_GRUB:
                    result = new IInputActionFilter[] { this.GripActionFilter };
                    break;

                case AVRControllerButtons.BTN.GRIP:
                    result = new IInputActionFilter[] { this.GripActionFilter };
                    break;

                case AVRControllerButtons.BTN.TRIGGER:
                    result = new IInputActionFilter[] { this.GripActionFilter };
                    break;
            }

            return result;
        }

        public override bool IsPressDown(AVRControllerButtons.BTN button)
        {
            foreach (var filter in this.GetButtonAction(button))
            {
                if (filter.IsPressDown)
                {
                    Logger.LogInfo($"PressDown: {button}");
                    return true;
                }
            }
            return false;
        }

        public override bool IsPressed(AVRControllerButtons.BTN button)
        {
            foreach (var filter in this.GetButtonAction(button))
            {

                if (filter.IsPressed)
                {
                    return true;
                }
            }
            return false;
        }

        public override bool IsPressUp(AVRControllerButtons.BTN button)
        {
            foreach (var filter in this.GetButtonAction(button))
            {
                if (filter.IsPressUp)
                {
                    Logger.LogInfo($"IsPressUp: {button}");
                    return true;
                }
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
