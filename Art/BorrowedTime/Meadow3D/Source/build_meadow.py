"""Build an original, editable 3D grass/clover meadow in Blender; no reference figures."""
import bpy
import math
import random
import json
import os
from pathlib import Path
from mathutils import Vector, noise

ROOT = Path(r'C:/Users/Pc/Documents/GitHub/AFO')
OUT = ROOT / 'Art/BorrowedTime/Meadow3D'
FBX = ROOT / 'Assets/BorrowedTime/Environment/Models/BT_Meadow.fbx'
rng = random.Random(246831)
bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.unit_settings.scale_length = 1.0

def enum_value(prop, desired):
    valid = [item.identifier for item in prop.enum_items]
    if desired not in valid:
        raise ValueError(f'{desired} not in {valid}')
    return desired

def linear(v):
    return v / 12.92 if v <= .04045 else ((v + .055) / 1.055) ** 2.4

def mix(a, b, t):
    t = max(0, min(1, t))
    return tuple(a[i] * (1-t) + b[i] * t for i in range(3))

def ground_height(x, y):
    return .035 * math.sin(x * .38) * math.cos(y * .44) + .018 * noise.noise(Vector((x * .8, y * .8, .7)))

def palette(x, y, variation=0):
    cloud = noise.noise(Vector((x * .37, y * .37, 2.3)))
    flecks = noise.noise(Vector((x * 2.1, y * 2.1, 4.6)))
    sun = .61 + .17 * math.sin(x * .42 + y * .24) + .19 * cloud + .12 * flecks
    sun += .16 * math.exp(-((x + 4) ** 2 + (y + 1) ** 2) / 65)
    sun -= .20 / (1 + math.exp(-(x - 5) * .45))
    sun -= .13 * min(1, (abs(x) / 17) ** 3 + (abs(y) / 12) ** 3)
    sun = max(0, min(1, sun + variation))
    shade, moss, light = (.20, .43, .35), (.48, .66, .27), (.79, .87, .39)
    return mix(shade, moss, sun * 2) if sun < .5 else mix(moss, light, (sun - .5) * 2)

