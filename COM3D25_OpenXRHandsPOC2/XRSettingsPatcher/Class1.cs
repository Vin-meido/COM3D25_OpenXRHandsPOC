using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XRSettingsPatcher
{
    
    public class Class1
    {
        // Disables autoamtic initialization of XR at startup
        // so we can do late registrations of subsystems and features
        // and start XR when those are ready

        public static IEnumerable<string> TargetDLLs { get; } = new[] { "Unity.XR.Management.dll" };

        public static void Patch(AssemblyDefinition assembly)
        {
            // get XRGeneralSettings
            var xrGeneralSettings = assembly.MainModule.GetType("UnityEngine.XR.Management.XRGeneralSettings");
            if (xrGeneralSettings == null)
            {
                throw new Exception("XRGeneralSettings not found");
            }

            // edit property InitManagerOnStart to always return false
            var initManagerOnStart = xrGeneralSettings.Properties.FirstOrDefault(p => p.Name == "InitManagerOnStart");
            if (initManagerOnStart != null)
            {
                var getterMethod = initManagerOnStart.GetMethod;
                if (getterMethod != null)
                {
                    var ilProcessor = getterMethod.Body.GetILProcessor();
                    ilProcessor.Body.Instructions.Clear();
                    ilProcessor.Body.Instructions.Add(ilProcessor.Create(OpCodes.Ldc_I4_0));
                    ilProcessor.Body.Instructions.Add(ilProcessor.Create(OpCodes.Ret));

                    Console.WriteLine("Patched XRGeneralSettings.InitManagerOnStart property");
                }
            }


        }
    }

    public class AssemblyXRHandsReferencePatcher
    {
        // Fixes Kiss code handling of VR Devices bug where
        // if a VR device is not recognized, it still gets added to the device list as null
        // and causes null reference exceptions later on

        public static IEnumerable<string> TargetDLLs { get; } = new[] { "Assembly-CSharp.dll" };

        public static void Patch(AssemblyDefinition assembly)
        {
            // patch VRDeviceManager.AddDevice
            var vrDeviceManager = assembly.MainModule.GetType("EnuVRControl.VRDeviceManager");
            if (vrDeviceManager == null)
            {
                throw new Exception("VRDeviceManager not found");
            }

            var addDeviceMethod = vrDeviceManager.Methods.FirstOrDefault(m => m.Name == "AddDevice");
            if (addDeviceMethod == null)
            {
                throw new Exception("VRDeviceManager.AddDevice not found");
            }

            // look for the following instructions where CreateVRDeviceInfo is called.
            // > VRDevice vrdevice = VRDeviceManager.CreateVRDeviceInfo(device);
            // > this.vrDeviceList.Add(vrdevice);
            // just right after assigning to vrdevice, add a check for null and return
            var ilProcessor = addDeviceMethod.Body.GetILProcessor();
            var instructions = addDeviceMethod.Body.Instructions;
            var found = false;
            foreach ( var instruction in instructions ) {
                if(instruction.OpCode == OpCodes.Call && instruction.Operand is MethodReference methodReference)
                {
                    if(methodReference.Name == "CreateVRDeviceInfo")
                    {
                        var nextInstruction = instruction.Next;
                        if(nextInstruction.OpCode == OpCodes.Stloc_0)
                        {
                            var nextNextInstruction = nextInstruction.Next;
                            if(nextNextInstruction.OpCode == OpCodes.Ldarg_0)
                            {
                                ilProcessor.InsertAfter(nextInstruction, ilProcessor.Create(OpCodes.Ret));
                                ilProcessor.InsertAfter(nextInstruction, ilProcessor.Create(OpCodes.Brtrue, nextNextInstruction));
                                ilProcessor.InsertAfter(nextInstruction, ilProcessor.Create(OpCodes.Ldloc_0));

                                found = true;
                                break;
                            }
                        }
                    }
                }
            }

            if(!found) {
                throw new Exception("VRDeviceManager.AddDevice not patched");
            }
        }
    }


    //public class GameMainPatcher
    //{
    //    public static IEnumerable<string> TargetDLLs { get; } = new[] { "Assembly-CSharp.dll" };

    //    // Patches the assemblies
    //    public static void Patch(AssemblyDefinition assembly)
    //    {
    //        // get OnInitialize method from assembly
    //        var onInitializeMethod = assembly.MainModule.GetType("GameMain")?.Methods.FirstOrDefault(m => m.Name == "OnInitialize");
    //        if(onInitializeMethod == null)
    //        {
    //            Console.WriteLine("OnInitialize method not found");
    //            return;
    //        }

    //        /*
    //         * find the following instructions and replace them with noop:
    //         *
    //         * ldloc.s
    //         * call void [mscordlib]System.IO.File::Delete(string)
    //         */
            
    //        var ilProcessor = onInitializeMethod.Body.GetILProcessor();

    //        var instructions = onInitializeMethod.Body.Instructions;
    //        foreach( var instruction in instructions) {
    //            if(instruction.OpCode == OpCodes.Ldloc_S && instruction.Next.OpCode == OpCodes.Call)
    //            {
    //                var callInstruction = instruction.Next;
    //                if(callInstruction.Operand is MethodReference methodReference)
    //                {
    //                    if(methodReference.DeclaringType.FullName == "System.IO.File" && methodReference.Name == "Delete")
    //                    {
    //                        ilProcessor.Replace(instruction, ilProcessor.Create(OpCodes.Nop));
    //                        ilProcessor.Replace(callInstruction, ilProcessor.Create(OpCodes.Nop));
    //                        Console.WriteLine("Found File.Delete and patched");
    //                        break;
    //                    }
    //                }
    //            }
    //        }
    //    }
    //}
}
