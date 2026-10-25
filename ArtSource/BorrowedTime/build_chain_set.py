import bpy, math
from mathutils import Vector, Quaternion
scene=bpy.data.scenes.new('BT_CausalCityMechanism')
bpy.context.window.scene=scene
col=bpy.data.collections.new('CausalCity_Parts');scene.collection.children.link(col)
palette={'CH_Stone':(.68,.64,.48,1),'CH_Iron':(.13,.22,.24,1),'CH_Brass':(.66,.34,.12,1),'CH_Wood':(.43,.26,.12,1),'CH_WoodLight':(.62,.43,.22,1),'CH_Blue':(.12,.36,.43,1),'CH_Steel':(.44,.5,.49,1),'CH_Dark':(.04,.065,.07,1)}
mats={}
for name,c in palette.items():
 m=bpy.data.materials.new(name);m.diffuse_color=c;m.use_nodes=True
 p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=c;p.inputs['Roughness'].default_value=.55;p.inputs['Metallic'].default_value=.35 if name not in ['CH_Wood','CH_WoodLight','CH_Stone'] else 0;mats[name]=m
def point(v):return Vector((v[0],-v[2],v[1]))
def empty(n):
 o=bpy.data.objects.new(n,None);col.objects.link(o);return o
def reg(o,n,parent,mat):
 o.name=n
 for c in list(o.users_collection):c.objects.unlink(o)
 col.objects.link(o);o.parent=parent;o.data.materials.append(mats[mat]);return o
def box(n,p,size,parent,mat,bevel=.035):
 bpy.ops.mesh.primitive_cube_add(size=1,location=point(p));o=reg(bpy.context.object,n,parent,mat);o.scale=(size[0],size[2],size[1]);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 if bevel:
  m=o.modifiers.new('CraftedEdges','BEVEL');m.width=bevel;m.segments=1;bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=m.name)
 return o
def beam(n,a,b,w,parent,mat):
 a=point(a);b=point(b);d=b-a
 bpy.ops.mesh.primitive_cube_add(size=1,location=(a+b)*.5);o=reg(bpy.context.object,n,parent,mat);o.scale=(w,w,d.length);o.rotation_euler=d.to_track_quat('Z','Y').to_euler();bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);return o
def cyl(n,p,r,length,parent,mat,axis=(1,0,0)):
 bpy.ops.mesh.primitive_cylinder_add(vertices=16,radius=r,depth=length,location=point(p));o=reg(bpy.context.object,n,parent,mat);o.rotation_euler=point(axis).to_track_quat('Z','Y').to_euler();return o
def torus(n,x,r,parent):
 bpy.ops.mesh.primitive_torus_add(major_segments=32,minor_segments=6,major_radius=r,minor_radius=.07,location=point((x,0,0)));o=reg(bpy.context.object,n,parent,'CH_Brass');o.rotation_euler[1]=math.pi/2;return o

root=empty('Chain_WheelFrame')
box('WheelFoundation',(0,.15,0),(4.6,.3,3.9),root,'CH_Stone')
for x in [-1.5,1.5]:
 beam('AFrame',(x,.3,-1.3),(x,1.8,0),.22,root,'CH_Iron');beam('AFrame',(x,.3,1.3),(x,1.8,0),.22,root,'CH_Iron')
 cyl('Bearing',(x,1.8,0),.29,.32,root,'CH_Brass');box('BearingSeat',(x,1.55,0),(.42,.5,.5),root,'CH_Iron')
root=empty('Chain_WheelRotor')
for x in [-.88,.88]:
 torus('WheelRim',x,1.35,root)
 for i in range(8):
  a=i*math.tau/8;beam('Spoke',(x,0,0),(x,math.sin(a)*1.32,math.cos(a)*1.32),.1,root,'CH_Iron')
cyl('Axle',(0,0,0),.17,3.5,root,'CH_Steel')
for i in range(12):
 a=i*math.tau/12;y=math.sin(a)*1.35;z=math.cos(a)*1.35
 p=box('Repair_Blade_%02d'%i,(0,y,z),(1.8,.16,.65),root,'CH_WoodLight');p.rotation_euler[0]=math.pi/2-a

