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

2. **Sáng toàn bộ bản đồ (Map-wide Fullbright & No Fog):**
   - Loại bỏ hoàn toàn bóng tối và sương mù trên toàn map (không chỉ là soi sáng xung quanh bản thân).
   - Biến toàn bộ map thành ban ngày, nhìn rõ mọi ngóc ngách, quái vật và đồ vật từ xa.

3. **Kho Đồ Đi Chợ (Shop Items Spawner):**
   - Tự do lấy mọi vật phẩm: Súng Laser, Súng Tranq, Drone Hồi Máu, Drone Pin, Lựu đạn nổ, Lựu đạn Stun, Búa tạ Melee, v.v.
   - Nút **`🎒 [+ CẤT VÀO BALO]`**: Đưa thẳng vào ô trống trong balo để bấm phím số dùng ngay lập tức.
   - Nút **`📦 Thả ra đất trước mặt`**: Spawn trực tiếp dưới đất cho bạn hoặc đồng đội nhặt.

4. **Max All Upgrades (1-Click):**
   - Tăng máu tối đa lên 250 HP, hồi đầy máu, nhảy 5 lần liên tiếp trên không, nhân đôi thể lực, mở khóa Đôi Cánh (Tumble Wings), tay hút đồ 15m siêu mạnh.

5. **Client-side 100% trong phòng Multiplayer (Chơi chung bạn bè):**
   - Speed Hack tùy chỉnh độ nhanh (1.0x - 6.0x).
   - Vô hạn Double Jump / Bay nhảy trên không.
   - Vô hạn Thể lực (Infinite Stamina).
   - Chống té ngã / lộn nhào (Anti-Tumble).
   - Bất tử (God Mode).

---

## ⌨️ Bảng phím tắt toàn cầu (Global Hotkeys)

Phím tắt hoạt động cả khi đang ở trong cửa sổ game hoặc ngoài Desktop:

| Phím tắt | Tính năng | Mô tả |
| :--- | :--- | :--- |
| **F2** | **Speed Hack** | Bật / Tắt tăng tốc độ di chuyển (thanh trượt từ 1.0x đến 6.0x) |
| **F3** | **Infinite Double Jump** | Vô hạn nhảy trên không (air jump liên tục như bay) |
| **F4** | **Infinite Stamina** | Vô hạn thể lực, chạy không bao giờ mệt |
| **F5** | **God Mode** | Bất tử, không bị quái cắn mất máu |
| **F6** | **Anti-Tumble** | Chống ngã / trượt chân / lộn nhào khi va chạm mạnh |
| **F7** | **Fullbright & No Fog** | **SÁNG TOÀN BỘ BẢN ĐỒ** & Xóa tan sương mù |
| **F9** | **Instant Heal** | Hồi phục 100% Máu ngay tức khắc |
| **F10** | **MAX ALL UPGRADES** | Max toàn bộ chỉ số nâng cấp (HP, Nhảy, Đôi cánh, Hút đồ) |

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
