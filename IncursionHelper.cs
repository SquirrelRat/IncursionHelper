using ExileCore;
using ExileCore.PoEMemory;
using ExileCore.PoEMemory.Elements;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Nodes;
using ImGuiNET;
using Newtonsoft.Json;
using SharpDX;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Vector2 = System.Numerics.Vector2;

namespace IncursionHelper;

public enum TierRank { Untiered = 0, C = 1, B = 2, A = 3, S = 4 }

public partial class IncursionHelper : BaseSettingsPlugin<IncursionHelperSettings>
{
    public class RoomInfo
    {
        public string Tier1Name { get; set; }
        public string Tier2Name { get; set; }
        public string Tier3Name { get; set; }
        public string Tier { get; set; }
        public string Description { get; set; }
        public string Comment { get; set; }

        public RoomInfo() { }

        public RoomInfo(string tier1, string tier2, string tier3, string tier, string description, string comment = null)
        {
            Tier1Name = tier1;
            Tier2Name = tier2;
            Tier3Name = tier3;
            Tier = tier;
            Description = description;
            Comment = comment;
        }

        public bool ContainsRoom(string roomName)
        {
            if (string.IsNullOrWhiteSpace(roomName)) return false;
            var normalizedRoomName = roomName.Trim();
            // Legacy alias: Splinter -> Anomaly (3.28 rename) handle both
            if (normalizedRoomName.Equals("Splinter Research Lab", StringComparison.OrdinalIgnoreCase) &&
                Tier1Name.Equals("Anomaly Research Lab", StringComparison.OrdinalIgnoreCase))
                return true;
            if (normalizedRoomName.Equals("Anomaly Research Lab", StringComparison.OrdinalIgnoreCase) &&
                Tier1Name.Equals("Splinter Research Lab", StringComparison.OrdinalIgnoreCase))
                return true;
            return normalizedRoomName.Equals(Tier1Name, StringComparison.OrdinalIgnoreCase) ||
                   normalizedRoomName.Equals(Tier2Name, StringComparison.OrdinalIgnoreCase) ||
                   normalizedRoomName.Equals(Tier3Name, StringComparison.OrdinalIgnoreCase);
        }

        [JsonIgnore]
        public bool IsUntiered => Tier1Name.Equals(Tier2Name, StringComparison.OrdinalIgnoreCase) &&
                                   Tier2Name.Equals(Tier3Name, StringComparison.OrdinalIgnoreCase);

        [JsonIgnore]
        public TierRank Rank => IsUntiered ? TierRank.Untiered : Tier switch
        {
            "S" => TierRank.S,
            "A" => TierRank.A,
            "B" => TierRank.B,
            "C" => TierRank.C,
            _ => TierRank.Untiered
        };

        [JsonIgnore]
        public int TierValue => (int)Rank;
    }

    private static List<RoomInfo> ImportantRooms;
    private Dictionary<string, RoomInfo> _lookup;

