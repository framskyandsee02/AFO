# รวม Vrzo เข้ากับ OOPS — 2026-10-23

ดึง origin/Vrzo commit a637291 ด้วย git pull --no-rebase origin Vrzo เกิด merge commit 6f0c474 บน local main ยังไม่ push
งานเดิมคืนจาก stash แล้ว และตรวจว่า BlobExpression, BlobFeedback, VfxKit, BlobElementPowers ตรงกับสำเนาก่อน pull
สำรอง: E:/!for WORK/GPT/AFO_before_Vrzo_20261023_212148.zip และ stash ชื่อ Before Vrzo pull: local folder move and animation preserved (ยังเก็บไว้)

ปรับแพ็กเกจ Pino ให้ใช้ OopsPlanet.BlobPlayer/BlobProperty แทน WobPlayer/WobProperty
ฟอร์มเดิม Spring=เด้ง, Gas=แรงดัน, Liquid=น้ำ, Magnet=แม่เหล็ก; คงชื่อ blendshape และไฟล์ profile เดิมพร้อมแปลงค่า enum ที่ serialize
ไฟฟ้า/ความร้อนแสดงทรง Normal เพราะแพ็กเกจไม่มีฟอร์มสำหรับสองพลังนี้ ยังใช้พลังเกมได้ตามเดิม
ไม่มีระบบ flattened ใน OOPS จึงไม่เปิด LiquidFlow จาก gameplay; shape ยังดูตัวอย่างด้วยเครื่องมือได้
Pino อ่านเฉพาะ yaw ของ visual เดิมเพื่อไม่ให้โยกตัวซ้ำกับ locomotion ของ Pino
Builder สร้าง PinoGameplayIntegration จากสำเนาฉาก V2 ปัจจุบัน ไม่แทนตัวละครในฉาก V2 ต้นฉบับ
แก้ ZoneAmbience ให้ผูก AudioSource กลับเมื่อ cache หาย ทดสอบล้าง cache แล้ว source คง 6 ตัว ไม่มีเสียงซ้ำ

เทสต์ geometry/adapter 87/87, locomotion 27/27 ผ่านแล้ว
เทสต์รวม V2 ในฉาก Pino ผ่าน 57/57 คอมโบ 30/30 และ stateMismatchSamples=0; ดู Assets/PinoCharacter/Evidence/PlaymodeIntegration.json และ GameplayRoute.json
การทดสอบใช้ test_v2/test_v2_reset ไม่ใช้เซฟจริง และเครื่องมือคืนผลรายงาน V2 ต้นฉบับหลังเสร็จ
การ port และผลทดสอบยังเป็น local changes ไม่ได้ commit เพิ่มหรือ push