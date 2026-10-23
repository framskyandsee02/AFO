# OOPS! PLANET — คู่มือส่งต่องานทีม (P08)

สถานะ ณ commit `a05f528` (P07) เอกสารนี้อธิบายเฉพาะสิ่งที่มีและทดสอบแล้วจริง ส่วนที่ยังไม่ยืนยันจะติดป้าย ⏳
เริ่มอ่านที่ `README.md` (เปิดโปรเจกต์, เข้า scene, ทดสอบ, build)

---

## 1. ภาพรวมระบบ (อ่านโค้ดจากตรงไหน)
| ระบบ | ไฟล์หลัก | หมายเหตุ |
|---|---|---|
| ผู้เล่น / กล้อง / ดูดซับ / 2 slot | `Assets/WobbleWorks/Scripts/BlobPlayer.cs` | `Grounded` = `isGrounded` หรือ sphere probe (แก้ใน P07 ให้ทนต่อ fps สูง) |
| โลก, HUD ภาษาไทย, เซฟ, เมนู | `Assets/WobbleWorks/Scripts/BlobWorld.cs` | มี `OnGUI` (IMGUI) และจำกัด frame rate ที่ 240 เมื่อ vSync ไม่ทำงาน |
| แหล่งพลังและโซน (Area, Checkpoint, Exit) | `Assets/WobbleWorks/Scripts/BlobInteractable.cs` | ค่า enum `BlobInteraction` ห้ามเปลี่ยนลำดับ เพราะ scene เก็บเป็นตัวเลข |
| พลัง Magnet / Pressure + คอมโบ | `Scripts/Powers/BlobPowers.cs` | Bounce อยู่ใน `BlobPlayer` (ส่วนการเคลื่อนที่) |
| ค่าพลังและคอมโบ | `Scripts/Powers/PowerDefinition.cs`, `CombinationDefinition.cs`, `PowerSet.cs` (`PowerCombos.Resolve`) | asset อยู่ที่ `Assets/OopsPlanet/Data` |
| ปริศนา | `Scripts/Puzzles/*` | `PuzzleObjective`, `IPuzzleCondition` (`ItemSocket`, `ClearZoneCondition`, `PressureTurbine`), `PuzzleItem`, `PuzzleReaction`, `PuzzleResetPad` |
| ความคืบหน้า / เซฟ | `Scripts/Progress/*` | `SaveSystem`, `ZoneGate`, `HubProgress` |
| เสียง / VFX / อารมณ์ BLOB | `Scripts/Audio/ProcAudio.cs`, `Scripts/Feedback/*` | ไม่มีไฟล์เสียง ทุกเสียงสร้างจากโค้ด |
| สร้าง scene / Validate / เมนูเทสต์ | `Assets/OopsPlanet/Editor/OopsPrototypeBuilder.cs` | **เป็นต้นฉบับของเลย์เอาต์ทั้งหมด** |
| เทสต์ | `Scripts/Oops*Test.cs`, `Scripts/Testing/OopsBuildTestRunner.cs`, `Editor/Tests/PowerComboResolverTests.cs` | รันได้ทั้งใน Editor และในตัว build |

> **enum `BlobProperty`** ใช้ค่าตัวเลขตายตัว (`None=0, Magnet=1, Pressure=5, Bounce=6`) เพราะ prefab เก็บค่าเป็นตัวเลข ถ้าเพิ่มพลังใหม่ให้ใช้เลขใหม่ ห้ามใช้เลขเดิมซ้ำ

---

## 2. วิธีทำงานที่ใช้บ่อย

