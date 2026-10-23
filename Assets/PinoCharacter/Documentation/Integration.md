# Pino — Character Morphing & Gameplay Integration

ระบบนี้พัฒนาต่อจาก Pino เดิม ใช้ `WobbleWorks.WobProperty` ของทีม Gameplay โดยไม่เพิ่มระบบดูดซับ Input การเคลื่อนที่ การต่อสู้ หรือระบบจัดการความสามารถอีกชุด

## ไฟล์ที่ใช้งาน

| งาน | ไฟล์ |
| --- | --- |
| โมเดลฐานเดิม | `Art/Pino/Pino.blend` |
| สำรองก่อนเพิ่ม Morph | `Art/Pino/Backups/BeforeMorphing/` |
| Blender ที่มี Shape Keys | `Art/Pino/Source/Pino_Morphs.blend` |
| สคริปต์สร้าง Morph ซ้ำได้ | `Art/Pino/Source/build_pino_morphs.py` |
| FBX สำหรับ Unity | `Assets/PinoCharacter/Models/Pino_Morphs.fbx` |
| FBX สำเนาสำหรับส่งออก | `Art/Pino/Exports/Pino_Morphs.fbx` |
| Prefab พร้อม Controller | `Assets/PinoCharacter/Prefabs/PinoCharacter.prefab` |
| ฉากดูฟอร์มและพารามิเตอร์ | `Assets/PinoCharacter/Scenes/PinoMorphDemo.unity` |
| ฉาก Gameplay สำเนาที่เชื่อมแล้ว | `Assets/PinoCharacter/Scenes/PinoGameplayIntegration.unity` |
| Material / Profile | `Assets/PinoCharacter/Materials/`, `Profiles/` |
| รายงานตรวจ Geometry และ API | `Assets/PinoCharacter/Evidence/Validation.json` |
| รายงาน Play Mode ที่เชื่อม Gameplay | `Assets/PinoCharacter/Evidence/PlaymodeIntegration.json` |
| ผลเส้นทางทดสอบเดิมบนฉากสำเนา | `Assets/PinoCharacter/Evidence/GameplayRoute.json` |
| ภาพตรวจใน Unity | `Art/Pino/Morphing/Unity_PinoMorphGallery.png` |

ฉาก Gameplay ต้นฉบับและ Build Settings ไม่ถูกแทนที่ เปิดฉากสำเนาเพื่อทดลองตัวละครก่อนนำ Prefab ไปใช้กับฉากของทีม

## ผลตรวจโมเดลจริง

- Blender 5.2.3; Unity 6000.3.25f1; Render Pipeline เดิมคือ URP 17.3.0
- ลำตัวเดิม 6,050 vertices และตาแยกสองชิ้น ชิ้นละ 610 vertices; ผิวปิด ไม่มี non-manifold edges
- โมเดลฐานสูง 0.52 เมตร Scale 1; ลำตัวและตาเดิมมี UV และ Materials
- ไม่พบ Armature หรือ Shape Keys เดิม จึงเพิ่ม Blend Shapes ให้ Mesh เดิม ไม่สร้างตัวละครใหม่
- เวอร์ชันนี้คงการตัดหัวใจ แท่น และประกายตามคำสั่งล่าสุด ไม่มีปากเพิ่มเอง
- ตรวจละเอียดได้ที่ `Art/Pino/Source/BaseCharacterAudit.json` และ `MorphAssetAudit.json`

## Morph Targets และการตามผิวของใบหน้า

| ฟอร์ม / พารามิเตอร์ | Target | ผลต่อ Geometry |
| --- | --- | --- |
| Normal | น้ำหนักฟอร์มทั้งหมดเป็นศูนย์ | กลับ Mesh ฐานเดิม |
| Spring | `Spring` | เอวแคบลงและลำตัวยืด พร้อมขดสปริงแยก |
| Spring ชาร์จ | `Spring` + `SpringCompress` | บีบตัวจริงจากความสูงประมาณ 0.634 เป็น 0.273 เมตร |
| Spring ปล่อย | `Spring` + `SpringStretch` | ยืดถึงประมาณ 0.920 เมตร |
| Magnet | `Magnet` | ลำตัวกว้างและเตี้ยลง พร้อมชิ้นแม่เหล็กโลหะ |
| Gas | `Gas` | พองลำตัวและเพิ่มความสูง พร้อม Material โปร่งแสง |
| Liquid | `Liquid` | ลำตัวแบนและกว้างขึ้น |
| Liquid ไหล / ผ่านช่อง | `Liquid` + `LiquidFlow` | แบนถึงประมาณ 0.109 เมตรและยืดตามแนวหน้า–หลัง |
| ดูดซับ / หายใจ / ลงพื้น | `Absorb`, `Idle`, `Land` | Animation ด้วย Geometry ที่ไม่แตะ Gameplay |

