# BORROWED TIME — Green Glass Menu

เปลี่ยนหน้าตา UI Toolkit เดิมตามภาพอ้างอิงใหม่ เป็นแผง/ปุ่มกระจกสีเขียว ขอบสีมะนาวซ้อน แสงสะท้อน ใบไม้และดอกไม้เล็ก ๆ ใช้ชื่อ **BORROWED TIME** บนแผงด้านบน ปุ่มหลัก **Play / Settings / Exit** อยู่ด้านขวาตามตำแหน่งเดิม

## ไฟล์ที่เปลี่ยน

- `Assets/BorrowedTime/UI/BorrowedMainMenu.uxml`: ตัวอักษรชื่อเกมจริงสองบรรทัด BORROWED / TIME และป้าย Settings / Exit โดยคงชื่อปุ่ม/Callback เดิม
- `Assets/BorrowedTime/UI/GreenGlassMenu.uss`: ผิวกระจกสีเขียว ฟอนต์สีขาว ขอบ/เงา และ Hover/Pressed/Focus พร้อมขนาดตัวอักษรสำหรับจอแคบ
- `Assets/BorrowedTime/Runtime/MenuIllustration.cs`: เส้นสว่างฝั่งซ้ายและลูกศรฝั่งขวาวาดด้วย Painter2D ให้คมตามความละเอียด ไม่รับ pointer หรือ focus
- `Assets/BorrowedTime/Runtime/BorrowedMainMenu.cs`: ติดรายละเอียดกระจกให้ปุ่มเดิม แทนไอคอนชุดเก่า โดยยังเรียก Play → Start, Settings เดิม และ Exit ตามแพลตฟอร์ม
- `Assets/BorrowedTime/Editor/BorrowedMenuValidation.cs`: ตรวจชื่อ BORROWED TIME จริง ป้ายปุ่มทั้งสาม และความพอดีของตัวอักษรชื่อเกมทุกสัดส่วน

## อาร์ต

สร้างเฉพาะพื้นผิวไม่มีตัวอักษรด้วย **built-in image_gen** พร้อม Alpha โปร่งใสจริง แล้วนำไปใช้เป็น background ของ Button/Title ที่ใช้งานได้ใน Unity ไม่ใช่ภาพเมนูสำเร็จรูปซ้อนทับจอ

- `Assets/BorrowedTime/UI/Art/GreenGlassButton.png`
- `Assets/BorrowedTime/UI/Art/GreenGlassTitle.png`
- Prompt เต็ม: `GenerationPrompts.json`

ตัวอักษรชื่อเกมใช้ [Lobster Two Bold Italic](https://github.com/google/fonts/tree/main/ofl/lobstertwo) ปุ่มใช้ [Quicksand](https://github.com/google/fonts/tree/main/ofl/quicksand) มีไฟล์ SIL OFL ของแต่ละฟอนต์ใน `Assets/BorrowedTime/UI/Fonts/` ส่วนหน้าตั้งค่าเก็บแบบเดิมไว้

ฉากหญ้า 3D กล้อง ตำแหน่งพื้นที่ตัวละคร ฉาก Start และโมเดล Blender ไม่ได้แก้ สำรองโค้ดและ UI ก่อนปรับอยู่ใน `Before/` พร้อม Hash สำหรับเทียบฉาก/โมเดล/Prefab

## ผลตรวจ

- ผ่าน **35/35** ใน Unity Editor รวมข้อความชื่อเกม ป้าย Play/Settings/Exit, การกดปุ่มเดิมจริง, เสียง และ Main Menu/Settings ที่ 9 ขนาดจอ
- ชื่อเกมและปุ่มพอดีทั้งจอกว้าง 4:3 Ultrawide และแนวตั้ง; ไม่มี Error/Warning ใน Console
- ผลเทียบ SHA-256 ของฉากเมนู FBX หญ้า Prefab หญ้า และ Start ตรงกับก่อนแก้ทุกไฟล์: `Preservation.json`
- ภาพใช้งานจริง: `Menu_GreenGlass.png`; รายงานเต็ม: `Validation.json`
