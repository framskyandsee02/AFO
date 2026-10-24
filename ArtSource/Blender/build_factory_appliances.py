# OOPS! PLANET - disguised power sources for the factory interior (owner brief 2026-10-24, Kirby-style "absorb the
# everyday object"): every absorbable is an ordinary factory appliance that belongs to its element, with a FULL look
# and a DRAINED look (shown while the source recovers for 20 s). Reuses the helpers/palette of
# build_factory_interior.py (everything before its LANDMARKS section).
#   Magnet   : ToolCart, MetalLocker           Pressure : FireExtinguisher, AirCompressor
#   Bounce   : TireStack, OldMattress, SpringHorse
#   Water    : WaterCooler, WaterBarrel, LeakyPipe
#   Electric : ElectricPanel, BatteryBank, ArcadeMachine
#   Heat     : Grill, Heater, CharcoalStove
# Each model: origin = floor centre, front faces -z, footprint <= 1.2 x 1.2 m.
# Run inside Blender:  exec(open(r"E:/!for WORK/AFO/ArtSource/Blender/build_factory_appliances.py").read())
# Output: Models/FactoryInterior/FI_Ap_<Name>.fbx and FI_Ap_<Name>_Drained.fbx + FactoryAppliances_manifest.json
import math
src=open(r'E:/!for WORK/AFO/ArtSource/Blender/build_factory_interior.py',encoding='utf-8').read()
exec(src[:src.index('# ======================= LANDMARKS')])
scene.name='OOPS_FACTORY_APPLIANCES'
OUT=globals().get('APPLIANCE_OUT',OUT); os.makedirs(OUT,exist_ok=True)   # staging folder while Unity is busy
random.seed(77)

def wheels(xs,zs,r=.08,role='Coal'):
    for x in xs:
        for z in zs: cyl('Wheel',(x,r,z),r,.06,role,axis='x',bev=.02,verts=16)
def hose(pts,r=.035,role='Coal'): tube('Hose',pts,r,role)
def gauge(p,r=.09,face='-z',live=True):
    cyl('GaugeRim',p,r,.05,'Metal',axis='z',bev=.015,verts=20)
    cyl('GaugeFace',(p[0],p[1],p[2]-.03),r*.8,.02,'Cream',axis='z',bev=0,verts=20)
    ang=math.radians(40 if live else -60)
    box('GaugeNeedle',(p[0]+math.sin(ang)*r*.35,p[1]+math.cos(ang)*r*.35,p[2]-.045),(.015,r*.7,.01),'Coral',0,rot=(0,0,-math.degrees(ang)))

# ---------------- MAGNET ----------------
def tool_cart(drained):
    body='Rust' if drained else 'Coral'
    box('CartBody',(0,.62,0),(1.0,.78,.58),body,.06)
    box('CartTop',(0,1.04,0),(1.06,.06,.64),'Metal',.03)
    for i,y in enumerate((.9,.66,.42)):
        dz=-.0 if not drained else -(.12+i*.07)
        box('Drawer',(0,y,-.29+dz),(.9,.2,.04 if not drained else .3),body,.02)
        box('Handle',(0,y,-.33+dz*(1 if not drained else 1.0)-(.0 if not drained else .14)),(.36,.04,.04),'Metal',.015)
    for s in (-1,1): tube('PushBar',[(s*.53,1.0,.25),(s*.62,1.3,.25),(s*.62,1.3,-.2)],.025,'Metal')
    if not drained:
        capsule('Wrench',(-.3,1.1,-.1),(.15,1.1,.12),.035,'Metal'); box('WrenchJaw',(.18,1.1,.13),(.12,.05,.09),'Metal',.02)
        capsule('Hammer',(.1,1.1,-.18),(.42,1.1,-.1),.03,'Teal'); box('HammerHead',(.43,1.12,-.1),(.08,.08,.2),'Metal',.02)
        for x in (-.35,-.2): cyl('Bolt',(x,1.1,.18),.04,.06,'Metal',bev=.01,verts=6)
    else:
        box('DentPatch',(.3,.75,-.3),(.22,.18,.02),'Dust',.01)
    wheels((-.42,.42),(-.22,.22))

