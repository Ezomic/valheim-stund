using TMPro;
using UnityEngine;

namespace Stund
{
    /// <summary>
    /// The clock itself: the time, and the label that shows it.
    ///
    /// Decisions worth reading before changing anything here.
    ///
    /// <b>The time is computed from the world clock, not from EnvMan.GetDayFraction().</b>
    /// That property returns <c>m_smoothDayFraction</c>, which is lerped a hundredth of the
    /// way towards the real value per FixedUpdate so the sun and the fog have something
    /// continuous to follow. It is the right number for lighting and the wrong one for a
    /// readout: it lags after a load or a time skip, which on a twenty-minute day is minutes
    /// of game time, and it would show a clock disagreeing with the game's own "Day N"
    /// message at dawn. EnvMan derives the true fraction from <c>ZNet.GetTimeSeconds()</c>
    /// and <c>m_dayLengthSec</c> in one line, so this does the same and gets the exact number.
    ///
    /// <b>The fraction is the game's RESCALED one, not raw world time.</b> The first version
    /// mapped raw time straight onto 24 hours, on the reasoning that the sun peaks at 0.5. That
    /// was right at noon and wrong everywhere else: EnvMan.RescaleDayFraction squeezes the
    /// night, stretching raw 0.15 to 0.85 onto 0.25 to 0.75, and every light, the horizon
    /// transitions at 0.26 and 0.74 and the morning trigger work on the rescaled number. So the
    /// raw clock read 03:36 when the "Day N" message appeared and 20:04 at sunset (LHM-57).
    /// Rescaled, the morning trigger and sunrise read 06:00, noon 12:00 and sunset 18:00, and
    /// midnight is still 00:00. The method is private, so <see cref="Rescale"/> is a copy.
    ///
    /// The game's own "Day N" message is decided on the smoothed fraction, so it fires about two
    /// real seconds after the world clock passes the morning trigger, and the HUD number, which
    /// follows the world clock, changes that much before the message appears.
    ///
    /// <b>The day number changes at 06:00, with the message.</b> See <see cref="TryRead"/>.
    /// </summary>
    internal static class Clock
    {
        /// <summary>EnvMan.GetMorningStartSec's 0.15, the raw fraction RescaleDayFraction maps to 0.25.</summary>
        private const double MorningFraction = 0.15;

        private static TMP_Text _label;

        /// <summary>
        /// What the label was last laid out for. Re-placing every frame would be cheap enough,
        /// but it would also silently undo anything else that ever moved the label, and that
        /// is the kind of thing that costs an afternoon later.
        /// </summary>
        private static Corner _placedWhere;
        private static float _placedX, _placedY;
        private static int _placedSize;

        private static string _last;

        /// <summary>
        /// Builds the label under the HUD, from a copy of the health readout.
        ///
        /// Cloning rather than building a TextMeshProUGUI from nothing is the suite's rule for
        /// UI - it inherits the font asset, the material, the outline and the colour, so the
        /// clock is in the game's own typeface without this mod shipping a font or guessing at
        /// one. It is also the only route that survives a font change in a game update.
        ///
        /// This runs on every <c>Hud.Awake</c>, and the static field is overwritten rather
        /// than checked. That is deliberate: in 1.0 the soft-ref bundles unload at logout and
        /// take borrowed assets with them, so a label cached across a trip to the main menu is
        /// a destroyed object wearing a destroyed font. A new Hud means a new label.
        /// </summary>
        internal static void Build(Hud hud)
        {
            _label = null;
            _last = null;

            if (hud == null) return;

            TMP_Text donor = hud.m_healthText;
            if (donor == null)
            {
                // Logged and skipped, never thrown. A missing donor costs the clock; it must
                // not cost the HUD, which is what an exception out of Hud.Awake would do.
                StundPlugin.Log.LogWarning(
                    "No health readout to copy the font from - no clock this session. "
                    + "Nothing else about the HUD is affected.");
                return;
            }

            Transform parent = hud.m_rootObject != null ? hud.m_rootObject.transform : hud.transform;

            GameObject go = Object.Instantiate(donor.gameObject, parent);
            go.name = "Stund_Clock";

            _label = go.GetComponent<TMP_Text>();
            if (_label == null)
            {
                Object.Destroy(go);
                StundPlugin.Log.LogWarning("The copied health readout carried no text component.");
                return;
            }

            Strip(go);

            // Said out loud rather than fixed in silence. Place() now resets the rotation, so
            // this never shows as a broken clock again - but a donor that is not upright is a
            // fact about the vanilla HUD worth having in the log the next time something
            // cloned from it comes out at an angle.
            Vector3 donorAngles = donor.transform.localEulerAngles;
            if (donorAngles.sqrMagnitude > 0.01f)
            {
                StundPlugin.Log.LogInfo(
                    "The health readout is not upright - local rotation " + donorAngles
                    + ". The clock resets its own, so this costs nothing here.");
            }

            go.SetActive(true);
            _label.text = "";
            _label.raycastTarget = false;

            // Force a layout on the first tick rather than trusting the donor's.
            _placedSize = 0;

            if (StundConfig.Verbose.Value)
            {
                StundPlugin.Log.LogInfo("Clock built under " + parent.name + ".");
            }
        }

