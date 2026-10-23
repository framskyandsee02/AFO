import bpy, math, json, bmesh, shutil
from pathlib import Path
from mathutils import Vector, Matrix

ROOT=Path(__file__).resolve().parents[3]
ART=ROOT/'Art/Pino'
ASSETS=ROOT/'Assets/PinoCharacter'
for directory in (ART/'Source',ART/'Exports',ART/'MorphPreviews',ASSETS/'Models',ASSETS/'Textures'):
    directory.mkdir(parents=True,exist_ok=True)
scene=bpy.data.scenes['Pino Studio']
bpy.context.window.scene=scene
collection=bpy.data.collections['01  PINO • character']
body=next(o for o in collection.objects if o.name.startswith('Pino.Body'))
eyes=[o for o in collection.objects if o.name.startswith('Pino.Eye')]
root=next(o for o in collection.objects if o.type=='EMPTY')
assert len(eyes)==2 and not body.data.shape_keys
H=.52

def smoothstep(a,b,t):
    t=min(1,max(0,(t-a)/(b-a)))
    return t*t*(3-2*t)

def spring(p):
    x,y,z=p; t=min(1,max(0,z/H))
    radius=.42+.58*smoothstep(.38,.74,t)
    return Vector((x*radius,y*radius,z*1.22))
def compressed(p):
    q=spring(p); return Vector((q.x*1.15,q.y*1.15,q.z*.43))
def stretched(p):
    q=spring(p); return Vector((q.x*.86,q.y*.86,q.z*1.45))
def magnet(p):
    x,y,z=p; t=min(1,max(0,z/H))
    return Vector((x*(1.13-.08*t),y*.93,z*.90+.012*math.sin(math.pi*t)))
def gas(p):
    x,y,z=p; t=min(1,max(0,z/H))
    return Vector((x*1.13,y*1.13,H*1.32*t**.83))
def liquid(p):
    x,y,z=p; return Vector((x*1.12,y*1.23,z*.65))
def liquid_flow(p):
    x,y,z=p; t=min(1,max(0,z/H))
    return Vector((x*.94,y*1.72-.035*math.sin(math.pi*t),H*.21*t**1.15))
def absorb(p):
    x,y,z=p; return Vector((x*1.055,y*1.055,z*1.065))
def idle(p):
    x,y,z=p; return Vector((x*.992,y*.992,z*1.018))
def land(p):
    x,y,z=p; return Vector((x*1.09,y*1.09,z*.83))

functions={'Spring':spring,'SpringCompress':compressed,'SpringStretch':stretched,'Magnet':magnet,'Gas':gas,'Liquid':liquid,'LiquidFlow':liquid_flow,'Absorb':absorb,'Idle':idle,'Land':land}
relative={'SpringCompress':'Spring','SpringStretch':'Spring','LiquidFlow':'Liquid'}

def body_points(func):
    return [func(v.co) for v in body.data.vertices]

def eye_points(obj,func,name):
    matrix=obj.matrix_world.copy(); inverse=matrix.inverted()
    center=matrix.translation.copy()
    old_normal=(matrix.to_3x3() @ Vector((0,0,1))).normalized()
    anchor=center-old_normal*.002
    eps=.00002
    columns=[]
    for axis in range(3):
        step=Vector((0,0,0)); step[axis]=eps
        columns.append((func(anchor+step)-func(anchor-step))/(2*eps))
    jacobian=Matrix(columns).transposed()
    normal=(jacobian.inverted_safe().transposed() @ old_normal).normalized()
    rotation=old_normal.rotation_difference(normal)
    new_center=func(anchor)+normal*.002
    size=.78 if name=='LiquidFlow' else 1
    return [inverse @ (new_center+rotation @ ((matrix @ v.co-center)*size)) for v in obj.data.vertices]

for obj in [body]+eyes:
    obj.shape_key_add(name='Basis')
    base=[v.co.copy() for v in obj.data.vertices]
    targets={name:(body_points(fn) if obj==body else eye_points(obj,fn,name)) for name,fn in functions.items()}
    for name in functions:
        key=obj.shape_key_add(name=name)
        for i,point in enumerate(targets[name]):
            key.data[i].co=base[i]+point-targets[relative[name]][i] if name in relative else point
        key.slider_min=0; key.slider_max=1
    obj['morph_notes']='SpringCompress and SpringStretch are additive to Spring; LiquidFlow is additive to Liquid.'