def metal_locker(drained):
    body='Rust' if drained else 'Teal'
    box('LockerBack',(0,.85,.12),(.92,1.7,.26),body,.04)
    box('LockerTop',(0,1.72,0),(.96,.06,.52),'Metal',.02)
    box('LockerBase',(0,.06,0),(.96,.12,.52),'Dark',.02)
    box('LockerInside',(0,.86,-.08),(.86,1.56,.1),'Dark',0)
    for s in (-1,1):
        if drained and s==1:
            # right door swung open about its outer hinge (x=+.46)
            box('LockerDoor',(.46+.2,.86,-.29+.02),(.44,1.56,.04),body,.02,rot=(0,-70,0))
            continue
        box('LockerDoor',(s*.225,.86,-.25),(.44,1.56,.04),body,.02)
        for k in range(4): box('Vent',(s*.225,1.45-k*.07,-.275),(.28,.025,.02),'Dark',0)
        box('LockerHandle',(s*.04,.95,-.285),(.03,.18,.04),'Metal',.01)
    if not drained:
        box('Magnet',(-.2,1.25,-.29),(.12,.12,.03),'Coral',.02); box('Label',(.22,1.15,-.275),(.18,.08,.01),'Cream',0)
    else:
        box('DentPatch',(-.25,.55,-.28),(.2,.25,.02),'Dust',.01)

# ---------------- PRESSURE ----------------
def fire_extinguisher(drained):
    if not drained:
        box('Stand',(0,.05,0),(.6,.1,.5),'Dark',.03)
        cyl('ExtBody',(0,.62,0),.26,1.0,'Coral',bev=.1)
        ball('ExtDome',(0,1.1,0),(.26,.14,.26),'Coral')
        cyl('ExtNeck',(0,1.2,0),.07,.12,'Dark',bev=.02)
        box('ExtValve',(0,1.27,0),(.12,.08,.12),'Metal',.02)
        box('ExtHandle',(0,1.33,.06),(.06,.03,.26),'Metal',.01,rot=(-12,0,0))
        hose([(.07,1.26,-.04),(.2,1.15,-.24),(.3,.8,-.28),(.28,.45,-.27)],.035)
        cyl('Nozzle',(.28,.38,-.27),.05,.14,'Dark',bev=.01)
        box('ExtLabel',(0,.6,-.26),(.26,.3,.02),'Cream',.01)
        gauge((0,.98,-.25),.07)
    else:
        box('Stand',(0,.05,0),(.6,.1,.5),'Dark',.03)
        cyl('ExtBodyFlat',(0,.32,0),.3,.42,'Coral',bev=.12)          # slumped, deflated body
        ball('ExtSag',(.05,.48,0),(.32,.12,.28),'Coral')
        box('ExtValve',(.1,.6,0),(.12,.08,.12),'Metal',.02,rot=(0,0,-35))
        hose([(.15,.6,-.02),(.32,.25,-.2),(.5,.12,-.35),(.62,.1,-.3)],.035)
        box('ExtLabel',(0,.3,-.29),(.26,.18,.02),'Cream',.01,rot=(8,0,0))
        gauge((-.12,.5,-.27),.06,live=False)

def air_compressor(drained):
    tank='Blue'
    if not drained:
        cyl('Tank',(0,.42,0),.3,1.0,tank,axis='x',bev=.12)
        for s in (-1,1): ball('TankCap',(s*.5,.42,0),(.1,.28,.28),tank)
    else:
        bpy.ops.mesh.primitive_cylinder_add(vertices=32,radius=.3,depth=1.0,location=xyz((0,.3,0)))
        o=bpy.context.object; o.rotation_euler=(0,math.pi/2,0); o.scale=(1,.85,.62)   # crushed tank
        bpy.ops.object.transform_apply(location=False,rotation=True,scale=True); soft(o,.08); finish(o,'Tank',tank)
        box('Crease',(0,.48,-.1),(.8,.03,.04),'Dust',.01)
    for s in (-1,1): box('Foot',(s*.32,.06,0),(.12,.12,.5),'Dark',.03)
    y=.78 if not drained else .6
    box('Motor',(-.1,y,0),(.46,.26,.34),'Dark',.06)
    cyl('Fan',(.22,y,0),.13,.08,'Metal',axis='x',bev=.02)
    gauge((.25,y+.05,-.2),.08,live=not drained)
    if not drained: hose([(.32,y,-.1),(.45,.5,-.35),(.3,.15,-.45),(-.1,.12,-.42),(-.25,.2,-.3)],.03,'Yellow')
    else: hose([(.32,y,-.1),(.5,.2,-.3),(.7,.05,-.4)],.03,'Yellow')
    cyl('Wheel',(-.5,.12,.2),.12,.06,'Coal',axis='x',bev=.02)

