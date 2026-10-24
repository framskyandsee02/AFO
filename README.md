# BORROWED TIME — Echoes of Tomorrow

**โปรเจกต์ป่า/ต้นไม้ — ส่งต่อบน AFO/main วันที่ 2026-10-24**

เจ้าของสั่งให้ใช้โปรเจกต์นี้แทน OOPS! PLANET/Pino ที่ราก main เกมเดิมยังอยู่
ในประวัติ Git ที่ commit `d87f0c5` โฟลเดอร์ต้นฉบับทั้งสองเกมในเครื่องเจ้าของไม่ถูกแก้

## วิธีเปิด

แนะนำ clone ลงโฟลเดอร์ใหม่ เพื่อไม่ปะปนกับไฟล์แก้ค้างของเกมเดิม:

```powershell
git clone --branch main https://github.com/framskyandsee02/AFO.git BorrowedTime
cd BorrowedTime
git lfs pull
```

เปิดโฟลเดอร์ BorrowedTime ด้วย Unity **6000.3.25f1** แล้วเปิด
**Assets/Scenes/BorrowedTimeMainMenu.unity** เป็นฉากเริ่มต้นใน Build Settings
กด Play ในเมนูเพื่อเข้า **Assets/_BorrowedTime/Scenes/TrainingArea.unity**
หน้าเมนูมีตัวละครนอนบนหญ้า ลืมตาเมื่อชี้ปุ่ม เพลงพื้นหลัง และเสียงปุ่ม/ชื่อเกม
ปรับระดับเสียงได้ใน Settings ใช้ URP 17.3, Input System 1.20 และ Cinemachine 3.1.7
การเปิดครั้งแรกต้องให้ Unity resolve packages ตาม Packages/manifest.json

**อย่า Restore stash จาก OOPS! PLANET/Pino ทับโปรเจกต์นี้** เพราะเป็นคนละเกม
ให้เก็บงานเดิมในโฟลเดอร์เก่าหรือ branch แยกก่อน

## ระบบที่มี

- WASD เดิน, Shift วิ่ง, Space กระโดด, เมาส์หมุนกล้อง, Escape ปลดเมาส์
- หลอดเวลาเดียว เริ่ม 0 ไม่มีฟื้นเอง: E ค้างเร่งอายุและเก็บเวลา,
  Q ค้างใช้เวลาเพื่อย้อนอายุ ปล่อยแล้วหยุดกลางทาง
- ต้นไม้ใหญ่: Young → E → Mature และ Withered → Q → Mature
- ท่อนไม้แหล่งเวลา 6 จุด จุดละ 25; ต้นไม้ใหญ่ใช้ 60 ต่อช่วงอายุ
- ฟื้นป่าและเปิดทางข้ามร่องเหวไปเมือง
- จุดต้นแบบปริศนาเสริม 7 หมวดใต้ BT_ForestPuzzleTrail

## สถานะจริงสำหรับคนรับช่วง

ระบบต้นไม้ใหญ่ การฟื้นป่า 110 ต้น และการข้ามขึ้นเมืองเคยผ่านการตรวจ state,
พลังงานและ CharacterController แบบควบคุมใน Unity Play mode ตาม
docs/handoff/RESULT.md ซึ่งเป็นรายงานก่อนเพิ่มจุดปริศนาใหม่ทั้งหมด

**ปริศนา 7 หมวดบันทึกไว้แล้ว แต่ยังทดสอบครบทุกหมวดไม่เสร็จ**
Unity/MCP หลุดระหว่างเริ่มทดสอบและผู้ใช้หยุดการควบคุมหน้าจอ คนรับช่วงควร
เริ่มจาก compile และทดสอบตาม docs/handoff/SEVEN_PUZZLES_PLAN.md
ห้ามใช้ตัวเลขผ่านของ Pino/OOPS! PLANET มาอ้างกับเกมนี้ รอบ push ไม่ได้รันเกมใหม่

จุดทดลองใช้ greybox ร่วมกับ asset ที่ติดตั้งอยู่ ไก่จาก Animals FREE ขนเมล็ดแทนนก
ชั่วคราว เมือง/โรงงานยังเป็น greybox ตัวละครในฉากยังเป็น placeholder
งานตัวละครอนิเมะที่ทำแยกนอกโปรเจกต์ไม่ได้รวม/เชื่อมเข้าฉากใน snapshot นี้
ยังไม่ใช่เกมสามโซนที่เสร็จสมบูรณ์หรือ build release ที่ผ่าน human playtest

## เอกสาร

- docs/handoff/SEVEN_PUZZLES_DESIGN.md — รายการ 7 หมวดจากเจ้าของ
- docs/handoff/SEVEN_PUZZLES_PLAN.md — งานตรวจที่ยังค้าง
- docs/handoff/RESULT.md — รายงานต้นไม้และป่า
- GAME_BRIEF.md / WORLD_DESIGN.md — แนวคิดโลก รวมบางส่วนที่ยังเป็นแผนเดิม
- MASTER_BUILD_PROMPT.md — โครงงานและข้อกำหนดเดิม

รักษา license/readme ของ imported assets ใต้ Assets/ithappy และ Assets/LMHPOLY
และตรวจสิทธิ์การใช้งานของทีมก่อนเผยแพร่เกม
Library, Temp, Logs, Build, recovery และไฟล์สำรองไม่รวมใน Git
