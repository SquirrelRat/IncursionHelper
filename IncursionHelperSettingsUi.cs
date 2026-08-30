using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ExileCore.Shared.Nodes;
using ImGuiNET;

namespace IncursionHelper;

public partial class IncursionHelper
{
    private bool _loadDefaultOpen;
    private bool _loadSeedOpen;
    private bool _loadRushOpen;
    private bool _resetTiersOpen;
    private string _roomSearchFilter = "";
    private int _roomTierFilter;

    public override void DrawSettings()
    {
        DrawSettingsHeader();
        if (!ImGui.BeginTabBar("##incursionTabs")) return;

        (string, Action)[] tabs =
        {
            ("Dashboard", DrawDashboardTab),
            ("Strategy", DrawStrategyTab),
            ("Rooms", DrawRoomsTab),
            ("Appearance", DrawAppearanceTab),
            ("Connections", DrawConnectionsTab),
        };

        foreach (var (label, draw) in tabs)
            if (ImGui.BeginTabItem(label)) { draw(); ImGui.EndTabItem(); }

        ImGui.EndTabBar();
    }

    private void DrawSettingsHeader()
    {
        ImGui.TextColored(Settings.Enable.Value ? new Vector4(0.49f, 1f, 0.25f, 1f) : new Vector4(0.65f, 0.65f, 0.7f, 1f),
            Settings.Enable.Value ? "INCURSION HELPER  /  ACTIVE" : "INCURSION HELPER  /  PAUSED");
        ImGui.Separator();
    }

    private void DrawDashboardTab()
    {
        ImGui.TextWrapped("Build temples for trade value or personal runs. Default strat is Meta Profit (Locus of Corruption + Doryani's Institute at S-tier). Tune weights or per-room tiers in Strategy/Rooms tabs.");
        ImGui.Spacing();

        var cardWidth = (ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X * 2f) / 3f;
        DrawStatusCard("Status", Settings.Enable.Value ? "Enabled" : "Disabled",
            Settings.Enable.Value ? new SharpDX.Color(124, 252, 0, 255) : new SharpDX.Color(150, 150, 160, 255), cardWidth);
        ImGui.SameLine();
        var tierCount = Settings.RoomTierOverrides.Count(x => x.Value == (int)TierRank.S);
        DrawStatusCard("S-tier lines", $"{tierCount} rooms",
            tierCount >= 2 ? Settings.STierColor.Value : new SharpDX.Color(255, 190, 70, 255), cardWidth);
        ImGui.SameLine();
        DrawStatusCard("Overlay", Settings.EnableTierFrames.Value ? "Tier frames on" : "Frames off",
            Settings.EnableTierFrames.Value ? Settings.ATierColor.Value : new SharpDX.Color(150, 150, 160, 255), cardWidth);

        ImGui.Spacing();
        ImGui.SeparatorText("Quick Setup");
        if (ImGui.Button("Load Meta Profit (Default)##dash", new Vector2(220f, 0)))
            _loadDefaultOpen = true;
        HelpMarker("Locus + Doryani at S, current weights. Recommended for trade (2-3 div per good temple).");
        ImGui.TextDisabled("Tip: use Strategy preset buttons for Rush vs Seed playstyles.");
        DrawStrategyPopups();
    }

    private void DrawStrategyTab()
    {
        ImGui.SeparatorText("Strategy Presets (MercScanner-style)");
        ImGui.TextWrapped("Weights control how architect choices are scored. Change early favors new room types when many incursions remain, upgrade late favors finishing T3s.");
        if (ImGui.Button("Load Meta Profit (Balanced)"))
            _loadDefaultOpen = true;
        ImGui.SameLine();
        if (ImGui.Button("Load Seed Diversity"))
            _loadSeedOpen = true;
        ImGui.SameLine();
        if (ImGui.Button("Load Rush to T3"))
            _loadRushOpen = true;
        HelpMarker("Seed favors changing rooms when 9+ incs remain; Rush favors upgrading when 3 or fewer remain.");
        DrawStrategyPopups();

        ImGui.SeparatorText("Scoring Weights");
        DrawSlider("Tier multiplier", Settings.WeightTier, "Score += tier * this. Higher makes S-tier dominate.");
        DrawSlider("Upgrade bonus", Settings.WeightUpgrade, "Bonus when architect will upgrade (Kill to upgrade).");
        DrawSlider("Scarcity bonus", Settings.WeightScarcity, "Bonus when target T3 not yet on map.");
        DrawSlider("Early Change bonus (>=9 left)", Settings.WeightChangeEarly, "Bonus for Kill to change when many incursions remain.");
        DrawSlider("Late Upgrade bonus (<=3 left)", Settings.WeightUpgradeLate, "Bonus for Kill to upgrade when few incursions remain.");
        DrawSlider("S-tier bonus", Settings.WeightSTierBonus, "Extra nudge for S-tier lines even early.");
        DrawSlider("Untiered penalty", Settings.WeightUntieredPenalty, "Negative score for untiered/bulk rooms.");

        ImGui.SeparatorText("Reset");
        if (ImGui.Button("Reset Weights to Default"))
        {
            Settings.WeightTier.Value = 10;
            Settings.WeightUpgrade.Value = 2;
            Settings.WeightScarcity.Value = 1;
            Settings.WeightChangeEarly.Value = 2;
            Settings.WeightUpgradeLate.Value = 4;
            Settings.WeightSTierBonus.Value = 1;
            Settings.WeightUntieredPenalty.Value = 5;
        }
    }

