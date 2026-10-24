# OOPS! PLANET - ruin kit for the abandoned factory interior (owner brief 2026-10-24: "heavily ruined, collapses open
# new routes"). Reuses the helpers/palette of build_factory_interior.py (everything before its LANDMARKS section).
# Unity axes through xyz(); origin = floor point under the piece unless noted; 1 unit = 1 m.
#   Route pieces : Girder (fallen I-beam stub over the cooling channel, top surface at y=0, runs +z),
#                  WallBreach (jagged frame for a 2.2 x 2.4 m hole in a 0.4 m wall, wall plane = x/y, thickness z),
#                  PlankBarricade (burnable planks across that hole)
#   Mess pieces  : RubblePile, RubbleSmall, FallenTruss (lies along +z), CeilingDebris (origin = ceiling point, hangs
#                  down), ToppledShelf, FloorCracks, ToyJunk, Overgrowth, Puddles
# Run inside Blender:  exec(open(r"E:/!for WORK/AFO/ArtSource/Blender/build_factory_ruins.py").read())
import math
src=open(r'E:/!for WORK/AFO/ArtSource/Blender/build_factory_interior.py',encoding='utf-8').read()
exec(src[:src.index('# ======================= LANDMARKS')])
scene.name='OOPS_FACTORY_RUINS'
OUT=globals().get('RUIN_OUT',OUT); os.makedirs(OUT,exist_ok=True)
random.seed(4242)
def chunk(name,p,s,role,tilt=25):
    box(name,p,s,role,min(.05,min(s)*.3),rot=(random.uniform(-tilt,tilt),0,0))
    o=bpy.context.object; o.rotation_euler.rotate_axis('Z',math.radians(random.uniform(0,360)))
    bpy.ops.object.transform_apply(location=False,rotation=True,scale=False)
def rebar(p,length,role='Rust'):
    a=random.uniform(0,2*math.pi); b=random.uniform(.3,1.1)
    tube('Rebar',[p,(p[0]+math.cos(a)*length*.6,p[1]+length*b*.5,p[2]+math.sin(a)*length*.6),(p[0]+math.cos(a)*length,p[1]+length*b*.6,p[2]+math.sin(a)*length)],.025,role)

# ---------------- route pieces ----------------
def girder():
    L=5.4
    box('Flange',(0,-.04,L/2),(.8,.08,L),'Rust',.02)
    box('Web',(0,-.3,L/2),(.1,.45,L),'Rust',.01)
    box('FlangeLow',(0,-.56,L/2),(.7,.07,L),'Rust',.02)
    for z in [.4+i*.7 for i in range(8)]:
        for x in (-.3,.3): ball('Rivet',(x,.01,z),(.05,.03,.05),'Metal',10)
    for z in (1.1,2.6,4.2): box('PaintFleck',(random.uniform(-.2,.2),.005,z),(.35,.01,.3),'Teal',0)
    box('BentTip',(0,-.14,L+.25),(.8,.08,.6),'Rust',.02,rot=(18,0,0))
    for k in range(3): rebar((random.uniform(-.3,.3),-.3,L-.2),.6)
    for z in (0,.6): chunk('Anchor rubble',(random.uniform(-.5,.5),-.1,z),(.5,.35,.45),'Cream')
def wall_breach():
    W,H,T=2.2,2.4,.4
    random.seed(11)
    for side in (-1,1):
        for y in [i*.32 for i in range(int(H/.32)+1)]:
            for edge in (-1,1):
                s=random.uniform(.25,.5); box('Jag',(edge*(W/2+s*.3),y+.15,side*(T/2+.02)),(s,.3,.12),random.choice(['Cream','Dust','Teal']),.03,rot=(0,0,random.uniform(-25,25)))
        for x in [-W/2+i*.35 for i in range(int(W/.35)+1)]:
            s=random.uniform(.25,.55); box('JagTop',(x,H+s*.25,side*(T/2+.02)),(.36,s,.12),random.choice(['Cream','Dust']),.03,rot=(0,0,random.uniform(-20,20)))
        for k in range(7): chunk('BreachRubble',(random.uniform(-1.4,1.4),.12,side*random.uniform(.5,1.4)),(random.uniform(.2,.5),random.uniform(.15,.35),random.uniform(.2,.45)),random.choice(['Cream','Dust','Teal']))
    for k in range(4): rebar((random.uniform(-1,1),H,0),.7)
    for k in range(6): box('Crack',(random.uniform(-2,2),random.uniform(.3,3.2),-T/2-.035),(random.uniform(.6,1.2),.04,.01),'Coal',0,rot=(0,0,random.uniform(-60,60)))
