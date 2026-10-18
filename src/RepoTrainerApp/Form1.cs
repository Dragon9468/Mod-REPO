using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RepoTrainerApp
{
    public class Form1 : Form
    {
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        // UI Controls
        private Label lblStatus;
        private CheckBox chkAlwaysOnTop;
        private Label lblHealth;

        private CheckBox chkNoclip;
        private CheckBox chkSpeed;
        private TrackBar tbSpeed;
        private Label lblSpeedVal;

        private CheckBox chkJump;
        private CheckBox chkStamina;
        private CheckBox chkTumble;
        private CheckBox chkGod;
        private CheckBox chkFullbright;
        private CheckBox chkEsp;
        private CheckBox chkAimbot;

        private Button btnHeal;
        private Button btnRevive;
        private Button btnMaxUpgrades;
        private Button btnChargeTruck;
        private CheckBox chkTruckEnergy;

        private TextBox txtSearchItem;
        private ComboBox cboCategory;
        private ListBox lbItems;
        private Button btnGiveBalo;
        private Button btnGiveGround;

        private System.Windows.Forms.Timer pollTimer;
        private System.Windows.Forms.Timer hotkeyTimer;

        public class ItemEntry
        {
            public string Category { get; set; } = "";
            public string Tag { get; set; } = "";
            public string Name { get; set; } = "";
            public string CleanKey => Name.Replace(" ", "").Replace("-", "").Replace("_", "").ToLowerInvariant();
            public override string ToString() => $"{Tag} {Name}";
        }

        public static int GetCategoryOrder(string cat)
        {
            switch (cat)
            {
                case "Súng": return 1;
                case "Cận chiến": return 2;
                case "Cháy nổ": return 3;
                case "Drone": return 4;
                case "Orb": return 5;
                case "Xe": return 6;
                case "Nâng cấp": return 7;
                case "Năng lượng": return 8;
                case "Y tế": return 9;
                case "Công cụ": return 10;
                default: return 99;
            }
        }

        private readonly List<ItemEntry> itemsCatalog = new List<ItemEntry>
        {
            // 🔫 1. Súng & Vũ khí (Guns)
            new ItemEntry { Category = "Súng", Tag = "[Súng]", Name = "Gun Handgun" },
            new ItemEntry { Category = "Súng", Tag = "[Súng]", Name = "Gun Laser" },
            new ItemEntry { Category = "Súng", Tag = "[Súng]", Name = "Gun Shockwave" },
            new ItemEntry { Category = "Súng", Tag = "[Súng]", Name = "Gun Shotgun" },
            new ItemEntry { Category = "Súng", Tag = "[Súng]", Name = "Gun Stun" },
            new ItemEntry { Category = "Súng", Tag = "[Súng]", Name = "Gun Tranq" },
            new ItemEntry { Category = "Súng", Tag = "[Súng]", Name = "Laser Cannon" },

            // ⚔️ 2. Cận chiến & Vũ khí đập (Melee)
            new ItemEntry { Category = "Cận chiến", Tag = "[Cận chiến]", Name = "Melee Baseball Bat" },
            new ItemEntry { Category = "Cận chiến", Tag = "[Cận chiến]", Name = "Melee Frying Pan" },
            new ItemEntry { Category = "Cận chiến", Tag = "[Cận chiến]", Name = "Melee Inflatable Hammer" },
            new ItemEntry { Category = "Cận chiến", Tag = "[Cận chiến]", Name = "Melee Sledgehammer" },
            new ItemEntry { Category = "Cận chiến", Tag = "[Cận chiến]", Name = "Melee Stun Baton" },
            new ItemEntry { Category = "Cận chiến", Tag = "[Cận chiến]", Name = "Melee Sword" },

            // 💣 3. Cháy nổ & Bom mìn (Explosives & Mines)
            new ItemEntry { Category = "Cháy nổ", Tag = "[Cháy nổ]", Name = "Grenade Duct Taped" },
            new ItemEntry { Category = "Cháy nổ", Tag = "[Cháy nổ]", Name = "Grenade Explosive" },
            new ItemEntry { Category = "Cháy nổ", Tag = "[Cháy nổ]", Name = "Grenade Human" },
            new ItemEntry { Category = "Cháy nổ", Tag = "[Cháy nổ]", Name = "Grenade Shockwave" },
            new ItemEntry { Category = "Cháy nổ", Tag = "[Cháy nổ]", Name = "Grenade Stun" },
            new ItemEntry { Category = "Cháy nổ", Tag = "[Cháy nổ]", Name = "Mine Explosive" },
            new ItemEntry { Category = "Cháy nổ", Tag = "[Cháy nổ]", Name = "Mine Shockwave" },
            new ItemEntry { Category = "Cháy nổ", Tag = "[Cháy nổ]", Name = "Mine Stun" },

            // 🤖 4. Drones Hỗ Trợ (Drones)
            new ItemEntry { Category = "Drone", Tag = "[Drone]", Name = "Drone Battery" },
            new ItemEntry { Category = "Drone", Tag = "[Drone]", Name = "Drone Feather" },
            new ItemEntry { Category = "Drone", Tag = "[Drone]", Name = "Drone Heal" },
            new ItemEntry { Category = "Drone", Tag = "[Drone]", Name = "Drone Indestructible" },
            new ItemEntry { Category = "Drone", Tag = "[Drone]", Name = "Drone Torque" },
            new ItemEntry { Category = "Drone", Tag = "[Drone]", Name = "Drone Zero Gravity" },

            // 🔮 5. Orbs Năng Lượng (Orbs)
            new ItemEntry { Category = "Orb", Tag = "[Orb]", Name = "Orb Battery" },
            new ItemEntry { Category = "Orb", Tag = "[Orb]", Name = "Orb Feather" },
            new ItemEntry { Category = "Orb", Tag = "[Orb]", Name = "Orb Heal" },
            new ItemEntry { Category = "Orb", Tag = "[Orb]", Name = "Orb Indestructible" },
            new ItemEntry { Category = "Orb", Tag = "[Orb]", Name = "Orb Magnet" },
            new ItemEntry { Category = "Orb", Tag = "[Orb]", Name = "Orb Torque" },
            new ItemEntry { Category = "Orb", Tag = "[Orb]", Name = "Orb Zero Gravity" },

            // 🛒 6. Xe Đẩy & Phương Tiện (Carts & Vehicles)
            new ItemEntry { Category = "Xe", Tag = "[Xe]", Name = "Cart Cannon" },
            new ItemEntry { Category = "Xe", Tag = "[Xe]", Name = "Cart Laser" },
            new ItemEntry { Category = "Xe", Tag = "[Xe]", Name = "Cart Medium" },
            new ItemEntry { Category = "Xe", Tag = "[Xe]", Name = "Cart Small" },
            new ItemEntry { Category = "Xe", Tag = "[Xe]", Name = "Vehicle Scooter" },
            new ItemEntry { Category = "Xe", Tag = "[Xe]", Name = "Vehicle Semiscooter" },
            new ItemEntry { Category = "Xe", Tag = "[Xe]", Name = "Vehicle Semiscooter Small" },

            // ⭐ 7. Hộp Nâng Cấp Nhân Vật (Player Upgrades)
            new ItemEntry { Category = "Nâng cấp", Tag = "[Nâng cấp]", Name = "Upgrade Player Crouch Rest" },
            new ItemEntry { Category = "Nâng cấp", Tag = "[Nâng cấp]", Name = "Upgrade Player Energy" },
            new ItemEntry { Category = "Nâng cấp", Tag = "[Nâng cấp]", Name = "Upgrade Player Extra Jump" },
            new ItemEntry { Category = "Nâng cấp", Tag = "[Nâng cấp]", Name = "Upgrade Player Grab Range" },
            new ItemEntry { Category = "Nâng cấp", Tag = "[Nâng cấp]", Name = "Upgrade Player Grab Strength" },
            new ItemEntry { Category = "Nâng cấp", Tag = "[Nâng cấp]", Name = "Upgrade Player Grab Throw" },
            new ItemEntry { Category = "Nâng cấp", Tag = "[Nâng cấp]", Name = "Upgrade Player Health" },
            new ItemEntry { Category = "Nâng cấp", Tag = "[Nâng cấp]", Name = "Upgrade Player Sprint Speed" },
            new ItemEntry { Category = "Nâng cấp", Tag = "[Nâng cấp]", Name = "Upgrade Player Tumble Climb" },
            new ItemEntry { Category = "Nâng cấp", Tag = "[Nâng cấp]", Name = "Upgrade Player Tumble Launch" },
            new ItemEntry { Category = "Nâng cấp", Tag = "[Nâng cấp]", Name = "Upgrade Player Tumble Wings" },

            // 🔋 8. Pin & Năng Lượng (Energy)
            new ItemEntry { Category = "Năng lượng", Tag = "[Năng lượng]", Name = "Battery" },
            new ItemEntry { Category = "Năng lượng", Tag = "[Năng lượng]", Name = "Power Crystal" },

            // 🩹 9. Y tế & Hồi Sinh (Medical)
            new ItemEntry { Category = "Y tế", Tag = "[Y tế]", Name = "Defibro" },
            new ItemEntry { Category = "Y tế", Tag = "[Y tế]", Name = "Health Pack Large" },
            new ItemEntry { Category = "Y tế", Tag = "[Y tế]", Name = "Health Pack Medium" },
            new ItemEntry { Category = "Y tế", Tag = "[Y tế]", Name = "Health Pack Small" },
            new ItemEntry { Category = "Y tế", Tag = "[Y tế]", Name = "Revive Item" },

            // 🛠️ 10. Công Cụ & Tiện Ích (Tools)
            new ItemEntry { Category = "Công cụ", Tag = "[Công cụ]", Name = "Duck Bucket" },
            new ItemEntry { Category = "Công cụ", Tag = "[Công cụ]", Name = "Extendable Ladder" },
            new ItemEntry { Category = "Công cụ", Tag = "[Công cụ]", Name = "Extraction Tracker" },
            new ItemEntry { Category = "Công cụ", Tag = "[Công cụ]", Name = "Hidey Box" },
            new ItemEntry { Category = "Công cụ", Tag = "[Công cụ]", Name = "Leaf Blower" },
            new ItemEntry { Category = "Công cụ", Tag = "[Công cụ]", Name = "Magic Staff" },
            new ItemEntry { Category = "Công cụ", Tag = "[Công cụ]", Name = "Phase Bridge" },
            new ItemEntry { Category = "Công cụ", Tag = "[Công cụ]", Name = "Roll Staff" },
            new ItemEntry { Category = "Công cụ", Tag = "[Công cụ]", Name = "Rubber Duck" },
            new ItemEntry { Category = "Công cụ", Tag = "[Công cụ]", Name = "Torque Staff" },
            new ItemEntry { Category = "Công cụ", Tag = "[Công cụ]", Name = "Valuable Tracker" },
            new ItemEntry { Category = "Công cụ", Tag = "[Công cụ]", Name = "Void Staff" },
            new ItemEntry { Category = "Công cụ", Tag = "[Công cụ]", Name = "Walkie Talkie" },
            new ItemEntry { Category = "Công cụ", Tag = "[Công cụ]", Name = "Zero Gravity Staff" }
        };

        public Form1()
        {
            InitializeComponent();
            StartTimers();
        }

        private void InitializeComponent()
        {
            this.Text = "★ R.E.P.O. Master External Trainer v2.1 ★";
            this.Size = new Size(590, 785);
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
                Size = new Size(545, 130),
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
                Size = new Size(300, 45),
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

            chkNoclip = new CheckBox
            {
                Text = "[F1] Bay Xuyên Tường (Noclip)",
                Location = new Point(15, 62),
                Size = new Size(245, 25),
                Font = itemFont,
                ForeColor = Color.Cyan
            };
            chkNoclip.Click += async (s, e) => await SendCommand("toggle_noclip");
            gbMovement.Controls.Add(chkNoclip);

            chkJump = new CheckBox
            {
                Text = "[F3] Vô hạn Double Jump / Bay nhảy",
                Location = new Point(275, 62),
                Size = new Size(255, 25),
                Font = itemFont,
                ForeColor = Color.White
            };
            chkJump.Click += async (s, e) => await SendCommand("toggle_jump");
            gbMovement.Controls.Add(chkJump);

            chkStamina = new CheckBox
            {
                Text = "[F4] Vô hạn Thể lực (Stamina)",
                Location = new Point(15, 95),
                Size = new Size(245, 25),
                Font = itemFont,
                ForeColor = Color.White
            };
            chkStamina.Click += async (s, e) => await SendCommand("toggle_stamina");
            gbMovement.Controls.Add(chkStamina);

            chkTumble = new CheckBox
            {
                Text = "[F5] Chống té ngã (Anti-Tumble)",
                Location = new Point(275, 95),
                Size = new Size(255, 25),
                Font = itemFont,
                ForeColor = Color.White
            };
            chkTumble.Click += async (s, e) => await SendCommand("toggle_tumble");
            gbMovement.Controls.Add(chkTumble);

            this.Controls.Add(gbMovement);
            currentY += 140;

            // 3. Nhóm Sinh tồn, Ánh sáng & Xe Truck (Survival, Light & Truck)
            var gbHealth = new GroupBox
            {
                Text = " 🛡️ SINH TỒN, ÁNH SÁNG & XE TRUCK (SURVIVAL & TRUCK) ",
                Location = new Point(15, currentY),
                Size = new Size(545, 225),
                ForeColor = Color.LightGreen,
                Font = titleFont
            };

            chkGod = new CheckBox
            {
                Text = "[F6] Bất tử",
                Location = new Point(15, 26),
                Size = new Size(110, 25),
                Font = itemFont,
                ForeColor = Color.White
            };
            chkGod.Click += async (s, e) => await SendCommand("toggle_god");
            gbHealth.Controls.Add(chkGod);

            chkFullbright = new CheckBox
            {
                Text = "[F7] Sáng map (Chống rung)",
                Location = new Point(130, 26),
                Size = new Size(205, 25),
                Font = itemFont,
                ForeColor = Color.White
            };
            chkFullbright.Click += async (s, e) => await SendCommand("toggle_fullbright");
            gbHealth.Controls.Add(chkFullbright);

            chkEsp = new CheckBox
            {
                Text = "[F8] ESP Xuyên Tường",
                Location = new Point(340, 26),
                Size = new Size(195, 25),
                Font = itemFont,
                ForeColor = Color.Gold
            };
            chkEsp.Click += async (s, e) => await SendCommand("toggle_esp");
            gbHealth.Controls.Add(chkEsp);

            btnHeal = new Button
            {
                Text = "🩹 [F9] Hồi Máu",
                Location = new Point(15, 58),
                Size = new Size(120, 32),
                Font = itemFont,
                BackColor = Color.FromArgb(40, 60, 45),
                FlatStyle = FlatStyle.Flat
            };
            btnHeal.Click += async (s, e) => await SendCommand("heal");
            gbHealth.Controls.Add(btnHeal);

            btnRevive = new Button
            {
                Text = "✨ [F10] Hồi Sinh",
                Location = new Point(140, 58),
                Size = new Size(120, 32),
                Font = itemFont,
                BackColor = Color.FromArgb(50, 45, 65),
                FlatStyle = FlatStyle.Flat
            };
            btnRevive.Click += async (s, e) => await SendCommand("revive");
            gbHealth.Controls.Add(btnRevive);

            btnMaxUpgrades = new Button
            {
                Text = "⭐ [F11] Max Perks",
                Location = new Point(265, 58),
                Size = new Size(130, 32),
                Font = itemFont,
                BackColor = Color.FromArgb(70, 55, 30),
                FlatStyle = FlatStyle.Flat
            };
            btnMaxUpgrades.Click += async (s, e) => await SendCommand("max_upgrades");
            gbHealth.Controls.Add(btnMaxUpgrades);

            btnChargeTruck = new Button
            {
                Text = "⚡ [F12] Sạc Xe [Chỉ Host]",
                Location = new Point(395, 58),
                Size = new Size(135, 32),
                Font = itemFont,
                BackColor = Color.FromArgb(25, 65, 75),
                FlatStyle = FlatStyle.Flat
            };
            btnChargeTruck.Click += async (s, e) => await SendCommand("charge_truck");
            gbHealth.Controls.Add(btnChargeTruck);

            chkTruckEnergy = new CheckBox
            {
                Text = "🔋 Vô hạn Pin Xe Truck [Chỉ Host] (Tự động giữ 100% pin cho Trạm Sạc / Xe)",
                Location = new Point(15, 96),
                Size = new Size(515, 25),
                Font = itemFont,
                ForeColor = Color.LightCyan
            };
            chkTruckEnergy.Click += async (s, e) => await SendCommand("toggle_truck_energy");
            gbHealth.Controls.Add(chkTruckEnergy);

            chkAimbot = new CheckBox
            {
                Text = "🎯 [Z] Aimbot Khóa Mục Tiêu (Chuột phải / V: Súng + Cận chiến + Ném Bom)",
                Location = new Point(15, 124),
                Size = new Size(515, 25),
                Font = itemFont,
                ForeColor = Color.OrangeRed
            };
            chkAimbot.Click += async (s, e) => await SendCommand("toggle_aimbot");
            gbHealth.Controls.Add(chkAimbot);

            var lblPerkNote = new Label
            {
                Text = "• [Z] Aimbot: Giữ Chuột Phải / V tự khóa quái (Súng, Cận chiến tự xoay trúng 100%, Bom tự căn góc ném).\n• [F1] Noclip | [F8] ESP 3D Box Xuyên Tường | [F11] Max Perks (Tầm nhặt 25m, siêu khỏe).\n• [Chỉ Host]: Sạc Xe [F12], Vô hạn pin xe, và Kho sinh vật phẩm.",
                Location = new Point(15, 154),
                Size = new Size(515, 58),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                ForeColor = Color.LightSteelBlue
            };
            gbHealth.Controls.Add(lblPerkNote);

            this.Controls.Add(gbHealth);
            currentY += 235;

            // 4. Nhóm Kho Đồ Đi Chợ (Shop Items Spawner)
            var gbItems = new GroupBox
            {
                Text = " 🎒 KHO ĐỒ ĐI CHỢ (SHOP ITEMS SPAWNER) [Chỉ Host] ",
                Location = new Point(15, currentY),
                Size = new Size(545, 290),
                ForeColor = Color.Khaki,
                Font = titleFont
            };

            var lblSearch = new Label
            {
                Text = "Tìm:",
                Location = new Point(15, 25),
                Size = new Size(38, 22),
                Font = itemFont,
                ForeColor = Color.White
            };
            gbItems.Controls.Add(lblSearch);

            txtSearchItem = new TextBox
            {
                Location = new Point(55, 23),
                Size = new Size(170, 25),
                Font = itemFont,
                BackColor = Color.FromArgb(35, 36, 42),
                ForeColor = Color.White
            };
            txtSearchItem.TextChanged += (s, e) => FilterItemsList();
            gbItems.Controls.Add(txtSearchItem);

            var lblCat = new Label
            {
                Text = "Nhóm:",
                Location = new Point(235, 25),
                Size = new Size(50, 22),
                Font = itemFont,
                ForeColor = Color.White
            };
            gbItems.Controls.Add(lblCat);

            cboCategory = new ComboBox
            {
                Location = new Point(288, 23),
                Size = new Size(242, 25),
                Font = itemFont,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(35, 36, 42),
                ForeColor = Color.White
            };
            cboCategory.Items.AddRange(new object[]
            {
                "Tất cả danh mục",
                "Súng",
                "Cận chiến",
                "Cháy nổ",
                "Drone",
                "Orb",
                "Xe",
                "Nâng cấp",
                "Năng lượng",
                "Y tế",
                "Công cụ"
            });
            cboCategory.SelectedIndex = 0;
            cboCategory.SelectedIndexChanged += (s, e) => FilterItemsList();
            gbItems.Controls.Add(cboCategory);

            lbItems = new ListBox
            {
                Location = new Point(15, 56),
                Size = new Size(325, 220),
                Font = itemFont,
                BackColor = Color.FromArgb(30, 31, 36),
                ForeColor = Color.White
            };
            gbItems.Controls.Add(lbItems);
            FilterItemsList();

            btnGiveBalo = new Button
            {
                Text = "🎒 [+ CẤT VÀO BALO]",
                Location = new Point(350, 58),
                Size = new Size(180, 45),
                Font = titleFont,
                BackColor = Color.FromArgb(30, 70, 45),
                FlatStyle = FlatStyle.Flat
            };
            btnGiveBalo.Click += async (s, e) =>
            {
                if (lbItems.SelectedItem is ItemEntry entry)
                {
                    await SendCommand("give_item", entry.Name);
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
                Location = new Point(350, 112),
                Size = new Size(180, 38),
                Font = itemFont,
                BackColor = Color.FromArgb(50, 50, 60),
                FlatStyle = FlatStyle.Flat
            };
            btnGiveGround.Click += async (s, e) =>
            {
                if (lbItems.SelectedItem is ItemEntry entry)
                {
                    await SendCommand("give_item_ground", entry.Name);
                }
            };
            gbItems.Controls.Add(btnGiveGround);

            var lblItemsNote = new Label
            {
                Text = "⚠️ LƯU Ý HOST:\n• Sinh đồ & Sạc xe chỉ hoạt động khi bạn là HOST (Chủ phòng).\n• Khi là Khách, cheat Player, Noclip, ESP 3D, Aimbot vẫn hoạt động bình thường.",
                Location = new Point(350, 158),
                Size = new Size(180, 118),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                ForeColor = Color.LightSalmon
            };
            gbItems.Controls.Add(lblItemsNote);

            this.Controls.Add(gbItems);
        }

        private void FilterItemsList()
        {
            string query = txtSearchItem?.Text?.Trim()?.ToLowerInvariant() ?? "";
            string selectedCat = cboCategory?.SelectedItem?.ToString() ?? "Tất cả danh mục";

            var matched = new List<ItemEntry>();

            foreach (var item in itemsCatalog)
            {
                if (selectedCat != "Tất cả danh mục" && !item.Category.Equals(selectedCat, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(query))
                {
                    if (!item.Name.ToLowerInvariant().Contains(query) && !item.Tag.ToLowerInvariant().Contains(query))
                    {
                        continue;
                    }
                }

                matched.Add(item);
            }

            // Sắp xếp phân loại cực chuẩn: Nhóm ưu tiên -> Tên A-Z
            matched.Sort((a, b) =>
            {
                int catOrderA = GetCategoryOrder(a.Category);
                int catOrderB = GetCategoryOrder(b.Category);
                if (catOrderA != catOrderB) return catOrderA.CompareTo(catOrderB);
                return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
            });

            lbItems.BeginUpdate();
            lbItems.Items.Clear();
            foreach (var item in matched)
            {
                lbItems.Items.Add(item);
            }
            if (lbItems.Items.Count > 0) lbItems.SelectedIndex = 0;
            lbItems.EndUpdate();
        }

        private void StartTimers()
        {
            // 1. Timer kiểm tra kết nối với Game
            pollTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            pollTimer.Tick += async (s, e) => await PollGameStatus();
            pollTimer.Start();

            // 2. Timer kiểm tra Global Hotkeys toàn cầu (GetAsyncKeyState)
            hotkeyTimer = new System.Windows.Forms.Timer { Interval = 30 };
            hotkeyTimer.Tick += async (s, e) => await CheckGlobalHotkeys();
            hotkeyTimer.Start();
        }

        private async Task<string> SendTcpRequestAsync(string command, int timeoutMs = 1500)
        {
            using (var tcp = new TcpClient())
            {
                using (var cts = new System.Threading.CancellationTokenSource(timeoutMs))
                {
                    await tcp.ConnectAsync(IPAddress.Loopback, 29999, cts.Token);
                    using (var stream = tcp.GetStream())
                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                    using (var writer = new StreamWriter(stream, Encoding.UTF8))
                    {
                        await writer.WriteLineAsync(command.AsMemory(), cts.Token);
                        await writer.FlushAsync(cts.Token);
                        string? line = await reader.ReadLineAsync(cts.Token);
                        if (line == null) throw new IOException("Server closed connection");
                        return line;
                    }
                }
            }
        }

        private int itemFetchCounter = 0;

        private async Task PollGameStatus()
        {
            try
            {
                var res = await SendTcpRequestAsync("STATUS", 1200);
                if (string.IsNullOrEmpty(res)) throw new Exception("Empty response");

                lblStatus.Text = "🟢 ĐÃ KẾT NỐI VỚI GAME R.E.P.O (SẴN SÀNG CHEAT)";
                lblStatus.ForeColor = Color.LightGreen;

                // Cập nhật trạng thái
                if (res.Contains("\"inGame\":true"))
                {
                    chkNoclip.Checked = res.Contains("\"noclip\":true");
                    chkSpeed.Checked = res.Contains("\"speed\":true");
                    chkJump.Checked = res.Contains("\"jump\":true");
                    chkStamina.Checked = res.Contains("\"stamina\":true");
                    chkGod.Checked = res.Contains("\"god\":true");
                    chkTumble.Checked = res.Contains("\"tumble\":true");
                    chkFullbright.Checked = res.Contains("\"fullbright\":true");
                    chkEsp.Checked = res.Contains("\"esp\":true");
                    chkAimbot.Checked = res.Contains("\"aimbot\":true");
                    chkTruckEnergy.Checked = res.Contains("\"truckenergy\":true");

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

                    // Tự động quét thêm đồ từ game nếu có
                    if (++itemFetchCounter >= 5)
                    {
                        itemFetchCounter = 0;
                        await FetchItemsFromGame();
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

        private async Task FetchItemsFromGame()
        {
            try
            {
                var json = await SendTcpRequestAsync("ITEMS", 1200);
                if (!string.IsNullOrEmpty(json) && json.Length > 2)
                {
                    string inner = json.Trim('[', ']');
                    var items = inner.Split(',');
                    bool changed = false;
                    foreach (var raw in items)
                    {
                        string clean = raw.Trim('"', ' ', '\r', '\n');
                        if (string.IsNullOrEmpty(clean)) continue;

                        if (AddDynamicItem(clean))
                        {
                            changed = true;
                        }
                    }
                    if (changed) FilterItemsList();
                }
            }
            catch { }
        }

        private bool AddDynamicItem(string rawName)
        {
            string name = rawName.Trim();
            if (name.StartsWith("Item ", StringComparison.OrdinalIgnoreCase))
                name = name.Substring(5).Trim();

            // Lọc bỏ triệt để các asset nội bộ / volumes / bộ phận mesh
            string nLower = name.ToLowerInvariant();
            if (nLower.StartsWith("volume") || nLower.Contains("volume") ||
                nLower.StartsWith("shelf") || nLower.Contains("shelf") ||
                nLower.StartsWith("valuable box") || nLower.Contains("valuable box") ||
                nLower.StartsWith("saw ") || nLower.Contains("saw body") || nLower.Contains("saw cover") || nLower.Contains("saw handle") || nLower.Contains("saw blade") ||
                nLower.StartsWith("ladder ") || nLower.Contains("ladder beam") || nLower.Contains("ladder end") || nLower.Contains("ladder handle") || nLower.Contains("ladder screen") ||
                nLower.Contains("orb_") || nLower.Contains("orb mesh") || nLower.Contains("orb core") || nLower.Contains("orb icon") || nLower.Contains("orb piece") ||
                nLower.Contains("cannon moving") || nLower.Contains("cannon barrel") || nLower.Contains("laser cannon screen") || nLower.Contains("buildup meter") ||
                nLower.Contains("walkiebox") || nLower.Contains("walkietalkiebox") || nLower.Contains("walkietalkie 1") || nLower.Contains("walkietalkie 2") ||
                nLower.Contains("upgrade particle") || nLower.Contains("upgrade map") || nLower.Contains("upgrade death head") ||
                nLower.Contains("gun bullet") || nLower.Contains("gun muzzle") ||
                nLower == "attributes" || nLower == "equip cube" || nLower == "equippable" || nLower == "manager" || nLower == "player avatar" || nLower == "stand new" || nLower == "stuff" || nLower == "unequip auto hold" ||
                nLower == "gun" || nLower == "melee" || nLower == "mine" || nLower == "orb" || nLower == "upgrade" || nLower == "vehicle" || nLower == "staffs" || nLower == "melee weapons")
            {
                return false;
            }

            // Chuẩn hóa từ đồng nghĩa
            if (name.Equals("Melee Sledge Hammer", StringComparison.OrdinalIgnoreCase)) name = "Melee Sledgehammer";
            else if (name.Equals("Leaf blower", StringComparison.OrdinalIgnoreCase)) name = "Leaf Blower";
            else if (name.Equals("ReviveItem", StringComparison.OrdinalIgnoreCase)) name = "Revive Item";
            else if (name.Equals("Staff Torque", StringComparison.OrdinalIgnoreCase)) name = "Torque Staff";
            else if (name.Equals("Staff Void", StringComparison.OrdinalIgnoreCase)) name = "Void Staff";
            else if (name.Equals("Staff Zero Gravity", StringComparison.OrdinalIgnoreCase)) name = "Zero Gravity Staff";
            else if (name.Equals("WalkieTalkie", StringComparison.OrdinalIgnoreCase)) name = "Walkie Talkie";
            else if (name.Equals("Stun Baton", StringComparison.OrdinalIgnoreCase) || name.Equals("Stun Baton Variant", StringComparison.OrdinalIgnoreCase)) name = "Melee Stun Baton";

            string key = name.Replace(" ", "").Replace("-", "").Replace("_", "").ToLowerInvariant();

            // Kiểm tra trùng lặp
            if (itemsCatalog.Exists(x => x.CleanKey == key || x.CleanKey == "item" + key || ("item" + x.CleanKey) == key))
                return false;

            // Phân loại nhóm thông minh
            string cat = "Công cụ";
            string tag = "[Công cụ]";

            string lower = name.ToLowerInvariant();
            if (lower.Contains("gun") || lower.Contains("shotgun") || lower.Contains("cannon") || lower.Contains("handgun")) { cat = "Súng"; tag = "[Súng]"; }
            else if (lower.Contains("melee") || lower.Contains("bat") || lower.Contains("sword") || lower.Contains("hammer") || lower.Contains("baton")) { cat = "Cận chiến"; tag = "[Cận chiến]"; }
            else if (lower.Contains("grenade") || lower.Contains("mine")) { cat = "Cháy nổ"; tag = "[Cháy nổ]"; }
            else if (lower.Contains("drone")) { cat = "Drone"; tag = "[Drone]"; }
            else if (lower.Contains("orb")) { cat = "Orb"; tag = "[Orb]"; }
            else if (lower.Contains("cart") || lower.Contains("scooter") || lower.Contains("vehicle")) { cat = "Xe"; tag = "[Xe]"; }
            else if (lower.Contains("upgrade")) { cat = "Nâng cấp"; tag = "[Nâng cấp]"; }
            else if (lower.Contains("crystal") || lower.Contains("battery")) { cat = "Năng lượng"; tag = "[Năng lượng]"; }
            else if (lower.Contains("health") || lower.Contains("revive") || lower.Contains("defib")) { cat = "Y tế"; tag = "[Y tế]"; }

            itemsCatalog.Add(new ItemEntry { Category = cat, Tag = tag, Name = name });
            return true;
        }

        private async Task SendCommand(string cmd, string val = "")
        {
            try
            {
                string payload = string.IsNullOrEmpty(val) ? $"CMD {cmd}" : $"CMD {cmd} {val}";
                await SendTcpRequestAsync(payload, 2000);
                System.Media.SystemSounds.Beep.Play();
            }
            catch { }
        }

        // Bắt phím tắt toàn cầu (kể cả khi đang trong game)
        private bool f1Down, f2Down, f3Down, f4Down, f5Down, f6Down, f7Down, f8Down, f9Down, f10Down, f11Down, f12Down;

        private async Task CheckGlobalHotkeys()
        {
            // F1 = 0x70 (Bay Xuyên Tường Noclip)
            if ((GetAsyncKeyState(0x70) & 0x8000) != 0) { if (!f1Down) { f1Down = true; await SendCommand("toggle_noclip"); } } else f1Down = false;
            // F2 = 0x71 (Speed Hack)
            if ((GetAsyncKeyState(0x71) & 0x8000) != 0) { if (!f2Down) { f2Down = true; await SendCommand("toggle_speed"); } } else f2Down = false;
            // F3 = 0x72 (Vô hạn Nhảy)
            if ((GetAsyncKeyState(0x72) & 0x8000) != 0) { if (!f3Down) { f3Down = true; await SendCommand("toggle_jump"); } } else f3Down = false;
            // F4 = 0x73 (Vô hạn Thể lực)
            if ((GetAsyncKeyState(0x73) & 0x8000) != 0) { if (!f4Down) { f4Down = true; await SendCommand("toggle_stamina"); } } else f4Down = false;
            // F5 = 0x74 (Chống ngã Anti-Tumble)
            if ((GetAsyncKeyState(0x74) & 0x8000) != 0) { if (!f5Down) { f5Down = true; await SendCommand("toggle_tumble"); } } else f5Down = false;
            // F6 = 0x75 (Bất tử God Mode)
            if ((GetAsyncKeyState(0x75) & 0x8000) != 0) { if (!f6Down) { f6Down = true; await SendCommand("toggle_god"); } } else f6Down = false;
            // F7 = 0x76 (Fullbright Sáng map)
            if ((GetAsyncKeyState(0x76) & 0x8000) != 0) { if (!f7Down) { f7Down = true; await SendCommand("toggle_fullbright"); } } else f7Down = false;
            // F8 = 0x77 (ESP Xuyên Tường)
            if ((GetAsyncKeyState(0x77) & 0x8000) != 0) { if (!f8Down) { f8Down = true; await SendCommand("toggle_esp"); } } else f8Down = false;
            // F9 = 0x78 (Hồi 100% Máu)
            if ((GetAsyncKeyState(0x78) & 0x8000) != 0) { if (!f9Down) { f9Down = true; await SendCommand("heal"); } } else f9Down = false;
            // F10 = 0x79 (Hồi sinh)
            if ((GetAsyncKeyState(0x79) & 0x8000) != 0) { if (!f10Down) { f10Down = true; await SendCommand("revive"); } } else f10Down = false;
            // F11 = 0x7A (Max Perks)
            if ((GetAsyncKeyState(0x7A) & 0x8000) != 0) { if (!f11Down) { f11Down = true; await SendCommand("max_upgrades"); } } else f11Down = false;
            // F12 = 0x7B (Sạc Xe Truck)
            if ((GetAsyncKeyState(0x7B) & 0x8000) != 0) { if (!f12Down) { f12Down = true; await SendCommand("charge_truck"); } } else f12Down = false;
        }
    }
}
