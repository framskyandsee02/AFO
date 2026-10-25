import bpy, math
from mathutils import Vector

scene=bpy.data.scenes.new('BT_TelescopicBridge')
bpy.context.window.scene=scene
collection=bpy.data.collections.new('TelescopicBridge_Parts')
scene.collection.children.link(collection)
palette={
 'SB_Deck':(.12,.19,.22,1),'SB_Frame':(.23,.34,.34,1),
 'SB_Ivory':(.77,.72,.55,1),'SB_Copper':(.58,.25,.09,1),
 'SB_Hazard':(.95,.54,.09,1),'SB_Dark':(.035,.06,.075,1),
 'SB_Light':(.12,.9,.85,1),'SB_Steel':(.48,.56,.58,1)}
mats={}
for name,col in palette.items():
 m=bpy.data.materials.new(name);m.diffuse_color=col;m.use_nodes=True
 bs=m.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=col
 bs.inputs['Metallic'].default_value=.35;bs.inputs['Roughness'].default_value=.48
 if name=='SB_Light':
  bs.inputs['Emission Color'].default_value=col;bs.inputs['Emission Strength'].default_value=2
 mats[name]=m
def empty(name):
 o=bpy.data.objects.new(name,None);collection.objects.link(o);return o
def put(o,name,parent,mat):
 o.name=name
 for c in list(o.users_collection):c.objects.unlink(o)
 collection.objects.link(o);o.parent=parent;o.data.materials.append(mats[mat]);return o
def box(name,p,size,parent,mat,bevel=.035):
 bpy.ops.mesh.primitive_cube_add(size=1,location=p);o=put(bpy.context.object,name,parent,mat)
 o.scale=size;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 if bevel:
  mod=o.modifiers.new('MachinedEdges','BEVEL');mod.width=bevel;mod.segments=1
  bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=mod.name)
 return o
def cylinder(name,p,r,depth,parent,mat,axis='Z'):
 bpy.ops.mesh.primitive_cylinder_add(vertices=12,radius=r,depth=depth,location=p)
 o=put(bpy.context.object,name,parent,mat)
 if axis=='X':o.rotation_euler[1]=math.pi/2
 if axis=='Y':o.rotation_euler[0]=math.pi/2
 return o
length=5.4
for i in range(5):
 root=empty('SB_Panel'+str(i));w=5.8-i*.28;top=.24+(4-i)*.025
 box('Deck',(0,0,top-.12),(w,length+.28,.24),root,'SB_Deck')
 box('InsetTread',(0,0,top+.008),(w-.5,length-.18,.016),root,'SB_Frame',.008)
 for sign in [-1,1]:
  x=sign*(w*.5-.12)
  box('SlidingGirder',(x,0,top-.28),(.24,length+.25,.3),root,'SB_Steel')
  box('CopperRail',(x,0,top+1.05),(.12,length+.15,.12),root,'SB_Copper')
  box('InnerGuard',(x,0,top+.48),(.08,length+.1,.08),root,'SB_Frame')
  box('EdgeLamp',(sign*(w*.5-.26),0,top+.025),(.06,length-.2,.025),root,'SB_Light',.008)
  for y in [-length*.43,0,length*.43]:
   box('RailPost',(x,y,top+.52),(.13,.13,1.04),root,'SB_Ivory')
   cylinder('Roller',(x,y,top-.27),.14,.3,root,'SB_Dark','X')
 for y in [-1.8,-.9,0,.9,1.8]:
  box('TreadSeam',(0,y,top+.019),(w-.65,.025,.016),root,'SB_Dark',0)
 for x in [-w*.5+.4,w*.5-.4]:
  for y in [-2.4,2.4]:cylinder('DeckBolt',(x,y,top+.025),.065,.04,root,'SB_Steel')
 box('LeadingLip',(0,-length*.5,top-.04),(w,.12,.10),root,'SB_Hazard')

dock=empty('SB_Dock')
box('FixedDock',(0,2.6,.14),(6.4,6.4,.28),dock,'SB_Frame')
for sign in [-1,1]:
 x=sign*3.45
 box('DriveHousing',(x,2.4,.65),(1.1,6.7,1.3),dock,'SB_Ivory',.09)
 box('HousingSide',(x+sign*.56,2.4,.63),(.08,5.7,.76),dock,'SB_Frame')
 for y in [.2,1.5,2.8,4.1]:box('Vent',(x+sign*.61,y,.65),(.04,.6,.25),dock,'SB_Dark',.01)
 cylinder('DriveMotor',(x,.1,1),.42,.8,dock,'SB_Copper','Y')
 cylinder('MotorCap',(x,-.34,1),.29,.07,dock,'SB_Dark','Y')
 box('StatusLamp',(x,-.72,1.16),(.6,.12,.12),dock,'SB_Light')
 box('DockMast',(x,-.25,1.5),(.28,.28,2.8),dock,'SB_Frame')
 box('DockLamp',(x,-.45,2.6),(.36,.15,.45),dock,'SB_Light')
box('CrossBrace',(0,-.25,3),(7.2,.35,.32),dock,'SB_Frame')
for side in [-1,1]:
 gate=empty('SB_GateL' if side<0 else 'SB_GateR')
 box('SafetyGate',(side*1.45,.58,.98),(2.9,.16,1.28),gate,'SB_Dark')
 for j in range(5):
  o=box('WarningStripe',(side*1.45-1.05+j*.52,.48,.98),(.18,.035,1.12),gate,'SB_Hazard',.015)
  o.rotation_euler[1]=-.28
box('DockPlate',(0,1,.29),(5.9,1.3,.08),dock,'SB_Steel')

scene['authoring_note']='Five nested sliding sections. Blender Y negative = forward in Unity. Fixed dock rests inside source island.'
bpy.ops.wm.save_as_mainfile(filepath='E:/!for WORK/GPT/BorrowedTime_TelescopicBridge.blend')
bpy.ops.object.select_all(action='DESELECT')
for obj in collection.objects:obj.select_set(True)
bpy.context.view_layer.objects.active=next(o for o in collection.objects if o.type=='MESH')
bpy.ops.export_scene.fbx(filepath='E:/!for WORK/all for one/Assets/Generated/Imported/SkyCity_TelescopicBridge.fbx',use_selection=True,object_types={'EMPTY','MESH'},global_scale=1,apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',bake_space_transform=False,axis_forward='-Z',axis_up='Y',bake_anim=False)
print('Exported bridge library:',len(collection.objects),'objects')
