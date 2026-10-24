# OOPS! PLANET — README สำหรับ AI ที่มาทำต่อ

**งานภาพล่าสุด 2026-10-23:** เจ้าของอนุมัติ Pino + โมเดลเดิมเป็น ref สำหรับทั้งแมพแล้ว ฉากหลัก V2 ใช้ Pino และ WorldPolishArt (revision 3) มี Blender ใหม่ 12 ชุด / แหล่งพลัง 34 จุด ผล EditMode 14/14, campaign + Pino 49/49 ครบ 15 คอมโบ, art smoke 27/27 ดู `Docs/WORLD_POLISH_2026-10-23_TH.md` พาธปัจจุบันคือ `Assets/Scenes/OopsPlanet/` แทน `Assets/OopsPlanet/` ย้ายกองเหล็ก R1 ไป (37,0,12) เพื่อไม่ขวางทางฟาง งานรอบนี้ยังไม่ commit/push

**กติกาล่าสุดจากเจ้าของ: 15 คอมโบไม่เรียงลำดับ** ทุกคู่ของ 6 พลังผสมได้ผลเดียว F เดี่ยว / Q เลือกเดี่ยว / G ผสม; ให้ยึด `Docs/COMBOS_15_TH.md` แทนกติกา 30 ช่องเดิม ผลล่าสุด EditMode 14/14, V2 49/49, คอมโบ 15/15 รวม Pino ผ่านแล้ว งานยังไม่ commit/push


เอกสารนี้เขียนให้ AI agent ตัวถัดไปอ่านก่อนเริ่มงาน (สถานะ ณ 2026-10-23)
คนทำงานจริงควรอ่าน `README.md` (วิธีเปิดโปรเจกต์ / build) และ `Docs/HANDOFF_TH.md` (วิธีเพิ่มพลัง/ปริศนา) ประกอบ
**ต้นฉบับของการออกแบบ v2 คือ `Docs/DESIGN_V2_TH.md`** (เจ้าของโปรเจกต์อนุมัติแล้ว) ถ้าข้อมูลในไฟล์นี้ขัดกับเอกสารนั้น ให้ยึดเอกสารออกแบบ

---

## 1. กติกาของเจ้าของโปรเจกต์ (สำคัญที่สุด — ต้องทำตาม)
1. **คุยและรายงานเป็นภาษาไทย** สรุปเป็นจุด ๆ บอกตรง ๆ ว่าอะไรผ่าน อะไรไม่ผ่าน อะไรยังไม่ได้ทำ
2. **ทำเฉพาะขั้นที่ได้รับอนุมัติ** แล้วรายงาน ถ้าไม่แน่ใจขอบเขตให้ถามก่อน
3. **อัปเดต 2026-10-23:** เจ้าของอนุญาต "แก้และเทสต์ต่อเองจนผ่าน" สำหรับงานแก้ V2 รอบนี้ และอนุญาตสำรองแล้ว Rebuild ฉาก V2 แล้ว สำหรับงานใหม่ที่อยู่นอกขอบเขตนี้ให้ตกลงขอบเขตก่อน
4. **ห้ามทำ Git action ใด ๆ** (commit / push / pull / merge / branch / reset / revert) ถ้าไม่มีคำสั่ง
   - ถ้าสั่ง commit: ห้ามรวม `*.zip`, โฟลเดอร์ prompt (`OOPS_PLANET_Unity_Blender_Prompt_Package_UPDATED/`, `WOBBLE_WORKS_Phase_Prompts_P0-P9/`), `*.blend1`, `Assets/OopsPlanet/Evidence/build-test-log.txt`
   - `ProjectSettings/UnityConnectSettings.asset` ต้องเป็น `m_Enabled: 0`
5. **ห้ามเปลี่ยน project settings / architecture / workflow** โดยไม่ขออนุมัติ (รวมถึง Build Settings scene list)
6. **ห้ามแตะเซฟจริงของผู้เล่น** `%USERPROFILE%\AppData\LocalLow\DefaultCompany\AFO\oops_save.json` (hash ปัจจุบัน `af5026e5…`)
   เทสต์ต้องใช้ slot แยก (`test_run`, `test_v2`, `p05_*`, `editmode_save_v2_test`) และลบทิ้งหลังใช้