root=empty('Chain_SluiceBlock')
for i in range(6):
 p=box('Age_BlockPlank_%02d'%i,(-2.65+i*1.06,.9,0),(.96,1.7,.24),root,'CH_Wood' if i%2 else 'CH_WoodLight');p.rotation_euler[1]=(.08 if i%2 else -.06)
for y in [.35,1.25]:box('Age_Brace',(0,y,-.2),(6.4,.22,.16),root,'CH_Wood')
root=empty('Chain_GearJam')
beam('Age_JamBeam',(-1.5,.2,0),(1.4,2.7,0),.3,root,'CH_Wood')
beam('Age_JamBeam',(-1.4,2.6,.12),(1.5,.4,.12),.28,root,'CH_WoodLight')
box('Age_JamSplinter',(0,1.25,-.1),(2.9,.18,.18),root,'CH_Wood')
root=empty('Chain_RailWedge')
for i in range(4):box('Age_Wedge_%02d'%i,(0,.2+i*.18,0),(2.6-i*.3,.18,1.25),root,'CH_Wood' if i%2 else 'CH_WoodLight')
for x in [-.8,.8]:box('Age_Brace',(x,.9,0),(.16,.6,.2),root,'CH_Wood')

root=empty('Chain_DockHead')
box('LockBase',(0,.22,0),(3.2,.44,2.8),root,'CH_Iron')
box('LockHousing',(0,.7,.3),(2.3,.8,1.7),root,'CH_Blue')
for side in [-1,1]:
 box('Repair_LockArm',(side*.95,1.05,-.75),(.36,.45,1.8),root,'CH_Steel')
 box('Repair_LockHook',(side*.72,1.08,-1.57),(.8,.4,.3),root,'CH_Brass')
 cyl('Repair_Piston',(side*.95,.65,.3),.15,1.6,root,'CH_Steel',axis=(0,0,1))
box('ContactCore',(0,1.1,.25),(.8,.12,.8),root,'CH_Brass')
root=empty('Chain_BridgeMotor')
box('MotorBase',(0,.18,0),(3.2,.36,2.8),root,'CH_Iron')
cyl('MotorBody',(0,.85,0),.6,2.5,root,'CH_Blue')
for x in [-1,-.7,-.4,-.1,.2,.5,.8]:cyl('CoolingFin',(x,.85,0),.69,.08,root,'CH_Iron')
box('GearBox',(1.1,.7,.1),(.8,1.2,1.4),root,'CH_Iron')
root=empty('Chain_MotorRotor')
cyl('Spindle',(0,0,0),.2,.6,root,'CH_Brass')
for i in range(4):
 a=i*math.tau/4;beam('CouplingSpoke',(0,0,0),(0,math.sin(a)*.5,math.cos(a)*.5),.12,root,'CH_Brass')
root=empty('Chain_ClockEnclosure')
for x in [-6.5,6.5]:
 for z in [-3.5,3.5]:box('StonePier',(x,1.7,z),(.5,3.4,.5),root,'CH_Stone')
for x in [-6.5,6.5]:
 for y in [.4,2.7]:box('FenceRail',(x,y,0),(.13,.14,7),root,'CH_Iron')
 for z in [-3,-2,-1,0,1,2,3]:box('FenceBar',(x,1.55,z),(.09,2.5,.09),root,'CH_Iron')
for side in [-1,1]:
 for y in [.4,2.7]:box('FrontRail',(side*4.15,y,-3.5),(4.7,.14,.14),root,'CH_Iron')
 for j in range(6):box('FrontBar',(side*(2+j*.8),1.55,-3.5),(.09,2.5,.09),root,'CH_Iron')
box('GateHeader',(0,3.2,-3.5),(4.2,.4,.5),root,'CH_Stone')
for side in [-1,1]:
 root=empty('Chain_ClockGateL' if side<0 else 'Chain_ClockGateR')
 for x in [side*.15,side*.7,side*1.25,side*1.8]:box('GateBar',(x,1.6,-3.5),(.12,3.1,.2),root,'CH_Iron')
 for y in [.35,1.4,2.8]:box('GateCrossbar',(side*.95,y,-3.5),(1.9,.16,.24),root,'CH_Brass')

