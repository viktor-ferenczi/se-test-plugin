using System.Collections.Generic;
using PluginSdk.Config;
using PluginSdk.Tools;
using Shared.Config;
using VRage;
using VRageMath;

namespace ServerPlugin.Config;

// =====================================================================
//  Enums used by some options (single and inside lists / structs)
// =====================================================================

public enum Quality
{
    [EnumCaption("Low quality")] Low,
    [EnumCaption("Medium quality")] Medium,
    High, // caption falls back to "High"
}

public enum Difficulty
{
    Peaceful,
    [EnumCaption("Easy mode")] Easy,
    Normal,
    [EnumCaption("Hard mode")] Hard,
}

// =====================================================================
//  Struct values used by some options.
//  Struct members may be scalars, enums, List<T>, SerializableDictionary
//  or another struct (one nesting level here). VRage value types are NOT
//  allowed inside a struct — only as top-level options.
// =====================================================================

// A flat struct of scalars. Used standalone, as a list element and as a
// dictionary value below.
public struct IntRange
{
    [StructMember("Lower bound (inclusive)")] public int Min;
    [StructMember("Upper bound (inclusive)")] public int Max;
}

// Struct containing a nested struct (struct-in-struct, one level) and an
// enum member (enum-in-struct). Used standalone and as a List element.
public struct RankReward
{
    [StructMember("Rank level")] public int Level;

    // The row caption shown in the List<RankReward> editor.
    [StructMember("Rank name"), StructCaption] public string Label { get; set; }

    [StructMember("Score span that earns this rank")] public IntRange Span; // struct in struct
    [StructMember("Minimum quality to qualify")] public Quality MinQuality; // enum in struct
    [StructMember("Whether the reward is granted")] public bool Enabled;
}

// Struct containing a List (list-in-struct) and a SerializableDictionary
// (dict-in-struct). Used standalone and as a List element.
public struct Squad
{
    [StructMember("Squad name"), StructCaption] public string Name { get; set; }
    [StructMember("Identity ids of the squad members")] public List<long> MemberIds; // list in struct
    [StructMember("Per-player score")] public SerializableDictionary<string, int> Scores; // dict in struct
}

// Struct used as a tree node: ParentId points at another element's Id.
public struct MenuNode
{
    [StructMember("Unique node id")] public int Id;
    [StructMember("Parent node id, -1 for a root node")] public int ParentId; // tree shape
    [StructMember("Menu entry label"), StructCaption] public string Label { get; set; }
}

// =====================================================================
//  The configuration class. Derives from PluginSdk PluginConfig (the only
//  configuration mechanism this server plugin uses) and additionally
//  implements the Shared IPluginConfig so the existing Harmony patch
//  scaffolding (Plugin.Common.Config) keeps gating on Enabled /
//  DetectCodeChanges. INotifyPropertyChanged is supplied by PluginConfig.
// =====================================================================

[Tab("general", caption: "General")]
[Tab("types", caption: "Data types")]
[Tab("collections", caption: "Collections")]
[Tab("advanced", caption: "Advanced")]

[Section("core", parent: "general", caption: "Core")]
[Column("core-left", parent: "core", caption: "Left")]
[Column("core-right", parent: "core", caption: "Right")]

[Section("scalars", parent: "types", caption: "Scalars")]
[Column("scalars-left", parent: "scalars", caption: "Left")]
[Column("scalars-right", parent: "scalars", caption: "Right")]
[Section("math", parent: "types", caption: "Math / VRage value types")]
[Section("enums", parent: "types", caption: "Enums")]

[Section("lists", parent: "collections", caption: "Lists")]
[Section("dicts", parent: "collections", caption: "Dictionaries")]
[Section("structs", parent: "collections", caption: "Structs")]

[Section("trees", parent: "advanced", caption: "Tree editors")]
public class TestPluginConfig : PluginSdk.Config.PluginConfig, IPluginConfig
{
    // ---- Core / IPluginConfig -----------------------------------------

    [BoolOption("Enable the plugin", Parent = "core-left")]
    public bool Enabled { get; set => SetField(ref field, value); } = true;

    [BoolOption("Detect conflicting game code changes (disable on Proton/Wine)", Parent = "core-left")]
    public bool DetectCodeChanges { get; set => SetField(ref field, value); } = true;

