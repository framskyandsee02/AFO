# OOPS! PLANET — We R in Trouble! (Prototype 01)

เกม 3D puzzle adventure มุมกล้อง third-person บน PC ผู้เล่นเป็น **BLOB** (ตัวละครที่ไม่พูด) ที่ดูดซับพลังจากวัตถุมาใช้แก้ปริศนาเพื่อฟื้นฟูโลก
ในโปรโตไทป์มี Hub, โซนโรงงาน และโซนนาข้าว รวมพลัง 3 แบบและปริศนา 3 ชุด

> Repo เดิมชื่อ "Wobble Works" ซึ่ง **เลิกใช้แล้ว** (ดูหัวข้อ Legacy ด้านล่าง)

## สิ่งที่ต้องมี (ตรวจแล้ว ณ commit P07)
| สิ่งที่ต้องมี | เวอร์ชัน |
|---|---|
| Unity Editor | **6000.3.25f1** (ติดตั้งผ่าน Unity Hub) |
| Git + **Git LFS** | ต้องมี LFS เพราะไฟล์ `.fbx` และ `.blend` เก็บใน LFS |
| Blender (ถ้าต้องแก้โมเดล) | 5.2.2 LTS (เวอร์ชันที่ใช้สร้าง asset) |
| Packages หลัก (`Packages/manifest.json`) | URP 17.3.0, Input System 1.20.0, Test Framework 1.6.0, uGUI 2.0.0, AI Navigation 2.0.14, Timeline 1.8.13 |
| Package เครื่องมือ | `com.coplaydev.unity-mcp` (ดึงจาก GitHub branch `main` **ไม่ได้ pin เวอร์ชัน** และต้องต่อเน็ตตอนเปิดโปรเจกต์ครั้งแรก) |

ฟอนต์ไทย Noto Sans Thai (SIL OFL 1.1) อยู่ในโปรเจกต์แล้ว ไม่ต้องติดตั้งเพิ่ม ส่วนเสียงทั้งหมดสร้างด้วยโค้ด ดู license ทั้งหมดได้ที่ `Assets/OopsPlanet/ASSET_LICENSES.md`

## เริ่มเล่นใน Editor
1. `git lfs install` (ทำครั้งเดียวต่อเครื่อง) แล้ว `git pull`
2. เปิดโฟลเดอร์ repo ด้วย Unity Hub โดยเลือก **6000.3.25f1** แล้วรอให้ import และ compile เสร็จ
3. เปิด scene `Assets/OopsPlanet/Scenes/OopsPlanet_Prototype01.unity` ซึ่งเป็น scene เดียวใน Build list
4. กด Play แล้วคลิกในหน้าจอเกมเพื่อให้เกมจับเมาส์

**ปุ่มควบคุม:** WASD เดิน • SPACE กระโดด (ถ้ามีพลังเด้ง ให้กดค้างเพื่อชาร์จ) • เมาส์ มอง • **E ค้าง** ดูดซับพลัง • **F** พลังหลัก • **G** พลังเสริม • **Q** สลับพลัง • Left Shift + ปุ่มแม่เหล็ก (F หรือ G) ผลัก • R กลับจุดเช็กพอยต์ • ESC หยุดเกม/ตั้งค่า • TAB มุมมองรวม

## ⚠ Scene ถูกสร้างด้วยโค้ด
`OopsPlanet_Prototype01.unity` ถูกสร้างจาก `Assets/OopsPlanet/Editor/OopsPrototypeBuilder.cs`
เมนู **Tools → OOPS Planet → Rebuild Prototype 01** จะ **เขียนทับ scene ทั้งไฟล์** ดังนั้นถ้าจะเปลี่ยนเลย์เอาต์ ให้แก้ใน builder แล้วกด Rebuild
อย่าแก้ scene ด้วยมือ ถ้าจำเป็นต้องแก้ด้วยมือจริง ๆ ให้ตกลงในทีมก่อน เพราะการแก้นั้นจะหายเมื่อมีคนกด Rebuild (รายละเอียดอยู่ใน `Docs/HANDOFF_TH.md`)

## ทดสอบ
- **EditMode:** Window → General → Test Runner → EditMode (เทสต์ combo resolver 6 ข้อ)
- **Play Mode** (กด Play ก่อน แล้วเลือกจากเมนู Tools → OOPS Planet):
  - **Run P01-P04 Play Tests (sequence)**: เส้นทาง, การดูดซับ, พลัง, ปริศนา
  - Run P05 Progress Test (1) → หยุด Play แล้วกด Play ใหม่ → Run P05 Progress Test (2): ทดสอบเซฟข้าม session
  - Run P06 Polish Test: UI ภาษาไทย, เสียง/VFX, license
