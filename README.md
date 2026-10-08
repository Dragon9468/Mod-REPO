# R.E.P.O. - Portable Standalone Mod Menu & Trainer

Bản mod menu dạng rời rạc (Standalone & Portable) dành cho game **R.E.P.O.** (Steam).
Được thiết kế độc lập, không phụ thuộc vào phần mềm bên thứ ba như WeMod, không bị giới hạn thời gian chơi.

---

## 🌟 Điểm nổi bật (Features)

1. **Kiến trúc Mod rời rạc (Portable):**
   - Không can thiệp, không sửa đổi hay ghi đè file gốc của game.
   - Khi chuyển sang máy mới: Chỉ cần tải/copy thư mục `dist/REPO_Portable_Mod` và dán vào thư mục cài đặt game là chơi được ngay.
2. **Giao diện Menu trực quan (In-game IMGUI):**
   - Bấm **`Insert`** hoặc **`F1`** để ẩn/hiện menu ngay trong game.
   - Cửa sổ phân chia các Tab rõ ràng, có thanh trượt điều chỉnh, có thể kéo thả tự do.
3. **Phím tắt nhanh (Hotkeys) trong trận:**
   - Kích hoạt tức thì bằng một phím bấm khi đang chạy trốn quái vật, có thông báo nhỏ (Toast) trên màn hình.
4. **Hệ thống X-Ray ESP (Nhìn xuyên tường):**
   - Định vị quái vật (màu Đỏ), vật phẩm & tiền quý (màu Vàng) và đồng đội (màu Xanh) kèm khoảng cách chính xác theo thời gian thực.
5. **Hệ thống Nâng cấp nhân vật (Perks & Grabber):**
   - 1-Click Max tất cả nâng cấp: +150 HP, +5 Extra Jumps, x2 Thể lực, Tầm với tay cầm đồ siêu xa (15m), nhấc đồ nặng như lông hồng, ném đồ cực mạnh, mở khóa Đôi Cánh (Tumble Wings) rơi không ngã.
6. **Kho Vật Phẩm Đi Chợ (Shop Items Spawner vào Balo):**
   - Tự do lấy bất kỳ món đồ nào trong Shop (Súng Laser, Súng Tranq, Drone Hồi máu, Drone Pin, Lựu đạn nổ, Lựu đạn Stun, Búa tạ Melee, Mìn nổ, Bình máu, v.v.).
   - Nút **`[+ Balo]`**: Tự động đưa thẳng vào ô trống trong balo của bạn để bấm phím số dùng ngay!
   - Nút **`[Thả đất]`**: Spawn ra đất ngay trước mặt để nhặt hoặc cho đồng đội nhặt.

---

## ⌨️ Danh sách phím tắt (Hotkeys)

| Phím tắt | Tính năng | Mô tả |
| :--- | :--- | :--- |
| **Insert** hoặc **F1** | **Ẩn / Hiện Menu** | Bật tắt cửa sổ Mod Menu chính |
| **F2** | **Speed Hack** | Tăng tốc độ chạy (điều chỉnh từ 1.0x đến 6.0x) |
| **F3** | **Infinite Double Jump** | Vô hạn nhảy trên không (air jump liên tục như bay) |
| **F4** | **Infinite Stamina** | Vô hạn thể lực / năng lượng, chạy không bao giờ mệt |
| **F5** | **God Mode** | Bất tử, không bị trừ máu khi bị quái cắn |
| **F6** | **Anti-Tumble / No Fall** | Chống ngã / trượt chân / lộn nhào khi va chạm mạnh |
| **F7** | **Fullbright / Nightvision** | Tạo nguồn sáng cá nhân tỏa rộng xung quanh |
| **F8** | **X-Ray ESP** | Bật / Tắt nhìn xuyên tường thấy Quái, Đồ và Bạn bè |

---

## 🛡️ Hoạt động khi là Client vào phòng người khác (Multiplayer)

* **Hoạt động 100%:**
  - Speed Hack (Chạy siêu nhanh)
  - Infinite Double Jump (Bay nhảy trên không)
  - Infinite Stamina (Thể lực vô tận)
  - X-Ray ESP (Nhìn xuyên tường quái vật, vật phẩm và đồng đội)
  - Fullbright (Đèn sáng cá nhân)
  - Chống ngã (Anti-Tumble)
  - Siêu tay cầm đồ (Tầm với xa, lực nhấc khỏe, ném xa)
  - Đôi Cánh (Tumble Wings)
  - Add đồ đi chợ vào Balo (Súng, Drone, Nade, Melee dùng bình thường)

---

## 📁 Cấu trúc thư mục Mod rời rạc

Toàn bộ gói mod chạy độc lập nằm trong thư mục:
```text
dist/REPO_Portable_Mod/
├── BepInEx/
│   ├── core/                      <-- Framework BepInEx 5.4.21
│   ├── plugins/
│   │   └── RepoModMenu/
│   │       └── RepoModMenu.dll    <-- File Mod chính do chúng ta tự viết
│   └── config/
├── doorstop_config.ini
└── winhttp.dll
```

### Cách mang sang máy mới:
1. Tải thư mục `dist/REPO_Portable_Mod`.
2. Copy toàn bộ các file bên trong (`BepInEx`, `winhttp.dll`, `doorstop_config.ini`) dán vào thư mục cài đặt game R.E.P.O trên máy mới (nơi chứa file `REPO.exe`).
3. Khởi động game và trải nghiệm!

---

## 🛠️ Cách tự chỉnh sửa & Build lại (Dành cho Dev)

### Lệnh Build:
```powershell
dotnet build src/RepoModMenu/RepoModMenu.csproj -c Release
```
Sau đó chạy script tự động cài đặt vào game:
```powershell
powershell -ExecutionPolicy Bypass -File install_to_game.ps1
```
