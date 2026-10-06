# Project-Z: สรุปงาน UI เมนู (Handoff สำหรับ Gemini / Cursor)

**Engine:** Unity 2022.3.62f3 | **UI:** uGUI (Canvas) + TextMeshPro | **Scene:** `MainMenu`
**Network:** Photon Fusion 2 | **สคริปต์ C#:** Cursor เป็นผู้เขียนทั้งหมด (UI ฝั่งนี้ทำเฉพาะหน้าตา/Layout ยังไม่มีโค้ดผูก)

> หมายเหตุ: สคริปต์ `MenuManager.cs`, `RoomItem.cs`, `OptionGroup.cs` ที่เคยได้ในแชตก่อนหน้า **เลิกใช้แล้ว** ให้ Cursor เขียนเองตามโครงสร้างด้านล่าง

---

## 1. ภาพรวมและแนวทางดีไซน์

เมนูธีมทหาร/เอาชีวิตรอดสีเขียวเข้ม ตามภาพต้นแบบ (หน้า: Main, Select Mode, Create Room, Join Room, Option, Loading)

### Canvas
- Canvas Scaler: **Scale With Screen Size**, Reference **1920x1080**, Match 0.5
- ทุก Panel เป็นลูกของ `Canvas` ยืดเต็มจอ และ **สลับกันด้วย SetActive** (Scene เดียว ไม่แยก Scene)
- สถานะตอนบันทึก: เปิดเฉพาะ `MainPanel` นอกนั้นปิดหมด

### พาเลตสี
| ใช้กับ | Hex |
|---|---|
| พื้นหลัง | `0B0E0A` |
| ช่องกรอก/พื้นที่เนื้อหา | `0A0D09` |
| ปุ่ม/กรอบปกติ | `111610` |
| เส้นขอบ (Outline) | `2A3A26` |
| สีเน้น/ตัวเลือกที่เลือก | `3B5228` |
| ปุ่มหลัก (PLAY, CREATE, JOIN) Normal / Hover | `3F5A2A` / `5A7F3A` |
| Pressed / ตัวหนังสือเน้น / Fill ของ Slider | `7FA35A` |
| ตัวหนังสือหลัก | `E8ECDF` |
| ตัวหนังสือรอง | `7D8A72` |
| ข้อความ error | `D9805F` |

### ฟอนต์
- **Oswald** (Font Asset: `Oswald SDF`, ตั้งเป็น Default ของ TMP แล้ว)
- **ไม่มีตัวอักษรไทย** ข้อความ UI ทั้งหมดเป็นอังกฤษ
- ข้อจำกัด: ช่องชื่อผู้เล่นพิมพ์ไทยไม่ได้จนกว่าจะเพิ่ม Fallback Font ไทย (เช่น Noto Sans Thai แบบ Dynamic)

### สไตล์ปุ่ม (ใช้ซ้ำทุกหน้า)
Button Transition = Color Tint, Image Color = ขาว (สีมาจาก Tint)
- ปกติ: Normal `111610` / Highlighted `3B5228` / Pressed `7FA35A` / Selected `3B5228` / Disabled `0A0D09`
- ปุ่มหลัก: Normal `3F5A2A` / Highlighted `5A7F3A` / Pressed `7FA35A`

### เทคนิค Layout ที่ใช้
แต่ละหน้ามี `MenuColumn` ที่ใช้ **Vertical Layout Group** (Control Child Size ติ๊ก Width+Height, Child Force Expand ไม่ติ๊ก) และแต่ละชิ้นกำหนดขนาดด้วย **Layout Element** (ปุ่มทั่วไป Preferred Width 420 / Height 55-60)

---

## 2. โครงสร้าง Hierarchy และสถานะรายหน้า

```
Canvas
├─ Image                (พื้นหลัง)
├─ MainPanel            ✅ เสร็จ
├─ ModePanel            ✅ เสร็จ
├─ CreatePanel          ✅ เสร็จ
├─ JoinPanel            ✅ เสร็จ
├─ OptionPanel          🟡 เสร็จเฉพาะแท็บ AUDIO
└─ LoadingPanel         ✅ เสร็จ (ต้องอยู่ล่างสุดใต้ Canvas เพื่อทับทุกหน้า)
EventSystem
```

### MainPanel ✅
| Object | หน้าที่ |
|---|---|
| `PROJECT-Z` (TMP) | ชื่อเกม |
| `MenuColumn` | Vertical Layout Group |
| `NameInput` (TMP_InputField) | ช่องกรอกชื่อผู้เล่น จำกัด 16 ตัวอักษร Placeholder "ENTER PLAYER NAME" |
| `PlayButton` | ปุ่มหลัก (เขียว) |
| `OptionButton` | เปิด OptionPanel |
| `ExitButton` | ออกเกม |

