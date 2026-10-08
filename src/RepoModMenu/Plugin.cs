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
                   $"\"speed\":{EnableSpeedHack.ToString().ToLower()}," +
                   $"\"speedMultiplier\":{SpeedMultiplier.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)}," +
                   $"\"jump\":{EnableInfiniteJump.ToString().ToLower()}," +
                   $"\"stamina\":{EnableInfiniteStamina.ToString().ToLower()}," +
                   $"\"god\":{EnableGodMode.ToString().ToLower()}," +
                   $"\"tumble\":{EnableNoTumble.ToString().ToLower()}," +
                   $"\"fullbright\":{EnableFullbright.ToString().ToLower()}}}";
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
                    found = cachedItems.Find(x =>
                        CleanItemKey(x.itemName).Contains(noItem) ||
                        CleanItemKey(x.name).Contains(noItem));
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
                    found = Resources.Load<Item>($"Items/{name}") ?? Resources.Load<Item>(name);
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
                var added = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var names = new List<string>();

                void AddItemCandidate(Item it)
                {
                    if (it == null) return;
                    string displayName = !string.IsNullOrEmpty(it.itemName) ? it.itemName : it.name;
                    if (string.IsNullOrEmpty(displayName)) return;
                    if (displayName.StartsWith("Item Upgrade Player", StringComparison.OrdinalIgnoreCase) ||
                        displayName.StartsWith("ItemUpgrade", StringComparison.OrdinalIgnoreCase))
                        return;

                    if (!added.Contains(displayName))
                    {
                        added.Add(displayName);
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

            var inv = Inventory.instance;
            if (inv != null)
            {
                int freeIndex = inv.GetFirstFreeInventorySpotIndex();
                if (freeIndex >= 0)
                {
                    var spot = inv.GetSpotByIndex(freeIndex);
                    var equippable = spawnedObj.GetComponentInChildren<ItemEquippable>();
                    if (spot != null && equippable != null)
                    {
                        spot.EquipItem(equippable);
                        ShowNotification($"Đã cất [{disp}] vào Balo (Ô {freeIndex + 1})!");
                        return;
                    }
                }
            }

            ShowNotification($"Balo đầy! Đã thả [{disp}] ra trước mặt.");
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
            Logger.LogWarning("Plugin GameObject OnDestroy called (IPC server remains active).");
            if (mapDirectionalLightObj != null) Destroy(mapDirectionalLightObj);
        }
    }
}