    private void DrawStrategyPopups()
    {
        if (_loadDefaultOpen) ImGui.OpenPopup("Load Meta Profit?");
        if (ImGui.BeginPopupModal("Load Meta Profit?", ref _loadDefaultOpen, ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.TextWrapped("Replace current weights and per-room tiers with Meta Profit defaults (Locus/Doryani S-tier, Vault/Temple Nexus S).");
            if (ImGui.Button("Apply"))
            {
                LoadDefaultStrategy();
                _loadDefaultOpen = false;
                ImGui.CloseCurrentPopup();
            }
            ImGui.SameLine();
            if (ImGui.Button("Cancel")) { _loadDefaultOpen = false; ImGui.CloseCurrentPopup(); }
            ImGui.EndPopup();
        }
        if (_loadSeedOpen) ImGui.OpenPopup("Load Seed Diversity?");
        if (ImGui.BeginPopupModal("Load Seed Diversity?", ref _loadSeedOpen, ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.TextWrapped("Balances toward seeding new types early. Good for first 5-6 incursions or SSF.");
            if (ImGui.Button("Apply")) { LoadSeedStrategy(); _loadSeedOpen = false; ImGui.CloseCurrentPopup(); }
            ImGui.SameLine();
            if (ImGui.Button("Cancel")) { _loadSeedOpen = false; ImGui.CloseCurrentPopup(); }
            ImGui.EndPopup();
        }
        if (_loadRushOpen) ImGui.OpenPopup("Load Rush to T3?");
        if (ImGui.BeginPopupModal("Load Rush to T3?", ref _loadRushOpen, ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.TextWrapped("Push upgrades late - good when you already have your S-tier line on the map.");
            if (ImGui.Button("Apply")) { LoadRushStrategy(); _loadRushOpen = false; ImGui.CloseCurrentPopup(); }
            ImGui.SameLine();
            if (ImGui.Button("Cancel")) { _loadRushOpen = false; ImGui.CloseCurrentPopup(); }
            ImGui.EndPopup();
        }
    }

    private static readonly string[] TierLabels = ["Untiered", "C", "B", "A", "S"];

    private void DrawRoomsTab()
    {
        // SimpleInformation Gem Tracker verbatim for rooms: search + color filter + color-coded list
        ImGui.TextWrapped("Pick which Temple room lines you value most. Each T1 line maps to its T3 and tier colour drives both the temple map frames and the architect/door scoring below.");
        ImGui.Spacing();

        var tracked = Settings.RoomTierOverrides.Count;
        ImGui.Text($"Tracked: {tracked} lines");
        ImGui.SameLine();
        if (ImGui.Button("Reset All to Default Tiers"))
            _resetTiersOpen = true;
        if (_resetTiersOpen) ImGui.OpenPopup("Reset tiers?");
        if (ImGui.BeginPopupModal("Reset tiers?", ref _resetTiersOpen, ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.TextWrapped("Reset every room line to DefaultImportantRooms values (e.g. Corruption Locus S, Anomaly A). This replaces your custom tiers.");
            if (ImGui.Button("Reset")) { ResetRoomTiers(); _resetTiersOpen = false; ImGui.CloseCurrentPopup(); }
            ImGui.SameLine();
            if (ImGui.Button("Cancel")) { _resetTiersOpen = false; ImGui.CloseCurrentPopup(); }
            ImGui.EndPopup();
        }
        HelpMarker("Default = Meta Profit (Locus + Doryani S). Tiers are profile-backed - presets in Strategy replace them too.");

        ImGui.SeparatorText("Target Rooms");

        ImGui.SetNextItemWidth(220f);
        ImGui.InputText("##roomSearch", ref _roomSearchFilter, 64, ImGuiInputTextFlags.AutoSelectAll);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(110f);
        string[] filterLabels = { "All", "S", "A", "B", "C", "Untiered" };
        if (ImGui.BeginCombo("##roomTierFilter", filterLabels[_roomTierFilter]))
        {
            for (var i = 0; i < filterLabels.Length; i++)
            {
                if (ImGui.Selectable(filterLabels[i], i == _roomTierFilter))
                    _roomTierFilter = i;
                if (i == _roomTierFilter) ImGui.SetItemDefaultFocus();
            }
            ImGui.EndCombo();
        }
        HelpMarker("Filter by tier. Colours match Appearance -> Tier Frames. Counts: " + string.Join(", ", new[] { 4, 3, 2, 1, 0 }.Select(v => $"{TierLabels[v]}={Settings.RoomTierOverrides.Count(kv => kv.Value == v)}")));

        var allKeys = Settings.RoomTierOverrides.Keys.ToList();
        if (allKeys.Count == 0)
        {
            ImGui.TextDisabled("No overrides yet - re-enter area or toggle Enable.");
            return;
        }

        var entries = allKeys
            .Where(k => _roomTierFilter == 0 || Settings.RoomTierOverrides[k] == TierFilterToRank(_roomTierFilter))
            .Where(k => _roomSearchFilter.Length == 0 || k.Contains(_roomSearchFilter, StringComparison.OrdinalIgnoreCase) || (DefaultImportantRooms.FirstOrDefault(r => r.Tier1Name == k)?.Tier3Name.Contains(_roomSearchFilter, StringComparison.OrdinalIgnoreCase) ?? false))
            .OrderByDescending(k => Settings.RoomTierOverrides[k])
            .ThenBy(k => k)
            .ToList();

        ImGui.Text($"{entries.Count} rooms shown");
        if (ImGui.BeginChild("##roomList", new Vector2(0, 300)))
        {
            foreach (var name in entries)
            {
                var v = Settings.RoomTierOverrides[name];
                var rank = (TierRank)v;
                var color = rank switch { TierRank.S => Settings.STierColor.Value, TierRank.A => Settings.ATierColor.Value, TierRank.B => Settings.BTierColor.Value, TierRank.C => Settings.CTierColor.Value, _ => Settings.UntieredRoomColor.Value };
                var info = DefaultImportantRooms.FirstOrDefault(r => r.Tier1Name == name);

                ImGui.PushStyleColor(ImGuiCol.Text, ToImGuiColor(color));
                ImGui.SetNextItemWidth(110f);
                var preview = TierLabels[Math.Clamp(v, 0, 4)];
                // color-coded combo preview like SimpleInformation's gem colour
                if (ImGui.BeginCombo($"##tier_{name}", preview))
                {
                    for (int i = 0; i < TierLabels.Length; i++)
                    {
                        var tierColor = (TierRank)i switch { TierRank.S => Settings.STierColor.Value, TierRank.A => Settings.ATierColor.Value, TierRank.B => Settings.BTierColor.Value, TierRank.C => Settings.CTierColor.Value, _ => Settings.UntieredRoomColor.Value };
                        ImGui.PushStyleColor(ImGuiCol.Text, ToImGuiColor(tierColor));
                        bool sel = i == v;
                        if (ImGui.Selectable(TierLabels[i], sel))
                            Settings.RoomTierOverrides[name] = i;
                        ImGui.PopStyleColor();
                        if (sel) ImGui.SetItemDefaultFocus();
                    }
                    ImGui.EndCombo();
                }
                ImGui.PopStyleColor();

                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Text, ToImGuiColor(color));
                ImGui.Text(name);
                ImGui.PopStyleColor();
                if (info != null)
                {
                    ImGui.SameLine();
                    ImGui.TextDisabled($"-> {info.Tier3Name}");
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip($"{info.Description}");
                }
            }
            ImGui.EndChild();
        }

        ImGui.SeparatorText("Stats");
        ImGui.Text($"S={Settings.RoomTierOverrides.Count(kv => kv.Value == 4)}  A={Settings.RoomTierOverrides.Count(kv => kv.Value == 3)}  B={Settings.RoomTierOverrides.Count(kv => kv.Value == 2)}  C={Settings.RoomTierOverrides.Count(kv => kv.Value == 1)}  Untiered={Settings.RoomTierOverrides.Count(kv => kv.Value == 0)}");
        ImGui.TextDisabled("Tip: S lines get the Purple/whatever frame on the map and score tier*Weight + bonuses in Strategy.");
    }

    private static int TierFilterToRank(int filterIdx) => filterIdx switch { 1 => 4, 2 => 3, 3 => 2, 4 => 1, 5 => 0, _ => 0 };

    private void DrawAppearanceTab()
    {
        ImGui.SeparatorText("Tier Frames");
        DrawToggle("Draw tier frames", Settings.EnableTierFrames, "Frames around each temple room colored by weighted tier.");
        DrawSlider("Frame thickness", Settings.FrameThickness);
        DrawColor("S Tier", Settings.STierColor, "Corruption Locus, Doryani, Nexus, Vault by default.");
        DrawColor("A Tier", Settings.ATierColor);
        DrawColor("B Tier", Settings.BTierColor);
        DrawColor("C Tier", Settings.CTierColor);
        DrawColor("Untiered", Settings.UntieredRoomColor, "Passageways, Hall of Mettle etc.");
        ImGui.SeparatorText("Recommendation Look");
        DrawToggle("Highlight recommended door suggestion", Settings.EnableRecommendation);
        DrawSlider("Recommended thickness", Settings.RecommendedThickness);
        DrawColor("Recommended highlight (sel rect/badge)", Settings.RecommendedChoiceHighlight);
        DrawToggle("Show tiny badge under architect", Settings.EnableBadge);
    }

    private void DrawConnectionsTab()
    {
        ImGui.SeparatorText("Connections");
        DrawToggle("Draw connections for selected room", Settings.EnableConnections, "Lines from selected room to its 6 walls (from diamond tooltips).");
        DrawColor("Locked door (red)", Settings.LockedDoorColor, "Locked walls requiring a Stone of Passage.");
        DrawColor("Suggested door (green)", Settings.SuggestedDoorColor, "Recommended locked wall to open next (weighted).");
        ImGui.TextDisabled("Unlocked connections are already open, so no line is drawn.");
        ImGui.Spacing();
        ImGui.TextWrapped("Green pick is highest weighted locked door by your Strategy weights (Scarcity + Tier).");
    }

    // helpers - MercScanner-style
    private static void DrawStatusCard(string title, string value, SharpDX.Color color, float width)
    {
        ImGui.BeginChild($"##card_{title}", new Vector2(width, 58f), ImGuiChildFlags.Border, ImGuiWindowFlags.NoScrollbar);
        ImGui.PushStyleColor(ImGuiCol.Text, ToImGuiColor(color));
        ImGui.Text(title);
        ImGui.PopStyleColor();
        ImGui.TextDisabled(value);
        ImGui.EndChild();
    }

    private static void HelpMarker(string text)
    {
        ImGui.SameLine();
        ImGui.TextDisabled("(?)");
        if (ImGui.IsItemHovered()) ImGui.SetTooltip(text);
    }

    private static void DrawToggle(string label, ToggleNode node, string help = null)
    {
        var v = node.Value;
        if (ImGui.Checkbox(label, ref v)) node.Value = v;
        if (help != null) HelpMarker(help);
    }

    private static void DrawColor(string label, ColorNode node, string help = null)
    {
        var c = node.Value;
        var v = new Vector4(c.R / 255f, c.G / 255f, c.B / 255f, c.A / 255f);
        if (ImGui.ColorEdit4(label, ref v))
            node.Value = new SharpDX.Color((byte)(v.X * 255f), (byte)(v.Y * 255f), (byte)(v.Z * 255f), (byte)(v.W * 255f));
        if (help != null) HelpMarker(help);
    }

    private static void DrawSlider(string label, RangeNode<int> node, string help = null)
    {
        var v = node.Value;
        if (ImGui.SliderInt($"{label}: {v}##{label}", ref v, node.Min, node.Max)) node.Value = v;
        if (help != null) HelpMarker(help);
    }

    private static void Section(string title) => ImGui.SeparatorText(title);
    private static Vector4 ToImGuiColor(SharpDX.Color c) => new(c.R / 255f, c.G / 255f, c.B / 255f, c.A / 255f);
}