ต้องการให้ Cursor ทำ: ตรวจชื่อ (ขั้นต่ำ 2 ตัวอักษร), บันทึกชื่อ (PlayerPrefs), ส่งชื่อไปใช้เป็น Nickname/ป้ายชื่อบนหัวตัวละครผ่าน Fusion
**ถ้าต้องการข้อความเตือนใต้ช่องชื่อ** ให้เพิ่ม TMP Text (`NameError`, สี `D9805F`) ใต้ `NameInput` (ยังไม่ได้สร้างใน Scene เช็กก่อนผูก)

### ModePanel (SELECT MODE) ✅
`QuickPlayButton` (ปุ่มหลัก), `CreateRoomButton`, `JoinRoomButton`, `BackButton`
(อาจมี TMP Text แสดง "PLAYER: ชื่อ" ถ้าเพิ่มภายหลัง เช็กใน Scene)

### CreatePanel ✅
| Object | ชนิด / หน้าที่ |
|---|---|
| `RoomNameInput` | TMP_InputField ชื่อห้อง |
| `MaxPlayersRow` | `MaxLabel`, `MinusButton`, `MaxPlayersText` (ค่า 1-4 เริ่มที่ 4), `PlusButton` |
| `DifficultyRow` | ปุ่ม 3 ตัว: `EasyButton` `NormalButton` `HardButton` (เลือกได้ทีละอัน) |
| `MapRow` | ปุ่ม 3 ตัว: `DowntownButton`/ชื่อแมพ ฯลฯ (**ใน Scene ปุ่มแรกชื่อ SCHOOL** ตามด้วย HOSPITAL, WAREHOUSE) เลือกได้ทีละอัน |
| `PrivateToggle` | Toggle ห้องส่วนตัว (Label "PRIVATE ROOM") |
| `CreateButton` | ปุ่มหลัก สร้างห้อง |
| `BackButton` | กลับ ModePanel |

ต้องการให้ Cursor ทำ: ไฮไลต์ปุ่มที่เลือกในกลุ่ม Difficulty/Map (ตัวที่เลือก = `3B5228`, ที่เหลือ = `111610`), ปุ่ม − + จำกัด 1-4, เรียก Fusion สร้าง Session/ห้อง, แสดง LoadingPanel ระหว่างรอ
ลำดับแมพ: 0 = SCHOOL (หรือ DOWNTOWN ถ้าเปลี่ยนกลับ), 1 = HOSPITAL, 2 = WAREHOUSE (ให้ Cursor ยืนยันกับ Scene/แมพจริง)

### JoinPanel ✅
```
JoinPanel
├─ JOIN ROOM (TMP หัวข้อ)
└─ MenuColumn
   ├─ Scroll View → Viewport → Content   ← ที่วางแถวห้อง
   └─ BackButton
```
- `Content`: Anchor top-stretch, Pivot (0.5, 1), **Vertical Layout Group** (Padding 8, Spacing 8, Control Child Size W+H, Force Expand เฉพาะ Width) + **Content Size Fitter** (Vertical Fit = Preferred Size)
- Scroll Rect: เลื่อนแนวตั้งอย่างเดียว, Movement Type Clamped
- **Prefab `RoomItem`** (แถวห้อง 1 แถว, สูง 70, Horizontal Layout Group):
  `LockIcon` (แสดงเมื่อห้องส่วนตัว), `TextBlock` → `NameText` + `InfoText` (เช่น "EASY / SCHOOL"), `CountText` (เช่น "3/4"), `JoinButton` (ข้อความ JOIN, Disabled = ห้องเต็ม สี `0A0D09`)
- ต้องการให้ Cursor ทำ: ดึงรายชื่อห้องจาก Fusion → Instantiate RoomItem ลง `Content`, ห้องเต็มให้ปุ่มเป็น Disabled/ข้อความ "FULL", กด JOIN แล้วแสดง LoadingPanel
- ไม่มีข้อความ "NO ROOMS FOUND" (ถ้าต้องการให้เพิ่ม TMP Text กลาง Scroll View)

### OptionPanel 🟡
```
OptionPanel
├─ OPTION (หัวข้อ)
└─ MenuColumn
   ├─ TabRow         (TabAudio, TabGraphics, TabControls)
   ├─ AudioContent   ✅
   ├─ GraphicsContent  ⏸ ยังไม่ทำ
   ├─ ControlsContent  ⏸ ยังไม่ทำ
   └─ BackButton
```
- **AudioContent ✅**: Slider 3 แถว `MasterRow`, `MusicRow`, `SfxRow` (ตัด Voice Chat ออกแล้ว) แต่ละแถวมี `LabelText`, `ValueText` (เช่น "80%"), `Slider` (Min 0, Max 1; ค่าเริ่ม 0.8 / 0.6 / 0.75)
- ต้องการให้ Cursor ทำ: ผูก Slider กับ AudioMixer, อัปเดต `ValueText` ตามค่า, บันทึกค่า, สลับแท็บ (เปิดทีละ Content; สีแท็บที่เลือก `3B5228` ที่เหลือ `111610`)
- **GraphicsContent / ControlsContent**: **พักไว้ก่อน** จะทำเมื่อรู้ว่าเกมมี Action/ตัวเลือกกราฟิกอะไรบ้าง
  - **งานเคลียร์ที่ยังค้าง:** ใน `MenuColumn` เคยมี `GraphicsContent` / `ControlsContent` ซ้ำอย่างละ 2 ตัว และบางตัวมี Row เสียงติดมา → ต้องลบตัวซ้ำให้เหลืออย่างละ 1 และเคลียร์ลูกให้ว่างก่อนให้ Cursor ผูก
  - ข้อเสนอ: ซ่อน `TabGraphics` และ `TabControls` ไว้ก่อน (ติ๊ก Active ออก) เพื่อไม่ให้ผู้เล่นเห็นแท็บเปล่า
  - แผนที่ออกแบบไว้: Graphics = Resolution / Display Mode / Quality (Dropdown), VSync (Toggle), FPS Limit (Dropdown); Controls = Scroll View รายการคีย์ + Mouse Sensitivity (Slider)

