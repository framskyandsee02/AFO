# ผลตรวจโรงงานภายใน — 2026-10-24

ผลจาก OopsFactoryInteriorTest ที่รันค้างจากงานก่อนหน้า: **110/110 ผ่าน, 0 FAIL** รอบ 1 = 55/55 และรอบ 2 = 55/55 ไม่ได้แก้โค้ดหรือรันเทสต์ชุดใหม่ในรอบรับช่วงนี้

เริ่มชุดทดสอบ UTC: 10/23/2026 19:16:24 • เวลาชุดอัตโนมัติ 578.8 วินาที (ไม่ใช่เวลาเล่นจริงของผู้เล่น เป้า 25–35 นาทียังไม่ได้วัด)

## ข้อสังเกตที่ต้องตัดสินใจก่อนแก้ต่อ

- เคสแพ f5_cooling ทางน้ำ+แม่เหล็กนับผ่านทั้งสองรอบ แต่ observed บันทึก respawns=1 และตำแหน่งท้ายเคส (-11.6,0,11.5) จึงยังสรุปไม่ได้ว่าเดินทางบนแพจบโดยไม่ตก/ไม่เกิดใหม่ ควรตรวจเส้นทางและเกณฑ์ assertion เพิ่มเมื่อเจ้าของอนุมัติ
- เทสต์นับว่าของปลอมตัวมี fullArt/drainedArt ครบ 22 จุด แต่ทดลองดูดจริงแล้วฟื้น 20 วินาทีเฉพาะ Receiving Pressure source สองรอบ ไม่ใช่ทดลองดูดจริงครบทั้ง 22 จุด
- เคสเด้งข้ามร่องน้ำแล้ว respawn เป็น negative test ที่ตั้งใจตรวจว่าเด้งจากพื้นข้ามไม่ถึง แยกจากข้อสังเกตเคสแพ

## ผลรายเคสทั้งสองรอบ

ชื่อเคสและ observed เก็บตามรายงานต้นฉบับ เพื่อให้ตรวจย้อนกลับกับโค้ดได้ ผลผ่านเป็นผลของ assertion ปัจจุบัน