ตาทั้งสองมี Target ชุดเดียวกับลำตัว ตำแหน่งและแนวผิวคำนวณจากการเปลี่ยนรูปของลำตัวและ surface normal ขนาดตาคงอ่านเป็นตาเดิม ขดสปริงมี Target บีบและยืดของตัวเอง

`SpringCompress`, `SpringStretch` เป็น **delta เพิ่มจาก Spring** และ `LiquidFlow` เป็น delta เพิ่มจาก Liquid ไม่ใช่ฟอร์มที่ใช้เดี่ยว ๆ Controller จัดการชุดน้ำหนักนี้ให้แล้ว

## API ของฝ่ายตัวละคร

ไฟล์ C# ฉบับเต็มอยู่ใน `Assets/PinoCharacter/Runtime/` ทั้งหมด ค่า weight ของ Public API เป็น **0–1**; Controller แปลงเป็น 0–100 ของ Unity ภายใน

```csharp
using UnityEngine;
using WobbleWorks;
using WobbleWorks.Characters;

// ตัวอย่างคำสั่งฝั่ง Presentation จากระบบภายนอก
character.BlendToMorphForm(WobProperty.Spring, 0.35f);
character.SetSpringCompression(0.8f);
character.SetSpringCompression(0f);
character.SetSpringStretch(1f);

character.BlendToMorphForm(WobProperty.Liquid, 0.3f);
character.SetLiquidFlow(1f);
character.ReturnToNormal(0.4f);
```

Public API เพิ่มเติม:

- `SetMorphForm(WobProperty form)` — เปลี่ยนทันที
- `SetMorphWeight(string shapeName, float weight)` — Override target ที่ระบุ จนสั่งฟอร์มใหม่
- `SetCharacterMaterial(CharacterMaterialProfile profile)` — เปลี่ยน appearance ฝั่งตัวละคร
- `SetAbsorptionProgress(float progress)`, `PlayAbsorptionReaction()`
- `PlayLandingReaction(float strength = 1f)`
- `SetVisualPaused(bool paused)`, `IdleEnabled`, `TransitionSeconds`
- `CurrentForm`, `TargetForm`, `IsTransitioning`, `GetMorphWeight(string)`
- `Tick(float deltaTime)` — ประเมินเวลาแบบกำหนดค่าได้ ใช้ตรวจสอบและสร้าง Preview

เมื่อเปลี่ยนเป้าหมายกลาง Transition ระบบใช้ pose ปัจจุบันเป็นจุดเริ่มใหม่ จึงไม่กระโดดกลับ Basis ก่อนเปลี่ยนฟอร์ม `ReturnToNormal` ล้าง reaction ที่ค้างด้วย ปิด `IdleEnabled` เมื่อต้องการเทียบ Geometry กับ Basis แบบตรงทุก vertex

## เชื่อมกับ Gameplay โดยไม่แก้โค้ดของทีม

```text
WOB Player                         ← CharacterController / WobPlayer เดิม
├── WOB Blender Visual             ← driver เดิม: Gameplay ยังควบคุมทิศทางและขนาด
└── PinoVisual                     ← sibling ไม่รับ scale ของ driver
    ├── PinoMorphController
    ├── PinoGameplayAdapter
    ├── PinoLocomotionAnimator
    └── Pino Motion                ← เด้ง/บีบ/ดุ๊กดิ๊กเฉพาะงานแสดงผล
        └── Pino Morph Model
            ├── ลำตัว + ตาสองชิ้น  ← SkinnedMeshRenderer / Blend Shapes
            ├── Pino.Magnet
            └── Pino.SpringCoil
```

`PinoGameplayAdapter` อ่านสถานะใน LateUpdate (order 500) แล้ว Controller ประเมิน Morph (order 1000):

| Gameplay เดิม | Character |
| --- | --- |
| `player.property` | เลือกฟอร์มผ่าน `WobProperty` เดิม; `None` คือ Normal |
| `player.springCharge` | น้ำหนักบีบสปริง |
| `player.flattened` | LiquidFlow ตามผลตรวจ clearance ของ Gameplay |
| `player.absorbProgress` | ปฏิกิริยาดูดซับ |
| `player.Grounded`, `velocity` | ปฏิกิริยาลงพื้นและ pulse ยืดสปริง |
| `player.paused`, `completed` | หยุดเวลา Animation ของตัวละคร |
| `player.visual.rotation` | คัดลอกเฉพาะทิศทาง ไม่คัดลอก Scale |
| `player.morphSeconds` | ระยะเวลาเปลี่ยนรูป ถ้าไม่ได้กำหนด override |

