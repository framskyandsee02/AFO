"""Approved references: user's giant rear-of-arena sketch and friendly hose-armed machine photo, 2026-10-24.
Build original rounded toy machinery in Blender; no external downloads.
"""
import bpy, os, math, json
ROOT='E:/!for WORK/AFO'
new_scene=bpy.data.scenes.new('OOPS_FACTORY_CARETAKER')
if bpy.context.window: bpy.context.window.scene=new_scene
header=open(ROOT+'/ArtSource/Blender/build_factory_interior.py',encoding='utf-8').read().split('# ======================= LANDMARKS')[0]
exec(compile(header,'factory_art_helpers','exec'))
scene.name='OOPS_FACTORY_CARETAKER'
OUT=ROOT+'/Assets/Scenes/OopsPlanet/Models/FactoryExpansion'
os.makedirs(OUT,exist_ok=True)
manifest=[]
# World architectural modules: dimensions stay explicit; no new mesh modeling in Unity.
box('WallUnit',(0,0,0),(1,1,1),'Cream',.055)
export('FE_WallUnit','one metre rounded module, centre pivot')
box('FloorUnit',(0,0,0),(1,1,1),'Teal',.025)
export('FE_FloorUnit','one metre floor module, centre pivot')
# Entrance facade to be placed in the original hub scene.
for s in (-1,1):
    box('EntrancePillar',(s*3.3,3.3,0),(1.1,6.6,1.8),'Teal',.3)
    ball('EntranceBolt',(s*3.3,1.4,-.95),(.16,.16,.1),'Yellow',16)
box('EntranceArch',(0,6.1,0),(7.7,1.25,1.8),'Coral',.4)
box('EntranceSign',(0,6.1,-.95),(4.5,.75,.14),'Cream',.15)
box('DoorInset',(0,2.6,.6),(5.5,5.2,.25),'Dark',.3)
for x in (-2.1,-1.4,-.7,0,.7,1.4,2.1):box('DoorSlat',(x,2.6,.42),(.56,4.9,.16),'Teal',.06)
export('FE_Entrance','facade only, front -Z, portal trigger goes before dark door')
# Caretaker body: rounded cuboid, round vacuum mouth, vent details, patch plates.
box('Body',(0,4.0,0),(10,8,4.8),'Teal',1.0)
box('Apron',(0,1.5,-2.45),(8.2,1.6,.32),'Cream',.4)
box('NamePlate',(0,1.55,-2.65),(3.2,.5,.15),'Yellow',.1)
for x in (-3.8,3.8):
    for y in (1.1,6.9):ball('Rivet',(x,y,-2.4),(.18,.18,.12),'Metal',16)
for x in (-2.6,2.6):ring('EyeSocket',(x,5.7,-2.42),1.15,.2,'Cream',axis='z')
ring('MouthRim',(0,3.3,-2.54),1.1,.22,'Coral',axis='z')
cyl('MouthDark',(0,3.3,-2.5),1.0,.09,'Dark',axis='z',bev=0)
for x in (-3.2,-2.4,-1.6,-.8,0,.8,1.6,2.4,3.2):box('TopVent',(x,7.2,-2.4),(.2,.45,.12),'Dark',.06)
for x,y in [(-4.3,3.4),(3.8,4.4),(-2,1.9)]:box('Patch',(x,y,-2.45),(.8,.3,.03),'Dust',.04)
for x in (-5.1,5.1):cyl('Shoulder',(x,4.1,0),1.0,.7,'Yellow',axis='x',bev=.12)
export('FE_BossBody','8m high body; face -Z, rig at floor+3m')
cyl('SpringBase',(0,1.5,0),2.8,3,'Dark',bev=.3)
for y in (.3,.7,1.1,1.5,1.9,2.3,2.7):ring('Bellows',(0,y,0),2.8,.2,'Metal')
cyl('BaseFoot',(0,.2,0),3.7,.4,'Coral',bev=.15)
export('FE_BossBase','bellows base')
ball('EyeWhite',(0,0,0),(1,1,.6),'White')
ball('Iris',(0,-.08,-.53),(.53,.57,.12),'Blue')
ball('Pupil',(0,-.08,-.64),(.25,.32,.07),'Dark')
ball('Glint',(-.13,.1,-.7),(.1,.1,.04),'White',16)
export('FE_BossEye','one eye, front -Z, centre pivot')
box('Brow',(0,0,0),(1.9,.3,.32),'Cream',.14)
export('FE_BossBrow','animatable expressive eyebrow')
for i in range(18):ring('HoseRib',(i*.25,0,0),.65,.13,'Dark',axis='x')
for x in (0,4.25):cyl('HoseCuff',(x,0,0),.82,.45,'Yellow',axis='x',bev=.1)
export('FE_BossArm','hose arm extends +X from shoulder pivot')
ball('Hand',(0,0,0),(1.3,1.15,1.2),'Yellow')
ring('HandCuff',(0,0,-.9),.85,.15,'Cream',axis='z')
cyl('HandIntake',(0,0,-1),.68,.1,'Dark',axis='z',bev=0)
for s in (-1,1):ball('Finger',(s*.9,-.3,-.6),(.5,.55,.65),'Coral')
export('FE_BossHand','round vacuum mitten, front -Z')
cyl('Arena',(0,-.2,0),22,.4,'Yellow',bev=.15,verts=96)
ring('ArenaRim',(0,.02,0),21.5,.13,'Coral')
for i in range(16):
    a=i*2*math.pi/16
    box('ArenaMark',(20*math.cos(a),.025,20*math.sin(a)),(.7,.025,.3),'Cream',.03,rot=(0,-a*180/math.pi,0))
export('FE_BossArena','44m circular yellow arena, top at y=0')
ring('GearRing',(0,.45,0),.55,.18,'Yellow')
for i in range(10):
    a=i*2*math.pi/10
    box('GearTooth',(.7*math.cos(a),.45,.7*math.sin(a)),(.3,.28,.3),'Metal',.06)
export('FE_BossGear','round portable repair gear, 0.9m high pivot base')
with open(OUT+'/FactoryExpansion_manifest.json','w',encoding='utf-8') as f:json.dump(manifest,f,indent=2)
bpy.ops.wm.save_as_mainfile(filepath=ROOT+'/ArtSource/Blender/OOPS_FactoryCaretaker.blend')
print('FACTORY_CARETAKER_EXPORTS',len(manifest),sum(x['triangles'] for x in manifest))
