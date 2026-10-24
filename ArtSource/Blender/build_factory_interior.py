# OOPS! PLANET - factory interior art kit (owner brief 2026-10-24: old, sleepy toy factory; rounded, toy-like
# machines; dusty teal / cream / faded coral). Same conventions as build_world_polish.py:
#   * coordinates are written in Unity space (x right, y up, z forward) through xyz(); 1 unit = 1 m
#   * every model is built around its own origin (bottom centre unless noted) and exported alone
#   * parts are joined per POL_<Role> material so Unity maps them onto the shared WorldPolish materials
# Run inside Blender:  exec(open(r"E:/!for WORK/AFO/ArtSource/Blender/build_factory_interior.py").read())
# Output: Assets/Scenes/OopsPlanet/Models/FactoryInterior/FI_*.fbx  + FactoryInterior_manifest.json
import bpy, bmesh, math, os, json, random
from mathutils import Vector
ROOT='E:/!for WORK/AFO'
OUT=ROOT+'/Assets/Scenes/OopsPlanet/Models/FactoryInterior'
os.makedirs(OUT,exist_ok=True)
# Build in the active scene (run from an empty file); bpy.context.window can be None when driven remotely.
scene=bpy.context.scene; scene.name='OOPS_FACTORY_INTERIOR'
for o in list(scene.objects): bpy.data.objects.remove(o,do_unlink=True)
random.seed(1024)
palette={'Cream':'FFF0CE','White':'FDF8ED','Teal':'579B9C','Dark':'294A60','Metal':'9BB9BE','Coral':'EB997B','Blue':'78B4D4',
         'Water':'50C8CE','Yellow':'F5D163','Orange':'EE9251','Leaf':'83AA78','Coal':'48515F','Rust':'B5735A','Dust':'D9D2BF','Glow':'FFD58A','Ice':'C6EEF7'}
mats={}
for role,h in palette.items():
    m=bpy.data.materials.get('POL_'+role) or bpy.data.materials.new('POL_'+role); m.use_nodes=True
    c=tuple(int(h[i:i+2],16)/255 for i in (0,2,4)); m.diffuse_color=(*c,1)
    n=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED'); n.inputs['Base Color'].default_value=(*c,1); n.inputs['Roughness'].default_value=.55
    if role=='Metal': n.inputs['Metallic'].default_value=.3
    if role=='Glow': n.inputs['Emission Color'].default_value=(*c,1); n.inputs['Emission Strength'].default_value=2
    mats[role]=m
def xyz(p): return (-p[0],-p[2],p[1])
def finish(o,name,role):
    o.name='FI_'+name+'__'+role; o.data.materials.clear(); o.data.materials.append(mats[role])
    for p in o.data.polygons: p.use_smooth=True
    return o
def soft(o,width,seg=3):
    b=o.modifiers.new('Soft','BEVEL'); b.width=width; b.segments=seg; b.limit_method='ANGLE'
    bpy.context.view_layer.objects.active=o; bpy.ops.object.modifier_apply(modifier=b.name)
    w=o.modifiers.new('WN','WEIGHTED_NORMAL'); bpy.ops.object.modifier_apply(modifier=w.name)
def box(name,p,s,role,r=.08,rot=(0,0,0)):
    bpy.ops.mesh.primitive_cube_add(size=1,location=xyz(p)); o=bpy.context.object; o.scale=(s[0],s[2],s[1])
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if r>0: soft(o,min(r,min(s)*.45))
    # rot = degrees about the Unity axes (single-axis use); Unity X->Blender -X, Unity Y->Blender Z, Unity Z->Blender -Y
    if rot!=(0,0,0):
        o.rotation_euler=(math.radians(-rot[0]),math.radians(-rot[2]),math.radians(rot[1]))
        bpy.ops.object.transform_apply(location=False,rotation=True,scale=False)
    return finish(o,name,role)
