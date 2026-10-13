Pilzling fuer Unity

Fuer dein URP-Projekt:
1. Pilzling_URP.unitypackage in Unity oeffnen und Import waehlen.
2. Assets/Pilzling/Pilzling.prefab in die Szene ziehen.
Das Material und die Texturen sind bereits zugewiesen; Rig ist Generic.

Der Ordner Pilzling_Unity enthaelt auch den FBX-Export und PNG-Texturen.
Den ganzen Ordner samt .meta-Dateien unter Assets kopieren, damit die
Materialzuweisungen und Import-Einstellungen erhalten bleiben.

Inhalt:
- Pilzling.fbx: Figur mit 23 Deformationsknochen und einem Root-Knochen.
- Pilzling.prefab: Figur mit zugewiesenem URP-Material.
- Pilzling_URP.mat: Universal Render Pipeline/Lit.
- Textures/Pilzling_BaseColor.png: Farbtextur, sRGB.
- Textures/Pilzling_Normal.png: Normal Map.
- Textures/Pilzling_MetallicSmoothness.png: Metallic in RGB,
  Smoothness in Alpha; fuer Unity, sRGB deaktiviert.
- Textures/Pilzling_MetallicRoughness.png: Originale Metallic/Roughness-
  Textur als Referenz; im Unity-Material nicht benoetigt.

Alle Texturen haben 2048 x 2048 Pixel. Modellhoehe etwa 1,90 Meter.
In der Quelldatei sind keine Animationsclips gespeichert.

Quelle: pilzling-pickaxe-manual.blend
Unity-Smoothness-Dokumentation:
https://docs.unity3d.com/Manual/StandardShaderMaterialParameterSmoothness.html
