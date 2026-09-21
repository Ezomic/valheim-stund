using HarmonyLib;

namespace Stund
{
    /// <summary>
    /// The mod's Harmony patches. One class named in the plugin's PatchAll, so nothing goes
    /// live by being written.
    ///
    /// Both patches hang off the HUD rather than off the player or off EnvMan, and that is
    /// what makes the clock behave without a line of code for any of it. A label parented to
    /// <c>Hud.m_rootObject</c> is hidden by the game's own HUD toggle, by cutscenes and by
    /// the death screen, because <c>Hud.SetVisible</c> moves that root off the screen rather
    /// than disabling it. Driving it from <c>Hud.Update</c> means the clock stops ticking in
    /// exactly the cases the HUD stops updating.
    /// </summary>
    internal static class StundPatches
    {
        /// <summary>
        /// Builds the clock with each new HUD - which is each world load, not each session.
        ///
        /// Awake is private, hence the string. Harmony resolves it the same way; the cost is
        /// that a rename in a game update becomes a patch that silently never applies rather
        /// than a compile error, which is what the verbose log line in Clock.Build is for.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Hud), "Awake")]
        private static void HudAwake(Hud __instance)
        {
            Clock.Build(__instance);
        }

        /// <summary>Drives the label. Cheap: it formats a string and writes it on a change.</summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Hud), "Update")]
        private static void HudUpdate()
        {
            Clock.Tick();
        }
    }
}
