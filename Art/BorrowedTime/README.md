# BORROWED TIME — Main Menu

**UI รุ่นล่าสุด:** กระจกเขียวหยกอมฟ้าเพิ่มความชัดจากสนามหญ้า พร้อมใบไม้เวกเตอร์ไหวเบา ๆ ตามมุมกรอบ ไม่เจนภาพเพิ่ม เก็บปุ่มตัดมุมขวา ขอบสองชั้น แสงสะท้อน และชื่อ Borrowed Time ไว้ ดูโค้ดและผลตรวจ **35/35** ใน [LeafGlass/README.md](LeafGlass/README.md) ชุดก่อนอยู่ใน ReferenceGlass และ GreenGlass

**ฉากหลังรุ่นล่าสุด:** ใช้โมเดลหญ้า 3D จาก Blender แทนภาพพื้นหลังแล้ว กล้องมองลง 78° รายละเอียดไฟล์ .blend / FBX / Prefab และภาพจาก Unity อยู่ใน [Meadow3D/README.md](Meadow3D/README.md) ภาพหญ้าและพุ่มไม้แบบเดิมยังเก็บไว้เป็นตัวเลือก ส่วน UI และระบบปุ่มยังใช้ชุดเดิม

เปิด `Assets/Scenes/BorrowedTimeMainMenu.unity` ใน Unity 6000.3.25f1 แล้วกด Play

โปรเจกต์หลังล้างงานเดิมไม่มี UI เหลืออยู่ จึงใช้ UI Toolkit แบบที่เคยใช้ในโปรเจกต์ โดยแยกไฟล์ UXML/USS และ Controller ไม่มี Canvas หรือ EventSystem อีกชุด

## การใช้งาน

- **Play** เปิดฉาก `Start` ตามที่ผู้ใช้เลือก ปรับปลายทางได้ที่ `BorrowedMainMenu.playSceneName` บน GameObject **Borrowed Time Main Menu UI** และเพิ่มฉากนั้นใน Build Settings
- **Setting** เปิดเสียง Master / Music / Effects, Mute, Resolution และ Fullscreen ปรับเสียงทีละ 1% ด้วย SliderInt / ปุ่มลูกศร หรือพิมพ์เลข 0–100 บันทึกด้วย PlayerPrefs ของ BORROWED TIME แยกจากงานเกมเก่า ปิดด้วย Back หรือ Esc
- **Quit** ออกจากเกม ใน Editor จะหยุด Play Mode
- UI หลักมีเพียง Play / Setting / Quit ชื่อเกมอยู่เหนือ Play และกึ่งกลางตรงกับกลุ่มปุ่มทางขวา Hover ยกและขยายปุ่มพร้อมสีเหลืองอ่อน Pressed ยุบลง Transition ใช้เวลา 0.16 วินาที รองรับ mouse/keyboard และ focus

## ชั้นภาพและพื้นที่ตัวละคร

1. **Background รุ่นล่าสุด**: Prefab `BT Meadow 3D Background` เป็น Mesh พื้น หญ้า โคลเวอร์ และดอกไม้จริง ใช้ `MeadowMenuView` จัดมุมมอง ส่วน Quad `Background - painted meadow` และ `MeadowBackdrop` แบบเก่าถูกปิดไว้
2. **Foreground Foliage**: `foreground-foliage` ใน UXML เป็น PNG โปร่งใสแยกชั้น รับ pointer ไม่ได้ รุ่นมองลงพื้นหญ้า 3D ซ่อนชั้นนี้ผ่าน `showForegroundFoliage` บน BorrowedMainMenu
3. **Falling Leaves**: `FallingLeavesLayer` ภายใน `falling-leaves` เป็นชั้น particle ของ UI แยกต่างหาก มีใบไม้ 18 ใบ เคลื่อนจากเหนือจอไปพ้นด้านล่าง แกว่งเอียง หมุน และพับตามระยะเวลา ใช้ unscaled real time และไม่บังการกดปุ่ม ไม่ใช่ใบไม้ที่อบลงบน Background
4. **CharacterPlacement - empty for future sleeping model**: Transform ว่างบนจอด้านซ้ายกลาง ปรับ `characterViewport` ที่ MeadowBackdrop เพื่อเปลี่ยนตำแหน่ง ตั้งโมเดลเป็นลูกของ Transform นี้แล้วจัดท่านอนเอง โมเดลอยู่ด้านหน้าภาพพื้นหญ้าและด้านหลัง UI ไม่มีการใส่ตัวละครลงในภาพ

## ระบบเสียง / ภาพ

Master ใช้ `AudioListener.volume`; Music/Effects ใช้ `MenuAudioBus` คูณระดับเสียงต้นฉบับของ AudioSource เพิ่ม component แล้วเลือก channel = Music สำหรับเพลง AudioSource ที่มีอยู่ตอนโหลดฉากและยังไม่ระบุหมวดจะถูกจัดเป็น Effects; source ที่สร้างระหว่างเกมให้เพิ่ม MenuAudioBus ด้วย เกมใหม่ยังไม่มีเพลงประกอบ จึงไม่ได้เพิ่มแทร็กเพลงใหม่

