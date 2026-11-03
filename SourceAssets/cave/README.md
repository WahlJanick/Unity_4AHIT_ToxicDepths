# Cave source model

`cave.blend` is the original Blender 5.2 source. Unity uses
`Assets/Assets/cave/cave.fbx`, so Blender is not required to open the game.

To update the model, export it as `cave.fbx` using the Unity editor's
`Data/Tools/Unity-BlenderToFBX.py` export settings. Keep object/mesh names and
`cave.fbx.meta` unchanged so scene and collider references remain valid.
The model material `RockBase` is remapped to the URP material `RockBase.mat`.