def cyl(name,p,r,h,role,axis='y',bev=.05,verts=32,r2=None):
    if r2 is None: bpy.ops.mesh.primitive_cylinder_add(vertices=verts,radius=r,depth=h,location=xyz(p))
    else: bpy.ops.mesh.primitive_cone_add(vertices=verts,radius1=r,radius2=r2,depth=h,location=xyz(p))
    o=bpy.context.object
    if axis=='x': o.rotation_euler=(0,math.pi/2,0)
    elif axis=='z': o.rotation_euler=(math.pi/2,0,0)
    bpy.ops.object.transform_apply(location=False,rotation=True,scale=False)
    if bev>0: soft(o,min(bev,h*.3))
    return finish(o,name,role)
def ball(name,p,s,role,seg=24):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=seg,ring_count=seg//2,location=xyz(p)); o=bpy.context.object
    o.scale=(s[0],s[2],s[1]); bpy.ops.object.transform_apply(location=False,rotation=False,scale=True); return finish(o,name,role)
def ring(name,p,R,t,role,axis='y'):
    bpy.ops.mesh.primitive_torus_add(major_segments=40,minor_segments=12,location=xyz(p),major_radius=R,minor_radius=t); o=bpy.context.object
    if axis=='x': o.rotation_euler=(0,math.pi/2,0)
    elif axis=='z': o.rotation_euler=(math.pi/2,0,0)
    bpy.ops.object.transform_apply(location=False,rotation=True,scale=False); return finish(o,name,role)
def tube(name,pts,r,role):
    c=bpy.data.curves.new(name,'CURVE'); c.dimensions='3D'; c.resolution_u=3; c.bevel_depth=r; c.bevel_resolution=4; c.use_fill_caps=True
    sp=c.splines.new('POLY'); sp.points.add(len(pts)-1)
    for a,p in zip(sp.points,pts): a.co=(*xyz(p),1)
    o=bpy.data.objects.new(name,c); scene.collection.objects.link(o); bpy.context.view_layer.objects.active=o
    bpy.ops.object.select_all(action='DESELECT'); o.select_set(True); bpy.ops.object.convert(target='MESH'); o=bpy.context.object; return finish(o,name,role)
def capsule(name,a,b,r,role): return tube(name,[a,b],r,role)
def rivets(prefix,center,radius,y,count,role='Metal',size=.07,axis='y'):
    for i in range(count):
        t=i/count*2*math.pi
        if axis=='y': ball(prefix,(center[0]+radius*math.cos(t),y,center[2]+radius*math.sin(t)),(size,size,size),role,12)
def peel(prefix,origin,normal_axis,count,area,role='Dust'):
    # flaking paint patches: thin irregular discs a hair off the surface
    for i in range(count):
        u=random.uniform(-area[0],area[0]); v=random.uniform(-area[1],area[1]); s=random.uniform(.12,.32)
        if normal_axis=='-z': box(prefix,(origin[0]+u,origin[1]+v,origin[2]-.012),(s,s*.7,.02),role,.008)
manifest=[]
def export(name,note=''):
    joined=[]
    for role in palette:
        batch=[o for o in scene.objects if o.type=='MESH' and o.name.startswith('FI_') and o not in joined and o.data.materials and o.data.materials[0]==mats[role]]
        if not batch: continue
        bpy.ops.object.select_all(action='DESELECT')
        for o in batch: o.select_set(True)
        bpy.context.view_layer.objects.active=batch[0]; bpy.ops.object.join(); j=bpy.context.object; j.name=name+'__'+role
        scene.cursor.location=(0,0,0); bpy.ops.object.origin_set(type='ORIGIN_CURSOR'); joined.append(j)
    bpy.ops.object.select_all(action='DESELECT')
    for o in joined: o.select_set(True)
    bpy.ops.export_scene.fbx(filepath=OUT+'/'+name+'.fbx',use_selection=True,object_types={'MESH'},apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',
                             axis_forward='-Z',axis_up='Y',bake_space_transform=True,mesh_smooth_type='FACE',add_leaf_bones=False,bake_anim=False)
    tris=sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in joined)
    xs=[v.co for o in joined for v in o.data.vertices]
    size=(max(v.x for v in xs)-min(v.x for v in xs),max(v.z for v in xs)-min(v.z for v in xs),max(v.y for v in xs)-min(v.y for v in xs))
    manifest.append({'name':name,'triangles':tris,'materials':[o.name.split('__')[1] for o in joined],'size_xyz':[round(a,2) for a in size],'note':note})
    # keep the built model in the .blend, moved aside on a showcase line
    off=Vector((len(manifest)*9.0,0,0))
    for o in joined: o.location+=off; o.name='KIT_'+o.name

