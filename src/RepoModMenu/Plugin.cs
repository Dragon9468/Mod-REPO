using System;
using System.Collections.Generic;
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
        public const string ModVersion = "1.2.0";

        // Cached Reflection Fields
        private static readonly FieldInfo FieldJumpExtra = typeof(PlayerController).GetField("JumpExtra", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly FieldInfo FieldJumpExtraCurrent = typeof(PlayerController).GetField("JumpExtraCurrent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly FieldInfo FieldMoveMultiplier = typeof(PlayerController).GetField("MoveMultiplier", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        
        private static readonly FieldInfo FieldGodMode = typeof(PlayerHealth).GetField("godMode", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly FieldInfo FieldHealth = typeof(PlayerHealth).GetField("health", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly FieldInfo FieldMaxHealth = typeof(PlayerHealth).GetField("maxHealth", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly FieldInfo FieldInvincibleTimer = typeof(PlayerHealth).GetField("invincibleTimer", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        private static readonly FieldInfo FieldTumbleWings = typeof(PlayerAvatar).GetField("upgradeTumbleWings", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly FieldInfo FieldTumbleClimb = typeof(PlayerAvatar).GetField("upgradeTumbleClimb", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly FieldInfo FieldCrouchRest = typeof(PlayerAvatar).GetField("upgradeCrouchRest", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        private static readonly FieldInfo FieldPlayerName = typeof(PlayerAvatar).GetField("playerName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly FieldInfo FieldIsLocal = typeof(PlayerAvatar).GetField("isLocal", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly FieldInfo FieldDollarValue = typeof(ValuableObject).GetField("dollarValueCurrent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        // UI State
        private bool isMenuVisible = false;
        private Rect windowRect = new Rect(40, 40, 500, 600);
        private int currentTab = 0;
        private readonly string[] tabNames = new string[] { "Movement", "Health", "Upgrades", "X-Ray ESP", "Balo Items", "Hotkeys" };

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

        // Upgrades Modifiers
        public static float CustomGrabRange = 4f;
        public static float CustomGrabStrength = 1f;
        public static float CustomThrowStrength = 1f;
        private bool upgradesInitialized = false;

        // X-Ray / ESP Settings
        public static bool EnableESP = false;
        public static bool EspShowEnemies = true;
        public static bool EspShowValuables = true;
        public static bool EspShowPlayers = true;
        public static float EspMaxDistance = 120f;

        // ESP Caching
        private float espCacheTimer = 0f;
        private readonly List<EnemyParent> cachedEnemies = new List<EnemyParent>();
        private readonly List<ValuableObject> cachedValuables = new List<ValuableObject>();
        private readonly List<PlayerAvatar> cachedPlayers = new List<PlayerAvatar>();

        // Items Database Cache
        private readonly List<Item> cachedItems = new List<Item>();
        private Vector2 itemScrollPos = Vector2.zero;
        private string itemSearchQuery = "";
        private float itemScanTimer = 0f;

        // Visual / Lighting
        private GameObject fullbrightLightObj;
        private Light fullbrightLight;

        // Notification toast
        private string notificationText = "";
        private float notificationTimer = 0f;

        // GUI Styles
        private GUIStyle enemyEspStyle;
        private GUIStyle valuableEspStyle;
        private GUIStyle playerEspStyle;

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

            if (Input.GetKeyDown(KeyCode.F8))
            {
                EnableESP = !EnableESP;
                ShowNotification($"X-Ray ESP: {(EnableESP ? "ON" : "OFF")}");
            }

            // 3. Thực thi logic cheat theo từng frame
            ApplyCheats();

            // 4. Cập nhật cache ESP mỗi 0.6 giây
            if (EnableESP)
            {
                espCacheTimer += Time.deltaTime;
                if (espCacheTimer > 0.6f)
                {
                    espCacheTimer = 0f;
                    RefreshEspCache();
                }
            }

            // 5. Quét danh sách vật phẩm nếu chưa có
            itemScanTimer += Time.deltaTime;
            if (itemScanTimer > 2.0f)
            {
                itemScanTimer = 0f;
                if (cachedItems.Count == 0)
                {
                    RefreshItemsList();
                }
            }

            // 6. Giảm thời gian thông báo Toast
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

            // --- Grabber Upgrades / Tay cầm đồ ---
            if (controller.playerAvatarScript != null && controller.playerAvatarScript.physGrabber != null)
            {
                var grabber = controller.playerAvatarScript.physGrabber;
                if (!upgradesInitialized)
                {
                    CustomGrabRange = grabber.grabRange;
                    CustomGrabStrength = grabber.grabStrength;
                    CustomThrowStrength = grabber.throwStrength;
                    upgradesInitialized = true;
                }

                grabber.grabRange = CustomGrabRange;
                grabber.grabStrength = CustomGrabStrength;
                grabber.throwStrength = CustomThrowStrength;
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

        private void RefreshEspCache()
        {
            cachedEnemies.Clear();
            cachedValuables.Clear();
            cachedPlayers.Clear();

            if (EspShowEnemies)
            {
                var enemies = FindObjectsOfType<EnemyParent>();
                if (enemies != null) cachedEnemies.AddRange(enemies);
            }

            if (EspShowValuables)
            {
                var valuables = FindObjectsOfType<ValuableObject>();
                if (valuables != null) cachedValuables.AddRange(valuables);
            }

            if (EspShowPlayers)
            {
                var players = FindObjectsOfType<PlayerAvatar>();
                if (players != null)
                {
                    foreach (var p in players)
                    {
                        if (p != null)
                        {
                            bool isLocalPlayer = false;
                            if (FieldIsLocal != null)
                            {
                                isLocalPlayer = (bool)FieldIsLocal.GetValue(p);
                            }
                            if (!isLocalPlayer)
                            {
                                cachedPlayers.Add(p);
                            }
                        }
                    }
                }
            }
        }

        private void RefreshItemsList()
        {
            cachedItems.Clear();
            var items = Resources.FindObjectsOfTypeAll<Item>();
            if (items == null) return;

            var added = new HashSet<string>();
            foreach (var it in items)
            {
                if (it != null && !string.IsNullOrEmpty(it.itemName) && !added.Contains(it.itemName))
                {
                    if (it.itemName.StartsWith("ItemUpgrade", StringComparison.OrdinalIgnoreCase)) continue;

                    added.Add(it.itemName);
                    cachedItems.Add(it);
                }
            }

            cachedItems.Sort((a, b) => string.Compare(a.itemName, b.itemName, StringComparison.OrdinalIgnoreCase));
        }

        private GameObject GetPrefabFromItem(Item item)
        {
            if (item == null || item.prefab == null) return null;
            try
            {
                var prop = item.prefab.GetType().GetProperty("Prefab", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (prop != null)
                {
                    return prop.GetValue(item.prefab) as GameObject;
                }
            }
            catch { }
            return null;
        }

        private void AddItemToPlayerInventory(Item item, bool forceSpawnOnGround = false)
        {
            if (item == null) return;

            GameObject prefabObj = GetPrefabFromItem(item);
            if (prefabObj == null)
            {
                ShowNotification($"Vật phẩm [{item.itemName}] không tìm thấy Prefab 3D!");
                return;
            }

            var controller = PlayerController.instance;
            if (controller == null)
            {
                ShowNotification("Chưa vào trận hoặc không tìm thấy Player!");
                return;
            }

            Vector3 spawnPos = controller.transform.position + controller.transform.forward * 1.2f + Vector3.up * 0.5f;
            GameObject spawnedObj = Instantiate(prefabObj, spawnPos, Quaternion.identity);

            if (forceSpawnOnGround)
            {
                ShowNotification($"Đã thả [{item.itemName}] ra trước mặt!");
                return;
            }

            // Cất vào Balo nếu còn ô trống
            var inv = Inventory.instance;
            if (inv != null)
            {
                int freeIndex = inv.GetFirstFreeInventorySpotIndex();
                if (freeIndex >= 0)
                {
                    var spot = inv.GetSpotByIndex(freeIndex);
                    var equippable = spawnedObj.GetComponent<ItemEquippable>();
                    if (spot != null && equippable != null)
                    {
                        spot.EquipItem(equippable);
                        ShowNotification($"Đã cất [{item.itemName}] vào Balo (Ô {freeIndex + 1})!");
                        return;
                    }
                }
            }

            ShowNotification($"Balo đã đầy! Đã thả [{item.itemName}] ra đất trước mặt.");
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

        private void InitStyles()
        {
            if (enemyEspStyle == null)
            {
                enemyEspStyle = new GUIStyle(GUI.skin.label);
                enemyEspStyle.fontSize = 12;
                enemyEspStyle.normal.textColor = Color.red;
                enemyEspStyle.alignment = TextAnchor.MiddleCenter;
            }

            if (valuableEspStyle == null)
            {
                valuableEspStyle = new GUIStyle(GUI.skin.label);
                valuableEspStyle.fontSize = 12;
                valuableEspStyle.normal.textColor = Color.yellow;
                valuableEspStyle.alignment = TextAnchor.MiddleCenter;
            }

            if (playerEspStyle == null)
            {
                playerEspStyle = new GUIStyle(GUI.skin.label);
                playerEspStyle.fontSize = 12;
                playerEspStyle.normal.textColor = Color.cyan;
                playerEspStyle.alignment = TextAnchor.MiddleCenter;
            }
        }

        private void OnGUI()
        {
            InitStyles();

            // 1. Vẽ X-Ray ESP
            if (EnableESP)
            {
                DrawEspOverlay();
            }

            // 2. Vẽ thông báo Toast nhỏ góc trên màn hình
            if (notificationTimer > 0f)
            {
                var notifStyle = new GUIStyle(GUI.skin.box);
                notifStyle.fontSize = 14;
                notifStyle.normal.textColor = Color.yellow;
                GUI.Box(new Rect(Screen.width / 2f - 180, 20, 360, 35), $"[MOD] {notificationText}", notifStyle);
            }

            // 3. Vẽ Menu chính
            if (isMenuVisible)
            {
                GUI.backgroundColor = new Color(0.12f, 0.12f, 0.14f, 0.95f);
                windowRect = GUI.Window(9999, windowRect, DrawWindowContent, $"★ R.E.P.O Master Menu v{ModVersion} ★");
            }
        }

        private void DrawEspOverlay()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 myPos = cam.transform.position;

            // --- Vẽ ESP Quái vật ---
            if (EspShowEnemies)
            {
                foreach (var enemy in cachedEnemies)
                {
                    if (enemy == null || !enemy.gameObject.activeInHierarchy) continue;

                    Vector3 pos = enemy.transform.position + Vector3.up * 0.8f;
                    float dist = Vector3.Distance(myPos, pos);
                    if (dist > EspMaxDistance) continue;

                    Vector3 screenPos = cam.WorldToScreenPoint(pos);
                    if (screenPos.z > 0)
                    {
                        string name = string.IsNullOrEmpty(enemy.enemyName) ? "Quái vật" : enemy.enemyName;
                        string text = $"🔴 {name} [{dist:F0}m]";
                        float y = Screen.height - screenPos.y;
                        GUI.Label(new Rect(screenPos.x - 100, y - 10, 200, 25), text, enemyEspStyle);
                    }
                }
            }

            // --- Vẽ ESP Vật phẩm / Tiền ---
            if (EspShowValuables)
            {
                foreach (var val in cachedValuables)
                {
                    if (val == null || !val.gameObject.activeInHierarchy) continue;

                    Vector3 pos = val.transform.position;
                    float dist = Vector3.Distance(myPos, pos);
                    if (dist > EspMaxDistance) continue;

                    Vector3 screenPos = cam.WorldToScreenPoint(pos);
                    if (screenPos.z > 0)
                    {
                        int dollar = 0;
                        if (FieldDollarValue != null)
                        {
                            dollar = (int)FieldDollarValue.GetValue(val);
                        }

                        string text = $"💰 ${dollar} [{dist:F0}m]";
                        float y = Screen.height - screenPos.y;
                        GUI.Label(new Rect(screenPos.x - 100, y - 10, 200, 25), text, valuableEspStyle);
                    }
                }
            }

            // --- Vẽ ESP Đồng đội ---
            if (EspShowPlayers)
            {
                foreach (var player in cachedPlayers)
                {
                    if (player == null || !player.gameObject.activeInHierarchy) continue;

                    Vector3 pos = player.transform.position + Vector3.up * 1.5f;
                    float dist = Vector3.Distance(myPos, pos);
                    if (dist > EspMaxDistance) continue;

                    Vector3 screenPos = cam.WorldToScreenPoint(pos);
                    if (screenPos.z > 0)
                    {
                        string pName = "Đồng đội";
                        if (FieldPlayerName != null)
                        {
                            var rawName = (string)FieldPlayerName.GetValue(player);
                            if (!string.IsNullOrEmpty(rawName)) pName = rawName;
                        }

                        string text = $"👤 {pName} [{dist:F0}m]";
                        float y = Screen.height - screenPos.y;
                        GUI.Label(new Rect(screenPos.x - 100, y - 10, 200, 25), text, playerEspStyle);
                    }
                }
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
                    DrawHealthTab();
                    break;
                case 2:
                    DrawUpgradesTab();
                    break;
                case 3:
                    DrawEspTab();
                    break;
                case 4:
                    DrawItemsTab();
                    break;
                case 5:
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

        private void DrawHealthTab()
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

        private void DrawUpgradesTab()
        {
            var controller = PlayerController.instance;
            var avatar = controller != null ? controller.playerAvatarScript : null;
            var health = controller != null ? GetLocalPlayerHealth(controller) : null;

            GUILayout.Label("<b>HỆ THỐNG NÂNG CẤP (PERKS & STATS):</b>");

            if (GUILayout.Button("⭐ MAX TẤT CẢ NÂNG CẤP (1-Click Max All) ⭐", GUILayout.Height(30)))
            {
                if (controller != null)
                {
                    if (health != null)
                    {
                        FieldMaxHealth?.SetValue(health, 250);
                        FieldHealth?.SetValue(health, 250);
                    }
                    FieldJumpExtra?.SetValue(controller, 5);
                    controller.EnergyStart = 200f;
                    controller.EnergyCurrent = 200f;

                    if (avatar != null)
                    {
                        FieldTumbleWings?.SetValue(avatar, true);
                        FieldTumbleClimb?.SetValue(avatar, true);
                        FieldCrouchRest?.SetValue(avatar, true);
                    }

                    CustomGrabRange = 15f;
                    CustomGrabStrength = 5f;
                    CustomThrowStrength = 3.5f;

                    ShowNotification("Đã Max tất cả chỉ số Nâng Cấp!");
                }
                else
                {
                    ShowNotification("Vui lòng vào trận trước khi áp dụng!");
                }
            }

            GUILayout.Space(10);
            GUILayout.Label("<b>Tùy chỉnh tay cầm đồ (PhysGrabber):</b>");

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Tầm với đồ vật: {CustomGrabRange:F1}m", GUILayout.Width(170));
            CustomGrabRange = GUILayout.HorizontalSlider(CustomGrabRange, 3f, 25f);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Lực cầm đồ nặng: {CustomGrabStrength:F1}x", GUILayout.Width(170));
            CustomGrabStrength = GUILayout.HorizontalSlider(CustomGrabStrength, 1f, 10f);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Lực ném đồ: {CustomThrowStrength:F1}x", GUILayout.Width(170));
            CustomThrowStrength = GUILayout.HorizontalSlider(CustomThrowStrength, 1f, 8f);
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.Label("<b>Nâng cấp nhân vật:</b>");

            if (GUILayout.Button("Tăng Máu Tối Đa (+100 Max HP)"))
            {
                if (health != null)
                {
                    int currentMax = GetMaxHealth(health);
                    FieldMaxHealth?.SetValue(health, currentMax + 100);
                    FieldHealth?.SetValue(health, currentMax + 100);
                    ShowNotification($"Máu tối đa mới: {currentMax + 100} HP");
                }
            }

            if (GUILayout.Button("Mở khóa Đôi Cánh (Tumble Wings)"))
            {
                if (avatar != null)
                {
                    FieldTumbleWings?.SetValue(avatar, true);
                    ShowNotification("Đã kích hoạt Đôi Cánh khi rơi!");
                }
            }
        }

        private void DrawEspTab()
        {
            EnableESP = GUILayout.Toggle(EnableESP, " [F8] Bật X-Ray ESP (Nhìn xuyên tường)");

            GUILayout.Space(10);
            GUILayout.Label("<b>Bộ lọc hiển thị:</b>");
            EspShowEnemies = GUILayout.Toggle(EspShowEnemies, " 🔴 Hiện Quái Vật (Enemies)");
            EspShowValuables = GUILayout.Toggle(EspShowValuables, " 💰 Hiện Vật Phẩm & Tiền (Valuables / Loot)");
            EspShowPlayers = GUILayout.Toggle(EspShowPlayers, " 👤 Hiện Đồng Đội (Players)");

            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Khoảng cách quét: {EspMaxDistance:F0}m", GUILayout.Width(160));
            EspMaxDistance = GUILayout.HorizontalSlider(EspMaxDistance, 30f, 250f);
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.Label($"<i>Đang hiển thị: {cachedEnemies.Count} quái, {cachedValuables.Count} đồ, {cachedPlayers.Count} bạn</i>");
        }

        private void DrawItemsTab()
        {
            GUILayout.Label("<b>KHO VẬT PHẨM ĐI CHỢ (SHOP ITEMS):</b>");

            // Nút Refresh danh sách
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("🔄 Quét lại kho đồ", GUILayout.Width(140)))
            {
                RefreshItemsList();
                ShowNotification($"Đã nạp {cachedItems.Count} vật phẩm từ game!");
            }

            GUILayout.Label("Tìm: ", GUILayout.Width(35));
            itemSearchQuery = GUILayout.TextField(itemSearchQuery);
            GUILayout.EndHorizontal();

            GUILayout.Space(5);
            if (cachedItems.Count == 0)
            {
                GUILayout.Label("<i>(Đang nạp dữ liệu vật phẩm... Hãy bấm 'Quét lại kho đồ' nếu chưa hiện)</i>");
            }
            else
            {
                GUILayout.Label($"Tìm thấy: <b>{cachedItems.Count}</b> vật phẩm sẵn sàng!");
            }

            GUILayout.Space(5);

            // Bắt đầu danh sách cuộn
            itemScrollPos = GUILayout.BeginScrollView(itemScrollPos, GUILayout.Height(330));
            foreach (var item in cachedItems)
            {
                if (item == null) continue;

                // Lọc tìm kiếm
                if (!string.IsNullOrEmpty(itemSearchQuery) && !item.itemName.ToLower().Contains(itemSearchQuery.ToLower()))
                {
                    continue;
                }

                GUILayout.BeginHorizontal(GUI.skin.box);
                
                string iconText = "📦";
                string lower = item.itemName.ToLower();
                if (lower.Contains("gun") || lower.Contains("laser") || lower.Contains("tranq")) iconText = "🔫";
                else if (lower.Contains("drone")) iconText = "🛸";
                else if (lower.Contains("grenade") || lower.Contains("mine") || lower.Contains("shockwave")) iconText = "💣";
                else if (lower.Contains("melee") || lower.Contains("hammer") || lower.Contains("baton")) iconText = "🔨";
                else if (lower.Contains("health") || lower.Contains("revive")) iconText = "💊";
                else if (lower.Contains("duck")) iconText = "🦆";

                GUILayout.Label($"{iconText} <b>{item.itemName}</b>", GUILayout.Width(230));

                if (GUILayout.Button("+ Balo", GUILayout.Width(85)))
                {
                    AddItemToPlayerInventory(item, false);
                }

                if (GUILayout.Button("Thả đất", GUILayout.Width(75)))
                {
                    AddItemToPlayerInventory(item, true);
                }

                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
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
            GUILayout.Label("• <b>F8</b>: Bật / Tắt X-Ray ESP (Nhìn xuyên tường)");

            GUILayout.Space(10);
            GUILayout.Label("<b>KHI VÀO LOBBY CỦA NGƯỜI KHÁC (CLIENT):</b>");
            GUILayout.Label("✔ Speed Hack, Air Jump, Stamina: Hoạt động 100%");
            GUILayout.Label("✔ X-Ray ESP, Fullbright: Hoạt động 100%");
            GUILayout.Label("✔ Super Grab (Tầm với xa, ném mạnh): Hoạt động 100%");
            GUILayout.Label("✔ Upgrades (Wings, Stamina, Max HP): Hoạt động 100%");
            GUILayout.Label("✔ Add đồ đi chợ vào Balo: Hoạt động 100%");
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
