import bpy, math, os, json, random
from mathutils import Vector
ROOT='E:/!for WORK/AFO'
OUT=ROOT+'/Assets/Scenes/OopsPlanet/Models/WorldPolish'
os.makedirs(OUT,exist_ok=True)
scene=bpy.data.scenes.new('OOPS_WORLD_POLISH');bpy.context.window.scene=scene
random.seed(821)
palette={'Cream':'FFF0CE','White':'FDF8ED','Teal':'579B9C','Dark':'294A60','Metal':'9BB9BE','Coral':'EB997B','Blue':'78B4D4','Pink':'E6A2BB','Water':'50C8CE','Yellow':'F5D163','Orange':'EE9251','Leaf':'83AA78','Grass':'A5C97E','Path':'DCC7A5','Coal':'48515F','DryLeaf':'BDC57B'}
mats={}
for role,h in palette.items():
 m=bpy.data.materials.new('POL_'+role);m.use_nodes=True
 c=tuple(int(h[i:i+2],16)/255 for i in (0,2,4));m.diffuse_color=(*c,1)
 n=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED');n.inputs['Base Color'].default_value=(*c,1);n.inputs['Roughness'].default_value=.48
 if role=='Metal':n.inputs['Metallic'].default_value=.28
 mats[role]=m
group=[]; exports=[]
def xyz(p):return (-p[0],-p[2],p[1])
def finish(o,name,role):
 o.name='POL_'+name+'__'+role;o.data.materials.clear();o.data.materials.append(mats[role])
 for p in o.data.polygons:p.use_smooth=True
 group.append(o);return o
def ball(name,p,size,role):
 bpy.ops.mesh.primitive_uv_sphere_add(segments=24,ring_count=12,location=xyz(p));o=bpy.context.object;o.scale=(size[0],size[2],size[1]);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);return finish(o,name,role)
def box(name,p,size,role,r=.09):
 bpy.ops.mesh.primitive_cube_add(size=1,location=xyz(p));o=bpy.context.object;o.scale=(size[0],size[2],size[1]);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 b=o.modifiers.new('Soft edges','BEVEL');b.width=min(r,min(size)*.35);b.segments=4;bpy.ops.object.modifier_apply(modifier=b.name)
 w=o.modifiers.new('Weighted normals','WEIGHTED_NORMAL');bpy.ops.object.modifier_apply(modifier=w.name);return finish(o,name,role)
def cyl(name,p,r,h,role):
 bpy.ops.mesh.primitive_cylinder_add(vertices=32,radius=r,depth=h,location=xyz(p));o=bpy.context.object
 b=o.modifiers.new('Rounded rim','BEVEL');b.width=min(.045,h*.25);b.segments=3;bpy.ops.object.modifier_apply(modifier=b.name);return finish(o,name,role)
def ring(name,p,r,t,role,vertical=False):
 bpy.ops.mesh.primitive_torus_add(major_segments=36,minor_segments=10,location=xyz(p),major_radius=r,minor_radius=t);o=bpy.context.object
 if vertical:o.rotation_euler.x=math.pi/2;bpy.ops.object.transform_apply(location=False,rotation=True,scale=False)
 return finish(o,name,role)
def tube(name,points,r,role):
 c=bpy.data.curves.new(name,'CURVE');c.dimensions='3D';c.resolution_u=2;c.bevel_depth=r;c.bevel_resolution=3
 sp=c.splines.new('POLY');sp.points.add(len(points)-1)
 for a,p in zip(sp.points,points):a.co=(*xyz(p),1)
 o=bpy.data.objects.new(name,c);scene.collection.objects.link(o);bpy.context.view_layer.objects.active=o;o.select_set(True);bpy.ops.object.convert(target='MESH');o=bpy.context.object;o.select_set(False);return finish(o,name,role)