def mat(name,c,metallic=0):
    m=bpy.data.materials.new(name); m.use_nodes=True
    bsdf=m.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value=(*c,1)
    bsdf.inputs['Metallic'].default_value=metallic
    bsdf.inputs['Roughness'].default_value=.24
    m.diffuse_color=(*c,1)
    return m
silver=mat('Pino.Accessory.Silver',(.58,.72,.78),.78)
red=mat('Pino.Accessory.MagnetRed',(.74,.022,.055),.55)
spring_metal=mat('Pino.Accessory.SpringMetal',(.07,.48,.75),.72)

def tube(name,path,radius,center,materials,material_for_point=None,sides=12):
    points=[Vector(p) for p in path]; verts=[]; faces=[]; mids=[]
    for i,p in enumerate(points):
        tangent=(points[min(i+1,len(points)-1)]-points[max(0,i-1)]).normalized()
        reference=Vector((0,1,0)) if abs(tangent.y)<.85 else Vector((1,0,0))
        axis=tangent.cross(reference).normalized(); other=tangent.cross(axis).normalized()
        for j in range(sides):
            a=2*math.pi*j/sides
            verts.append(tuple(p-center+radius*(math.cos(a)*axis+math.sin(a)*other)))
    for i in range(len(points)-1):
        for j in range(sides):
            k=(j+1)%sides
            faces.append((i*sides+j,i*sides+k,(i+1)*sides+k,(i+1)*sides+j))
            mids.append(material_for_point((points[i]+points[i+1])*.5) if material_for_point else 0)
    faces.append(tuple(reversed(range(sides)))); mids.append(0)
    faces.append(tuple((len(points)-1)*sides+j for j in range(sides))); mids.append(0)
    mesh=bpy.data.meshes.new(name+' mesh'); mesh.from_pydata(verts,[],faces); mesh.update()
    obj=bpy.data.objects.new(name,mesh); collection.objects.link(obj); obj.parent=root; obj.location=center
    for material in materials: mesh.materials.append(material)
    for poly,index in zip(mesh.polygons,mids): poly.material_index=index; poly.use_smooth=True
    bm=bmesh.new(); bm.from_mesh(mesh); bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces)); bm.to_mesh(mesh); bm.free()
    return obj

# Spring proof is built and checked before the other form presentation assets.
coil_path=[]
for i in range(225):
    t=i/224; a=t*math.tau*3.4
    coil_path.append((.175*math.cos(a),.175*math.sin(a),.025+.29*t))
coil=tube('Pino.SpringCoil',coil_path,.017,Vector((0,0,.17)),[spring_metal])
coil.shape_key_add(name='Basis')
for name,sy,sr in [('SpringCompress',.43,1.15),('SpringStretch',1.45,.86)]:
    key=coil.shape_key_add(name=name)
    for vertex,point in zip(coil.data.vertices,key.data):
        world=vertex.co+coil.location
        point.co=Vector((world.x*sr,world.y*sr,world.z*sy))-coil.location
coil['visibility']='Spring form only. Runtime controller owns visibility and growth.'

magnet_path=[(-.11,.025,.66),(-.11,.025,.52)]
for i in range(49):
    angle=math.pi+math.pi*i/48
    magnet_path.append((.11*math.cos(angle),.025,.52+.11*math.sin(angle)))
magnet_path.append((.11,.025,.66))
magnet_obj=tube('Pino.Magnet',magnet_path,.025,Vector((0,.025,.53)),[red,silver],lambda p:1 if p.z>.625 else 0,16)
magnet_obj['visibility']='Magnet form only. Runtime controller owns visibility and growth.'

# Protect source mesh topology and attachment deltas before export.
report={'base_source':'Art/Pino/Pino.blend','unity_version':'6000.3.25f1','pipeline':'URP 17.3.0','technique':'FBX blend shapes + additive surface-attached eye targets + separate form accessories','heart':'Absent, preserving the user-requested heartless base.','meshes':[],'spring_proof':{}}
for obj in collection.objects:
    if obj.type!='MESH': continue
    bm=bmesh.new(); bm.from_mesh(obj.data)
    bad=sum(not e.is_manifold for e in bm.edges); bm.free()
    assert bad==0,(obj.name,bad)
    report['meshes'].append({'name':obj.name,'vertices':len(obj.data.vertices),'faces':len(obj.data.polygons),'shape_keys':[k.name for k in obj.data.shape_keys.key_blocks] if obj.data.shape_keys else [],'uv_layers':[u.name for u in obj.data.uv_layers],'manifold':True})