7. **โมเดล 3D ทุกชิ้นต้องขอ reference (รูป/สเก็ตช์) จากเจ้าของก่อนเสมอ** ระหว่างรอใช้ greybox primitive ได้ แต่ต้องบอกว่าเป็นของชั่วคราว
8. **เช็ค `EditorApplication.isPlaying` ก่อน compile/refresh ทุกครั้ง** — เจ้าของชอบลองเล่นใน Play mode ถ้ากำลังเล่นอยู่ให้ **ถามก่อน** กดหยุด (เคยเกิด error เพราะ recompile ระหว่างเล่น)
9. การดาวน์โหลดไฟล์จากเน็ตต้องขออนุญาตก่อน

---

## 2. สภาพแวดล้อม
| | |
|---|---|
| Unity | 6000.3.25f1, URP 17.3, Input System 1.20 (อ่าน device ตรง ไม่มี action map), Test Framework 1.6 |
| เครื่องมือ AI | Unity MCP (`com.coplaydev.unity-mcp#main`), Blender 5.2.2 ผ่าน Blender MCP |
| OS / shell | Windows 11, Git Bash + PowerShell, Git LFS (`.fbx`, `.blend`) |
| ฟอนต์ UI | Noto Sans Thai (OFL) อยู่ใน `Assets/OopsPlanet/Fonts` |

**กับดักที่เจอมาแล้ว**
- Git Bash heredoc + `python -` กิน `\\` และอาจทำภาษาไทยเพี้ยน → เขียนสคริปต์ Python ลงไฟล์ด้วย Write tool แล้วรันด้วย `PYTHONUTF8=1 python file.py`
- `rm` ที่มีตัวแปร path ถูกบล็อก ต้องเขียน path ตรงตัว
- `sleep` ยาว ๆ ใน Bash ถูกบล็อก ให้ใช้ loop `until [ file -nt marker ]; do sleep 5; done` รอไฟล์รายงานเทสต์
- `mcp__unityMCP__execute_code` ต้องส่ง `action: "execute"` และใช้ `UnityEngine.Object` แบบเต็ม (`Object` กำกวม)
- หลังเขียนไฟล์ .cs Unity อาจ auto-refresh เอง; ถ้า MCP ตอบ "No Unity Editor instances" ให้รอแล้วลองใหม่
- `manage_camera screenshot` เขียนไฟล์ลง `Assets/Screenshots/` — ถ้าใช้ต้องลบไฟล์ที่ตัวเองสร้าง
- TextMesh ป้าย: `yaw` = ทิศที่ผู้อ่าน **มอง** (ผู้อ่านมอง +z → yaw 0, มอง +x → yaw 90) ผิดแล้วตัวหนังสือกลับด้าน
- ตั้งค่า field บน **prefab instance** จาก editor script ต้องเรียก `PrefabUtility.RecordPrefabInstancePropertyModifications` ไม่งั้นค่าหายตอน save (เคยทำ `powerLabel` หาย)

---

## 3. ประวัติงาน
| Commit | งาน |
|---|---|
| `54cdca0` … `e71ef45` | P01–P08: Prototype 01 (Hub + โรงงาน + นาข้าว, 3 พลัง, 3 ปริศนา, เซฟ, UI ไทย, build PC) |
| `d2eb952` | Design v2 ขั้น A: `Docs/DESIGN_V2_TH.md` |
| รอบส่ง master 2026-10-23 | v2 ขั้น B–E + แก้ปริศนา + HUD + Soft World art และเทสต์ (ดู §6–7) |

กำลังส่งงานที่ผ่านการตรวจแล้วไป origin/master ตามคำสั่งเจ้าของวันที่ 2026-10-23

