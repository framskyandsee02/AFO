# BORROWED TIME — Original glass shapes in green

ปรับให้ตรงกับภาพ Slime Beyond Glassy Menu มากขึ้น โดยกลับไปใช้รูปทรงและวิธีวาดแบบชุดกระจกเดิม: แผงชื่อมุมมน, ปุ่มมุมขวาตัดเฉียง, ขอบบางซ้อนสองชั้น, แถบสะท้อนแสงทแยง, รายละเอียดสี่เหลี่ยมฝ้า และไล่สีโปร่งแสง เปลี่ยนพาเลตเป็นเขียวอ่อน ชื่อเกมยังเป็น BORROWED TIME โดยแสดง Borrowed / Time ด้วยฟอนต์ตัวเขียนสีขาว

## ไฟล์ใช้งาน

- `Assets/BorrowedTime/Runtime/ReferenceGlassSurface.cs`: วาดผิว/ขอบ/แสง/ลูกศรด้วย UI Toolkit mesh และ Painter2D ตามทรงกระจกเดิม
- `Assets/BorrowedTime/UI/GreenGlassMenu.uss`: สีเขียว, ขนาด, ฟอนต์, Hover/Pressed/Focus และสัดส่วนจอแนวตั้ง
- `Assets/BorrowedTime/UI/BorrowedMainMenu.uxml`: ข้อความชื่อเกมจริง และ Button เดิม Play / Settings / Exit
- `Assets/BorrowedTime/Runtime/BorrowedMainMenu.cs`: ใส่ผิวใหม่ในปุ่มเดิมและ Label ที่อ่านได้ โดยคงชื่อปุ่มและ Callback
- `Assets/BorrowedTime/Editor/BorrowedMenuValidation.cs`: ตรวจชื่อเกมโดยไม่จำกัดรูปแบบตัวพิมพ์ และตรวจข้อความ/ตำแหน่งทุกสัดส่วน

ชุดนี้ไม่ใช้ PNG ปุ่มสีเขียวที่สร้างในรอบก่อน ผิวกระจกวาดจากโค้ดและสีจึงคมชัดเมื่อปรับขนาด โดยคงอาร์ตเดิมไว้ใน Assets สำหรับย้อนกลับ ไอคอนใบไม้/ดอกไม้และกรอบหนาทรงแคปซูลไม่แสดงในชุดใหม่นี้

แก้สีผ่าน `--glass-top`, `--glass-bottom`, `--glass-rim` ใน GreenGlassMenu.uss ทั้งชื่อเกมและปุ่ม ใช้ตัวอักษรจริงสองชั้นโดย Label ด้านบนไม่รับ pointer/focus และไม่ทับเหตุการณ์ Button

## ผลตรวจ

- ผ่าน **35/35** ใน Unity Editor รวม Play → Start, Settings/Back, Exit, เสียง และ Main Menu/Settings ที่ **9 ขนาดจอ**: `Validation.json`
- Console ไม่มี Error/Warning
- ฉากเมนู โมเดล/Prefab หญ้า และฉาก Start ตรงกับ SHA-256 ก่อนปรับทุกไฟล์: `Preservation.json`
- พรีวิวจาก Unity: `Menu_ReferenceGlass.png`
- สำรอง UI ก่อนปรับ: `Before/`
