# Game mechanics reference

This page records how Escape from Duckov behaves, as far as it has been checked. It comes from the research notes of the
Newbe.Dod project (sections 1 to 8). Each statement carries one of three evidence levels:

- **Verified**: observed in the real game with the debug tooling, on the game version that was tested.
- **Static read**: read from the decompiled game code only. It was not run in the game, so it can be wrong.
- **Unverified**: a question that was not checked, or a guess. Do not rely on it without testing.

Statements describe the game version the research was done on. A game update can change them.

## 1. Buff resistance (`buffResist`)

- **Verified**: `CharacterMainControl.buffResist` is a public list of `Buff.BuffExclusiveTags`. A buff whose tag is in the list
  is not added. Adding a tag blocks later buffs with that tag, and removing the tag lets them through again.
- **Verified**: buffs that are already on the character are not removed when the tag is added. Remove them yourself, for
  example with `CharacterMainControl.RemoveBuffsByTag(tag, false)`. The call itself is a static read, not a verified result.
- **Verified**: the tag belongs to the buff prefab, not to the kind of effect. Fracture and wound buffs carry `NotExclusive`,
  so resisting `Bleeding` does not stop them.
- **Verified**: `NotExclusive` must not be put in `buffResist`. It blocks ordinary buffs as well.
- **Verified**: buffs with the same tag but a different ID replace each other. Adding a poison-low buff while a poison buff is
  active also adds a nausea buff, so full poison immunity needs `Nauseous` as well.
- **Verified**: `buffResist` is cleared on every scene change. It must be written again after each
  `LevelManager.OnAfterLevelInitialized`.
- **Verified**: equipment with resistance keeps adding a hidden, short-lived `Bleeding` buff about once a second. Those
  buffs and `buffResist` are two separate mechanisms that can be used together.
- **Verified**: poison damage carries `isFromBuffOrEffect = true` and no attacker. Use that flag to tell continuous damage
  from a hit.

Tags: `NotExclusive`, `Bleeding`, `Starve`, `Thirsty`, `Weight`, `Poison`, `Pain`, `Electric`, `Burning`, `Space`,
`StormProtection`, `Nauseous`, `Stun`, `Ghost`, `Freeze`, `Tagilla`, `BlueBall`, `RedBall`.

