# Changelog

## 1.0.1 - 2026-10-05

### Fixed

- **The clock now reads the sky's time, not raw world time.** Valheim lights the sky from a
  rescaled day fraction (`EnvMan.RescaleDayFraction`) that squeezes the night, so with raw time
  the "Day N" message arrived at 03:36, the sun rose near 03:56 and set near 20:04. Only noon
  agreed. Stund now applies the same rescaling (a copy, since the method is private), so the
  morning is 06:00, sunrise is about 06:00, noon is 12:00, sunset is 18:00 and midnight is 00:00.
- The day number changes at 06:00 together with the "Day N" message, not at midnight where
  `EnvMan.GetDay()` rolls it. The message trails the HUD by about two real seconds.
- The cfg description of `TwentyFourHour` gave the old sunrise and sunset times.

The scenario `stund-clock-agrees-with-the-world` now steps through 06:00, 12:00, 18:00 and 00:00,
and fails on the old readings. It assumes the 1200 second day. It cannot check that the day
number rolled, only that a "Day" label is present, since scenarios cannot read a number off the
HUD.

## 1.0.0 - 2026-09-30

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