### 2.1 เพิ่มวัตถุที่ดูดซับได้ (absorbable)
**วิธีแนะนำ (ผ่าน builder):** ใน `OopsPrototypeBuilder.Build()`
```csharp
var prefab = AbsorbablePrefab("Absorbable_Magnet_Pipe", "ABS_MyPipe", new[]{metal, magnetRed},
                              BlobProperty.Magnet, AbsorbResult.Remains /* หรือ Disable / Remnant */, remnantModel: null);
PlaceSource(prefab, "My Magnet source (remains)", new Vector3(x, 0, z), yaw);
```
จากนั้นกด **Tools → OOPS Planet → Rebuild Prototype 01** แล้วกด **Validate Prototype 01**
- `AbsorbResult`: `Remains` (ดูดซ้ำได้ไม่จำกัด ใช้ในโซนปริศนาเพื่อกัน softlock), `Disable` (หายไปจนกว่าจะ respawn หรือ restart), `Remnant` (สลับเป็นโมเดลซาก)
- ถ้ามี prefab ชื่อเดียวกันอยู่แล้ว builder **จะไม่สร้างทับ** ต้องลบ prefab เดิมเองก่อน
- ป้ายชื่อพลังภาษาไทยเหนือแหล่งพลังถูกสร้างให้อัตโนมัติ
- **ถ้าทำด้วยมือ** (การแก้นี้จะหายเมื่อกด Rebuild): สร้าง GameObject ใส่ `BlobInteractable` (`kind=Source`, `property`, `onAbsorbed`, `indicator`=Renderer, `fullVisual`) ใส่ collider แล้ว **เพิ่มเข้าใน `BlobWorld.sources`** เพราะระบบเล็งเป้าหาแหล่งพลังจาก array นี้เท่านั้น

### 2.2 ปรับค่าพลัง หรือเพิ่มคอมโบ
- **ปรับค่า:** เลือก `Assets/OopsPlanet/Data/Power_*.asset` แล้วแก้ใน Inspector (`range`, `force`, `massLimit`, `cooldown`, `minLaunch/maxLaunch`, `rebound`, `maxRebounds`) builder จะไม่เขียนทับไฟล์เหล่านี้
  - ค่าที่ทดสอบผ่าน: Pressure `force=14` (P03), Bounce `maxLaunch=15` ซึ่งขึ้นแท่นสูง 3.5 m ได้
- **เพิ่มคอมโบ:** Create → OOPS Planet → Combination Definition ตั้ง `a`, `b`, `effect` และข้อความภาษาไทย แล้วใส่ใน `PowerSet_Prototype.combinations`
  - `effect` ตอนนี้มีแค่ `MagnetCannon` กับ `PressureDash` ถ้าเป็นผลแบบใหม่ ต้องเพิ่มโค้ดใน `BlobPowers.Pressure()` หรือ `Press()`
  - `Validate` ยอมรับคอมโบได้ 2–3 คู่ (ขอบเขตของ prototype) และควรเพิ่มเคสใน `PowerComboResolverTests`
- **พลังใหม่ทั้งหมด** ต้องแก้ enum `BlobProperty` (ใช้ตัวเลขใหม่), เพิ่มตรรกะใน `BlobPowers`/`BlobPlayer`, เพิ่ม `PropertyColor`, ชิ้นส่วน `BLOB_Cue_*` ใน Blender, HUD และเทสต์ **ซึ่งเป็นการเปลี่ยน scope ต้องขออนุมัติก่อน**

### 2.3 สร้างปริศนาใหม่
1. ทำวัตถุที่ต้องเคลื่อนย้าย: ใส่ `Rigidbody` + `PuzzleItem` (กำหนด `key`) และถ้าตำแหน่งเริ่มอยู่ในโซนเป้าหมาย ให้ตั้ง `useFallback`/`fallbackPosition` ด้วย ถ้าเป็นวัตถุโลหะให้ใส่ `BlobMagnetic`
2. ใส่เงื่อนไขอย่างน้อย 1 ตัว: `ItemSocket` (key ตรงกัน, วางนิ่ง), `ClearZoneCondition` (ไม่มี item ใน zone), `PressureTurbine` (เป่าแรงดันจนหมุนเต็ม) หรือเขียนใหม่ที่ implement `IPuzzleCondition`
3. ใน builder:
   ```csharp
   var pz = Puzzle("Puzzle X", "unique_id", "ชื่อไทย", "คำใบ้ไทย", "ข้อความเมื่อสำเร็จ",
                   new Bounds(center, size), requires: null, new MonoBehaviour[]{cond}, new[]{item});
   pz.celebratePoint = ...;  // ตำแหน่งที่เล่นเอฟเฟกต์ฉลอง
   ```
   **`id` ต้องไม่ซ้ำ และห้ามเปลี่ยนหลังปล่อยเกม** เพราะเซฟอ้างอิงด้วย `id`
