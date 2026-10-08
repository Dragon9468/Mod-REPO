using System;
using System.Reflection;
using BepInEx;
using UnityEngine;

namespace RepoModMenu
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string ModGUID = "com.phong.repocoolmenu";
        public const string ModName = "REPO Master Mod Menu";
        public const string ModVersion = "1.0.0";

        // Cached Reflection Fields
        private static readonly FieldInfo FieldJumpExtra = typeof(PlayerController).GetField("JumpExtra", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly FieldInfo FieldJumpExtraCurrent = typeof(PlayerController).GetField("JumpExtraCurrent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly FieldInfo FieldMoveMultiplier = typeof(PlayerController).GetField("MoveMultiplier", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        
        private static readonly FieldInfo FieldGodMode = typeof(PlayerHealth).GetField("godMode", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly FieldInfo FieldHealth = typeof(PlayerHealth).GetField("health", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly FieldInfo FieldMaxHealth = typeof(PlayerHealth).GetField("maxHealth", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly FieldInfo FieldInvincibleTimer = typeof(PlayerHealth).GetField("invincibleTimer", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        // UI State
        private bool isMenuVisible = false;
        private Rect windowRect = new Rect(40, 40, 380, 500);
        private int currentTab = 0;
        private readonly string[] tabNames = new string[] { "Movement", "Player & Stats", "Hotkeys & Info" };

        // Cheats & Settings
        public static bool EnableSpeedHack = false;
        public static float SpeedMultiplier = 2.5f;
        private float originalMoveSpeed = -1f;
        private float originalSprintSpeed = -1f;

        public static bool EnableInfiniteJump = false;
        public static bool EnableInfiniteStamina = false;
        public static bool EnableNoTumble = false;
        public static bool EnableGodMode = false;
        public static bool EnableAntiGravity = false;
        public static bool EnableFullbright = false;

        // Visual / Lighting
        private GameObject fullbrightLightObj;
        private Light fullbrightLight;

        // Notification toast
        private string notificationText = "";
        private float notificationTimer = 0f;

        private void Awake()
        {
            Logger.LogInfo($"{ModName} v{ModVersion} has loaded successfully!");
        }

        private void Update()
        {
            // 1. Phím tắt Toggle Menu: [Insert] hoặc [F1]
            if (Input.GetKeyDown(KeyCode.Insert) || Input.GetKeyDown(KeyCode.F1))
            {
                isMenuVisible = !isMenuVisible;
            }

            // 2. Hotkeys kích hoạt nhanh trong trận
            if (Input.GetKeyDown(KeyCode.F2))
            {
                EnableSpeedHack = !EnableSpeedHack;
                ShowNotification($"Speed Hack: {(EnableSpeedHack ? "ON (" + SpeedMultiplier.ToString("F1") + "x)" : "OFF")}");
            }

            if (Input.GetKeyDown(KeyCode.F3))
            {
                EnableInfiniteJump = !EnableInfiniteJump;
                ShowNotification($"Infinite Double Jump: {(EnableInfiniteJump ? "ON" : "OFF")}");
            }

            if (Input.GetKeyDown(KeyCode.F4))
            {
                EnableInfiniteStamina = !EnableInfiniteStamina;
                ShowNotification($"Infinite Stamina: {(EnableInfiniteStamina ? "ON" : "OFF")}");
            }

            if (Input.GetKeyDown(KeyCode.F5))
            {
                EnableGodMode = !EnableGodMode;
                ShowNotification($"God Mode: {(EnableGodMode ? "ON" : "OFF")}");
            }

            if (Input.GetKeyDown(KeyCode.F6))
            {
                EnableNoTumble = !EnableNoTumble;
                ShowNotification($"Anti-Tumble / No Fall: {(EnableNoTumble ? "ON" : "OFF")}");
            }

            if (Input.GetKeyDown(KeyCode.F7))
            {
                EnableFullbright = !EnableFullbright;
                ShowNotification($"Fullbright / Nightvision: {(EnableFullbright ? "ON" : "OFF")}");
            }

            // 3. Thực thi logic cheat theo từng frame
            ApplyCheats();

            // 4. Giảm thời gian thông báo
            if (notificationTimer > 0f)
            {
                notificationTimer -= Time.deltaTime;
            }
        }

        private void ApplyCheats()
        {
            var controller = PlayerController.instance;
            if (controller == null) return;

            // Lưu giá trị mặc định của tốc độ ban đầu
            if (originalMoveSpeed < 0f && controller.MoveSpeed > 0f)
            {
                originalMoveSpeed = controller.MoveSpeed;
                originalSprintSpeed = controller.SprintSpeed;
            }

            // --- Movement Cheats ---
            if (EnableSpeedHack)
            {
                FieldMoveMultiplier?.SetValue(controller, SpeedMultiplier);
                if (originalSprintSpeed > 0f)
                {
                    controller.SprintSpeed = originalSprintSpeed * SpeedMultiplier;
                }
            }
            else
            {
                FieldMoveMultiplier?.SetValue(controller, 1f);
                if (originalSprintSpeed > 0f)
                {
                    controller.SprintSpeed = originalSprintSpeed;
                }
            }

            if (EnableInfiniteJump)
            {
                // Luôn nạp đầy lượt nhảy trên không (air jumps)
                FieldJumpExtra?.SetValue(controller, 999);
                FieldJumpExtraCurrent?.SetValue(controller, 999);
            }

            if (EnableInfiniteStamina)
            {
                controller.DebugEnergy = true;
                controller.EnergyCurrent = controller.EnergyStart;
            }
            else
            {
                controller.DebugEnergy = false;
            }

            if (EnableNoTumble)
            {
                controller.DebugNoTumble = true;
            }

            if (EnableAntiGravity)
            {
                controller.AntiGravity(1f);
            }

            // --- Health & Combat Cheats ---
            PlayerHealth health = GetLocalPlayerHealth(controller);
            if (health != null)
            {
                if (EnableGodMode)
                {
                    FieldGodMode?.SetValue(health, true);
                    FieldInvincibleTimer?.SetValue(health, 999f);

                    int maxHp = GetMaxHealth(health);
                    if (maxHp > 0)
                    {
                        FieldHealth?.SetValue(health, maxHp);
                    }
                }
                else
                {
                    FieldGodMode?.SetValue(health, false);
                }
            }

            // --- Fullbright / Đèn sáng cá nhân ---
            if (EnableFullbright)
            {
                if (fullbrightLightObj == null)
                {
                    fullbrightLightObj = new GameObject("ModFullbrightLight");
                    fullbrightLight = fullbrightLightObj.AddComponent<Light>();
                    fullbrightLight.type = LightType.Point;
                    fullbrightLight.range = 50f;
                    fullbrightLight.intensity = 2f;
                    fullbrightLight.color = Color.white;
                }

                if (Camera.main != null)
                {
                    fullbrightLightObj.transform.position = Camera.main.transform.position;
                }
                else
                {
                    fullbrightLightObj.transform.position = controller.transform.position + Vector3.up;
                }
                fullbrightLight.enabled = true;
            }
            else
            {
                if (fullbrightLight != null)
                {
                    fullbrightLight.enabled = false;
                }
            }
        }

        private PlayerHealth GetLocalPlayerHealth(PlayerController controller)
        {
            if (controller.playerAvatarScript != null && controller.playerAvatarScript.playerHealth != null)
            {
                return controller.playerAvatarScript.playerHealth;
            }
            return null;
        }

        private int GetCurrentHealth(PlayerHealth health)
        {
            if (health == null || FieldHealth == null) return 0;
            return (int)FieldHealth.GetValue(health);
        }

        private int GetMaxHealth(PlayerHealth health)
        {
            if (health == null || FieldMaxHealth == null) return 100;
            return (int)FieldMaxHealth.GetValue(health);
        }

        private void ShowNotification(string msg)
        {
            notificationText = msg;
            notificationTimer = 2.5f;
        }

        private void OnGUI()
        {
            // Vẽ thông báo Toast nhỏ góc trên màn hình khi bật/tắt phím tắt
            if (notificationTimer > 0f)
            {
                var notifStyle = new GUIStyle(GUI.skin.box);
                notifStyle.fontSize = 14;
                notifStyle.normal.textColor = Color.yellow;
                GUI.Box(new Rect(Screen.width / 2f - 160, 20, 320, 35), $"[MOD] {notificationText}", notifStyle);
            }

            // Vẽ Menu chính nếu đang bật
            if (isMenuVisible)
            {
                GUI.backgroundColor = new Color(0.12f, 0.12f, 0.14f, 0.95f);
                windowRect = GUI.Window(9999, windowRect, DrawWindowContent, $"★ R.E.P.O Master Menu v{ModVersion} ★");
            }
        }

        private void DrawWindowContent(int id)
        {
            GUILayout.BeginVertical();

            // Thanh chuyển Tab
            currentTab = GUILayout.Toolbar(currentTab, tabNames);
            GUILayout.Space(10);

            switch (currentTab)
            {
                case 0:
                    DrawMovementTab();
                    break;
                case 1:
                    DrawPlayerTab();
                    break;
                case 2:
                    DrawInfoTab();
                    break;
            }

            GUILayout.FlexibleSpace();
            GUILayout.Box("Nhấn [Insert] hoặc [F1] để ẩn/hiện Menu", GUILayout.ExpandWidth(true));
            GUILayout.EndVertical();

            // Kéo thả cửa sổ
            GUI.DragWindow(new Rect(0, 0, 10000, 25));
        }

        private void DrawMovementTab()
        {
            var controller = PlayerController.instance;

            EnableSpeedHack = GUILayout.Toggle(EnableSpeedHack, " [F2] Tăng tốc độ chạy (Speed Hack)");
            if (EnableSpeedHack)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"Hệ số tốc độ: {SpeedMultiplier:F1}x", GUILayout.Width(130));
                SpeedMultiplier = GUILayout.HorizontalSlider(SpeedMultiplier, 1.0f, 6.0f);
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(5);
            EnableInfiniteJump = GUILayout.Toggle(EnableInfiniteJump, " [F3] Vô hạn Double Jump / Bay nhảy");
            
            GUILayout.Space(5);
            EnableInfiniteStamina = GUILayout.Toggle(EnableInfiniteStamina, " [F4] Vô hạn Thể lực / Năng lượng");

            GUILayout.Space(5);
            EnableNoTumble = GUILayout.Toggle(EnableNoTumble, " [F6] Chống té ngã / No Tumble");

            GUILayout.Space(5);
            EnableAntiGravity = GUILayout.Toggle(EnableAntiGravity, " Giảm trọng lực (Float / Anti-Gravity)");

            GUILayout.Space(10);
            if (controller != null)
            {
                if (GUILayout.Button("Rơi chậm như lông vũ (Feather Fall 10s)"))
                {
                    controller.Feather(10f);
                    ShowNotification("Đã kích hoạt Feather Fall (10 giây)!");
                }
            }
        }

        private void DrawPlayerTab()
        {
            var controller = PlayerController.instance;
            PlayerHealth health = controller != null ? GetLocalPlayerHealth(controller) : null;

            EnableGodMode = GUILayout.Toggle(EnableGodMode, " [F5] Bất tử (God Mode)");

            GUILayout.Space(5);
            EnableFullbright = GUILayout.Toggle(EnableFullbright, " [F7] Đèn sáng toàn cảnh (Fullbright / Nightvision)");

            GUILayout.Space(10);
            GUILayout.Label("--- Thao tác tức thì ---");

            if (GUILayout.Button("Hồi phục 100% Máu (Instant Heal)"))
            {
                if (health != null)
                {
                    health.Heal(100);
                    ShowNotification("Đã hồi phục toàn bộ máu!");
                }
                else
                {
                    ShowNotification("Chưa vào trận hoặc không tìm thấy Player!");
                }
            }

            GUILayout.Space(5);
            if (GUILayout.Button("Hồi sinh lập tức (Instant Revive)"))
            {
                if (controller != null)
                {
                    controller.Revive(controller.transform.eulerAngles);
                    ShowNotification("Đã gọi hàm Revive!");
                }
                else
                {
                    ShowNotification("Không tìm thấy PlayerController!");
                }
            }

            GUILayout.Space(10);
            if (health != null)
            {
                int hp = GetCurrentHealth(health);
                int maxHp = GetMaxHealth(health);
                GUILayout.Label($"Máu hiện tại: {hp} / {maxHp}");
            }
            else
            {
                GUILayout.Label("Trạng thái: Chưa vào phòng game");
            }
        }

        private void DrawInfoTab()
        {
            GUILayout.Label("<b>DANH SÁCH PHÍM TẮT NHANH (HOTKEYS):</b>");
            GUILayout.Label("• <b>Insert / F1</b>: Ẩn / Hiện Menu");
            GUILayout.Label("• <b>F2</b>: Bật / Tắt Speed Hack");
            GUILayout.Label("• <b>F3</b>: Bật / Tắt Vô hạn Double Jump");
            GUILayout.Label("• <b>F4</b>: Bật / Tắt Vô hạn Thể lực (Stamina)");
            GUILayout.Label("• <b>F5</b>: Bật / Tắt Bất tử (God Mode)");
            GUILayout.Label("• <b>F6</b>: Bật / Tắt Chống ngã (Anti-Tumble)");
            GUILayout.Label("• <b>F7</b>: Bật / Tắt Sáng màn hình (Fullbright)");

            GUILayout.Space(10);
            GUILayout.Label("<b>LƯU Ý VỀ PHẦN MỀM:</b>");
            GUILayout.Label("• Mod này hoạt động độc lập và vĩnh viễn.");
            GUILayout.Label("• Không bị giới hạn thời gian (như WeMod/Wand).");
            GUILayout.Label("• Copy thư mục BepInEx sang máy khác là chơi được ngay.");
        }

        private void OnDestroy()
        {
            if (fullbrightLightObj != null)
            {
                Destroy(fullbrightLightObj);
            }
        }
    }
}