    [StringOption(maxLength: 64, description: "Display name shown to players", Parent = "core-right")]
    public string ServerName { get; set => SetField(ref field, value); } = "Test Server";

    // ---- Scalars: bool / int / long / float / double / string ---------

    [BoolOption("Verbose logging", Parent = "scalars-left")]
    public bool Verbose { get; set => SetField(ref field, value); }

    [IntOption(1, 240, "Simulation ticks per second", Parent = "scalars-left")]
    public int TickRate { get; set => SetField(ref field, value); } = 60;

    [LongOption(0, long.MaxValue, "Maximum world size in bytes", Parent = "scalars-left")]
    public long MaxWorldSizeBytes { get; set => SetField(ref field, value); } = 1_073_741_824;

    [FloatOption(0f, 10f, "Inventory size multiplier", Parent = "scalars-right")]
    public float InventoryMultiplier { get; set => SetField(ref field, value); } = 1.0f;

    [DoubleOption(0.0, 4.0, "Gravity multiplier", Parent = "scalars-right")]
    public double GravityMultiplier { get; set => SetField(ref field, value); } = 1.0;

    [StringOption(maxLength: 32, pattern: @"^[A-Za-z0-9_-]+$", description: "World slug (letters, digits, _ and -)", Parent = "scalars-right")]
    public string WorldSlug { get; set => SetField(ref field, value); } = "default-world";

    // ---- VRage value types (top-level only) ---------------------------

    [ColorOption(ColorFormat.Rgb, "HUD accent colour (no alpha)", Parent = "math")]
    public Color HudColor { get; set => SetField(ref field, value); } = Color.Cyan;

    [ColorOption(ColorFormat.Rgba, "Trail tint (with alpha)", Parent = "math")]
    public Color TrailColor { get; set => SetField(ref field, value); }
        = new Color((byte)255, (byte)128, (byte)0, (byte)200);

    [Vector2DOption("Minimap centre offset", Parent = "math")]
    public Vector2D MinimapOffset { get; set => SetField(ref field, value); } = Vector2D.Zero;

    [Vector3DOption("World offset applied to all spawn points", Parent = "math")]
    public Vector3D WorldOffset { get; set => SetField(ref field, value); } = Vector3D.Zero;

    [Vector2IOption("Grid cell coordinate", Parent = "math")]
    public Vector2I GridCell { get; set => SetField(ref field, value); } = new Vector2I(1, 2);

    [Vector3IOption("Block coordinate", Parent = "math")]
    public Vector3I BlockCoord { get; set => SetField(ref field, value); } = new Vector3I(1, 2, 3);

    [DirectionOption("Default placement direction", Parent = "math")]
    public Base6Directions.Direction PlaceDirection { get; set => SetField(ref field, value); }
        = Base6Directions.Direction.Forward;

    [PositionAndOrientationOption("Default spawn pose", Parent = "math")]
    public MyPositionAndOrientation SpawnPose { get; set => SetField(ref field, value); }
        = new MyPositionAndOrientation(Vector3D.Zero, Vector3.Forward, Vector3.Up);

    // ---- Enums: single value and List<enum> ---------------------------

    [EnumOption("Render quality", Parent = "enums")]
    public Quality RenderQuality { get; set => SetField(ref field, value); } = Quality.Medium;

    [EnumOption("Difficulty preset", Parent = "enums")]
    public Difficulty Difficulty { get; set => SetField(ref field, value); } = Difficulty.Normal;

    [EnumOption("Quality presets to rotate through", Parent = "enums")]
    public List<Quality> QualityPresets { get; set => SetField(ref field, value); }
        = new List<Quality> { Quality.Low, Quality.High };

    // ---- Lists of scalars ---------------------------------------------

    [ListOption(description: "Free-form tags", Parent = "lists")]
    public List<string> Tags { get; set => SetField(ref field, value); }
        = new List<string> { "pvp", "survival" };

    [ListOption(maxCount: 16, description: "Whitelisted UDP ports", Parent = "lists")]
    public List<int> Ports { get; set => SetField(ref field, value); }
        = new List<int> { 27016, 27017 };

    // ---- Dictionaries: string / int / long keys, scalar values --------