def plank_barricade():
    for i,y in enumerate((.35,.95,1.55,2.1)):
        box('Plank',(random.uniform(-.1,.1),y,-.3),(2.7,.28,.07),'Orange',.02,rot=(0,0,random.uniform(-9,9)))
    for s in (-1,1): box('CrossPlank',(0,1.2,-.36),(2.9,.24,.06),'Rust',.02,rot=(0,0,s*38))
    for x in (-1.15,1.15):
        for y in (.35,.95,1.55,2.1): ball('Nail',(x,y,-.36),(.04,.04,.03),'Metal',8)
    box('Sign',(.3,1.75,-.4),(.7,.35,.03),'Yellow',.01,rot=(0,0,-6))
    box('SignX',(.3,1.75,-.42),(.5,.06,.01),'Coral',0,rot=(0,0,30)); box('SignX',(.3,1.75,-.42),(.5,.06,.01),'Coral',0,rot=(0,0,-30))

# ---------------- mess pieces ----------------
def rubble_pile(sx=3.2,sz=2.6,h=1.3,n=46):
    for k in range(n):
        u,v=random.uniform(-1,1),random.uniform(-1,1)
        if u*u+v*v>1: continue
        y=h*(1-(u*u+v*v))*random.uniform(.5,1)
        s=random.uniform(.25,.6)*(1.2-y/h*.5)
        chunk('Rubble',(u*sx/2,y*.8,v*sz/2),(s,s*random.uniform(.5,.9),s*random.uniform(.6,1)),random.choice(['Cream','Cream','Dust','Teal','White']))
    ball('RubbleCore',(0,0,0),(sx*.45,h*.65,sz*.45),'Dust',16)
    for k in range(5): rebar((random.uniform(-.8,.8),h*.5,random.uniform(-.6,.6)),random.uniform(.6,1.1))
    for k in range(3): box('Tile',(random.uniform(-sx/2,sx/2)*1.2,.03,random.uniform(-sz/2,sz/2)*1.2),(.4,.04,.4),'White',.01,rot=(random.uniform(-8,8),0,0))
def fallen_truss():
    L=8.0
    for x in (-.45,.45):
        box('Chord',(x,.12,L/2),(.14,.14,L),'Teal',.03); box('TopChord',(x*0,.9,L/2),(.14,.14,L-.6),'Teal',.03)
    for i in range(10):
        z=.4+i*.8
        for x in (-.45,.45): capsule('Diag',(x,.12,z),(0,.9,z+.4),.04,'Teal')
    box('BrokenEnd',(0,.5,L+.1),(.9,.5,.3),'Rust',.04,rot=(0,0,25))
    for k in range(4): rebar((random.uniform(-.4,.4),.5,L),.5)
    rubble_small(at=(0,0,-.3))
def rubble_small(at=(0,0,0)):
    for k in range(12):
        u,v=random.uniform(-1,1),random.uniform(-1,1)
        if u*u+v*v>1: continue
        s=random.uniform(.15,.35); chunk('RubbleS',(at[0]+u*.7,s*.3,at[2]+v*.6),(s,s*.6,s*.8),random.choice(['Cream','Dust','Teal']))
    ball('RubbleSCore',at,(.6,.25,.5),'Dust',12)
def ceiling_debris():
    # origin = the ceiling point; everything hangs below it
    for k,(x,z,len_) in enumerate([(-.6,0,1.8),(.5,.4,1.2),(0,-.7,2.4)]):
        box('HangingSlab',(x,-len_/2,z),(random.uniform(.8,1.3),len_,.15),random.choice(['Cream','Teal']),.04,rot=(random.uniform(10,30),0,random.uniform(-20,20)))
    for k in range(5):
        x,z=random.uniform(-1,1),random.uniform(-1,1)
        tube('Cable',[(x,0,z),(x+random.uniform(-.3,.3),-1.2,z),(x+random.uniform(-.5,.5),-2.6+random.uniform(0,.8),z+random.uniform(-.4,.4))],.03,random.choice(['Coal','Yellow','Coral']))
    box('BrokenLampHousing',(.9,-2.2,-.2),(.5,.3,.5),'Metal',.06,rot=(40,0,0))
    for k in range(4): rebar((random.uniform(-1,1),0,random.uniform(-1,1)),.8)