def bolt(p,scale=1):
 pts=[(-.02,.24),(.14,.24),(.01,.04),(.15,.04),(-.13,-.25),(-.04,-.04),(-.15,-.04)]
 verts=[]
 for depth in [-.025,.025]:
  verts.extend(xyz((p[0]+x*scale,p[1]+y*scale,p[2]+depth)) for x,y in pts)
 k=len(pts);faces=[tuple(range(k-1,-1,-1)),tuple(range(k,k*2))]+[(i,(i+1)%k,(i+1)%k+k,i+k) for i in range(k)]
 mesh=bpy.data.meshes.new('Lightning');mesh.from_pydata(verts,[],faces);mesh.update();o=bpy.data.objects.new('Lightning',mesh);scene.collection.objects.link(o)
 b=o.modifiers.new('Soft lightning','BEVEL');b.width=.018;b.segments=3;bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=b.name);return finish(o,'Lightning','Yellow')
def select(obs):
 bpy.ops.object.select_all(action='DESELECT')
 for o in obs:o.select_set(True)
 if obs:bpy.context.view_layer.objects.active=obs[0]
def export(name,display=(0,0,0)):
 global group
 # Batch by role inside each reusable model.
 joined=[]
 for role in palette:
  batch=[o for o in list(scene.objects) if o.type=='MESH' and o.name.startswith('POL_') and o.data.materials and o.data.materials[0]==mats[role]]
  if not batch:continue
  select(batch);bpy.ops.object.join();o=bpy.context.object;o.name=name+'__'+role
  scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR');joined.append(o)
 select(joined)
 bpy.ops.export_scene.fbx(filepath=OUT+'/'+name+'.fbx',use_selection=True,object_types={'MESH'},apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',bake_space_transform=True,mesh_smooth_type='FACE',add_leaf_bones=False,bake_anim=False)
 tris=sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in joined)
 exports.append({'name':name,'triangles':tris,'batches':len(joined)})
 for o in joined:o.location=Vector(xyz(display))
 group=[]

# The six absorbables use the existing objects as references, with readable silhouettes and material accents.
box('Scrap tray',(0,.08,0),(.85,.15,.64),'Dark')
for x,y,z,s in [(-.22,.2,0,.22),(.2,.18,.12,.2),(0,.2,-.18,.18)]:box('Scrap block',(x,y,z),(s,s,s),'Metal',.05)
tube('Magnet horseshoe',[(math.cos(a)*.27,.65-math.sin(a)*.28,-.04) for a in [i*math.pi/32 for i in range(33)]],.09,'Coral')
for x in [-.27,.27]:box('Magnet tips',(x,.68,-.04),(.18,.18,.2),'White',.04)
export('Source_Magnet',(-4,0,0))

cyl('Tank foot',(0,.1,0),.32,.18,'Dark');cyl('Pressure tank',(0,.6,0),.32,.88,'Blue')
ball('Tank dome',(0,1.02,0),(.32,.16,.32),'Blue');ring('Tank collar',(0,.35,0),.32,.035,'Cream')
cyl('Valve stem',(0,1.16,0),.055,.18,'Metal');ring('Valve wheel',(0,1.27,0),.17,.035,'Yellow')
for x in [-.1,0,.1]:tube('Air mark',[(x-.05,.55,-.325),(x+.04,.6,-.325),(x-.05,.65,-.325)],.018,'White')
export('Source_Pressure',(-2,0,0))

ring('Tyre',(0,.14,0),.32,.13,'Dark');ring('Tyre rim',(0,.14,0),.2,.035,'Metal')
tube('Spring',[(.19*math.cos(i*math.pi/16),.23+i/96*.55,.19*math.sin(i*math.pi/16)) for i in range(97)],.035,'Metal')
cyl('Bounce top',(0,.82,0),.31,.12,'Pink');ring('Bounce top rim',(0,.85,0),.28,.025,'Cream')
export('Source_Bounce',(0,0,0))

cyl('Water barrel',(0,.45,0),.4,.74,'Cream')
for y in [.15,.72]:ring('Barrel hoop',(0,y,0),.4,.045,'Teal')
cyl('Open water',(0,.84,0),.35,.035,'Water');ring('Water ripple',(0,.865,0),.2,.012,'White')
ball('Water drop',(0,.45,-.41),(.11,.16,.04),'Water')
for x in [-.44,.44]:ring('Barrel handle',(x,.62,0),.11,.03,'Metal',True)
export('Source_Water',(2,0,0))