**บรีฟ v2 จากเจ้าของ:** ของดูดได้กลมกลืนกับฉาก • พลังมากขึ้นจากสิ่งของในฉาก • ปริศนาแก้ได้หลายวิธี • ทุกพลังผสมกันได้และมีประโยชน์ • แมพที่เหมาะกับระบบ
**ค่าที่ตัดสินแล้ว:** 6 พลัง (แม่เหล็ก แรงดัน เด้ง น้ำ ไฟฟ้า ความร้อน) • ถือได้ 2 • Hub + 2 โซน • ของสำคัญฟื้นเองใน ~20 วิ + ตอน respawn • โหมดดมพลัง = คลิกขวาค้าง (free-look ย้ายไปปุ่มกลาง) • นาข้าวเปิดเมื่อแก้ F1 + อีก 1 จาก F2/F3 • จบเดโมเมื่อซ่อมยานครบ (ติดตั้ง 4 ชิ้น มีจากทุกโซน)
**Workflow ล่าสุดที่เจ้าของสั่ง:** สร้างขั้น C→E ต่อเนื่อง (ทำแล้ว) แล้วเทสต์รวม (ทำแล้ว) → แก้เคสที่เหลือและรันทดสอบรวมผ่านแล้ว (§7)

---

## 4. Scene และการ build scene
ทุก scene **สร้างจากโค้ด** ห้ามแก้ scene ด้วยมือ (Rebuild จะเขียนทับ)
| Scene | Builder | เมนู |
|---|---|---|
| `Assets/OopsPlanet/Scenes/OopsPlanet_Prototype01.unity` (ฐานเดิม + Element Lab สำหรับ dev ที่ (-60,0,-60)) | `Assets/OopsPlanet/Editor/OopsPrototypeBuilder.cs` | Tools → OOPS Planet → Rebuild Prototype 01 / Validate Prototype 01 |
| `Assets/OopsPlanet/Scenes/OopsPlanet_V2.unity` (**แมพ v2**) | `Assets/OopsPlanet/Editor/OopsV2Builder.cs` | Tools → OOPS Planet → V2 → Rebuild V2 Greybox Scene / Validate V2 Scene / Capture V2 Overview Image |

- V2 builder ต้องการ asset ที่ Prototype builder สร้างไว้ (Power_*, Combo_*, `ComboTable_V2`, `OOPS_PostFX`, `OOPS_SenseFX`) — ถ้าเริ่มจากศูนย์ให้ Rebuild Prototype 01 ก่อน
- ฉาก Prototype 01 **ไม่มี** `BlobElementPowers` (ตั้งใจ เพื่อให้ regression เดิมคงที่) จึงเล่นได้แค่ 3 พลังเดิม
- Build Settings ยังมีแค่ Prototype 01 (การเพิ่ม V2 ต้องขออนุมัติ)
- ภาพผังแมพ: `Assets/OopsPlanet/Evidence/v2-overview.png`

---

## 5. สถาปัตยกรรม v2 (ไฟล์สำคัญ)
สคริปต์แกน BLOB อยู่ใน `Assets/WobbleWorks/Scripts/` (ชื่อโฟลเดอร์เก่า แต่เป็นโค้ด OOPS) ที่เหลืออยู่ใน `Assets/OopsPlanet/Scripts/`

**ผู้เล่นและพลัง**
- `BlobPlayer.cs` — เดิน/กระโดด/เด้ง, 2 slot (`property` = หลัก F, `secondary` = เสริม G), ดูดซับ (E ค้าง), `clinging` (ไต่ผนัง), event `ColliderHit`, test hooks (`testMode`, `testMove`, `testAction`, `testSecondary`, `testJump`, `testSense`, `testAim`…)
- `BlobWorld.cs` — HUD IMGUI ภาษาไทย (ช่องพลัง 6, บรรทัดคอมโบ, สมุดพลัง J), เซฟ/โหลด, `Restart` (**Restart เรียก Save() ด้วย**), regen ของ source, `CompleteDemo`, `ship`
- `Powers/BlobPowers.cs` — แม่เหล็ก/แรงดัน + คอมโบเดิม 2 แบบ (ปืนแม่เหล็ก, พุ่งแรงดัน); ส่งต่อปุ่มให้ `BlobElementPowers`
- `Powers/BlobElementPowers.cs` — **พลังใหม่ + ครบ 30 ช่องคอมโบ** กติกา: F = ท่าทางของพลังหลักพาธาตุของพลังเสริม, G = ท่าทางของพลังเสริมเดี่ยว ๆ; สมุดพลัง (`discovered`, event `CellUsed`/`Discovered`)
- `Powers/ComboTable.cs` + `Data/ComboTable_V2.asset` — ตาราง 6×6 แบบเรียงลำดับ (30 ช่อง, id เช่น `"magnet+pressure"`)
- `Absorb/AbsorbableVisual.cs`, `Absorb/BlobSense.cs` — ประกาย, โหมดดมพลัง (ระยะ 12 ม.), ป้ายชื่อพลังขึ้นเฉพาะตอนดม