def toppled_shelf():
    box('ShelfFrame',(0,.55,0),(2.2,.1,1.1),'Metal',.02,rot=(0,0,22))
    box('ShelfFrame2',(-.1,.95,.3),(2.2,.1,.5),'Metal',.02,rot=(0,0,22))
    for x in (-1.05,1.05): box('Upright',(x,.55+x*.4,0),(.08,1.3,1.1),'Teal',.02,rot=(0,0,22))
    for k in range(7):
        s=random.uniform(.3,.55); chunk('SpilledBox',(random.uniform(-1.6,1.6),s*.4,random.uniform(-1.2,1.3)),(s,s*.75,s*.9),random.choice(['Orange','Cream','Coral','Yellow']),tilt=35)
    for k in range(6): box('Paper',(random.uniform(-1.5,1.5),.01,random.uniform(-1.3,1.3)),(.25,.01,.32),'White',0,rot=(0,0,0))
def floor_cracks():
    for k in range(9):
        a=random.uniform(0,180); l=random.uniform(.8,2.2)
        box('Crack',(random.uniform(-1.3,1.3),.006,random.uniform(-1.3,1.3)),(l,.012,.06),'Coal',0); o=bpy.context.object; o.rotation_euler.rotate_axis('Z',math.radians(a)); bpy.ops.object.transform_apply(rotation=True)
    for k in range(6): box('LiftedTile',(random.uniform(-1.2,1.2),.05,random.uniform(-1.2,1.2)),(.45,.06,.45),random.choice(['White','Dust']),.01,rot=(random.uniform(-12,12),0,0))
    for k in range(4): chunk('Grit',(random.uniform(-1.4,1.4),.05,random.uniform(-1.4,1.4)),(.15,.1,.12),'Dust')
def toy_junk():
    for k in range(4): cyl('ToyWheel',(random.uniform(-.9,.9),.12,random.uniform(-.9,.9)),.12,.08,random.choice(['Coal','Yellow']),axis='x',bev=.02,verts=14)
    for k in range(6): chunk('ToyBlock',(random.uniform(-.9,.9),.1,random.uniform(-.9,.9)),(.2,.2,.2),random.choice(['Coral','Blue','Yellow','Leaf']),tilt=40)
    for k in range(2): ball('DollHead',(random.uniform(-.8,.8),.15,random.uniform(-.8,.8)),(.16,.15,.16),'Cream',14)
    capsule('ToyArm',(-.4,.06,.5),(-.1,.06,.7),.05,'Cream')
    for k in range(5): box('Paper',(random.uniform(-1,1),.01,random.uniform(-1,1)),(.22,.01,.3),'White',0)
def overgrowth():
    for k in range(14):
        ball('Leaf',(random.uniform(-.9,.9),random.uniform(0,1.6),random.uniform(-.1,.1)),(random.uniform(.18,.35),random.uniform(.12,.22),.1),'Leaf',10)
    for k in range(4): tube('Vine',[(random.uniform(-.8,.8),2.2,0),(random.uniform(-.8,.8),1.2,.05),(random.uniform(-.8,.8),0,.08)],.03,'Leaf')
    for k in range(5): ball('FloorLeaf',(random.uniform(-.8,.8),.05,random.uniform(.1,.8)),(.25,.06,.2),'Leaf',10)
def puddles():
    for k in range(4): cyl('Puddle',(random.uniform(-1.2,1.2),.004,random.uniform(-1.2,1.2)),random.uniform(.3,.7),.008,'Water',bev=0,verts=20)
    for k in range(3): ball('Drip',(random.uniform(-.4,.4),.02,random.uniform(-.4,.4)),(.06,.02,.06),'Water',10)

def wall_stain():
    # wall plane x/y, a hair in front of z=0 (toward -z)
    for k in range(5):
        x=random.uniform(-1.4,1.4); top=random.uniform(2.2,3.6); l=random.uniform(1.2,2.6)
        box('Streak',(x,top-l/2,-.012),(random.uniform(.07,.16),l,.01),random.choice(['Dust','Dust','Dust','Rust']),0)
    for k in range(7): box('Peel',(random.uniform(-1.6,1.6),random.uniform(.6,3.4),-.015),(random.uniform(.25,.6),random.uniform(.2,.45),.012),random.choice(['Dust','White']),0,rot=(0,0,random.uniform(-15,15)))
    box('Grime',(0,.25,-.01),(3.4,.5,.01),'Dust',0)
    for k in range(4): box('Crack',(random.uniform(-1.4,1.4),random.uniform(1,3),-.018),(random.uniform(.6,1.1),.035,.01),'Coal',0,rot=(0,0,random.uniform(-70,70)))