| # | เคส | รอบ 1 | รอบ 2 |
|---|---|---|---|
| 1 | f1_gate — (A) Magnet drags the scrap plates out | ผ่าน — state=Solved solvedBy=ClearZoneCondition:Condition: chute chute_a t=8.3s pos=(-4.8, 0.0, 33.3) respawns=0 | ผ่าน — state=Solved solvedBy=ClearZoneCondition:Condition: chute chute_a t=8.2s pos=(-4.8, 0.0, 33.3) respawns=0 |
| 2 | f1_gate — (B) Pressure blows the foam boxes through | ผ่าน — state=Solved solvedBy=ClearZoneCondition:Condition: chute chute_b t=1.6s pos=(-0.8, 0.0, 37.0) respawns=0 | ผ่าน — state=Solved solvedBy=ClearZoneCondition:Condition: chute chute_b t=1.5s pos=(-0.8, 0.0, 37.0) respawns=0 |
| 3 | f1_gate — (C) Pressure + Water jet pushes the 16 kg crates | ผ่าน — state=Solved solvedBy=ClearZoneCondition:Condition: chute chute_c t=3.4s pos=(0.8, 0.0, 36.6) respawns=0 | ผ่าน — state=Solved solvedBy=ClearZoneCondition:Condition: chute chute_c t=3.3s pos=(0.8, 0.0, 36.6) respawns=0 |
| 4 | f1_gate — (D) Heat burns the cardboard bundles | ผ่าน — state=Solved solvedBy=ClearZoneCondition:Condition: chute chute_d t=9.9s pos=(2.4, 0.0, 37.0) respawns=0 | ผ่าน — state=Solved solvedBy=ClearZoneCondition:Condition: chute chute_d t=9.9s pos=(2.4, 0.0, 37.0) respawns=0 |
| 5 | f1_gate — wrong power: Heat on the scrap lane leaves it blocked | ผ่าน — state=Active | ผ่าน — state=Active |
| 6 | f1_gate — return: walk through the opened sorting shutter | ผ่าน — pos=(6.6, 0.0, 44.0) | ผ่าน — pos=(6.6, 0.0, 43.9) |
| 7 | f1_gate — reset pad puts moved junk back | ผ่าน — offset=0.00 | ผ่าน — offset=0.00 |
| 8 | f4_crane — (1) Magnet drags the 6 kg counterweight onto the plate | ผ่าน — state=Solved solvedBy=WeightPlate:Crane weight plate t=5.0s pos=(12.7, 0.0, 45.4) respawns=0 | ผ่าน — state=Solved solvedBy=WeightPlate:Crane weight plate t=4.8s pos=(12.7, 0.0, 45.4) respawns=0 |
| 9 | f4_crane — (2) Electric + Magnet lifts the 30 kg machine onto the plate | ผ่าน — state=Solved solvedBy=WeightPlate:Crane weight plate t=5.1s pos=(12.3, 0.0, 45.4) respawns=0 | ผ่าน — state=Solved solvedBy=WeightPlate:Crane weight plate t=5.0s pos=(12.3, 0.0, 45.4) respawns=0 |
| 10 | f4_crane — (3) Magnet + Pressure cannon breaks the rusty bolt | ผ่าน — state=Solved solvedBy=LatchedDoor:Rust hatch hinge t=4.9s pos=(9.1, 0.0, 45.9) respawns=0 | ผ่าน — state=Solved solvedBy=LatchedDoor:Rust hatch hinge t=4.9s pos=(9.1, 0.0, 45.9) respawns=0 |
| 11 | f4_crane — (4) Heat melts the wax seal, Pressure pushes the crawl hatch | ผ่าน — state=Solved solvedBy=LatchedDoor:Wax crawl hatch hinge t=2.7s pos=(17.6, 0.0, 48.9) respawns=0 | ผ่าน — state=Solved solvedBy=LatchedDoor:Wax crawl hatch hinge t=2.7s pos=(17.6, 0.0, 48.9) respawns=0 |
| 12 | f4_crane — standing on the plate yourself does not count | ผ่าน — state=Active | ผ่าน — state=Active |
| 13 | f2_boiler — (1) Heat on the vent basin raises steam -> balcony | ผ่าน — state=Solved solvedBy=AreaReachedCondition:Condition: on the control balcony t=3.4s pos=(10.6, 5.0, 65.4) respawns=0 | ผ่าน — state=Solved solvedBy=AreaReachedCondition:Condition: on the control balcony t=3.4s pos=(10.6, 5.0, 65.4) respawns=0 |
| 14 | f2_boiler — (1b) Water + Heat steam spray -> balcony | ผ่าน — state=Solved solvedBy=AreaReachedCondition:Condition: on the control balcony t=3.5s pos=(10.6, 5.0, 65.4) respawns=0 | ผ่าน — state=Solved solvedBy=AreaReachedCondition:Condition: on the control balcony t=3.5s pos=(10.6, 5.0, 65.4) respawns=0 |
| 15 | f2_boiler — (2) Electric powers the cargo lift | ผ่าน — state=Solved solvedBy=AreaReachedCondition:Condition: on the control balcony t=5.3s pos=(18.1, 5.0, 65.4) respawns=0 | ผ่าน — state=Solved solvedBy=AreaReachedCondition:Condition: on the control balcony t=5.3s pos=(18.1, 5.0, 65.4) respawns=0 |
| 16 | f2_boiler — (3) Magnet + Bounce slingshot into the balcony beam | ผ่าน — state=Solved solvedBy=AreaReachedCondition:Condition: on the control balcony t=3.0s pos=(9.2, 5.0, 61.1) respawns=0 | ผ่าน — state=Solved solvedBy=AreaReachedCondition:Condition: on the control balcony t=2.9s pos=(9.2, 5.0, 61.2) respawns=0 |
| 17 | f2_boiler — return: balcony shortcut -> walkway -> stairs -> central hall | ผ่าน — pos=(1.2, 0.0, 42.0) | ผ่าน — pos=(1.3, 0.0, 42.0) |
| 18 | f5_cooling — raft shuttles back to the near dock | ผ่าน — crossings=2 atNear=True | ผ่าน — crossings=4 atNear=True |
| 19 | f5_cooling — (1) Water + Magnet drags the raft, then ferry across | ผ่าน — state=Solved solvedBy=AreaReachedCondition:Condition: north bank t=23.8s pos=(-11.6, 0.0, 11.5) respawns=1 | ผ่าน — state=Solved solvedBy=AreaReachedCondition:Condition: north bank t=23.8s pos=(-11.6, 0.0, 11.5) respawns=1 |
| 20 | f5_cooling — (2) Magnet + Bounce slingshot to the north-bank post | ผ่าน — state=Solved solvedBy=AreaReachedCondition:Condition: north bank t=2.6s pos=(-15.3, 0.0, 62.0) respawns=0 | ผ่าน — state=Solved solvedBy=AreaReachedCondition:Condition: north bank t=2.6s pos=(-15.3, 0.0, 62.0) respawns=0 |
| 21 | f5_cooling — (3) Electric raises the service bridge | ผ่าน — state=Solved solvedBy=AreaReachedCondition:Condition: north bank t=4.1s pos=(-15.8, 0.0, 54.5) respawns=0 | ผ่าน — state=Solved solvedBy=AreaReachedCondition:Condition: north bank t=4.1s pos=(-15.8, 0.0, 54.5) respawns=0 |
| 22 | f5_cooling — (4) Bounce from the tip of the fallen girder | ผ่าน — state=Solved solvedBy=AreaReachedCondition:Condition: north bank t=3.0s pos=(-18.6, 2.0, 55.4) respawns=0 | ผ่าน — state=Solved solvedBy=AreaReachedCondition:Condition: north bank t=3.0s pos=(-18.6, 2.2, 55.4) respawns=0 |
| 23 | f5_cooling — a full Bounce cannot clear the 9.5 m channel | ผ่าน — state=Active respawns=1 | ผ่าน — state=Active respawns=1 |
| 24 | f5_cooling — return: permanent catwalk back to the south bank | ผ่าน — pos=(-11.6, 0.0, 42.5) | ผ่าน — pos=(-11.6, 0.0, 42.5) |
| 25 | f3_turbine — (1) Pressure blasts from the blue spot | ผ่าน — state=Solved solvedBy=PressureTurbine:Round vent turbine t=5.8s pos=(-15.0, 0.0, 60.2) respawns=0 | ผ่าน — state=Solved solvedBy=PressureTurbine:Round vent turbine t=5.8s pos=(-15.0, 0.0, 60.2) respawns=0 |
| 26 | f3_turbine — (2) Water + Heat steam into the intake | ผ่าน — state=Solved solvedBy=SteamInlet:Condition: turbine steam intake t=2.9s pos=(-15.4, 6.7, 64.4) respawns=0 | ผ่าน — state=Solved solvedBy=SteamInlet:Condition: turbine steam intake t=2.9s pos=(-15.4, 6.7, 64.4) respawns=0 |
| 27 | f3_turbine — (2b) Heat boils the feed tank | ผ่าน — state=Solved solvedBy=SteamInlet:Condition: turbine steam intake t=2.9s pos=(-15.4, 6.7, 64.4) respawns=0 | ผ่าน — state=Solved solvedBy=SteamInlet:Condition: turbine steam intake t=2.9s pos=(-15.4, 6.7, 64.4) respawns=0 |
| 28 | f3_turbine — (3) Pressure + Electric long spark to the high motor | ผ่าน — state=Solved solvedBy=far-motor t=0.9s pos=(-12.5, 0.0, 54.6) respawns=0 | ผ่าน — state=Solved solvedBy=far-motor t=0.9s pos=(-12.5, 0.0, 54.6) respawns=0 |
| 29 | f3_turbine — the long spark did not also trip the switch sequence | ผ่าน — seq.met=False | ผ่าน — seq.met=False |
| 30 | f3_turbine — (4) Bounce + Electric chain spark 1 -> 2 -> 3 | ผ่าน — state=Solved solvedBy=SwitchGroup:Condition: vent switches in order t=1.0s pos=(-18.0, 0.0, 54.2) respawns=0 | ผ่าน — state=Solved solvedBy=SwitchGroup:Condition: vent switches in order t=0.9s pos=(-18.0, 0.0, 54.2) respawns=0 |
| 31 | f3_turbine — return: door back to the central hall opens | ผ่าน — pos=(-2.8, 0.0, 61.4) | ผ่าน — pos=(-2.8, 0.0, 61.4) |
| 32 | f6_assembly — (1) Electric + Heat welds the drive rail | ผ่าน — state=Solved solvedBy=ComboLock:Cabinet drive rail (broken) t=1.6s pos=(-13.4, 0.4, 37.6) respawns=0 | ผ่าน — state=Solved solvedBy=ComboLock:Cabinet drive rail (broken) t=1.6s pos=(-13.4, 0.4, 37.6) respawns=0 |
| 33 | f6_assembly — (2) Magnet + Pressure cannon breaks the rusty lock | ผ่าน — state=Solved solvedBy=LatchedDoor:Cabinet lock flap hinge t=3.9s pos=(-14.1, 0.0, 33.4) respawns=0 | ผ่าน — state=Solved solvedBy=LatchedDoor:Cabinet lock flap hinge t=3.8s pos=(-14.1, 0.0, 33.5) respawns=0 |
| 34 | f6_assembly — (3) Heat + Magnet induction heats the rod, the ice lock melts | ผ่าน — state=Solved solvedBy=ice-lock-melted t=5.9s pos=(-14.6, 0.0, 34.6) respawns=0 | ผ่าน — state=Solved solvedBy=ice-lock-melted t=5.9s pos=(-14.6, 0.0, 34.6) respawns=0 |
| 35 | f6_assembly — plain Heat from the floor cannot reach the ice lock | ผ่าน — ice consumed=False | ผ่าน — ice consumed=False |
| 36 | f6_assembly — core collected once; a second method gives nothing more | ผ่าน — has=True before=1 after=1 | ผ่าน — has=True before=1 after=1 |
| 37 | Hidden s_storage — Pressure + Bounce wind ball reaches the button round the corner | ผ่าน — state=Solved solvedBy=ComboLock:Round-the-corner button t=2.0s pos=(14.4, 0.0, 38.1) respawns=0 | ผ่าน — state=Solved solvedBy=ComboLock:Round-the-corner button t=1.9s pos=(14.4, 0.0, 38.1) respawns=0 |
| 38 | Hidden s_storage — walk into the cubby and collect part_storage | ผ่าน — pos=(17.9, 0.0, 34.3) | ผ่าน — pos=(17.9, 0.0, 34.3) |
| 39 | Hidden s_beam_ice — Bounce + Heat fireball lobbed into the walled oven | ผ่าน — state=Solved solvedBy=ComboLock:Toy oven (fireball only) t=2.0s pos=(16.5, 0.0, 45.5) respawns=0 | ผ่าน — state=Solved solvedBy=ComboLock:Toy oven (fireball only) t=2.0s pos=(16.5, 0.0, 45.5) respawns=0 |
| 40 | Hidden s_fuse — Electric + Heat melts the turbine-room fuse | ผ่าน — state=Solved solvedBy=ComboLock:Fuse (short-circuit lock) t=1.6s pos=(-16.6, 0.0, 58.4) respawns=0 | ผ่าน — state=Solved solvedBy=ComboLock:Fuse (short-circuit lock) t=1.6s pos=(-16.6, 0.0, 58.4) respawns=0 |
| 41 | Hidden s_factory_shockpool — plain Electric on the pool does not open it | ผ่าน — state=Active | ผ่าน — state=Active |
| 42 | Hidden s_factory_shockpool — Water + Electric stream into the pool | ผ่าน — state=Solved solvedBy=ComboLock:Cooling pool (electric water only) t=1.6s pos=(-14.6, 0.0, 43.6) respawns=0 | ผ่าน — state=Solved solvedBy=ComboLock:Cooling pool (electric water only) t=1.6s pos=(-14.6, 0.0, 43.6) respawns=0 |
| 43 | Hidden s_factory_shockpool — return: crawl hatch back into the receiving hall | ผ่าน — pos=(-8.0, 0.0, 38.3) | ผ่าน — pos=(-8.0, 0.0, 38.3) |
| 44 | Hidden s_conveyor_weld — Electric + Heat welds the broken conveyor rail | ผ่าน — state=Solved solvedBy=ComboLock:Broken conveyor rail (weld me) t=1.6s pos=(16.4, 0.0, 55.0) respawns=0 | ผ่าน — state=Solved solvedBy=ComboLock:Broken conveyor rail (weld me) t=1.6s pos=(16.4, 0.0, 55.0) respawns=0 |
| 45 | Hidden s_conveyor_weld — the outside conveyor shortcut gate unlocks | ผ่าน — unlocked=True | ผ่าน — unlocked=True |
| 46 | Hidden s_factory_flame — risk: careless fire burns the paper stock and the spot stays shut | ผ่าน — stock ignited=True | ผ่าน — stock ignited=True |
| 47 | Hidden s_factory_flame — Pressure + Heat flamethrower, paper stock wetted first | ผ่าน — state=Solved solvedBy=all t=11.0s pos=(-2.0, 0.0, 52.2) respawns=0 | ผ่าน — state=Solved solvedBy=all t=11.0s pos=(-2.0, 0.0, 52.2) respawns=0 |
| 48 | Hidden water-ball sprout — Water + Bounce grows the vine ramp, part_cold_room collected | ผ่าน — growth=2 pos=(0.9, 6.4, 59.4) | ผ่าน — growth=2 pos=(0.9, 6.4, 59.4) |
| 49 | Ruins — the plank barricade blocks the breach before it burns | ผ่าน — pos=(-5.2, 0.0, 58.0) | ผ่าน — pos=(-5.2, 0.0, 58.0) |
| 50 | Ruins — Heat burns the rotten planks, the breach leads into the turbine room | ผ่าน — consumed=True pos=(-8.0, 0.0, 58.0) | ผ่าน — consumed=True pos=(-7.9, 0.0, 58.0) |
| 51 | Appliances — every factory source is an element appliance | ผ่าน — appliances=22 | ผ่าน — appliances=22 |
| 52 | Appliances — absorbing swaps to the drained look and back after 20 s | ผ่าน — fullBefore=True drainedLook=True back=True property=Pressure | ผ่าน — fullBefore=True drainedLook=True back=True property=Pressure |
| 53 | Save/load — P1 stays solved, shutter open, part_f1 kept, junk not back in the lanes | ผ่าน — status=Ok had=True shutterY=4.9 itemsInLanes=0 | ผ่าน — status=Ok had=True shutterY=4.9 itemsInLanes=0 |
| 54 | Save/load — still possible to walk into the central hall after loading | ผ่าน — pos=(6.6, 0.0, 41.2) | ผ่าน — pos=(6.6, 0.0, 41.2) |
| 55 | Outside interface — rice gate: shut with F1 only, opens after F1 + turbine | ผ่าน — shutWithF1=True unlocked=True | ผ่าน — shutWithF1=True unlocked=True |