Examples of buff IDs by tag (from the game's buff list): poison 1061, 1122, 1062, 1063; electric 1071, 1128; bleeding 1001,
1002; pain 1081; burning 1121, 1125; nausea 1123.

## 2. Enemy spawn and readiness

- **Static read**: spawning is asynchronous, and the game raises no public "spawn complete" event.
- **Verified**: a postfix on `CharacterMainControl.SetTeam` runs once for every spawned character, except the player.
- **Verified**: when that hook runs, the AI controller, the held gun, the initial items, the preset buffs and `buffResist`
  do not exist yet.
- **Verified** (52 enemies on one level): the AI controller appears within 0 to 1 frame. The main weapon-slot item appears
  within 1 frame for 43 of them; the rest have no main weapon, for example melee enemies. Initial inventory items appear
  within 1 frame. Only 2 of the 52 took out a gun within 15 seconds.
- **Consequence**: wait one or two frames before changing an enemy. Detect guns from the main weapon slot, not from the held
  gun (`CurrentHoldGun`).
- **Unverified**: `AICharacterController.Init` as a later hook.
- **Verified**: entering a raid spawns more than 40 enemies within about five seconds, in batches 0.1 to 0.5 seconds apart,
  plus a batch three seconds in. Spawning is not continuous.
- **Verified**: teams are not only "enemy". `scav`, `usec`, and `wolf` are used, and `all` is used by a merchant and a melee
  enemy. Do not treat "not the player's team" as "enemy", because that counts the merchant as an enemy.
- **Verified**: pets use the same spawn hook, with team `player`.
- **Verified**: enemies beyond 50 metres are dormant (`SetActiveByPlayerDistance.Instance.Distance` is 50). Their game objects
  are inactive, so `FindObjectsOfType` does not find them. Use `Resources.FindObjectsOfTypeAll` or keep your own registry.
- **Consequence**: dormant enemies do not run their AI. An effect that advances enemies over time must decide what happens
  to dormant ones.

## 3. AI perception and sound

- **Static read**: every sound goes through `AIMainBrain.MakeSound(AISound)`. `AISound` has a position, a radius, a team, a
  sound type (`unknowNoise`, `combatSound`, `grenadeDropSound`, `alert`), and the sources. `AIMainBrain.OnSoundSpawned` is
  raised for every sound.
- **Static read**: an AI ignores sounds from its own team. It compares the horizontal distance with `radius × hearing`; a
  sound that is close enough sets `noticed`.
- **Verified**: walking enemies emit `unknowNoise` with radius 8 about every 0.33 seconds. Enemy hearing ability is 1.
- **Verified**: a fake `combatSound` at the player's position, radius 40, made an enemy 23 metres away `noticed` in the same
  frame. Radius 10 at the same distance did nothing. The sound's source must be the player for the AI to target the player.
- **Verified**: `noticed` is a pulse. It becomes true, then false about 0.24 seconds later, and the behaviour tree clears it
  when it reads it. To detect the first notice, check every frame and remember it.
- **Static read**: `OnHurt` sets `noticed` only when the damage is not from a buff or an effect.
- **Unverified**: silencing the player by setting `WalkSoundRange` and `RunSoundRange` to 0 (the sound call still happens,
  with radius 0).
- **Unverified**: a multiplier on footstep range for overweight.
- **Not investigated**: stopping enemy movement, and making enemies not see the player.

## 4. Damage pipeline (`Health.Hurt`)

- **Static read**: every damage goes through `Health.Hurt(DamageInfo)`. The order is: scene loading, invincible and dead
  checks (each returns `false` with no event); damage-carried buffs; bleeding buffs; the critical roll (written to
  `damageInfo.crit`); the player-team difficulty factor (`DamageFactor_ToPlayer`) unless ignored; armor, unless the damage is
  real or ignores armor (`2 / (armor − penetration + 2)`, using head armor for critical hits and body armor otherwise);
  element resistance; a minimum of 1; the health write; then, on death, `OnDead` and deactivation, and only after that
  `OnHurt`.
- **Verified**: `OnDead` fires before `OnHurt`.
- **Verified**: `finalDamage` in `OnHurt` is capped at the current health plus 1. A 1000-point hit on a 23-health enemy
  reports `final = 24`. Do not use `finalDamage` to measure overkill; read the health and the raw damage in the prefix.
- **Verified**: when the target is invincible, `Hurt` returns `false`, health does not change and `OnHurt` does not fire,
  but a `Hurt` prefix still runs with `Invincible` true. A rule must check `Invincible` and `IsDead` itself.
- **Verified**: a prefix that rewrites `damageValue` through `ref` changes the outcome. Twenty real damage with a 0.5
  multiplier became ten damage, and health went from 77 to 67.
- **Verified**: a prefix can read the victim's current and maximum health, the raw damage, the damage type, whether armor and
  difficulty are ignored, the critical rate, penetration, body and head armor, invincible, dead, explosion, plus
  `fromCharacter`, `fromWeaponItemID`, `damagePoint` and `damageNormal` (the last four from the structure).
- **Verified**: a 10-point explosion on the player, with difficulty factor 0.8 and body armor 4.45, did 2.48 damage: the
  difficulty factor gives 8, and armor gives `2 / (4.45 + 2) = 0.31`. The `damage` in the event is the value after the
  difficulty factor, not the raw value.
- **Verified**: damage from `damage-player` had `from = none`; damage applied to enemies had the player as `from`.
- **Unverified**: the victim's facing, which a "from behind" effect would need. Not confirmed.

Guns and bullets:

- **Verified**: `ItemSetting_Gun.onHurtEnemyEvent` can be subscribed from outside. It fires on a hit with the
  `DamageReceiver` and the `DamageInfo`. The `crit` field is **-1** at that point, because criticals are rolled later inside
  `Health.Hurt`. Use `OnHurt` to learn about a critical.
- **Verified**: the gun event belongs to one gun object. After the held gun changes, subscribe again.
- **Verified**: a bullet's `penetrate` value goes down by one when it hits a damageable target. A bullet that hits a wall
  keeps its value. A bullet that hits nothing ends without a `gunHit`.
- **Rule of thumb, not separately verified**: a bullet with no `gunHit` within about 0.3 seconds of firing missed.
- **Verified**: every bullet hit on the player adds a `Pain` buff. With `Pain` resistance, that buff is refused.
- **Verified**: a shotgun shot creates five bullets (observed on an enemy shotgun).
- **Static read**: a player who is dashing is not hit by bullets or explosions. Player bullets do not hit the player.
- **Static read**: a bullet that hits a reflecting shield is sent back at 1.5 times the damage and always crits.
- **Not investigated**: bullets passing through walls (would need the bullet's layer mask changed).

## 5. Explosions

- **Verified**: `LevelManager.Instance.ExplosionManager.CreateExplosion(center, radius, DamageInfo, fxType, shake, canHurtSelf)`
  can be called directly.
- **Static read**: the explosion collects receivers with a sphere overlap buffer of 8 colliders, so at most 8 characters are
  affected per explosion. Walls block it (a ray check). Each character is hit once. A dashing character is immune.
- **Verified** (explosion centred on the player):

  | Source `fromCharacter` | `canHurtSelf` | Player hurt |
  |---|---|---|
  | none | true | yes |
  | player | false | no |
  | player | true | yes |
  | none | false | yes |

  The reason: without a source the team is "all", and only a source with `canHurtSelf = false` filters by its team. The
  filter is `Team.IsEnemy`, where `middle` is not an enemy of anyone and `all` is an enemy of everyone.
- **Unverified**: "hurt only the player" explosions. The static read suggests a source from the enemy team with
  `canHurtSelf = false`, but this was not tested. Whether the player-sourced "hurt only enemies" setup works on enemies was
  also not tested, because no enemy was in range.
- **Verified**: explosion damage is a normal damage type, so difficulty and armor apply (10 became 2.48 on the player).
  Real damage ignores both (static read).
- **Static read**: effect types `normal` and `flash` create a prefab; `fire`, `ice` and `custom` create nothing, and the
  caller must add its own effect. **Unverified**: whether `flash` blinds the player or enemies.
- **Static read**: explosions come from grenades (including landmines, `isLandmine`), breakables, item actions, projectiles and
  a few enemy attachments. **Unverified**: how to create a Widow mine from the original prefab.

## 6. Weather and clock

- **Static read**: `GameClock.Instance` is the scene clock. A day is 86300 seconds. `clockTimeScale` is game seconds per real
  second. Phases: night before 05:00 and from 19:00, day from 05:00 to 16:00, dawn from 16:00 to 19:00.
- **Verified**: `clockTimeScale` was 40 in the tested scene (the code default is 60; the value comes from the prefab).
- **Verified**: `GameClock.Instance.StepTimeTil(22:00:00)` sets the hour to 22 and makes `TimeOfDayController.Instance.AtNight`
  true. Setting `clockTimeScale` to 0 froze the time for three seconds.
- **Static read**: at dawn (16:00 to 19:00) `AtNight` is false. Whether dawn counts as day is a design choice.
- **Verified**: `WeatherManager.SetForceWeather(true, Sunny)` changes the weather within one second. `SetForceWeather(false, ...)`
  returns to the natural weather.
- **Verified**: forcing `Stormy_I` spawned several storm creatures (wolf team, 120 health each) and gave the player a `Space`
  buff. Storms have side effects, so do not use them as ordinary weather. Forcing `Snow` produced no extra events.
- **Static read**: rain changes element factors only (fire damage −15%, electric damage +20%), and not inside the base.
- **Static read**: storms are gameplay systems (`OnStormStarted`, `OnStormEnded`, `Storm.GetStormLevel`), not only visuals.
- **Unverified**: side effects of freezing the clock. `clockTimeScale` may also drive hunger, food spoilage, shop restocking and
  `GameClock.OnGameClockStep` subscribers. Not investigated.
- **Not investigated**: whether the moon can be hidden, which is a sky configuration question.
- **Static read**: no acid-rain weather was found in the game. An acid-rain effect would need its own damage-over-time logic.

## 7. Rolling, running and stamina

- **Static read**: `CharacterMainControl.Dash()` needs `DashSpeed >= 0.1` and either no attack in progress or an attack that
  has already dealt damage. The dash action has a 0.5-second cooldown and needs `staminaCost` (10) to start.
- **Verified**: the base values are `DashSpeed` 10 and `DashCanControl` 0. `Dash()` raises `OnActionStartEvent` with a `CA_Dash`
  action, `Dashing` is true for 0.48 seconds, the displacement was about 4.1 units, and stamina dropped by 10.
- **Verified**: with `DashSpeed` set to 0, `Dash()` does not start.
- **Verified**: the dash **end** has no event. Read `Dashing` every frame to get it.
- **Verified**: stamina `104`, drain `6.6` per second while running, recovery `20.8` per second, recovery delay `1.5` seconds.
  Running for three seconds dropped stamina from 103.7 to 84.
- **Static read**: `UseStamina(float)` spends stamina. Setting `CurrentStamina` directly has no public setter.
- **Verified**: when total weight exceeds `MaxWeight`, the game adds the `Weight_Overweight` buff (id 1024). It sets walking and
  running speed to 0, and `CanMove()` returns false. The test used 84.8 weight against a limit of 79.
- **Verified**: `CharacterMainControl.SetMoveInput` and `SetRunInput` called from outside every frame have no effect, because
  the input script writes them again each frame. To control movement, patch those methods as a prefix.
- **Static read**: a dashing character can use its hands and aim if `DashCanControl` is above 0. **Unverified**: that firing
  actually works during a dash.
- **Unverified**: whether stamina recovers while standing still and drains while moving, as a "reverse" effect would need.
  The methods were not tested.

## 8. Loot boxes and interaction

- **Static read**: `InteractableBase.OnInteractStartStaticEvent` is raised for any interaction. `InteractableLootbox.OnStartLoot`
  is raised when the search starts and `OnStopLoot` when it ends. `CA_Interact` is raised through `OnActionStartEvent`.
- **Unverified**: `InteractablePickup.OnPickupSuccess`.
- **Verified**: opening a box the first time gives `interactStart`, `actionStart CA_Interact`, `lootStart` with `Looted` false,
  and after about 2.3 seconds `lootStop` with `Looted` true. Opening the same box again raises `lootStart` immediately, with
  `Looted` true.
- **Verified**: `Inventory.hasBeenInspectedInLootBox` becomes true only after `OnStopLoot`. Use `InteractableLootbox.Looted`
  to tell a first opening, read in `OnStartLoot`.
- **Verified**: the box closes itself about 2.3 seconds after opening (cause not investigated).
- **Static read**: corpse loot boxes are also `InteractableLootbox`, and so is the pet proxy. Exclude them by name (`LootBox_*`)
  or by object type.
- **Unverified**: which boxes need inspection (`needInspect`). Both boxes tested had an inspection time of 0.
- **Not investigated**: how cash is stored (`EconomyManager` or an inventory item).
- **Verified**: items can be created with `ItemAssetsCollection.InstantiateSync(typeId)` (used by the research tool).

## Helper usage in this SDK

The helpers below implement parts of the mechanics above. The Harmony-based ones are compiled only when the mod sets
`IncludeHarmony` to true (the build then defines `DUCKY_SDK_HARMONY`).

- **`BuffImmunity.Grant(tag)`** (no Harmony): returns a handle. `NotExclusive` throws `ArgumentException` before any change.
  Existing buffs with the tag are removed. The resistance entry is reapplied after each `OnAfterLevelInitialized` (section 1).
  The SDK removes only the entry it added, and disposing the handle twice is safe. Each compiled copy has its own grants.
- **`StatBuff`** (no Harmony): subclass it and call `Modify(key, type, value)` from `ApplyModifiers()`. Modifiers are removed
  when the buff is destroyed. Applying the same buff again replaces its earlier modifiers.
- **`PlayerCombat.OnHit` and `PlayerCombat.OnKill`** (no Harmony): handlers for player hits and kills, filtered with
  `PlayerCombat.DamageFromPlayerToEnemy`. The game event is subscribed once per copy and released with the last handler.
- **`HurtRules.Register(rule, priority)`** (Harmony): a rule receives `ref DamageInfo` and a read-only `HurtContext`. Rules run by
  priority, then registration order. A rule that throws is logged and skipped. Use this for damage rewriting, and check
  `HurtContext.Invincible` and `HurtContext.IsDead` (section 4).
- **`LethalDamageGuard.Register(rescue)`** (Harmony): a rescue is asked only for a lethal write on a target the game would hurt.
  It may return a positive health to keep (section 4).
- **`EnemyReady.Subscribe(handler)`** (Harmony): the handler runs once per enemy, after its AI and main-slot item exist, within
  at most 10 frames (section 2). `HasGun` comes from the main weapon slot, not from the held gun.
- **`ItemSetting_GunExtensions.OnHurtEnemy`** (no Harmony): the gun hit event, with `crit` equal to -1 (section 4). Subscribe
  again after the held gun changes.
- **`WeatherManagerExtensions.SetForceWeather`, `GameClockExtensions.StepTimeTil`** (no Harmony): forward to the game's static
  and instance members.
- **`Dice`, `Clock`, `EffectTrace`** (no Harmony): controllable randomness, a skippable clock (a negative skip is rejected) and an
  in-memory event trace. They keep state per compiled copy.
- **`Item.Drop` and `InteractableLootbox.Looted`**: game members, not SDK wrappers. The SDK adds no overload with the same
  name, because a same-named extension in the SDK would make calls ambiguous in mods that also see the game's extension.