# ======================= LANDMARKS =======================
# Big Toy Press - central hall landmark (front = -z, toward the receiving hall). Eyes stay in Unity (they light up).
cyl('PressBase',(0,.6,0),2.8,1.2,'Teal',bev=.25)
ring('PressBaseRim',(0,1.2,0),2.75,.12,'Cream')
cyl('PressBody',(0,3.3,0),2.1,4.2,'Cream',bev=.2)
for y in (1.6,5.0): ring('PressBand',(0,y,0),2.12,.09,'Teal')
ball('PressDome',(0,5.4,0),(2.1,1.25,2.1),'Teal')
cyl('PressLedge',(0,6.62,0),1.6,.25,'Teal',bev=.08)
for i,(x,y) in enumerate([(-.9,2.3),(.6,1.9),(1.3,3.4),(-1.2,3.9)]): box('PressPeel',(x,y,-2.08),(random.uniform(.25,.5),random.uniform(.2,.35),.02),'Dust',.01)
box('PressMouthPlate',(0,2.6,-2.06),(1.9,.6,.06),'Teal',.03)
for s in (-1,1):
    capsule('PressArm',(s*2.05,3.9,-.4),(s*2.9,2.6,-.9),.42,'Coral')
    ball('PressHand',(s*2.95,2.35,-.95),(.55,.5,.55),'Coral')
    cyl('PressShoulder',(s*2.05,3.9,-.4),.5,.35,'Teal',axis='x',bev=.06)
for z,s in [(-.6,1),(.8,1)]:
    cyl('PressGear',(2.12,4.4,z),.55,.18,'Yellow',axis='x',bev=.03,verts=12)
cyl('PressChimney',(1.1,6.9,.9),.25,.9,'Metal',bev=.04)
ring('PressChimneyRim',(1.1,7.35,.9),.27,.05,'Teal')
box('PressNameplate',(0,1.35,-2.6),(1.8,.4,.08),'Yellow',.03)
export('FI_BigToyPress','origin = floor centre; front faces -z; eyes are separate in Unity')

# Giant Boiler - boiler room landmark (front = -x, facing the central hall side)
cyl('BoilerFoot',(0,.25,0),2.4,.5,'Dark',bev=.12)
cyl('BoilerShell',(0,3.3,0),2.2,5.7,'Coral',bev=.25)
for y in (1.2,3.2,5.2): ring('BoilerBand',(0,y,0),2.22,.1,'Teal')
ball('BoilerDome',(0,6.2,0),(2.2,1.0,2.2),'Teal')
cyl('BoilerSteamCap',(0,7.25,0),.35,.5,'Metal',bev=.06)
cyl('BoilerFirebox',(-2.15,1.0,0),.75,.12,'Dark',axis='x',bev=.04)
ring('BoilerFireboxRim',(-2.2,1.0,0),.75,.07,'Metal',axis='x')
cyl('BoilerGauge',(-2.2,3.6,-.8),.38,.1,'Cream',axis='x',bev=.03); box('BoilerNeedle',(-2.27,3.65,-.8),(.02,.28,.04),'Coral',.005)
ring('BoilerValve',(-2.35,2.4,.9),.42,.06,'Yellow',axis='x')
tube('BoilerPipe',[(0,7.4,0),(0,7.8,0),(-1.6,7.8,0),(-1.6,7.95,1.8)],.18,'Metal')
for i in range(5): box('BoilerDust',(random.uniform(-1.5,1.5),.52,random.uniform(-2.6,-2.3)),(random.uniform(.3,.6),.04,random.uniform(.2,.4)),'Dust',.01)
export('FI_GiantBoiler','origin = floor centre, r 2.2, h 7.5')