material = bpy.data.materials.new('BT_Painterly_VertexColor')
material.use_nodes = True
bsdf = next(n for n in material.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
bsdf.inputs['Roughness'].default_value = .94
bsdf.inputs['Specular IOR Level'].default_value = .15
attribute = material.node_tree.nodes.new('ShaderNodeVertexColor')
attribute.layer_name = 'Color'
material.node_tree.links.new(attribute.outputs['Color'], bsdf.inputs['Base Color'])

root = bpy.data.objects.new('BT_Meadow_3D', None)
scene.collection.objects.link(root)
groups = {}
for name in ['01_Ground', '02_Grass', '03_Clover', '04_Wildflowers']:
    group = bpy.data.objects.new(name, None)
    scene.collection.objects.link(group)
    group.parent = root
    groups[name] = group

class MeshBatch:
    def __init__(self, name, group):
        self.name, self.group = name, group
        self.vertices, self.faces, self.colors = [], [], []
    def vertex(self, xyz, color):
        self.vertices.append(xyz)
        self.colors.append((*[linear(max(0, min(1, c))) for c in color], 1))
        return len(self.vertices)-1
    def face(self, *indices):
        self.faces.append(indices)
    def finish(self):
        mesh = bpy.data.meshes.new(self.name)
        mesh.from_pydata(self.vertices, [], self.faces)
        mesh.update()
        colors = mesh.color_attributes.new(name='Color', type='FLOAT_COLOR', domain='POINT')
        colors.data.foreach_set('color', [c for color in self.colors for c in color])
        obj = bpy.data.objects.new(self.name, mesh)
        scene.collection.objects.link(obj)
        obj.parent = self.group
        obj.data.materials.append(material)
        return obj

# A gently undulating, full mesh ground (36 x 26 metres), with organic dappled color.
ground = MeshBatch('Ground_Surface_36x26m', groups['01_Ground'])
nx, ny = 180, 130
for j in range(ny+1):
    y = -13 + 26*j/ny
    for i in range(nx+1):
        x = -18 + 36*i/nx
        ground.vertex((x, y, ground_height(x, y)), palette(x, y, -.10))
for j in range(ny):
    for i in range(nx):
        a = j*(nx+1)+i
        ground.face(a, a+1, a+nx+2)
        ground.face(a, a+nx+2, a+nx+1)
ground.finish()

def tile(x, y):
    return min(3, max(0, int((x+18)/9))) + 4*min(2, max(0, int((y+13)/(26/3))))

grass = [MeshBatch(f'Grass_Tile_{i:02}', groups['02_Grass']) for i in range(12)]
clover = [MeshBatch(f'Clover_Tile_{i:02}', groups['03_Clover']) for i in range(12)]
flowers = [MeshBatch(f'Flowers_Tile_{i:02}', groups['04_Wildflowers']) for i in range(12)]

# Short, bent broad grass blades. Keep the resting area particularly low and soft.
for n in range(14000):
    x, y = rng.uniform(-17.9,17.9), rng.uniform(-12.9,12.9)
    batch = grass[tile(x,y)]
    resting = ((x+4.5)/6)**2 + ((y+1)/4.5)**2 < 1
    for blade in range(rng.randint(3,5)):
        px, py = x+rng.uniform(-.1,.1), y+rng.uniform(-.1,.1)
        height = rng.uniform(.07,.17) if resting else rng.uniform(.12,.30)
        width = rng.uniform(.022,.047)
        angle = rng.random()*math.tau
        direction = Vector((math.cos(angle),math.sin(angle),0))
        side = Vector((-direction.y,direction.x,0))
        center = Vector((px,py,ground_height(px,py)+.006))
        color = palette(px,py,rng.uniform(-.1,.11))
        ids=[]
        for t, taper in [(0,.75),(.55,1),(1,.01)]:
            position = center + direction*(height*.8*t*t) + Vector((0,0,height*t))
            tint = mix(color, (.85,.91,.47), .18*t)
            ids.extend([batch.vertex(tuple(position-side*width*taper),tint),batch.vertex(tuple(position+side*width*taper),tint)])
        batch.face(ids[0],ids[1],ids[3]); batch.face(ids[0],ids[3],ids[2])
        batch.face(ids[2],ids[3],ids[5]); batch.face(ids[2],ids[5],ids[4])

# Trifoliate clover: genuinely modelled heart-shaped, cupped leaves and central ribs.
for n in range(3900):
    x,y = rng.uniform(-17.8,17.8),rng.uniform(-12.8,12.8)
    batch=clover[tile(x,y)]
    size=rng.uniform(.10,.19); az=rng.random()*math.tau
    base_z=ground_height(x,y)+rng.uniform(.035,.085)
    shade=rng.uniform(-.1,.13)
    for petal in range(3):
        a=az+petal*math.tau/3
        outward=Vector((math.cos(a),math.sin(a),0)); across=Vector((-outward.y,outward.x,0))
        center=Vector((x,y,base_z))+outward*size*.85
        color=palette(x,y,shade)
        middle=batch.vertex(tuple(center+Vector((0,0,.026))),mix(color,(.84,.89,.43),.13))
        outline=[]
        for k in range(16):
            t=math.tau*k/16
            hx=math.sin(t)**3
            hy=(13*math.cos(t)-5*math.cos(2*t)-2*math.cos(3*t)-math.cos(4*t))/17
            v=center+across*(hx*size*.76)+outward*(hy*size)
            v.z += .014*hy - .015*abs(hx)
            outline.append(batch.vertex(tuple(v),mix(color,(.69,.81,.38),.08*(1+hy))))
        for k in range(16): batch.face(middle,outline[k],outline[(k+1)%16])

# Restrained, tiny five-petal cream/butter flowers, mainly outside the sleeping area.
count=0
while count<300:
    x,y=rng.uniform(-17.6,17.6),rng.uniform(-12.6,12.6)
    if ((x+4.5)/6.3)**2+((y+1)/4.8)**2 < 1 and rng.random()<.94: continue
    batch=flowers[tile(x,y)]
    scale=rng.uniform(.07,.135); z=ground_height(x,y)+rng.uniform(.09,.17)
    rotation=rng.random()*math.tau
    petal_color=(.99,.95,.72) if count%4 else (.88,.92,.49)
    for p in range(5):
        a=rotation+p*math.tau/5
        d=Vector((math.cos(a),math.sin(a),0)); side=Vector((-d.y,d.x,0))
        center=Vector((x,y,z))+d*scale*.62
        c=batch.vertex(tuple(center+Vector((0,0,-.01))),petal_color)
        ids=[]
        for k in range(10):
            t=k*math.tau/10
            v=center+d*(math.cos(t)*scale*.69)+side*(math.sin(t)*scale*.43)
            v.z += .015*(1+math.cos(t))
            ids.append(batch.vertex(tuple(v),mix(petal_color,(1,1,.9),.13)))
        for k in range(10): batch.face(c,ids[k],ids[(k+1)%10])
    c=batch.vertex((x,y,z+.018),(.88,.69,.21))
    ids=[batch.vertex((x+math.cos(k*math.tau/10)*scale*.32,y+math.sin(k*math.tau/10)*scale*.32,z+.015),(.94,.79,.28)) for k in range(10)]
    for k in range(10): batch.face(c,ids[k],ids[(k+1)%10])
    count+=1

for batch in grass+clover+flowers:
    if batch.faces: batch.finish()

anchor=bpy.data.objects.new('Character_Area_EMPTY',None)
scene.collection.objects.link(anchor); anchor.location=(-4.5,-1,.08)

camera_data=bpy.data.cameras.new('Meadow_PresentationCamera')
camera=bpy.data.objects.new('Meadow_PresentationCamera',camera_data)
scene.collection.objects.link(camera); camera.location=(0,-5,24)
camera.rotation_euler=(Vector((0,0,0))-camera.location).to_track_quat('-Z','Y').to_euler()
camera_data.type=enum_value(camera_data.bl_rna.properties['type'],'ORTHO')
camera_data.ortho_scale=30.8; scene.camera=camera

sun_data=bpy.data.lights.new('Warm_dappled_sun',enum_value(bpy.types.Light.bl_rna.properties['type'],'SUN'))
sun_data.energy=1.65; sun_data.angle=.38; sun_data.color=(1,.96,.79)
sun=bpy.data.objects.new('Warm_dappled_sun',sun_data); scene.collection.objects.link(sun)
sun.rotation_euler=(math.radians(25),math.radians(-20),math.radians(-28))
world=bpy.data.worlds.new('Soft_sky_fill'); scene.world=world; world.use_nodes=True
background=next(n for n in world.node_tree.nodes if n.type=='BACKGROUND')
background.inputs['Color'].default_value=(.71,.80,.62,1); background.inputs['Strength'].default_value=.65

scene.render.resolution_x=1600; scene.render.resolution_y=1000; scene.render.resolution_percentage=100
scene.render.image_settings.file_format=enum_value(scene.render.image_settings.bl_rna.properties['file_format'],'PNG')
scene.render.film_transparent=False
available=[i.identifier for i in scene.view_settings.bl_rna.properties['view_transform'].enum_items]
if 'Standard' in available: scene.view_settings.view_transform='Standard'
scene.view_settings.exposure=0; scene.view_settings.gamma=1
scene.render.filepath=str(OUT/'Preview/Blender_Meadow.png')

for obj in bpy.context.view_layer.objects: obj.select_set(False)
for obj in bpy.context.scene.objects:
    if obj==root or obj.parent==root or (obj.parent and obj.parent.parent==root): obj.select_set(True)
bpy.context.view_layer.objects.active=root
props=bpy.ops.export_scene.fbx.get_rna_type().properties
kwargs=dict(filepath=str(FBX),use_selection=True,axis_forward=enum_value(props['axis_forward'],'-Z'),axis_up=enum_value(props['axis_up'],'Y'),add_leaf_bones=False,bake_anim=False)
if 'colors_type' in props: kwargs['colors_type']=enum_value(props['colors_type'],'SRGB')
bpy.ops.export_scene.fbx(**kwargs)

stats={'seed':246831,'dimensions_metres':[36,26],'mesh_objects':len([o for o in scene.objects if o.type=='MESH']),
       'vertices':sum(len(o.data.vertices) for o in scene.objects if o.type=='MESH'),
       'triangles':sum(len(p.vertices)-2 for o in scene.objects if o.type=='MESH' for p in o.data.polygons),
       'flower_clusters':count,'clover_clusters':3900,'grass_clumps':14000,'engine':scene.render.engine,
       'camera_position':list(camera.location),'orthographic_width_metres':camera_data.ortho_scale,
       'note':'Only ground and plants. No people, animals or buildings. All foliage is modelled mesh with vertex colors.'}
(OUT/'ModelInfo.json').write_text(json.dumps(stats,indent=2),encoding='utf-8')
scene['Reference_notes']='Ground-only top-down framing from reference 1; lime, cream, sage and blue-green palette from references 2 and 3. No figures copied.'
for area in bpy.context.screen.areas if bpy.context.screen else []:
    if area.type=='VIEW_3D':
        area.spaces.active.region_3d.view_perspective='CAMERA'
        shading=area.spaces.active.shading
        shading.color_type=enum_value(shading.bl_rna.properties['color_type'],'VERTEX')
for obj in scene.objects: obj.select_set(False)
root.select_set(True); bpy.context.view_layer.objects.active=root
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'BT_Meadow.blend'))
print('MEADOW_MODEL_READY '+json.dumps(stats),flush=True)
bpy.ops.render.render(write_still=True)
print('MEADOW_RENDER_READY '+scene.render.filepath,flush=True)
camera.location=(-5,-5,3.7)
camera.rotation_euler=(Vector((-4,-1,0))-camera.location).to_track_quat('-Z','Y').to_euler()
camera_data.ortho_scale=5.4
scene.render.resolution_x=1100; scene.render.resolution_y=850
scene.render.filepath=str(OUT/'Preview/Blender_Detail.png')
bpy.ops.render.render(write_still=True)
print('MEADOW_DETAIL_READY '+scene.render.filepath,flush=True)
