using TMPro;
using UnityEngine;

namespace Stund
{
    /// <summary>
    /// The clock itself: the time, and the label that shows it.
    ///
    /// Two decisions worth reading before changing anything here.
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
    /// <b>Midnight is fraction 0, midday is 0.5.</b> Not a guess, and not the 0.15 that
    /// <c>GetMorningStartSec</c> uses - that is where sleeping puts you, which is before
    /// dawn, and anchoring a clock there would put noon in the afternoon. The sun is the
    /// authority: EnvMan's day factor peaks at 0.5 and its two horizon transitions are
    /// centred on 0.26 and 0.74, so fraction times 24 gives midday at 12:00, sunrise near
    /// 06:15 and sunset near 17:45.
    /// </summary>
    internal static class Clock
    {
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
            double fraction = seconds % length / length;

            double hours = fraction * 24.0;
            hour = (int)hours;
            minute = (int)((hours - hour) * 60.0);

            // The game's own day number, so this agrees with the "Day N" message at dawn
            // rather than counting days of its own.
            day = env.GetDay();
            return true;
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
            rect.localScale = Vector3.one;

            _label.alignment = alignment;
            _label.fontSize = size;
            _label.enableAutoSizing = false;
        }
    }
}