Adapter **ไม่เขียน** `property`, Input, Movement, Velocity, Collider หรือสถานะ World และไม่ใช้ `target.property` เป็นความสามารถที่ได้รับแล้ว การคืน `None` จาก Respawn จึงแสดง Normal ได้ตามระบบเดิม

แยก Pino ออกจาก driver เพราะ `WobPlayer.UpdateMorph()` ยังปรับ Scale และ `_BaseColor` ของ Renderer ชื่อ Body/Orbit ทุกเฟรม Adapter ซ่อนเฉพาะ Renderer ของ driver ระหว่างใช้งาน และคืน enabled state เดิมเมื่อปิด component

Gameplay ปัจจุบันไม่มี Event สำหรับเริ่มใช้แม่เหล็กหรือปล่อยสปริง การยืดตอนปล่อยสปริงจึงอนุมานจาก charge ลดเป็นศูนย์ร่วมกับออกจากพื้นและความเร็วขึ้น ส่วน visual ดูดซับอ่าน progress/การเปลี่ยนฟอร์ม การดูดซับฟอร์มเดิมสำเร็จซ้ำภายในเฟรมเดียวอาจไม่มีสัญญาณแยกที่แน่นอน หากต้องการจังหวะที่แน่นอนให้ทีมเรียก API ด้านบนหรือเพิ่ม Event โดยตกลงกันภายหลัง

## Import Settings และ Inspector

ใช้เมนู `Tools > Pino Character > Build Character Assets` เพื่อสร้าง/อัปเดตเฉพาะ Assets ของ Pino:

1. FBX: Scale Factor 1, Use File Scale, Bake Axis Conversion, Import Blend Shapes, Read/Write เปิด, Mesh Compression Off
2. Normals Import, Blend Shape Normals Calculate, Tangents Calculate Mikk; Generic และไม่ Import Animation เพราะใช้ระบบ Animation แบบ procedural
3. Texture: sRGB, Clamp, Mipmaps; Texture ถูกเชื่อมใน Materials ของ Pino
4. Prefab: Controller อ้างอิง Renderer พร้อม Shape Keys, body renderer และ accessory renderers ถูกแยกครบ
5. Profiles แยกตาม `WobProperty`; Gas ใช้ URP/Lit transparent ส่วนอื่น opaque ไม่เปลี่ยน Render Pipeline
6. Inspector ของ Controller มีปุ่มทุกฟอร์มและ sliders สำหรับ compression, extension, flow และ absorption; ดู Preview ได้โดยไม่เปิด Gameplay

โมเดลใน Prefab คงความสูงฐาน 0.52 เมตร ฉาก Integration ขยายเฉพาะ visual ให้สูงประมาณ 1.2 เมตรเพื่อใกล้ placeholder เดิม โดยยังใช้ Collider 1.3 เมตรเดิมของทีม สไลม์มีทรงกว้างกว่า capsule; การปรับความแม่นยำ Collider เป็นขอบเขต Gameplay และไม่ได้แก้ในการส่งมอบนี้

## ขั้นตอนทดสอบซ้ำ

1. เปิด `PinoMorphDemo.unity` เพื่อตรวจฟอร์มและวัสดุทั้งหมด
2. เลือกตัวละครในฉาก เช่น `NORMAL` หรือ `SPRING` และใช้ Inspector ของ PinoMorphController เปลี่ยน Spring → Compress → Stretch จากนั้นลอง Magnet / Gas / Liquid / Flow / Normal
3. เมนู `Tools > Pino Character > Validate Character` ตรวจ Mesh ที่ Bake แล้วจริง, การตามผิวของตา, transition ที่ถูกขัดจังหวะ, การคืน Normal ซ้ำ, pause/reaction/accessory และ Adapter ที่อ่านอย่างเดียว รายละเอียดอยู่ใน `Evidence/Validation.json`
4. เปิด `PinoGameplayIntegration.unity` แล้ว Play: ใช้ E ดูดซับ, Spring ชาร์จ SPACE, Liquid กด F และ Respawn ด้วย R ตาม Gameplay เดิม
5. เมนู `Tools > Pino Character > Run Integrated Gameplay Playtest` ใช้ `WobPlaytest` เดิมของทีมบนฉากสำเนา พร้อมสำรองและคืนรายงานเดิม ตรวจผลใน Evidence ของ Pino เมนูนี้ต้องเริ่มนอก Play Mode และบันทึก Scene ที่แก้ค้างไว้ก่อน ระบบจะคืนฉากที่เปิดไว้หลังจบทดสอบ