4. การฟื้นฟูโลก: เพิ่ม `PuzzleReaction` (`show/hideWhenSolved`, `mover`, `lights`, `recolor`, `stopWhenSolved`, `grow`) ต้องกำหนดค่า array ทุกตัว อย่าปล่อยเป็น null
5. ทางแก้เมื่อผู้เล่นทำพลาด: `ResetPad("...", pos, pz)`
6. เพิ่มปริศนาเข้า `world.puzzles` และถ้าใช้ปลดล็อกโซนหรือขั้นของ Hub ให้เพิ่มใน `ZoneGate.requires` และ `HubProgress.stage*Requires` ด้วย
7. **`Validate` ตอนนี้บังคับให้มีปริศนาพอดี 3 ชุด** ถ้าเพิ่มต้องแก้เงื่อนไขนั้นด้วย และควรเพิ่มขั้นตอนใน `OopsPuzzleTest`

### 2.4 Export จาก Blender แล้ว import เข้า Unity
**กติกา:** หน่วยเป็นเมตร, แกน Z ชี้ขึ้น, ด้านหน้าตัวละครหัน **−Y**, จุด origin อยู่ **กึ่งกลางฐาน**, ใช้ material slot ร่วมกันได้

| Prefix ชื่อ object | ผลลัพธ์ใน `Assets/OopsPlanet/Models` |
|---|---|
| `BLOB_*` | รวมเป็น `BLOB_Greybox.fbx` ไฟล์เดียว (ส่วน `BLOB_Cue_*`, `BLOB_Mouth` ฯลฯ ถูกเรียกจากโค้ดด้วยชื่อ **ห้ามเปลี่ยนชื่อ**) |
| `HUB_*` | รวมเป็น `HUB_Landmark.fbx` ไฟล์เดียว |
| `PROP_*`, `ABS_*`, `PZ_*` | แยกเป็น FBX ต่อชิ้น |

ขั้นตอน:
1. เปิดไฟล์ใน `ArtSource/Blender/` แล้วรัน `export_to_unity.py` จาก Text Editor หรือสั่ง `blender -b <file>.blend --python ArtSource/Blender/export_to_unity.py`
2. Unity จะ import อัตโนมัติ ถ้าเป็นโมเดลใหม่ให้เพิ่มผ่าน builder (`ModelInstance` / `Decor` / `AbsorbablePrefab`) แล้วกด Rebuild
3. ตรวจ scale, การหมุน, collider และ material ใน scene แล้วกด Validate (Validate จับ material สีชมพูไม่ได้ แต่ P06 test จับได้)
4. `.blend` และ `.fbx` อยู่ใน Git LFS **ไฟล์ binary merge ไม่ได้** ต้องตกลงในทีมก่อนว่าใครแก้ไฟล์ไหน

### 2.5 Build สำหรับ PC
ดู `README.md` หัวข้อ "Build สำหรับ PC" และทดสอบในตัว build ด้วย `-oopsAutoTest`
**ยืนยันแล้ว:** Windows 64 (P07) ส่วน Mac/Linux ⏳ ยังไม่ได้ทดสอบ

---

## 3. การแบ่งความรับผิดชอบ (ข้อเสนอ ทีมตกลงกันเองได้)
| บทบาท | ดูแลหลัก | ไฟล์ |
|---|---|---|
| **Gameplay Programmer** | การเคลื่อนที่, ดูดซับ, พลัง, คอมโบ, ระบบปริศนา, เซฟ, เทสต์ | `Assets/WobbleWorks/Scripts/*`, `Scripts/Powers`, `Puzzles`, `Progress`, `Testing`, `Oops*Test.cs`, `Editor/Tests` |
| **Level / Puzzle Designer** | เลย์เอาต์โซน, ตำแหน่งปริศนา, ป้ายและข้อความไทย, ค่าพลัง | **`OopsPrototypeBuilder.cs` (ส่วน layout)**, `Assets/OopsPlanet/Data/*.asset` |
| **Art / Integration** | โมเดล, material, แสง, post FX, VFX/เสียง, ฟอนต์, license | `ArtSource/Blender/*`, `Models`, `Materials`, `Settings/OOPS_PostFX.asset`, `Scripts/Feedback`, `Scripts/Audio`, `ASSET_LICENSES.md` |