**ธาตุและวัสดุ** (`Scripts/Elements/`)
- `WorldMaterial` (โลหะ ฟาง ไม้ น้ำแข็ง ขี้ผึ้ง พืช โคลน น้ำ เครื่องจักร) + `ElementReaction` (ตาราง §5: ไฟลาม ≤3 ม., รอ 2 วิก่อนลาม, ดับเองใน 15 วิ; ไฟฟ้าวิ่งผ่านของนำไฟ)
- `ElementHit` (ส่งธาตุให้วัสดุ + `IElementReceiver`), `ElementProjectile`, `MagnetAnchor`, `ElementCarrier`, `SteamSystem` (เสาไอน้ำยก BLOB), `MaterialStateCondition`

**ปริศนาและความคืบหน้า**
- `Puzzles/PuzzleObjective.cs` — โหมด `All`/`Any`, `solvedBy`, `isSecret` (จุดซ่อนไม่นับใน "ฟื้นฟูโลก x/7"), `materials` (reset)
- `Scripts/Devices/` — `AreaReachedCondition`, `WeightPlate`, `LatchedDoor`, `ImpactBreak`, `PoweredLift`, `SteamInlet`, `ElectricSwitch` + `SwitchGroup`, `VineBridge`, `FerryRaft`, `WaterSkim`, `ComboLock`
- `Progress/ShipRepair.cs`, `ShipPart.cs` (ยาน + ชิ้นส่วน 15 ชิ้น), `ZoneGate.cs` (`requiresAnyOf`/`anyCount`), `SaveSystem.cs` (**SaveData v2**: + `discoveredCombos`, `shipParts`, `installedParts`; ไฟล์ v1 ถูกแปลงอัตโนมัติ; version อื่น = WrongVersion)
- ฉาก V2 ใช้ slot `oops_save_v2`

**ปริศนาในแมพ V2** (id → ทางแก้ ดูรายละเอียดใน builder และ `Docs/DESIGN_V2_TH.md` §6)
`h1_pit_exit`, `f1_gate`, `f2_boiler`, `f3_turbine`, `r1_sluice`, `r2_canal`, `r3_pump` + จุดซ่อน `s_hub_pads`, `s_hub_lights`, `s_storage`, `s_fuse`, `s_beam_ice`, `s_conveyor_weld`, `s_kiln` (+ ชิ้นส่วนบนหลังคาโรงเก็บของ, ห้องเย็น, หลังคาปั๊ม ที่ไม่มี objective)

---

## 6. เทสต์
**Play Mode** (กด Play ใน scene ที่ถูกต้องก่อน แล้วเลือกเมนู) รายงานเขียนลง `Assets/OopsPlanet/Evidence/*.json`
| Scene | เมนู | ผลล่าสุด |
|---|---|---|
| Prototype01 | Run P01-P04 Play Tests (sequence) | ✅ 27 / 24 / 26 / 29 |
| Prototype01 | Run P05 Progress Test (1) → หยุด Play → Play ใหม่ → (2) | ✅ 12 / 12 |
| Prototype01 | Run P06 Polish Test | ✅ 19/19 |
| Prototype01 | Run Stage B Element Lab Test | ✅ 21/21 |
| V2 | V2 → Run V2 Test (all solutions) → `v2-test.json` | ✅ **57/57** |
| — | EditMode (Test Runner) | ✅ 14/14 (`PowerComboResolverTests`, `ComboTableTests`, `SaveV2Tests`) |