# ---------------- BOUNCE ----------------
def tire_stack(drained):
    if not drained:
        for i in range(3): ring('Tire',(0,.17+i*.33,0),.36,.16,'Coal'); cyl('Rim',(0,.17+i*.33,0),.22,.2,'Metal',bev=.03)
        ring('TireTop',(.05,1.1,-.05),.32,.14,'Coal')
        box('Stripe',(0,.5,-.52),(.3,.06,.02),'Yellow',0)
    else:
        for i,(dx,dz) in enumerate([(0,0),(.08,-.05),(-.06,.06)]):
            bpy.ops.mesh.primitive_torus_add(major_segments=40,minor_segments=12,location=xyz((dx,.07+i*.12,dz)),major_radius=.42,minor_radius=.16)
            o=bpy.context.object; o.scale=(1.08,1,.42); bpy.ops.object.transform_apply(location=False,rotation=False,scale=True); finish(o,'TireFlat','Coal')
        ring('TireSlump',(.25,.42,0),.3,.12,'Coal',axis='x')

def old_mattress(drained):
    if not drained:
        box('Crate',(0,.25,.15),(1.0,.5,.7),'Rust',.04)
        box('Mattress',(0,.85,-.05),(1.0,1.15,.24),'Cream',.12,rot=(-14,0,0))
        for x in (-.3,0,.3): box('MatStripe',(x,.85,-.18),(.08,1.1,.02),'Coral',0,rot=(-14,0,0))
        for x,y in [(-.25,1.25),(.2,.6),(.33,1.15)]: ring('Spring',(x,y,-.21),.07,.02,'Metal',axis='z')
    else:
        box('Crate',(0,.25,.15),(1.0,.5,.7),'Rust',.04)
        box('MatFlop',(0,.56,.1),(1.0,.12,.9),'Cream',.05)
        box('MatDroop',(0,.33,-.42),(1.0,.5,.12),'Cream',.05,rot=(15,0,0))
        for x in (-.3,0,.3): box('MatStripe',(x,.63,.1),(.08,.02,.85),'Coral',0)
        for x,z in [(-.3,.2),(.25,-.05),(.1,.35)]:
            for k in range(4): ring('LooseSpring',(x,.66+k*.05,z),.05,.012,'Metal')

def spring_horse(drained):
    box('Base',(0,.05,0),(.8,.1,.8),'Teal',.04)
    n=7 if not drained else 7
    gap=.07 if not drained else .025
    for k in range(n): ring('Coil',(0,.13+k*gap,0),.16,.03,'Metal')
    top=.13+n*gap
    tilt=0 if not drained else 28
    def hp(p):   # tilt the horse forward (about x) around the spring top when drained
        if not tilt: return (p[0],p[1]+top,p[2])
        a=math.radians(tilt); y,z=p[1],p[2]
        return (p[0],top+y*math.cos(a)+z*math.sin(a)*-1+0,z*math.cos(a)+y*math.sin(a)*-1)
    capsule('HorseBody',hp((0,.22,.22)),hp((0,.22,-.18)),.2,'Yellow')
    capsule('HorseNeck',hp((0,.3,-.2)),hp((0,.55,-.32)),.1,'Yellow')
    ball('HorseHead',hp((0,.62,-.42)),(.12,.13,.2),'Yellow')
    for s in (-1,1): ball('Ear',hp((s*.06,.78,-.36)),(.04,.07,.03),'Yellow'); ball('Eye',hp((s*.1,.66,-.5)),(.03,.03,.03),'Dark')
    for k in range(4): ball('Mane',hp((0,.5+k*.08,-.22-k*.04)),(.05,.06,.06),'Coral')
    for s in (-1,1): cyl('Handle',hp((s*.14,.6,-.36)),.03,.12,'Coral',axis='x',bev=.01)
    box('Saddle',hp((0,.42,.05)),(.3,.04,.26),'Coral',.02)
    capsule('Tail',hp((0,.3,.4)),hp((0,.12,.5)),.05,'Coral')

