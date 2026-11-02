# Game mechanics reference

This page records how Escape from Duckov behaves, as far as it has been checked. It comes from the research notes of the
Newbe.Dod project (sections 1 to 8; sections 9 to 19 are the second batch, from research sections 7 to 15). Each statement carries one of three evidence levels:

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

## 9. Healing and eating

- **Static read**: drugs call `CharacterMainControl.AddHealth(float)`, which calls `health.AddHealth(value * (1 + HealGain))`,
  and `Health.AddHealth` clamps to `MaxHealth`. A negative value goes through `Health.Hurt` as damage with no source.
- **Static read**: food and drink call `AddEnergy(float)` and `AddWater(float)`, each `value * (1 + FoodGain)`, clamped to
  `MaxEnergy`/`MaxWater` and to zero.
- **Verified**: `CharacterMainControl.UseItem` first checks `UsageUtilities.IsUsable`. A medical item is not usable at full
  health (`usable=false`).
- **Verified**: event chain of a large first-aid kit (about 3 seconds): `hold` (the gun is put away), `actionStart CA_UseItem`,
  `useItem` (`OnMainCharacterStartUseItem`), then about 3 seconds later `addHealth`, `itemUsed`
  (`UsageUtilities.OnItemUsedStaticEvent`, with the item) and `hold` (the gun comes back). Healing is applied once when the use
  action ends, not over time. Eating follows the same pattern (a can takes about 2 seconds).
- **Verified**: `HealGain = -0.5` halves the healing (a first-aid kit with `addHealth value=20` gave 10 health). `FoodGain = -0.9`
  leaves about 10% of the food energy (`addEnergy value=40` gave about 4).
- **Verified**: a rule in the `AddHealth` prefix can rewrite the amount. A first-aid kit with `value=6` was halved to 3.
- **Verified**: overflow above maximum health (maximum 77 in the test). A first-aid kit gives only the missing amount, so it never
  overflows. A bandage always gives 5, so it overflows only above 72 health. Aspirin always gives 10, so at 70 health it overflows
  by 3 (`70 + 10 - 77`).
