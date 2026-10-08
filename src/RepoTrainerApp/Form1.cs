using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RepoTrainerApp
{
    public class Form1 : Form
    {
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private static readonly HttpClient client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        private const string BASE_URL = "http://127.0.0.1:29999";

        // UI Controls
        private Label lblStatus;
        private CheckBox chkAlwaysOnTop;
        private Label lblHealth;

        private CheckBox chkSpeed;
        private TrackBar tbSpeed;
        private Label lblSpeedVal;

        private CheckBox chkJump;
        private CheckBox chkStamina;
        private CheckBox chkGod;
        private CheckBox chkTumble;
        private CheckBox chkFullbright;

        private Button btnHeal;
        private Button btnRevive;
        private Button btnMaxUpgrades;

        private TextBox txtSearchItem;
        private ListBox lbItems;
        private Button btnGiveBalo;
        private Button btnGiveGround;

        private System.Windows.Forms.Timer pollTimer;
        private System.Windows.Forms.Timer hotkeyTimer;

        private readonly List<string> allItemNames = new List<string>
        {
            "ItemGunLaser", "ItemGunTranq", "ItemCartLaser",
            "ItemDroneHeal", "ItemDroneBattery", "ItemDroneFeather", "ItemDroneTorque", "ItemDroneZeroGravity",
            "ItemGrenadeExplosive", "ItemGrenadeStun", "ItemGrenadeShockwave", "ItemGrenadeHuman",
            "ItemMineExplosive", "ItemMineStun",
            "ItemStunBaton", "ItemMeleeInflatableHammer",
            "ItemHealthPack", "ItemReviveItem", "ItemBattery", "ItemLeafBlower", "ItemRubberDuck", "ItemWalkieTalkie"
        };

        public Form1()
        {
            InitializeComponent();
            StartTimers();
        }

        private void InitializeComponent()
        {
            this.Text = "★ R.E.P.O. Master External Trainer v2.0 ★";
            this.Size = new Size(580, 720);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(24, 25, 29);
            this.ForeColor = Color.White;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            var titleFont = new Font("Segoe UI", 11, FontStyle.Bold);
            var headerFont = new Font("Segoe UI", 12, FontStyle.Bold);
            var itemFont = new Font("Segoe UI", 9.5f, FontStyle.Regular);

            // 1. Header & Status
            lblStatus = new Label
            {
                Text = "🔴 Đang chờ kết nối với game R.E.P.O...",
                Location = new Point(15, 12),
                Size = new Size(380, 25),
                Font = titleFont,
                ForeColor = Color.Tomato
            };
            this.Controls.Add(lblStatus);

            chkAlwaysOnTop = new CheckBox
            {
                Text = "📌 Ghim trên cùng",
                Location = new Point(410, 12),
                Size = new Size(140, 25),
                Font = itemFont,
                Checked = true
            };
            this.TopMost = true;
            chkAlwaysOnTop.CheckedChanged += (s, e) => this.TopMost = chkAlwaysOnTop.Checked;
            this.Controls.Add(chkAlwaysOnTop);

            lblHealth = new Label
            {
                Text = "Máu: -- / --",
                Location = new Point(15, 40),
                Size = new Size(250, 20),
                Font = itemFont,
                ForeColor = Color.LightGray
            };
            this.Controls.Add(lblHealth);

            int currentY = 70;

            // 2. Nhóm Di chuyển (Movement)
            var gbMovement = new GroupBox
            {
                Text = " 🏃 DI CHUYỂN (MOVEMENT) ",
                Location = new Point(15, currentY),
                Size = new Size(535, 130),
                ForeColor = Color.SkyBlue,
                Font = titleFont
            };

            chkSpeed = new CheckBox
            {
                Text = "[F2] Speed Hack",
                Location = new Point(15, 28),
                Size = new Size(160, 25),
                Font = itemFont,
                ForeColor = Color.White
            };
            chkSpeed.Click += async (s, e) => await SendCommand("toggle_speed");
            gbMovement.Controls.Add(chkSpeed);

            lblSpeedVal = new Label
            {
                Text = "2.5x",
                Location = new Point(180, 28),
                Size = new Size(45, 25),
                Font = itemFont,
                ForeColor = Color.Yellow
            };
            gbMovement.Controls.Add(lblSpeedVal);

            tbSpeed = new TrackBar
            {
                Location = new Point(230, 25),
                Size = new Size(290, 45),
                Minimum = 10,
                Maximum = 60,
                Value = 25,
                TickFrequency = 5
            };
            tbSpeed.ValueChanged += async (s, e) =>
            {
                float val = tbSpeed.Value / 10f;
                lblSpeedVal.Text = $"{val:F1}x";
                await SendCommand("set_speed", val.ToString("F1"));
            };
            gbMovement.Controls.Add(tbSpeed);

            chkJump = new CheckBox
            {
                Text = "[F3] Vô hạn Double Jump / Bay nhảy",
                Location = new Point(15, 62),
                Size = new Size(250, 25),
                Font = itemFont,
                ForeColor = Color.White
            };
            chkJump.Click += async (s, e) => await SendCommand("toggle_jump");
            gbMovement.Controls.Add(chkJump);

            chkStamina = new CheckBox
            {
                Text = "[F4] Vô hạn Thể lực (Stamina)",
                Location = new Point(280, 62),
                Size = new Size(240, 25),
                Font = itemFont,
                ForeColor = Color.White
            };
            chkStamina.Click += async (s, e) => await SendCommand("toggle_stamina");
            gbMovement.Controls.Add(chkStamina);

            chkTumble = new CheckBox
            {
                Text = "[F6] Chống té ngã (Anti-Tumble)",
                Location = new Point(15, 95),
                Size = new Size(250, 25),
                Font = itemFont,
                ForeColor = Color.White
            };
            chkTumble.Click += async (s, e) => await SendCommand("toggle_tumble");
            gbMovement.Controls.Add(chkTumble);

            this.Controls.Add(gbMovement);
            currentY += 140;

            // 3. Nhóm Sinh tồn & Ánh sáng (Health & Lighting)
            var gbHealth = new GroupBox
            {
                Text = " 🛡️ SINH TỒN & ÁNH SÁNG (SURVIVAL & LIGHT) ",
                Location = new Point(15, currentY),
                Size = new Size(535, 140),
                ForeColor = Color.LightGreen,
                Font = titleFont
            };

            chkGod = new CheckBox
            {
                Text = "[F5] Bất tử (God Mode)",
                Location = new Point(15, 28),
                Size = new Size(200, 25),
                Font = itemFont,
                ForeColor = Color.White
            };
            chkGod.Click += async (s, e) => await SendCommand("toggle_god");
            gbHealth.Controls.Add(chkGod);

            chkFullbright = new CheckBox
            {
                Text = "[F7] SÁNG TOÀN BỘ MAP (Fullbright & No Fog)",
                Location = new Point(220, 28),
                Size = new Size(300, 25),
                Font = itemFont,
                ForeColor = Color.White
            };
            chkFullbright.Click += async (s, e) => await SendCommand("toggle_fullbright");
            gbHealth.Controls.Add(chkFullbright);

            btnHeal = new Button
            {
                Text = "🩹 [F9] Hồi 100% Máu",
                Location = new Point(15, 65),
                Size = new Size(160, 32),
                Font = itemFont,
                BackColor = Color.FromArgb(40, 60, 45),
                FlatStyle = FlatStyle.Flat
            };
            btnHeal.Click += async (s, e) => await SendCommand("heal");
            gbHealth.Controls.Add(btnHeal);

            btnRevive = new Button
            {
                Text = "✨ Hồi sinh tức thì",
                Location = new Point(185, 65),
                Size = new Size(150, 32),
                Font = itemFont,
                BackColor = Color.FromArgb(50, 45, 65),
                FlatStyle = FlatStyle.Flat
            };
            btnRevive.Click += async (s, e) => await SendCommand("revive");
            gbHealth.Controls.Add(btnRevive);

            btnMaxUpgrades = new Button
            {
                Text = "⭐ [F10] MAX ALL UPGRADES",
                Location = new Point(345, 65),
                Size = new Size(175, 32),
                Font = itemFont,
                BackColor = Color.FromArgb(70, 55, 30),
                FlatStyle = FlatStyle.Flat
            };
            btnMaxUpgrades.Click += async (s, e) => await SendCommand("max_upgrades");
            gbHealth.Controls.Add(btnMaxUpgrades);

            var lblPerkNote = new Label
            {
                Text = "• Max Upgrades: +150 HP, Nhảy 5 lần, Đôi Cánh (Tumble Wings), Tay hút đồ 15m.",
                Location = new Point(15, 107),
                Size = new Size(505, 20),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                ForeColor = Color.DarkGray
            };
            gbHealth.Controls.Add(lblPerkNote);

            this.Controls.Add(gbHealth);
            currentY += 150;

            // 4. Nhóm Kho Đồ Đi Chợ (Shop Items Spawner)
            var gbItems = new GroupBox
            {
                Text = " 🎒 KHO ĐỒ ĐI CHỢ (SHOP ITEMS SPAWNER) ",
                Location = new Point(15, currentY),
                Size = new Size(535, 280),
                ForeColor = Color.Khaki,
                Font = titleFont
            };

            var lblSearch = new Label
            {
                Text = "Tìm món đồ:",
                Location = new Point(15, 26),
                Size = new Size(85, 22),
                Font = itemFont,
                ForeColor = Color.White
            };
            gbItems.Controls.Add(lblSearch);

            txtSearchItem = new TextBox
            {
                Location = new Point(105, 25),
                Size = new Size(415, 25),
                Font = itemFont,
                BackColor = Color.FromArgb(35, 36, 42),
                ForeColor = Color.White
            };
            txtSearchItem.TextChanged += (s, e) => FilterItemsList(txtSearchItem.Text);
            gbItems.Controls.Add(txtSearchItem);

            lbItems = new ListBox
            {
                Location = new Point(15, 58),
                Size = new Size(330, 205),
                Font = itemFont,
                BackColor = Color.FromArgb(30, 31, 36),
                ForeColor = Color.White
            };
            gbItems.Controls.Add(lbItems);
            FilterItemsList("");

            btnGiveBalo = new Button
            {
                Text = "🎒 [+ CẤT VÀO BALO]",
                Location = new Point(355, 65),
                Size = new Size(165, 45),
                Font = titleFont,
                BackColor = Color.FromArgb(30, 70, 45),
                FlatStyle = FlatStyle.Flat
            };
            btnGiveBalo.Click += async (s, e) =>
            {
                if (lbItems.SelectedItem != null)
                {
                    string item = lbItems.SelectedItem.ToString();
                    await SendCommand("give_item", item);
                }
                else
                {
                    MessageBox.Show("Vui lòng chọn một món đồ trong danh sách!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };
            gbItems.Controls.Add(btnGiveBalo);

            btnGiveGround = new Button
            {
                Text = "📦 Thả ra đất trước mặt",
                Location = new Point(355, 120),
                Size = new Size(165, 38),
                Font = itemFont,
                BackColor = Color.FromArgb(50, 50, 60),
                FlatStyle = FlatStyle.Flat
            };
            btnGiveGround.Click += async (s, e) =>
            {
                if (lbItems.SelectedItem != null)
                {
                    string item = lbItems.SelectedItem.ToString();
                    await SendCommand("give_item_ground", item);
                }
            };
            gbItems.Controls.Add(btnGiveGround);

            var lblItemsNote = new Label
            {
                Text = "Mẹo:\n- Chọn Laser Gun / Tranq để có súng ngay.\n- Chọn Drone Heal để hồi máu liên tục.\n- Chọn Grenade để nổ quái vật.",
                Location = new Point(355, 175),
                Size = new Size(165, 80),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                ForeColor = Color.LightSlateGray
            };
            gbItems.Controls.Add(lblItemsNote);

            this.Controls.Add(gbItems);
        }

        private void FilterItemsList(string query)
        {
            lbItems.Items.Clear();
            foreach (var item in allItemNames)
            {
                if (string.IsNullOrEmpty(query) || item.ToLower().Contains(query.ToLower()))
                {
                    lbItems.Items.Add(item);
                }
            }
            if (lbItems.Items.Count > 0) lbItems.SelectedIndex = 0;
        }

        private void StartTimers()
        {
            // 1. Timer kiểm tra kết nối với Game
            pollTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            pollTimer.Tick += async (s, e) => await PollGameStatus();
            pollTimer.Start();

            // 2. Timer kiểm tra Global Hotkeys toàn cầu (GetAsyncKeyState)
            hotkeyTimer = new System.Windows.Forms.Timer { Interval = 70 };
            hotkeyTimer.Tick += async (s, e) => await CheckGlobalHotkeys();
            hotkeyTimer.Start();
        }

        private async Task PollGameStatus()
        {
            try
            {
                var res = await client.GetStringAsync($"{BASE_URL}/api/status");
                lblStatus.Text = "🟢 ĐÃ KẾT NỐI VỚI GAME R.E.P.O (SẴN SÀNG CHEAT)";
                lblStatus.ForeColor = Color.LightGreen;

                // Cập nhật trạng thái
                if (res.Contains("\"inGame\":true"))
                {
                    chkSpeed.Checked = res.Contains("\"speed\":true");
                    chkJump.Checked = res.Contains("\"jump\":true");
                    chkStamina.Checked = res.Contains("\"stamina\":true");
                    chkGod.Checked = res.Contains("\"god\":true");
                    chkTumble.Checked = res.Contains("\"tumble\":true");
                    chkFullbright.Checked = res.Contains("\"fullbright\":true");

                    // Parse máu đơn giản
                    int idxHp = res.IndexOf("\"health\":");
                    if (idxHp >= 0)
                    {
                        int comma = res.IndexOf(',', idxHp);
                        string hpPart = res.Substring(idxHp + 9, comma - (idxHp + 9));
                        int idxMax = res.IndexOf("\"maxHealth\":");
                        int commaMax = res.IndexOf(',', idxMax);
                        string maxPart = res.Substring(idxMax + 12, commaMax - (idxMax + 12));
                        lblHealth.Text = $"Máu hiện tại: {hpPart} / {maxPart} HP";
                    }
                }
                else
                {
                    lblHealth.Text = "Trạng thái: Ở sảnh chính (Chưa vào trận)";
                }
            }
            catch
            {
                lblStatus.Text = "🔴 Đang chờ mở game R.E.P.O...";
                lblStatus.ForeColor = Color.Tomato;
                lblHealth.Text = "Máu: -- / --";
            }
        }

        private async Task SendCommand(string cmd, string val = "")
        {
            try
            {
                await client.GetStringAsync($"{BASE_URL}/api/cmd?cmd={cmd}&val={Uri.EscapeDataString(val)}");
                System.Media.SystemSounds.Beep.Play();
            }
            catch { }
        }

        // Bắt phím tắt toàn cầu (kể cả khi đang trong game)
        private bool f2Down, f3Down, f4Down, f5Down, f6Down, f7Down, f9Down, f10Down;

        private async Task CheckGlobalHotkeys()
        {
            // F2 = 0x71
            if ((GetAsyncKeyState(0x71) & 0x8000) != 0) { if (!f2Down) { f2Down = true; await SendCommand("toggle_speed"); } } else f2Down = false;
            // F3 = 0x72
            if ((GetAsyncKeyState(0x72) & 0x8000) != 0) { if (!f3Down) { f3Down = true; await SendCommand("toggle_jump"); } } else f3Down = false;
            // F4 = 0x73
            if ((GetAsyncKeyState(0x73) & 0x8000) != 0) { if (!f4Down) { f4Down = true; await SendCommand("toggle_stamina"); } } else f4Down = false;
            // F5 = 0x74
            if ((GetAsyncKeyState(0x74) & 0x8000) != 0) { if (!f5Down) { f5Down = true; await SendCommand("toggle_god"); } } else f5Down = false;
            // F6 = 0x75
            if ((GetAsyncKeyState(0x75) & 0x8000) != 0) { if (!f6Down) { f6Down = true; await SendCommand("toggle_tumble"); } } else f6Down = false;
            // F7 = 0x76
            if ((GetAsyncKeyState(0x76) & 0x8000) != 0) { if (!f7Down) { f7Down = true; await SendCommand("toggle_fullbright"); } } else f7Down = false;
            // F9 = 0x78
            if ((GetAsyncKeyState(0x78) & 0x8000) != 0) { if (!f9Down) { f9Down = true; await SendCommand("heal"); } } else f9Down = false;
            // F10 = 0x79
            if ((GetAsyncKeyState(0x79) & 0x8000) != 0) { if (!f10Down) { f10Down = true; await SendCommand("max_upgrades"); } } else f10Down = false;
        }
    }
}
