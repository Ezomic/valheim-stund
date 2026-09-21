using System.Runtime.CompilerServices;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using Ezomic.Core;
using HarmonyLib;

namespace Stund
{
    /// <summary>
    /// Stund puts the time of day and the day number on the screen.
    ///
    /// The argument for it is that Valheim already has a clock and shows it to you in the
    /// most expensive way there is. The sun is precise - sunrise and sunset are fixed
    /// fractions of the day, and every experienced player is reading them - but reading it
    /// means being outdoors, looking up, and having a horizon. In a mine, a crypt, a
    /// longhouse or a fog bank you have nothing, and the decisions that actually turn on the
    /// time are exactly the ones you make in those places: whether there is enough light
    /// left to sail home, whether to start the last corridor, whether it is worth going to
    /// bed. The information is in the game. It is just not readable when it matters.
    ///
    /// Client-side in the strict sense: every effect is computed by the owning client off
    /// state it already has. The clock reads EnvMan and the world clock, both of which every
    /// client is already given, and writes nothing anywhere. A player without Stund sees
    /// exactly the game they would have seen, which is why Requirement.HostOnly below is
    /// correct rather than merely permissive.
    ///
    /// There is deliberately no BepInProcess attribute. A dedicated server runs
    /// valheim_server.exe, and Core's gate only refuses on the server side of RPC_PeerInfo -
    /// so a mod that must be enforced has to be allowed to load there.
    /// </summary>
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    // Soft, not hard. A hard dependency that is absent does not degrade - the plugin never
    // loads at all - and every mod here has to be installable on its own, because a stranger
    // should not need two installs to get one mod. Soft still buys the load-order guarantee
    // when Core is present, which is all that registering with the gate needs.
    [BepInDependency(CoreGuid, BepInDependency.DependencyFlags.SoftDependency)]
    public class StundPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "ezomic.valheim.stund";
        public const string PluginName = "Stund";
        public const string PluginVersion = "0.1.0";
        public const string PluginAuthor = "Robbin Thijssen";

        /// <summary>Core's plugin GUID. Optional - see TryRegisterWithCore.</summary>
        private const string CoreGuid = "ezomic.valheim.core";

        internal static ManualLogSource Log;

        /// <summary>
        /// Whether Core answered at load. Worth keeping even when nothing reads it yet: the
        /// difference between gated and ungated is invisible to a player otherwise, and this
        /// is what a warning on spawn would be driven by.
        /// </summary>
        internal static bool CorePresent;

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;

            // Config first. Registering absorbs every entry the mod has bound, so anything
            // bound after this line is carried only because Core re-absorbs at manifest
            // time - and depending on the order of two lines in an Awake is not a thing
            // worth relying on.
            StundConfig.Bind(Config);

            TryRegisterWithCore();

            // PatchAll over a named type, never the whole assembly. A bare PatchAll() walks
            // every type in the DLL, so a half-written patch class in another file goes live
            // the moment it compiles.
            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll(typeof(StundPatches));

            // The startup line every mod in the suite writes. It is how a log answers "which
            // build of what is actually loaded" without anyone guessing.
            Log.LogInfo(PluginName + " " + PluginVersion + " by " + PluginAuthor + " - ready.");
        }

        /// <summary>
        /// Joins Core's version gate when Core is installed, and does nothing when it is not.
        ///
        /// Name here exactly what standing alone costs, because it is usually not the mod.
        /// For most of these it is the *enforcement*: without Core nothing refuses a client
        /// that lacks the plugin, so the rule becomes an agreement between players rather
        /// than a property of the server. That is a real loss and a legitimate choice, and
        /// it is the server owner's to make - which is why this logs rather than refusing
        /// to run.
        /// </summary>
        private void TryRegisterWithCore()
        {
            CorePresent = Chainloader.PluginInfos.ContainsKey(CoreGuid);

            if (!CorePresent)
            {
                Log.LogInfo("Core not installed - running standalone, without the version gate.");
                return;
            }

            RegisterWithCore();
        }

        /// <summary>
        /// Kept separate and never inlined on purpose. The JIT resolves the assemblies a
        /// method needs when it first compiles that method, so a Suite call sitting directly
        /// in Awake would drag Ezomic.Core in before the check above could prevent it - and
        /// the missing-assembly exception would land during plugin load, which is the exact
        /// failure this arrangement exists to avoid. Isolating it means the type is only
        /// ever resolved on a machine that has Core.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void RegisterWithCore()
        {
            // HostOnly, and it is the whole of what this mod asks of a server: nothing. It
            // registers no prefab, writes no ZDO and changes no item, so a client without it
            // is genuinely unaffected and a client with it is not carrying anything the other
            // end has to understand. Core honours that in both directions - a server running
            // Stund lets in a client without it, and a server without Stund lets in a client
            // that has it, which is the half that had to be fixed for Skaft.
            Suite.Register(PluginGuid, PluginName, PluginVersion, Config, Requirement.HostOnly);

            // Every entry is Local, and that is not caution - Register absorbs the whole file
            // and the host's values are imposed on anything left synced, which for this mod
            // would mean a server deciding where on your screen your clock sits and how big
            // the text is. Vaettir paid for that lesson with a grid angle that turned in
            // singleplayer and refused to turn online: Core's SettingChanged watch puts an
            // imposed value straight back the moment anything writes it.
            Suite.Local(
                StundConfig.Enabled,
                StundConfig.Where,
                StundConfig.OffsetX,
                StundConfig.OffsetY,
                StundConfig.FontSize,
                StundConfig.ShowDay,
                StundConfig.TwentyFourHour,
                StundConfig.Verbose);
        }

        private void OnDestroy()
        {
            // UnpatchSelf, never UnpatchAll(). The argumentless one unpatches every mod in
            // the process, not just this one.
            if (_harmony != null) _harmony.UnpatchSelf();
        }
    }
}
