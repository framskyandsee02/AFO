# BORROWED TIME — Jade glass and animated leaves

เพิ่มช่อใบไม้ตามมุมซ้ายบน/ขวาล่างของกรอบชื่อและปุ่มเดิม โดยวาดรูปทรง แสง เงา และเส้นใบด้วย UI Toolkit Painter2D ไม่ได้เจนภาพเพิ่ม ไม่เพิ่ม texture หรือ GameObject และไม่เปลี่ยนจุดรับคลิก

เปลี่ยนแผงเป็นเขียวหยกอมฟ้าที่เข้มกว่าสนามหญ้า เพิ่มความทึบเล็กน้อยและขอบเงาสีเข้ม ช่วยให้ตัวอักษรขาวอ่านชัด โดยเก็บทรงกระจกเดิม ขอบสองชั้น แถบสะท้อนแสง และตำแหน่งองค์ประกอบไว้

ใบไม้ไหวรอบก้านประมาณ 8 องศา พร้อมปลายใบเด้งประมาณ 3 พิกเซล แต่ละช่อเหลื่อมจังหวะกัน วาดใหม่สูงสุดประมาณ 30 ครั้ง/วินาทีโดยใช้เวลาจริง จึงทำงานได้แม้ Time.timeScale เป็น 0 ตารางอัปเดตหยุดเองเมื่อ VisualElement ถูกถอดจาก panel ปุ่มและข้อความไม่ขยับตามใบไม้ เปิดดูการขยับใน Play Mode

## ไฟล์

- `Assets/BorrowedTime/Runtime/ReferenceGlassSurface.cs`: ช่อใบไม้แบบเวกเตอร์ แอนิเมชัน และเงารอบกรอบ
- `Assets/BorrowedTime/UI/GreenGlassMenu.uss`: พาเลตเขียวหยกและสี Hover / Focus / Pressed
- `Before/`: โค้ด สี และภาพก่อนปรับ
- `Preservation.json`: SHA-256 ของฉากเมนู หญ้า FBX / Prefab ฉาก Start และ Mobile_RPAsset ตรงกับก่อนปรับ

## ตรวจใน Unity Editor

ผ่าน 35/35 จากชุดตรวจเดิม ครอบคลุม Play → Start, Settings / Back, Exit, เสียง และ layout 9 ขนาดจอ รายงานอยู่ใน `Validation.json` ภาพปัจจุบันอยู่ใน `Menu_LeafGlass.png` และ `Menu_Portrait.png` ไม่ได้ทดสอบ Standalone build