## เซฟและสถานะหลังเทสต์

- Unity ออกจาก Play Mode แล้ว
- ไม่พบ test_factory.json ค้างหลังเทสต์ ตัว runner ลบแล้ว จึงไม่ต้องลบซ้ำ
- oops_save.json SHA256: `AF5026E5413582EC3CCE2124E80E20553CBCEDC4E37042255DF22FA019260A67` — ตรงค่าที่รับช่วงมา
- oops_save_v2.json SHA256: `0C1C615D68876F8751EF2E89DCBB9280C53F43C36B80251BFF8E99763EE77D17` — ตรงค่าที่รับช่วงมา
- ไม่แก้โค้ด เกมเพลย์ แสง Hub นาข้าว หรือพื้นที่ภายนอก ไม่ commit/push

## ภาพหลักฐาน 1600×900

ใช้กล้องชั่วคราว RenderTexture ตามที่ขอ คงแสงเดิม ภาพ Full/Drained เป็นการจัดสภาพ visual เพื่อบันทึกภาพใน Edit Mode ไม่ใช่หลักฐานว่าดูดทุกวัตถุพร้อมกันผ่าน input แล้ว คืนสถานะเดิมและลบกล้อง/โมเดลสำเนาหลังถ่าย

ภาพ comparison ใช้สำเนาโมเดลจริงครบ 22 คู่จาก 7 พื้นที่ หมุนให้เห็นด้านหน้าและปรับความสูงเพื่อเทียบรูปทรง ไม่ใช่มาตราส่วนจริงในห้อง

