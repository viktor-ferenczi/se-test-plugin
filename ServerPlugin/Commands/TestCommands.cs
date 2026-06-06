using System.Collections.Generic;
using PluginSdk;
using PluginSdk.Commands;
using ServerPlugin.Config;
using VRage.Game.ModAPI;
using VRageMath;

namespace ServerPlugin.Commands;

/// <summary>
/// Chat commands exposed under <c>!test</c>. Demonstrates the PluginSdk
/// command system: argument binding, permissions, the various reply shapes,
/// the in-place collection mutation pitfall (NotifyChanged), and the
/// ServerControl lifecycle facade. The host auto-generates <c>!test</c> and
/// <c>!test help</c>; each command supplies a third <c>helpText</c> argument so
/// <c>!test help &lt;cmd&gt;</c> prints per-sub-command usage beyond the
/// auto-generated syntax line.
/// </summary>
[CommandRoot("test", "TestPlugin", "PluginSdk feature test commands")]
public sealed class TestCommands : CommandModule
{
    private static TestPluginConfig Config => Plugin.TestConfig;

    // IEnumerable<string> reply: one private line per item.
    [Command("info", "Show a summary of the current configuration",
        "Prints a multi-line snapshot of every config value (enabled state, server "
        + "name, tick rate, quality, difficulty, tags, ports and collection counts). "
        + "Takes no arguments and changes nothing. Example: !test info")]
    [Permission(MyPromoteLevel.None)]
    public IEnumerable<string> Info()
    {
        var c = Config;
        yield return $"Enabled:     {c.Enabled}";
        yield return $"ServerName:  {c.ServerName}";
        yield return $"TickRate:    {c.TickRate}";
        yield return $"Quality:     {c.RenderQuality}";
        yield return $"Difficulty:  {c.Difficulty}";
        yield return $"Tags:        {string.Join(", ", c.Tags)}";
        yield return $"Ports:       {string.Join(", ", c.Ports)}";
        yield return $"Squads: {c.Squads.Count}, Ranks: {c.RankRewards.Count}, Menu nodes: {c.MenuTree.Count}";
    }

    // bool argument (true/false, yes/no, on/off, 1/0). Scalar setter notifies.
    [Command("enable", "Enable or disable the plugin",
        "<on> is a boolean accepting true/false, yes/no, on/off or 1/0. The new value "
        + "is persisted on the next save. Example: !test enable off")]
    [Permission(MyPromoteLevel.Admin)]
    public string Enable(bool on)
    {
        Config.Enabled = on;
        return $"Enabled set to {on}.";
    }

    // int argument with manual range validation -> CommandReply.Error/Ok.
    [Command("tickrate", "Set the simulation tick rate (1..240)",
        "<value> is the target ticks per second and must be between 1 and 240 "
        + "inclusive; out-of-range values are rejected with an error. "
        + "Example: !test tickrate 60")]
    [Permission(MyPromoteLevel.Admin)]
    public CommandReply SetTickRate(int value)
    {
        if (value < 1 || value > 240)
            return CommandReply.Error("Tick rate must be between 1 and 240.");
        Config.TickRate = value;
        return CommandReply.Ok($"Tick rate set to {value}.");
    }

    // enum argument, bound case-insensitively by name.
    [Command("quality", "Set render quality (Low / Medium / High)",
        "<quality> is one of Low, Medium or High, matched case-insensitively by name. "
        + "Example: !test quality high")]
    [Permission(MyPromoteLevel.Admin)]
    public string SetQuality(Quality quality)
    {
        Config.RenderQuality = quality;
        return $"Render quality set to {quality}.";
    }

    // Sub-namespace "tag" with two leaf commands. Demonstrates the list
    // in-place mutation pitfall: mutate, then NotifyChanged.
    [Command("tag add", "Add a tag (in-place list mutation + NotifyChanged)",
        "<tag> is appended to the tag list. Quote multi-word tags to keep spaces. "
        + "Example: !test tag add \"PvP Zone\"")]
    [Permission(MyPromoteLevel.Moderator)]
    public string TagAdd(string tag)
    {
        Config.Tags.Add(tag);
        Config.NotifyChanged(nameof(Config.Tags));
        return $"Added tag '{tag}'. Now {Config.Tags.Count} tag(s).";
    }

    [Command("tag clear", "Remove all tags",
        "Empties the tag list. Takes no arguments. Example: !test tag clear")]
    [Permission(MyPromoteLevel.Moderator)]
    public string TagClear()
    {
        Config.Tags.Clear();
        Config.NotifyChanged(nameof(Config.Tags));
        return "Cleared all tags.";
    }

    // Struct scalar field: copy, edit, reassign (the setter notifies).
    [Command("portrange", "Set the default port range (min max)",
        "<min> and <max> are the inclusive bounds of the default port range, given as "
        + "two integers. Example: !test portrange 27015 27020")]
    [Permission(MyPromoteLevel.Admin)]
    public string SetPortRange(int min, int max)
    {
        var range = Config.DefaultPortRange;
        range.Min = min;
        range.Max = max;
        Config.DefaultPortRange = range;
        return $"Default port range set to {min}..{max}.";
    }

    [Command("save", "Save the configuration to disk",
        "Writes the current configuration to its XML file on disk. Takes no arguments. "
        + "Example: !test save")]
    [Permission(MyPromoteLevel.Admin)]
    public string Save()
    {
        Plugin.TrySaveConfig();
        return "Configuration saved.";
    }

    // ServerControl facade: save the world without quitting.
    [Command("saveworld", "Save the game world (ServerControl.SaveWorld)",
        "Saves the current game world without quitting the server. Takes no arguments "
        + "and requires a loaded session. Example: !test saveworld")]
    [Permission(MyPromoteLevel.Admin)]
    public string SaveWorld()
        => ServerControl.SaveWorld() ? "World saved." : "Could not save the world (no session loaded?).";

    // ServerControl facade: re-read the dedicated config.
    [Command("reloadconfig", "Reload the dedicated server config (ServerControl.ReloadConfig)",
        "Re-reads the dedicated server configuration from disk. Takes no arguments. "
        + "Example: !test reloadconfig")]
    [Permission(MyPromoteLevel.Admin)]
    public string ReloadConfig()
        => ServerControl.ReloadConfig() ? "Dedicated config reloaded." : "Could not reload the dedicated config.";

    // params string[] captures all remaining words; broadcast reply.
    [Command("announce", "Broadcast a message to everyone",
        "All remaining words form a message broadcast to every player in yellow, "
        + "attributed to TestPlugin. At least one word is required. "
        + "Example: !test announce Server restart in 5 minutes")]
    [Permission(MyPromoteLevel.Admin)]
    public CommandReply Announce(params string[] words)
    {
        if (words.Length == 0)
            return CommandReply.Error("Nothing to announce.");
        return CommandReply.Announce(string.Join(" ", words), Color.Yellow).WithAuthor("TestPlugin");
    }
}