- **Static read**: `AddHealth` is also called by sources that are not items (for example the game's own "just survived" effect).
  Telling item healing from other healing by the time window between `OnMainCharacterStartUseItem` and `itemUsed` is
  **unverified**.
- **Helpers**: `RestoreRules.Register(kind, rule, priority)` rewrites the amount of a health, energy or water restoration
  (Harmony). `CharacterRestore.Heal`, `HealToFull`, `AddEnergy` and `AddWater` route a restoration through the game's methods and
  mark it as a mod restoration. `RestoreContext.Overflow` gives the overflow for each rule. `ItemUseEvents.OnStartUse` and
  `OnUsed` subscribe to the two item-use events.

## 10. AI control and hiding

- **Verified**: a Harmony prefix on `CharacterMainControl.SetMoveInput(Vector3)` that replaces the input with zero stops
  non-player characters. In a 5-second test, 2 of 3 enemies moved (15.1 units total); with the prefix on, none moved.
- **Verified**: calling `SetMoveInput` or `SetRunInput` from outside every frame does not work. The player's input script writes
  the input again each frame. The input must be replaced in a prefix.
- **Verified**: stopping movement does not stop shooting, turning or reloading. Enemies still shoot.
- **Unverified**: whether forced movement (for example the AI's roll, which uses `SetForceMoveVelocity`) and knockback continue
  while a move rule is active. Only `SetMoveInput` was tested.
- **Verified**: `CharacterMainControl.Hide()` hides a non-player character: the model and its health bar disappear and the
  `Hidden` flag is set. `Hide()` and `Show()` have no effect on the player team, including the player's pet.
- **Static read**: the player's aim assist skips hidden characters, so hidden enemies are not auto-aimed.
- **Verified**: hiding does not make AI blind or stop it from acting. Hidden enemies still act and shoot.
- **Static read**: the AI's check of whether the player hears a sound ignores hidden characters.
- **Unverified**: a rule that hides enemies until they fire, and when such a rule should show them again.
- **Helpers**: `CharacterControlRules.RegisterMove` and `RegisterRun` (Harmony) replace move and run input. Their limits are
  listed in the XML comments: forced velocity, knockback, aim and shooting are not covered. `CharacterVisibility.CanHide` and
  `CharacterVisibility.Hide` hide a non-player character with reference counting; a direct call to the game's `Hide()` or
  `Show()` bypasses the count.

## 11. Economy

- **Verified**: there are two kinds of money. The bank deposit is `EconomyManager.Money`, a `long` with a private setter. It changes
  with `EconomyManager.Add(long)`, which accepts negative values. Each change raises the static event
  `EconomyManager.OnMoneyChanged(old, new)`.
- **Verified**: cash on hand is `EconomyManager.Cash`, the number of items of type **451** (`CashItemID = 451`) in the inventory.
  It is read-only. `ItemUtilities.ConsumeItems(451, n)` removes cash (a test removed 5 from 74842, leaving 74837).
- **Verified**: `EconomyManager.Pay(cost, ...)` **deducts the bank deposit first** and takes cash only for the rest. A test paid 3
  from a deposit of 325848: the deposit became 325845 and the cash did not change. Do not use `Pay` to take cash.
- **Verified**: `EconomyManager.Add(100)` raised the deposit from 325845 to 325945.
- **Static read**: `ItemUtilities.ConsumeItems(int, long)` is `internal` in the game, so the SDK reaches it by reflection, once.
- **Helpers**: `Economy.Money`, `Economy.Cash`, `Economy.AddMoney`, `Economy.TryConsumeCash` (it never calls `Pay`, and it is
  all or nothing) and `Economy.OnMoneyChanged`.

## 12. Grenades and mines

- **Static read**: each grenade item has an `ItemSetting_Skill` whose `Skill` is a `Skill_Grenade`. That holds `grenadePfb` (a
  `Grenade` prefab) and the parameters `delayTime`, `isLandmine`, `landmineTriggerRange`, `canHurtSelf` and `damageInfo`.
- **Verified**: a grenade is made by `Object.Instantiate(grenadePfb)`, then configured, `Grenade.Launch(start, velocity, owner,
  canHurtSelf)` is called and `SetWeaponIdInfo(itemType)` follows. A zero velocity drops it in place.
- **Verified**: `Launch`'s owner decides the grenade's team (`selfTeam`), and the grenade ignores collisions with its owner for a
  short time after landing.
- **Verified** values (game version tested): hand grenade (type 67) radius 5.5, delay 1.2 seconds, damage 75, `normal` effect,
  exploding where it lands. Flash grenade (type 66) radius 8, delay 1.8 seconds, damage 1, `flash` effect; on explosion it adds the
  `Stun` buff to a character in range.
- **Verified**: a landmine with an enemy owner, placed 4 metres ahead, exploded when the player walked through its trigger range,
  at the mine's position. Damage 75 times difficulty 0.8 gave 60, and armour reduced the real loss to 15.77.
- **Static read**: a landmine triggers only for a character on a team different from its owner's (`filterByTeam`). A grenade with
  no owner has `selfTeam` set to the player team, so the player does not trigger it. The no-owner case was not tested separately.
  **Consequence**: a mine the player must trigger needs an owner on an enemy team.
- **Static read**: the flash grenade has no line-of-sight or facing check. It is an explosion effect plus the stun buff, so a player
  who turns away is still affected. Whether the stun affects shooting is **unverified**.
- **Helpers**: `Grenades.Spawn(itemType, position, owner, options)` and `GrenadeOptions`. The owner check for a landmine the
  player must trigger is described in the XML comments.

## 13. Bullet blockers

- **Verified**: a `BulletBlocker` on a collider on the `BlockBullet` layer stops bullets. Without a wall, a shot reaches the
  enemy (`gunHit`). With a wall between shooter and target, there is no `gunHit`, and the projectile ends at the wall with
  `traveled=0`. A shotgun fired through the wall at the player ended with all 5 pellets at `traveled=0`, and the player took no
  damage.
- **Verified**: the wall's team decides who it stops. A bullet is blocked when `Team.IsEnemy(bullet team, wall team)` is true. A
  wall on the player team (`team = player`) let the player's shots through, and the enemy's shots against it were not separately
  tested. With an enemy team, the player's bullets and those of other hostile teams are stopped.
- **Verified**: the wall has no health. A bullet that hits it disappears with no damage notification.
- **Unverified**: `bulletBack` (reflecting bullets). Making the wall destructible was not investigated.
- **Unverified**: whether the wall blocks character movement. Only bullets were tested.
- The wall is an ordinary scene object. It is removed when the scene changes, and a mod must destroy it itself on hot reload.
- **Helper**: `BulletBlockers.Spawn(position, rotation, width, team, height, thickness)`. The XML comments state the limits above.

## 14. Noticing

- **Verified**: `AICharacterController.SetNoticedToTarget(DamageReceiver)` is a continuous signal, not a one-time event. For one
  enemy, a postfix counted 287 calls in about 14 seconds (about every 0.05 seconds), each with the player's `DamageReceiver` as
  target. "First discovery" must be recorded by the caller.
- **Verified**: `noticed` on an AI is a pulse. After a sound, it was set to true and 0.24 seconds later back to false. A behaviour
  task clears it when it reads it, so a mod must check it every frame if it uses it.
- **Static read**: a sound and a hurt both set `noticed` directly without going through `SetNoticedToTarget`. A hurt by a buff or
  effect (poison, bleeding, burn) does not set it, because the game checks `isFromBuffOrEffect`. Discovery by sound or by being hurt
  therefore does not raise the `SetNoticedToTarget` callback.
- **Helper**: `EnemyNoticed.OnFirstNoticed(handler)` (Harmony) runs the handler once per enemy instance and clears its record on every
  level change.

## 15. Burn, stun, overweight and rolling

- **Verified**: burn (`Burn` buff) deals 1 point of real damage (`type = realDamage`, no source) every 0.5 seconds. It lasts up to 10
  seconds and stacks up to 3 times. The game ticks it itself; a mod only adds the buff.
- **Verified**: stun (`Stun` buff) reduces `Moveability` from 1.02 to 0.25 (two modifiers). Walk, run, turn and aim-turn speeds are
  multiplied by `Moveability`. The character can still move and use its hands but cannot run. Stun is a slowdown to about a quarter,
  not a freeze.
- **Verified**: overweight. When total weight is more than `MaxWeight` (79 in the test, with 84.8 carried), the game adds the
  `Weight_Overweight` buff, which sets `WalkSpeed` and `RunSpeed` to 0. `CanMove()` then returns false. Raising `MaxWeight` restored
  `WalkSpeed` to 4.
- **Static read**: the footstep sound is multiplied by 1.5 when weight is above 75% of `MaxWeight`, and not while aiming down sights.
- **Verified**: rolling starts only when `DashSpeed >= 0.1`. The base `DashSpeed` is 10. Setting it to 0 stopped `Dash()` from
  starting (no `actionStart`). The roll costs 10 stamina, has a cooldown of 0.5 seconds and lasts about 0.48 seconds. Its end has no
  event; a mod must watch `Dashing` each frame.
- **Static read**: while rolling, `CanMove` and `CanRun` are false. `CanUseHand` and `CanControlAim` depend on `DashCanControl`
  (base 0).
- **Unverified**: that a character can fire while rolling with `DashCanControl` above 0.
- **Verified**: a character that is rolling is not hit by bullets or explosions (the damage pipeline and explosion targeting in the
  sections above).
- **Unverified**: whether forced roll movement (`SetForceMoveVelocity`) is covered by the move-input rules. It is not.
- **Helpers**: `StatKeys.DashSpeed`, `StatKeys.DashCanControl`, `StatKeys.Moveability`, `StatKeys.WalkSoundRange` and the other
  constants in `StatKeys`, for use with `StatBuff.Modify`.

## 16. Loot and pickup

- **Verified**: dropping the item in slot 0 raised `OnMainCharacterInventoryChangedEvent` with index 0 and created an
  `InteractablePickup` on the ground.
- **Verified**: picking it up raised, in one frame, `interactStart`, the inventory event with index 0,
  `InteractablePickup.OnPickupSuccess(pickup, character)` and `actionStart CA_Interact`.
- **Verified**: the first opening of a box raises `OnStartLoot` with `Looted` false. Stopping raised `OnStopLoot` about 2.3 seconds
  later. Opening the same box again raised `OnStartLoot` at once with `Looted` true.
- **Verified**: `Inventory.hasBeenInspectedInLootBox` becomes true only after `OnStopLoot`. It cannot tell whether a box is opened
  for the first time when the loot starts. Use `InteractableLootbox.Looted`, read at `OnStartLoot`.
- **Static read**: corpse loot boxes are ordinary `InteractableLootbox` objects. The pet's inventory is `PetProxy.PetInventory`,
  and the SDK identifies a pet box by comparing the box's `Inventory` with it. The research note describes the pet as an
  `InteractableLootbox` subclass; the decompiled `PetProxy` is a `MonoBehaviour`, so the comparison is used instead.
- **Unverified**: whether an inventory slot that is empty is `null` in `Inventory.Content`, and whether `Item.MaxStackCount` can be
  changed per item instance to double stack limits.
- **Helpers**: `LootEvents.OnLootStart(handler, includePets)` (pets are excluded by default), `OnLootStop`, `OnPickup`,
  `OnPlayerInventoryChanged` (main character only) and `LootEvents.IsPet`.

## 17. Sounds

- **Verified**: a player's `WalkSoundRange` base is 8 and `RunSoundRange` base is 16. An enemy's walking makes an `unknowNoise`
  sound with radius 8 about every 0.33 seconds. `HearingAbility` base is 1.
- **Static read**: an AI compares the sound distance with the sound radius multiplied by its own `HearingAbility`, so the
  effective radius differs for each listener.
- **Static read**: an AI ignores a sound from its own team for `unknowNoise` and `combatSound`. A sound meant to attract enemies must
  therefore carry a team other than the enemy team it targets. Grenade drops make a `grenadeDropSound` of radius 20.
- **Helper**: `AiSounds.Make(position, radius, type, source)`. A negative, infinite or NaN radius is rejected; zero is accepted.

## 18. Explosions: targeting

- **Verified**: who is hurt depends on the source and on `canHurtSelf`. With no source and `canHurtSelf` true, the player is hurt. With
  the player as source and `canHurtSelf` false, the player is not hurt. With the player as source and `canHurtSelf` true, the player
  is hurt. With no source and `canHurtSelf` false, the player **is hurt**, because the team used for exclusion stays `all`.
- **Static read**: the game excludes the source's team only when a source is present and `canHurtSelf` is false, using
  `Team.IsEnemy`. `Team.IsEnemy` is false for `middle` and true for `all`; other teams are enemies of each other.
- **Verified**: explosion damage is normal damage, reduced by armour and difficulty. A 10-point explosion took 2.48 from the tested player.
  Real damage (`damageType = realDamage`) ignores both.
- **Static read**: the explosion manager sets `isExplosion` on the damage it creates.
- **Verified**: `ExplosionFxTypes.normal` and `flash` make an effect; `fire`, `ice` and `custom` do not, so the caller must show its own.
- **Unverified**: that an explosion targeting only enemies (source = player, `canHurtSelf` false) hurts enemies; the test had no enemy in range.
- **Static read**: one explosion affects at most eight colliders, and a character behind a wall is not hurt.
- **Helper**: `Explosions.Create(center, radius, damage, source, canHurtSelf, effect, isTrueDamage)`. It validates the radius and the
  damage, and passes a camera shake of zero.

## 19. Player attributes

The table of all 71 player attributes is not included yet. The exact names must be read from the game (the Newbe.DebugMod
`research/stats` output or the stat database). The research notes name only 57 of them, and this page does not guess the rest.
The attribute keys named in `StatKeys` are the ones the research validated.

## 20. Mouse aiming (`MouseAimControl`)

- **Static read**: inside a level the game hides the cursor and turns mouse movement into the aim point. `InputManager.InputActived`
  is false while its source set `blockInputSources` is not empty or its 2-frame cool-down is above zero. `DisableInput(GameObject)`
  adds a source and sets the cool-down; `ActiveInput(GameObject)` removes the source. The game's own `View` screens use this pair
  (`OnOpen` disables, `OnClose` activates).
- **Static read**: while `InputActived` is false, the cursor is shown, the aim point follows the real mouse position and stays
  frozen (`SetAimInputUsingMouse` returns before `SetAimPoint`), fire is forced off, move input is forced to zero, and the item
  shortcuts, the interact HUD and camera edge-pan stop.
- **Static read**: movement, firing and aiming share this one flag, so there is no aim-only block; it would need Harmony patches on
  private members of `InputManager`.
- **Static read**: each frame `UpdateInputActived` removes every source that is `null` or not `activeInHierarchy`, so a deactivated or
  destroyed source releases itself.
- **Static read**: `DisableInput` checks the `InputManager` instance; `ActiveInput` does not and throws when it is missing. The
  helper checks `LevelManager.Instance.InputManager` before each call.
- **Helper**: `MouseAimControl.StopAiming()` returns a handle; `StopAiming(owner)` ties the stop to a `GameObject`;
  `RestoreAiming()` releases every stop taken through this copy of the SDK; `IsAimingStopped` reports whether a stop is still in force.
- **Side effect**: stopping aiming also stops movement, firing, item shortcuts and camera edge-pan, because they share the flag above.
  A panel that needs the mouse only should not use it.
- **Level start**: a stop taken outside a level has no effect on the game. Every stop is forgotten when a level is initialized
  (`LevelEvents.OnInitialized`), and a handle taken before that does nothing when disposed.
- **Release rules**: release the handle when the panel closes, on Esc and on scene change. Add it to `ModBehaviourBase.Subscriptions`
  so it is released when the mod is disabled. A stop of an owner that is destroyed or deactivated stops counting within a frame,
  because the SDK's holder object prunes it every frame. A panel that reopens must take a new stop.
- **Counting**: stops are counted per owner object and per SDK copy. The copies of two mods do not share counts, but the game keeps
  both sources in its set, so input stays blocked until both release.
- **Main thread**: every call touches Unity objects, so call it on the Unity main thread only.
- **Static read, not yet verified in the game** (task 4.2): the cursor becomes visible and clicks reach the panel, the aim stays
  put, movement and firing stop, the click that closes the panel does not fire, aim resumes from the cursor, and deactivating an
  owner without disposing restores input.

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
- **`MouseAimControl`** (no Harmony): stops mouse aiming while a panel is open and gives it back on release (section 20).
  Stopping aiming also stops movement and firing.