base_z=max(v.co.z for v in body.data.vertices)
for name,fn in [('Spring',spring),('SpringCompressed',compressed),('SpringStretched',stretched)]:
    pts=body_points(fn)
    height=max(p.z for p in pts)-min(p.z for p in pts)
    assert abs(height-base_z)>.02
    report['spring_proof'][name]={'height_m':height,'geometry_changes':True}
report['spring_proof']['passed']=True

def pose(form=None,charge=0,stretch=0,flow=0):
    for obj in collection.objects:
        if obj.type=='MESH' and obj.data.shape_keys:
            for key in obj.data.shape_keys.key_blocks:
                key.value=0
            values={form:1,'SpringCompress':charge,'SpringStretch':stretch,'LiquidFlow':flow}
            for name,value in values.items():
                key=obj.data.shape_keys.key_blocks.get(name) if name else None
                if key: key.value=value
    coil.hide_render=form!='Spring'
    magnet_obj.hide_render=form!='Magnet'
    bpy.context.view_layer.update()

pose()
for obj in scene.objects: obj.select_set(False)
for obj in collection.objects: obj.select_set(True)
bpy.context.view_layer.objects.active=body
kwargs=dict(use_selection=True,object_types={'MESH','EMPTY'},use_mesh_modifiers=False,bake_anim=False,add_leaf_bones=False,axis_forward='-Z',axis_up='Y',global_scale=1,apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',mesh_smooth_type='FACE',path_mode='COPY',embed_textures=True)
bpy.ops.export_scene.fbx(filepath=str(ASSETS/'Models/Pino_Morphs.fbx'),**kwargs)
shutil.copy2(ASSETS/'Models/Pino_Morphs.fbx',ART/'Exports/Pino_Morphs.fbx')
shutil.copy2(ART/'Pino_Body_BaseColor.png',ASSETS/'Textures/Pino_Body_BaseColor.png')
scene['morph_pipeline']='Normal, Magnet, Spring, Gas, Liquid. Shape-key parameters and accessories documented in Unity integration guide.'
root['notes']='Morph-ready Pino; heart, podium and sparkle decorations remain absent.'
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(ART/'Source/Pino_Morphs.blend'),check_existing=False)
(ART/'Source/MorphAssetAudit.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print('PINO_MORPHS_EXPORTED='+json.dumps(report,ensure_ascii=False),flush=True)

# Consistent small studio renders make all authored geometry reviewable.
prefs=bpy.context.preferences.addons['cycles'].preferences
try:
    prefs.compute_device_type='CUDA'; prefs.refresh_devices()
    for device in prefs.devices: device.use=device.type=='CUDA'
    scene.cycles.device='GPU' if any(d.type=='CUDA' for d in prefs.devices) else 'CPU'
except Exception:
    scene.cycles.device='CPU'
scene.cycles.samples=40
scene.render.resolution_x=640; scene.render.resolution_y=640
scene.camera.data.ortho_scale=1.15
scene.camera.location=(.78,-3.25,1.15)
scene.camera.rotation_euler=(Vector((0,0,.36))-scene.camera.location).to_track_quat('-Z','Y').to_euler()
for name,form,charge,stretch,flow in [('Normal',None,0,0,0),('Spring','Spring',0,0,0),('SpringCompressed','Spring',1,0,0),('SpringStretched','Spring',0,1,0),('Magnet','Magnet',0,0,0),('Gas','Gas',0,0,0),('Liquid','Liquid',0,0,0),('LiquidFlow','Liquid',0,0,1)]:
    pose(form,charge,stretch,flow)
    scene.render.filepath=str(ART/'MorphPreviews'/('Pino_'+name+'.png'))
    bpy.ops.render.render(write_still=True,scene=scene.name)
    print('PINO_MORPH_RENDER='+name,flush=True)
pose()
print('PINO_MORPHS_DONE',flush=True)