    private static readonly List<RoomInfo> DefaultImportantRooms = new List<RoomInfo>
    {
        new RoomInfo("Corruption Chamber", "Catalyst Of Corruption", "Locus Of Corruption", "S", "Altar of Corruption for double-corrupting items.", "S-Tier Room"),
        new RoomInfo("Gemcutter's Workshop", "Department Of Thaumaturgy", "Doryani's Institute", "S", "Lapidary Lens for double-corrupting gems.", "S-Tier Room"),
        new RoomInfo("Shrine Of Empowerment", "Sanctum Of Unity", "Temple Nexus", "S", "Upgrades adjacent rooms, increases item level.", "S-Tier Room"),
        new RoomInfo("Vault", "Treasury", "Wealth Of The Vaal", "S", "Contains valuable currency chests.", "S-Tier Room"),

        new RoomInfo("Sacrificial Chamber", "Hall Of Offerings", "Apex Of Ascension", "A", "Gamble unique items for potentially better ones.", "A-Tier Room"),
        new RoomInfo("Jeweller's Workshop", "Jewellery Forge", "Glittering Halls", "A", "Contains rare jewellery chests.", "A-Tier Room"),
        new RoomInfo("Hall Of Mettle", "Hall Of Heroes", "Hall Of Legends", "A", "Access to Timeless Monoliths for Legion encounters.", "A-Tier Room"),
        new RoomInfo("Surveyor's Study", "Office Of Cartography", "Atlas Of Worlds", "A", "Contains maps; 3.28+ architects also drop Scarabs here.", "A-Tier Room"),
        new RoomInfo("Anomaly Research Lab", "Breach Containment Chamber", "House Of The Others", "A", "Unstable Breaches (re-enabled 3.28).", "A-Tier Room"),

        new RoomInfo("Armourer's Workshop", "Armoury", "Chamber Of Iron", "B", "Contains armour chests, monsters have increased resistances.", "B-Tier Room"),
        new RoomInfo("Guardhouse", "Barracks", "Hall Of War", "B", "Increases monster pack size.", "B-Tier Room"),
        new RoomInfo("Hatchery", "Automaton Lab", "Hybridisation Chamber", "B", "Adds minion monster packs, valuable item at T3.", "B-Tier Room"),
        new RoomInfo("Pools Of Restoration", "Sanctum Of Vitality", "Sanctum Of Immortality", "B", "Monsters regenerate life, valuable item at T3.", "B-Tier Room"),
        new RoomInfo("Explosives Room", "Demolition Lab", "Shrine Of Unmaking", "B", "Contains explosive charges for sealed passages/coffers.", "B-Tier Room"),
        new RoomInfo("Workshop", "Engineering Department", "Factory", "B", "Increases item quantity from Temple, tougher boss.", "B-Tier Room"),
        new RoomInfo("Trap Workshop", "Temple Defense Workshop", "Defense Research Lab", "B", "Adds traps, valuable item at T3.", "B-Tier Room"),
        new RoomInfo("Flame Workshop", "Omnitect Forge", "Crucible Of Flame", "B", "Adds fire monster packs, valuable item at T3.", "B-Tier Room"),
        new RoomInfo("Poison Garden", "Cultivar Chamber", "Toxic Grove", "B", "Adds chaos damage over time, valuable item at T3.", "B-Tier Room"),
        new RoomInfo("Sparring Room", "Arena Of Valour", "Hall Of Champions", "B", "Contains weapon chests, increases monster criticals.", "B-Tier Room"),
        new RoomInfo("Tempest Generator", "Hurricane Engine", "Storm Of Corruption", "B", "Adds Tempests, valuable item at T3.", "B-Tier Room"),
        new RoomInfo("Torment Cells", "Torture Cages", "Sadist's Den", "B", "Contains Tormented Spirits.", "B-Tier Room"),
        new RoomInfo("Royal Meeting Room", "Hall Of Lords", "Throne Of Atziri", "B", "Increases magic monsters, Queen Atziri fight at T3.", "B-Tier Room"),
        new RoomInfo("Strongbox Chamber", "Hall Of Locks", "Court Of Sealed Death", "B", "Contains Strongboxes.", "B-Tier Room"),

        new RoomInfo("Storage Room", "Warehouses", "Museum Of Artefacts", "C", "Contains generic item chests.", "C-Tier Room"),
        new RoomInfo("Lightning Workshop", "Omnitect Reactor Plant", "Conduit Of Lightning", "C", "Adds lightning monster packs, generally undesirable.", "C-Tier Room"),

        new RoomInfo("Antechamber", "Antechamber", "Antechamber", "C", "Basic untiered room.", "Untiered Base Room"),
        new RoomInfo("Apex Of Atzoatl", "Apex Of Atzoatl", "Apex Of Atzoatl", "C", "Basic untiered room.", "Untiered Base Room"),
        new RoomInfo("Banquet Hall", "Banquet Hall", "Banquet Hall", "C", "Basic untiered room.", "Untiered Base Room"),
        new RoomInfo("Cellar", "Cellar", "Cellar", "C", "Basic untiered room.", "Untiered Base Room"),
        new RoomInfo("Chasm", "Chasm", "Chasm", "C", "Basic untiered room.", "Untiered Base Room"),
        new RoomInfo("Cloister", "Cloister", "Cloister", "C", "Basic untiered room.", "Untiered Base Room"),
        new RoomInfo("Entrance", "Entrance", "Entrance", "C", "Basic untiered room.", "Untiered Base Room"),
        new RoomInfo("Halls", "Halls", "Halls", "C", "Basic untiered room.", "Untiered Base Room"),
        new RoomInfo("Passageways", "Passageways", "Passageways", "C", "Basic untiered room.", "Untiered Base Room"),
        new RoomInfo("Pits", "Pits", "Pits", "C", "Basic untiered room.", "Untiered Base Room"),
        new RoomInfo("Tombs", "Tombs", "Tombs", "C", "Basic untiered room.", "Untiered Base Room"),
        new RoomInfo("Tunnels", "Tunnels", "Tunnels", "C", "Basic untiered room.", "Untiered Base Room")
    };

