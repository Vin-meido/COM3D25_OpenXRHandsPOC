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
using UnityEngine.XR.OpenXR.Features.Interactions;
using System.Reflection;


namespace COM3D25_OpenXRHandsPOC2
{
    [BepInPlugin("COM3D25_OpenXRHandsPOC2Plugin", "OpenXR Hands POC2", "0.0.1")]
    public class COM3D25_OpenXRHandsPOC2Plugin : BaseUnityPlugin
    {
        public static COM3D25_OpenXRHandsPOC2Plugin Instance { get; private set;}

        GameObject LeftHandPrefab;
        GameObject RightHandPrefab;

        bool startupComplete = false;
        
        public void Awake()
        {
            Logger.LogInfo("OpenXR Hands POC2 initialize");

            try
            {
                if (Instance != null)
                {
                    Logger.LogError("Instance already set");
                    throw new Exception("Instance already set");
                }

                Instance = this;

                Logger.LogInfo($"OpenXRSettings is: {OpenXRSettings.Instance}");
                Logger.LogInfo($"XRGeneralSettings is: {XRGeneralSettings.Instance}");
                Logger.LogInfo($"XRManager is: {XRGeneralSettings.Instance.Manager}. init is: {XRGeneralSettings.Instance.Manager.isInitializationComplete}");

                SetupHandTracking();

                Logger.LogInfo("Patching VRDeviceManager");
                VRDeviceManagerPatch.Patch();
            }
            catch (Exception e)
            {
                Logger.LogError($"Initialization failed: {e}\n{e.StackTrace}");
                throw e;
            }
        }

        void SetupHandTracking()
        {
            Logger.LogInfo("Hand tracking setup");

            Logger.LogInfo("Running subsystem registrations...");
            RunSubsystemRegistrations<HandTracking>();

            Logger.LogInfo("Loading burst compiled dll...");
            if (!System.IO.File.Exists(BurstDllPath))
            {
                Logger.LogError($"Burst compiled dll not found at {BurstDllPath}");
                throw new Exception($"Burst compiled dll not found at {BurstDllPath}");
            }

            if (!BurstRuntime.LoadAdditionalLibrary(BurstDllPath))
            {
                Logger.LogError($"Failed to load burst compiled dll from {BurstDllPath}");
                throw new Exception($"Failed to load burst compiled dll from {BurstDllPath}");
            }

            Logger.LogInfo("Setting up Hand tracking OpenXR features...");
            var handTrackingFeature = SetupFeature<HandTracking>(
                "Hand Tracking Subsystem",
                "0.0.1",
                "com.unity.openxr.feature.input.handtracking",
                "XR_EXT_hand_tracking",
                "Unity",
                -100);

            var metaHandTrackingAimFeature = SetupFeature<MetaHandTrackingAim>(
                "Meta Aim Hand Tracking",
                "0.0.1",
                "com.unity.openxr.feature.input.metahandtrackingaim",
                "XR_FB_hand_tracking_aim",
                "Unity",
                -100);

            var additionalFeatures = new OpenXRFeature[] { handTrackingFeature, metaHandTrackingAimFeature };

            // OpenXRSettings.features is internal so use reflection to get it
            var featuresField = OpenXRSettings.Instance.GetType().GetField("features", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var features = featuresField.GetValue(OpenXRSettings.Instance) as OpenXRFeature[];
            var newFeatures = features.Concat(additionalFeatures).ToArray();

            // set OpenXRSettings.features to the new array
            featuresField.SetValue(OpenXRSettings.Instance, newFeatures);

            // enable some features
            var enableFeatures = new Type[]
            {
                //typeof(HandInteractionProfile),
                //typeof(HandCommonPosesInteraction),
                //typeof(PalmPoseInteraction),
            };

            foreach (var f in newFeatures)
            {
                if (enableFeatures.Contains(f.GetType()))
                {
                    f.enabled = true;
                }
            }

            Logger.LogInfo($"OpenXRSettings.features is now:");
            foreach (var f in newFeatures)
            {
                Logger.LogInfo($"  {f} enabled: {f.enabled}");
            }

            Logger.LogInfo("Hand tracking setup complete");
        }

        void RunSubsystemRegistrations(Assembly assembly)
        {
            Logger.LogInfo($"Running subsystem registrations for {assembly}");
            // scan assembly with private static methods decorated with [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
            // then call them
            foreach (var type in assembly.GetTypes())
            {
                foreach (var method in type.GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic))
                {
                    var attributes = method.GetCustomAttributes(typeof(UnityEngine.RuntimeInitializeOnLoadMethodAttribute), true);
                    if (attributes.Length > 0)
                    {
                        var attribute = attributes[0] as UnityEngine.RuntimeInitializeOnLoadMethodAttribute;
                        if (attribute.loadType == UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)
                        {
                            Logger.LogInfo($"Running subsystem registration method {method} on type {type}");
                            method.Invoke(null, null);
                        }
                    }
                }
            }
        }

        void RunSubsystemRegistrations<T>()
        {
            RunSubsystemRegistrations(typeof(T).Assembly);
        }

        OpenXRFeature SetupFeature<T>(string nameUi, string version, string featureIdInternal, string openxrExtensionStrings, string company, int priority) where T: OpenXRFeature
        {
            var feature = ScriptableObject.CreateInstance<T>();

            var fieldValues = new Dictionary<string, object>
                {
                    { "nameUi", nameUi },
                    { "version", version },
                    { "featureIdInternal", featureIdInternal },
                    { "openxrExtensionStrings", openxrExtensionStrings },
                    { "company", company },
                    { "priority", priority },
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

            return feature;
        }


        String AssemblyPath => System.Reflection.Assembly.GetExecutingAssembly().Location;
        String AssetBundlePath => System.IO.Path.Combine(System.IO.Path.GetDirectoryName(AssemblyPath), "xrhands");
        String AssemblyName => System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
        String BurstDllPath => System.IO.Path.Combine(System.IO.Path.GetDirectoryName(AssemblyPath), $"{AssemblyName}_burst.dll");

        public void Start()
        {
            Logger.LogInfo("OpenXR Hands POC2 started");
            Logger.LogInfo($"GameMain is: {GameMain.Instance}");

            if (!GameMain.Instance.VRMode)
            {
                Logger.LogInfo("Not running in VR mode, disabling plugin");
                this.enabled = false;
                return;
            }

            if(!startupComplete)
            {
                startupComplete = true;
                Logger.LogInfo("Running startup coroutines");
                StartCoroutine(StartXRCoroutine());
                StartCoroutine(SetupHandVisualizerCoroutine());
            }
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

            assetBundle.Unload(false);
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
                Logger.LogInfo($"Left hand: {xrHandSubsystem.leftHand}");
                Logger.LogInfo($"Right hand: {xrHandSubsystem.rightHand}");
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

        internal static void LogInfo(string message)
        {
            Instance?.Logger.LogInfo(message);
        }

        internal static void LogError(string message)
        {
            Instance?.Logger.LogError(message);
        }
    }
}