# Round vent turbine housing (axis = z) and rotor (axis = z, centred on origin, blades in the xy plane)
ring('TurbineRing',(0,0,0),2.35,.28,'Teal',axis='z')
ring('TurbineInnerRing',(0,0,.15),2.0,.07,'Cream',axis='z')
for a in range(0,360,45): box('TurbineBolt',(2.35*math.cos(math.radians(a)),2.35*math.sin(math.radians(a)),-.3),(.16,.16,.12),'Metal',.04)
box('TurbineBracket',(0,-2.6,.2),(1.4,.5,.6),'Dark',.1)
export('FI_TurbineHousing','origin = turbine centre; axis z')
cyl('RotorHub',(0,0,0),.45,.5,'Coral',axis='z',bev=.12)
ball('RotorNose',(0,0,-.3),(.38,.38,.3),'Cream')
for i in range(5):
    a=i*2*math.pi/5; c=Vector((math.sin(a)*1.1,math.cos(a)*1.1,0))
    box('RotorBlade',(c.x,c.y,0),(.62,1.9,.1),'Coral',.12,rot=(0,0,math.degrees(a)))   # long axis points out from the hub
export('FI_TurbineRotor','origin = hub centre; blades in xy plane; spins about z')

# Overhead crane (crane room). Origin = crane room floor centre (unity 13.1,0,45).
for x in (-6.1,6.1): box('CraneRail',(x,6.6,0),(.45,.45,12),'Teal',.08)
box('CraneBridge',(0,6.25,1),(12.4,.7,1.2),'Coral',.18)
for x in (-6.1,6.1): box('CraneTruck',(x,6.25,1),(.8,.9,1.6),'Dark',.12)
box('CraneTrolley',(.5,5.55,1),(1.4,.8,1.3),'Yellow',.2)
tube('CraneCable',[(.5,5.15,1),(.5,3.7,1)],.035,'Dark')
capsule('CraneHookTop',(.5,3.7,1),(.5,3.35,1),.18,'Yellow')
tube('CraneHook',[(.5,3.3,1)]+[(.5+.32*math.sin(t),3.0-.32*math.cos(t)+.0,1) for t in [i*math.pi/10 for i in range(0,15)]],.07,'Yellow')
box('CraneSign',(0,6.25,.38),(2.2,.4,.04),'Cream',.02)
export('FI_OverheadCrane','origin = crane room floor centre (unity 13.1,0,45)')

# Parts cabinet (assembly vault). Origin = bottom centre; front = +x (faces the room). Doors exported separately.
box('CabinetBody',(0,1.7,0),(2,3.4,4.4),'Teal',.18)
box('CabinetPlinth',(0,.12,0),(2.2,.24,4.6),'Dark',.06)
box('CabinetCrown',(0,3.48,0),(2.2,.22,4.6),'Cream',.06)
for z in (-1.1,1.1): box('CabinetFrame',(1.02,1.7,z),(.06,3.0,2.1),'Dark',.02)
box('CabinetDriveSlot',(1.02,.3,1.9),(.08,.3,.8),'Dark',.02)
box('CabinetPeel',(1.03,2.6,-1.6),(.02,.4,.6),'Dust',.008)
cyl('CabinetRodClamp',(0,5.6,0),.2,.25,'Metal',bev=.04)
export('FI_PartsCabinet','origin = bottom centre; front faces +x')
box('CabinetDoor',(0,0,0),(.12,3.2,2.1),'Cream',.05)
cyl('CabinetDoorHandle',(.09,0,.7),.08,.3,'Yellow',bev=.03)
for y in (-1.1,1.1): box('CabinetHinge',(.08,y,-.98),(.06,.25,.12),'Metal',.02)
box('CabinetDoorWindow',(.07,.7,0),(.02,.6,1.2),'Coal',.02)
export('FI_CabinetDoor','origin = door centre; 0.12 x 3.2 x 2.1')

# Assembly furnace (landmark of the vault room)
cyl('FurnaceBody',(0,1.2,0),1.2,2.4,'Coral',bev=.3)
ball('FurnaceTop',(0,2.4,0),(1.2,.55,1.2),'Teal')
cyl('FurnaceChimney',(0,3.3,0),.3,1.4,'Metal',bev=.05)
cyl('FurnaceWindow',(0,1.4,-1.18),.42,.08,'Glow',axis='z',bev=.03)
ring('FurnaceWindowRim',(0,1.4,-1.2),.45,.06,'Dark',axis='z')
for a in range(0,360,60): cyl('FurnaceFoot',(1.0*math.cos(math.radians(a)),.08,1.0*math.sin(math.radians(a))),.18,.16,'Dark',bev=.04)
export('FI_Furnace','origin = floor centre')