---

## 4. ไฟล์ที่ห้ามแก้พร้อมกัน และข้อควรระวังตอน merge
| ไฟล์ | เหตุผล | กติกา |
|---|---|---|
| `Assets/OopsPlanet/Editor/OopsPrototypeBuilder.cs` | เป็นต้นฉบับของ scene ทั้งหมด | แก้ทีละคน แล้วกด Rebuild และ Validate ก่อน commit |
| `Assets/OopsPlanet/Scenes/OopsPlanet_Prototype01.unity` | ไฟล์ YAML ขนาดใหญ่ที่ถูกสร้างอัตโนมัติ | **ห้ามแก้ด้วยมือ** ถ้าเกิด conflict ให้เลือกฝั่งใดฝั่งหนึ่ง แล้ว Rebuild จาก builder ที่ merge แล้ว |
| `ArtSource/Blender/*.blend`, `Models/*.fbx` | binary ใน LFS ที่ merge ไม่ได้ | คนเดียวแก้ต่อไฟล์ และแจ้งในทีมก่อนเริ่ม |
| `BlobPlayer.cs`, `BlobWorld.cs` | ระบบแกนกลางที่เทสต์ทุกชุดพึ่งพา | แก้แล้วต้องรัน P01–P06 ใหม่ทั้งหมด |
| `Data/*.asset` | ค่าพลังที่ส่งผลต่อเทสต์ P03/P04 (เช่นความสูง Bounce, แรงผลักของ Pressure) | ปรับแล้วรัน P03 และ P04 |
| `ProjectSettings/*`, `Assets/Settings/*` | Unity มักเขียนไฟล์เหล่านี้ใหม่เองตอน build หรืออัปเดต | ตรวจ diff ก่อน commit และ **`UnityConnectSettings.asset` ต้องเป็น `m_Enabled: 0`** |

**หลังจาก merge ทุกครั้ง:** Rebuild → Validate → EditMode → P01–P04 (sequence) → P05 (2 session) → P06

---

## 5. ทางเดินแก้ปริศนา (ทางหลักที่ทดสอบแล้ว)
1. **Hub:** ฝึกดูดพลังที่ลาน (เล็งแล้วกด E ค้าง) ทางไปนาข้าว **ยังล็อก**
2. **โรงงาน, ประตูโรงงาน:** ดูดพลังแรงดัน (ถังทางขวาของทางเข้า) และแม่เหล็ก (เศษเหล็กทางซ้าย) → ใช้ F (แม่เหล็ก) ลากตุ้มถ่วงสีแดงไปวางบนแท่นขวาข้างประตู → ประตูเลื่อนขึ้น
3. **โรงงาน, กังหันระบายอากาศ:** ยืนบนจุดสีฟ้าในห้องด้านหลัง เล็งกังหัน แล้วกดใช้พลังแรงดันประมาณ 4 ครั้งจนไฟครบ 3 ดวง → ช่องระบายเปิด ควันหยุด ไฟเปลี่ยนเป็นสีอุ่น และ **ทางไปนาข้าวปลดล็อก**
4. **กลับ Hub:** ต้นไม้เริ่มผลิใบ และประตูทางทิศตะวันออกเปิด
5. **นาข้าว, ฟางอุดประตูน้ำ:** ดูดพลังเด้งและแรงดันจากจุดทางเข้า → กด SPACE ค้างเพื่อชาร์จ แล้วปล่อยขณะเดินเข้าหาแท่นสูง 3 m → เป่าฟางทั้ง 4 มัดออก → น้ำไหล ข้าวกลับมาเขียว และ Hub ฟื้นฟูครบ
6. เดินตามคันนาไปที่ **จุดจบเดโม**

