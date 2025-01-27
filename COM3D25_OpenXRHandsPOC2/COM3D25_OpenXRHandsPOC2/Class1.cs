using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using BepInEx;
using UnityEngine;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.Hands.OpenXR;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Hands.ProviderImplementation;
using UnityEngine.XR.Hands.Samples.VisualizerSample;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using UnityEngine.XR.Hands;
using UnityEngine.SubsystemsImplementation;
using Unity.Burst;


namespace COM3D25_OpenXRHandsPOC2
{
    [BepInPlugin("COM3D25_OpenXRHandsPOC2", "OpenXR Hands POC2", "0.0.1")]
    public class COM3D25_OpenXRHandsPOC2 : BaseUnityPlugin
    {
        GameObject LeftHandPrefab;
        GameObject RightHandPrefab;
        bool ready = false;

        public void Awake()
        {
            Logger.LogInfo("OpenXR Hands POC2 initialize");
            Logger.LogInfo($"OpenXRSettings is: {OpenXRSettings.Instance}");

            try
            {
                // add HandTracking OpenXRFeature
                // OpenXRSettings.features is internal so use reflection to add it
                var featuresField = OpenXRSettings.Instance.GetType().GetField("features", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var features = featuresField.GetValue(OpenXRSettings.Instance) as OpenXRFeature[];

                //var feature = new HandTracking();
                var feature = ScriptableObject.CreateInstance<HandTracking>();

                // use reflection to set the following internal fields on feature
                /*
                 *   nameUi: Hand Tracking Subsystem
                version: 0.0.1
                featureIdInternal: com.unity.openxr.feature.input.handtracking
                openxrExtensionStrings: XR_EXT_hand_tracking
                company: Unity
                priority: -100
                */

                // first create a dict of the fields and values to set
                var fieldValues = new Dictionary<string, object>
                {
                    { "nameUi", "Hand Tracking Subsystem" },
                    { "version", "0.0.1" },
                    { "featureIdInternal", "com.unity.openxr.feature.input.handtracking" },
                    { "openxrExtensionStrings", "XR_EXT_hand_tracking" },
                    { "company", "Unity" },
                    { "priority", -100 },
                };

                // set m_enabled on feature subclass of HandTracking
                var enabledField = feature.GetType().BaseType.GetField("m_enabled", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                enabledField.SetValue(feature, true);


                // set the fields on the feature
                foreach (var field in fieldValues)
                {
                    var featureField = feature.GetType().GetField(field.Key, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (featureField == null)
                    {
                        Logger.LogError($"Failed to find field {field.Key} on HandTracking feature");
                        throw new Exception($"Failed to find field {field.Key} on HandTracking feature");
                    }

                    featureField.SetValue(feature, field.Value);
                }

                // create a new OpenXRFeature array with HandTracking added
                var newFeatures = new OpenXRFeature[features.Length + 1];
                features.CopyTo(newFeatures, 0);
                newFeatures[features.Length] = feature;

                // set OpenXRSettings.features to the new array
                featuresField.SetValue(OpenXRSettings.Instance, newFeatures);

                Logger.LogInfo($"HandTracking feature added: {feature}");

                // Lets check loader status
                var xrGeneralSettings = XRGeneralSettings.Instance;
                Logger.LogInfo($"XRGeneralSettings is: {xrGeneralSettings}");
                var xrManager = xrGeneralSettings.Manager;
                Logger.LogInfo($"XRManager is: {xrManager}. init is: {xrManager.isInitializationComplete}");
                //DumpReport();

                // call private static OpenXRHandProvider.Register
                Logger.LogInfo("Registering HandTracking provider...");
                var registerMethod = typeof(OpenXRHandProvider).GetMethod("Register", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                registerMethod.Invoke(null, null);

                // load burst compiled dll
                Logger.LogInfo("Loading burst compiled dll...");
                if (!System.IO.File.Exists(BurstDllPath))
                {
                    Logger.LogError($"Burst compiled dll not found at {BurstDllPath}");
                    throw new Exception($"Burst compiled dll not found at {BurstDllPath}");
                }
                BurstRuntime.LoadAdditionalLibrary(BurstDllPath);

            }
            catch (Exception e)
            {
                Logger.LogError($"Failed to add HandTracking feature: {e}");
                throw e;
            }
        }

        String AssemblyPath => System.Reflection.Assembly.GetExecutingAssembly().Location;
        String AssetBundlePath => System.IO.Path.Combine(System.IO.Path.GetDirectoryName(AssemblyPath), "xrhands");
        String AssemblyName => System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
        String BurstDllPath => System.IO.Path.Combine(System.IO.Path.GetDirectoryName(AssemblyPath), $"{AssemblyName}_burst.dll");

        public void Start()
        {
            Logger.LogInfo("OpenXR Hands POC2 started");
            Logger.LogInfo($"GameMain is: {GameMain.Instance}");

            StartCoroutine(StartXRCoroutine());
            StartCoroutine(SetupHandVisualizerCoroutine());
        }

        IEnumerator SetupHandVisualizerCoroutine()
        {
            // load async
            var assetBundleCreateRequest = UnityEngine.AssetBundle.LoadFromFileAsync(AssetBundlePath);
            yield return assetBundleCreateRequest;

            var assetBundle = assetBundleCreateRequest.assetBundle;
            if (assetBundle == null)
            {
                Logger.LogError($"Failed to load asset bundle from {AssetBundlePath}");
                yield break;
            }

            // load the prefabs
            LeftHandPrefab = assetBundle.LoadAsset<GameObject>("Left Hand Tracking Custom");
            RightHandPrefab = assetBundle.LoadAsset<GameObject>("Right Hand Tracking Custom");

            if (LeftHandPrefab == null || RightHandPrefab == null)
            {
                throw new Exception("Failed to load Left or Right Hand Tracking Custom prefab");
            }

            // Get XR Rig Root and attach an XROrigin
            var xrRigRoot = GameObject.Find("__GameMain__/OpenXRCameraRig(Clone)");
            if (xrRigRoot == null)
            {
                Logger.LogError("XR Rig Root not found");
                yield break;
            }

            var xrOriginComponent = xrRigRoot.AddComponent<XROrigin>();

            // get XR Rig Camera and attach the hand prefabs on its parent
            var xrRigCamera = GameObject.Find("__GameMain__/OpenXRCameraRig(Clone)/TrackingSpace/CenterEyeAnchor");
            if (xrRigCamera == null)
            {
                Logger.LogError("XR Rig Camera not found");
                yield break;
            }

            // set camera for xrOrigin
            xrOriginComponent.Camera = xrRigCamera.GetComponent<Camera>();

            var leftHand = GameObject.Instantiate(LeftHandPrefab, xrRigCamera.transform.parent);
            var rightHand = GameObject.Instantiate(RightHandPrefab, xrRigCamera.transform.parent);

            // create HandVisualizer game object at parent of xrRigCamera
            var handVisualizer = new GameObject("HandVisualizer");
            handVisualizer.transform.parent = xrRigCamera.transform.parent;
            handVisualizer.transform.localPosition = Vector3.zero;
            var handVisualizerComponent = handVisualizer.AddComponent<HandVisualizer>();
            handVisualizerComponent.m_LeftHandMesh = leftHand;
            handVisualizerComponent.m_RightHandMesh = rightHand;
            handVisualizerComponent.m_DebugDrawPrefab = assetBundle.LoadAsset<GameObject>("Joint");
            handVisualizerComponent.m_VelocityPrefab = assetBundle.LoadAsset<GameObject>("VelocityPrefab");
            handVisualizerComponent.debugDrawJoints = true;
            handVisualizerComponent.velocityType = HandVisualizer.VelocityType.None;
            handVisualizerComponent.drawMeshes = true;
        }

        string GetDeviceCharacteristics(InputDeviceCharacteristics characteristics)
        {
            var sb = new StringBuilder();
            foreach (var value in Enum.GetValues(typeof(InputDeviceCharacteristics)))
            {
                if (characteristics.HasFlag((InputDeviceCharacteristics)value))
                {
                    sb.Append($"{value} ");
                }
            }
            return sb.ToString();
        }

        public void DumpReport()
        {
            // check openxr diagnostics
            // UnityEngine.XR.OpenXR.DiagnosticReport is internal so use reflection to get it
            // first get reference to type
            var diagnosticReportType = typeof(UnityEngine.XR.OpenXR.OpenXRLoader).Assembly.GetType("UnityEngine.XR.OpenXR.DiagnosticReport");

            // then run internal static string GenerateReport()
            var generateReportMethod = diagnosticReportType.GetMethod("GenerateReport", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var report = generateReportMethod.Invoke(null, null) as string;

            Logger.LogInfo(report);
        }

        public void DumpDevices()
        {
            var devices = new List<InputDevice>();
            InputDevices.GetDevices(devices);

            Logger.LogInfo("Searching for XR devices...");
            foreach (var device in devices)
            {
                Logger.LogInfo($"Device found: {device.name} {GetDeviceCharacteristics(device.characteristics)}");
            }

            Logger.LogInfo("Checking for hand tracking devices...");
            var xrHandSubsystem = HandTracking.subsystem;
            if (xrHandSubsystem != null)
            {
                Logger.LogInfo($"Left hand: {xrHandSubsystem.leftHand} tracked: {xrHandSubsystem.leftHand.isTracked}");
                Logger.LogInfo($"Right hand: {xrHandSubsystem.rightHand} tracked: {xrHandSubsystem.rightHand.isTracked}");
            } else
            {
                Logger.LogWarning("XRHandSubsystem not found or initialized");
            }
        }

        public IEnumerator StartXRCoroutine()
        {
            Logger.LogInfo("Starting XR manually...");
            yield return XRGeneralSettings.Instance.Manager.InitializeLoader();

            if (XRGeneralSettings.Instance.Manager.activeLoader == null)
            {
                Logger.LogWarning("XRGeneralSettings.Instance.Manager.activeLoader is null after InitializeLoader");
            }

            Logger.LogInfo("Starting XR Subsystems...");
            XRGeneralSettings.Instance.Manager.StartSubsystems();
            DumpReport();
            DumpDevices();

        }
    }

}
