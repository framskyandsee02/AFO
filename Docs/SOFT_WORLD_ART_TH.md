# OOPS! PLANET — Soft World map art

วันที่ 23 ต.ค. 2026 เจ้าของให้ใช้ Kirby เป็นแนวอ้างอิง: รูปทรงกลมมน ไม่มีขอบแข็ง สีสดและบรรยากาศเป็นมิตร ใช้หลักการรูปทรงและสีเพื่อออกแบบแมพ OOPS! PLANET โดยสร้าง geometry ใหม่ใน Blender ไม่ได้นำ asset ของ Kirby มาใช้

ภาพอ้างอิง: https://www.nintendo.com/zh-hans-hk/switch/arzga/town/index.html

## ไฟล์ทำงาน

- `ArtSource/Blender/OOPS_V2_SoftWorld.blend` — เปิดฉาก Soft World Final ที่ active อยู่ โมเดลเก่าเก็บแยกฉากไว้
- `ArtSource/Blender/build_soft_world.py` — สคริปต์สร้างและส่งออกแมพจาก Blender
- `ArtSource/Blender/SoftWorld_layout.json` — ตำแหน่ง รูปทรง และชื่อชิ้นฉากจาก V2
- `ArtSource/Blender/SoftWorld_palette.json` — สีที่ใช้
- `Assets/OopsPlanet/Models/SoftWorld/SoftWorld_Modules.fbx` — 194 rounded mesh variants รวม cube และทางลาด
- `Assets/OopsPlanet/Models/SoftWorld/SoftWorld_Garden.fbx` — เนิน พุ่มไม้ ต้นไม้ ดอกไม้ เมฆ ปล่องโรงงาน และรายละเอียดสวน รวม 15 material batches
- `Assets/OopsPlanet/Models/SoftWorld/SoftWorld_manifest.json` — จับคู่ mesh กับ object เดิมใน Unity
- `Assets/OopsPlanet/Editor/OopsSoftWorldArt.cs` — ตัวใส่งานภาพและจัดแสงในฉาก

## สิ่งที่เปลี่ยนในเกม

เปลี่ยน mesh ของฉากเดิมเป็นขอบมน ซ่อนเส้นกริด debug เปลี่ยนกำแพงขอบเป็นขอบสวนปุ่มมน ปรับโทนหญ้า น้ำ อาคาร และแสง เพิ่มรายละเอียดบริเวณ Hub/โรงงาน/นาข้าว ทำป้ายบอกทางให้เล็กลง และเพิ่มผิวทางลาดเถาวัลย์ที่แสดงเมื่อปริศนาเปิดทาง

ตำแหน่งและ collider ของปริศนาเดิมคงเดิม ส่วนของตกแต่งใน Garden เป็น visual mesh ไม่มี collider ไม่ได้เปลี่ยน project settings, Build Settings หรือโมเดล BLOB

เมนู `Tools → OOPS Planet → V2 → Apply Blender Soft World Art` ใส่งานภาพซ้ำได้ ตัว V2 builder เรียกขั้นนี้อัตโนมัติเมื่อมี asset พร้อม จึงไม่หายเมื่อ Rebuild ต้องออก Play Mode ก่อน

## การตรวจสอบ

Validate V2 และ compilation ผ่าน ทดสอบหลังเปลี่ยนงานภาพ: **57/57**, คอมโบ **30/30** (`Assets/OopsPlanet/Evidence/v2-soft-world-test.json`, UTC 2026-10-23T10:55:58.6167493Z) ไม่พบ console error ในรอบยืนยัน

ตรวจหลังบันทึกและเปิดฉากใหม่: วัสดุ Garden 15/15 ชุดคงอยู่ครบ มีการบันทึก prefab material overrides ไว้แล้ว ภาพรวมอยู่ `Docs/Images/SoftWorld_Overview.png` (ถ่ายใน Unity โดยปิดหมอกชั่วคราวเฉพาะตอนถ่ายภาพรวม และคืนค่าหลังถ่าย)

งานนี้เป็นการทำภาพแมพรอบแรก ยังไม่ได้ทำโมเดลรูปร่างพลังใหม่บน BLOB, เสียง/VFX ธาตุใหม่, มาตรวัดมลพิษ หรือ build PC
