import bpy, math, json, random, os
from mathutils import Matrix, Vector
random.seed(1837)
ROOT='E:/!for WORK/AFO'
OUT=ROOT+'/Assets/OopsPlanet/Models/SoftWorld'
os.makedirs(OUT,exist_ok=True)
for datablock in list(bpy.data.meshes)+list(bpy.data.objects):
    if datablock.name.startswith(('SOFT_','DEC_')): datablock.name='ARCHIVE_'+datablock.name
scene=bpy.data.scenes.new('OOPS_V2_SOFT_WORLD_FINAL'); bpy.context.window.scene=scene
for ob in list(scene.objects):
    if ob.name=='SOFT_CALIBRATION': bpy.data.objects.remove(ob,do_unlink=True)
palette={
 'Grass':'79C762','GrassLight':'91D66C','GrassDeep':'5C9B88','Cliff':'79AEA2',
 'Cream':'F9E8B9','Path':'E8CE98','Water':'61CFDC','Teal':'4C929B','Mint':'B4DBCD',
 'Coral':'EA9E84','Peach':'F6C2A0','Roof':'65B8B0','Yellow':'FFD579','Violet':'B5A8DE',
 'Trunk':'B58C6A','Leaf':'6ABD72','LeafLight':'ADDA80','Flower':'F2ACCF','Cloud':'FFF9E8',
 'Metal':'98B8C5','Dark':'54848A','White':'FFFCED'}
def rgb(h):
    values=[int(h[i:i+2],16)/255 for i in (0,2,4)]
    return tuple(v/12.92 if v<=.04045 else ((v+.055)/1.055)**2.4 for v in values)
mats={}
for role,h in palette.items():
    m=bpy.data.materials.new('SW_'+role); m.use_nodes=True; m.diffuse_color=(*rgb(h),1)
    bs=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
    bs.inputs['Base Color'].default_value=(*rgb(h),1); bs.inputs['Roughness'].default_value=.73
    mats[role]=m
def assign(ob,role):
    ob.data.materials.clear(); ob.data.materials.append(mats[role])
    for p in ob.data.polygons: p.use_smooth=True
def role_for(row):
    n=row['name'].lower(); m=row['material'].lower()
    if 'water' in m or 'water' in n or 'channel' in n: return 'Water'
    if 'ice' in m: return 'Mint'
    if 'wax' in m: return 'Yellow'
    if 'plant' in m or 'vine' in m: return 'LeafLight'
    if 'paddy' in m: return 'Grass'
    if 'floor' in n or 'path' in n or 'shortcut' in n and 'wall' not in n:
        return 'Path' if 'factory' in n else 'GrassLight'
    if 'checkpoint' in m or 'pad' in n: return 'Yellow'
    if 'ship part' in m: return 'Yellow'
    if 'ship' in m: return 'Coral' if 'hull' in m else 'Mint'
    if 'pit wall' in m: return 'Cliff'
    if 'wood' in m or 'pump house' in n or 'shed' in n: return 'Peach'
    if 'steel' in m or 'metal' in m: return 'Metal'
    if 'rust' in m or 'boiler' in n: return 'Coral'
    if 'machine' in m or 'conveyor' in m: return 'Teal'
    if 'src electric' in m: return 'Yellow'
    if 'src magnet' in m: return 'Metal'
    if 'hazard' in m: return 'Yellow'
    if 'charcoal' in m: return 'Teal' if 'factory' in n or 'storage' in n or 'hall' in n else 'Cliff'
    return 'Cream'
rows=json.load(open(ROOT+'/ArtSource/Blender/SoftWorld_layout.json',encoding='utf-8-sig'))
rows=[r for r in rows if r['material']!='Grid lines']
modules=[]
def rounded_module(row):
    sx,sy,sz=[max(abs(v),.005) for v in row['size']]
    # Model in metres, bevel in metres, then normalize the mesh for the original Unity transform.
    bpy.ops.mesh.primitive_cube_add(size=1)
    ob=bpy.context.object; ob.name=row['key']; ob.data.name=row['key']
    ob.scale=(sx,sz,sy); bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    bevel=ob.modifiers.new('Soft six-segment edges','BEVEL'); bevel.width=min(.45,min(sx,sy,sz)*.38); bevel.segments=6
    bpy.ops.object.modifier_apply(modifier=bevel.name)
    for face in ob.data.polygons: face.use_smooth=True
    wn=ob.modifiers.new('Soft weighted normals','WEIGHTED_NORMAL'); wn.keep_sharp=True
    bpy.ops.object.modifier_apply(modifier=wn.name)
    ob.data.transform(Matrix.Diagonal((1/sx,1/sz,1/sy,1)))
    ob.location=(-row['pos'][0],-row['pos'][2],row['pos'][1]); ob.scale=(sx,sz,sy)
    ux,uy,uz=row['rot']; ob.rotation_euler=(math.radians(ux),math.radians(-uz),math.radians(-uy))
    assign(ob,role_for(row)); row['role']=role_for(row)
    if not row['active'] or row['name'].lower().startswith(('hub wall ','factory wall ','rice wall ','factory path wall ','rice path wall ','dike wall ','conveyor wall ','lights shortcut wall ')):
        ob.hide_render=True; ob.hide_set(True)
    row['key']=ob.data.name; modules.append(ob)
