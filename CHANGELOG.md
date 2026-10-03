# Changelog

## Unreleased

The clock can say less, or say it in a word (LHM-51). A new `Content` setting replaces `ShowDay`:
`DayAndTime` as before, `TimeOnly`, `DayOnly`, or `TimeOfDay`, which shows Dawn, Day, Dusk or Night
from the rescaled clock the digits already come from (Dawn 04:48 to 07:12, Dusk 16:48 to 19:12). A
file with `ShowDay = false` is carried over once as `TimeOnly`. `FontSize` now has a range of 8 to 96,
wide enough that a deliberate size, small or large, is kept instead of being clamped when the file
is loaded; the settings page steps through the same range.

The clock's five settings (on or off, what it shows, 24-hour, position, size) are listed on Core's
Settings page in the compendium, so they can be changed in game. With no Core nothing changes: the
`.cfg` is still the way, and the call to Core is skipped. Built, not run in game.

Fixed the clock running on raw world time (LHM-57). Valheim lights the sky from a rescaled day
fraction, `EnvMan.RescaleDayFraction`, which stretches raw 0.15 to 0.85 onto 0.25 to 0.75. Stund
showed the raw one, so the "Day N" message arrived at 03:36, the sun rose near 03:56 and set near
20:04. Only noon agreed, which is why the old scenario passed: it looked at nothing else. The clock
now applies the same rescaling (a copy, since the method is private), so the morning trigger reads 06:00, sunrise about 06:00, noon 12:00, sunset 18:00 and midnight 00:00.

The day number now changes at 06:00 with the "Day N" message, not at midnight where
`EnvMan.GetDay()` rolls, so the HUD never shows a day the game has not announced yet.

The scenario `stund-clock-agrees-with-the-world` now steps through 06:00 (the real morning skip),
12:00, 18:00 and 00:00, and fails on the old raw readings. It assumes the 1200 second day. It does not check that the day NUMBER rolled at 06:00: Devkit
scenarios can assert that a HUD label contains a fixed string (`onscreen`) but cannot read a
number off it or compare two readings, and the starting day is not known, so the scenario only
checks that a "Day" label is present.

## 1.0.0 - pending

First release.

`Day 43   17:45` on the HUD, in the game's own typeface, in a corner of your choosing. The
label is a copy of the health readout with everything that was driving it stripped off, which
is where the font, the material and the outline come from without shipping a font.

The sun already tells you the time and is no use underground, which is where the questions that
turn on it get asked: whether there is light left to sail home, whether to start one more
corridor, how much of the night is left.

Settled since 0.1.0 by running it:

- The cloned readout does not come up clean. `Instantiate(donor, parent)` keeps the donor's
  LOCAL transform, and vanilla's health readout sits at `(0, 0, 270)`, measured from the
  running game. The clock came out reading top to bottom with every glyph on its side while all
  three of its scenario's assertions passed, because those read the label's text and text is
  the same text at any angle. `Place()` now resets the rotation beside the scale it was already
  resetting, and `Build()` logs a donor that is not upright.
- The default position lands where it was meant to. Top centre, in the one part of the screen
  vanilla leaves empty in ordinary play, with the bars on the top left and the minimap on the
  top right. The offset is a judgement rather than a measurement and it is three lines in the
  config.

Proven in game by `stund-clock-agrees-with-the-world`, 8 steps. It skips to noon through Devkit
and checks the clock says 12 rather than 14. Midnight is day fraction 0, and anchoring on
`GetMorningStartSec` instead, which is 0.15 of the day and where sleeping puts you, reads that
same moment as 14:24. So 12 means the clock is using the sun and 14 means it is using the bed.

The eighth step is `upright`, added to Devkit in the same pass, because the other seven passed
while the clock was unreadable.

Two things nothing automatic covers, both one look to check. Whether the label is visible,
since `Hud.SetVisible` parks its root off the screen rather than disabling it. And the rebuild
on a second world load, since the scene a scenario runs in does not survive one.

Purely local. It reads a clock every client is already given and draws a label, so a server
does not know it is there. Registers with Core's gate at `Requirement.HostOnly` and marks every
setting as yours, so a host does not get to decide where on your screen your clock sits.
