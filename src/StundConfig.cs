using System;
using System.IO;
using BepInEx.Configuration;

namespace Stund
{
    /// <summary>
    /// Which corner the clock hangs off. The offset is measured from that corner, so the sign
    /// that moves it inwards flips with the corner - negative Y from a top corner, positive Y
    /// from a bottom one.
    /// </summary>
    internal enum Corner
    {
        TopLeft,
        TopCentre,
        TopRight,
        BottomLeft,
        BottomCentre,
        BottomRight
    }

    /// <summary>
    /// What the label says. The first is the clock as it was before this existed, so a file that
    /// never chose has the same clock as before.
    /// </summary>
    internal enum ClockContent
    {
        DayAndTime,
        TimeOnly,
        DayOnly,
        TimeOfDay
    }

    /// <summary>
    /// Everything tunable, bound in one place so the .cfg reads as a document rather than as
    /// whatever order the code happened to need things in.
    ///
    /// The standing BepInEx trap applies here as everywhere: every entry is written to disk on
    /// first run and the saved value beats a new default in code. Changing a default does
    /// nothing on a machine that has already run the plugin - edit
    /// <c>&lt;profile&gt;\BepInEx\config\ezomic.valheim.stund.cfg</c> as part of the same
    /// change. When a config-driven change appears to do nothing in game, read the cfg before
    /// reading any code.
    /// </summary>
    internal static class StundConfig
    {
        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<Corner> Where;
        internal static ConfigEntry<float> OffsetX;
        internal static ConfigEntry<float> OffsetY;
        internal static ConfigEntry<int> FontSize;
        internal static ConfigEntry<ClockContent> Content;
        internal static ConfigEntry<bool> TwentyFourHour;
        internal static ConfigEntry<bool> Verbose;

        /// <summary>
        /// What the file says before anything is bound: whether it already has a Content line, and
        /// whether it has ShowDay turned off. Read as text because BepInEx keeps the lines it has
        /// no entry for to itself.
        /// </summary>
        private static void ReadOldFile(string path, out bool chosen, out bool showDayOff)
        {
            chosen = false;
            showDayOff = false;

            try
            {
                foreach (string line in File.ReadAllLines(path))
                {
                    string[] pair = line.Split(new[] { '=' }, 2);
                    if (pair.Length != 2) continue;

                    string key = pair[0].Trim();
                    if (key == "Content") chosen = true;
                    if (key == "ShowDay" && pair[1].Trim().Equals("false", StringComparison.OrdinalIgnoreCase))
                        showDayOff = true;
                }
            }
            catch (IOException)
            {
            }
        }

        internal static void Bind(ConfigFile cfg)
        {
            // Loaded, bound, patched, and deciding nothing. Not "unloaded" - a plugin cannot
            // unload itself, and a switch that pretends otherwise is a lie somebody will debug.
            Enabled = cfg.Bind("Stund", "Enabled", true,
                "Off leaves the plugin loaded and takes the clock off the screen.");

            // TopCentre by default because it is the only part of the screen vanilla leaves
            // empty in ordinary play: the health, stamina and eitr bars own the top left and
            // the minimap owns the top right. The thing that will sit on top of it is the boss
            // health bar, which is also top centre - that is transient, and moving the clock
            // for it is two lines here.
            Where = cfg.Bind("Stund", "Where", Corner.TopCentre,
                "Which corner of the screen the clock hangs off. The top left is the bars and "
                + "the top right is the map, so the top centre is the one that is usually "
                + "empty - a boss health bar is the thing that will overlap it.");

            OffsetX = cfg.Bind("Stund", "OffsetX", 0f,
                "Pixels from that corner, left to right. Positive is rightwards from any "
                + "corner; from a right-hand corner that is off the screen.");

            OffsetY = cfg.Bind("Stund", "OffsetY", -28f,
                "Pixels from that corner, bottom to top. Negative moves down, so a top corner "
                + "wants a negative number here and a bottom corner a positive one.");

            // 12px is the suite's floor for readable text on this setup and 20 is comfortable
            // at 1440p. It is a size, not a scale, so it does not follow the game's UI scaling.
            FontSize = cfg.Bind("Stund", "FontSize", 20,
                new ConfigDescription(
                    "Point size of the clock. Below about 12 it stops being readable at a glance, "
                    + "which defeats the point of it being on screen at all.",
                    new AcceptableValueRange<int>(12, 48)));

            // Replaced ShowDay (LHM-51), which could say two things where the settings screen
            // wants four. A file written before still says ShowDay = false, and that choice is
            // carried over once, so nobody's clock grows a day number they had turned off.
            bool chosen, hadShowDayOff;
            ReadOldFile(cfg.ConfigFilePath, out chosen, out hadShowDayOff);

            Content = cfg.Bind("Stund", "Content", ClockContent.DayAndTime,
                "What the clock shows. DayAndTime is 'Day 43   17:45', TimeOnly is the time, DayOnly "
                + "is 'Day 43', and TimeOfDay is a word from the rescaled clock: Dawn from 04:48 to "
                + "07:12, Day until 16:48, Dusk until 19:12, and Night after that. The day number is "
                + "the one the game announces at dawn, so it agrees with the message rather than "
                + "counting its own days.");

            if (hadShowDayOff && !chosen) Content.Value = ClockContent.TimeOnly;

            // 24-hour by default because Valheim has no AM/PM anywhere and the sun is the only
            // other clock in the game - a 17:45 sunset reads once, a 5:45 sunset reads twice.
            TwentyFourHour = cfg.Bind("Stund", "TwentyFourHour", true,
                "Off gives 5:45 PM instead of 17:45. Sunrise is about 06:00 and sunset about "
                + "18:00 either way.");

            // Not synced by intent - see the plugin. A diagnostic flag is personal, and a host
            // turning on someone else's logging is not a thing anybody asked for.
            Verbose = cfg.Bind("Stund", "Verbose", false,
                "Write what the clock found to build itself with to BepInEx/LogOutput.log. Off "
                + "unless the clock is missing; it is a handful of lines at each world load.");
        }
    }
}