การสร้าง Blender Morphs ซ้ำจากโฟลเดอร์โปรเจกต์:

```text
blender --background Art/Pino/Pino.blend --python Art/Pino/Source/build_pino_morphs.py
```

สคริปต์เปิดใช้กับไฟล์ฐานที่ไม่มี Shape Keys; ไฟล์ที่สร้างใหม่อยู่ใน Source/Exports และ Assets/PinoCharacter ไม่เขียนทับโมเดลฐานเดิม

## รายการไฟล์โค้ด

- Runtime: `CharacterMaterialProfile.cs`, `PinoMorphController.cs`, `PinoGameplayAdapter.cs`, `PinoLocomotionAnimator.cs`
- Editor: `PinoCharacterBuilder.cs`, `PinoMorphControllerEditor.cs`, `PinoCharacterValidation.cs`, `PinoCharacterAutomation.cs`, `PinoPlaymodeValidation.cs`, `PinoLocomotionValidation.cs`
- ไม่แก้ `WobPlayer`, `WobWorld`, `WobInteractable`, Input, Ability Logic หรือฉาก Gameplay ต้นฉบับ

## เดินเด้งต่ำและ Idle ดุ๊กดิ๊กทุกฟอร์ม

`PinoLocomotionAnimator` อยู่บน Prefab แล้ว ฉาก `PinoGameplayIntegration.unity` รับการเดิน W/A/S/D ของ `WobPlayer` เดิม โดยอ่านระยะเคลื่อนที่จริงทุกเฟรม จึงไม่เด้งเดินซ้ำเมื่อกด W ชนกำแพง ไม่เพิ่ม Input หรือเปลี่ยนแรงกระโดด/Collider

- Normal: เด้งประมาณ 2 ครั้งต่อวินาที มีช่วงบีบตัวก่อนขึ้นและแตะพื้น ความสูงสูงสุด 5.5% ของตัวละคร (โมเดลฐานประมาณ 2.86 ซม.; ตัวละครสูง 1.2 เมตรในฉากประมาณ 6.6 ซม.)
- Magnet: เด้งและพองยุบหนึบขึ้น จังหวะช้ากว่า Normal เล็กน้อย โดยขั้วแม่เหล็กเคลื่อนตามตัว
- Spring: เด้งและบีบยืดชัดขึ้น ขดสปริงเคลื่อนพร้อมตัว เมื่อชาร์จเต็มจะหยุดเด้งเดินเพื่อให้เห็นท่าบีบอัดจริง; ไม่แก้น้ำหนัก Shape ของการชาร์จและปล่อย
- Gas: โยกและลอยเบา ๆ ทั้งตอนเดินและอยู่นิ่ง รวมตอนลอยอยู่ในอากาศ การลอยดุ๊กดิ๊กเป็น Offset เฉพาะโมเดล
- Liquid: เด้งเตี้ยกว่าร่างอื่นและกระเพื่อมด้านข้าง ตอน `LiquidFlow = 1` จะไม่ยกตัว ไม่เพิ่ม Scale Y และไม่เอียง Pitch/Roll เพื่อคงความสูงตอนลอดช่อง แต่ยังดุ๊กดิ๊กในแนวพื้นได้
- อยู่นิ่ง: พองยุบและยืดด้านข้างเล็กน้อย ส่าย/เอียงตามลักษณะร่าง ใช้คลื่นหลายจังหวะให้ดูนุ่มเป็นธรรมชาติ
- เริ่ม/หยุดเดิน Blend ประมาณ 0.18 วินาที; รูปแบบจังหวะผสมตามน้ำหนัก Morph ที่แสดงจริงระหว่างเปลี่ยนร่าง เมื่อกระโดดจริงจะหยุดการเด้งเดิน Pause หยุดเวลา Animation และ Respawn ล้างจังหวะเก่า
- ปรับใน Inspector: `Hop Height Ratio`, `Hops Per Second`, `Walk Blend Seconds`, `Walk Squash`, `Idle Squish`, `Idle Sway Degrees` และ `Form Motion Multipliers` แยก Magnet/Spring/Gas/Liquid โดยไม่ต้องปรับระบบฟิสิกส์
- `PinoMorphController.IdleEnabled = false` ปิด Idle ดุ๊กดิ๊กได้; ปิด component `PinoLocomotionAnimator` เพื่อปิดทั้งเดินเด้งและ Idle พร้อมคืน Transform เดิม
- ใช้จากระบบอื่นได้ด้วย `Tick(deltaTime, worldVelocity, grounded, paused)` และ `ResetMotion()` โดยไม่ได้บังคับให้ใช้ `WobPlayer`
- เมนู `Tools > Pino Character > Validate Walk and Idle` ตรวจจังหวะ/ความสูง/การหยุด/ฟอร์มอื่น; ผลอยู่ใน `Evidence/Locomotion.json` และ Play Mode report บันทึก `maximumWalkingHopMetres` จาก Gameplay จริง
- ภาพเคลื่อนไหวทุกฟอร์มอยู่ใน `Art/Pino/Morphing/Pino_AllForms_Idle_Walk.gif` สร้างเฟรมใหม่ด้วย `Tools > Pino Character > Capture Walk and Idle Preview` แล้วใช้ `Art/Pino/Source/create_all_forms_locomotion_preview.py` เพื่อรวมเป็น GIF; พรีวิว Normal รุ่นก่อนอยู่ใน `Pino_Idle_Walk.gif`