- **Validate Prototype 01:** ตรวจว่า scene ต่อสายครบ (ฟอนต์, พลัง, ปริศนา, ประตูโซน)
- รายงานผลจะถูกเขียนลง `Assets/OopsPlanet/Evidence/*.json`

เทสต์ทุกชุดใช้ save slot แยกของตัวเอง จึง **ไม่แตะเซฟจริงของผู้เล่น** (`oops_save.json`)

## Build สำหรับ PC (Windows 64-bit)
1. File → Build Profiles → เลือก Windows แล้วตรวจว่ามี scene `OopsPlanet_Prototype01` แค่ scene เดียว
2. Build ไปที่ `Builds/OopsPlanet_Win64/OopsPlanet.exe` (โฟลเดอร์ `Builds/` อยู่ใน .gitignore)
3. เปิด `OopsPlanet.exe` ได้เลยโดยไม่ต้องใช้ Unity
4. **ทดสอบในตัว build (ไม่ใช้ Editor):** สั่ง
   `OopsPlanet.exe -oopsAutoTest route,absorb,power,puzzle,perf`
   (เพิ่ม `-batchmode -nographics` ถ้าจะรันแบบไม่มีหน้าต่าง) รายงานจะไปที่ `%USERPROFILE%\AppData\LocalLow\DefaultCompany\AFO\test-reports`

**ยืนยันแล้วใน P07:** build ขนาด 99.5 MB, error 0, ผ่าน P01–P04 ทั้งแบบมีหน้าต่างและ headless ดูรายละเอียดใน `Docs/P07_Test_Checklist.md`

## เซฟเกม
ไฟล์อยู่ที่ `%USERPROFILE%\AppData\LocalLow\DefaultCompany\AFO\oops_save.json` และเก็บเฉพาะปริศนาที่แก้แล้ว, โซนที่ปลดล็อก และค่าตั้งค่า
ถ้าไฟล์เสีย เกมจะเริ่มใหม่อย่างปลอดภัย และเก็บไฟล์เดิมไว้เป็น `.corrupt.json`
"เริ่มเกมใหม่" ในเมนู ESC (ต้องกดยืนยันสองครั้ง) จะล้างความคืบหน้า

## โครงสร้างโปรเจกต์
| Path | เนื้อหา |
|---|---|
| `Assets/OopsPlanet/Scenes` | scene ต้นแบบ (ถูกสร้างจาก builder) |
| `Assets/OopsPlanet/Scripts` | `Powers/`, `Puzzles/`, `Progress/`, `Feedback/`, `Audio/`, `Testing/` และเทสต์ `Oops*Test.cs` |
| `Assets/WobbleWorks/Scripts` | **สคริปต์หลักของ BLOB:** `BlobPlayer`, `BlobWorld`, `BlobInteractable` (ชื่อโฟลเดอร์เป็นของเก่า แต่โค้ดเป็นของ OOPS) |
| `Assets/OopsPlanet/Editor` | `OopsPrototypeBuilder.cs` (สร้าง scene + Validate + เมนูเทสต์) และ `Tests/` (EditMode) |
| `Assets/OopsPlanet/Data` | ค่าพลังและคอมโบ (ScriptableObject) ปรับได้ใน Inspector |
| `Assets/OopsPlanet/Models` / `Prefabs` / `Materials` / `Fonts` / `Settings` | asset runtime |
| `ArtSource/Blender` | ไฟล์ต้นฉบับ `.blend` 7 ไฟล์ และสคริปต์ `export_to_unity.py` |
| `Docs/` | คู่มือส่งต่องาน, checklist ทดสอบ, แบบฟอร์มทดลองเล่น |

## Legacy (เลิกใช้)
`Assets/WobbleWorks/{Scenes,Prefabs,Materials,Models,Evidence}` เป็นด่าน Wobble Works เดิมที่ใช้ระบบ 4 พลัง **ไม่ได้อยู่ใน Build แล้วและเล่นไม่ได้** เก็บไว้อ้างอิงเท่านั้น

## เอกสารเพิ่มเติม
- `Docs/HANDOFF_TH.md`: วิธีเพิ่มวัตถุที่ดูดซับได้, เพิ่มพลังหรือคอมโบ, สร้างปริศนาใหม่, export จาก Blender, การแบ่งความรับผิดชอบ, ไฟล์ที่ห้ามแก้พร้อมกัน, known issues
- `Docs/P07_Test_Checklist.md`: ผลทดสอบล่าสุด
