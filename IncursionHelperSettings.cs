using ExileCore.Shared.Attributes;
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;

namespace IncursionHelper;

public class IncursionHelperSettings : ISettings
{
    public ToggleNode Enable { get; set; } = new ToggleNode(false);

    [Menu("S-Tier Room Color")]
    public ColorNode STierColor { get; set; } = new ColorNode(SharpDX.Color.Purple);

    [Menu("A-Tier Room Color")]
    public ColorNode ATierColor { get; set; } = new ColorNode(SharpDX.Color.Green);

    [Menu("B-Tier Room Color")]
    public ColorNode BTierColor { get; set; } = new ColorNode(SharpDX.Color.Yellow);

    [Menu("C-Tier Room Color")]
    public ColorNode CTierColor { get; set; } = new ColorNode(SharpDX.Color.Red);

    [Menu("Untiered Room Color")]
    public ColorNode UntieredRoomColor { get; set; } = new ColorNode(SharpDX.Color.Gray);

    [Menu("Recommended Choice Highlight")]
    public ColorNode RecommendedChoiceHighlight { get; set; } = new ColorNode(SharpDX.Color.Cyan);
}