เทสต์ V2 (`Scripts/OopsV2Test.cs`) มี 1 เคสต่อทางแก้ ทุกเคสเริ่มจาก `Restart()` แล้วตั้งพลังด้วย `SetProperties` และกดปุ่มผ่าน test hooks; ท้ายสุดเช็คว่าคอมโบถูกใช้ครบ 30 ช่อง

---

## 7. ผลแก้ V2 ล่าสุด — 2026-10-23

**แก้เคสที่ล้มเหลวเดิมครบแล้ว: V2 57/57, คอมโบ 30/30** รวมการแก้เทสต์เซฟและลมร้อนจากรอบก่อน
สลิง F2 ผ่านโดยไม่ respawn เทสต์ Case บังคับไม่เกิด respawn แล้ว
แก้ error MaterialPropertyBlock / ข้อมูลสีหายหลัง reload ใน WorldMaterial และ AbsorbableVisual ไม่พบ error ในรอบยืนยัน

Regression รอบนี้: EditMode 14/14, P03 26/26, P06 19/19, Element Lab 21/21 ไม่ได้รัน P01/P02/P04/P05 หรือ standalone build ใหม่
รายละเอียดสาเหตุ การแก้ และข้อจำกัด: `Docs/V2_FIXES_2026-10-23_TH.md`
สำเนาฉากก่อน Rebuild: `E:/!for WORK/GPT/OopsPlanet_V2.before-rebuild.unity`
HUD IMGUI ปรับให้เล็กลงแล้ว เหลือสองช่องพลัง รายละเอียดเต็มอยู่ TAB / J ยังไม่ได้ย้ายเป็น uGUI
งานรอบนี้รวมในการส่ง origin/master ตามคำสั่งเจ้าของ ไม่ได้เปลี่ยน Build Settings

**งานภาพแมพล่าสุด:** เจ้าของให้ ref เป็น Kirby รูปทรงกลมมน ทำแมพ Soft World รอบแรกใน Blender และใส่ใน Unity แล้ว ต้นฉบับ `ArtSource/Blender/OOPS_V2_SoftWorld.blend` พร้อมสคริปต์สร้างซ้ำ ตัว V2 builder ผูก `OopsSoftWorldArt.Apply()` แล้ว ผลหลังใส่งานภาพ V2 57/57, คอมโบ 30/30 รายละเอียด `Docs/SOFT_WORLD_ART_TH.md` งานโมเดลชุดอื่นยังต้องขอ ref ก่อนเสมอ

**สิ่งที่ยังไม่ได้ทำจากแผน v2**
- มาตรวัดมลพิษ (design §3.3 ข้อ 6)
- ร่องรอยรูปร่างบนตัว BLOB สำหรับ น้ำ/ไฟฟ้า/ความร้อน (ต้องขอ ref โมเดลก่อน)
- ขั้น F: art ตาม ref (§9 ของเอกสารออกแบบ), แสง, VFX/เสียงธาตุใหม่, HUD แบบ uGUI — **ต้องขอ reference ก่อนแตะโมเดล**
- ขั้น G: ทดสอบ Editor รวมผ่านแล้ว ยังเหลือคนนอกทดลองเล่นและ build PC (V2 ยังไม่อยู่ใน Build Settings)

---

## 8. สถานะ Git ตอนส่งต่อ
- งานขั้น B–E พร้อมการแก้ปริศนาและ Soft World art รวมในการส่ง origin/master วันที่ 2026-10-23
- `ProjectSettings/ProjectSettings.asset` ถูก Unity แก้เอง (`preloadedAssets` ว่างลง) — ตรวจกับเจ้าของก่อนรวมใน commit
- ไฟล์ `Assets/OopsPlanet/Evidence/*.json` เป็นผลเทสต์ล่าสุด (ถูกเขียนทับทุกครั้งที่รัน)
- ไฟล์ที่ห้ามรวมใน commit ดู §1 ข้อ 4

งานแอนิเมชันและความชัดของพลัง 2026-10-23: เดิน/ดูดบนโมเดลเดิมและเส้นเอฟเฟกต์พลัง อ่าน Docs/ANIMATION_FEEDBACK_2026-10-23_TH.md; V2 57/57 คอมโบ 30/30 ไม่มี commit/push รอบนี้

