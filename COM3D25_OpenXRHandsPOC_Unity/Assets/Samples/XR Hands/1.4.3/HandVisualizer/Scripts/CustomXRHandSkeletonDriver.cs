using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.Hands;


public class CustomXRHandSkeletonDriver : XRHandSkeletonDriver, ISerializationCallbackReceiver
{

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
        }

        public void FixJoints() {
            m_JointTransformReferences = new List<JointToTransformReference>();
            FindJointsFromRoot(new List<string>());
            InitializeFromSerializedReferences();
        }

    protected override void OnEnable()
    {
        if (m_JointTransformReferences == null || m_JointTransformReferences.Count == 0)
        {
            FixJoints();
        }

        base.OnEnable();
    }

}
