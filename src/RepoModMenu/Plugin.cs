using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
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

        private static readonly FieldInfo FieldPositionPrevious = typeof(PlayerController).GetField("positionPrevious", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly FieldInfo FieldVelocityImpulse = typeof(PlayerController).GetField("VelocityImpulse", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly FieldInfo FieldVelocityRelativeNew = typeof(PlayerController).GetField("VelocityRelativeNew", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly FieldInfo FieldFallDistance = typeof(PlayerCollisionController).GetField("fallDistance", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly FieldInfo FieldFallLastY = typeof(PlayerCollisionController).GetField("fallLastY", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        // Cached Reflection Fields for Truck & ESP
        private static readonly FieldInfo FieldCsMaxCrystals = typeof(ChargingStation).GetField("maxCrystals", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly FieldInfo FieldCsEnergyPerCrystal = typeof(ChargingStation).GetField("energyPerCrystal", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly FieldInfo FieldCsChargeTotal = typeof(ChargingStation).GetField("chargeTotal", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly FieldInfo FieldCsChargeFloat = typeof(ChargingStation).GetField("chargeFloat", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly FieldInfo FieldCsChargeSegmentCurrent = typeof(ChargingStation).GetField("chargeSegmentCurrent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly FieldInfo FieldCsChargeSegments = typeof(ChargingStation).GetField("chargeSegments", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly FieldInfo FieldCsChargeScaleTarget = typeof(ChargingStation).GetField("chargeScaleTarget", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly MethodInfo MethodCsCrystalsItShouldHave = typeof(ChargingStation).GetMethod("CrystalsItShouldHave", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly MethodInfo MethodCsStartCharge = typeof(ChargingStation).GetMethod("StartCharge", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        private static readonly FieldInfo FieldEpEnemyName = typeof(EnemyParent).GetField("enemyName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly FieldInfo FieldEpSpawned = typeof(EnemyParent).GetField("Spawned", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly FieldInfo FieldEpEnemy = typeof(EnemyParent).GetField("Enemy", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly FieldInfo FieldEnemyCenterTransform = typeof(Enemy).GetField("CenterTransform", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly FieldInfo FieldEnemyKillLookAtTransform = typeof(Enemy).GetField("KillLookAtTransform", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly FieldInfo FieldEnemyHealth = typeof(Enemy).GetField("Health", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        private static readonly FieldInfo FieldEhDead = typeof(EnemyHealth).GetField("dead", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly FieldInfo FieldEhHealthCurrent = typeof(EnemyHealth).GetField("healthCurrent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        private static readonly FieldInfo FieldVoDollarValueCurrent = typeof(ValuableObject).GetField("dollarValueCurrent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly FieldInfo FieldVoRb = typeof(ValuableObject).GetField("rb", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly FieldInfo FieldPgGrabbedPhysGrabObject = typeof(PhysGrabber).GetField("grabbedPhysGrabObject", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        private static readonly FieldInfo FieldPaPlayerName = typeof(PlayerAvatar).GetField("playerName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly FieldInfo FieldPaIsLocal = typeof(PlayerAvatar).GetField("isLocal", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

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

        // ESP X-Ray
        public class EspEntry
        {
            public Transform Transform;
            public string Name;
            public string ExtraInfo;
            public int Type; // 0: Enemy, 1: Valuable, 2: Teammate
        }

        public static bool EnableEsp = false;
        private float espScanTimer = 0f;
        private readonly List<EspEntry> cachedEspEntries = new List<EspEntry>();
        private GUIStyle espEnemyStyle;
        private GUIStyle espValuableStyle;
        private GUIStyle espPlayerStyle;
        private GUIStyle espBackgroundBoxStyle;
        private Texture2D espBoxTex;

        // Noclip / Bay Xuyên Tường
        public static bool EnableNoclip = false;
        private readonly List<Collider> disabledNoclipColliders = new List<Collider>();
        private float originalCustomGravity = 30f;
        private bool customGravitySaved = false;

        // Debounce Timers (chống double-toggle khi dùng phím tắt)
        private static float lastNoclipTime = 0f;
        private static float lastSpeedTime = 0f;
        private static float lastJumpTime = 0f;
        private static float lastStaminaTime = 0f;
        private static float lastTumbleTime = 0f;
        private static float lastGodTime = 0f;
        private static float lastFullbrightTime = 0f;
        private static float lastEspTime = 0f;
        private static float lastHealTime = 0f;
        private static float lastReviveTime = 0f;
        private static float lastMaxUpgradesTime = 0f;
        private static float lastTruckEnergyTime = 0f;
        private static float lastRechargeTruckTime = 0f;
        private static float lastAimbotTime = 0f;

        // Aimbot (Hỗ trợ Súng + Cận chiến + Lựu đạn/Ném)
        public static bool EnableAimbot = false;
        public static bool AimbotAlwaysLock = false;
        public static float AimbotFov = 120f;
        public static float AimbotMaxDistance = 50f;
        public static float AimbotSmoothing = 35f;

        private static bool isTargetLocked = false;
        private static Transform currentLockedTarget = null;
        private static string currentLockedName = "";
        private static float currentLockedDistance = 0f;
        private static Vector3 currentLockedScreenPos = Vector3.zero;

        // Truck Energy
        public static bool EnableInfiniteTruckEnergy = false;

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

        // Safe Cached States (Main Thread -> Background Thread)
        private static volatile bool cachedInGame = false;
        private static volatile int cachedHealth = 100;
        private static volatile int cachedMaxHealth = 100;
        private static volatile string cachedItemsJson = "[]";

        // IPC TCP Server (Pure WinSock socket, decoupled from scene GameObject)
        private static TcpListener tcpListener;
        private static Thread tcpListenerThread;
        private static volatile bool isIpcRunning = false;
        private static readonly ConcurrentQueue<Action> mainThreadActions = new ConcurrentQueue<Action>();

        private void Awake()
        {
            DontDestroyOnLoad(this.gameObject);
            this.gameObject.hideFlags = HideFlags.HideAndDontSave;
            Logger.LogInfo($"{ModName} v{ModVersion} initializing persistent IPC TCP server...");
            if (!isIpcRunning)
            {
                StartTcpServer();
            }
        }

        private void StartTcpServer()
        {
            try
            {
                tcpListener = new TcpListener(IPAddress.Loopback, 29999);
                tcpListener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                tcpListener.Start();
                isIpcRunning = true;

                tcpListenerThread = new Thread(TcpServerLoop)
                {
                    IsBackground = true
                };
                tcpListenerThread.Start();
                Logger.LogInfo("IPC TCP Server successfully listening on 127.0.0.1:29999");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to start IPC TCP Server: {ex.Message}");
            }
        }

        private void TcpServerLoop()
        {
            Logger.LogInfo("TcpServerLoop entered and listening on port 29999...");
            while (isIpcRunning && tcpListener != null)
            {
                try
                {
                    var client = tcpListener.AcceptTcpClient();
                    Logger.LogInfo("New IPC client connected!");
                    ThreadPool.QueueUserWorkItem((state) => HandleTcpClient(client));
                }
                catch (SocketException ex)
                {
                    Logger.LogWarning($"TcpServerLoop SocketException: {ex.Message}");
                    if (!isIpcRunning) break;
                    Thread.Sleep(300);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"TCP accept error: {ex.Message}");
                    if (!isIpcRunning) break;
                    Thread.Sleep(300);
                }
            }
            Logger.LogWarning("TcpServerLoop has exited!");
        }

        private void HandleTcpClient(TcpClient client)
        {
            using (client)
            {
                client.ReceiveTimeout = 4000;
                client.SendTimeout = 4000;
                try
                {
                    using (var stream = client.GetStream())
                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                    using (var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
                    {
                        string line = reader.ReadLine();
                        if (string.IsNullOrEmpty(line)) return;

                        bool isHttp = line.StartsWith("GET ", StringComparison.OrdinalIgnoreCase);
                        string responseBody = "";

                        if (isHttp)
                        {
                            // Drain remaining HTTP headers
                            string h;
                            while (!string.IsNullOrEmpty(h = reader.ReadLine())) { }

                            string[] parts = line.Split(' ');
                            string rawUrl = parts.Length > 1 ? parts[1] : "/";
                            responseBody = ProcessIpcRequest(rawUrl, true);

                            byte[] bodyBytes = Encoding.UTF8.GetBytes(responseBody);
                            string httpResp = $"HTTP/1.1 200 OK\r\n" +
                                              $"Content-Type: application/json; charset=utf-8\r\n" +
                                              $"Access-Control-Allow-Origin: *\r\n" +
                                              $"Connection: close\r\n" +
                                              $"Content-Length: {bodyBytes.Length}\r\n\r\n" +
                                              responseBody;
                            writer.Write(httpResp);
                            writer.Flush();
                        }
                        else
                        {
                            // Raw TCP command: "STATUS", "CMD ...", "ITEMS"
                            responseBody = ProcessIpcRequest(line, false);
                            writer.WriteLine(responseBody);
                            writer.Flush();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"HandleTcpClient error: {ex.Message}");
                }
            }
        }

        private string ProcessIpcRequest(string req, bool isHttp)
        {
            string cmd = "";
            string val = "";

            if (isHttp)
            {
                string path = req.Split('?')[0].ToLower();
                if (path == "/api/status") return GetStatusJson();
                if (path == "/api/items") return cachedItemsJson;
                if (path == "/api/cmd")
                {
                    cmd = GetQueryParam(req, "cmd");
                    val = GetQueryParam(req, "val");
                }
                else return "{\"error\":\"not_found\"}";
            }
            else
            {
                string trimmed = req.Trim();
                if (trimmed.Equals("STATUS", StringComparison.OrdinalIgnoreCase)) return GetStatusJson();
                if (trimmed.Equals("ITEMS", StringComparison.OrdinalIgnoreCase)) return cachedItemsJson;
                if (trimmed.StartsWith("CMD ", StringComparison.OrdinalIgnoreCase))
                {
                    string rest = trimmed.Substring(4).Trim();
                    int spaceIdx = rest.IndexOf(' ');
                    if (spaceIdx > 0)
                    {
                        cmd = rest.Substring(0, spaceIdx).Trim();
                        val = rest.Substring(spaceIdx + 1).Trim();
                    }
                    else
                    {
                        cmd = rest;
                    }
                }
                else return "{\"error\":\"unknown_command\"}";
            }

            if (!string.IsNullOrEmpty(cmd))
            {
                mainThreadActions.Enqueue(() =>
                {
                    ExecuteCommand(cmd, val);
                });
                return "{\"ok\":true}";
            }

            return "{\"ok\":false}";
        }

        private string GetQueryParam(string url, string key)
        {
            int qIdx = url.IndexOf('?');
            if (qIdx < 0) return "";
            string query = url.Substring(qIdx + 1);
            string[] pairs = query.Split('&');
            foreach (var pair in pairs)
            {
                string[] kv = pair.Split('=');
                if (kv.Length >= 2 && kv[0].Equals(key, StringComparison.OrdinalIgnoreCase))
                {
                    return Uri.UnescapeDataString(kv[1]);
                }
            }
            return "";
        }

        private string GetStatusJson()
        {
            return $"{{\"inGame\":{cachedInGame.ToString().ToLower()}," +
                   $"\"health\":{cachedHealth}," +
                   $"\"maxHealth\":{cachedMaxHealth}," +
                   $"\"noclip\":{EnableNoclip.ToString().ToLower()}," +
                   $"\"speed\":{EnableSpeedHack.ToString().ToLower()}," +
                   $"\"speedMultiplier\":{SpeedMultiplier.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)}," +
                   $"\"jump\":{EnableInfiniteJump.ToString().ToLower()}," +
                   $"\"stamina\":{EnableInfiniteStamina.ToString().ToLower()}," +
                   $"\"god\":{EnableGodMode.ToString().ToLower()}," +
                   $"\"tumble\":{EnableNoTumble.ToString().ToLower()}," +
                   $"\"fullbright\":{EnableFullbright.ToString().ToLower()}," +
                   $"\"esp\":{EnableEsp.ToString().ToLower()}," +
                   $"\"aimbot\":{EnableAimbot.ToString().ToLower()}," +
                   $"\"truckEnergy\":{EnableInfiniteTruckEnergy.ToString().ToLower()}," +
                   $"\"truckenergy\":{EnableInfiniteTruckEnergy.ToString().ToLower()}}}";
        }

        private void ToggleSpeed()
        {
            if (Time.unscaledTime - lastSpeedTime < 0.35f) return;
            lastSpeedTime = Time.unscaledTime;
            EnableSpeedHack = !EnableSpeedHack;
            ShowNotification($"Speed Hack: {(EnableSpeedHack ? "ON" : "OFF")}");
        }

        private void ToggleJump()
        {
            if (Time.unscaledTime - lastJumpTime < 0.35f) return;
            lastJumpTime = Time.unscaledTime;
            EnableInfiniteJump = !EnableInfiniteJump;
            ShowNotification($"Infinite Jump: {(EnableInfiniteJump ? "ON" : "OFF")}");
        }

        private void ToggleStamina()
        {
            if (Time.unscaledTime - lastStaminaTime < 0.35f) return;
            lastStaminaTime = Time.unscaledTime;
            EnableInfiniteStamina = !EnableInfiniteStamina;
            ShowNotification($"Infinite Stamina: {(EnableInfiniteStamina ? "ON" : "OFF")}");
        }

        private void ToggleGod()
        {
            if (Time.unscaledTime - lastGodTime < 0.35f) return;
            lastGodTime = Time.unscaledTime;
            EnableGodMode = !EnableGodMode;
            ShowNotification($"God Mode: {(EnableGodMode ? "ON" : "OFF")}");
        }

        private void ToggleTumble()
        {
            if (Time.unscaledTime - lastTumbleTime < 0.35f) return;
            lastTumbleTime = Time.unscaledTime;
            EnableNoTumble = !EnableNoTumble;
            ShowNotification($"Anti-Tumble: {(EnableNoTumble ? "ON" : "OFF")}");
        }

        private void ToggleFullbright()
        {
            if (Time.unscaledTime - lastFullbrightTime < 0.35f) return;
            lastFullbrightTime = Time.unscaledTime;
            EnableFullbright = !EnableFullbright;
            ShowNotification($"Fullbright: {(EnableFullbright ? "ON" : "OFF")}");
        }

        private void ToggleEsp()
        {
            if (Time.unscaledTime - lastEspTime < 0.35f) return;
            lastEspTime = Time.unscaledTime;
            EnableEsp = !EnableEsp;
            ShowNotification($"ESP Nhìn Xuyên Tường: {(EnableEsp ? "BẬT" : "TẮT")}");
        }

        private void ToggleTruckEnergy()
        {
            if (Time.unscaledTime - lastTruckEnergyTime < 0.35f) return;
            lastTruckEnergyTime = Time.unscaledTime;
            EnableInfiniteTruckEnergy = !EnableInfiniteTruckEnergy;
            ShowNotification($"Vô hạn Pin Xe Truck: {(EnableInfiniteTruckEnergy ? "BẬT" : "TẮT")}");
        }

        private void ToggleAimbot()
        {
            if (Time.unscaledTime - lastAimbotTime < 0.35f) return;
            lastAimbotTime = Time.unscaledTime;
            EnableAimbot = !EnableAimbot;
            isTargetLocked = false;
            ShowNotification(EnableAimbot ? "🎯 AIMBOT: BẬT (Giữ Chuột Phải / V để khóa mục tiêu)" : "🎯 AIMBOT: TẮT");
        }

        private void TriggerHeal()
        {
            if (Time.unscaledTime - lastHealTime < 0.35f) return;
            lastHealTime = Time.unscaledTime;
            if (PlayerController.instance != null)
            {
                var h = GetLocalPlayerHealth(PlayerController.instance);
                if (h != null)
                {
                    h.Heal(100);
                    ShowNotification("Đã hồi phục 100% Máu!");
                }
            }
        }

        private void TriggerRevive()
        {
            if (Time.unscaledTime - lastReviveTime < 0.35f) return;
            lastReviveTime = Time.unscaledTime;
            if (PlayerController.instance != null)
            {
                PlayerController.instance.Revive(PlayerController.instance.transform.eulerAngles);
                ShowNotification("Đã gọi hàm Revive!");
            }
        }

        private void TriggerMaxUpgrades()
        {
            if (Time.unscaledTime - lastMaxUpgradesTime < 0.35f) return;
            lastMaxUpgradesTime = Time.unscaledTime;
            ApplyMaxUpgrades();
        }

        private void ExecuteCommand(string cmd, string val)
        {
            switch (cmd.ToLower())
            {
                case "toggle_noclip":
                    ToggleNoclip();
                    break;
                case "toggle_speed":
                    ToggleSpeed();
                    break;
                case "set_speed":
                    if (float.TryParse(val, out float spd)) SpeedMultiplier = Mathf.Clamp(spd, 1f, 10f);
                    break;
                case "toggle_jump":
                    ToggleJump();
                    break;
                case "toggle_stamina":
                    ToggleStamina();
                    break;
                case "toggle_god":
                    ToggleGod();
                    break;
                case "toggle_tumble":
                    ToggleTumble();
                    break;
                case "toggle_fullbright":
                    ToggleFullbright();
                    break;
                case "toggle_esp":
                    ToggleEsp();
                    break;
                case "toggle_aimbot":
                    ToggleAimbot();
                    break;
                case "toggle_truck_energy":
                    ToggleTruckEnergy();
                    break;
                case "charge_truck":
                    RechargeTruck();
                    break;
                case "heal":
                    TriggerHeal();
                    break;
                case "revive":
                    TriggerRevive();
                    break;
                case "max_upgrades":
                    TriggerMaxUpgrades();
                    break;
                case "give_item":
                    GiveItemByName(val, true);
                    break;
                case "give_item_ground":
                    GiveItemByName(val, false);
                    break;
            }
        }

        private void RechargeTruck()
        {
            if (Time.unscaledTime - lastRechargeTruckTime < 0.35f) return;
            lastRechargeTruckTime = Time.unscaledTime;

            if (ChargingStation.instance != null)
            {
                var cs = ChargingStation.instance;
                int max = FieldCsMaxCrystals != null ? (int)FieldCsMaxCrystals.GetValue(cs) : 4;
                int per = FieldCsEnergyPerCrystal != null ? (int)FieldCsEnergyPerCrystal.GetValue(cs) : 100;
                if (max <= 0) max = 4;
                if (per <= 0) per = 100;
                int total = max * per;
                int segs = FieldCsChargeSegments != null ? (int)FieldCsChargeSegments.GetValue(cs) : 4;

                FieldCsChargeTotal?.SetValue(cs, total);
                FieldCsChargeFloat?.SetValue(cs, (float)total);
                FieldCsChargeSegmentCurrent?.SetValue(cs, segs);
                FieldCsChargeScaleTarget?.SetValue(cs, 1f);

                try { MethodCsCrystalsItShouldHave?.Invoke(cs, null); } catch { }
                try { MethodCsStartCharge?.Invoke(cs, null); } catch { }
                ShowNotification("⚡ Đã sạc ĐẦY 100% Pin cho Xe Truck!");
            }
            else
            {
                ShowNotification("Chưa tìm thấy Trạm Sạc / Xe Truck trong map!");
            }
        }

        private string CleanItemKey(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace(" ", "").Replace("-", "").Replace("_", "").ToLowerInvariant();
        }

        private void GiveItemByName(string name, bool toBalo)
        {
            if (string.IsNullOrEmpty(name)) return;
            if (cachedItems.Count == 0)
            {
                RefreshItemsList();
            }

            string cleanSearch = CleanItemKey(name);
            Item found = null;

            lock (cachedItems)
            {
                // 1. Tìm chính xác itemName hoặc name
                found = cachedItems.Find(x => 
                    (x.itemName != null && x.itemName.Equals(name, StringComparison.OrdinalIgnoreCase)) ||
                    (x.name != null && x.name.Equals(name, StringComparison.OrdinalIgnoreCase)));

                // 2. Tìm không phân biệt khoảng trắng/ký tự đặc biệt
                if (found == null)
                {
                    found = cachedItems.Find(x =>
                        CleanItemKey(x.itemName).Equals(cleanSearch) ||
                        CleanItemKey(x.name).Equals(cleanSearch));
                }

                // 3. Tìm dạng Contains (từ khóa con)
                if (found == null)
                {
                    found = cachedItems.Find(x =>
                        (!string.IsNullOrEmpty(x.itemName) && CleanItemKey(x.itemName).Contains(cleanSearch)) ||
                        (!string.IsNullOrEmpty(x.name) && CleanItemKey(x.name).Contains(cleanSearch)));
                }

                // 4. Nếu từ khóa bắt đầu hoặc không bắt đầu bằng "item", thử gỡ/thêm "item"
                if (found == null)
                {
                    string noItem = cleanSearch.StartsWith("item") ? cleanSearch.Substring(4) : cleanSearch;
                    string withItem = cleanSearch.StartsWith("item") ? cleanSearch : "item" + cleanSearch;
                    found = cachedItems.Find(x =>
                        CleanItemKey(x.itemName).Equals(noItem) ||
                        CleanItemKey(x.name).Equals(noItem) ||
                        CleanItemKey(x.itemName).Equals(withItem) ||
                        CleanItemKey(x.name).Equals(withItem));
                }
            }

            // 5. Nếu vẫn chưa thấy, tìm trực tiếp trong StatsManager.instance.itemDictionary
            if (found == null && StatsManager.instance != null && StatsManager.instance.itemDictionary != null)
            {
                foreach (var kvp in StatsManager.instance.itemDictionary)
                {
                    var it = kvp.Value;
                    if (it == null) continue;
                    if (CleanItemKey(kvp.Key).Contains(cleanSearch) ||
                        CleanItemKey(it.name).Contains(cleanSearch) ||
                        CleanItemKey(it.itemName).Contains(cleanSearch))
                    {
                        found = it;
                        break;
                    }
                }
            }

            // 6. Nếu vẫn chưa thấy, thử tải trực tiếp Resources.Load
            if (found == null)
            {
                try
                {
                    found = Resources.Load<Item>($"Items/{name}") 
                         ?? Resources.Load<Item>(name) 
                         ?? Resources.Load<Item>($"Items/Item {name}") 
                         ?? Resources.Load<Item>($"Item {name}");
                }
                catch { }
            }

            if (found != null)
            {
                string disp = !string.IsNullOrEmpty(found.itemName) ? found.itemName : found.name;
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
                FieldTumbleWings?.SetValue(controller.playerAvatarScript, 1f);
                FieldTumbleClimb?.SetValue(controller.playerAvatarScript, 1f);
                FieldCrouchRest?.SetValue(controller.playerAvatarScript, 1f);
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
                try { action?.Invoke(); } catch (Exception ex) { Logger.LogError($"Action error: {ex.Message}"); }
            }

            // 2. Cập nhật cache trạng thái Player an toàn cho IPC
            var localPlayer = PlayerController.instance;
            cachedInGame = localPlayer != null;
            if (cachedInGame)
            {
                var h = GetLocalPlayerHealth(localPlayer);
                if (h != null)
                {
                    cachedHealth = GetCurrentHealth(h);
                    cachedMaxHealth = GetMaxHealth(h);
                }
            }
            else
            {
                cachedHealth = 100;
                cachedMaxHealth = 100;
            }

            // 3. Thực thi logic cheat theo từng frame
            ApplyCheats();

            // 4. Quét ESP định kỳ mỗi 0.4s
            if (EnableEsp)
            {
                espScanTimer += Time.deltaTime;
                if (espScanTimer >= 0.4f)
                {
                    espScanTimer = 0f;
                    UpdateEspTargets();
                }
            }

            // 5. Phím tắt F1 - F12 trực tiếp trong game (hỗ trợ cả Legacy Input và New Input System)
            if (IsKeyHit(KeyCode.F1, Key.F1)) ToggleNoclip();
            if (IsKeyHit(KeyCode.F2, Key.F2)) ToggleSpeed();
            if (IsKeyHit(KeyCode.F3, Key.F3)) ToggleJump();
            if (IsKeyHit(KeyCode.F4, Key.F4)) ToggleStamina();
            if (IsKeyHit(KeyCode.F5, Key.F5)) ToggleTumble();
            if (IsKeyHit(KeyCode.F6, Key.F6)) ToggleGod();
            if (IsKeyHit(KeyCode.F7, Key.F7)) ToggleFullbright();
            if (IsKeyHit(KeyCode.F8, Key.F8)) ToggleEsp();
            if (IsKeyHit(KeyCode.F9, Key.F9)) TriggerHeal();
            if (IsKeyHit(KeyCode.F10, Key.F10)) TriggerRevive();
            if (IsKeyHit(KeyCode.F11, Key.F11)) TriggerMaxUpgrades();
            if (IsKeyHit(KeyCode.F12, Key.F12)) RechargeTruck();
            if (IsKeyHit(KeyCode.Z, Key.Z)) ToggleAimbot();

            // 6. Quét danh sách item
            itemScanTimer += Time.deltaTime;
            if (itemScanTimer > 2.0f)
            {
                itemScanTimer = 0f;
                if (cachedItems.Count == 0)
                {
                    RefreshItemsList();
                }
            }

            // 7. Giảm thời gian toast
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

            if (EnableInfiniteTruckEnergy && ChargingStation.instance != null)
            {
                var cs = ChargingStation.instance;
                int max = FieldCsMaxCrystals != null ? (int)FieldCsMaxCrystals.GetValue(cs) : 4;
                int per = FieldCsEnergyPerCrystal != null ? (int)FieldCsEnergyPerCrystal.GetValue(cs) : 100;
                if (max <= 0) max = 4;
                if (per <= 0) per = 100;
                int total = max * per;
                int segs = FieldCsChargeSegments != null ? (int)FieldCsChargeSegments.GetValue(cs) : 4;

                FieldCsChargeTotal?.SetValue(cs, total);
                FieldCsChargeFloat?.SetValue(cs, (float)total);
                FieldCsChargeSegmentCurrent?.SetValue(cs, segs);
                FieldCsChargeScaleTarget?.SetValue(cs, 1f);
            }

            ApplyNoclip();
            ApplyMapLighting();
        }

        private static bool IsKeyHit(KeyCode legacyKey, Key newKey)
        {
            try
            {
                if (UnityEngine.Input.GetKeyDown(legacyKey)) return true;
            }
            catch { }

            try
            {
                if (Keyboard.current != null && Keyboard.current[newKey].wasPressedThisFrame) return true;
            }
            catch { }

            return false;
        }

        private static bool IsKeyHeld(KeyCode legacyKey, Key newKey)
        {
            try
            {
                if (UnityEngine.Input.GetKey(legacyKey)) return true;
            }
            catch { }

            try
            {
                if (Keyboard.current != null && Keyboard.current[newKey].isPressed) return true;
            }
            catch { }

            return false;
        }

        private void ToggleNoclip()
        {
            if (Time.unscaledTime - lastNoclipTime < 0.35f) return;
            lastNoclipTime = Time.unscaledTime;

            EnableNoclip = !EnableNoclip;
            var controller = PlayerController.instance;
            if (controller != null)
            {
                if (EnableNoclip)
                {
                    if (!customGravitySaved && controller.CustomGravity > 0f)
                    {
                        originalCustomGravity = controller.CustomGravity;
                        customGravitySaved = true;
                    }
                    controller.CustomGravity = 0f;
                    controller.AntiGravity(1f);

                    if (controller.rb != null)
                    {
                        controller.rb.isKinematic = true;
                        controller.rb.detectCollisions = false;
                        controller.rb.velocity = Vector3.zero;
                        controller.rb.angularVelocity = Vector3.zero;
                    }
                    controller.Velocity = Vector3.zero;
                    controller.VelocityRelative = Vector3.zero;
                    FieldVelocityRelativeNew?.SetValue(controller, Vector3.zero);
                    FieldVelocityImpulse?.SetValue(controller, Vector3.zero);

                    disabledNoclipColliders.Clear();
                    var colls = controller.GetComponentsInChildren<Collider>(true);
                    foreach (var c in colls)
                    {
                        if (c != null && c.enabled)
                        {
                            disabledNoclipColliders.Add(c);
                            c.enabled = false;
                        }
                    }

                    if (controller.playerAvatarScript != null)
                    {
                        var avRbs = controller.playerAvatarScript.GetComponentsInChildren<Rigidbody>(true);
                        foreach (var r in avRbs)
                        {
                            if (r != null)
                            {
                                r.isKinematic = true;
                                r.detectCollisions = false;
                                r.velocity = Vector3.zero;
                                r.angularVelocity = Vector3.zero;
                            }
                        }

                        var avatarColls = controller.playerAvatarScript.GetComponentsInChildren<Collider>(true);
                        foreach (var c in avatarColls)
                        {
                            if (c != null && c.enabled && !disabledNoclipColliders.Contains(c))
                            {
                                disabledNoclipColliders.Add(c);
                                c.enabled = false;
                            }
                        }
                    }

                    ShowNotification("Bay Xuyên Tường (Noclip): BẬT [F1] (WASD + Shift + Space/Ctrl)");
                }
                else
                {
                    if (customGravitySaved)
                    {
                        controller.CustomGravity = originalCustomGravity;
                    }

                    if (controller.rb != null)
                    {
                        controller.rb.isKinematic = false;
                        controller.rb.detectCollisions = true;
                        controller.rb.velocity = Vector3.zero;
                        controller.rb.angularVelocity = Vector3.zero;
                    }
                    controller.Velocity = Vector3.zero;
                    FieldPositionPrevious?.SetValue(controller, controller.transform.position);

                    foreach (var c in disabledNoclipColliders)
                    {
                        if (c != null) c.enabled = true;
                    }
                    disabledNoclipColliders.Clear();

                    if (controller.playerAvatarScript != null)
                    {
                        var avRbs = controller.playerAvatarScript.GetComponentsInChildren<Rigidbody>(true);
                        foreach (var r in avRbs)
                        {
                            if (r != null)
                            {
                                r.isKinematic = false;
                                r.detectCollisions = true;
                                r.velocity = Vector3.zero;
                                r.angularVelocity = Vector3.zero;
                            }
                        }
                    }

                    if (controller.CollisionController != null)
                    {
                        FieldFallDistance?.SetValue(controller.CollisionController, 0f);
                        FieldFallLastY?.SetValue(controller.CollisionController, controller.transform.position.y);
                        controller.CollisionController.Grounded = true;
                    }

                    ShowNotification("Bay Xuyên Tường (Noclip): TẮT [F1]");
                }
            }
            else
            {
                ShowNotification($"Bay Xuyên Tường (Noclip): {(EnableNoclip ? "BẬT" : "TẮT")}");
            }
        }

        private void ApplyNoclip()
        {
            var controller = PlayerController.instance;
            if (controller == null) return;

            if (EnableNoclip)
            {
                controller.CustomGravity = 0f;
                controller.AntiGravity(1f);

                if (controller.rb != null)
                {
                    controller.rb.isKinematic = true;
                    controller.rb.detectCollisions = false;
                    controller.rb.velocity = Vector3.zero;
                    controller.rb.angularVelocity = Vector3.zero;
                }
                controller.Velocity = Vector3.zero;
                controller.VelocityRelative = Vector3.zero;
                FieldVelocityImpulse?.SetValue(controller, Vector3.zero);
                FieldVelocityRelativeNew?.SetValue(controller, Vector3.zero);

                Camera cam = Camera.main;
                if (cam == null) cam = Camera.current;
                if (cam == null) cam = controller.GetComponentInChildren<Camera>();
                Transform camT = cam != null ? cam.transform : controller.transform;

                Vector3 moveDir = Vector3.zero;
                float speed = 14f;

                if (IsKeyHeld(KeyCode.LeftShift, Key.LeftShift) || IsKeyHeld(KeyCode.RightShift, Key.RightShift))
                {
                    speed *= 2.5f;
                }

                if (IsKeyHeld(KeyCode.W, Key.W) || IsKeyHeld(KeyCode.UpArrow, Key.UpArrow))
                    moveDir += camT.forward;
                if (IsKeyHeld(KeyCode.S, Key.S) || IsKeyHeld(KeyCode.DownArrow, Key.DownArrow))
                    moveDir -= camT.forward;
                if (IsKeyHeld(KeyCode.D, Key.D) || IsKeyHeld(KeyCode.RightArrow, Key.RightArrow))
                    moveDir += camT.right;
                if (IsKeyHeld(KeyCode.A, Key.A) || IsKeyHeld(KeyCode.LeftArrow, Key.LeftArrow))
                    moveDir -= camT.right;

                if (IsKeyHeld(KeyCode.Space, Key.Space) || IsKeyHeld(KeyCode.E, Key.E))
                    moveDir += Vector3.up;
                if (IsKeyHeld(KeyCode.LeftControl, Key.LeftCtrl) || IsKeyHeld(KeyCode.C, Key.C) || IsKeyHeld(KeyCode.Q, Key.Q))
                    moveDir -= Vector3.up;

                if (moveDir.sqrMagnitude > 0.001f)
                {
                    moveDir.Normalize();
                    Vector3 delta = moveDir * (speed * Time.deltaTime);
                    Vector3 targetPos = controller.transform.position + delta;
                    controller.transform.position = targetPos;
                    if (controller.rb != null) controller.rb.position = targetPos;
                    FieldPositionPrevious?.SetValue(controller, targetPos);

                    if (controller.playerAvatarScript != null)
                    {
                        controller.playerAvatarScript.transform.position = targetPos;
                        var avRbs = controller.playerAvatarScript.GetComponentsInChildren<Rigidbody>(true);
                        foreach (var r in avRbs)
                        {
                            if (r != null) r.position = targetPos;
                        }
                    }
                }
            }
        }

        private void FixedUpdate()
        {
            if (EnableNoclip && PlayerController.instance != null)
            {
                var controller = PlayerController.instance;
                if (controller.rb != null)
                {
                    controller.rb.velocity = Vector3.zero;
                    controller.rb.angularVelocity = Vector3.zero;
                }
                controller.Velocity = Vector3.zero;
                controller.VelocityRelative = Vector3.zero;
                FieldVelocityImpulse?.SetValue(controller, Vector3.zero);
                FieldVelocityRelativeNew?.SetValue(controller, Vector3.zero);

                if (controller.playerAvatarScript != null)
                {
                    var avRbs = controller.playerAvatarScript.GetComponentsInChildren<Rigidbody>(true);
                    foreach (var r in avRbs)
                    {
                        if (r != null)
                        {
                            r.velocity = Vector3.zero;
                            r.angularVelocity = Vector3.zero;
                        }
                    }
                }
            }
        }

        private void LateUpdate()
        {
            if (EnableAimbot)
            {
                ApplyAimbot();
            }

            if (EnableNoclip && PlayerController.instance != null)
            {
                var controller = PlayerController.instance;
                FieldPositionPrevious?.SetValue(controller, controller.transform.position);

                if (controller.playerAvatarScript != null)
                {
                    controller.playerAvatarScript.transform.position = controller.transform.position;
                }

                if (controller.CollisionController != null)
                {
                    FieldFallDistance?.SetValue(controller.CollisionController, 0f);
                    FieldFallLastY?.SetValue(controller.CollisionController, controller.transform.position.y);
                    controller.CollisionController.Grounded = true;
                }
            }
        }

        #region Aimbot Implementation
        private enum HeldWeaponType
        {
            None,
            Gun,
            Melee,
            Grenade
        }

        private static bool IsAimTriggerHeld()
        {
            if (IsKeyHeld(KeyCode.V, Key.V) || IsKeyHeld(KeyCode.LeftAlt, Key.LeftAlt)) return true;

            try
            {
                if (Input.GetMouseButton(1)) return true;
            }
            catch { }

            try
            {
                if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.rightButton.isPressed) return true;
            }
            catch { }

            return false;
        }

        private static bool IsAttackTriggerHeld()
        {
            try
            {
                if (Input.GetMouseButton(0)) return true;
            }
            catch { }

            try
            {
                if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.isPressed) return true;
            }
            catch { }

            return false;
        }

        private HeldWeaponType GetCurrentHeldWeapon(out GameObject heldObject)
        {
            heldObject = null;
            try
            {
                var grabber = PhysGrabber.instance;
                PhysGrabObject pgo = null;
                if (grabber != null && FieldPgGrabbedPhysGrabObject != null)
                {
                    pgo = FieldPgGrabbedPhysGrabObject.GetValue(grabber) as PhysGrabObject;
                }
                if (pgo != null)
                {
                    heldObject = pgo.gameObject;
                    if (pgo.GetComponent<ItemGrenade>() != null || pgo.GetComponentInChildren<ItemGrenade>() != null)
                        return HeldWeaponType.Grenade;
                    if (pgo.GetComponent<ItemGun>() != null || pgo.GetComponentInChildren<ItemGun>() != null)
                        return HeldWeaponType.Gun;
                    if (pgo.GetComponent<ItemMelee>() != null || pgo.GetComponentInChildren<ItemMelee>() != null)
                        return HeldWeaponType.Melee;
                    return HeldWeaponType.Grenade; // Bất kỳ đồ vật cầm ném nào cũng áp dụng bù trừ góc ném
                }

                var controller = PlayerController.instance;
                if (controller != null)
                {
                    var gr = controller.GetComponentInChildren<ItemGrenade>();
                    if (gr != null) { heldObject = gr.gameObject; return HeldWeaponType.Grenade; }
                    var gn = controller.GetComponentInChildren<ItemGun>();
                    if (gn != null) { heldObject = gn.gameObject; return HeldWeaponType.Gun; }
                    var ml = controller.GetComponentInChildren<ItemMelee>();
                    if (ml != null) { heldObject = ml.gameObject; return HeldWeaponType.Melee; }
                }
            }
            catch { }

            return HeldWeaponType.None;
        }

        private Transform FindBestAimbotTarget(Vector3 camPos, Vector3 camForward, out Vector3 targetAimPos, out string targetName, out float targetDist, out bool isMeleeClose)
        {
            targetAimPos = Vector3.zero;
            targetName = "";
            targetDist = 9999f;
            isMeleeClose = false;

            Transform bestTarget = null;
            float bestScore = float.MaxValue;

            HeldWeaponType weaponType = GetCurrentHeldWeapon(out GameObject _);

            var enemies = FindObjectsOfType<EnemyParent>();
            if (enemies != null)
            {
                foreach (var ep in enemies)
                {
                    if (ep == null || ep.transform == null) continue;

                    if (FieldEpSpawned != null)
                    {
                        bool spawned = (bool)FieldEpSpawned.GetValue(ep);
                        if (!spawned) continue;
                    }

                    Enemy enemyObj = null;
                    if (FieldEpEnemy != null)
                    {
                        enemyObj = FieldEpEnemy.GetValue(ep) as Enemy;
                    }
                    if (enemyObj == null)
                    {
                        enemyObj = ep.GetComponentInChildren<Enemy>();
                    }

                    if (enemyObj != null)
                    {
                        EnemyHealth eh = null;
                        if (FieldEnemyHealth != null)
                        {
                            eh = FieldEnemyHealth.GetValue(enemyObj) as EnemyHealth;
                        }
                        if (eh == null)
                        {
                            eh = enemyObj.GetComponentInChildren<EnemyHealth>() ?? ep.GetComponentInChildren<EnemyHealth>();
                        }

                        if (eh != null && FieldEhDead != null)
                        {
                            bool dead = (bool)FieldEhDead.GetValue(eh);
                            if (dead) continue;
                            if (FieldEhHealthCurrent != null)
                            {
                                int hp = (int)FieldEhHealthCurrent.GetValue(eh);
                                if (hp <= 0) continue;
                            }
                        }
                    }

                    Transform headOrWeak = null;
                    Transform centerT = null;
                    if (enemyObj != null)
                    {
                        if (FieldEnemyKillLookAtTransform != null)
                            headOrWeak = FieldEnemyKillLookAtTransform.GetValue(enemyObj) as Transform;
                        if (FieldEnemyCenterTransform != null)
                            centerT = FieldEnemyCenterTransform.GetValue(enemyObj) as Transform;
                    }

                    Vector3 basePos = centerT != null ? centerT.position : ep.transform.position + Vector3.up * 0.7f;
                    Vector3 weakPos = headOrWeak != null ? headOrWeak.position : basePos + Vector3.up * 0.3f;

                    float dist = Vector3.Distance(camPos, basePos);
                    if (dist > AimbotMaxDistance) continue;

                    Vector3 toEnemy = (basePos - camPos).normalized;
                    float angle = Vector3.Angle(camForward, toEnemy);
                    if (angle > AimbotFov * 0.5f) continue;

                    float score = angle * 1.5f + dist * 0.6f;
                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestTarget = ep.transform;
                        targetDist = dist;

                        string ename = FieldEpEnemyName != null ? (string)FieldEpEnemyName.GetValue(ep) : "";
                        if (string.IsNullOrEmpty(ename)) ename = ep.gameObject.name.Replace("(Clone)", "");
                        targetName = ename;

                        if (weaponType == HeldWeaponType.Grenade)
                        {
                            // Lựu đạn / ném: bù góc bắn cầu vồng theo khoảng cách để rơi đúng vị trí
                            float arcCompensation = Mathf.Clamp(dist * 0.085f, 0.15f, 4.0f);
                            targetAimPos = basePos + Vector3.up * arcCompensation;
                        }
                        else if (weaponType == HeldWeaponType.Melee || dist <= 4.0f)
                        {
                            // Cận chiến: nhắm vào thân ngực
                            targetAimPos = basePos;
                            isMeleeClose = true;
                        }
                        else
                        {
                            // Súng tầm xa: nhắm vào đầu / điểm yếu
                            targetAimPos = weakPos;
                        }
                    }
                }
            }

            return bestTarget;
        }

        private void ApplyAimbot()
        {
            isTargetLocked = false;
            currentLockedTarget = null;
            currentLockedName = "";
            currentLockedDistance = 0f;

            if (!EnableAimbot) return;

            var controller = PlayerController.instance;
            if (controller == null) return;

            var camAim = CameraAim.Instance ?? controller.cameraAim;
            if (camAim == null) return;

            Vector3 camPos = camAim.transform.position;
            Vector3 camForward = camAim.transform.forward;

            bool isAimHeld = IsAimTriggerHeld();
            bool isAttackHeld = IsAttackTriggerHeld();

            Transform target = FindBestAimbotTarget(camPos, camForward, out Vector3 aimPos, out string tName, out float dist, out bool isMeleeClose);

            if (target != null)
            {
                bool shouldLock = AimbotAlwaysLock || isAimHeld || (isMeleeClose && isAttackHeld);

                if (shouldLock)
                {
                    Vector3 aimDir = (aimPos - camPos).normalized;
                    if (aimDir.sqrMagnitude > 0.001f)
                    {
                        Quaternion targetRot = Quaternion.LookRotation(aimDir, Vector3.up);
                        float smooth = Mathf.Clamp01(AimbotSmoothing * Time.deltaTime);
                        Quaternion nextRot = Quaternion.Slerp(camAim.transform.localRotation, targetRot, smooth);

                        camAim.SetPlayerAim(nextRot, true);

                        // Cận chiến: xoay thân nhân vật đối diện quái để 100% vung trúng đòn
                        if (isMeleeClose)
                        {
                            Vector3 flatDir = aimPos - controller.transform.position;
                            flatDir.y = 0f;
                            if (flatDir.sqrMagnitude > 0.01f)
                            {
                                controller.transform.rotation = Quaternion.Slerp(
                                    controller.transform.rotation,
                                    Quaternion.LookRotation(flatDir.normalized, Vector3.up),
                                    smooth);
                            }
                        }
                    }

                    isTargetLocked = true;
                    currentLockedTarget = target;
                    currentLockedName = tName;
                    currentLockedDistance = dist;
                    if (Camera.main != null)
                    {
                        currentLockedScreenPos = Camera.main.WorldToScreenPoint(aimPos);
                    }
                }
            }
        }
        #endregion

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
                var addedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var names = new List<string>();

                void AddItemCandidate(Item it)
                {
                    if (it == null) return;
                    string displayName = !string.IsNullOrEmpty(it.itemName) ? it.itemName : it.name;
                    if (string.IsNullOrEmpty(displayName)) return;

                    // Chuẩn hóa loại bỏ "Item " ở đầu nếu có
                    if (displayName.StartsWith("Item ", StringComparison.OrdinalIgnoreCase))
                    {
                        displayName = displayName.Substring(5).Trim();
                    }

                    // Lọc bỏ triệt để các asset nội bộ / volumes / bộ phận mesh
                    string nLower = displayName.ToLowerInvariant();
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
                        return;
                    }

                    // Chuẩn hóa từ đồng nghĩa
                    if (displayName.Equals("Melee Sledge Hammer", StringComparison.OrdinalIgnoreCase)) displayName = "Melee Sledgehammer";
                    else if (displayName.Equals("Leaf blower", StringComparison.OrdinalIgnoreCase)) displayName = "Leaf Blower";
                    else if (displayName.Equals("ReviveItem", StringComparison.OrdinalIgnoreCase)) displayName = "Revive Item";
                    else if (displayName.Equals("Staff Torque", StringComparison.OrdinalIgnoreCase)) displayName = "Torque Staff";
                    else if (displayName.Equals("Staff Void", StringComparison.OrdinalIgnoreCase)) displayName = "Void Staff";
                    else if (displayName.Equals("Staff Zero Gravity", StringComparison.OrdinalIgnoreCase)) displayName = "Zero Gravity Staff";
                    else if (displayName.Equals("WalkieTalkie", StringComparison.OrdinalIgnoreCase)) displayName = "Walkie Talkie";
                    else if (displayName.Equals("Stun Baton", StringComparison.OrdinalIgnoreCase) || displayName.Equals("Stun Baton Variant", StringComparison.OrdinalIgnoreCase)) displayName = "Melee Stun Baton";

                    string key = CleanItemKey(displayName);
                    if (!addedKeys.Contains(key))
                    {
                        addedKeys.Add(key);
                        cachedItems.Add(it);
                        names.Add(displayName);
                    }
                }

                // 1. Quét từ StatsManager.instance.itemDictionary
                try
                {
                    if (StatsManager.instance != null && StatsManager.instance.itemDictionary != null)
                    {
                        foreach (var kvp in StatsManager.instance.itemDictionary)
                        {
                            AddItemCandidate(kvp.Value);
                        }
                    }
                }
                catch { }

                // 2. Tải toàn bộ vật phẩm từ thư mục Resources/Items
                try
                {
                    var loaded = Resources.LoadAll<Item>("Items");
                    if (loaded != null)
                    {
                        foreach (var it in loaded)
                        {
                            AddItemCandidate(it);
                        }
                    }
                }
                catch { }

                // 3. Fallback tìm trong Object bộ nhớ
                try
                {
                    var inMem = Resources.FindObjectsOfTypeAll<Item>();
                    if (inMem != null)
                    {
                        foreach (var it in inMem)
                        {
                            AddItemCandidate(it);
                        }
                    }
                }
                catch { }

                cachedItems.Sort((a, b) =>
                {
                    string nameA = !string.IsNullOrEmpty(a.itemName) ? a.itemName : a.name;
                    string nameB = !string.IsNullOrEmpty(b.itemName) ? b.itemName : b.name;
                    return string.Compare(nameA, nameB, StringComparison.OrdinalIgnoreCase);
                });
                names.Sort(StringComparer.OrdinalIgnoreCase);
                cachedItemsJson = "[\"" + string.Join("\",\"", names.ToArray()) + "\"]";
                Logger.LogInfo($"[RepoModMenu] Đã làm mới danh mục: {cachedItems.Count} vật phẩm sẵn sàng.");
            }
        }

        private GameObject GetPrefabFromItem(Item item)
        {
            if (item == null) return null;
            if (item.prefab != null)
            {
                try
                {
                    if (item.prefab.Prefab != null) return item.prefab.Prefab;
                }
                catch { }
                try
                {
                    if (!string.IsNullOrEmpty(item.prefab.ResourcePath))
                    {
                        var loaded = Resources.Load<GameObject>(item.prefab.ResourcePath);
                        if (loaded != null) return loaded;
                    }
                }
                catch { }
                try
                {
                    if (!string.IsNullOrEmpty(item.prefab.PrefabName))
                    {
                        var loaded = Resources.Load<GameObject>(item.prefab.PrefabName);
                        if (loaded != null) return loaded;
                    }
                }
                catch { }
            }

            try
            {
                var loaded = Resources.Load<GameObject>($"Items/{item.name}") ?? Resources.Load<GameObject>(item.name);
                if (loaded != null) return loaded;
            }
            catch { }

            return null;
        }

        private void AddItemToPlayerInventory(Item item, bool forceSpawnOnGround = false)
        {
            if (item == null) return;
            string disp = !string.IsNullOrEmpty(item.itemName) ? item.itemName : item.name;
            GameObject prefabObj = GetPrefabFromItem(item);
            if (prefabObj == null)
            {
                ShowNotification($"Vật phẩm [{disp}] không tìm thấy Prefab!");
                return;
            }

            var controller = PlayerController.instance;
            if (controller == null)
            {
                ShowNotification("Chưa vào trận hoặc không tìm thấy Player!");
                return;
            }

            Vector3 spawnPos = controller.transform.position + controller.transform.forward * 1.2f + Vector3.up * 0.5f;
            GameObject spawnedObj = Instantiate(prefabObj, spawnPos, controller.transform.rotation);

            if (forceSpawnOnGround)
            {
                ShowNotification($"Đã thả [{disp}] ra trước mặt!");
                return;
            }

            var equippable = spawnedObj.GetComponentInChildren<ItemEquippable>();
            if (equippable == null)
            {
                ShowNotification($"[{disp}] là vật phẩm lớn/đặc biệt, đã thả ngay trước mặt bạn!");
                return;
            }

            var inv = Inventory.instance;
            if (inv != null)
            {
                int freeIndex = inv.GetFirstFreeInventorySpotIndex();
                if (freeIndex >= 0)
                {
                    var spot = inv.GetSpotByIndex(freeIndex);
                    if (spot != null)
                    {
                        spot.EquipItem(equippable);
                        ShowNotification($"Đã cất [{disp}] vào Balo (Ô {freeIndex + 1})!");
                        return;
                    }
                }
            }

            ShowNotification($"Balo đầy! Đã thả [{disp}] ra trước mặt.");
        }

        private void UpdateEspTargets()
        {
            if (!EnableEsp) return;

            var newEntries = new List<EspEntry>();

            try
            {
                // 1. Quét Quái vật (Enemies)
                var enemies = FindObjectsOfType<EnemyParent>();
                var addedEnemyTransforms = new HashSet<Transform>();
                if (enemies != null)
                {
                    foreach (var ep in enemies)
                    {
                        if (ep == null || ep.transform == null) continue;
                        bool spawned = FieldEpSpawned != null ? (bool)FieldEpSpawned.GetValue(ep) : true;
                        if (!spawned) continue;

                        string ename = FieldEpEnemyName != null ? (string)FieldEpEnemyName.GetValue(ep) : "";
                        if (string.IsNullOrEmpty(ename)) ename = ep.gameObject.name.Replace("(Clone)", "");

                        addedEnemyTransforms.Add(ep.transform);
                        newEntries.Add(new EspEntry
                        {
                            Transform = ep.transform,
                            Name = ename,
                            Type = 0
                        });
                    }
                }

                var eHealths = FindObjectsOfType<EnemyHealth>();
                if (eHealths != null)
                {
                    foreach (var eh in eHealths)
                    {
                        if (eh == null || eh.transform == null) continue;
                        bool dead = FieldEhDead != null ? (bool)FieldEhDead.GetValue(eh) : false;
                        if (dead) continue;
                        if (addedEnemyTransforms.Contains(eh.transform)) continue;

                        int hp = FieldEhHealthCurrent != null ? (int)FieldEhHealthCurrent.GetValue(eh) : 0;
                        newEntries.Add(new EspEntry
                        {
                            Transform = eh.transform,
                            Name = eh.gameObject.name.Replace("(Clone)", ""),
                            ExtraInfo = hp > 0 ? $"{hp} HP" : "",
                            Type = 0
                        });
                    }
                }
            }
            catch { }

            try
            {
                // 2. Quét Đồ giá trị (Valuables)
                var valuables = FindObjectsOfType<ValuableObject>();
                if (valuables != null)
                {
                    foreach (var val in valuables)
                    {
                        if (val == null || val.transform == null) continue;
                        float curVal = FieldVoDollarValueCurrent != null ? (float)FieldVoDollarValueCurrent.GetValue(val) : 0f;
                        int price = Mathf.RoundToInt(curVal);
                        float mass = 0f;
                        var vRb = val.GetComponent<Rigidbody>() ?? val.GetComponentInChildren<Rigidbody>();
                        if (vRb == null && FieldVoRb != null)
                        {
                            vRb = FieldVoRb.GetValue(val) as Rigidbody;
                        }
                        if (vRb != null) mass = vRb.mass;

                        string extra = "";
                        if (price > 0 && mass > 0f) extra = $"${price} | {mass:0.#}kg";
                        else if (price > 0) extra = $"${price}";
                        else if (mass > 0f) extra = $"{mass:0.#}kg";

                        newEntries.Add(new EspEntry
                        {
                            Transform = val.transform,
                            Name = "",
                            ExtraInfo = extra,
                            Type = 1
                        });
                    }
                }
            }
            catch { }

            try
            {
                // 3. Quét Đồng đội (Teammates)
                var players = FindObjectsOfType<PlayerAvatar>();
                if (players != null)
                {
                    foreach (var p in players)
                    {
                        if (p == null || p.transform == null) continue;
                        bool isLocal = FieldPaIsLocal != null ? (bool)FieldPaIsLocal.GetValue(p) : false;
                        if (isLocal) continue;

                        string pname = FieldPaPlayerName != null ? (string)FieldPaPlayerName.GetValue(p) : "";
                        if (string.IsNullOrEmpty(pname)) pname = p.gameObject.name;

                        newEntries.Add(new EspEntry
                        {
                            Transform = p.transform,
                            Name = pname,
                            Type = 2
                        });
                    }
                }
            }
            catch { }

            lock (cachedEspEntries)
            {
                cachedEspEntries.Clear();
                cachedEspEntries.AddRange(newEntries);
            }
        }

        private void DrawEsp()
        {
            if (!EnableEsp) return;

            Camera cam = Camera.main;
            if (cam == null) cam = Camera.current;
            if (cam == null) return;

            Vector3 camPos = cam.transform.position;

            if (espEnemyStyle == null)
            {
                espBoxTex = new Texture2D(1, 1);
                espBoxTex.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.70f));
                espBoxTex.Apply();

                espBackgroundBoxStyle = new GUIStyle();
                espBackgroundBoxStyle.normal.background = espBoxTex;

                // Quái vật: Chữ màu ĐỎ TƯƠI nổi bật (báo hiệu nguy hiểm), cỡ chữ 11 đậm
                espEnemyStyle = new GUIStyle();
                espEnemyStyle.fontSize = 11;
                espEnemyStyle.fontStyle = FontStyle.Bold;
                espEnemyStyle.normal.textColor = new Color(1f, 0.22f, 0.22f, 1f); // Màu đỏ tươi
                espEnemyStyle.alignment = TextAnchor.MiddleCenter;

                // Đồ đạc: Giá tiền + Cân nặng màu vàng, cỡ chữ 10 thu nhỏ
                espValuableStyle = new GUIStyle();
                espValuableStyle.fontSize = 10;
                espValuableStyle.fontStyle = FontStyle.Bold;
                espValuableStyle.normal.textColor = new Color(1f, 0.88f, 0.15f, 1f); // Màu vàng
                espValuableStyle.alignment = TextAnchor.MiddleCenter;

                // Đồng đội: Chữ màu trắng, cỡ chữ 10 thu nhỏ
                espPlayerStyle = new GUIStyle();
                espPlayerStyle.fontSize = 10;
                espPlayerStyle.fontStyle = FontStyle.Bold;
                espPlayerStyle.normal.textColor = Color.white;
                espPlayerStyle.alignment = TextAnchor.MiddleCenter;
            }

            List<EspEntry> entries;
            lock (cachedEspEntries)
            {
                entries = new List<EspEntry>(cachedEspEntries);
            }

            foreach (var entry in entries)
            {
                if (entry == null || entry.Transform == null) continue;

                Vector3 targetPos = entry.Transform.position;
                float dist = Vector3.Distance(camPos, targetPos);

                // Thu khoảng cách thấy lại để tránh rối mắt
                if (entry.Type == 0 && dist > 40f) continue;  // Quái: 40m
                if (entry.Type == 1 && dist > 28f) continue;  // Đồ đạc: 28m
                if (entry.Type == 2 && dist > 60f) continue;  // Đồng đội: 60m

                Vector3 screenPos = cam.WorldToScreenPoint(targetPos);
                if (screenPos.z <= 0.1f) continue; // Phía sau camera

                float screenY = Screen.height - screenPos.y;
                float screenX = screenPos.x;

                if (screenX < -60 || screenX > Screen.width + 60 || screenY < -60 || screenY > Screen.height + 60)
                    continue;

                string label;
                GUIStyle style;
                switch (entry.Type)
                {
                    case 0:
                        label = string.IsNullOrEmpty(entry.ExtraInfo)
                            ? $"[👹 {entry.Name}] ({dist:0}m)"
                            : $"[👹 {entry.Name} - {entry.ExtraInfo}] ({dist:0}m)";
                        style = espEnemyStyle;
                        break;
                    case 1:
                        // Đồ đạc CHỈ hiện giá tiền + độ nặng màu vàng
                        label = !string.IsNullOrEmpty(entry.ExtraInfo)
                            ? $"[{entry.ExtraInfo}] ({dist:0}m)"
                            : $"[💎] ({dist:0}m)";
                        style = espValuableStyle;
                        break;
                    case 2:
                        label = $"[🧑 {entry.Name}] ({dist:0}m)";
                        style = espPlayerStyle;
                        break;
                    default:
                        continue;
                }

                Vector2 size = style.CalcSize(new GUIContent(label));
                Rect boxRect = new Rect(screenX - (size.x / 2f) - 3, screenY - (size.y / 2f) - 1, size.x + 6, size.y + 2);

                GUI.Box(boxRect, GUIContent.none, espBackgroundBoxStyle);
                GUI.Label(boxRect, label, style);
            }
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
            if (EnableEsp)
            {
                DrawEsp();
            }

            if (EnableAimbot && isTargetLocked && currentLockedScreenPos.z > 0.1f)
            {
                DrawAimbotLockIndicator();
            }

            if (notificationTimer > 0f)
            {
                var notifStyle = new GUIStyle(GUI.skin.box);
                notifStyle.fontSize = 15;
                notifStyle.fontStyle = FontStyle.Bold;
                notifStyle.normal.textColor = Color.yellow;
                GUI.Box(new Rect(Screen.width / 2f - 200, 25, 400, 40), $"[TRAINER] {notificationText}", notifStyle);
            }
        }

        private void DrawAimbotLockIndicator()
        {
            float screenX = currentLockedScreenPos.x;
            float screenY = Screen.height - currentLockedScreenPos.y;

            if (screenX < 0 || screenX > Screen.width || screenY < 0 || screenY > Screen.height)
                return;

            if (espBoxTex == null)
            {
                espBoxTex = new Texture2D(1, 1);
                espBoxTex.SetPixel(0, 0, Color.white);
                espBoxTex.Apply();
            }

            var style = new GUIStyle(GUI.skin.box);
            style.fontSize = 12;
            style.fontStyle = FontStyle.Bold;
            style.normal.textColor = new Color(1f, 0.25f, 0.25f, 1f);
            style.alignment = TextAnchor.MiddleCenter;

            string info = $"🎯 LOCK: {currentLockedName} ({currentLockedDistance:0.#}m)";
            Vector2 size = style.CalcSize(new GUIContent(info));
            GUI.Box(new Rect(screenX - (size.x + 14f) / 2f, screenY - 32f, size.x + 14f, 24f), info, style);

            Color oldColor = GUI.color;
            GUI.color = Color.red;
            GUI.DrawTexture(new Rect(screenX - 8, screenY - 1, 16, 2), espBoxTex);
            GUI.DrawTexture(new Rect(screenX - 1, screenY - 8, 2, 16), espBoxTex);
            GUI.color = oldColor;
        }

        private void OnDestroy()
        {
            Logger.LogWarning("Plugin GameObject OnDestroy called (IPC server remains active).");
            if (mapDirectionalLightObj != null) Destroy(mapDirectionalLightObj);
        }
    }
}
