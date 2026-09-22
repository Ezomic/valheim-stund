# Changelog

## 1.0.0 - pending

First release.

`Day 43   17:45` on the HUD, in the game's own typeface, in a corner of your choosing. The
label is a copy of the health readout with everything that was driving it stripped off, which
is how it gets the font, the material and the outline without this mod shipping a font or
guessing at one.

Valheim already has a clock and it is a good one. The sun rises and sets at fixed fractions of
the day and every experienced player reads it without being told to. It stops working the
moment you are underground, which is where the questions that turn on the time actually get
asked: whether there is light left to sail home, whether to start one more corridor, whether
night is nearly over or has just begun.

Settled since 0.1.0, and neither was readable any other way:

- **The cloned readout does not come up clean, and the way it fails is invisible to a
  scenario.** `Instantiate(donor, parent)` keeps the donor's LOCAL transform, and vanilla's
  health readout sits at `(0, 0, 270)` - measured from the running game, not inferred. The
  clock came out reading top to bottom with every glyph on its side while all three of its
  scenario's assertions passed, because those read the label's text and text is the same text
  at any angle. `Place()` now resets the rotation beside the scale it was already resetting,
  and `Build()` logs a donor that is not upright so the next thing cloned off that readout has
  the reason waiting in the log.
- **The default position lands where it was meant to.** Top centre, in the one part of the
  screen vanilla leaves empty in ordinary play - the bars own the top left and the minimap the
  top right. The offset is still a judgement rather than a measurement, and it is three lines
  in the config.

Proven in game by `stund-clock-agrees-with-the-world`, 8 steps. It skips to noon through
Devkit and then checks the clock says 12 rather than 14, which is the discriminator that
matters: midnight is day fraction 0, and anchoring on `GetMorningStartSec` instead - 0.15 of
the day, which is where sleeping puts you - reads the same moment as 14:24. A clock that says
12 there is using the sun; one that says 14 is using the bed.

The eighth step is `upright`, added to Devkit in the same pass. It exists because the other
seven passed while the clock was unreadable.

Still not covered by anything automatic, and both are one look to check: that the label is
actually **visible** - `Hud.SetVisible` parks its root off the screen rather than disabling it,
so nothing in the hierarchy can tell you - and the rebuild on a second world load, because the
scene a scenario runs in does not survive one.

Purely local. It reads a clock every client is already given and draws a label, so a server
does not know it is there. It registers with Core's gate at `Requirement.HostOnly` and marks
every setting as yours: a host does not get to decide where on your screen your clock sits.
