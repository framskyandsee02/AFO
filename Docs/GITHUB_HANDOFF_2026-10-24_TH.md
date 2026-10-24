# ส่งต่องาน GitHub main — 2026-10-24

Repository: https://github.com/framskyandsee02/AFO

## เริ่มทำต่อ

1. Clone repository และ checkout `main` แล้วรัน `git lfs pull` เพื่อดึง `.blend`,
   `.fbx` และเสียงจริง ไม่ใช่เฉพาะไฟล์ pointer
2. เปิดโฟลเดอร์โปรเจกต์ด้วย Unity **6000.3.25f1** ตาม `ProjectVersion.txt`
3. ฉากปัจจุบันอยู่ที่ `Assets/Scenes/OopsPlanet/Scenes/`:
   `OopsPlanet_V2.unity` และ `OopsPlanet_Factory.unity`
4. อ่าน `README_AI_HANDOFF.md`, `Docs/DESIGN_V2_TH.md`, `Docs/COMBOS_15_TH.md`,
   `Docs/FACTORY_INTERIOR_TH.md` และ `Docs/FACTORY_INTERIOR_VERIFICATION_2026-10-24_TH.md`

## สิ่งที่รวมในการส่งต่อ

- การย้ายเนื้อหาเกมจาก `Assets/OopsPlanet/` ไป `Assets/Scenes/OopsPlanet/`
  โดยรักษา GUID ของโฟลเดอร์/ไฟล์ที่ย้าย ตรวจแล้วไฟล์ที่ลบจากพาธเก่ามีคู่ในพาธใหม่ครบ
- งาน Pino, คอมโบ, ภาพแมพ และการขยายโรงงานที่ค้างใน working tree
- ต้นฉบับ Blender และสคริปต์สร้างงานที่เกี่ยวข้อง โดยใช้ Git LFS ตาม `.gitattributes`
- รายงานตรวจและภาพประกอบใน `Docs/`
- Snapshot รายงาน JSON ที่เครื่องมือเก่ายังเขียนลงพาธเดิม เก็บที่
  `Docs/LegacyEvidenceSnapshot/` เพื่อไม่ให้หายไปกับการย้ายโฟลเดอร์

ไฟล์สำรอง `.blend1`, `Backups/`, cache/build, ไฟล์ zip, staging/render ชั่วคราว
และโฟลเดอร์ prompt ไม่รวมในการส่งต่อตามกติกา repo โดยยังอยู่ในเครื่องเดิม
เครื่องมือเก่าบางตัวอาจยังสร้าง evidence ใน `Assets/OopsPlanet/` ซึ่งเป็น output
ที่ ignore ไว้ ไม่ใช่ตำแหน่ง source ปัจจุบัน

## ขอบเขตการตรวจรอบส่ง Git

รอบนี้ตรวจสถานะ Git, ความครบของการย้ายไฟล์, พาธฉากใน Build Settings,
ข้อยกเว้นไฟล์, staged diff และ Git LFS ก่อนเผยแพร่ ไม่ได้รันเกมหรือเทสต์ Unity ใหม่
ตัวเลขผ่าน/ข้อจำกัดในรายงานเดิมเป็นผลจากรอบที่ระบุในเอกสารนั้น ไม่ใช่ผลทดสอบรอบ push
โดยเฉพาะเคสแพ `f5_cooling` ยังมีข้อสังเกตเรื่อง respawn ตามรายงานโรงงาน

ไม่แก้เซฟจริงของผู้เล่น และไม่เปลี่ยนระบบเกมเพิ่มเติมในรอบส่งต่อ
