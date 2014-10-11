
# Rcdtcs Pre-Release 2

![Alt text](/RcdtcsScreenshot.jpg?raw=true "Rcdtcs Unity Demo")

## Origin of the software and disclaimer :

Rcdtcs is an experimental C# port of the Recast & Detour libraries, originally written by Mikko Mononen. The original author is not associated with this port.

- The original software can be found here https://github.com/memononen/recastnavigation 

Rcdtcs is based on this commit (21st feb 2014) https://github.com/memononen/recastnavigation/commit/740a7ba51600a3c87ce5667ae276a38284a1ce75

## Purpose

The aim of Rcdtcs is to be a C# port that is as close as possible from the original C++ source code, so that :

- Programmers with previous experience of the original software should recognize the code
- Code relying on the original software should be easy to port
- Reflecting changes from the original software should be easy

The port aims to remain portable, so the Recast and Detour part of Rcdtcs does not rely on any external codebase. It is independent from Unity code and uses the original types (vertices as float array, instead of Vector3 arrays etc).

Code in the RcdtcsUnityDemo\Scripts folders provides some example code and helpers to integrate Rcdtcs in a Unity project. It is fully optional code.

Although Unity includes its own version of the Recast/Detour library, this implementation allows full access to code and specific features such as runtime baking or custom serialization. The downside is that the C# code is slower than the C++. Development benchmark show navmesh baking to be 2 to 3 times slower in Unity stand-alone/web compared to C++ build, although specific cases may vary in terms of performance (and there is room for optimization).

The Unity code has no reliance on Pro features and there is no Editor-only code so it's fully embeddable and Unity Free compatible.

**Please note that for this pre-release, the Unity code is only compatible with the public beta version of Unity 4.6 b20**. This is due to reliance on Unity's new GUI system, which should soon be part of the main version of the Editor.

## Porting scope

- Recast and Detour folders from the original software are fully ported (but not fully tested, see below). 
- DebugUtils and RecastDemo exist in some form in the Unity sample application. 
- DetourCrowd and DetourTileCache have not been ported yet.

The sample application shows what features are actually tested and officially supported. 

Untested features have a low chance of working out of the box, these include:
- 64 bit version for very large navmeshes, you'll see typedefs related to it but it's probably incomplete
- Sliced path finding
- offmesh links

Additional supported features not shown in the demo include:
- Custom binary serialization / deserialization through dtRawTileData in Rcdtcs, and an example use case within SystemHelper.ComputeSystem(byte[] tileRawData, int start) in the Unity implementation. 
- Building a navmesh from multiple meshes (see SystemHelper.AddMesh) is also available in the Unity application code but not shown in the demo.

## Running the demo

You need the free or pro version of **Unity 4.6 Beta 20**.

Open the repository's folder as a Unity Project, then open the following scene :

rcdtcs\Assets\RcdtcsUnityDemo\RcdtcsUnityDemo.unity

Note that performance will be higher in stand alone client or web browser builds than in the editor.

## License

Rcdtcs is licensed under the **MIT license**, see License.txt for more information.

## Porting notes :

- Pointer arithmetics : the biggest change is related to pointers to array elements used to process subsets of large datasets. Managed C# memory does not have to be contiguous so these things are not possible. In those cases Rcdtcs uses an additional int array index, with the original array. This applies to function parameters and function bodies. The naming convention applied is to use the name of the C++ pointer and to append Start or Index after it. Ex : float[] verts, float* v becomes float[] verts, int vStart.
- structs : all structs from the original software have been replaced by classes, except very few exceptions when mass allocation of small objects was causing major performance impact.
- function library : C# functions need to belong to an object, so Recast and Detour are two static classes defined with the keyword partial in each file that needs to add to them.
- C# port functions : a limited number of low level functions were added, prefixed with rccs and dtcs
- Memory is managed through regular C# garbage collection
- ex .h and .cpp files have been merged into single cs files. Extensive documentation found in the comments has been merged as well.
- typedefs are a per file concept in C#, so you'll see the same typedefs over and over at the beginning of various files
- unsigned char type is replaced by byte
- variables named in, ref, out, params have been renamed because they are reserved language keywords. All other variables, function names should have their original name so that you can compare them to the C++ version when investigating a bug for instance
- c# is less permissive with some bitwise operations, such as unsigned types on the right of << or >> so some changes have happened there
- any field whose bit size is specified in the C++ code was upped to the closest highest parent. Ex: uint : 2 becomes byte, uint : 30 becomes uint, etc
- Some variables declared in blocks tend to bleed to the upper scope in C# so some vars had to be renamed (ex: { float v; } float v; <-- illegal.
- Some ported code doesn't make much sense in C# such as out of memory management. C# is more likely to throw an exception than return null after an unsuccessful new, but appropriate behaviour was not implemented. 


