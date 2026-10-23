# OOPS! PLANET — Blender -> Unity export (repeatable pipeline)
# Usage: open a source .blend in ArtSource/Blender, then run this in Blender's Text Editor
# (or: blender -b <file>.blend --python export_to_unity.py).
# Exports to Assets/OopsPlanet/Models:
#   - objects named BLOB_*  -> one combined BLOB_Greybox.fbx (hierarchy kept, pivots at feet)
#   - objects named PROP_* / ABS_* (absorbable) / PZ_* (puzzle) -> one FBX per prop, exported at the world origin
# Conventions: 1 Blender unit = 1 m, Z-up, character faces -Y, prop origin = bottom centre.
import bpy, os

OUT = os.path.normpath(os.path.join(os.path.dirname(bpy.data.filepath), "..", "..", "Assets", "OopsPlanet", "Models"))
FBX = dict(use_selection=True, object_types={'MESH'}, apply_unit_scale=True,
           apply_scale_options='FBX_SCALE_UNITS', axis_forward='-Z', axis_up='Y',
           bake_space_transform=True, mesh_smooth_type='FACE', add_leaf_bones=False, bake_anim=False)

def select_only(objs):
    for o in bpy.context.scene.objects: o.select_set(False)
    for o in objs: o.select_set(True)

os.makedirs(OUT, exist_ok=True)
meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
blob = [o for o in meshes if o.name.startswith("BLOB_")]
if blob:
    select_only(blob)
    bpy.ops.export_scene.fbx(filepath=os.path.join(OUT, "BLOB_Greybox.fbx"), **FBX)
for o in [o for o in meshes if o.name.startswith(("PROP_", "ABS_", "PZ_"))]:
    loc = o.location.copy(); o.location = (0, 0, 0); select_only([o])
    bpy.ops.export_scene.fbx(filepath=os.path.join(OUT, o.name + ".fbx"), **FBX)
    o.location = loc
select_only([])
print("Exported to", OUT)