box('Battery casing',(0,.45,0),(.62,.8,.45),'Yellow',.12)
box('Battery face',(0,.47,-.235),(.43,.56,.045),'Dark',.045)
for x,role in [(-.18,'Coral'),(.18,'Metal')]:cyl('Battery terminal',(x,.92,0),.09,.16,role)
bolt((0,.48,-.28),.7)
for x in [-.2,.2]:box('Battery foot',(x,.06,0),(.2,.1,.48),'Dark',.035)
export('Source_Electric',(4,0,0))

for a in [0,2.1,4.2]:ball('Brazier leg',(math.cos(a)*.28,.14,math.sin(a)*.28),(.075,.17,.075),'Metal')
ball('Coal bowl',(0,.36,0),(.44,.18,.4),'Dark');ring('Brazier lip',(0,.42,0),.38,.05,'Metal')
for x,z in [(-.18,0),(.12,.12),(.16,-.14),(0,-.12)]:ball('Hot coal',(x,.48,z),(.16,.1,.13),'Coal')
for x,z,h in [(-.17,.02,.25),(.12,.08,.3),(0,-.08,.45)]:ball('Soft flame',(x,.52+h*.45,z),(.11,h,.11),'Orange');ball('Flame core',(x,.53+h*.25,z-.015),(.07,h*.6,.08),'Yellow')
export('Source_Heat',(6,0,0))

# Machinery: rounded housings, readable pressure gauges, pipework and service panels.
box('Pump base',(0,.12,0),(1.4,.24,1),'Dark',.1)
box('Pump body',(-.25,.7,0),(.8,1.0,.85),'Teal',.16)
cyl('Motor barrel',(.4,.52,0),.32,.75,'Metal');ball('Pump dome',(-.25,1.2,0),(.4,.3,.42),'Cream')
ring('Pump valve',(-.27,.77,-.48),.22,.04,'Coral',True)
tube('Pump outlet',[(.42,.82,0),(.75,1.0,0),(.8,1.35,0)],.12,'Metal')
ball('Gauge',(-.25,1.17,-.42),(.18,.18,.05),'White');tube('Gauge needle',[(-.25,1.17,-.48),(-.17,1.23,-.48)],.014,'Dark')
bolt((.4,.59,-.34),.4);export('Machine_Pump',(-3,0,3))

box('Boiler feet',(0,.1,0),(1.5,.2,1.2),'Dark',.1)
cyl('Boiler vessel',(0,.9,0),.62,1.3,'Coral');ball('Boiler dome',(0,1.56,0),(.62,.25,.62),'Cream')
for y in [.44,1.3]:ring('Boiler band',(0,y,0),.63,.045,'Metal')
ring('Boiler hatch',(0,.8,-.62),.28,.055,'Metal',True);ball('Boiler hatch glass',(0,.8,-.64),(.23,.23,.035),'Dark')
tube('Boiler elbow',[(.35,1.7,0),(.35,1.95,0),(.85,1.95,0)],.13,'Metal')
ball('Boiler gauge',(-.3,1.3,-.6),(.17,.17,.05),'White');tube('Boiler pointer',[(-.3,1.3,-.66),(-.37,1.38,-.66)],.014,'Dark')
export('Machine_Boiler',(-1,0,3))

ring('Turbine outer frame',(0,0,.12),1.45,.13,'Cream',True)
ring('Turbine inner rim',(0,0,.14),1.22,.035,'Metal',True)
for x in [-1.25,1.25]:box('Turbine bolt',(x,0,.12),(.22,.22,.2),'Coral',.08)
export('Machine_Turbine',(2,1.5,3))