## ผลทดสอบส่งมอบ — 23 ตุลาคม 2026

ทดสอบใน Unity Editor 6000.3.25f1 กับ FBX และ Prefab ที่นำเข้าจริง:

- Geometry / API / Adapter: **87 ผ่าน, 0 ไม่ผ่าน** รวมแกนนำเข้า ความสูงฐาน 0.520 เมตร ทิศใบหน้า +Z การตามผิวของตา การเก็บค่าพรีวิวเมื่อโหลดซ้ำ และวัสดุ Gas
- Spring proof: ความสูง 0.6344 เมตร → บีบ 0.2728 เมตร → ยืด 0.9199 เมตร; Liquid Flow สูง 0.1092 เมตร ทั้งหมดวัดจาก Mesh ที่ Bake ใน Unity
- ระยะตาจากผิวในฟอร์มทดสอบประมาณ 2.05–2.47 มิลลิเมตร; เปลี่ยนเป้าหมายกลาง Transition ไม่มี snap และวนเปลี่ยนฟอร์ม 12 รอบกลับ Normal มี vertex residual เท่ากับศูนย์
- Walk / Idle: **27 ผ่าน, 0 ไม่ผ่าน** ตรวจทุกฟอร์ม รวมสปริงตอนชาร์จ Gas ตอนลอย และ Liquid แบบแบนที่ไม่เพิ่มความสูง ตลอดจนการเด้งต่ำ การแตะพื้น Pause การเปลี่ยนฟอร์ม และการคืน Transform เมื่อปิด component
- Play Mode ใช้เส้นทาง `WobPlaytest` ของทีม: **51 ผ่าน, 0 ไม่ผ่าน, completedRoute=true** เท่ากับ baseline เดิม ใช้เวลาประมาณ 56 วินาทีหลังเพิ่ม Walk / Idle
- ตรวจตัวละคร 16,057 เฟรม พบครบ `None`, `Magnet`, `Spring`, `Gas`, `Liquid`; compression, stretch และ flow ถึง 1.0 ทั้งหมด สถานะหลัง transition ตรงกับ Gameplay ทุก 15,202 ตัวอย่าง ไม่มี mismatch วัดจังหวะเดินเด้งจริงสูงสุด 0.06585 เมตร
- คืนรายงานทดสอบเดิมและ Scene setup เดิมสำเร็จ ตรวจ SHA-256 ของ Gameplay 5 ไฟล์ ฉากต้นฉบับ Graphics Settings และ Build Settings แล้วไม่เปลี่ยนแปลง
- ตรวจภาพ Blender และภาพ Render จาก Unity แล้ว โมเดลยืนถูกแกน ตาอยู่ด้านหน้า Gas แสดงโปร่งแสง และชิ้นพิเศษซ่อน/แสดงตามฟอร์ม

Unity บันทึกค่า migration ของ URP บางไฟล์โดยอัตโนมัติระหว่างเปิดโปรเจกต์ ได้เก็บสำเนาไว้ใน `Art/Pino/Backups/UnityAutomaticMigration/` และคืนไฟล์เดิมแล้ว ไม่มีการเปลี่ยน Render Pipeline หรือ Project Settings เป็นส่วนหนึ่งของงานตัวละคร

รายการส่งมอบแยกตามงาน: Blender/source/export/previews/backup อยู่ใน `Art/Pino/`; Runtime, Editor, FBX, texture, materials, profiles, prefab, ฉากสำเนา, คู่มือและ evidence อยู่ใน `Assets/PinoCharacter/` ทุกไฟล์โค้ด C# เป็นฉบับเต็มใน Runtime และ Editor ไม่มีการเพิ่มโค้ด Gameplay อีกชุด
