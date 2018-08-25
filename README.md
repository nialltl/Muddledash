# Muddledash

This repository contains bits of the source for Muddledash, a local multiplayer octopus racing game I released for Switch and desktop platforms. It's MIT license and you can do whatever you like with it!

Noel Berry sharing the source code for the player controller in Celeste made me want to do the same (https://github.com/NoelFB/Celeste), hopefully it can be of use to yours eyes and paws.

## Source

Player.cs
- This handles the bulk of Player platform controls and state-changes for individual podes in Muddledash. It doesn't contain any of the procedural leg code (this is handled by another component), but does handle making heads all squishy.

## Misc notes

- I don't know what I would do with pull requests as the game seems to (mostly) work and it's released now, but if you have any funky suggestions they're very welcome.

- External libraries referenced are the incredibly useful GoKit tweening library (https://github.com/prime31/GoKit) and Rewired (http://guavaman.com/projects/rewired/).

- **NOTE**: MIT License only applies to the code in this repo and does not include the actual commercial Muddledash game or assets.