# A repaired retro capsule echoes the old ship silhouette while keeping the same footprint.
ball('Ship fuselage',(0,1.45,0),(2.1,1.32,2.9),'Cream')
ball('Ship nose',(0,1.25,2.35),(1.55,1.03,1.5),'Coral')
for x in [-2.1,2.1]:
 box('Ship soft fin',(x,.7,-.5),(1.4,.36,2.8),'Teal',.16)
 ball('Engine pod',(x,.8,-1.8),(.55,.6,1.1),'Coral')
 ring('Engine rim',(x,.8,-2.6),.4,.08,'Metal',True)
 ball('Landing foot',(x,.18,.25),(.42,.2,.65),'Dark')
ring('Porthole rim',(0,1.7,-2.57),.67,.12,'Metal',True)
ball('Porthole glass',(0,1.7,-2.65),(.57,.57,.18),'Blue')
ball('Window glint',(-.2,1.9,-2.82),(.14,.12,.025),'White')
tube('Ship aerial',[(.6,2.5,.1),(.8,3.2,.1)],.055,'Metal');ball('Aerial tip',(.8,3.25,.1),(.17,.17,.17),'Water')
for x in [-1.0,0,1.0]:ball('Hull rivet',(x,.8,-2.65),(.07,.07,.05),'Yellow')
export('Ship_Capsule',(0,0,8))

# A whole rice plot is batched so watering can recolour its seedlings without dozens of draw calls.
box('Paddy soil',(0,.04,0),(3.25,.08,4.4),'Path',.035)
for x in [-1,0,1]:
 for z in [-1.5,0,1.5]:
  for dx,dz in [(-.11,0),(.11,.04),(0,-.09)]:
   leaf=ball('Rice seedling',(x+dx,.33,z+dz),(.065,.3,.055),'DryLeaf')
   leaf.rotation_euler.y=dx*2
export('Rice_Plot',(5,0,4))
# World-coordinate accents: clear entrances and destination rings, all outside collision paths.
ring('Ship landing ring',(8,.07,6),4.5,.075,'Cream')
for x in [-3.65,3.65]:
 box('Workshop arch pillar',(x,2.5,29.5),(.7,5,1),'Coral',.22)
 ball('Workshop arch cap',(x,5.1,29.5),(.65,.5,.65),'Cream')
box('Workshop arch lintel',(0,5.5,29.5),(8,.8,1.1),'Cream',.24)
for z in [-3.65,3.65]:
 box('Rice arch pillar',(31,2.1,z),(.85,4.2,.7),'Teal',.2)
 ball('Rice arch cap',(31,4.4,z),(.6,.4,.6),'Cream')
box('Rice arch lintel',(31,4.8,0),(1,.65,8),'Cream',.2)
for x,z in [(-3,18),(3,23),(-5,32),(5,32),(35,-3),(35,3),(49,-19),(70,20),(-18,46),(18,47)]:
 for dx in [-.45,.45]:
  ball('Route flower',(x+dx,.2,z),(.23,.1,.23),'Pink' if z<25 else 'Yellow')
  ball('Flower center',(x+dx,.3,z),(.07,.04,.07),'Cream')
for x,z in [(0,19),(0,22),(0,25),(23,0),(26,0),(29,0)]:
 ball('Route stone',(x,.045,z),(.7,.05,.58),'Cream')
export('World_Accents')

json.dump({'palette':palette,'exports':exports},open(OUT+'/WorldPolish_manifest.json','w',encoding='utf-8'),ensure_ascii=False,indent=2)
world=bpy.data.worlds.new('Polish daylight');world.use_nodes=True;bg=next(n for n in world.node_tree.nodes if n.type=='BACKGROUND');bg.inputs[0].default_value=(.62,.78,.85,1);bg.inputs[1].default_value=.6;scene.world=world
ld=bpy.data.lights.new('Polish key','AREA');ld.energy=1800;ld.shape='DISK';ld.size=12;l=bpy.data.objects.new('Polish key',ld);scene.collection.objects.link(l);l.location=(2,-6,14)
scene.render.engine='CYCLES';scene.cycles.samples=24
bpy.ops.wm.save_as_mainfile(filepath=ROOT+'/ArtSource/Blender/OOPS_WorldPolish.blend')
print(json.dumps({'exported':exports,'triangles':sum(x['triangles'] for x in exports),'source':bpy.data.filepath},ensure_ascii=False))
