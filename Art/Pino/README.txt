Pino / พีโน่ — Blue Jelly Slime

Files
- Pino.blend: editable character, separate studio collection, camera and lights.
- Pino.glb: character only, 3 meshes, 14,528 triangles, embedded color texture.
- Pino_Body_BaseColor.png: blue-to-mint body color map (also packed in the blend and GLB).
- Pino_Preview.png: rendered preview.
- Pino_ModelInfo.json: geometry validation report.

Character height: 0.52 m. Width: 0.644 m. Front: -Y in Blender.
Meshes: body, left eye, right eye.
Edit the character in collection "01  PINO • character".
No heart, podium or sparkle decorations. The camera, lighting and background floor are separate from the exported character.
No rig or animation is included.

Adaptive Slime extension (23 October 2026)
- Source/Pino_Morphs.blend: the same base character with ten body/eye shape keys and ability parts.
- Exports/Pino_Morphs.fbx: Unity blend shape export.
- Morphing/Unity_PinoMorphGallery.png: verified Unity render of all forms.
- Morphing/Pino_Idle_Walk.gif: Unity preview of gentle resting jelly motion and low walking hops.
- Morphing/Pino_AllForms_Idle_Walk.gif: updated Unity preview with idle and walking motion for all five forms.
- MorphPreviews/: individual Blender renders.
- Backups/BeforeMorphing/: preserved base character files.
- Unity prefab, runtime C#, editor tools, materials, scenes, test results and Thai integration guide:
  Assets/PinoCharacter/Documentation/Integration.md (from the repository root).
The base files above remain the static original; the extension provides procedural animation and morphing.

The GLB uses standard glTF clearcoat, transmission and IOR material extensions.
Jelly appearance depends on the lighting and material support of the destination renderer.