    public IncursionWindow IncursionPanel { get; set; }

    private static Element SafeGet(Element root, params int[] indices)
    {
        try
        {
            var cur = root;
            foreach (var i in indices)
            {
                if (cur == null || cur.ChildCount <= i) return null;
                cur = cur.GetChildAtIndex(i);
                if (cur == null) return null;
            }
            return cur;
        }
        catch { return null; }
    }

    private string ExtractRoomNameFromRewardString(string rewardString)
    {
        if (string.IsNullOrWhiteSpace(rewardString)) return string.Empty;
        // Handles "(Kill to change to X)" and "(Kill to upgrade to X)" case-insensitive, trims trailing ")}" etc.
        var lower = rewardString.ToLowerInvariant();
        int start = lower.IndexOf("(kill to change to ", StringComparison.Ordinal);
        int len;
        if (start == -1)
        {
            start = lower.IndexOf("(kill to upgrade to ", StringComparison.Ordinal);
            if (start == -1) return string.Empty;
            len = "(kill to upgrade to ".Length;
        }
        else
        {
            len = "(kill to change to ".Length;
        }
        start += len;
        int end = rewardString.IndexOf(")}", start, StringComparison.Ordinal);
        if (end == -1) end = rewardString.IndexOf(')', start);
        if (end == -1) end = rewardString.Length;
        var extracted = rewardString.Substring(start, end - start).Trim().Trim('}', ')', ' ');
        // strip any tags left
        return extracted;
    }

    private static bool IsUpgrade(string raw) => raw.IndexOf("KILL TO UPGRADE TO", StringComparison.OrdinalIgnoreCase) >= 0;
    private static bool IsChange(string raw) => raw.IndexOf("KILL TO CHANGE TO", StringComparison.OrdinalIgnoreCase) >= 0;

