using System.Collections.Generic;
using ExileCore.Shared.Attributes;
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;

namespace IncursionHelper;

public class IncursionHelperSettings : ISettings
{
    public ToggleNode Enable { get; set; } = new ToggleNode(false);

    [Menu("Draw tier frames on temple rooms", "Frames around each temple room colored by weighted tier.")]
    public ToggleNode EnableTierFrames { get; set; } = new ToggleNode(true);

    [Menu("Highlight recommended architect", "Thick cyan frame + badge on the winning architect button; hover for one-line reason.")]
    public ToggleNode EnableRecommendation { get; set; } = new ToggleNode(true);

    [Menu("Show tiny badge under recommendation", "Short UP/NEW label with target name under the recommended choice.")]
    public ToggleNode EnableBadge { get; set; } = new ToggleNode(true);

    [Menu("Frame thickness", "Border width for tier frames on the temple map (1-5).")]
    public RangeNode<int> FrameThickness { get; set; } = new RangeNode<int>(2, 1, 5);

    [Menu("Recommended thickness", "Border width for the recommended door/architect highlight (1-6).")]
    public RangeNode<int> RecommendedThickness { get; set; } = new RangeNode<int>(3, 1, 6);

    // --- Strategy weights (customizable, profile-backed) - MercScanner + SimpleInformation formatting ---
    [Menu("Tier weight multiplier", "Scoring: tier rank * this. Higher makes S-tier dominate picks.")]
    public RangeNode<int> WeightTier { get; set; } = new RangeNode<int>(10, 0, 20);

    [Menu("Upgrade bonus", "Bonus added when the architect will upgrade (Kill to upgrade to T2/T3).")]
    public RangeNode<int> WeightUpgrade { get; set; } = new RangeNode<int>(2, 0, 10);

    [Menu("Scarcity bonus (target T3 not on map)", "Bonus when the resulting T3 is not yet present on the current map.")]
    public RangeNode<int> WeightScarcity { get; set; } = new RangeNode<int>(1, 0, 5);

    [Menu("Early-game Change bonus (remaining >=9)", "Bonus for Kill to change when many incursions remain (seeding phase).")]
    public RangeNode<int> WeightChangeEarly { get; set; } = new RangeNode<int>(2, 0, 10);

    [Menu("Late-game Upgrade bonus (remaining <=3)", "Bonus for Kill to upgrade when few incursions remain (finishing phase).")]
    public RangeNode<int> WeightUpgradeLate { get; set; } = new RangeNode<int>(4, 0, 10);

    [Menu("S-tier bonus", "Extra nudge for S-tier lines even early in the temple.")]
    public RangeNode<int> WeightSTierBonus { get; set; } = new RangeNode<int>(1, 0, 5);

    [Menu("Untiered penalty", "Negative score for untiered/bulk lines (e.g. Antechamber, Passageways).")]
    public RangeNode<int> WeightUntieredPenalty { get; set; } = new RangeNode<int>(5, 0, 10);

    // Per-room tier overrides: T1Name -> TierRank (0=Untiered,1=C,2=B,3=A,4=S)
    public Dictionary<string, int> RoomTierOverrides { get; set; } = new();

    [Menu("S-Tier Room Color", "Corruption Locus, Doryani, Temple Nexus, Vault by default.")]
    public ColorNode STierColor { get; set; } = new ColorNode(SharpDX.Color.Purple);

    [Menu("A-Tier Room Color", "High-value farms like Apex, Anomaly, Legion, Strongboxes.")]
    public ColorNode ATierColor { get; set; } = new ColorNode(SharpDX.Color.Green);

    [Menu("B-Tier Room Color", "Solid mid-value rooms.")]
    public ColorNode BTierColor { get; set; } = new ColorNode(SharpDX.Color.Yellow);

    [Menu("C-Tier Room Color", "Low-value/niche rooms.")]
    public ColorNode CTierColor { get; set; } = new ColorNode(SharpDX.Color.Red);

    [Menu("Untiered Room Color", "Bulk corridors like Antechamber, Passageways.")]
    public ColorNode UntieredRoomColor { get; set; } = new ColorNode(SharpDX.Color.Gray);

    [Menu("Recommended Choice Highlight", "Thick frame for recommended architect button and selected-room highlight.")]
    public ColorNode RecommendedChoiceHighlight { get; set; } = new ColorNode(SharpDX.Color.Cyan);

    [Menu("Draw connections for selected room", "Centre-to-centre lines from selected room to its 6 walls (from diamond tooltips).")]
    public ToggleNode EnableConnections { get; set; } = new ToggleNode(true);

    [Menu("Locked door color (red)", "Locked walls requiring a Stone of Passage.")]
    public ColorNode LockedDoorColor { get; set; } = new ColorNode(SharpDX.Color.Red);

    [Menu("Suggested door to open (green)", "The single weighted pick among locked walls.")]
    public ColorNode SuggestedDoorColor { get; set; } = new ColorNode(SharpDX.Color.Lime);
}