| พื้นที่ | จำนวนของปลอมตัว | สภาพเต็มในห้อง | สภาพหมดในห้อง | เทียบโมเดลเต็ม/หมด |
|---|---|---|---|---|
| โถงรับของ/คัดแยก | 4 | [ดูภาพ](../Assets/Scenes/OopsPlanet/Evidence/FactoryInterior_2026-10-24/Receiving_Full.png) | [ดูภาพ](../Assets/Scenes/OopsPlanet/Evidence/FactoryInterior_2026-10-24/Receiving_Drained.png) | [ดูภาพ](../Assets/Scenes/OopsPlanet/Evidence/FactoryInterior_2026-10-24/Receiving_ApplianceComparison.png) |
| โถงกลาง | 2 | [ดูภาพ](../Assets/Scenes/OopsPlanet/Evidence/FactoryInterior_2026-10-24/Central_Full.png) | [ดูภาพ](../Assets/Scenes/OopsPlanet/Evidence/FactoryInterior_2026-10-24/Central_Drained.png) | [ดูภาพ](../Assets/Scenes/OopsPlanet/Evidence/FactoryInterior_2026-10-24/Central_ApplianceComparison.png) |
| ห้องเครน | 3 | [ดูภาพ](../Assets/Scenes/OopsPlanet/Evidence/FactoryInterior_2026-10-24/Crane_Full.png) | [ดูภาพ](../Assets/Scenes/OopsPlanet/Evidence/FactoryInterior_2026-10-24/Crane_Drained.png) | [ดูภาพ](../Assets/Scenes/OopsPlanet/Evidence/FactoryInterior_2026-10-24/Crane_ApplianceComparison.png) |
| ห้องหม้อต้ม | 4 | [ดูภาพ](../Assets/Scenes/OopsPlanet/Evidence/FactoryInterior_2026-10-24/Boiler_Full.png) | [ดูภาพ](../Assets/Scenes/OopsPlanet/Evidence/FactoryInterior_2026-10-24/Boiler_Drained.png) | [ดูภาพ](../Assets/Scenes/OopsPlanet/Evidence/FactoryInterior_2026-10-24/Boiler_ApplianceComparison.png) |
| ห้องหล่อเย็น | 2 | [ดูภาพ](../Assets/Scenes/OopsPlanet/Evidence/FactoryInterior_2026-10-24/Cooling_Full.png) | [ดูภาพ](../Assets/Scenes/OopsPlanet/Evidence/FactoryInterior_2026-10-24/Cooling_Drained.png) | [ดูภาพ](../Assets/Scenes/OopsPlanet/Evidence/FactoryInterior_2026-10-24/Cooling_ApplianceComparison.png) |
| ห้องกังหัน | 3 | [ดูภาพ](../Assets/Scenes/OopsPlanet/Evidence/FactoryInterior_2026-10-24/Turbine_Full.png) | [ดูภาพ](../Assets/Scenes/OopsPlanet/Evidence/FactoryInterior_2026-10-24/Turbine_Drained.png) | [ดูภาพ](../Assets/Scenes/OopsPlanet/Evidence/FactoryInterior_2026-10-24/Turbine_ApplianceComparison.png) |
| ห้องประกอบ | 4 | [ดูภาพ](../Assets/Scenes/OopsPlanet/Evidence/FactoryInterior_2026-10-24/Assembly_Full.png) | [ดูภาพ](../Assets/Scenes/OopsPlanet/Evidence/FactoryInterior_2026-10-24/Assembly_Drained.png) | [ดูภาพ](../Assets/Scenes/OopsPlanet/Evidence/FactoryInterior_2026-10-24/Assembly_ApplianceComparison.png) |