    private static bool TryParseDoorTarget(string tooltipText, out string targetRoomName, out bool isUnlocked)
    {
        targetRoomName = null;
        isUnlocked = false;
        if (string.IsNullOrWhiteSpace(tooltipText)) return false;
        isUnlocked = tooltipText.IndexOf("Unlocked", StringComparison.OrdinalIgnoreCase) >= 0;
        int idx = tooltipText.IndexOf("Door to ", StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return false;
        idx += "Door to ".Length;
        int end = tooltipText.IndexOf('|', idx);
        int end2 = tooltipText.IndexOf('\n', idx);
        int end3 = tooltipText.IndexOf('<', idx);
        int e = tooltipText.Length;
        if (end >= 0) e = Math.Min(e, end);
        if (end2 >= 0) e = Math.Min(e, end2);
        if (end3 >= 0) e = Math.Min(e, end3);
        var target = tooltipText.Substring(idx, e - idx).Trim();
        if (string.IsNullOrWhiteSpace(target)) return false;
        targetRoomName = target;
        return true;
    }

    private static int GetRemainingIncursions(IncursionWindow panel)
    {
        try
        {
            var txtEl = SafeGet(panel, 5);
            if (txtEl != null && !string.IsNullOrWhiteSpace(txtEl.Text))
            {
                var parts = txtEl.Text.Split(' ');
                if (int.TryParse(parts[0], out var n)) return n;
            }
            var f = panel.FindChildRecursive(e => e.Text != null && e.Text.Contains("Incursions Remaining"), 8);
            if (f != null)
            {
                var p = f.Text.Split(' ');
                if (int.TryParse(p[0], out var n2)) return n2;
            }
        }
        catch { }
        return 6;
    }

    private void BuildLookup()
    {
        _lookup = new Dictionary<string, RoomInfo>(StringComparer.OrdinalIgnoreCase);
        foreach (var r in ImportantRooms)
        {
            if (!string.IsNullOrWhiteSpace(r.Tier1Name)) _lookup[r.Tier1Name.Trim()] = r;
            if (!string.IsNullOrWhiteSpace(r.Tier2Name)) _lookup[r.Tier2Name.Trim()] = r;
            if (!string.IsNullOrWhiteSpace(r.Tier3Name)) _lookup[r.Tier3Name.Trim()] = r;
            // legacy alias
            if (r.Tier1Name.Equals("Anomaly Research Lab", StringComparison.OrdinalIgnoreCase))
                _lookup["Splinter Research Lab"] = r;
        }
    }

    private RoomInfo Lookup(string roomName)
    {
        if (string.IsNullOrWhiteSpace(roomName)) return null;
        var key = roomName.Trim();
        if (_lookup != null && _lookup.TryGetValue(key, out var v)) return v;
        return ImportantRooms.FirstOrDefault(r => r.ContainsRoom(key));
    }

    private TierRank GetEffectiveRank(RoomInfo info)
    {
        if (info == null) return TierRank.Untiered;
        if (Settings.RoomTierOverrides != null && Settings.RoomTierOverrides.TryGetValue(info.Tier1Name, out var ov) && ov >= 0 && ov <= 4)
            return (TierRank)ov;
        return info.Rank;
    }

    private int GetEffectiveTierValue(RoomInfo info) => (int)GetEffectiveRank(info);

    private void EnsureRoomTierOverrides()
    {
        if (Settings.RoomTierOverrides == null) Settings.RoomTierOverrides = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        if (Settings.RoomTierOverrides.Count == 0)
        {
            foreach (var r in DefaultImportantRooms)
            {
                if (!string.IsNullOrWhiteSpace(r.Tier1Name) && !Settings.RoomTierOverrides.ContainsKey(r.Tier1Name))
                    Settings.RoomTierOverrides[r.Tier1Name] = (int)r.Rank;
            }
        }
        else
        {
            // fill missing new rooms without overwriting user choices
            foreach (var r in DefaultImportantRooms)
            {
                if (!Settings.RoomTierOverrides.ContainsKey(r.Tier1Name))
                    Settings.RoomTierOverrides[r.Tier1Name] = (int)r.Rank;
            }
        }
    }

    public void LoadDefaultStrategy()
    {
        Settings.WeightTier.Value = 10;
        Settings.WeightUpgrade.Value = 2;
        Settings.WeightScarcity.Value = 1;
        Settings.WeightChangeEarly.Value = 2;
        Settings.WeightUpgradeLate.Value = 4;
        Settings.WeightSTierBonus.Value = 1;
        Settings.WeightUntieredPenalty.Value = 5;
        Settings.RoomTierOverrides.Clear();
        foreach (var r in DefaultImportantRooms)
            Settings.RoomTierOverrides[r.Tier1Name] = (int)r.Rank;
        LogMessage("Loaded default Meta Profit strategy (Locus/Doryani S-tier focus).");
    }

    public void LoadSeedStrategy()
    {
        LoadDefaultStrategy();
        Settings.WeightChangeEarly.Value = 5;
        Settings.WeightScarcity.Value = 3;
        Settings.WeightUpgradeLate.Value = 2;
        LogMessage("Loaded Seed Diversity strategy (favor changes early).");
    }

    public void LoadRushStrategy()
    {
        LoadDefaultStrategy();
        Settings.WeightUpgradeLate.Value = 6;
        Settings.WeightUpgrade.Value = 4;
        Settings.WeightChangeEarly.Value = 0;
        LogMessage("Loaded Rush-to-T3 strategy (favor upgrades late).");
    }

    public void ResetRoomTiers()
    {
        Settings.RoomTierOverrides.Clear();
        foreach (var r in DefaultImportantRooms)
            Settings.RoomTierOverrides[r.Tier1Name] = (int)r.Rank;
        LogMessage("Room tiers reset to defaults.");
    }

    private ColorNode ColorFor(RoomInfo info)
    {
        if (info == null) return Settings.CTierColor;
        return GetEffectiveRank(info) switch
        {
            TierRank.S => Settings.STierColor,
            TierRank.A => Settings.ATierColor,
            TierRank.B => Settings.BTierColor,
            TierRank.C => Settings.CTierColor,
            _ => Settings.UntieredRoomColor
        };
    }

    public override bool Initialise()
    {
        var roomDataPath = Path.Combine(ConfigDirectory, "RoomData.json");
        if (File.Exists(roomDataPath))
        {
            try
            {
                var json = File.ReadAllText(roomDataPath);
                var loaded = JsonConvert.DeserializeObject<List<RoomInfo>>(json);
                if (loaded != null && loaded.Count > 0)
                {
                    // migrate legacy Splinter -> Anomaly
                    bool migrated = false;
                    foreach (var r in loaded)
                    {
                        if (r.Tier1Name.Equals("Splinter Research Lab", StringComparison.OrdinalIgnoreCase))
                        {
                            r.Tier1Name = "Anomaly Research Lab";
                            migrated = true;
                        }
                    }
                    ImportantRooms = loaded;
                    if (migrated)
                    {
                        File.WriteAllText(roomDataPath, JsonConvert.SerializeObject(ImportantRooms, Formatting.Indented));
                        LogMessage("Migrated Splinter Research Lab -> Anomaly Research Lab");
                    }
                    LogMessage($"Loaded {ImportantRooms.Count} Incursion rooms from {roomDataPath}");
                }
                else ImportantRooms = DefaultImportantRooms;
            }
            catch (Exception ex)
            {
                LogError($"Failed to load Incursion rooms from {roomDataPath}: {ex.Message}. Using default rooms.");
                ImportantRooms = DefaultImportantRooms;
            }
        }
        else
        {
            ImportantRooms = DefaultImportantRooms;
            try
            {
                var json = JsonConvert.SerializeObject(ImportantRooms, Formatting.Indented);
                File.WriteAllText(roomDataPath, json);
                LogMessage($"Created default Incursion rooms file at {roomDataPath}");
            }
            catch (Exception ex)
            {
                LogError($"Failed to save default Incursion rooms to {roomDataPath}: {ex.Message}");
            }
        }
        BuildLookup();
        EnsureRoomTierOverrides();
        return true;
    }

    public override void AreaChange(AreaInstance area) { }
    public override Job Tick() => null;

    private void ProcessCurrentRooms(Element incursionRooms)
    {
        if (!Settings.EnableTierFrames.Value) return;
        if (incursionRooms == null) return;
        int thickness = Settings.FrameThickness.Value;
        foreach (var item in incursionRooms.Children)
        {
            if (item == null || item.ChildCount == 0) continue;
            var nameEl = SafeGet(item, 0, 0);
            if (nameEl == null || string.IsNullOrWhiteSpace(nameEl.Text)) continue;
            var roomName = nameEl.Text.Trim();
            var info = Lookup(roomName);
            var color = ColorFor(info);
            try
            {
                var rect = nameEl.GetClientRectCache;
                int t = (info != null && info.IsUntiered) ? Math.Max(1, thickness - 1) : thickness;
                Graphics.DrawFrame(rect, color, t);
            }
            catch { }
        }
    }

    private void DrawConnections(Element incursionRooms)
    {
        if (!Settings.EnableConnections.Value) return;
        if (incursionRooms == null || IncursionPanel == null) return;
        Element selectedRoom = null;
        foreach (var item in incursionRooms.Children)
        {
            var hl = SafeGet(item, 2);
            if (hl != null && hl.IsVisible) { selectedRoom = item; break; }
        }
        if (selectedRoom == null) return;
        var diamond = SafeGet(IncursionPanel, 3, 13, 0);
        if (diamond == null || !diamond.IsVisible) return;
        var selRect = selectedRoom.GetClientRectCache;
        var selCenter = new Vector2(selRect.X + selRect.Width * 0.5f, selRect.Y + selRect.Height * 0.5f);
        var map = new Dictionary<string, RectangleF>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in incursionRooms.Children)
        {
            var nEl = SafeGet(item, 0, 0);
            if (nEl == null || string.IsNullOrWhiteSpace(nEl.Text)) continue;
            map[nEl.Text.Trim()] = item.GetClientRectCache;
        }
        string bestLockedName = null;
        RoomInfo bestLockedInfo = null;
        RectangleF bestLockedRect = default;
        Element bestLockedDot = null;
        int bestLockedScore = -999;
        int bestLockedIdx = -1;
        int diamondChildCount = (int)diamond.ChildCount;
        // first pass: find best locked to highlight as green
        for (int i = 3; i < diamondChildCount && i <= 8; i++)
        {
            var dotParent = SafeGet(diamond, i);
            if (dotParent == null) continue;
            var dot = SafeGet(dotParent, 1);
            if (dot == null) continue;
            var tip = dot.Tooltip;
            if (tip == null) continue;
            if (!TryParseDoorTarget(tip.Text, out var target, out var isUnlocked)) continue;
            if (isUnlocked) continue;
            var tInfo = Lookup(target);
            int sc = tInfo != null ? GetEffectiveTierValue(tInfo) * Settings.WeightTier.Value : 0;
            if (tInfo != null && GetEffectiveRank(tInfo) == TierRank.S) sc += Settings.WeightSTierBonus.Value;
            if (tInfo != null && GetEffectiveRank(tInfo) == TierRank.Untiered) sc -= Settings.WeightUntieredPenalty.Value;
            if (sc > bestLockedScore)
            {
                bestLockedScore = sc;
                bestLockedName = target;
                bestLockedInfo = tInfo;
                bestLockedIdx = i;
                bestLockedDot = dot;
                if (map.TryGetValue(target, out var tr)) bestLockedRect = tr;
                else
                {
                    var kv = map.FirstOrDefault(kv2 => kv2.Key.Equals(target, StringComparison.OrdinalIgnoreCase));
                    if (kv.Key != null) bestLockedRect = kv.Value;
                }
            }
        }
        if (!Settings.EnableRecommendation.Value)
        {
            bestLockedIdx = -1;
            bestLockedDot = null;
            bestLockedName = null;
            bestLockedRect = default;
        }
        // draw only not-connected (red) and suggested (green) - hide already-connected blue
        for (int i = 3; i < diamondChildCount && i <= 8; i++)
        {
            var dotParent = SafeGet(diamond, i);
            if (dotParent == null) continue;
            var dot = SafeGet(dotParent, 1);
            if (dot == null) continue;
            var tip = dot.Tooltip;
            if (tip == null) continue;
            if (!TryParseDoorTarget(tip.Text, out var target, out var isUnlocked)) continue;
            if (isUnlocked) continue; // hide already connected (blue)
            bool isBest = i == bestLockedIdx;
            if (!map.TryGetValue(target, out var tRect))
            {
                var kv = map.FirstOrDefault(kv2 => kv2.Key.Equals(target, StringComparison.OrdinalIgnoreCase));
                if (kv.Key == null) continue;
                tRect = kv.Value;
            }
            var tCenter = new Vector2(tRect.X + tRect.Width * 0.5f, tRect.Y + tRect.Height * 0.5f);
            SharpDX.Color col = isBest ? Settings.SuggestedDoorColor.Value : Settings.LockedDoorColor.Value;
            float thick = isBest ? Settings.RecommendedThickness.Value : 2.0f;
            try { Graphics.DrawLine(selCenter, tCenter, thick, col); } catch { }
        }
        try { Graphics.DrawFrame(selRect, Settings.RecommendedChoiceHighlight, 2); } catch { }
        if (bestLockedDot != null && !string.IsNullOrWhiteSpace(bestLockedName))
        {
            try
            {
                var dRect = bestLockedDot.GetClientRectCache;
                int recThick = Settings.RecommendedThickness.Value;
                Graphics.DrawFrame(dRect, Settings.SuggestedDoorColor, recThick);
                if (bestLockedRect.Width > 0) Graphics.DrawFrame(bestLockedRect, Settings.SuggestedDoorColor, recThick);
                const string label = "Open This Door";
                var sz = Graphics.MeasureText(label);
                // label on diamond
                var dpCenter = new Vector2(dRect.X + dRect.Width * 0.5f, dRect.Y + dRect.Height * 0.5f);
                var pos = new Vector2(dpCenter.X - sz.X * 0.5f, dRect.Y - sz.Y - 4);
                var bg = new RectangleF(pos.X - 4, pos.Y - 1, sz.X + 8, sz.Y + 2);
                Graphics.DrawBox(bg, new Color(0, 0, 0, 210));
                Graphics.DrawFrame(bg, Settings.SuggestedDoorColor, 1);
                Graphics.DrawText(label, pos, Color.White);
                // label also on the main tile (e.g. Corruption Chamber)
                if (bestLockedRect.Width > 0)
                {
                    var pos2 = new Vector2(bestLockedRect.X + (bestLockedRect.Width - sz.X) * 0.5f, bestLockedRect.Y - sz.Y - 4);
                    // if above goes off window, put just inside top
                    if (pos2.Y < 5) pos2.Y = bestLockedRect.Y + 2;
                    var bg2 = new RectangleF(pos2.X - 4, pos2.Y - 1, sz.X + 8, sz.Y + 2);
                    Graphics.DrawBox(bg2, new Color(0, 0, 0, 210));
                    Graphics.DrawFrame(bg2, Settings.SuggestedDoorColor, 1);
                    Graphics.DrawText(label, pos2, Color.White);
                }
            }
            catch { }
        }
    }