# ---------------- WATER ----------------
def water_cooler(drained):
    box('CoolerBase',(0,.5,0),(.42,1.0,.4),'Cream',.05)
    box('CoolerTray',(0,.6,-.22),(.26,.03,.1),'Dark',.01)
    for x,c in ((-.07,'Blue'),(.07,'Coral')): box('Tap',(x,.75,-.22),(.05,.08,.06),c,.015)
    cyl('JugNeck',(0,1.05,0),.07,.1,'Blue',bev=.02)
    if not drained:
        cyl('Jug',(0,1.32,0),.19,.44,'Water',bev=.08); ring('JugRib',(0,1.25,0),.19,.012,'Blue'); ring('JugRib',(0,1.4,0),.19,.012,'Blue')
        ball('Bubble',(.05,1.4,-.12),(.03,.03,.03),'White')
    else:
        cyl('JugEmpty',(0,1.32,0),.19,.44,'Ice',bev=.08,r2=None); box('Crack',(.12,1.3,-.17),(.02,.2,.01),'Dark',0,rot=(0,0,20))
    box('Sticker',(0,.42,-.205),(.2,.14,.01),'Blue' if not drained else 'Dust',0)

def water_barrel(drained):
    cyl('Barrel',(0,.42,0),.36,.84,'Teal',bev=.06,r2=None)
    for y in (.2,.65): ring('Hoop',(0,y,0),.37,.025,'Metal')
    ring('Lip',(0,.84,0),.35,.035,'Teal')
    if not drained:
        cyl('WaterTop',(0,.78,0),.32,.03,'Water',bev=0)
        cyl('Dipper',(.15,.9,.05),.07,.08,'Yellow',bev=.02); capsule('DipperStick',(.15,.9,.05),(.32,1.15,.25),.02,'Yellow')
        cyl('Puddle',(-.4,.005,-.3),.22,.01,'Water',bev=0)
    else:
        cyl('DryBottom',(0,.1,0),.32,.03,'Dust',bev=0); ring('Stain',(0,.5,0),.355,.012,'Dust')
        cyl('DipperDrop',(-.45,.04,-.25),.07,.08,'Yellow',bev=.02,axis='x')
        box('Crack',(.25,.45,-.27),(.02,.3,.01),'Dark',0,rot=(0,-30,12))

def leaky_pipe(drained):
    box('Flange',(0,.04,0),(.5,.08,.5),'Metal',.02)
    cyl('PipeUp',(0,.75,0),.14,1.4,'Teal',bev=.03)
    ring('Joint',(0,1.0,0),.16,.035,'Metal'); ring('Joint',(0,.35,0),.16,.035,'Metal')
    cyl('ValveStem',(0,1.2,-.18),.03,.15,'Metal',axis='z',bev=.01); ring('ValveWheel',(0,1.2,-.26),.12,.02,'Coral',axis='z')
    box('Tape',(0,.75,-.13),(.24,.14,.04),'Dust',.02)
    if not drained:
        cyl('Puddle',(0,.005,-.25),.42,.01,'Water',bev=0)
        for k,y in enumerate((.66,.45,.25)): ball('Drop',(.02*k,y,-.18-.03*k),(.035,.05,.035),'Water',12)
        ball('Spray',(.07,.78,-.16),(.06,.04,.04),'Water',12)
    else:
        ring('DryStain',(0,.004,-.25),.36,.03,'Dust'); box('RustDrip',(0,.62,-.14),(.06,.22,.01),'Rust',0)

# ---------------- ELECTRIC ----------------
def electric_panel(drained):
    box('Plinth',(0,.08,0),(.8,.16,.45),'Dark',.03)
    box('Cabinet',(0,.75,.05),(.72,1.18,.36),'Teal',.04)
    box('Hazard',(0,1.27,-.14),(.5,.12,.02),'Yellow',0)
    lamp='Coal' if drained else 'Glow'
    for x in (-.2,0,.2): ball('Lamp',(x,1.12,-.15),(.05,.05,.03),lamp,12)
    if not drained:
        box('Door',(0,.66,-.14),(.62,.82,.03),'Teal',.02)
        box('Lever',(.22,.75,-.2),(.04,.22,.04),'Coral',.01,rot=(-25,0,0))
        box('Bolt',(-.05,.78,-.165),(.08,.14,.01),'Yellow',0)
    else:
        box('DoorOpen',(-.31-.28,.66,-.14-.15),(.62,.82,.03),'Teal',.02,rot=(0,55,0))
        box('Inside',(0,.66,-.11),(.6,.78,.02),'Dark',0)
        for x in (-.15,.0,.15): box('Breaker',(x,.8,-.125),(.08,.14,.03),'Dust',.01)
        box('Lever',(.22,.5,-.2),(.04,.22,.04),'Coral',.01,rot=(25,0,0))
        box('Scorch',(.1,1.0,-.125),(.2,.12,.01),'Coal',0)
    tube('Cable',[(-.2,1.34,.1),(-.2,1.6,.1),(-.1,1.9,.15)],.04,'Coal'); tube('Cable',[(.2,1.34,.1),(.25,1.7,.15)],.03,'Yellow')