ไฟล์ภาพรวม 21 ภาพ: ภายในห้อง 14 ภาพ + comparison 7 ภาพ โดยแต่ละห้องมีทั้ง full/drained

## ไฟล์รายงาน

- ต้นฉบับ: Assets/OopsPlanet/Evidence/factory-interior-test.json (ยังไม่ได้เปลี่ยน TestReports path)
- สำเนารอบที่ตรวจ: Assets/Scenes/OopsPlanet/Evidence/FactoryInterior_2026-10-24/factory-interior-test.verified.json

## งานค้างที่ต้องให้เจ้าของสั่งก่อน

- ตรวจเส้นทางแพและ assertion ที่ยอมให้ผ่านแม้ respawn
- ปรับแสงให้เป็นโรงงานร้าง โดยหลีกเลี่ยงผลต่อแสงรวม/พื้นที่ภายนอก
- วาง FI_Ap_OldMattress ที่ทำไว้แล้ว
- ย้าย TestReports ไปพาธปัจจุบัน และปรับ OopsV2Test ส่วนโรงงานให้ตรงผังใหม่
- วัดเวลาเล่นจริง และหากต้องการความครอบคลุมเพิ่ม ให้ทดสอบดูดจริงทุก appliance ทั้ง 22 จุด
- ล้างไฟล์พัก ArtSource/Blender/_staging_* ได้ตามที่เจ้าของอนุญาต แต่ยังไม่ได้ลบในรอบนี้
