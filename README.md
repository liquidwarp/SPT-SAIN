# Solarint's AI Modifications

Fork of SAIN.

## What's new in 5.0.0 (SPT 4.1.6)

How bots behave differently in raid compared to the previous release.

### Aim tightens over the whole fight

- Bots start an engagement with loose aim and get steadily more precise for as long as they stay on you, down to their "max aiming upgrade by time" limit.
- Long bursts, or you moving or strafing, no longer reset their aim to its starting precision mid-fight.
- If you peek again from the same spot, the bot keeps the precision it has already built up.
- The first shots are still forgiving, but long fights and repeated peeks from one spot are noticeably deadlier.

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

Some bot settings used to be read only after the preset was saved from the in-game editor, so freshly spawned bots ran on built-in values. They now follow your preset from the start:

- **Taunts and enemy voice lines depend on personality.** Before, every bot taunted, and none faked death or begged.
  - GigaChad, Wreckless, SnappingTurtle and Chad taunt, and more often than before.
  - Rat, Timmy, Coward and Normal no longer taunt.
  - GigaChad, Wreckless and SnappingTurtle can fake death when solo and badly hurt.
  - Timmy and Coward can beg for their lives.
  - SnappingTurtle, Chad, Rat and Timmy no longer answer enemy voice lines.
- **Grenades:**
  - The global "Bots Use Grenades" toggle and each bot type's grenade setting are respected.
  - Bots skip throws where the grenade would land well past you, such as when you're much higher or lower than them.
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

### Note on presets

The new default hearing and squad talk values only apply if your preset uses the defaults. Values you've changed and saved in your own preset are kept.

### Other changes

- Removed settings that were never used, along with their code, such as the unstuck/teleport options, door kicking and the AI-vs-AI vision limit. None of it was running before.
- Safer handling of destroyed game objects, such as despawned players..
