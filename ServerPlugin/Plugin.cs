using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Threading;
using HarmonyLib;
using PluginSdk.Config;
using PluginSdk.Commands;
using PluginSdk.Paths;
using ServerPlugin.Config;
using Shared.Config;
using Shared.Logging;
using Shared.Patches;
using Shared.Plugin;
using VRage.FileSystem;
using VRage.Game;
using VRage.Plugins;
using SdkLogger = PluginSdk.Logging.Logger;

// Define assembly version when compiled by Magnetar
#if !DEV_BUILD
using System.Reflection;

[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
#endif

namespace ServerPlugin;

// ReSharper disable once UnusedType.Global
public class Plugin : IPlugin, ICommonPlugin
{
    public const string Name = "TestPlugin";
    public static Plugin Instance { get; private set; }

    public long Tick { get; private set; }
    private static bool failed;

    // Shared logger, kept only to satisfy ICommonPlugin / the Harmony patch
    // scaffolding (Plugin.Common.Logger). All PluginSdk-feature logging goes
    // through SdkLog below.
    public IPluginLogger Log => Logger;
    private static readonly IPluginLogger Logger = new PluginLogger(Name);

    // PluginSdk logger: environment-agnostic. Writes to the Magnetar game log
    // when standalone, or structured JSON when managed by Quasar. This is the
    // logger used for config changes, so they can be followed in the log.
    private static readonly SdkLogger SdkLog = SdkLogger.Create(Name);

    // The PluginSdk-managed configuration is the single source of truth. It
    // also implements Shared IPluginConfig so the patches can gate on it.
    public IPluginConfig Config => config;
    private static TestPluginConfig config;
    public static TestPluginConfig TestConfig => config;

    private static string configPath;

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    public void Init(object gameInstance)
    {
#if DEBUG
        // Allow the debugger some time to connect once the plugin assembly is loaded
        Thread.Sleep(100);
#endif

        Instance = this;

        SdkLog.Info("Loading TestPlugin (PluginSdk feature example)");

#if PLATFORM_LINUX
        SdkLog.Info("Compiled for Linux (PLATFORM_LINUX)");
#elif PLATFORM_WINDOWS
        SdkLog.Info("Compiled for Windows (PLATFORM_WINDOWS)");
#else
        SdkLog.Info("Compiled without a platform symbol (local/standalone build)");
#endif

        // Resolve the config path case-insensitively: a no-op on Windows, the
        // LinuxCompat resolver on Linux. Works as one code path on both.
        configPath = PathResolver.Normalize(Path.Combine(MyFileSystem.UserDataPath, $"{Name}.cfg"));
        SdkLog.Info("Resolved config path", new
        {
            configPath,
            caseInsensitiveResolver = PathResolver.IsCaseInsensitiveResolverActive,
        });

        // Load existing values, or a default-constructed instance when absent.
        config = ConfigStorage.LoadXml<TestPluginConfig>(configPath);

        // Persist immediately so a fresh install leaves a sparse on-disk file
        // (only non-default values) to inspect.
        TrySaveConfig();

        // Log every config change so it can be followed in the Magnetar log.
        config.PropertyChanged += OnConfigChanged;

        // Build the JSON envelope once (schema + defaults + values) to confirm
        // the whole config — including every struct/list/dict combination —
        // round-trips through the schema builder.
        LogConfigEnvelope();

        var gameVersion = MyFinalBuildConstants.APP_VERSION_STRING.ToString();
        Common.SetPlugin(this, gameVersion, MyFileSystem.UserDataPath);

        RegisterCommands();

        if (!PatchHelpers.HarmonyPatchAll(Log, new Harmony(Name)))
        {
            failed = true;
            return;
        }

        SdkLog.Info("Successfully loaded");
    }

    private static void OnConfigChanged(object sender, PropertyChangedEventArgs e)
    {
        SdkLog.Info($"Config changed: {e.PropertyName}");

        // Re-persist so the on-disk file always reflects the live config.
        TrySaveConfig();
    }

    internal static void TrySaveConfig()
    {
        if (config == null || configPath == null)
            return;

        try
        {
            ConfigStorage.SaveXml(config, configPath);
            SdkLog.Debug("Config saved to disk", new { configPath });
        }
        catch (Exception ex)
        {
            SdkLog.Error("Failed to save config", ex);
        }
    }

    private static void LogConfigEnvelope()
    {
        try
        {
            var json = ConfigStorage.SaveJson(config);
            SdkLog.Debug($"Config JSON envelope built ({json.Length} chars)");
        }
        catch (Exception ex)
        {
            SdkLog.Error("Failed to build config JSON envelope", ex);
        }
    }

    private void RegisterCommands()
    {
        // The registrar is installed by the host. When absent (e.g. a host
        // build without chat-command support) registration would throw, so
        // guard it and warn instead.
        if (ServerCommands.Registrar == null)
        {
            SdkLog.Warning("No command registrar available; chat commands not registered.");
            return;
        }

        try
        {
            ServerCommands.Register(Assembly.GetExecutingAssembly());
            SdkLog.Info("Registered chat commands under !test");
        }
        catch (Exception ex)
        {
            SdkLog.Error("Failed to register chat commands", ex);
        }
    }

    public void Dispose()
    {
        try
        {
            if (config != null)
                config.PropertyChanged -= OnConfigChanged;
        }
        catch (Exception ex)
        {
            SdkLog.Critical("Dispose failed", ex);
        }

        Instance = null;
    }

    public void Update()
    {
        if (failed)
            return;

#if DEBUG
        CustomUpdate();
        Tick++;
#else
        try
        {
            CustomUpdate();
            Tick++;
        }
        catch (Exception e)
        {
            SdkLog.Critical("Update failed", e);
            failed = true;
        }
#endif
    }

    private void CustomUpdate()
    {
        PatchHelpers.PatchUpdates();
    }
}