def battery_bank(drained):
    box('Tray',(0,.05,0),(1.05,.1,.6),'Dark',.03)
    for i,x in enumerate((-.33,0,.33)):
        tip=drained and i==2
        if tip:
            cyl('Battery',(x+.05,.18,-.05),.15,.62,'Dust',axis='x',bev=.04); cyl('Cap',(x+.39,.18,-.05),.06,.06,'Metal',axis='x',bev=.01)
            continue
        body='Dust' if drained else 'Yellow'
        cyl('Battery',(x,.45,0),.15,.7,body,bev=.04)
        cyl('Band',(x,.62,0),.152,.14,'Dark',bev=0)
        cyl('Cap',(x,.84,0),.06,.07,'Metal',bev=.01)
        box('Charge',(x,.35,-.15),(.08,.24 if not drained else .05,.01),'Coal' if drained else 'Glow',0)
    if not drained: tube('Jumper',[(-.33,.88,0),(-.16,.98,0),(0,.88,0)],.02,'Coral'); tube('Jumper',[(0,.88,0),(.16,.98,0),(.33,.88,0)],.02,'Coral')
    else: tube('Jumper',[(-.33,.88,0),(-.2,.5,-.2),(-.1,.12,-.35)],.02,'Coral')

def arcade_machine(drained):
    box('Cab',(0,.8,.05),(.68,1.6,.62),'Blue',.05)
    box('SideArt',(-.345,.9,.05),(.02,.6,.4),'Coral',0); box('SideArt',(.345,.9,.05),(.02,.6,.4),'Coral',0)
    box('Marquee',(0,1.5,-.27),(.6,.16,.06),'Coal' if drained else 'Glow',.02)
    box('ScreenBezel',(0,1.15,-.24),(.56,.42,.06),'Dark',.02,rot=(-12,0,0))
    if not drained: box('Screen',(0,1.15,-.28),(.46,.32,.02),'Glow',0,rot=(-12,0,0))
    else: box('Screen',(0,1.15,-.28),(.46,.32,.02),'Coal',0,rot=(-12,0,0)); box('Crack',(.06,1.17,-.295),(.02,.26,.01),'Metal',0,rot=(-12,0,35))
    box('Deck',(0,.85,-.36),(.62,.08,.26),'Teal',.03,rot=(8,0,0))
    capsule('Stick',(-.15,.9,-.38),(-.15,1.0,-.38),.015,'Metal'); ball('Knob',(-.15,1.02,-.38),(.04,.04,.04),'Coral',12)
    for x in (.05,.15,.25): cyl('Button',(x,.91,-.38),.03,.03,'Yellow' if not drained else 'Dust',bev=.01,verts=12)
    box('CoinDoor',(0,.42,-.25),(.24,.3,.02),'Metal',.01)
    for x in (-.05,.05): box('CoinSlot',(x,.48,-.265),(.03,.08,.01),'Coal' if drained else 'Coral',0)

# ---------------- HEAT ----------------
def grill(drained):
    for a in (0,120,240):
        t=math.radians(a); tube('Leg',[(.28*math.cos(t),.55,.28*math.sin(t)),(.45*math.cos(t),0,.45*math.sin(t))],.025,'Coal')
    ball('Bowl',(0,.62,0),(.42,.26,.42),'Coal')
    cyl('BowlLip',(0,.66,0),.42,.06,'Coal',bev=.02)
    cyl('Coals',(0,.68,0),.36,.04,'Dust' if drained else 'Glow',bev=0)
    if not drained:
        for k in range(6): ball('Ember',(random.uniform(-.2,.2),.71,random.uniform(-.2,.2)),(.06,.04,.06),'Orange',10)
        cyl('Grate',(0,.74,0),.38,.015,'Metal',bev=0,verts=24)
        for x in (-.2,0,.2): capsule('Skewer',(x,.77,-.25),(x,.77,.25),.03,'Coral'); ball('Meatball',(x,.8,0),(.06,.06,.06),'Rust',10)
        ball('Lid',(.0,.96,.38),(.4,.2,.12),'Coal')       # lid propped open behind
    else:
        cyl('Grate',(0,.74,0),.38,.015,'Metal',bev=0,verts=24)
        ball('LidDown',(.55,.18,-.3),(.36,.16,.36),'Coal'); cyl('LidKnob',(.55,.34,-.3),.04,.06,'Metal',bev=.01)
        for k in range(4): ball('Ash',(random.uniform(-.2,.2),.71,random.uniform(-.2,.2)),(.07,.03,.07),'Dust',10)
    box('SideTable',(.0,.6,-.48),(.5,.04,.14),'Cream',.02)