# ======================= PUZZLE PROPS =======================
# Sorting chutes frame (P1). Origin = floor, centre of the chute row (unity 0,0,40). 4 lanes x -3.25..3.25, depth 3.
for x in (-3.4,-1.6,0,1.6,3.4): box('ChuteDivider',(x,1.1,0),(.32,2.2,3.1),'Teal',.12)
box('ChuteRoof',(0,2.38,0),(7.1,.32,3.2),'Teal',.12)
box('ChuteHopper',(0,3.3,.4),(6.8,1.4,2.2),'Cream',.25,rot=(-15,0,0))
for i,x in enumerate((-2.4,-.8,.8,2.4)):
    box('ChuteLip',(x,2.15,-1.52),(1.25,.14,.1),['Coral','Yellow','Coral','Yellow'][i],.04)
for x in (-3.4,3.4): cyl('ChuteBolt',(x,1.6,-1.57),.1,.05,'Metal',axis='z',bev=.02)
box('ChuteRust',(-2.0,2.38,-1.58),(.6,.25,.02),'Rust',.01)
export('FI_SortingChutes','origin = floor centre of the 4 lanes (front = -z)')

# Safety (vault) door 3 x 4 with a big wheel; origin = door centre; front = -z
box('SafetyDoorSlab',(0,0,0),(3,4,.45),'Coral',.25)
ring('SafetyDoorRim',(0,.2,-.25),1.0,.08,'Teal',axis='z')
ring('SafetyWheel',(0,.2,-.38),.6,.07,'Yellow',axis='z')
for a in (0,60,120): box('SafetySpoke',(0,.2,-.38),(1.2,.08,.08),'Yellow',.03,rot=(0,0,a))
cyl('SafetyHub',(0,.2,-.4),.14,.12,'Metal',axis='z',bev=.03)
for i in range(6): box('SafetyStripe',(-1.2+i*.48,-1.75,-.24),(.22,.3,.02),'Dark' if i%2 else 'Yellow',.01)
export('FI_SafetyDoor','origin = door centre; 3 x 4 x 0.45')

# Small hatch (rust hatch / wax crawl hatch); origin = centre; 1.6 x 2.2
box('HatchSlab',(0,0,0),(1.6,2.2,.25),'Teal',.12)
for y in (-.7,0,.7): box('HatchRib',(0,y,-.14),(1.3,.1,.04),'Cream',.02)
cyl('HatchHandle',(.55,0,-.17),.06,.28,'Metal',bev=.02)
export('FI_Hatch','origin = centre; 1.6 x 2.2 x 0.25')
ball('BoltHead',(0,0,0),(.2,.2,.14),'Rust'); cyl('BoltShaft',(0,0,.12),.08,.2,'Rust',axis='z',bev=.02)
for i in range(4): ball('RustFlake',(random.uniform(-.15,.15),random.uniform(-.15,.15),-.1),(.05,.05,.03),'Orange',8)
export('FI_RustBolt','origin = centre')
ball('WaxSealBlob',(0,0,0),(.18,.26,.13),'Cream'); ball('WaxDrip',(.02,-.24,-.02),(.07,.1,.06),'Cream',12); ball('WaxStamp',(0,0,-.11),(.11,.11,.03),'Coral',12)
export('FI_WaxSeal','origin = centre')

# Cargo lift platform 2.6 x 2.6 (origin = centre, top at +0.15) and the round-cornered raft 2.2 x 0.4 x 2.2
box('LiftDeck',(0,0,0),(2.6,.3,2.6),'Yellow',.12)
for i in range(4): box('LiftStripe',(-1.3+.15,.16,-.9+i*.6),(.25,.02,.3),'Dark',.01)
for x in (-1.15,1.15):
    for z in (-1.15,1.15): cyl('LiftCorner',(x,.2,z),.1,.12,'Metal',bev=.03)