Resolution ใช้ค่าจาก Screen.resolutions พร้อมขนาดหน้าต่างปัจจุบัน หากแพลตฟอร์มไม่คืนรายการจะมี 1280×720, 1600×900 และ 1920×1080 เลือกแล้วเรียก Screen.SetResolution และจำค่า ใน Standalone จะโหลดค่าภาพที่บันทึกไว้กลับเมื่อเปิดเมนู ใน Unity Editor ขนาด Game View ควบคุมจาก Editor ตามปกติ

## ไฟล์หลัก

- `Assets/BorrowedTime/UI/BorrowedMainMenu.uxml`, `.uss`, `BorrowedMenuTheme.tss`, `BorrowedMenuPanelSettings.asset`
- `Assets/BorrowedTime/Runtime/BorrowedMainMenu.cs`: callback, keyboard, audio and display controls
- `MeadowBackdrop.cs`: background cover และจุดวางโมเดล
- `MenuIllustration.cs`: ไอคอนใบไม้/เฟือง/ออก และใบไม้ร่วง
- `MenuAudioSettings.cs`, `MenuAudioBus.cs`: ระดับเสียงที่จำข้ามฉาก
- `Assets/BorrowedTime/Editor/BorrowedMenuBuilder.cs`: สร้าง/เปิดฉากผ่าน Tools > Borrowed Time > Create or Open Main Menu
- `BorrowedMenuValidation.cs`: ตรวจการกดปุ่มจริง เสียง และ layout ผ่าน runtime panel หลายสัดส่วน เครื่องมือคืนค่าระดับเสียงของผู้ใช้เมื่อจบ

## ภาพและลิขสิทธิ์

สร้างอาร์ตด้วย **built-in image_gen** แยก 4 ภาพ ไม่ได้นำภาพเมนูทั้งภาพมาทับ UI Prompt ฉบับเต็มอยู่ใน `GenerationPrompts.json` ภาพใช้งานจริงอยู่ใน `Assets/BorrowedTime/UI/Art/`:

- `MeadowBackground.png`: พื้นหญ้าสีน้ำ ไม่มีตัวละครหรือ UI
- `BorrowedTimeLogo.png`: โลโก้ BORROWED TIME โปร่งใส
- `BotanicalButton.png`: ป้ายครีมไม่มีข้อความ Text และไอคอนเป็น UI จริง
- `ForegroundFoliage.png`: พุ่มไม้โปร่งใส

ฟอนต์ปุ่มและ Setting ใช้ [Patrick Hand จาก Google Fonts](https://github.com/google/fonts/tree/main/ofl/patrickhand) พร้อมไฟล์ SIL OFL ใน `Assets/BorrowedTime/UI/Fonts/OFL.txt`

ไม่ได้แก้ฉาก Start หรือเนื้อหาเกมอื่น การเปลี่ยน Build Settings มีเฉพาะเพิ่มเมนูไว้ก่อนฉาก Start การเปลี่ยน productName เป็น BORROWED TIME ตามชื่อเกมใหม่

## ผลตรวจใน Unity Editor

- **34 ผ่าน / 0 ไม่ผ่าน**: Title เหนือ Play, ปุ่มหลักครบสามปุ่ม, Setting/Back ผ่าน navigation event จริง, เสียงสามหมวดเชื่อม AudioSource/AudioListener จริง, clamp 0–100, จำค่าและ Mute/Restore, Play โหลด Start และ Quit ออกจาก Play Mode
- Main Menu และ Setting ผ่าน **9 ขนาดจอ**: 1920×1080, 1280×720, 2560×1440, 1024×768, 2560×1080, 720×1280, 800×600, 1088×1445 และ 1280×1024 ตรวจกลุ่มปุ่มฝั่งขวา ลำดับชื่อ/ปุ่ม ขอบจอ และการเลื่อนหน้าตั้งค่าเมื่อพื้นที่ไม่พอ
- Console: 0 Error / Warning; Scene validation: 0 missing scripts / broken prefabs
- รายงานเต็ม: `Validation.json`; ฉาก Start และการแก้ Mobile_RPAsset ที่มีอยู่ก่อนเริ่มงานยังไม่เปลี่ยน: `Preservation.json`
- ภาพจาก Unity runtime render: `Menu_1920x1080.png`, `Menu_1024x768.png`, `Menu_720x1280.png`, `Settings_1920x1080.png`, `Settings_720x1280.png`
- รันทดสอบซ้ำด้วย **Tools > Borrowed Time > Validate Main Menu** ขณะอยู่นอก Play Mode ชุดตรวจคืนค่าเสียงของผู้ใช้เมื่อจบ ยังไม่ได้ทดสอบ Standalone build
