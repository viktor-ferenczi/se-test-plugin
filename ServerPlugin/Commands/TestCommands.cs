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
/// <c>!test help</c>.
/// </summary>
[CommandRoot("test", "TestPlugin", "PluginSdk feature test commands")]
public sealed class TestCommands : CommandModule
{
    private static TestPluginConfig Config => Plugin.TestConfig;

    // IEnumerable<string> reply: one private line per item.
    [Command("info", "Show a summary of the current configuration")]
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
    [Command("enable", "Enable or disable the plugin")]
    [Permission(MyPromoteLevel.Admin)]
    public string Enable(bool on)
    {
        Config.Enabled = on;
        return $"Enabled set to {on}.";
    }

    // int argument with manual range validation -> CommandReply.Error/Ok.
    [Command("tickrate", "Set the simulation tick rate (1..240)")]
    [Permission(MyPromoteLevel.Admin)]
    public CommandReply SetTickRate(int value)
    {
        if (value < 1 || value > 240)
            return CommandReply.Error("Tick rate must be between 1 and 240.");
        Config.TickRate = value;
        return CommandReply.Ok($"Tick rate set to {value}.");
    }

    // enum argument, bound case-insensitively by name.
    [Command("quality", "Set render quality (Low / Medium / High)")]
    [Permission(MyPromoteLevel.Admin)]
    public string SetQuality(Quality quality)
    {
        Config.RenderQuality = quality;
        return $"Render quality set to {quality}.";
    }

    // Sub-namespace "tag" with two leaf commands. Demonstrates the list
    // in-place mutation pitfall: mutate, then NotifyChanged.
    [Command("tag add", "Add a tag (in-place list mutation + NotifyChanged)")]
    [Permission(MyPromoteLevel.Moderator)]
    public string TagAdd(string tag)
    {
        Config.Tags.Add(tag);
        Config.NotifyChanged(nameof(Config.Tags));
        return $"Added tag '{tag}'. Now {Config.Tags.Count} tag(s).";
    }

    [Command("tag clear", "Remove all tags")]
    [Permission(MyPromoteLevel.Moderator)]
    public string TagClear()
    {
        Config.Tags.Clear();
        Config.NotifyChanged(nameof(Config.Tags));
        return "Cleared all tags.";
    }

    // Struct scalar field: copy, edit, reassign (the setter notifies).
    [Command("portrange", "Set the default port range (min max)")]
    [Permission(MyPromoteLevel.Admin)]
    public string SetPortRange(int min, int max)
    {
        var range = Config.DefaultPortRange;
        range.Min = min;
        range.Max = max;
        Config.DefaultPortRange = range;
        return $"Default port range set to {min}..{max}.";
    }

    [Command("save", "Save the configuration to disk")]
    [Permission(MyPromoteLevel.Admin)]
    public string Save()
    {
        Plugin.TrySaveConfig();
        return "Configuration saved.";
    }

    // ServerControl facade: save the world without quitting.
    [Command("saveworld", "Save the game world (ServerControl.SaveWorld)")]
    [Permission(MyPromoteLevel.Admin)]
    public string SaveWorld()
        => ServerControl.SaveWorld() ? "World saved." : "Could not save the world (no session loaded?).";

    // ServerControl facade: re-read the dedicated config.
    [Command("reloadconfig", "Reload the dedicated server config (ServerControl.ReloadConfig)")]
    [Permission(MyPromoteLevel.Admin)]
    public string ReloadConfig()
        => ServerControl.ReloadConfig() ? "Dedicated config reloaded." : "Could not reload the dedicated config.";

    // params string[] captures all remaining words; broadcast reply.
    [Command("announce", "Broadcast a message to everyone")]
    [Permission(MyPromoteLevel.Admin)]
    public CommandReply Announce(params string[] words)
    {
        if (words.Length == 0)
            return CommandReply.Error("Nothing to announce.");
        return CommandReply.Announce(string.Join(" ", words), Color.Yellow).WithAuthor("TestPlugin");
    }
}