for r in rows: rounded_module(r)
rounded_module({'key':'SOFT_RAMP','name':'Vine stair walkable collision','size':[math.sqrt(7.2**2+6.6**2),.16,1.6],'pos':[63.6,3.3,13],'rot':[0,0,math.degrees(math.atan2(6.6,7.2))],'active':False,'material':'V2 vine'})
rounded_module({'key':'SOFT_UNIT','name':'Rounded cube','size':[1,1,1],'pos':[0,-20,0],'rot':[0,0,0],'active':False,'material':'Crate wood'})

decor=[]
def sphere(name,center,size,role,segments=24,rings=12):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments,ring_count=rings,location=(-center[0],-center[2],center[1]))
    ob=bpy.context.object; ob.name=name; ob.scale=(size[0],size[2],size[1]); bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    assign(ob,role); decor.append(ob); return ob
def softbox(name,center,size,role,bevel=.2):
    bpy.ops.mesh.primitive_cube_add(size=1,location=(-center[0],-center[2],center[1]))
    ob=bpy.context.object; ob.name=name; ob.scale=(size[0],size[2],size[1]); bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    mod=ob.modifiers.new('Rounded corners','BEVEL'); mod.width=min(bevel,min(size)*.4); mod.segments=6
    bpy.ops.object.modifier_apply(modifier=mod.name); assign(ob,role); decor.append(ob); return ob
def tree(x,z,scale=1):
    sphere('Tree trunk',(x,1.5*scale,z),(.34*scale,1.65*scale,.34*scale),'Trunk')
    for dx,dy,dz,s in [(-.85,3.8,0,1.55),(.85,4.0,.1,1.6),(0,5.05,.1,1.55),(0,4.1,-.75,1.45)]:
        sphere('Cloud canopy',(x+dx*scale,dy*scale,z+dz*scale),(s*scale,s*.9*scale,s*scale),'LeafLight' if dy>4.5 else 'Leaf')
def bush(x,z,s=1,y=.35):
    sphere('Pillow shrub',(x,y+.35*s,z),(s,.7*s,s*.8),random.choice(['Leaf','GrassDeep','LeafLight']))
def flower(x,z,s=1):
    role=random.choice(['Flower','Yellow','White'])
    for a in range(5):
        t=a*math.tau/5
        sphere('Flower petal',(x+math.cos(t)*.16*s,.16,z+math.sin(t)*.16*s),(.16*s,.08*s,.16*s),role,16,8)
    sphere('Flower centre',(x,.22,z),(.1*s,.08*s,.1*s),'Yellow',16,8)

# Soft island skirts sit below the playable surfaces, outside puzzle paths.
for center,size,role in [((0,-3.2,0),(23,3.2,23),'Cliff'),((0,-4,48),(23,4,21),'Cliff'),((52,-4,0),(23,4,26),'Cliff')]:
    sphere('Floating island underside',center,size,role,48,24)
    sphere('Floating island soft bottom',(center[0],center[1]-2.4,center[2]),(size[0]*.82,size[1]*.9,size[2]*.84),'Mint',40,20)

# Borders: leave every doorway and connection unobstructed at ground level.
for x,z,s in [(-25,-16,1.25),(-25,-4,1.1),(-25,12,1.4),(-16,-25,1.2),(-2,-25,1.15),(14,-25,1.1),
              (-25,34,1.3),(-25,47,1.5),(-25,61,1.4),(-15,72,1.4),(2,72,1.6),(17,72,1.3),
              (37,-27,1.1),(50,-28,1.2),(65,-27,1.4),(78,-17,1.3),(78,-3,1.2),(78,13,1.5),(65,27,1.2),(49,27,1.5),(35,27,1.2)]:
    tree(x,z,s)
