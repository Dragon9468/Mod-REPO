# R.E.P.O. - Portable Standalone Mod & External Trainer (Wand-Style)

Bản mod & ứng dụng Trainer ngoài độc lập dạng rời rạc (Standalone & Portable) dành cho game **R.E.P.O.** (Steam).
Được thiết kế giao diện ứng dụng ngoài Desktop giống hệt **Wand / WeMod**, không giới hạn thời gian chơi, không cần tài khoản, hoàn toàn miễn phí và mã nguồn mở.

---

## 🌟 Điểm nổi bật & Cải tiến v2.0 (New Architecture)

1. **Ứng dụng Trainer ngoài độc lập (`RepoTrainerApp.exe`):**
   - Không bị giới hạn bởi engine Unity New Input System hay lỗi đè phím trong game.
   - Giao diện Dark-Mode hiện đại, hiển thị trạng thái kết nối trực tiếp với game: `🟢 ĐÃ KẾT NỐI VỚI GAME R.E.P.O` và lượng Máu (HP) theo thời gian thực.
   - Tính năng **"📌 Ghim trên cùng"** (Always On Top) giúp bạn vừa chơi game vừa thấy và chỉnh tính năng cheat dễ dàng.
   - **Phím tắt toàn cầu (Global Hotkeys F2 - F10):** Dùng trực tiếp Windows API `GetAsyncKeyState`, bạn có thể bấm phím tắt ngay cả khi đang tập trung chơi game ở chế độ Fullscreen!

2. **Sáng toàn bộ bản đồ & Tầm nhìn siêu xa (Map-wide Fullbright & Infinite Vision):**
   - Mở rộng tầm nhìn của Camera (`FarClipPlane = 1000m`) và xóa sạch sương mù, giúp nhìn thông suốt toàn bộ bản đồ mà không bị cụt hay giới hạn tầm nhìn phía xa.
   - Chiếu sáng rực rỡ mọi góc tối mà không gây chói.

3. **Chống rung lắc Camera (Anti Camera Shake & Head Bobbing):**
   - Loại bỏ 100% rung chấn camera, nhún bước chân khi đi bộ/chạy nước rút và giật nảy khi tiếp đất.
   - Giúp di chuyển mượt mà, dễ chịu, chống chóng mặt và say xe tuyệt đối.

4. **ESP Nhìn Xuyên Tường với Khung Hộp 3D (3D Bounding Box ESP):**
   - **Hộp chữ nhật 3D chuẩn xác (Wireframe 3D Box):**
     - 🟥 **Quái vật / Enemies:** Hộp 3D đỏ tươi bọc sát cơ thể quái vật.
     - 🟨 **Đồ đạc / Valuables:** Hộp 3D vàng kim + thông tin giá trị tiền ($) và độ nặng (kg).
     - 🟦 **Đồng đội / Teammates:** Hộp 3D xanh lam cyan.
     - 🟩 **Trạm Rút Tiền / Extractor (`ExtractionPoint`):** Hộp 3D xanh lá lime + khoảng cách.
   - **Chấm chỉ báo màu tinh gọn (Colored Dot Markers):**
     - 🟪 **Đồ mua shop rơi ngoài đất (`ItemEquippable`):** Chấm tím + tên đồ + khoảng cách.
     - 🟧 **Đầu đồng đội hi sinh (`PlayerDeathHead`):** Chấm cam cứu viện + khoảng cách.
     - 🟨 **Xe đẩy (`PhysGrabCart`):** Chấm vàng đẩy đồ + khoảng cách.

5. **Kho Đồ Đi Chợ (Shop Items Spawner) `[Chỉ Host]`:**
   - Tự do lấy mọi vật phẩm: Súng Laser, Súng Tranq, Drone Hồi Máu, Drone Pin, Lựu đạn nổ, Lựu đạn Stun, Búa tạ Melee, v.v.
   - Nút **`🎒 [+ CẤT VÀO BALO]`**: Đưa thẳng vào ô trống trong balo để bấm phím số dùng ngay lập tức.
   - Nút **`📦 Thả ra đất trước mặt`**: Spawn trực tiếp dưới đất cho bạn hoặc đồng đội nhặt.
   - *Lưu ý: Tính năng sinh đồ (Spawn Item) cần quyền Máy Chủ (Host) để vật thể đồng bộ qua mạng tới người chơi khác.*

6. **Max All Upgrades & Siêu Cánh Tay (1-Click):**
   - Tăng máu tối đa lên 250 HP, hồi đầy máu, nhảy 5 lần liên tiếp trên không, nhân đôi thể lực, mở khóa Đôi Cánh (Tumble Wings).
   - **Tay hút đồ siêu xa & siêu khỏe:** Tầm với lên đến **25m**, lực nâng cực đại **5000f**, lực giữ **1500f**, dễ dàng nhấc và kéo mọi đồ vật siêu nặng từ xa mà không sợ tuột tay.

