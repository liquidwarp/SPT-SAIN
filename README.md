# Modifications of Solarint's AI Modifications

Fork of SAIN, whose continued repo is now archived.

## Requirements

- [BotHealingFixes, forked version](https://github.com/liquidwarp/SPT-BotHealingFixes). SAIN doesn't load without it.

## Changes

### Aim keeps tightening while you stay exposed

- A bot's aim starts loose when it gets a shot at you and gets steadily more precise for as long as it keeps that shot, down to its "max aiming upgrade by time" limit.
- Before, its aim went back to loose every 2 or 3 trigger pulls, so a bot firing steadily rarely got close to that limit.
- It starts loose again as soon as the bot loses its shot: you break line of sight, or it reloads, sprints or switches to another target. Peeking and ducking back out of sight is as forgiving as before.
- Fixed bots sometimes starting on you with the precision they had built up earlier, for example on another target, before being flashed, or before a brain layer from EFT or another mod took over for a while.
- The first shots are still forgiving, but staying in the open against PMCs, Raiders, bosses and their followers is noticeably deadlier.
- Each bot type now gets its own "max aim time" and "max aiming upgrade by time" from the game's data, as was intended. Before, nearly every bot type got the same values (2 and 0.25). On the default presets:
  - Scavs and marksmen barely tighten their aim: their limit is now 0.85 instead of 0.25 (0.4 on impossible difficulty), so long fights against them are much less precise than before.
  - The Goons, Sanitar and their followers can tighten further than before, down to 0.15.
  - Most bots finish aiming in at most 1.5 seconds instead of 2, and Rogues in 1.1.
  - The Easy preset still keeps every bot at 2 seconds and 0.4 or looser, and the Harder PMCs preset still sets its own values for PMCs.

### Bot recoil works as intended

- Fixed a bug that cancelled bot recoil almost immediately on about 1 shot in 4.
- Recoil now fades at the same rate no matter how often a bot updates.
- Sustained full-auto fire from bots is a little less accurate and behaves the same from bot to bot.

### Hearing follows EFT's headset rules

- Headsets built into helmets now count as headphones. Before, only the ear-piece slot counted.
- Any headset now removes the helmet hearing penalty, as it does in EFT.
- Armored face covers that muffle sound now count, and a new "Light Helmet" setting sits between no helmet and a heavy helmet.
- New default multipliers (unsuppressed gunshots are not affected):

| Bot gear | Before | Now |
|---|---|---|
| Headset, with or without helmet | 1.0 (0.8 with heavy helmet) | 1.0 |
| No headset, no helmet | 0.6 | 0.7 |
| No headset, light helmet | 0.6 | 0.63 |
| No headset, heavy helmet | 0.48 | 0.48 |

### Preset settings apply as soon as a bot spawns

Some bot settings used to be read only after the preset was saved from the in-game editor, and the Squad Talk settings were never read at all, so freshly spawned bots ran on built-in values. They now follow your preset from the start:

- **Taunts and enemy voice lines depend on personality.** Before, every bot taunted, and none faked death or begged.
  - GigaChad, Wreckless, SnappingTurtle and Chad taunt, and more often than before.
  - Rat, Timmy, Coward and Normal no longer taunt.
  - Each bot type's "Bot Taunts" setting is respected.
  - GigaChad, Wreckless and SnappingTurtle can fake death when solo and badly hurt, or when a grenade explodes near them mid-fight.
  - Timmy and Coward can beg for their lives.
  - SnappingTurtle, Chad, Rat and Timmy no longer answer enemy voice lines.
- **Friendly voice lines:** bots answer a friendly player's voice lines from up to 65 meters away instead of 60.
- **Grenades:**
  - The global "Bots Use Grenades" toggle and each bot type's grenade setting are respected.
  - Bots skip throws that need far more strength than the distance calls for, which mostly happens when you're well above them.
- **Squad callouts use the Squad Talk settings:**
  - "Reloading" calls: at most every 4 seconds instead of every second.
  - "Heard a noise" calls: 50% chance instead of 40%, but at most every 30 seconds instead of every second, so far less chatter.
  - Enemy direction calls ("on our six", "left flank"): 70% chance instead of 60%.

### "Not looking" reaction

- The "Bot Reaction Changes Toggle" now controls the small delay in how fast a bot spots you when your back is turned. Before, the delay applied even with the toggle off.
- The time limit now runs from when you enter the bot's line of sight and resets when sight is broken.
- The look angle now accounts for looking up or down at a bot.
- The accuracy penalty in these settings never did anything and has been removed.

### Extraction

- SAIN's own bot extraction, which was off by default, has been removed, along with its Looting Bots, Questing Bots and Realism integrations. On default settings nothing changes, and bots still use vanilla extraction or whatever a mod like ORBIT provides.
- The extraction calls other mods use (`ExtractBot`, `TrySetExfilForBot`, `GetExtractedBots`, `GetExtractionInfos`) still exist but do nothing and report no extracted bots.
- Bots always use the standard recoil and armor values, whether or not Realism is installed.

### Bots heal properly

Healing now relies on the forked BotHealingFixes, which this SAIN fork requires.

- **Surgery:**
  - Bots complete surgery and go from one destroyed limb to the next.
  - A bot whose surgical kit fails to start waits 10 seconds before trying again.
  - Stopping surgery no longer partly restores destroyed arms, legs and stomach for free.
- **First aid** is no longer cut off after 6 seconds, and is cancelled when an enemy becomes a threat.
- **Stims** are no longer cut off after 3 seconds.
- **Picking meds:** bleeding bots use a bandage, tourniquet or medkit that can stop the bleed, and surgical kits are only used on destroyed limbs.

### Bots reach cover when reloading or out of ammo

- A bot falling back to reload, or with an empty gun, keeps moving to cover instead of restarting its move every second or so.
- A reload that never finishes is cancelled after 16 seconds (magazine) or 40 seconds (loading rounds one by one), and the bot tries again.

### Other changes

- Removed settings that were never used, along with their code, such as the unstuck/teleport options, door kicking and the AI-vs-AI vision limit. None of it was running before.
- Safer handling of destroyed game objects, such as despawned players.
- Bots no longer miss a player's footsteps for the rest of a run or sprint after that player is briefly deactivated.
- A bot that can't find anywhere to escape a grenade waits a moment before searching again, instead of searching every frame.
- Bots crouch, stand up and lean at the same speed whatever the frame rate. Before, they did it faster the higher the frame rate.
- The preset editor's tabs have their padding back.