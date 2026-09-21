# Changelog

## 0.1.0 - unreleased

First version. Builds and deploys; **never run in game**.

The time and the day number on the HUD, in the game's own typeface, in a corner of your
choosing. The label is a copy of the health readout with everything that was driving it
stripped off, which is how it gets the font, the material and the outline without this mod
shipping a font or guessing at one.

Unverified until it is run:

- **Whether the copied readout comes up clean.** A cloned HUD object has never run its own
  lifecycle, and the suite has been caught by that before — a cloned bar drew nothing at all
  three separate ways. A text label is a much simpler case, but "simpler" is not "checked".
- **Where the default position actually lands.** Top centre with a small drop is reasoning
  about a screenshot, not a look at the screen; the offset may want moving once it is seen
  against a real HUD at this resolution.