def heater(drained):
    box('HeaterBody',(0,.55,0),(.82,.9,.3),'Cream',.12)
    box('Feet',(0,.04,0),(.7,.08,.4),'Dark',.03)
    box('Grille',(0,.6,-.155),(.66,.58,.02),'Metal',.02)
    for k in range(3):
        cyl('Element',(0,.42+k*.18,-.13),.04,.6,'Coal' if drained else 'Orange',axis='x',bev=.01)
        if not drained: cyl('Glow',(0,.42+k*.18,-.15),.03,.58,'Glow',axis='x',bev=0)
    for x in (-.32,.32): cyl('Knob',(x,.94,-.16),.04,.05,'Coral',axis='z',bev=.01)
    box('Handle',(0,1.04,0),(.3,.06,.08),'Teal',.03)
    if drained: box('Frost',(-.2,.25,-.16),(.25,.08,.01),'Ice',0)

def charcoal_stove(drained):
    cyl('Bucket',(0,.25,0),.26,.5,'Orange',bev=.05,r2=.33)
    ring('Band',(0,.42,0),.32,.025,'Metal')
    cyl('ClayRim',(0,.52,0),.3,.06,'Rust',bev=.02)
    box('AirHole',(0,.16,-.26),(.16,.1,.04),'Coal',.02)
    if not drained:
        cyl('Coals',(0,.54,0),.25,.04,'Glow',bev=0)
        for k in range(5): ball('Coal',(random.uniform(-.13,.13),.57,random.uniform(-.13,.13)),(.06,.04,.06),'Coal',10)
        cyl('Kettle',(0,.68,0),.18,.22,'Metal',bev=.06); ball('KettleTop',(0,.8,0),(.18,.07,.18),'Metal')
        capsule('Spout',(.12,.66,-.05),(.28,.78,-.12),.03,'Metal'); tube('KHandle',[(-.12,.85,0),(0,.97,0),(.12,.85,0)],.015,'Dark')
        for k in range(3): ball('Puff',(.32+k*.05,.86+k*.1,-.14),(.05+.02*k,.04+.02*k,.05+.02*k),'White',10)
    else:
        cyl('Ash',(0,.54,0),.25,.04,'Dust',bev=0)
        cyl('Kettle',(.42,.12,-.2),.18,.22,'Metal',axis='x',bev=.06)
        box('Crack',(.2,.3,-.24),(.02,.25,.01),'Dark',0,rot=(0,-30,15))

KIT=[('ToolCart',tool_cart),('MetalLocker',metal_locker),('FireExtinguisher',fire_extinguisher),('AirCompressor',air_compressor),
     ('TireStack',tire_stack),('OldMattress',old_mattress),('SpringHorse',spring_horse),
     ('WaterCooler',water_cooler),('WaterBarrel',water_barrel),('LeakyPipe',leaky_pipe),
     ('ElectricPanel',electric_panel),('BatteryBank',battery_bank),('ArcadeMachine',arcade_machine),
     ('Grill',grill),('Heater',heater),('CharcoalStove',charcoal_stove)]
for name,fn in KIT:
    fn(False); export('FI_Ap_'+name,'disguised source - full')
    fn(True);  export('FI_Ap_'+name+'_Drained','disguised source - drained (shown while recovering)')
# lay the showcase out as pairs: full in front, drained right behind it
for o in scene.objects:
    if o.name.startswith('KIT_FI_Ap_'):
        i=[n for n,_ in KIT].index(o.name[10:].split('__')[0].replace('_Drained',''))
        o.location=(-(i%8)*2.0,-(0 if '_Drained' not in o.name else -2.2)-(i//8)*5.0,0)
json.dump(manifest,open(OUT+'/FactoryAppliances_manifest.json','w'),indent=1)
bpy.ops.wm.save_as_mainfile(filepath=ROOT+'/ArtSource/Blender/OOPS_FactoryAppliances.blend')
print('APPLIANCES',len(manifest))
