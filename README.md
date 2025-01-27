A proof of concept project to enable hand tracking for COM3D2.5

THIS PROJECT IS A WORK IN PROGRESS AND IS NOT YET FUNCTIONAL.

COM3D2.5 updated the game engine to a fairly recent unity version, which allows various XR libraries to be used, but there are some complications to get it to work:

- Adding additional libraries that the game didnt ship with doesnt allow deserialization of the additional assetbundles using those libraries. Atleast not fully. They can be loaded but any custom types that are used by the assetbundle wont deserialize properly. See https://discussions.unity.com/t/loading-assemblies-with-serializable-classes-at-runtime-results-in-null-deserializations/728950/31 and https://issuetracker.unity3d.com/issues/assetbundle-is-not-loaded-correctly-when-they-reference-a-script-in-custom-dll-which-contains-system-dot-serializable-in-the-build
- It seems it's not enough to patch the Assembly-CSharp.dll to add the additional XR libraries for them to be initialized together. We need to manually call the initialization functions (in particular, registering the input subsystems) for some of the functionality to work properly. We need to look for a way to hook into how the game loads the base libraries and maybe hook into that so that additional libraries that are needed are properly initialized.
- ~~Burst-compiled code needs to be handled properly and loaded manually. This part is where we are right now.~~
- It's probably technically possible to not depend on the XR Hands libraries... but we'll need to reimplement everything from scratch, which is a lot of work (not to mention we would still need native-compiled code to replace the optimized burst-code (which I'm guessing is for calculating bone/joint positions, rotations, and gesture detection).

The goal of this project is to find out how to get hand tracking working first and foremost. We'll consider this project working once we have Unity's hand tracking visualizer working in the game.

We'll probably rewrite and create a new project for integration of the hand tracking in the game to replace various functions that only controllers could provide (maybe via gestures or something).

~~Where we are right now: XR Hands uses burst compiled code. We need to somehow generate that code and see if we can load that dynamically. See https://docs.unity3d.com/Packages/com.unity.burst@1.8/manual/modding-support.html~~ We're good on burst loading now. We should cleanup the project to make sure anyone can build it from source, and write install instructions.