    [DictOption(description: "Per-player score (string key, int value)", Parent = "dicts")]
    public SerializableDictionary<string, int> PlayerScores { get; set => SetField(ref field, value); }
        = new SerializableDictionary<string, int> { ["alice"] = 100, ["bob"] = 50 };

    [DictOption(description: "Faction names by id (int key, string value)", Parent = "dicts")]
    public SerializableDictionary<int, string> FactionNames { get; set => SetField(ref field, value); }
        = new SerializableDictionary<int, string> { [1] = "Engineers", [2] = "Pirates" };

    [DictOption(description: "Resource yield by ore id (long key, double value)", Parent = "dicts")]
    public SerializableDictionary<long, double> OreYields { get; set => SetField(ref field, value); }
        = new SerializableDictionary<long, double> { [1001L] = 1.5, [1002L] = 0.75 };

    // ---- Struct as a dictionary value (struct-in-dict) ----------------

    [DictOption(description: "Allowed port range per region (struct value)", Parent = "dicts")]
    public SerializableDictionary<string, IntRange> RegionPortRanges { get; set => SetField(ref field, value); }
        = new SerializableDictionary<string, IntRange>
        {
            ["eu"] = new IntRange { Min = 27000, Max = 27099 },
            ["us"] = new IntRange { Min = 27100, Max = 27199 },
        };

    // ---- Structs: single, nested, with list/dict members --------------

    // Plain struct of scalars.
    [StructOption(description: "Default port range", Parent = "structs")]
    public IntRange DefaultPortRange { get; set => SetField(ref field, value); }
        = new IntRange { Min = 27016, Max = 27066 };

    // Struct containing a nested struct + an enum member.
    [StructOption(description: "Starter rank reward (struct in struct + enum in struct)", Parent = "structs")]
    public RankReward StarterRank { get; set => SetField(ref field, value); }
        = new RankReward
        {
            Level = 1,
            Label = "Recruit",
            Span = new IntRange { Min = 0, Max = 999 },
            MinQuality = Quality.Low,
            Enabled = true,
        };

    // Struct containing a list + a dictionary.
    [StructOption(description: "Default squad (list in struct + dict in struct)", Parent = "structs")]
    public Squad DefaultSquad { get; set => SetField(ref field, value); }
        = new Squad
        {
            Name = "Alpha",
            MemberIds = new List<long> { 1, 2, 3 },
            Scores = new SerializableDictionary<string, int> { ["alice"] = 10 },
        };

    // ---- Lists of structs (struct-in-list) ----------------------------

    [StructOption(description: "Rank reward tiers (struct in list)", Parent = "structs")]
    public List<RankReward> RankRewards { get; set => SetField(ref field, value); }
        = new List<RankReward>
        {
            new RankReward { Level = 1, Label = "Recruit", Span = new IntRange { Min = 0, Max = 999 }, MinQuality = Quality.Low, Enabled = true },
            new RankReward { Level = 2, Label = "Veteran", Span = new IntRange { Min = 1000, Max = 4999 }, MinQuality = Quality.Medium, Enabled = true },
        };

    [StructOption(description: "Configured squads (struct in list, each with list + dict members)", Parent = "structs")]
    public List<Squad> Squads { get; set => SetField(ref field, value); }
        = new List<Squad>
        {
            new Squad { Name = "Alpha", MemberIds = new List<long> { 1, 2 }, Scores = new SerializableDictionary<string, int> { ["alice"] = 10 } },
            new Squad { Name = "Bravo", MemberIds = new List<long> { 3, 4 }, Scores = new SerializableDictionary<string, int> { ["bob"] = 20 } },
        };

    // ---- Tree-shaped list of structs ----------------------------------

    [ListOption(description: "Context menu tree", TreeParentField = nameof(MenuNode.ParentId), Parent = "trees")]
    public List<MenuNode> MenuTree { get; set => SetField(ref field, value); }
        = new List<MenuNode>
        {
            new MenuNode { Id = 0, ParentId = -1, Label = "Root" },
            new MenuNode { Id = 1, ParentId = 0, Label = "Build" },
            new MenuNode { Id = 2, ParentId = 0, Label = "Admin" },
            new MenuNode { Id = 3, ParentId = 2, Label = "Kick player" },
        };
}
