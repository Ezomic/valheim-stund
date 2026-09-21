# Stund

The time of day and the day number, on the screen.

## Why

Valheim already has a clock, and it shows it to you in the most expensive way there is.

The sun is precise. Sunrise and sunset are fixed fractions of the day, always the same, and
every experienced player is reading them without being told to. You look at the light and
you know roughly how long you have. It works well enough that a clock feels redundant right
up until you are underground.

And underground is where the decisions are. In a mine, a crypt, a longhouse, a fog bank or
below deck, there is no sun and no horizon, and the questions that actually turn on the time
are exactly the ones you ask in those places. Is there enough light left to sail home. Is it
worth starting the last corridor. Is it nearly night, so going to bed now is free, or did
night just start, so it is eight minutes of standing about. The information is in the game
already. It is simply not readable at the moment it matters.

*Stund* is Old Norse for an hour, a while, a point in time.

## What it shows

`Day 43   17:45`, in the game's own typeface, in a corner you choose.

The day number is the game's own: the same one it announces at dawn, read from the same
place, so the clock and the message never disagree. The time is derived from the world
clock rather than from the smoothed value the sun and the fog are drawn with, which lags by
a couple of seconds of real time. On a twenty-minute day that is minutes of game time, and
a clock that is minutes out at dawn is a clock people stop trusting.

Midnight is 00:00 and midday is 12:00, which puts sunrise near 06:15 and sunset near 17:45.
Those are not chosen numbers: Valheim's own day curve peaks at the halfway point and its two
horizon transitions sit at 0.26 and 0.74 of the day, so mapping the day straight onto
twenty-four hours is what makes the clock agree with the sky.

## Where it sits

Top centre by default, because it is the only part of the screen vanilla leaves empty in
ordinary play. The health, stamina and eitr bars own the top left and the minimap owns the
top right. The thing that will sit on top of it is a boss health bar, which is also top
centre and is not there for long. Corner, offset and size are three lines in the config.

It hides when the rest of the HUD hides, including in cutscenes and on the death screen,
because it is parented to the HUD rather than drawn over it.

## Installing

Needs BepInEx. Nothing else. Through a mod manager it is one install. By hand, put
`Stund.dll` in `BepInEx/plugins/Stund/`.

Then start the game once and quit. That first run writes the config file. It does not exist
before the mod has loaded, which is the usual reason people think it is broken.

## Settings

The file is `BepInEx/config/ezomic.valheim.stund.cfg`. Open it in any text editor. Every
setting has a comment above it, so the file explains itself.

Note that changing a default in a new version does nothing on a machine that has already run
the mod. BepInEx writes every entry on first run and the saved value wins.

## Multiplayer

**Nobody else needs it.** Purely local and purely visual: it reads a clock every client is
already given and draws a label. A server does not know it is there.

If [Core](https://github.com/Ezomic/valheim-core) is installed, this mod registers with its
version gate so a mismatch is reported rather than discovered later. Every setting here is
marked as yours. A host does not get to decide where on your screen your clock sits.

## Bugs and ideas

Both go to the site. [longhouse.thijssensoftware.nl/bugs](https://longhouse.thijssensoftware.nl/bugs)
is for anything broken, and [longhouse.thijssensoftware.nl/ideas](https://longhouse.thijssensoftware.nl/ideas)
is for what a mod should do next. You can vote on other people's ideas there as well.

Signing in takes a Steam or Discord account. I work from that list, so the votes decide what
I pick up next.

## Licence

MIT. See `LICENSE`.