คอมโบที่รองรับ: แม่เหล็ก + แรงดัน = **ปืนแม่เหล็กแรงดัน** (ดึงวัตถุแล้วกดแรงดันเพื่อยิง), แรงดัน + เด้ง = **พุ่งแรงดัน** (เด้งแล้วกดแรงดันกลางอากาศ) ส่วนแม่เหล็ก + เด้ง ผสมกันไม่ได้ เกมจะแจ้งผู้เล่น

---

## 6. หลักฐานการทดสอบ
| ชุดเทสต์ | Editor | Build headless | Build แบบหน้าต่าง |
|---|---|---|---|
| EditMode | 6/6 | – | – |
| P01 route | 26/0 | 26/0 | 26/0 |
| P02 absorb | 24/0 | 24/0 | 24/0 |
| P03 powers | 26/0 | 26/0 | 26/0 |
| P04 puzzles | 29/0 | 29/0 | 29/0 |
| P05 save (2 sessions) | 12/0 + 12/0 | ⏳ | ⏳ |
| P06 polish | 19/0 | ⏳ | ⏳ |

มีการเล่นด้วยมือจนจบเกมหนึ่งครั้ง (โดยเจ้าของโปรเจกต์ ใน Editor) ส่วนการทดลองเล่นโดยคนนอกทีมยัง ⏳ ไม่ได้ทำ
Evidence อยู่ที่ `Assets/OopsPlanet/Evidence` และ `Docs/P07_Test_Checklist.md`

## 7. Known issues
1. ⚠ ถ้าถอดการจำกัด frame rate ออก (~16k fps ทำได้เฉพาะในเทสต์) การเด้งจะได้แค่ 0.73 m ในเกมจริงเกิดไม่ได้เพราะมีทั้ง vSync และ cap 240 แต่ยังไม่ได้หาสาเหตุ
2. ป้ายในฉาก (TextMesh) มองทะลุกำแพงได้ เป็นเรื่องภาพ แก้ด้วย material 3D text ที่มี depth test
3. ป้าย "ลานฝึกดูดพลัง" ซ้อนกับพุ่มต้นไม้ใน Hub เล็กน้อยเมื่อมองบางมุม
4. สคริปต์หลักของ BLOB ยังอยู่ใน `Assets/WobbleWorks/Scripts` และยังมี asset ของ Wobble ที่เลิกใช้อยู่ในโปรเจกต์ (ย้ายหรือลบได้ แต่ต้องขออนุมัติ และย้ายพร้อม `.meta`)
5. ชื่อโปรเจกต์และบริษัทยังเป็นค่าเริ่มต้น (`AFO` / `DefaultCompany`) ถ้าเปลี่ยน ตำแหน่งไฟล์เซฟจะย้ายตาม
6. `com.coplaydev.unity-mcp` ดึงจาก `#main` จึงอาจได้เวอร์ชันต่างกันในแต่ละเครื่อง ควร pin ไว้ที่ commit หรือ tag
7. `*.blend1` (ไฟล์ backup ของ Blender) ยังไม่อยู่ใน `.gitignore`
8. HUD ใช้ IMGUI ถ้าจะทำ UI จริงจังควรย้ายไป uGUI หรือ UI Toolkit (ไม่อยู่ในขอบเขตของ prototype)
9. Checkpoint และพลังที่ถืออยู่ไม่ถูกเซฟ (ตั้งใจ) โหลดเกมแล้วจะเริ่มที่ Hub

## 8. งานทำมือที่ยังค้าง
- [ ] ทดลองเล่นโดยคนนอกทีมโดยใช้ `Docs/P07_Playtest_Form_TH.md`
- [ ] ตัดสินใจชื่อโปรดักต์และบริษัท (กระทบตำแหน่งไฟล์เซฟ)
- [ ] เพิ่ม `*.blend1` ใน `.gitignore`
- [ ] ตัดสินใจเรื่อง asset ของ Wobble ที่เลิกใช้ (เก็บไว้, ย้ายไปเก็บ หรือลบ)
- [ ] `git push` (remote คือ `origin` = GitHub `framskyandsee02/AFO`)