export('FI_LiftDeck','origin = centre; 2.6 x 0.3 x 2.6')
box('RaftHull',(0,0,0),(2.2,.4,2.2),'Coral',.18)
for x in (-.8,0,.8): box('RaftPlank',(x,.21,0),(.6,.04,2.0),'Rust',.02)
for x in (-1,1):
    for z in (-1,1): ball('RaftBuoy',(x,-.05,z),(.22,.22,.22),'Cream',16)
ring('RaftRing',(0,.24,-.95),.18,.04,'Yellow',axis='z')
export('FI_Raft','origin = centre; 2.2 x 0.4 x 2.2')
box('BridgeDeck',(0,0,0),(2.2,.3,9.4),'Yellow',.1)
for s in (-1,1): box('BridgeRail',(s*1.05,.55,0),(.08,.08,9.2),'Teal',.03)
for z in range(-4,5,2):
    for s in (-1,1): box('BridgePost',(s*1.05,.3,z),(.08,.6,.08),'Dark',.02)
export('FI_ServiceBridge','origin = centre; 2.2 x 0.3 x 9.4 (rails above)')

# Motor housing (generic toy motor for lift / bridge / far turbine motor). Origin = bottom centre, 0.9 x 1.1 x 0.9
box('MotorBox',(0,.5,0),(.9,1.0,.9),'Teal',.2)
cyl('MotorCoil',(0,.65,0),.32,.95,'Coral',axis='x',bev=.08)
cyl('MotorLamp',(0,1.06,0),.12,.1,'Glow',bev=.03)
box('MotorFeet',(0,.04,0),(1.0,.08,1.0),'Dark',.03)
box('MotorBolt',(0,.6,-.47),(.2,.36,.04),'Yellow',.02)
export('FI_Motor','origin = bottom centre; 0.9 x 1.1 x 0.9')

# ======================= MOVABLE JUNK (P1 chutes, P2, P6) =======================
box('ScrapPlate',(0,0,0),(1.1,.55,1.0),'Metal',.1)
box('ScrapDent',(.2,.28,-.1),(.4,.03,.35),'Rust',.02)
for i in range(3): cyl('ScrapRivet',(-.4+i*.4,.28,.35),.05,.03,'Dark',bev=.01)
export('FI_ScrapPlate','origin = centre; 1.1 x 0.55 x 1.0 (4 kg)')
box('FoamBox',(0,0,0),(1.05,.65,1.0),'Coral',.14)
for x in (-.18,.18): ball('FoamEye',(x,.06,-.5),(.08,.08,.03),'Dark',12)
box('FoamSmile',(0,-.12,-.505),(.22,.04,.02),'Dark',.01)
box('FoamTape',(0,.33,0),(.2,.02,1.0),'Cream',.005)
export('FI_FoamToyBox','origin = centre; 1.05 x 0.65 x 1.0 (1 kg)')
box('HeavyCrate',(0,0,0),(1.1,1.0,1.6),'Blue',.12)
for z in (-.4,.4): box('CrateGrip',(.56,0,z),(.04,.18,.35),'Dark',.02)
for y in (-.25,.25): box('CrateRib',(0,y,-.81),(1.0,.1,.04),'Dark',.02)
box('CrateWeight',(0,.15,-.83),(.5,.22,.02),'Yellow',.01)
export('FI_HeavyCrate','origin = centre; 1.1 x 1.0 x 1.6 (16 kg)')
box('CardBundle',(0,0,0),(1.1,.62,1.2),'Orange',.06)
for x in (-.3,.3): box('CardString',(x,.0,0),(.03,.64,1.22),'Cream',.01)
box('CardLabel',(0,.1,-.61),(.4,.25,.02),'White',.01)
export('FI_CardboardBundle','origin = centre; 1.1 x 0.62 x 1.2 (14 kg, burns)')
ball('WeightBall',(0,0,0),(.5,.5,.5),'Metal'); ring('WeightHandle',(0,.45,0),.22,.05,'Dark',axis='z'); box('WeightLabel',(0,0,-.49),(.3,.18,.02),'Yellow',.01)
export('FI_Counterweight','origin = centre; diameter 1 (6 kg)')
box('OldMachine',(0,0,0),(1.3,1.5,1.3),'Teal',.25); cyl('OldMachineDial',(0,.25,-.66),.25,.06,'Cream',axis='z',bev=.02)
for x in (-.35,.35): box('OldMachineButton',(x,-.3,-.66),(.18,.18,.06),'Coral',.04)
box('OldMachineDust',(0,.76,0),(1.0,.03,1.0),'Dust',.01)
export('FI_OldPressMachine','origin = centre; 1.3 x 1.5 x 1.3 (30 kg)')

