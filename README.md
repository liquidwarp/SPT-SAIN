# Modifications of Solarint's AI Modifications

Fork of SAIN, whose continued repo is now archived.

## Requirements

- [BotHealingFixes, forked version](https://github.com/liquidwarp/SPT-BotHealingFixes). SAIN doesn't load without it, and logs an error at startup if the original is installed.

## Reworks and new behavior

### Hearing

- **Reworked hearing** that no longer uses your real position as a basis. Bots get the direction roughly right and the distance less so, are worse at it from far away or behind, and get better the more they hear you. Their mistakes stay consistent instead of jumping around you, and moving quietly after making noise can shake them off.
- **Pain, breathing and voice lines** follow normal hearing, so walls and distance apply.
- **Grenade explosions** no longer reveal the thrower.
- **Headsets and helmets** follow EFT's rules: built-in headsets count, any headset removes the helmet penalty, and there's a new light-helmet setting.
- New hearing settings replace the old position randomization settings.

### Less hidden information

- Bots turn toward a sound only when they can see where they think it came from, not because they secretly have line of sight to you.
- Flashed bots fire at where they last knew (or thought) you were, not at your real position.

### Healing

- Bot healing relies on the forked BotHealingFixes. SAIN decides when bots use first aid, surgery or stims, BotHealingFixes which item they use -- see the fork's README for details.

### Removed

- SAIN's own bot extraction, which was off by default, and its Looting Bots, Questing Bots and Realism integrations. Bots use vanilla extraction or a mod like ORBIT.
- Settings that never did anything, such as the unstuck/teleport options and door kicking.
- Debug options in release builds. They now only appear in the editor of debug builds, and release builds ignore any a preset has saved.

## Fixes

- Fix bots looking, suppressing and leaning at you through walls instead of watching the corner or doorway you'd come out of.
- Fix bots noticing flashlight beams through walls, and mispredicting where grenades land when a wall is in the way.
- Fix bot aim resetting every few shots. It now keeps tightening while a bot keeps its shot on you, and resets when you break line of sight, so staying exposed is deadlier while peeking is as forgiving as before.
- Fix aim limits ignoring each bot type's own game data. Scavs and marksmen are now less precise in long fights, and the Goons and Sanitar more precise.
- Fix bot recoil resetting early on some shots.
- Fix bots restarting their move to cover when reloading or out of ammo, and reloads that get stuck never being retried.
- Fix bots not finishing surgery, first aid and stims.
- Fix preset settings only applying after saving from the in-game editor. Taunting, faking death and begging now depend on personality, squad callouts follow the Squad Talk settings with far less chatter, and the grenade toggles are respected.
- Fix the "Bot Reaction Changes Toggle" not controlling the "not looking" reaction delay.
- Fix bots crouching, standing and leaning faster at higher frame rates.
- Fix errors with despawned players and other destroyed objects.
- Smaller fixes to footstep hearing, grenade escape and the preset editor.

## Development

- Debug builds add hearing test tools under General → Debug: a hearing overlay, markers where bots guess you are, logs of why a sound was or wasn't heard, and a "Bots Can't See Me" option to test hearing alone.