root=empty('Chain_ServiceBay')
box('BayFloor',(0,3.25,0),(11,.3,9),root,'CH_Stone')
box('BayInset',(0,3.415,0),(10.2,.03,8.2),root,'CH_Iron')
for x in [-5.1,5.1]:
 for z in [-4.1,4.1]:
  box('Column',(x,1.55,z),(.55,3.1,.55),root,'CH_Stone');box('Foot',(x,.2,z),(1,.4,1),root,'CH_Iron')
for z in [-4.35,4.35]:
 box('BayRail',(0,4.5,z),(10.7,.14,.14),root,'CH_Brass')
 for x in [-5,-2.5,0,2.5,5]:box('BayPost',(x,3.95,z),(.15,1.1,.15),root,'CH_Iron')
box('FarRail',(5.35,4.5,0),(.14,.14,8.7),root,'CH_Brass')
for z in [-4,-2,0,2,4]:box('FarPost',(5.35,3.95,z),(.15,1.1,.15),root,'CH_Iron')
for z in [-3,3]:box('EntryWing',(-5.35,4.5,z),(.14,.14,2.4),root,'CH_Brass')

root=empty('Chain_InputShaft')
cyl('InputSpindle',(0,0,0),.12,12,root,'CH_Steel')
for x in [-5,-2,2,5]:
 cyl('ShaftBearing',(x,0,0),.23,.25,root,'CH_Brass');box('BearingFoot',(x,-.55,0),(.4,1,.4),root,'CH_Iron')
root=empty('Chain_ClockHandMinute')
box('MinuteHand',(0,.6,0),(.1,1.25,.06),root,'CH_Brass')
root=empty('Chain_ClockHandHour')
box('HourHand',(.35,0,.04),(.8,.13,.07),root,'CH_Stone')

root=empty('Chain_GearRotor')
bpy.ops.mesh.primitive_torus_add(major_segments=32,minor_segments=6,major_radius=1.22,minor_radius=.1,location=(0,0,0));o=reg(bpy.context.object,'GearInnerRim',root,'CH_Iron');o.rotation_euler[0]=math.pi/2
cyl('GearHub',(0,0,0),.27,.6,root,'CH_Brass',axis=(0,0,1))
for i in range(8):
 a=i*math.tau/8;beam('GearSpoke',(0,0,0),(math.sin(a)*1.2,math.cos(a)*1.2,0),.12,root,'CH_Iron')
for i in range(24):
 a=i*math.tau/24;o=box('Repair_Tooth_%02d'%i,(math.sin(a)*1.4,math.cos(a)*1.4,0),(.24,.35,.35),root,'CH_Brass');o.rotation_mode='QUATERNION';o.rotation_quaternion=Quaternion(point((0,0,1)),-a)
root=empty('Chain_PipePatch')
for i in range(12):
 a=i*math.tau/12;o=box('Repair_PipeSleeve_%02d'%i,(0,math.sin(a)*.51,math.cos(a)*.51),(1.2,.13,.29),root,'CH_Blue');o.rotation_mode='QUATERNION';o.rotation_quaternion=Quaternion(point((1,0,0)),math.pi/2-a)
scene['spec']='Water opens gear enclosure. Clock transmission deploys access ramp. Water + transmission power bridge. Head repair before removing support.'
bpy.ops.wm.save_as_mainfile(filepath='E:/!for WORK/GPT/BorrowedTime_CausalCity.blend')
bpy.ops.object.select_all(action='DESELECT')
for o in col.objects:o.select_set(True)
bpy.context.view_layer.objects.active=next(o for o in col.objects if o.type=='MESH')
bpy.ops.export_scene.fbx(filepath='E:/!for WORK/all for one/Assets/Generated/Imported/SkyCity_CausalCity.fbx',use_selection=True,object_types={'EMPTY','MESH'},global_scale=1,apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',bake_space_transform=False,bake_anim=False)
print('Saved editable causal mechanism;',len(col.objects),'objects')