        /// <summary>
        /// Takes everything off the clone that was driving the health readout.
        ///
        /// A cloned HUD object carries the donor's scripts, and they keep running - a copy of
        /// the health text would go on writing health into itself every frame, which reads as
        /// "the clock shows the wrong thing" rather than as a stray component. The same goes
        /// for a Localize, which would put the donor's label back at the next language change.
        ///
        /// DestroyImmediate, not Destroy: Destroy is deferred to the end of the frame, so the
        /// stripped scripts would each get one more Update first.
        /// </summary>
        private static void Strip(GameObject go)
        {
            foreach (Component component in go.GetComponents<Component>())
            {
                if (component is RectTransform) continue;
                if (component is CanvasRenderer) continue;
                if (component is TMP_Text) continue;

                Object.DestroyImmediate(component);
            }

            // Children too - the donor may carry a shadow or a sub-label of its own, and an
            // orphan of the health panel hanging off the clock is a thing nobody would guess at.
            for (int i = go.transform.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(go.transform.GetChild(i).gameObject);
            }
        }

        /// <summary>Drives the label. Called from Hud.Update, so it stops when the HUD does.</summary>
        internal static void Tick()
        {
            if (_label == null) return;

            bool on = StundConfig.Enabled.Value;
            if (_label.gameObject.activeSelf != on) _label.gameObject.SetActive(on);
            if (!on) return;

            Place();

            int hour, minute, day;
            if (!TryRead(out hour, out minute, out day))
            {
                // Between worlds. Blank rather than stale - a clock frozen at the time you
                // logged out is worse than no clock, because it looks like it is working.
                if (_last != "")
                {
                    _last = "";
                    _label.text = "";
                }

                return;
            }

            string text = Render(hour, minute, day);

            // TextMeshPro rebuilds its mesh on every assignment, so only write on a change.
            // A minute of game time is a second or two of real time, so that is a write a
            // second or so instead of one a frame.
            if (text == _last) return;

            _last = text;
            _label.text = text;
        }

        /// <summary>
        /// The world clock, as hours and minutes. False when there is no world - there is a
        /// Hud behind the main menu for part of the load.
        /// </summary>
        internal static bool TryRead(out int hour, out int minute, out int day)
        {
            hour = 0;
            minute = 0;
            day = 0;

            EnvMan env = EnvMan.instance;
            if (env == null || ZNet.instance == null) return false;

            // Read the day length off the running game every time. The field reads 1200 in a
            // decompile and that is the class default, not the value the asset ships - this is
            // asset data, and the only honest place to get it is the instance in front of you.
            long length = env.m_dayLengthSec;
            if (length <= 0) return false;

            double seconds = ZNet.instance.GetTimeSeconds();
            double fraction = Rescale(seconds % length / length);

            double hours = fraction * 24.0;
            hour = (int)hours;
            minute = (int)((hours - hour) * 60.0);

            // The day number rolls at the morning trigger, 0.15 of the raw day, not at raw
            // midnight where EnvMan.GetDay() rolls. The "Day N" message is shown at that
            // trigger with N = total seconds over day length, which is GetDay() at that moment,
            // so the two agree from 06:00 on. Before it GetDay() has already moved on to N while
            // the message for N has not appeared, and a HUD reading "Day 44   03:10" is a day
            // the game has not announced. Shifting by the same 0.15 makes the number change on
            // the message, which is also when the clock reads 06:00.
            day = (int)((seconds - length * MorningFraction) / length);
            return true;
        }