for x,z in [(-24,23),(-24,57),(7,73),(32,28),(78,20),(80,-20),(-18,-28),(9,-29)]:
    sphere('Rounded distant hill',(x,1.6,z),(random.uniform(4.5,7),random.uniform(3.2,6),random.uniform(4,6)),'Grass',32,16)

# Low pillow hedges align with collision boundaries without blocking openings.
for x in range(-18,19,3): bush(x,-21.4,1.5); bush(x,68,1.5)
for z in range(-18,19,3): bush(-21.4,z,1.5)
for z in range(32,65,3): bush(-21.4,z,1.5)
for z in range(-20,21,3): bush(73.4,z,1.5)
for x in range(34,72,3): bush(x,-23.4,1.5); bush(x,23.4,1.5)

# A readable crash-site garden around the central ship; no props on its approach or pickup paths.
for x,z in [(-3,-17),(3,-17),(15,-17),(-4,4),(-6,12),(8,15),(14,8),(16,-5)]:
    for k in range(3): flower(x+random.uniform(-.65,.65),z+random.uniform(-.65,.65),random.uniform(.9,1.3))
for x,z in [(-17,5),(-12,14),(6,-17),(15,13)]: bush(x,z,.9)
for x,z,s in [(-16,13,.65),(15,-17,.55)]: tree(x,z,s)

# Factory reads as a friendly toy workshop: rounded roof caps and a cluster of puffing stacks.
softbox('Shed rounded roof',(-16.5,7.12,33.5),(5.3,.26,5.3),'Roof',.2)
softbox('Pump house rounded eave',(69,4.94,13),(4.15,.16,4.15),'Roof',.07)
for x,z,h in [(-17,67,8.5),(-11,67,10),(-5,67,7.8)]:
    sphere('Workshop chimney',(x,h/2,z),(1.05,h/2,1.05),'Coral',24,16)
    sphere('Chimney rounded rim',(x,h,z),(1.23,.35,1.23),'Cream')
    for j in range(3): sphere('Marshmallow steam',(x+.25*j,h+1+j*1.15,z),(.7+j*.2,.65+j*.2,.7+j*.2),'Cloud')
for x,z in [(-17,44),(-18,62),(17,47),(17,63)]: bush(x,z,.65)

# Rice field borders, distant reeds and little water lilies.
for x,z in [(36,-18),(41,-18),(46,-18),(34,20),(49,20),(70,-18)]: flower(x,z,1.25)
for x,z in [(53,-18),(58,-5),(54,9),(58,18)]:
    sphere('Water lily leaf',(x,-.27,z),(.6,.035,.52),'LeafLight',24,10)
    flower(x,z,.8)
for x,z in [(35,-12),(35,-4),(49,19),(70,-7)]:
    for dx in [-.15,.1,.3]: sphere('Rice reed',(x+dx,.55,z+random.uniform(-.2,.2)),(.12,.6,.12),'LeafLight',16,8)

# Curved stepping-stone path accents use mesh only, so they cannot alter puzzle physics.
for x,z in [(-4,-2),(-3,1),(-2,4),(-1,7),(0,10),(0,13),(0,16),(0,22),(0,25),(0,28),
             (22,0),(25,0),(28,0),(31,0),(35,0),(39,0),(43,0),(47,0)]:
    sphere('Cream path stone',(x,.012,z),(1.0,.026,.75),'Path',24,10)

# A few high, rounded clouds form a soft horizon, well above all game cameras.
for cx,cy,cz,s in [(-28,20,-30,1.3),(12,24,-35,1.6),(58,23,-31,1.5),(-32,25,40,1.4),(36,24,78,1.8),(85,25,30,1.4)]:
    for dx,dz,k in [(-2,0,1),(0,0,1.4),(2,.2,1),(0,1,1.1)]: sphere('Cotton cloud',(cx+dx*s,cy,cz+dz*s),(2*s*k,.9*s*k,1.4*s*k),'Cloud',32,16)


