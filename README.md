# Pre-Alpha Notes #

This Pre-Alpha SDK is intended to get us started in the discussion of what features everyone wants, and how they want to use the head set. It does not include the actual libraries required to run the headset, which are still being developed.

To use this project in Unity, simply copy the project directory into your Assets folder inside your Unity project.

In the FOVEInterface.cs file, there is an IFOVEState reference `_f_state` which at runtime is set to an instance of the FOVEState_NoHMD class. The IFOVEState interface is intended to represent all of the functionality required to interface directly between the C++ SDK and an object in Unity -- specifically, it gets eye gaze, head orientation, and head position information. The NoHMD version is what we use internally when building out new demos to rapidly prototype and check features before putting an actual headset on. The "live" version that interfaces with the C++ SDK has been omitted from this pre-alpha release for the sake of simplicity in getting it out.

FOVEInterface also includes a number of static methods which are intended as helper methods for asking if, for example, the user's gaze collides with a given object. This is not yet a comprehensive set of methods, but rather the set that we've needed as we build out our own demos internally.

## Mouse Emulation ##

The NoHMD implementation uses the mouse to simulate wearing an actual headset. The mouse cursor is projected into the scene to generate eye rays, and right clicking will rotate your virtual head. If you have alternative inputs you'd like to use, feel free to overwrite ours here, or to create and specify your own.

## Barrel Shader ##

We have included a simple postprocess barrel shader with our SDK. The HMD will ship with a compositor solution very similar to current headsets on the market, so you won't need this shader in the end. However we found it useful to be able to test this while we get our compositor up and running, so we hacked together a simple barrel distortion effect. Feel free to use it or not. And if you see it and cringe and have to make it better, feel free to do so with our thanks. ;)

We should note that, because of the barrel shader, the mouse cursor doesn't map one-to-one with where the gaze appears to be in the final image. This is simply because we aren't running the mouse position through an algorithm to counteract the barrel distortion. On a calibrated headset, the barrel distortion is automatically handled and we do not see this kind of inaccuracy.

It is recommended that this shader be used in Linear color space, not gamma. Go to `Edit > Project Settings > Player` and then under `Other Settings > Rendering > Color Space*` select "Linear". If you prefer to use gamma color space, you will probably want to modify the "Gamma Mod" value on the Barrel Distortion script of the FOVE Eye Camera prefab. Setting it to 1 will match gamma colorspace, setting it to 1.8 will generally match linear color space.

## Pre-Alpha Reminder ##

Please note, this SDK is pre-alpha, which means that the features listed here aren't final yet. In fact, we hope that community members will take this initial Unity SDK and let us know what features they want added as they develop things with it. We encourage everyone to fork the project, make any additions you want, and if you have something you'd like to see in the final SDK, submit a pull request. We also plan to be as active as we can (considering the time difference and other tasks we're working on over here) in adding methods, fixes, etc...

The changes made from this SDK will be seen in future releases, including the C++ SDK, Unreal plugin, and everywhere else we show up.