        /// <summary>
        /// A copy of EnvMan.RescaleDayFraction, assembly_valheim 1.0, which is private. Copied
        /// rather than reflected into: five lines that have not changed, against a reflection
        /// bound to a private name that would cost the clock outright if it were renamed. The
        /// scenario stund-clock-agrees-with-the-world is what notices if the game moves them,
        /// because it asks the real morning skip what the clock reads.
        /// </summary>
        internal static double Rescale(double fraction)
        {
            if (fraction >= 0.15 && fraction <= 0.85)
            {
                return 0.25 + (fraction - 0.15) / 0.7 * 0.5;
            }

            if (fraction < 0.5) return fraction / 0.15 * 0.25;

            return 0.75 + (fraction - 0.85) / 0.15 * 0.25;
        }

        private static string Render(int hour, int minute, int day)
        {
            string time;
            if (StundConfig.TwentyFourHour.Value)
            {
                time = hour.ToString("00") + ":" + minute.ToString("00");
            }
            else
            {
                int twelve = hour % 12;
                if (twelve == 0) twelve = 12;
                time = twelve + ":" + minute.ToString("00") + (hour < 12 ? " AM" : " PM");
            }

            return StundConfig.ShowDay.Value ? "Day " + day + "   " + time : time;
        }

        /// <summary>
        /// Anchors the label to the chosen corner. Anchor, pivot and text alignment all move
        /// together: anchoring right without pivoting right hangs the label off the edge of
        /// the screen by half its width, and the offset then means something different in
        /// every corner.
        /// </summary>
        private static void Place()
        {
            Corner where = StundConfig.Where.Value;
            float x = StundConfig.OffsetX.Value;
            float y = StundConfig.OffsetY.Value;
            int size = StundConfig.FontSize.Value;

            if (where == _placedWhere && x == _placedX && y == _placedY && size == _placedSize) return;

            _placedWhere = where;
            _placedX = x;
            _placedY = y;
            _placedSize = size;

            Vector2 anchor;
            TextAlignmentOptions alignment;

            switch (where)
            {
                case Corner.TopLeft:
                    anchor = new Vector2(0f, 1f);
                    alignment = TextAlignmentOptions.TopLeft;
                    break;
                case Corner.TopRight:
                    anchor = new Vector2(1f, 1f);
                    alignment = TextAlignmentOptions.TopRight;
                    break;
                case Corner.BottomLeft:
                    anchor = new Vector2(0f, 0f);
                    alignment = TextAlignmentOptions.BottomLeft;
                    break;
                case Corner.BottomCentre:
                    anchor = new Vector2(0.5f, 0f);
                    alignment = TextAlignmentOptions.Bottom;
                    break;
                case Corner.BottomRight:
                    anchor = new Vector2(1f, 0f);
                    alignment = TextAlignmentOptions.BottomRight;
                    break;
                default:
                    anchor = new Vector2(0.5f, 1f);
                    alignment = TextAlignmentOptions.Top;
                    break;
            }

            var rect = (RectTransform)_label.transform;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.sizeDelta = new Vector2(320f, size * 2f);
            rect.anchoredPosition = new Vector2(x, y);

            // Rotation and scale are both reset, and the rotation is the one that was missing.
            // Instantiate(donor, parent) keeps the donor's LOCAL transform, so a clone starts
            // out wearing whatever the health readout's own transform was doing - and in 1.0
            // that is a local rotation of (0, 0, 270), measured from the running game rather
            // than assumed. The clock came out reading top to bottom with every glyph on its
            // side, which looks like a font or a layout bug and is neither.
            //
            // A cloned HUD object owns nothing about its own placement until this code sets
            // it, which is the same reason localScale is here: the label has to be told every
            // part of where it is, not just the parts that looked wrong at the time.
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;

            _label.alignment = alignment;
            _label.fontSize = size;
            _label.enableAutoSizing = false;
        }
    }
}