# ======================= DECOR KIT (old, sleepy toy factory) =======================
cyl('PillarShaft',(0,4,0),.38,8,'Cream',bev=.1); ring('PillarBandLow',(0,.7,0),.4,.07,'Teal'); ring('PillarBandHigh',(0,7.3,0),.4,.07,'Teal')
cyl('PillarFoot',(0,.15,0),.5,.3,'Teal',bev=.08)
box('PillarPeel',(0,3.4,-.38),(.25,.5,.02),'Dust',.01)
export('FI_Pillar','origin = floor; height 8 (floor to roof)')
cyl('PipeRun',(0,0,0),.18,6,'Metal',axis='x',bev=.03)
for x in (-2.8,0,2.8): cyl('PipeFlange',(x,0,0),.26,.12,'Teal',axis='x',bev=.03)
ball('PipeDrip',(1.4,-.3,0),(.06,.09,.06),'Water',10); ball('PipeDrip2',(1.4,-.75,0),(.05,.07,.05),'Water',10)
box('PipeRust',(-1.2,.17,0),(.6,.03,.2),'Rust',.01)
export('FI_PipeRun','origin = pipe centre; 6 m along x, a drip at +1.4')
for i in range(7):
    a=random.uniform(0,6.28); r=random.uniform(0,.45)
    ball('Sprout',(r*math.cos(a),.12,r*math.sin(a)),(random.uniform(.07,.12),random.uniform(.12,.2),random.uniform(.07,.12)),'Leaf',12)
box('CrackA',(0,.005,0),(.9,.01,.06),'Coal',.003,rot=(0,25,0)); box('CrackB',(.3,.005,.15),(.5,.01,.05),'Coal',.003,rot=(0,-30,0))
export('FI_FloorSprouts','origin = floor; little plants in a floor crack')
for i in range(6): ball('DustLump',(random.uniform(-.6,.6),.03,random.uniform(-.4,.4)),(random.uniform(.2,.45),.05,random.uniform(.15,.35)),'Dust',12)
export('FI_DustPile','origin = floor')
ball('ToyBearBody',(0,.35,0),(.32,.36,.28),'Coral'); ball('ToyBearHead',(0,.82,0),(.25,.23,.22),'Coral')
for s in (-1,1):
    ball('ToyBearEar',(s*.17,1.02,0),(.08,.08,.06),'Coral',12); ball('ToyBearArm',(s*.33,.42,-.05),(.1,.18,.1),'Coral',12)
for s in (-1,1): ball('ToyBearEye',(s*.08,.86,-.2),(.035,.035,.02),'Dark',8)
ball('ToyBearPatch',(.1,.3,-.26),(.1,.1,.02),'Cream',10)
export('FI_BrokenToyBear','origin = floor; sitting plush 1 m')
box('CrateStackA',(0,.4,0),(.9,.8,.9),'Dust',.1); box('CrateStackB',(.15,1.15,.05),(.75,.7,.75),'Cream',.1); box('CrateStackTag',(0,.5,-.46),(.3,.2,.02),'Teal',.01)
export('FI_OldCrates','origin = floor')
cyl('LampShade',(0,0,0),.45,.28,'Teal',bev=.08,r2=.3); ball('LampBulb',(0,-.18,0),(.2,.2,.2),'Glow',16); cyl('LampCord',(0,.6,0),.03,1.0,'Dark',bev=0)
export('FI_CeilingLamp','origin = shade centre; cord goes up 1 m')

bpy.ops.wm.save_as_mainfile(filepath=ROOT+'/ArtSource/Blender/OOPS_FactoryInterior.blend')
json.dump(manifest,open(OUT+'/FactoryInterior_manifest.json','w'),indent=1)
print('FACTORY_INTERIOR_ART', len(manifest), 'models ->', OUT)