    private int ScoreChoice(RoomInfo info, bool isUpgrade, bool isChange, int remaining, HashSet<string> visibleRooms)
    {
        if (info == null) return 0;
        var effRank = GetEffectiveRank(info);
        if (effRank == TierRank.Untiered) return -Settings.WeightUntieredPenalty.Value;
        int score = GetEffectiveTierValue(info) * Settings.WeightTier.Value;
        if (isUpgrade) score += Settings.WeightUpgrade.Value;
        // scarcity: target T3 not yet on map is valuable
        if (!string.IsNullOrWhiteSpace(info.Tier3Name) && !visibleRooms.Contains(info.Tier3Name.Trim()))
            score += Settings.WeightScarcity.Value;
        // early game favour change to seed new rooms, late game favour upgrade to finish
        if (remaining <= 3 && isUpgrade) score += Settings.WeightUpgradeLate.Value;
        else if (remaining >= 9 && isChange) score += Settings.WeightChangeEarly.Value;
        // S-tier extra nudge even when early
        if (effRank == TierRank.S) score += Settings.WeightSTierBonus.Value;
        return score;
    }

    private (string bestName, Element bestEl, bool bestIsUpgrade, RoomInfo bestInfo, string badge) ProcessRewardChoices(string reward1Raw, string reward2Raw, HashSet<string> visibleRooms, int remaining)
    {
        string r1Name = ExtractRoomNameFromRewardString(reward1Raw);
        string r2Name = ExtractRoomNameFromRewardString(reward2Raw);
        var r1Info = Lookup(r1Name);
        var r2Info = Lookup(r2Name);
        // fallback for unknown -> C untiered-ish
        if (r1Info == null && !string.IsNullOrWhiteSpace(r1Name)) r1Info = new RoomInfo(r1Name, r1Name, r1Name, "C", "Unknown");
        if (r2Info == null && !string.IsNullOrWhiteSpace(r2Name)) r2Info = new RoomInfo(r2Name, r2Name, r2Name, "C", "Unknown");

        bool r1Up = IsUpgrade(reward1Raw), r1Ch = IsChange(reward1Raw);
        bool r2Up = IsUpgrade(reward2Raw), r2Ch = IsChange(reward2Raw);

        int s1 = ScoreChoice(r1Info, r1Up, r1Ch, remaining, visibleRooms);
        int s2 = ScoreChoice(r2Info, r2Up, r2Ch, remaining, visibleRooms);

        Element r1El = SafeGet(IncursionPanel, 3, 13, 3);
        Element r2El = SafeGet(IncursionPanel, 3, 13, 4);
        Element bestEl = null;

        Element changeIcon = SafeGet(IncursionPanel, 3, 13, 0, 1);
        Element upgradeIcon = SafeGet(IncursionPanel, 3, 13, 0, 2);

        string bestName;
        RoomInfo bestInfo;
        bool bestIsUp;
        Element bestIcon = null;

        // no frames around architect names - data already visible, frames are bloat
        if (s1 > s2 || (s1 == s2 && r1Up && remaining <= 4))
        {
            bestName = r1Name; bestEl = r1El; bestInfo = r1Info; bestIsUp = r1Up;
            bestIcon = r1Up ? upgradeIcon : r1Ch ? changeIcon : null;
        }
        else if (s2 > s1 || !string.IsNullOrWhiteSpace(r2Name))
        {
            if (s1 == s2 && r1Up && remaining <= 4)
            {
                bestName = r1Name; bestEl = r1El; bestInfo = r1Info; bestIsUp = r1Up;
                bestIcon = r1Up ? upgradeIcon : r1Ch ? changeIcon : null;
            }
            else
            {
                bestName = r2Name; bestEl = r2El; bestInfo = r2Info; bestIsUp = r2Up;
                bestIcon = r2Up ? upgradeIcon : r2Ch ? changeIcon : null;
            }
        }
        else
        {
            bestName = r1Name; bestEl = r1El; bestInfo = r1Info; bestIsUp = r1Up;
        }

        string badge = "";
        if (!string.IsNullOrWhiteSpace(bestName) && bestInfo != null && !bestInfo.IsUntiered)
        {
            var shortName = bestName.Length > 18 ? bestName.Substring(0, 18) + "..." : bestName;
            badge = (bestIsUp ? "UP " : "NEW ") + shortName + $" ({bestInfo.Tier})";
        }
        return (bestName, bestEl, bestIsUp, bestInfo, badge);
    }