# Broad soft perimeter mounds replace the tall thin developer boundary panels.
for row in rows:
    n=row['name'].lower()
    if not n.startswith(('hub wall ','factory wall ','rice wall ','factory path wall ','rice path wall ','dike wall ','conveyor wall ','lights shortcut wall ')): continue
    sx,sy,sz=row['size']; x,y,z=row['pos']; along_x=sx>sz
    length=max(sx,sz); count=max(1,math.ceil(length/6)); spacing=length/count
    for i in range(count):
        delta=-length/2+(i+.5)*spacing
        centre=(x+delta if along_x else x,y,z if along_x else z+delta)
        size=(spacing*.58,max(1.3,sy*.53),1.7) if along_x else (1.7,max(1.3,sy*.53),spacing*.58)
        sphere('Rounded mossy boundary',centre,size,'GrassDeep' if 'rice' in n else 'Leaf',32,20)

# Export normalized replacement meshes with identity object transforms. Keep world placement in the .blend.
def select(objects):
    bpy.ops.object.select_all(action='DESELECT')
    for ob in objects: ob.select_set(True)
    if objects: bpy.context.view_layer.objects.active=objects[0]
fbx=dict(use_selection=True,object_types={'MESH'},apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',bake_space_transform=True,mesh_smooth_type='FACE',add_leaf_bones=False,bake_anim=False)
saved=[(o,o.matrix_world.copy()) for o in modules]
for o,_ in saved: o.hide_set(False); o.matrix_world=Matrix.Identity(4)
select(modules)
bpy.ops.export_scene.fbx(filepath=OUT+'/SoftWorld_Modules.fbx',**fbx)
for o,m in saved: o.matrix_world=m; o.hide_set(o.hide_render)

# Batch decorative meshes by material, with origin at world zero, for inexpensive Unity draw calls.
joined=[]
for role in palette:
    group=[o for o in list(scene.objects) if o.type=='MESH' and not o.name.startswith('SOFT_') and o.data.materials and o.data.materials[0]==mats[role]]
    if not group: continue
    select(group); bpy.ops.object.join(); ob=bpy.context.object; ob.name='DEC_'+role; ob.data.name='DEC_'+role
    scene.cursor.location=(0,0,0); bpy.ops.object.origin_set(type='ORIGIN_CURSOR'); joined.append(ob)
select(joined); bpy.ops.export_scene.fbx(filepath=OUT+'/SoftWorld_Garden.fbx',**fbx)
json.dump({'items':rows},open(OUT+'/SoftWorld_manifest.json','w',encoding='utf-8'),ensure_ascii=False,indent=2)
json.dump(palette,open(ROOT+'/ArtSource/Blender/SoftWorld_palette.json','w'),indent=2)

# Blender presentation scene. The Unity scene receives its own matching light setup.
world=bpy.data.worlds.new('Soft turquoise daylight'); world.use_nodes=True
next(n for n in world.node_tree.nodes if n.type=='BACKGROUND').inputs[0].default_value=(.53,.76,.86,1)
next(n for n in world.node_tree.nodes if n.type=='BACKGROUND').inputs[1].default_value=.65
scene.world=world
ld=bpy.data.lights.new('Warm afternoon sun','SUN'); sun=bpy.data.objects.new('Warm afternoon sun',ld); scene.collection.objects.link(sun); sun.rotation_euler=(.45,-.45,-.4); ld.energy=2; ld.angle=.25
ld=bpy.data.lights.new('Soft sky fill','AREA'); light=bpy.data.objects.new('Soft sky fill',ld); scene.collection.objects.link(light); light.location=(0,-25,65); ld.energy=1800; ld.shape='DISK'; ld.size=65
camd=bpy.data.cameras.new('Soft World Overview'); cam=bpy.data.objects.new('Soft World Overview',camd); scene.collection.objects.link(cam); cam.location=(105,110,105)
target=Vector((-20,-22,0)); cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler(); camd.type='ORTHO'; camd.ortho_scale=143; scene.camera=cam
scene.render.resolution_x=1600; scene.render.resolution_y=1100; scene.render.resolution_percentage=100
scene.view_settings.view_transform='AgX'
select([])
for area in bpy.context.screen.areas:
    if area.type=='VIEW_3D':
        area.spaces.active.region_3d.view_location=target; area.spaces.active.region_3d.view_distance=140
bpy.ops.wm.save_as_mainfile(filepath=ROOT+'/ArtSource/Blender/OOPS_V2_SoftWorld.blend')
print(json.dumps({'modules':len(modules),'decor_batches':len(joined),'triangles_estimate':sum(len(p.vertices)-2 for o in modules+joined for p in o.data.polygons),'source':bpy.data.filepath}))