7. **Client-side 100% trong phòng Multiplayer (Chơi chung bạn bè):**
   - Speed Hack tùy chỉnh độ nhanh (1.0x - 6.0x).
   - Vô hạn Double Jump / Bay nhảy trên không.
   - Vô hạn Thể lực (Infinite Stamina).
   - Chống té ngã / lộn nhào (Anti-Tumble).
   - Bất tử (God Mode).
   - Tắt rung lắc camera.

---

## ⌨️ Bảng phím tắt toàn cầu (Global Hotkeys)

Phím tắt hoạt động cả khi đang ở trong cửa sổ game hoặc ngoài Desktop:

| Phím tắt | Tính năng | Mô tả chi tiết |
| :--- | :--- | :--- |
| **F1** | **Noclip (Bay Xuyên Tường)** | Bay tự do xuyên qua mọi bức tường, sàn nhà (WASD + Space/Ctrl + Shift tăng tốc) |
| **F2** | **Speed Hack** | Bật / Tắt tăng tốc độ di chuyển (thanh trượt từ 1.0x đến 6.0x) |
| **F3** | **Infinite Jump** | Vô hạn nhảy trên không (air jump liên tục) |
| **F4** | **Infinite Stamina** | Vô hạn thể lực, chạy liên tục không mệt |
| **F5** | **Anti-Tumble** | Chống ngã té / lộn nhào / choáng khi va đập mạnh |
| **F6** | **God Mode** | Bất tử tuyệt đối, không nhận sát thương từ quái hay bẫy |
| **F7** | **Fullbright & Far View** | **SÁNG TOÀN BỘ BẢN ĐỒ** & Mở rộng tầm nhìn xa 1000m, Xóa sương mù |
| **F8** | **ESP Khung Hộp 3D** | Quái (Đỏ 3D), Valuables (Vàng 3D), Đồng đội (Cyan 3D), Extractor (Xanh lá 3D), Đồ rơi/Đầu đồng đội/Xe đẩy (Chấm màu) |
| **F9** | **Instant Heal** | Hồi phục 100% Máu ngay tức khắc |
| **F10** | **Instant Revive** | Hồi sinh đồng đội hoặc bản thân lập tức khi gục ngã |
| **F11** | **MAX ALL UPGRADES** | Max toàn bộ chỉ số (250 HP, Nhảy 5 lần, Cánh, Tay hút đồ xa 25m & Lực 5000f) |
| **F12** | **Sạc Đầy Xe Truck `[Chỉ Host]`** | Bơm đầy 100% Pin năng lượng cho Trạm Sạc / Xe Truck (Cần Host) |
| **Z** | **🎯 Aimbot Khóa Mục Tiêu** | Tự động bắt mục tiêu: **Súng** (khóa đầu/yếu điểm), **Cận chiến** (tự quay người trúng 100%), **Lựu đạn/Ném** (tự căn góc cầu vồng bù trọng lực). Giữ **Chuột Phải** hoặc phím **V** để khóa! |

---

## 📁 Cấu trúc thư mục Mod rời rạc (Portable)

Toàn bộ gói mod và ứng dụng trainer nằm trong thư mục:
```text
dist/REPO_Portable_Mod/
├── RepoTrainerApp.exe        <-- Ứng dụng ngoài (External Trainer) giống Wand
├── BepInEx/
│   ├── core/
│   ├── plugins/
│   │   └── RepoModMenu/
│   │       └── RepoModMenu.dll  <-- Plugin kết nối IPC cục bộ v2.0
│   └── config/
├── doorstop_config.ini
└── winhttp.dll
```

---

## 🚀 Hướng dẫn sử dụng (Rất đơn giản)

### Lần đầu cài đặt hoặc mang sang máy mới:
1. Copy toàn bộ các file trong `dist/REPO_Portable_Mod/` vào thư mục cài game R.E.P.O (nơi có file `REPO.exe`).
   *(Hoặc nếu ở trên máy hiện tại, chỉ cần chạy file `install_to_game.ps1`)*.
2. Mở game **R.E.P.O.**.
3. Chạy file **`RepoTrainerApp.exe`** (có thể ghim ra Desktop hoặc mở trực tiếp).
   - Đèn trạng thái trên ứng dụng sẽ chuyển sang: **`🟢 ĐÃ KẾT NỐI VỚI GAME R.E.P.O`**.
4. Vào trận và bấm trực tiếp các nút trên ứng dụng Trainer hoặc bấm phím tắt **F2, F3, F4, F5, F6, F7, F9, F10** bất cứ lúc nào!

---

## 🛠️ Dành cho Developer (Tự build lại)

### 1. Build Mod DLL (BepInEx Plugin):
```powershell
& "C:\Program Files\dotnet\dotnet.exe" build src/RepoModMenu/RepoModMenu.csproj -c Release
```

### 2. Build Trainer App (External Executable):
```powershell
& "C:\Program Files\dotnet\dotnet.exe" publish src/RepoTrainerApp/RepoTrainerApp.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o dist/REPO_Portable_Mod
```