รวม Vrzo/Pino ในเครื่องแล้ว (merge 6f0c474) และ port ให้ตรง BlobPlayer/BlobProperty รายละเอียด Docs/VRZO_PINO_INTEGRATION_2026-10-23_TH.md; geometry 87/87, locomotion 27/27, V2 ในฉาก Pino 57/57 คอมโบ 30/30 ไม่มี state mismatch; ยังไม่ push และยังใช้ตัวละครเดิมในฉาก V2 ต้นฉบับ

## 9. อัปเดต 2026-10-24 — โรงงานโถงใหญ่ + บอส (ล่าสุด)

เจ้าของสั่ง **โรงงานใหญ่โถงเดียว ไม่แบ่งห้อง** แต่ยังวาร์ปจากประตูในฉากโลกไปฉากโรงงานแยก ขนาดประมาณ 90 × 159 เมตร ใช้หลังคาร่วม เครื่องจักร 6 จุดซ่อม จุดละ 3 ทางหลัก มีทางเดินชั้นบน และบอสผู้ดูแลตัวใหญ่ท้ายโถง ใช้วัตถุรอบตัว + น้ำ + การเชื่อมซ่อม บอสโจมตีได้และพลาดแล้วเริ่มที่ checkpoint บอส

- อ่าน `Docs/FACTORY_EXPANSION_TH.md` สำหรับสเปก ผลทดสอบ และข้อจำกัดปัจจุบัน
- ฉากใหม่: `Assets/Scenes/OopsPlanet/Scenes/OopsPlanet_Factory.unity` โลกเดิม: `OopsPlanet_V2.unity` มี facade และ portal ทั้งสองฉากอยู่ใน Build Settings แล้ว
- Builder: `Tools/OOPS Planet/Factory Expansion/Build Separate Factory And Entrance` สำรองฉากก่อนสร้างและเก็บโลกภายนอกไว้ รุ่นเก่าใน `FactoryInterior` และ `OopsV2Test` ใช้ผังเก่า ไม่ใช่ผลรับรองฉากนี้
- `Factory_SourceLayout.unity` เป็นต้นแบบสำรองผังเก่าให้ builder ไม่อยู่ใน Build Settings ห้ามลบทิ้งโดยไม่แก้ builder
- Ref บอสเจ้าของส่งแล้ว: `ArtSource/References/FactoryCaretaker/Owner_Arena_Sketch.png` และ `Owner_Machine_Reference.png` ใช้สร้างบอสนี้ต่อได้ ไม่ต้องขอรูปเดิมซ้ำ เจ้าของอนุญาต asset ภายนอกแล้ว แต่รอบนี้ไม่ได้ดาวน์โหลด asset
- โมเดลใหม่ปั้นใน Blender: `ArtSource/Blender/build_factory_boss.py` และ `OOPS_FactoryCaretaker.blend` FBX 11 ชิ้นรวม 49,852 สามเหลี่ยม
- ผลล่าสุด: หลัก 66/66, เสริม 30/30, วาร์ป 7/7, บอสซ้ำ 14/14 ที่ `Assets/Scenes/OopsPlanet/Evidence/FactoryExpansion/`
- เทสต์ต้องเพิ่ม component **หลังเข้า Play แล้ว** และเปลี่ยนไปสล็อต test ก่อนเริ่ม ไม่ฝัง component ทดสอบลงฉากก่อนเข้า Play เพราะ Unity อาจโหลด snapshot นั้นซ้ำเมื่อวาร์ป
- เป้าหมาย 35–45 นาทีรวมบอสยังไม่ได้จับเวลาเล่นโดยคนจริง ไม่มี build PC/commit/push รอบนี้
- เซฟจริงตอนเริ่มงาน: oops_save.json SHA256 AF5026E5413582EC3CCE2124E80E20553CBCEDC4E37042255DF22FA019260A67; oops_save_v2.json SHA256 A9D8E521FB806324352D620DB1B8ED67108E86E10E60179D6AF244FA02E1DB5A (เจ้าของเล่นหลัง handoff เก่า จึงห้ามย้อนกลับไป hash 0C1C เดิม)
