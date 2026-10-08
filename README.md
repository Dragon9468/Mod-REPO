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
   - Cửa sổ phân chia các Tab rõ ràng, có thanh trượt điều chỉnh tốc độ, có thể kéo thả tự do.
3. **Phím tắt nhanh (Hotkeys) trong trận:**
   - Kích hoạt tức thì bằng một phím bấm khi đang chạy trốn quái vật, có thông báo nhỏ (Toast) trên màn hình.
4. **Không giới hạn thời gian:**
   - Chạy vĩnh viễn, không cần tài khoản, không quảng cáo.

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

### Tính năng tức thời (Trong tab Player):
- **Instant Heal:** Hồi phục 100% máu lập tức.
- **Instant Revive:** Tự hồi sinh tại chỗ ngay khi vừa chết.
- **Feather Fall:** Rơi chậm như lông vũ trong 10 giây.

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

### Yêu cầu:
- .NET SDK 8.0+

### Lệnh Build:
```powershell
dotnet build src/RepoModMenu/RepoModMenu.csproj -c Release
```
File DLL đầu ra sẽ nằm tại:
`src/RepoModMenu/bin/Release/netstandard2.1/RepoModMenu.dll`

Sau đó chạy script tự động cài đặt vào game:
```powershell
powershell -ExecutionPolicy Bypass -File install_to_game.ps1
```
