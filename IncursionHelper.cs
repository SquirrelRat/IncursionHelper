using ExileCore;
using ExileCore.PoEMemory;
using ExileCore.PoEMemory.Elements;
using ExileCore.PoEMemory.MemoryObjects;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ExileCore.Shared;
using ExileCore.Shared.Nodes;
using Newtonsoft.Json;
using System.IO;
using Vector2 = System.Numerics.Vector2;

namespace IncursionHelper;

public class IncursionHelper : BaseSettingsPlugin<IncursionHelperSettings>
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
            var trimmedRoomName = roomName.Trim();
            return trimmedRoomName.Equals(Tier1Name, StringComparison.OrdinalIgnoreCase) ||
                   trimmedRoomName.Equals(Tier2Name, StringComparison.OrdinalIgnoreCase) ||
                   trimmedRoomName.Equals(Tier3Name, StringComparison.OrdinalIgnoreCase);
        }

        [JsonIgnore]
        public int TierValue
        {
            get
            {
                return Tier switch
                {
                    "S" => 4,
                    "A" => 3,
                    "B" => 2,
                    "C" => 1,
                    _ => 0
                };
            }
        }
    }

    private static List<RoomInfo> ImportantRooms;

    private static readonly List<RoomInfo> DefaultImportantRooms = new List<RoomInfo>
    {
        new RoomInfo("Corruption Chamber", "Catalyst Of Corruption", "Locus Of Corruption", "S", "Altar of Corruption for double-corrupting items.", "S-Tier Room"),
        new RoomInfo("Gemcutter's Workshop", "Department Of Thaumaturgy", "Doryani's Institute", "S", "Lapidary Lens for double-corrupting gems.", "S-Tier Room"),
        new RoomInfo("Shrine Of Empowerment", "Sanctum Of Unity", "Temple Nexus", "S", "Upgrades adjacent rooms, increases item level.", "S-Tier Room"),
        new RoomInfo("Vault", "Treasury", "Wealth Of The Vaal", "S", "Contains valuable currency chests.", "S-Tier Room"),

        new RoomInfo("Sacrificial Chamber", "Hall Of Offerings", "Apex Of Ascension", "A", "Gamble unique items for potentially better ones.", "A-Tier Room"),
        new RoomInfo("Jeweller's Workshop", "Jewellery Forge", "Glittering Halls", "A", "Contains rare jewellery chests.", "A-Tier Room"),
        new RoomInfo("Hall Of Mettle", "Hall Of Heroes", "Hall Of Legends", "A", "Access to Timeless Monoliths for Legion encounters.", "A-Tier Room"),
        new RoomInfo("Surveyor's Study", "Office Of Cartography", "Atlas Of Worlds", "A", "Contains maps and map-related currency.", "A-Tier Room"),
        new RoomInfo("Splinter Research Lab", "Breach Containment Chamber", "House Of The Others", "A", "Provides Breach encounters for splinters and items.", "A-Tier Room"),

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

    private string ExtractRoomNameFromRewardString(string rewardString)
    {
        if (string.IsNullOrWhiteSpace(rewardString)) return string.Empty;

        int startIndex = rewardString.IndexOf("(KILL TO CHANGE TO ", StringComparison.OrdinalIgnoreCase);
        if (startIndex == -1)
        {
            startIndex = rewardString.IndexOf("(KILL TO UPGRADE TO ", StringComparison.OrdinalIgnoreCase);
            if (startIndex == -1) return string.Empty;
            startIndex += "(KILL TO UPGRADE TO ".Length;
        }
        else
        {
            startIndex += "(KILL TO CHANGE TO ".Length;
        }

        int endIndex = rewardString.IndexOf(")}", startIndex, StringComparison.Ordinal);
        if (endIndex == -1) return string.Empty;

        return rewardString.Substring(startIndex, endIndex - startIndex).Trim();
    }

    private string ExtractRoomNameFromConnectionTooltip(string tooltipText)
    {
        if (string.IsNullOrWhiteSpace(tooltipText)) return string.Empty;

        int startIndex = tooltipText.IndexOf("LOCKED DOOR TO ", StringComparison.OrdinalIgnoreCase);
        if (startIndex == -1) return string.Empty;
        startIndex += "LOCKED DOOR TO ".Length;

        int endIndex = tooltipText.IndexOf("\n", startIndex, StringComparison.Ordinal);
        if (endIndex == -1) endIndex = tooltipText.Length;

        return tooltipText.Substring(startIndex, endIndex - startIndex).Trim();
    }

    private HashSet<string> GetVisibleRoomNames(Element incursionRooms, string reward1, string reward2)
    {
        var visibleRooms = new HashSet<string>();

        foreach (var item in incursionRooms.Children)
        {
            var roomNameElement = item.GetChildFromIndices(0, 0);
            if (roomNameElement != null && !string.IsNullOrWhiteSpace(roomNameElement.Text))
            {
                visibleRooms.Add(roomNameElement.Text.Trim());
            }
        }

        if (!string.IsNullOrWhiteSpace(reward1))
        {
            visibleRooms.Add(reward1);
        }
        if (!string.IsNullOrWhiteSpace(reward2))
        {
            visibleRooms.Add(reward2);
        }

        return visibleRooms;
    }
    public override bool Initialise()
    {
        var roomDataPath = Path.Combine(ConfigDirectory, "RoomData.json");
        if (File.Exists(roomDataPath))
        {
            try
            {
                var json = File.ReadAllText(roomDataPath);
                ImportantRooms = JsonConvert.DeserializeObject<List<RoomInfo>>(json);
                LogMessage($"Loaded {ImportantRooms.Count} Incursion rooms from {roomDataPath}");
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
        return true;
    }

    public override void AreaChange(AreaInstance area)
    {
    }

    public override Job Tick()
    {
        return null;
    }

    private void ProcessCurrentRooms(Element incursionRooms)
    {
        foreach (var item in incursionRooms.Children)
        {
            if (item == null || item.ChildCount == 0 || item.GetChildFromIndices(0, 0) == null || item.GetChildFromIndices(0, 0).Text == null) continue;

            var roomName = item.GetChildFromIndices(0, 0).Text.Trim();
            RoomInfo currentRoomInfo = ImportantRooms.FirstOrDefault(r => r.ContainsRoom(roomName));

            ColorNode color = Settings.CTierColor;

            if (currentRoomInfo != null)
            {
                if (currentRoomInfo.Tier == "C" && currentRoomInfo.TierValue == 0)
                {
                    color = Settings.UntieredRoomColor;
                }
                else if (currentRoomInfo.Tier == "S") color = Settings.STierColor;
                else if (currentRoomInfo.Tier == "A") color = Settings.ATierColor;
                else if (currentRoomInfo.Tier == "B") color = Settings.BTierColor;
            }
            Graphics.DrawFrame(item.GetChildFromIndices(0, 0).GetClientRectCache, color, 3);
        }
    }

    private (string bestRewardRoomName, Element bestRewardElement) ProcessRewardChoices(string reward1Raw, string reward2Raw)
    {
        string bestRewardRoomName = string.Empty;
        Element bestRewardElement = null;

        string reward1 = ExtractRoomNameFromRewardString(reward1Raw);
        string reward2 = ExtractRoomNameFromRewardString(reward2Raw);

        RoomInfo reward1RoomInfo = ImportantRooms.FirstOrDefault(r => r.ContainsRoom(reward1));
        RoomInfo reward2RoomInfo = ImportantRooms.FirstOrDefault(r => r.ContainsRoom(reward2));

        if (reward1RoomInfo == null) reward1RoomInfo = new RoomInfo(reward1, reward1, reward1, "C", "Unknown Room");
        if (reward2RoomInfo == null) reward2RoomInfo = new RoomInfo(reward2, reward2, reward2, "C", "Unknown Room");

        Element reward1Element = IncursionPanel.GetChildFromIndices(3, 13, 3);
        Element reward2Element = IncursionPanel.GetChildFromIndices(3, 13, 4);

        ColorNode reward1Color = Settings.CTierColor;
        ColorNode reward2Color = Settings.CTierColor;

        if (reward1RoomInfo.Tier == "S") reward1Color = Settings.STierColor;
        else if (reward1RoomInfo.Tier == "A") reward1Color = Settings.ATierColor;
        else if (reward1RoomInfo.Tier == "B") reward1Color = Settings.BTierColor;

        if (reward2RoomInfo.Tier == "S") reward2Color = Settings.STierColor;
        else if (reward2RoomInfo.Tier == "A") reward2Color = Settings.ATierColor;
        else if (reward2RoomInfo.Tier == "B") reward2Color = Settings.BTierColor;

        const string KillToChangeTo = "KILL TO CHANGE TO";
        const string KillToUpgradeTo = "KILL TO UPGRADE TO";

        bool reward1IsChange = reward1Raw.Contains(KillToChangeTo);
        bool reward1IsUpgrade = reward1Raw.Contains(KillToUpgradeTo);
        bool reward2IsChange = reward2Raw.Contains(KillToChangeTo);
        bool reward2IsUpgrade = reward2Raw.Contains(KillToUpgradeTo);

        Element changeRoomIcon = IncursionPanel.GetChildFromIndices(3, 13, 0, 1);
        Element upgradeRoomIcon = IncursionPanel.GetChildFromIndices(3, 13, 0, 2);

        if (reward1RoomInfo.TierValue > reward2RoomInfo.TierValue)
        {
            Graphics.DrawFrame(reward1Element.GetClientRectCache, Settings.RecommendedChoiceHighlight, 3);
            Graphics.DrawFrame(reward2Element.GetClientRectCache, reward2Color, 2);
            bestRewardRoomName = reward1;
            bestRewardElement = reward1Element;

            if (reward1IsChange) Graphics.DrawFrame(changeRoomIcon.GetClientRectCache, Settings.RecommendedChoiceHighlight, 4);
            else if (reward1IsUpgrade) Graphics.DrawFrame(upgradeRoomIcon.GetClientRectCache, Settings.RecommendedChoiceHighlight, 4);
        }
        else if (reward2RoomInfo.TierValue > reward1RoomInfo.TierValue)
        {
            Graphics.DrawFrame(reward2Element.GetClientRectCache, Settings.RecommendedChoiceHighlight, 3);
            Graphics.DrawFrame(reward1Element.GetClientRectCache, reward1Color, 2);
            bestRewardRoomName = reward2;
            bestRewardElement = reward2Element;

            if (reward2IsChange) Graphics.DrawFrame(changeRoomIcon.GetClientRectCache, Settings.RecommendedChoiceHighlight, 4);
            else if (reward2IsUpgrade) Graphics.DrawFrame(upgradeRoomIcon.GetClientRectCache, Settings.RecommendedChoiceHighlight, 4);
        }
        else // Tiers are equal
        {
            Graphics.DrawFrame(reward1Element.GetClientRectCache, reward1Color, 3);
            Graphics.DrawFrame(reward2Element.GetClientRectCache, reward2Color, 3);
            bestRewardRoomName = reward1;
            bestRewardElement = reward1Element;
        }
        return (bestRewardRoomName, bestRewardElement);
    }

    private void DrawStrategyAdvisor(string bestRewardRoomName, Element bestRewardElement, Element incursionRooms, string reward1, string reward2)
    {
        if (bestRewardElement != null && !string.IsNullOrEmpty(bestRewardRoomName))
        {
            var incursionWindowRect = IncursionPanel.GetClientRectCache;
            var advisorPanelPos = new Vector2(incursionWindowRect.Left + 10, incursionWindowRect.Top + 10);

            var visibleRoomNames = GetVisibleRoomNames(incursionRooms, reward1, reward2);
            var futureGoals = new List<string>();

            foreach (var roomInfo in ImportantRooms)
            {
                if ((roomInfo.Tier == "S" || roomInfo.Tier == "A") && !visibleRoomNames.Contains(roomInfo.Tier3Name))
                {
                    futureGoals.Add(roomInfo.Tier3Name);
                }
            }

            var advisorTextLines = new List<string>
            {
                $"Best Choice: {CultureInfo.CurrentCulture.TextInfo.ToTitleCase(bestRewardRoomName.ToLower())}"
            };

            if (futureGoals.Any())
            {
                advisorTextLines.Add("Future Goals:");
                foreach (var roomInfo in ImportantRooms)
                {
                    if ((roomInfo.Tier == "S" || roomInfo.Tier == "A") && !visibleRoomNames.Contains(roomInfo.Tier3Name))
                    {
                        advisorTextLines.Add($"  - {CultureInfo.CurrentCulture.TextInfo.ToTitleCase(roomInfo.Tier3Name.ToLower())}: {roomInfo.Description}");
                    }
                }
            }
            else
            {
                advisorTextLines.Add("No immediate high-tier future goals.");
            }

            var fullAdvisorText = string.Join("\n", advisorTextLines);
            var textSize = Graphics.MeasureText(fullAdvisorText);
            var panelRect = new RectangleF(advisorPanelPos.X, advisorPanelPos.Y, textSize.X + 20, textSize.Y + 10);

            Graphics.DrawBox(panelRect, Color.Black);
            Graphics.DrawFrame(panelRect, Settings.RecommendedChoiceHighlight, 2);
            Graphics.DrawText(fullAdvisorText, new Vector2(panelRect.Left + 10, panelRect.Top + 5), Color.White);

            var advisorCenter = new Vector2(panelRect.Right, panelRect.Center.Y);
            var rewardElementCenter = bestRewardElement.GetClientRectCache.Center;

            Graphics.DrawLine(new Vector2(advisorCenter.X, advisorCenter.Y), new Vector2(rewardElementCenter.X, rewardElementCenter.Y), 2f, Settings.RecommendedChoiceHighlight.Value);
        }
    }

    public override void Render()
    {
        if (!GameController.IngameState.IngameUi.IncursionWindow.IsVisible) return;

        IncursionPanel = GameController.IngameState.IngameUi.IncursionWindow;
        var incursionRooms = IncursionPanel.GetChildFromIndices(3);

        ProcessCurrentRooms(incursionRooms);

        // Check if the reward choice elements exist before processing choices
        // This is a more reliable indicator than checking raw reward strings
        var reward1ChoiceElement = IncursionPanel.GetChildFromIndices(3, 13, 3);

        if (reward1ChoiceElement != null) // If the first reward choice element exists, assume choices are active
        {
            string reward1Raw = IncursionPanel.Reward1.ToUpper().Trim();
            string reward2Raw = IncursionPanel.Reward2.ToUpper().Trim();

            (string bestRewardRoomName, Element bestRewardElement) = ProcessRewardChoices(reward1Raw, reward2Raw);
            DrawStrategyAdvisor(bestRewardRoomName, bestRewardElement, incursionRooms, reward1Raw, reward2Raw);
        }
    }

    public override void EntityAdded(Entity entity)
    {
    }
}