### LoadingPanel ✅
```
LoadingPanel   (Image เต็มจอ สี 0A0D09 alpha ~220, Raycast Target = เปิด → บังการกดปุ่มข้างหลัง)
└─ LoadingBox  (กลางจอ 460x230, Vertical Layout Group)
   ├─ Spinner        (Image, Filled Radial 360, Fill Amount 0.75, สี 7FA35A; มี SpinnerHole เป็นวงเจาะกลาง)
   ├─ LoadingText    ("Loading..." / "Creating Room..." / "Joining Room...")
   └─ LoadingSubText ("Connecting to Server...")
```
- **เปิด/ปิดด้วย `SetActive` ของ `LoadingPanel`** (ไม่มี Canvas Group; ถ้าอยากได้ fade ให้ Cursor เพิ่มเอง)
- ต้องการให้ Cursor ทำ: เปิดตอนกด Create/Join/Quick Play, หมุน `Spinner` รอบแกน Z, เปลี่ยนข้อความตามสถานะ, timeout/แสดง error เมื่อเชื่อมต่อไม่สำเร็จแล้วปิดเอง
- ต้องอยู่ **ล่างสุดใต้ Canvas** เพื่อให้แสดงทับทุกหน้า

---

## 3. สิ่งที่ยังไม่ได้ทำ / ข้อควรระวัง

1. **Graphics & Controls tabs** ตามด้านบน (พักไว้, มีงานเคลียร์ Object ซ้ำ)
2. **ฟอนต์ไทย** ยังไม่รองรับ (กระทบช่องชื่อผู้เล่น)
3. **ไฮไลต์ปุ่มที่เลือก** (Difficulty/Map/แท็บ) ต้องให้ Cursor ทำ
4. **ป้ายชื่อบนหัวตัวละคร** (World Space Canvas) ยังไม่ได้ออกแบบ
5. **หน้าที่ยังไม่มี:** Lobby/รอผู้เล่น, HUD ในเกม, Game Over/Victory
6. **การรองรับความละเอียดจอหลายขนาด** ยังไม่ได้ทดสอบ (ทดสอบแค่ 1920x1080)
7. ชื่อ Object บางตัวอาจไม่ตรงกับตารางทั้งหมด ให้ Cursor ตรวจจาก Scene จริงก่อนผูก (โดยเฉพาะ `NameError`, ข้อความ Player ใน ModePanel, ชื่อปุ่มแมพ)
8. ก่อน Build/Commit ตรวจ: เปิดเฉพาะ `MainPanel`, ปิด Template ของ Dropdown (ถ้ามี) และกด Ctrl+S

---

## 4. ข้อความแนะนำสำหรับส่งให้ Cursor

> ฝั่ง UI ของ Scene `MainMenu` ทำเสร็จแล้ว (ตามเอกสารนี้) ต้องการให้เขียนสคริปต์ C# สำหรับ:
> 1) สลับ Panel ด้วย SetActive 2) ช่องชื่อผู้เล่นใน MainPanel (ตรวจ ≥2 ตัวอักษร, จำค่า, ส่งไป Fusion เป็นชื่อ/ป้ายชื่อ) 3) CreatePanel (ปุ่ม −/+ 1-4, ไฮไลต์ตัวเลือก Difficulty/Map, Private toggle, สร้างห้องผ่าน Fusion) 4) JoinPanel (ดึงรายชื่อห้องจาก Fusion → Instantiate Prefab `RoomItem` ใน `Content`, ห้องเต็ม = ปุ่ม Disabled) 5) LoadingPanel (SetActive, หมุน Spinner, เปลี่ยนข้อความ, timeout/error) 6) OptionPanel แท็บ Audio (Slider ↔ AudioMixer, อัปเดต ValueText) และการสลับแท็บ
> **อย่าแก้โครงสร้าง/สี/Layout ของ UI** ถ้าต้องการ Object เพิ่มให้แจ้ง และตรวจชื่อ Object จาก Scene จริงก่อนอ้างอิง