def hanging_beam():
    # origin = ceiling point; a broken roof beam hangs from one end down to ~2.3 m below
    box('Beam',(0,-1.6,1.2),(.35,.45,3.6),'Teal',.04,rot=(-40,0,0))
    box('BeamBreak',(0,-2.75,2.55),(.4,.5,.35),'Rust',.04,rot=(-25,0,15))
    for k in range(3): rebar((random.uniform(-.1,.1),-2.8,2.6),.5)
    tube('Cable',[(.3,0,-.2),(.4,-1.0,.3),(.2,-2.2,.6)],.03,'Coal')
    for k in range(2): box('Chunk',(random.uniform(-.3,.3),-.1,random.uniform(-.5,.5)),(.5,.2,.4),'Cream',.03)
def debris_scatter():
    for k in range(26):
        s=random.uniform(.1,.32); chunk('Bit',(random.uniform(-1.6,1.6),s*.3,random.uniform(-1.6,1.6)),(s,s*.6,s*.8),random.choice(['Cream','Cream','Dust','Teal','White','Rust']),tilt=40)
    for k in range(5): box('Plank',(random.uniform(-1.4,1.4),.04,random.uniform(-1.4,1.4)),(random.uniform(.6,1.2),.06,.16),'Orange',.01,rot=(random.uniform(-6,6),0,0))
    for k in range(6): box('Paper',(random.uniform(-1.6,1.6),.01,random.uniform(-1.6,1.6)),(.22,.01,.3),'White',0)
def fallen_pipe():
    cyl('Pipe',(0,.2,0),.2,3.4,'Teal',axis='z',bev=.04)
    for z in (-1.2,1.2): ring('Joint',(0,.2,z),.23,.04,'Metal',axis='z')
    cyl('PipeBroken',(.5,.15,2.1),.2,.9,'Teal',axis='x',bev=.04)
    box('Leak',(0,.005,-1.9),(.9,.01,.7),'Water',0)
    for k in range(5): chunk('Bit',(random.uniform(-.6,.6),.06,random.uniform(-1.8,1.8)),(.18,.1,.15),'Dust')
KIT=[('Girder',girder,'route: fallen I-beam stub, top at y=0, runs +z 5.7 m'),('WallBreach',wall_breach,'route: jagged frame for a 2.2 x 2.4 m wall hole'),
     ('PlankBarricade',plank_barricade,'route: burnable planks across the breach'),('RubblePile',rubble_pile,'mess'),('RubbleSmall',rubble_small,'mess'),
     ('FallenTruss',fallen_truss,'mess: lies along +z'),('CeilingDebris',ceiling_debris,'mess: origin at the ceiling, hangs down'),('ToppledShelf',toppled_shelf,'mess'),
     ('FloorCracks',floor_cracks,'mess: flat'),('ToyJunk',toy_junk,'mess'),('Overgrowth',overgrowth,'mess: wall patch, plane x/y'),('Puddles',puddles,'mess: flat'),
     ('WallStain',wall_stain,'mess: wall decal, plane x/y, faces -z'),('HangingBeam',hanging_beam,'mess: origin at the ceiling'),('DebrisScatter',debris_scatter,'mess: flat 3x3'),('FallenPipe',fallen_pipe,'mess: lies along z')]
for name,fn,note in KIT:
    fn(); export('FI_Ru_'+name,note)
for o in scene.objects:
    if o.name.startswith('KIT_FI_Ru_'):
        i=[n for n,_,_ in KIT].index(o.name[10:].split('__')[0]); o.location=(-(i%6)*7.0,-(i//6)*8.0,3.0 if ('CeilingDebris' in o.name or 'HangingBeam' in o.name) else 0)
json.dump(manifest,open(OUT+'/FactoryRuins_manifest.json','w'),indent=1)
bpy.ops.wm.save_as_mainfile(filepath=ROOT+'/ArtSource/Blender/OOPS_FactoryRuins.blend')
print('RUINS',len(manifest))
