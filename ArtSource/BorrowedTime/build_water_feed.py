import bpy,math
from mathutils import Vector
scene=bpy.data.scenes.new('BT_LocalWaterFeed');bpy.context.window.scene=scene
col=bpy.data.collections.new('WaterFeed');scene.collection.children.link(col)
mat=bpy.data.materials.get('CH_Blue');stone=bpy.data.materials.get('CH_Stone')
def point(v):return Vector((v[0],-v[2],v[1]))
def beam(name,a,b,width,material):
 a=point(a);b=point(b);d=b-a;bpy.ops.mesh.primitive_cylinder_add(vertices=12,radius=width,depth=d.length,location=(a+b)*.5);o=bpy.context.object;o.name=name;o.rotation_euler=d.to_track_quat('Z','Y').to_euler();o.data.materials.append(material)
 for c in list(o.users_collection):c.objects.unlink(o)
 col.objects.link(o)
points=[(0,1.1,12.5),(0,1.1,9.8),(6,1.1,9.8),(6,.6,10.5)]
for i in range(len(points)-1):beam('LocalFeed',points[i],points[i+1],.14,mat)
for p in points[1:-1]:
 bpy.ops.mesh.primitive_uv_sphere_add(segments=12,ring_count=6,radius=.19,location=point(p));o=bpy.context.object;o.name='PipeElbow';o.data.materials.append(mat)
 for c in list(o.users_collection):c.objects.unlink(o)
 col.objects.link(o)
scene['note']='Single non-emissive local feed from tank to sluice; no city-spanning utility routes.'
bpy.ops.wm.save_as_mainfile(filepath='E:/!for WORK/GPT/BorrowedTime_LocalFeed.blend')
bpy.ops.object.select_all(action='DESELECT')
for o in col.objects:o.select_set(True)
bpy.ops.export_scene.fbx(filepath='E:/!for WORK/all for one/Assets/Generated/Imported/SkyCity_LocalFeed.fbx',use_selection=True,object_types={'MESH'},global_scale=1,apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',bake_anim=False)
print('Exported one local inlet pipe')
