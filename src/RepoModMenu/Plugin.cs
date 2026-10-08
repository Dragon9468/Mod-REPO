using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using BepInEx;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RepoModMenu
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string ModGUID = "com.phong.repocoolmenu";
        public const string ModName = "REPO Master Mod Menu";
        public const string ModVersion = "2.0.0";

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

        // Cheats & Settings (Public for IPC)
        public static bool EnableSpeedHack = false;
        public static float SpeedMultiplier = 2.5f;
        private float originalMoveSpeed = -1f;
        private float originalSprintSpeed = -1f;

        public static bool EnableInfiniteJump = false;
        public static bool EnableInfiniteStamina = false;
        public static bool EnableNoTumble = false;
        public static bool EnableGodMode = false;
        public static bool EnableAntiGravity = false;

        // Fullbright
        public static bool EnableFullbright = false;
        public static float FullbrightIntensity = 2.0f;
        public static bool DisableFog = true;
        private GameObject mapDirectionalLightObj;
        private Light mapDirectionalLight;
        private bool origLightingSaved = false;
        private Color origAmbientLight;
        private UnityEngine.Rendering.AmbientMode origAmbientMode;
        private float origAmbientIntensity;
        private bool origFog;
        private float origFogDensity;

        // Upgrades
        public static float CustomGrabRange = 4f;
        public static float CustomGrabStrength = 1f;
        public static float CustomThrowStrength = 1f;
        private bool upgradesInitialized = false;

        // Items Cache
        private readonly List<Item> cachedItems = new List<Item>();
        private float itemScanTimer = 0f;

        // Notification toast
        private string notificationText = "";
        private float notificationTimer = 0f;

        // IPC Server
        private HttpListener httpListener;
        private Thread httpListenerThread;
        private bool isHttpRunning = false;
        private readonly ConcurrentQueue<Action> mainThreadActions = new ConcurrentQueue<Action>();

        private void Awake()
        {
            Logger.LogInfo($"{ModName} v{ModVersion} initializing IPC server...");
            StartHttpServer();
        }

        private void StartHttpServer()
        {
            try
            {
                httpListener = new HttpListener();
                httpListener.Prefixes.Add("http://127.0.0.1:29999/");
                httpListener.Start();
                isHttpRunning = true;

                httpListenerThread = new Thread(HttpServerLoop)
                {
                    IsBackground = true
                };
                httpListenerThread.Start();
                Logger.LogInfo("IPC Server started on http://127.0.0.1:29999/");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to start IPC Server: {ex.Message}");
            }
        }

        private void HttpServerLoop()
        {
            while (isHttpRunning && httpListener != null && httpListener.IsListening)
            {
                try
                {
                    var context = httpListener.GetContext();
                    ThreadPool.QueueUserWorkItem((state) => HandleHttpRequest(context));
                }
                catch (HttpListenerException) { break; }
                catch (Exception ex)
                {
                    Logger.LogError($"IPC error: {ex.Message}");
                }
            }
        }

        private void HandleHttpRequest(HttpListenerContext context)
        {
            var req = context.Request;
            var res = context.Response;
            res.Headers.Add("Access-Control-Allow-Origin", "*");
            res.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");

            try
            {
                string path = req.Url.AbsolutePath.ToLower();

                if (path == "/api/status")
                {
                    bool inGame = PlayerController.instance != null;
                    int hp = 0, maxHp = 100;
                    if (inGame)
                    {
                        var health = GetLocalPlayerHealth(PlayerController.instance);
                        if (health != null)
                        {
                            hp = GetCurrentHealth(health);
                            maxHp = GetMaxHealth(health);
                        }
                    }

                    string json = $"{{\"inGame\":{inGame.ToString().ToLower()}," +
                                  $"\"health\":{hp}," +
                                  $"\"maxHealth\":{maxHp}," +
                                  $"\"speed\":{EnableSpeedHack.ToString().ToLower()}," +
                                  $"\"speedMultiplier\":{SpeedMultiplier}," +
                                  $"\"jump\":{EnableInfiniteJump.ToString().ToLower()}," +
                                  $"\"stamina\":{EnableInfiniteStamina.ToString().ToLower()}," +
                                  $"\"god\":{EnableGodMode.ToString().ToLower()}," +
                                  $"\"tumble\":{EnableNoTumble.ToString().ToLower()}," +
                                  $"\"fullbright\":{EnableFullbright.ToString().ToLower()}}}";

                    SendResponse(res, json, "application/json");
                    return;
                }

                if (path == "/api/items")
                {
                    var list = new List<string>();
                    lock (cachedItems)
                    {
                        foreach (var it in cachedItems)
                        {
                            if (it != null && !string.IsNullOrEmpty(it.itemName)) list.Add(it.itemName);
                        }
                    }
                    string json = "[\"" + string.Join("\",\"", list.ToArray()) + "\"]";
                    SendResponse(res, json, "application/json");
                    return;
                }

                if (path == "/api/cmd")
                {
                    string cmd = req.QueryString["cmd"] ?? "";
                    string val = req.QueryString["val"] ?? "";

                    mainThreadActions.Enqueue(() =>
                    {
                        ExecuteCommand(cmd, val);
                    });

                    SendResponse(res, "{\"ok\":true}", "application/json");
                    return;
                }

                SendResponse(res, "{\"error\":\"not_found\"}", "application/json", 404);
            }
            catch (Exception ex)
            {
                SendResponse(res, $"{{\"error\":\"{ex.Message}\"}}", "application/json", 500);
            }
        }

        private void SendResponse(HttpListenerResponse res, string text, string contentType, int statusCode = 200)
        {
            res.StatusCode = statusCode;
            res.ContentType = contentType;
            byte[] buf = Encoding.UTF8.GetBytes(text);
            res.ContentLength64 = buf.Length;
            using (var stream = res.OutputStream)
            {
                stream.Write(buf, 0, buf.Length);
            }
        }

        private void ExecuteCommand(string cmd, string val)
        {
            switch (cmd.ToLower())
            {
                case "toggle_speed":
                    EnableSpeedHack = !EnableSpeedHack;
                    ShowNotification($"Speed Hack: {(EnableSpeedHack ? "ON" : "OFF")}");
                    break;
                case "set_speed":
                    if (float.TryParse(val, out float spd)) SpeedMultiplier = Mathf.Clamp(spd, 1f, 10f);
                    break;
                case "toggle_jump":
                    EnableInfiniteJump = !EnableInfiniteJump;
                    ShowNotification($"Infinite Jump: {(EnableInfiniteJump ? "ON" : "OFF")}");
                    break;
                case "toggle_stamina":
                    EnableInfiniteStamina = !EnableInfiniteStamina;
                    ShowNotification($"Infinite Stamina: {(EnableInfiniteStamina ? "ON" : "OFF")}");
                    break;
                case "toggle_god":
                    EnableGodMode = !EnableGodMode;
                    ShowNotification($"God Mode: {(EnableGodMode ? "ON" : "OFF")}");
                    break;
                case "toggle_tumble":
                    EnableNoTumble = !EnableNoTumble;
                    ShowNotification($"Anti-Tumble: {(EnableNoTumble ? "ON" : "OFF")}");
                    break;
                case "toggle_fullbright":
                    EnableFullbright = !EnableFullbright;
                    ShowNotification($"Fullbright: {(EnableFullbright ? "ON" : "OFF")}");
                    break;
                case "heal":
                    if (PlayerController.instance != null)
                    {
                        var h = GetLocalPlayerHealth(PlayerController.instance);
                        if (h != null)
                        {
                            h.Heal(100);
                            ShowNotification("Đã hồi phục 100% Máu!");
                        }
                    }
                    break;
                case "revive":
                    if (PlayerController.instance != null)
                    {
                        PlayerController.instance.Revive(PlayerController.instance.transform.eulerAngles);
                        ShowNotification("Đã gọi hàm Revive!");
                    }
                    break;
                case "max_upgrades":
                    ApplyMaxUpgrades();
                    break;
                case "give_item":
                    GiveItemByName(val, true);
                    break;
                case "give_item_ground":
                    GiveItemByName(val, false);
                    break;
            }
        }

        private void GiveItemByName(string name, bool toBalo)
        {
            Item found = null;
            lock (cachedItems)
            {
                found = cachedItems.Find(x => x.itemName.Equals(name, StringComparison.OrdinalIgnoreCase));
            }

            if (found != null)
            {
                AddItemToPlayerInventory(found, !toBalo);
            }
            else
            {
                ShowNotification($"Không tìm thấy vật phẩm: {name}");
            }
        }

        private void ApplyMaxUpgrades()
        {
            var controller = PlayerController.instance;
            if (controller == null)
            {
                ShowNotification("Chưa vào trận!");
                return;
            }

            var health = GetLocalPlayerHealth(controller);
            if (health != null)
            {
                FieldMaxHealth?.SetValue(health, 250);
                FieldHealth?.SetValue(health, 250);
            }
            FieldJumpExtra?.SetValue(controller, 5);
            controller.EnergyStart = 200f;
            controller.EnergyCurrent = 200f;

            if (controller.playerAvatarScript != null)
            {
                FieldTumbleWings?.SetValue(controller.playerAvatarScript, true);
                FieldTumbleClimb?.SetValue(controller.playerAvatarScript, true);
                FieldCrouchRest?.SetValue(controller.playerAvatarScript, true);
            }

            CustomGrabRange = 15f;
            CustomGrabStrength = 5f;
            CustomThrowStrength = 3.5f;

            ShowNotification("Đã Max tất cả chỉ số Nâng Cấp!");
        }

        private void Update()
        {
            // 1. Thực thi các lệnh IPC được gửi từ ứng dụng ngoài trên Main Thread
            while (mainThreadActions.TryDequeue(out var action))
            {
                action?.Invoke();
            }

            // 2. Thực thi logic cheat theo từng frame
            ApplyCheats();

            // 3. Quét danh sách item
            itemScanTimer += Time.deltaTime;
            if (itemScanTimer > 2.0f)
            {
                itemScanTimer = 0f;
                if (cachedItems.Count == 0)
                {
                    RefreshItemsList();
                }
            }

            // 4. Giảm thời gian toast
            if (notificationTimer > 0f)
            {
                notificationTimer -= Time.deltaTime;
            }
        }

        private void ApplyCheats()
        {
            var controller = PlayerController.instance;
            if (controller == null) return;

            if (originalMoveSpeed < 0f && controller.MoveSpeed > 0f)
            {
                originalMoveSpeed = controller.MoveSpeed;
                originalSprintSpeed = controller.SprintSpeed;
            }

            if (EnableSpeedHack)
            {
                FieldMoveMultiplier?.SetValue(controller, SpeedMultiplier);
                if (originalSprintSpeed > 0f) controller.SprintSpeed = originalSprintSpeed * SpeedMultiplier;
            }
            else
            {
                FieldMoveMultiplier?.SetValue(controller, 1f);
                if (originalSprintSpeed > 0f) controller.SprintSpeed = originalSprintSpeed;
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

            PlayerHealth health = GetLocalPlayerHealth(controller);
            if (health != null)
            {
                if (EnableGodMode)
                {
                    FieldGodMode?.SetValue(health, true);
                    FieldInvincibleTimer?.SetValue(health, 999f);
                    int maxHp = GetMaxHealth(health);
                    if (maxHp > 0) FieldHealth?.SetValue(health, maxHp);
                }
                else
                {
                    FieldGodMode?.SetValue(health, false);
                }
            }

            ApplyMapLighting();
        }

        private void ApplyMapLighting()
        {
            if (EnableFullbright)
            {
                if (!origLightingSaved)
                {
                    origAmbientLight = RenderSettings.ambientLight;
                    origAmbientMode = RenderSettings.ambientMode;
                    origAmbientIntensity = RenderSettings.ambientIntensity;
                    origFog = RenderSettings.fog;
                    origFogDensity = RenderSettings.fogDensity;
                    origLightingSaved = true;
                }

                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = Color.white;
                RenderSettings.ambientIntensity = FullbrightIntensity;

                if (DisableFog)
                {
                    RenderSettings.fog = false;
                    RenderSettings.fogDensity = 0f;
                }

                if (mapDirectionalLightObj == null)
                {
                    mapDirectionalLightObj = new GameObject("ModMapDirectionalLight");
                    mapDirectionalLight = mapDirectionalLightObj.AddComponent<Light>();
                    mapDirectionalLight.type = LightType.Directional;
                    mapDirectionalLight.color = Color.white;
                    mapDirectionalLightObj.transform.rotation = Quaternion.Euler(60f, -40f, 0f);
                    DontDestroyOnLoad(mapDirectionalLightObj);
                }

                if (mapDirectionalLight != null)
                {
                    mapDirectionalLight.intensity = FullbrightIntensity;
                    mapDirectionalLight.enabled = true;
                }
            }
            else
            {
                if (mapDirectionalLight != null) mapDirectionalLight.enabled = false;
                if (origLightingSaved)
                {
                    RenderSettings.ambientLight = origAmbientLight;
                    RenderSettings.ambientMode = origAmbientMode;
                    RenderSettings.ambientIntensity = origAmbientIntensity;
                    RenderSettings.fog = origFog;
                    RenderSettings.fogDensity = origFogDensity;
                }
            }
        }

        private void RefreshItemsList()
        {
            lock (cachedItems)
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
        }

        private GameObject GetPrefabFromItem(Item item)
        {
            if (item == null || item.prefab == null) return null;
            try
            {
                var prop = item.prefab.GetType().GetProperty("Prefab", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (prop != null) return prop.GetValue(item.prefab) as GameObject;
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
                ShowNotification($"Vật phẩm [{item.itemName}] không tìm thấy Prefab!");
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

            ShowNotification($"Balo đầy! Đã thả [{item.itemName}] ra trước mặt.");
        }

        private PlayerHealth GetLocalPlayerHealth(PlayerController controller)
        {
            if (controller.playerAvatarScript != null && controller.playerAvatarScript.playerHealth != null)
                return controller.playerAvatarScript.playerHealth;
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
            if (notificationTimer > 0f)
            {
                var notifStyle = new GUIStyle(GUI.skin.box);
                notifStyle.fontSize = 15;
                notifStyle.fontStyle = FontStyle.Bold;
                notifStyle.normal.textColor = Color.yellow;
                GUI.Box(new Rect(Screen.width / 2f - 200, 25, 400, 40), $"[TRAINER] {notificationText}", notifStyle);
            }
        }

        private void OnDestroy()
        {
            isHttpRunning = false;
            try { httpListener?.Stop(); } catch { }
            if (mapDirectionalLightObj != null) Destroy(mapDirectionalLightObj);
        }
    }
}