    public override void Render()
    {
        if (!Settings.Enable.Value) return;
        try
        {
            var window = GameController?.IngameState?.IngameUi?.IncursionWindow;
            if (window == null || !window.IsVisible) return;
            IncursionPanel = window;

            Element incursionRooms = SafeGet(IncursionPanel, 3);
            if (incursionRooms == null) return;

            ProcessCurrentRooms(incursionRooms);
            DrawConnections(incursionRooms);

            Element r1El = SafeGet(IncursionPanel, 3, 13, 3);
            if (r1El == null || !r1El.IsVisible) return;

            string reward1Raw = IncursionPanel.Reward1 ?? "";
            string reward2Raw = IncursionPanel.Reward2 ?? "";
            if (string.IsNullOrWhiteSpace(reward1Raw) && string.IsNullOrWhiteSpace(reward2Raw)) return;

            int remaining = GetRemainingIncursions(IncursionPanel);

            // visible rooms for scarcity check
            var visibleRooms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in incursionRooms.Children)
            {
                var n = SafeGet(item, 0, 0);
                if (n != null && !string.IsNullOrWhiteSpace(n.Text)) visibleRooms.Add(n.Text.Trim());
            }

            var (bestName, bestEl, bestIsUp, bestInfo, badge) = ProcessRewardChoices(reward1Raw, reward2Raw, visibleRooms, remaining);

            if (Settings.EnableBadge.Value && !string.IsNullOrWhiteSpace(badge) && bestEl != null)
            {
                try
                {
                    var r = bestEl.GetClientRectCache;
                    var pos = new Vector2(r.Left, r.Bottom + 4);
                    // small background for readability
                    var sz = Graphics.MeasureText(badge);
                    var bg = new RectangleF(pos.X, pos.Y, sz.X + 8, sz.Y + 4);
                    Graphics.DrawBox(bg, new Color(0, 0, 0, 180));
                    Graphics.DrawFrame(bg, Settings.RecommendedChoiceHighlight, 1);
                    Graphics.DrawText(badge, new Vector2(pos.X + 4, pos.Y + 2), Color.White);
                }
                catch { }
            }

            // hover tooltip: if mouse over best choice, show one-line reason
            if (bestEl != null && bestInfo != null)
            {
                try
                {
                    var mouse = ImGui.GetMousePos();
                    var rect = bestEl.GetClientRectCache;
                    if (rect.Contains(mouse.X, mouse.Y))
                    {
                        var reason = bestIsUp ? $"Upgrade to {bestInfo.Tier3Name} ({bestInfo.Tier})" : $"Change to {bestInfo.Tier3Name} ({bestInfo.Tier})";
                        if (bestInfo.IsUntiered) reason = bestName;
                        else reason += $" - {bestInfo.Description}";
                        var tp = new Vector2(rect.Right + 8, rect.Top);
                        var tsz = Graphics.MeasureText(reason);
                        var tr = new RectangleF(tp.X, tp.Y, tsz.X + 10, tsz.Y + 6);
                        Graphics.DrawBox(tr, new Color(20, 20, 20, 220));
                        Graphics.DrawFrame(tr, Settings.RecommendedChoiceHighlight, 1);
                        Graphics.DrawText(reason, new Vector2(tr.Left + 5, tr.Top + 3), Color.White);
                    }
                }
                catch { }
            }
        }
        catch (Exception ex)
        {
            LogError($"IncursionHelper Render error: {ex.Message} {ex.StackTrace}");
        }
    }

    public override void EntityAdded(Entity entity) { }
}
