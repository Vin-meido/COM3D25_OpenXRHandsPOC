using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class XRLeftHandDriver : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        var device = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        if (device.isValid)
        {
            Debug.Log("Left hand is valid");
        }

        Vector3 position;
        Quaternion rotation;

        if (device.TryGetFeatureValue(CommonUsages.devicePosition, out position) &&
            device.TryGetFeatureValue(CommonUsages.deviceRotation, out rotation))
        {
            transform.localPosition = position;
            transform.localRotation = rotation;
        }
    }
}
