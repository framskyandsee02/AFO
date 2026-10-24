# BORROWED TIME — Blender 3D meadow

ต้นฉบับ `BT_Meadow.blend` สร้างด้วย Blender **5.2.3 LTS** ผ่านสคริปต์ `Source/build_meadow.py` ใช้โปรแกรม Blender ที่ติดตั้งในเครื่อง เนื่องจาก MCP ของ Blender ยังไม่ได้เชื่อมต่อขณะทำงาน

อ้างอิงภาพแรกเฉพาะมุมมองลงบนพื้นหญ้า ไม่รวมคนหรือแมว ใช้กลุ่มสีเขียวมะนาว เหลืองครีม มอส และเขียวอมฟ้าจากภาพตัวอย่างสองภาพอื่น ไม่มีตัวละคร สัตว์ หรือสิ่งปลูกสร้างในโมเดล

## โมเดลจริง

- พื้นเป็น Mesh ขนาดประมาณ **36 × 26 เมตร** สูงต่ำอย่างนุ่มนวล
- หญ้าเตี้ย **14,000 กอ** ใบโคลเวอร์ **3,900 กลุ่ม** และดอกไม้เล็กสีครีม/เหลือง **300 กลุ่ม**
- แยกกลุ่ม Ground / Grass / Clover / Wildflowers รวม **37 Meshes**, **476,556 triangles**
- เว้นพื้นที่วางตัวละครให้หญ้าเตี้ยและดอกไม้เบาบางกว่าส่วนขอบ
- สีและลายแสงเงาเก็บใน Vertex Color ไม่ใช้ภาพคนหรือภาพเรฟเป็น Texture ไม่มีการเอาภาพผืนหญ้ามาวางแทน Mesh
- ความสูงรวมหลังนำเข้า Unity ประมาณ **0.39 เมตร** ใบหญ้าและโคลเวอร์มีรูปทรงจริง

## เข้า Unity แล้ว

- FBX: `Assets/BorrowedTime/Environment/Models/BT_Meadow.fbx`
- Prefab: `Assets/BorrowedTime/Environment/Prefabs/BT_Meadow.prefab`
- Material: `Assets/BorrowedTime/Environment/Materials/BT_PainterlyMeadow.mat`
- URP shader: `Assets/BorrowedTime/Environment/Shaders/PainterlyMeadow.shader`
- ฉากใช้งาน: `Assets/Scenes/BorrowedTimeMainMenu.unity`

Prefab ถูกวางในเมนูแล้ว กล้องมองลงประมาณ **78°** ปรับขนาดภาพอัตโนมัติตาม Aspect Ratio ผ่าน `MeadowMenuView` ปุ่ม/ชื่อเกม/Setting/Play → Start และใบไม้ร่วงของเมนูเดิมยังใช้ระบบเดิม ไม่มีการเปลี่ยน Gameplay

ภาพหญ้าแบบเก่ากับ `MeadowBackdrop` ถูกปิดไว้ในฉาก และซ่อนภาพพุ่มไม้ระยะหน้าเพื่อให้ตรงกับมุมมองบนพื้นตามเรฟ เปิดกลับได้จาก GameObject Background เดิมและ `showForegroundFoliage` บน BorrowedMainMenu

`CharacterPlacement - empty for future sleeping model` ยังว่างและตั้งอยู่เหนือพื้นในตำแหน่งซ้ายกลาง ปรับ `characterViewport` และ `characterGroundHeight` ที่ MeadowMenuView เพื่อวางโมเดลภายหลัง

Shader ใช้สีแบบด้าน แสงแดดอุ่น และเงาเขียว มีกระเพื่อมปลายหญ้าเล็กน้อย ปรับ `_WindAmplitude` เป็น 0 เพื่อปิดได้ ลายแสงลอดเรือนยอดส่วนใหญ่เป็นการลงสีบน Vertex ไม่ต้องมีต้นไม้ใหญ่หรือแผ่นภาพบังกล้อง

## สร้าง / นำเข้าซ้ำ

1. รัน Blender ด้วย `--background --threads 4 --python Art/BorrowedTime/Meadow3D/Source/build_meadow.py`
2. ใน Unity ขณะอยู่นอก Play Mode เลือก **Tools > Borrowed Time > Import Blender 3D Meadow**
3. เปิด Main Menu แล้วกด Play

สคริปต์สร้างเฉพาะไฟล์ Meadow ที่เป็นผลลัพธ์ของงานนี้ ไม่ได้แก้ไฟล์ Blender อื่น ฉากเมนูก่อนเปลี่ยนฉากหลังเก็บใน `BeforeMeadow3D.unity.txt` (ไม่ให้นำเข้าเป็นฉากซ้ำ)

ภาพจาก Blender: `Preview/Blender_Meadow.png`, `Preview/Blender_Detail.png`; ข้อมูลต้นฉบับ: `ModelInfo.json`; ผลตรวจแกน ขนาด และ Vertex Colors หลังนำเข้า: `UnityImport.json`

## ผลตรวจหลังใช้งานใน Unity

- นำเข้า Mesh ครบ **37 ชิ้น**, Vertex Color ครบทุก Mesh ขนาดรวม **36.23 × 0.39 × 26.32 เมตร** (X/Y/Z) ไม่มีตัวละครหรือ Animation Rig
- เมนูเดิมผ่าน **34/34** หลังเปลี่ยนฉากหลัง รวม Play ไป Start, Setting/Back, Quit, เสียง และ Main Menu/Setting ที่ **9 ขนาดจอ** รายงาน `MenuValidation.json`
- ภาพจากกล้องและ UI ที่ทำงานจริง: `Preview/Unity_Menu_Meadow3D.png`
- ฉาก Start และการแก้ Mobile_RPAsset ที่มีอยู่ก่อนเริ่มงานยังไม่เปลี่ยน รายงาน `Preservation.json`
- โมเดลเหมาะกับฉากหลังเมนูบน PC; ยังไม่ได้ทำ LOD หรือทดสอบ Standalone build
