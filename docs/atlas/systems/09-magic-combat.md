# 09-magic-combat

> Kapsam: buyu (spell katalogu, cast zinciri, ward/cooldown tick'i) + savas (melee cozumu,
> dunya karsilasmasi, encounter leglari), vitals havuzlari, olum/corpse/respawn akisi.
> Her iddia `file:line` ile kanitlidir; emin olunamayanlar "dogrulanmadi" olarak isaretlendi.

## HLD - Ne ve Neden

Buyu+savas sistemi, Daggerfall-esintili gercek zamanli (pause'suz) bir birinci-sahis
dovus dongusunu deterministik bir simulasyon cekirdegi uzerine oturtur. Oyuncu E ile
bir dusmana baglanir ("world encounter"), attack tusuyla `TryMeleeStrike` atar, 1-8
tuslariyla 8 buyuluk okulu kullanir; dusman kendi ~2.4 saniyelik kadansiyla karsi vurur.
Tum zarlar seed'li `XorShiftRng` ile atilir ki save/load sonrasi ayni dunyada ayni
sonuclar cikssin — "feel" katmani (billboard flash, corpse pozu, bolt vfx) tamamen
Presentation'da yasar ve sim'e geri yazmaz. Vitals uc havuzdur (Health/Fatigue/Mana,
immutable `VitalStat`); Health 0 = olum. Dusman olumu yagma+XP+bounty'yi kapatir ve
ceset olarak yerde kalir (respawn'i bloklar); oyuncu olumu bir duvar degil bir bedeldir:
%20 altin, +8 saat, plaza'da uyanis. Felsefe: "pure cekirdek + adapter orkestrasyonu" —
Domain/Simulation katmani Unity'siz ve testlerle pinli, adapter (DomainSimulationAdapter
partial'lari) canli oyunun tum kablolamasini yapar. Tarihsel borc: ayni is icin uc savas
cekirdegi ve iki buyu boru hatti birikmistir (asagida "Bilinen Borclar").

## HLD - Akis

### A. Dunya karsilasmasi (canli oyun, frame kadansi)
1. **Baglama**: E ile etkilesim → hedef Outlaw (veya bounty varken Guard) ise
   `TryBeginWorldEncounter` dusmani `_worldEncounterId`'ye baglar, `WorldEncounterSignal`
   + ses sting'i raise edilir (DomainSimulationAdapter.WorldEncounter.cs:51-68). Alternatif:
   `TickHostileAi` kovalayan dusman ≤3 hucreye gelince otomatik baglar (WorldEncounter.cs:279-280).
2. **Oyuncu vurusu**: HUD/attack → `TryMeleeStrike` — hedef cozumu (isimli veya en yakin
   dusman, Chebyshev ≤6), F23 suc kontrolu (sivile vurus = bounty +40g), seed'li RNG kurulumu,
   `CombatActionResolver.Resolve` (DomainSimulationAdapter.Combat.Melee.cs:126-236).
3. **Zar zinciri**: stamina kapisi → hit roll (baz 50 + acc − dodge, klamp 15..95) → hasar
   rolu (base+band−armor) → F28 ward emilimi (`defenderMitigation`) → health mutasyonu →
   `CombatResolved` eventi (CombatActionResolver.cs:49-79, CombatHitRollService.cs:20-29).
4. **Dusman karsi vurusu**: HUD pompasi her unpaused frame `TickWorldEncounter` cagirir;
   2.4s kadans, menzil ≤6, ayni resolver, oyuncunun ward'i mitigation seam'inden gecer
   (WorldEncounter.cs:290-328).
5. **Kapanis**: her combat-screen okumasi `SettleWorldEncounterIfOver` calistirir — dusman
   oluyse 25g spoils, +40 XP, bounty questi tamamlanir, `RaiseFelled` ile corpse pozu,
   final Warden ise ana quest finali (WorldEncounter.cs:966-992; CombatScreen.cs:394-395).

### B. Buyu cast'i (canli oyun, tus basimi)
1. 1-8 tuslari `EmberPlayerSpellCaster.Cast` → modal acik degilse
   `adapter.TryCastSpell(slot)` (EmberPlayerSpellCaster.cs:46-94).
2. Adapter: bilinen buyu listesi (`PlayerKnownSpellIds`, bos ise tum katalog fallback),
   mana on-kapisi, canli rig pozisyonunun ActorRecord'a senklenmesi (F28 root fix),
   `SelectSpellTarget` ile dost/dusman niyetine gore hedef secimi — dusman buyusu hedefsizse
   durust ret (DomainSimulationAdapter.Combat.Spells.cs:260-314).
3. `SpellExecutionService.TryExecute`: TryPrepareCast (bilinirlik+cooldown+mana) →
   SpellTargetValidator → CanResolve on-izleme → CommitPreparedCast (mana dusumu +
   cooldown baslatma) → ResolveInstantaneousEffects (SpellExecutionService.cs:95-201,
   SpellCastingService.cs:43-99).
4. Adapter post-islem: `SpellResolved` eventi, hit-flash feed'i, timed ShieldBuff'larin
   `PlayerShieldBuffs`'a yazimi, open-set kodlarin ("light"/"haste"/"recall") template-id
   ile Presentation'da karsilanmasi — recall oyuncuyu yerlesim merkezine tasir
   (Combat.Spells.cs:329-374).
5. Cast basariliysa `FlyBolt` vfx'i ucar; bolt rengi hasar tipine gore boyanir
   (EmberPlayerSpellCaster.cs:89-93, 111-120).

### C. Tick kadansi (simulasyon)
1. `WorldTickComposer` her tick `MagicStep`'i (PerTick:20, TimeStep'ten hemen sonra)
   kosturur: `MagicTickDriver.AdvanceTicks(PlayerSpellCooldowns, PlayerShieldBuffs, delta)`
   (DefaultTickSystems.cs:105-121; WorldTickComposer.cs:79).
2. NPC-vs-NPC savas: `PredationStep` (Hourly:40) avcilar sivilleri tokatlar ama olduremez
   (0'a dusen sivil 1 HP'de "mauled" yasar), Guard'lar avcilari StrikeReach'te vurur;
   `CompanionGuardStep` (Hourly:42) yoldaslar oyuncunun yanindaki dusmanlari vurur
   (CascadeSystems.cs:18-75; DefaultTickSystems.cs:54-55, 316-324).

### D. Olum → corpse → respawn
1. `ActorVitals.IsDead => Health.IsDepleted` (ActorVitals.cs:21); `ActorRecord.IsAlive`
   bunun tersidir (ActorRecord.cs:78).
2. **Dusman**: `SettleWorldEncounterIfOver` → `WorldCombatFeedbackFeed.RaiseFelled` →
   `ActorCombatFeedbackView` billboard'u yatirir + griler ("a corpse, not a vanish",
   ActorCombatFeedbackView.cs:8, 87). Ceset ActorStore'da kalir: haunter respawn'i
   "alive or corpse — never duplicate" kuraliyla bloklanir
   (DomainSimulationAdapter.Haunters.cs:39, 127); olu aktorler aclik tick'inden muaf
   (DefaultTickSystems.cs:373); dunya projeksiyonu cesetleri ayakta dogurtmaz
   (DomainSimulationAdapter.WorldProjection.cs:163).
3. **Oyuncu**: HUD digest'inde Hp≤0 gorulunce `InGameUiController` olum ekranini tam bir
   kez acar (`_deathScreenShown` kapisi, InGameUiController.cs:293-300); "Awaken" →
   `RespawnAfterDeath`: %20 altin bedeli, uc vital'in Refill'i, encounter unbind, saat
   saat +8 saat (tek atlamali tick hourly kadanslari kacirir — banked shipcheck-6 dersi),
   mevcut yerlesimin plaza merkezine MoveTo (WorldEncounter.cs:336-362). Olum ekrani kapisi
   respawn'da sifirlanir ki sonraki olum ekrani tekrar acabilsin (InGameUiController.cs:337, 352).
4. **Yoldas**: dusen companion rosterdan "gurultuyle" cikar — olum bir hikaye beat'i
   (CompanionSystem.cs:75).

### E. Encounter leglari (LOOP-PROOF tanilari)
"Leg" kavrami kod tarafinda `--ember-looptest` proof surucusunun bacaklaridir — uretim
yollarini (binding, resolver zarlari, spoils, bounty) headless kosturup transcript satiri
dondururler (WorldEncounter.cs:70-73):
- `ProofRunEncounterLeg` — outlaw'a 250 swing butcesiyle tam dovus; yol dogrulanir, denge
  degil (WorldEncounter.cs:847-878).
- `ProofDieAndRespawn` — bilerek kaybedilen AFK duello + respawn bedel raporu
  (WorldEncounter.cs:366-407).
- `ProofBindDungeonHaunter` / `ProofBindDelveWarden` / `ProofFinishBoundEncounter` —
  zindan/boss leglari, corpse pozu yakalanabilir (WorldEncounter.cs:777-845).
- `ProofBindEncounterForMusic` — BATTLE muzik slotu gecis penceresi (WorldEncounter.cs:181-191).
- `ProofArmSpellSchool` — 8 buyunun tamamini ogret, manayi 40'a bas, hedef dik
  (WorldEncounter.cs:887-918).
- Surucu: EmberProofScreenshotDriver.cs:451, 564, 590-598, 912.

## LLD - Veri Modeli

| Tip | Alanlar | Kaynak |
|---|---|---|
| `VitalStat` (struct, immutable) | `Current`, `Max`, `IsDepleted`; `Damage/Restore/Refill` klampli | VitalStat.cs:132-165 |
| `ActorVitals` (struct, immutable) | `Health`, `Fatigue`, `Mana`, `IsDead`; `WithHealth/WithFatigue/WithMana` | ActorVitals.cs:9-37 |
| `ActorRecord` (savas alanlari) | `Accuracy`, `Dodge`, `Armor`, `BaseDamage` (readonly); `IsAlive`; `ApplyVitals`, `MoveTo` | ActorRecord.cs:74-78, 87, 117 |
| `SpellDefinition` (sealed, immutable) | `TemplateId`, `DisplayName`, `School`, `TargetKind`, `ManaCost`, `RangeInTiles` (0=sinirsiz), `CooldownTicks`, `Effects[]`; ctor default-struct/negatif dogrulamasi | SpellDefinition.cs:14-121 |
| `SpellEffectSpec` (struct) | `Kind`, `Magnitude`, `DurationTicks`, `IsInstantaneous` | SpellEffectSpec.cs:12-32 |
| `SpellEffectCode` (string-kodlu struct) | Kanonik 7 verb: `direct_damage`, `restore_health`, `restore_fatigue`, `shield_buff`, `restore_mana`, `direct_mana`, `direct_fatigue`; `IsCanonical`; OPEN-SET serbest kodlar (`light`/`haste`/`recall`) `FromCode` ile gecer | SpellEffectCode.cs:21-56 |
| `MagicSchool` (enum) | None, Destruction, Restoration, Illusion, Conjuration, Mysticism, Alteration | MagicSchool.cs:9-18 |
| `SpellTargetKind` (enum) | None, CasterSelf, Touch, SingleTarget, AreaAroundCaster, AreaAtRange (area'lar cozulmuyor) | SpellTargetKind.cs:324-332 |
| `SpellCooldownState` | `spellTemplateId -> remainingTicks` sozlugu; 0 = sil | SpellCooldownState.cs:274-313 |
| `ShieldBuffState` | `spellTemplateId -> (RemainingTicks, Magnitude)`; 0 tick = sil | ShieldBuffState.cs:181-258 |
| `ShieldBuffStateRegistry` | actorId → ShieldBuffState (canli oyunda kullanilmiyor; sadece tekil `PlayerShieldBuffs` bagi kullaniliyor) | Assets/Scripts/Domain/Magic/ (SpellEffectResolutionService.cs:160-171 uzerinden) |
| `EffectDefinition` / `EffectId` / `EffectOperation` | "Yeni" veri-suruslu buyu satiri: `Id`, `SchoolTag`, `Cost`, `CooldownTicks`, `Operations[]` | EffectDefinition.cs:10-76 |
| `CombatActionDef` | `Id`, `StaminaCost`, hit/damage formula anahtarlari, `AnimationTag` | CombatActionDef.cs:13-31 |
| `CombatActionOutcome` (struct) | `Hit`, `Damage` | CombatActionResolver.cs:85-90 |
| `CombatStrikeResult` | hitChance/roll/bodyPart/raw-mitigated damage/summary (Sprint 1 slice cekirdegi) | CombatMathService.cs:25-55 |
| `RealtimeDamageResult` | AC, bodyPart, defenseIntent, roll, mitigated damage (collider boru hatti) | RealtimeDamageService.cs:42-71 |
| WorldState buyu alanlari | `PlayerKnownSpellIds` (L162), `PlayerSpellCooldowns` (L182), `PlayerShieldBuffs` (L183) | WorldState.cs:162, 182-183 |
| Save DTO'lari | `SpellCooldownSaveData`/`ShieldBuffSaveData` — ordinal-sirali stabil JSON; ToState 0/negatif girdileri atar | SpellCooldownSaveMapper.cs:15-52; ShieldBuffSaveMapper.cs:15-57 |

### Spell katalogu (8 buyu, statik ve deterministik — WorldSpellCatalog.cs)

| TemplateId | Okul | Hedef | Mana | Menzil | CD | Etki | Satir |
|---|---|---|---|---|---|---|---|
| `flame_bolt` | Destruction | SingleTarget | 12 | 8 | 6 | direct_damage 8 | :45-56 |
| `mending_touch` | Restoration | Touch | 10 | - | 4 | restore_health 6 | :58-69 |
| `ember_ward` | Alteration | CasterSelf | 15 | - | 30 | shield_buff 4, 30 tick (CD=sure → cift stack imkansiz, :38-43) | :71-82 |
| `frost_lance` | Destruction | SingleTarget | 17 | 7 | 9 | direct_damage 11 | :93-104 |
| `spark_arc` | Destruction | SingleTarget | 9 | 9 | 3 | direct_damage 6 | :106-117 |
| `lantern_glow` | Alteration | CasterSelf | 6 | - | 20 | OPEN-SET `light` 60/60 (presentation isik) | :119-130 |
| `wind_step` | Alteration | CasterSelf | 12 | - | 30 | restore_fatigue 10 + OPEN-SET `haste` 30/30 | :132-147 |
| `recall_gate` | Alteration | CasterSelf | 20 | - | 60 | OPEN-SET `recall` 1 (yerlesim merkezine isinla) | :149-160 |

Open-set kodlar pure resolver'da SKIP edilir (mana yine yanar), sahiplenen sistem
Presentation'dir (WorldSpellCatalog.cs:84-86; SpellEffectResolutionService.cs:62-69;
Combat.Spells.cs:354-374).

## LLD - Fonksiyon Haritasi

### Savas cekirdegi (Simulation.Combat)
- `CombatActionResolver.Resolve(action, attacker, defender, damageBandWidth, rng, now, siteId, events, accBonus=0, dmgBonus=0, defenderMitigation=null) : CombatActionOutcome` — CombatActionResolver.cs:25 — stamina kapisi → hit → damage → ward → health + `CombatResolved` eventi; CANLI oyunun tek gercek vurus yolu.
- `CombatHitRollService.Roll(attackerAccuracy, defenderDodge, rng) : bool` — CombatHitRollService.cs:11 — her cagri bir RollPercent tuketir (RNG dizisi sans degerinden bagimsiz); sans = 50+acc−dodge, klamp [15,95] (v0.3 playtest "nadiren vuruyorum" duzeltmesi).
- `CombatDamageService.Roll(baseDamage, bandWidth, armor, rng) : int` — CombatDamageService.cs:11 — base + rng[0..band] − armor, min 0; band>0 iken rng zorunlu.
- `CombatMathService.ResolveAttack(attacker, defender, rng, accBonus, dmgBonus) : CombatStrikeResult` — CombatMathService.cs:25 — Sprint 1 tek-vurus cekirdegi (45-bazli sans, body-part bonusu, min 1 hasar). Sadece `EncounterTurnService` kullanir.
- `EncounterTurnService.Advance(encounter, player, enemy, rng, playerEq, enemyEq) : CombatStrikeResult` — EncounterTurnService.cs:23 — sirali 1v1 slice dongusu; uretimde cagiran yok (yalniz testler — grep dogrulandi).
- `RealtimeDamageService.ResolveWeaponHit(hitEvent, attacker, defender, defenseIntent, rng) : RealtimeDamageResult` — RealtimeDamageService.cs:18 — collider-kokenli AC boru hatti (blok/kacinma niyetleri, body-part carpani); tek kullanicisi Presentation/Combat/CombatInputAdapter.cs:27 (CombatPlayground sahnesi).

### Buyu cekirdegi (Simulation.Magic) — CANLI (legacy) boru hatti
- `SpellCastingService.TryPrepareCast(caster, spellId, knownIds, cooldownState) : SpellCastResult` — SpellCastingService.cs:43 — mutasyonsuz on-kontrol (caster canli, bilinir, cooldown, mana).
- `SpellCastingService.CommitPreparedCast(caster, spell, cooldownState) : SpellCastResult` — SpellCastingService.cs:74 — mana dusumu + `StartCooldown`; tek mutasyon noktasi.
- `SpellTargetValidator.Validate(spell, caster, requestedTarget) : SpellTargetValidationResult` — SpellTargetValidator.cs:19 — CasterSelf/Touch(Manhattan=1)/SingleTarget(menzil) yonlendirir; Area* reddedilir.
- `SpellEffectResolutionService.ResolveInstantaneousEffects(castResult, target)` — SpellEffectResolutionService.cs:42 — 6 anlik vitals verbi uygular; timed/open-set SKIP (F28 sozlesme degisikligi, :62-69).
- `SpellEffectResolutionService.ApplyShieldBuffs(castResult, shieldBuffState)` — SpellEffectResolutionService.cs:120 — timed shield_buff satirlarini bag'a yazar; registry overload :160.
- `SpellExecutionService.TryExecute(caster, spellId, knownIds, target, cooldownState)` — SpellExecutionService.cs:64 — atomik orkestrasyon: prepare → target → preview → commit → resolve. `TryExecuteWithRoll` (:84) fizzle zari ekler ama uretim cagirani yok (grep dogrulandi — canli cast hic fizzle olmaz).
- `SpellSuccessChanceService.Calculate(caster, spell)` — SpellSuccessChanceService.cs:212 — 40 baz + Mnd/Ins bonuslari − mana/karmasiklik/hedef/menzil cezalari, klamp [5,95]. Sadece roll servisince kullanilir → uretimde olu.
- `SpellCostCalculator.EstimateTotalManaCost(spell)` — SpellCostCalculator.cs:332 — Σ(magnitude+sure/10) × hedef carpani (2/2..5/2, yukari yuvarla); open-set etkiler 0 fiyatlanir (:348-353). Katalog fiyatlari elle yazilir, bu tahminci taban niyetlidir.
- `SpellCooldownService.GetRemaining/StartCooldown/AdvanceTicks` — SpellCooldownService.cs:76-119.
- `ShieldBuffService.AdvanceTicks / AbsorbDamage(state, damage)` — ShieldBuffService.cs:19, 70 — ordinal-sirali deterministik emilim; magnitude biten buff suresi dolmadan silinir.
- `MagicTickDriver.AdvanceTicks(cooldowns, shieldBuffs, elapsedTicks)` — MagicTickDriver.cs:27 — tek tick giris noktasi.

### Buyu cekirdegi — "yeni" veri-suruslu boru hatti (uretimde BAGLI DEGIL)
- `SpellResolver.Resolve(definition, casterMana, now, siteContext, events, context)` — SpellResolver.cs:23 — cost kapisi, handler on-dogrulamasi (kismi mutasyon yok), `SpellResolved` telemetri; `SpellResolverContext` DirectDamage/DirectRestore/TerrainApply uygular (:105-137).
- `EffectOperationHandlers.Register/TryHandle/HasHandler` — EffectOperationHandlers.cs:130-153.
- `EffectRegistry.Register/TryGet/Definitions` — EffectRegistry.cs:88-116.
- Grep dogrulandi: `new SpellResolver(` yalnizca test dosyalarinda gecer — canli `TryCastSpell` bunu KULLANMAZ.

### Adapter orkestrasyonu (Presentation.Ember.Adapters)
- `TryMeleeStrike(targetActorName, rawDamage) : bool` — Combat.Melee.cs:126 — hedef cozumu, suc, seed (time×event-count×strike-serial), resolver, hit-flash feed.
- `TryCastSpell(spellSlotIndex) : bool` — Combat.Spells.cs:260 — akis B'nin tamami.
- `TryBeginWorldEncounter(actor, npc) : bool` — WorldEncounter.cs:51.
- `TickHostileAi(unscaledNow)` — WorldEncounter.cs:219 — 0.45s kadansli kovalama, lair leash, town safety.
- `TickWorldEncounter(unscaledNow)` — WorldEncounter.cs:290 — dusmanin 2.4s karsi vurusu.
- `SettleWorldEncounterIfOver(worldEnemy)` — WorldEncounter.cs:967 — spoils/XP/quest/finale.
- `RespawnAfterDeath() : string` — WorldEncounter.cs:336.
- `SelectSpellTarget(spell, player)` — Combat.Helpers.cs:51 — dost/dusman niyeti etki kodundan cikartilir; dusman buyusu hedefsizken null (caster'i yakma bugfix'i, :115-120).
- `AbsorbWithPlayerWard(incomingDamage) : int` — Combat.Helpers.cs:162 — ward emilim seam'i.
- `TakePlayerDamage(amount)` — Combat.cs:27 — dis kaynakli hasari gercek ActorRecord'a yazar.
- `ReadCombatScreenState()` — CombatScreen.cs:389 — encounter durumu + spell satirlari; her okuma settle + muzik mirror'i gunceller.
- `EmberPlayerSpellCaster.Cast/ProofCast` — EmberPlayerSpellCaster.cs:59, 98.

### NPC-vs-NPC (Simulation.Living)
- `PredationSystem.Tick(world, stamp) : int` — CascadeSystems.cs:23 (dosya ici :18'de sinif) — avci-av-devriye ucgeni; sivil 0'a dusunce 1 HP'ye geri cekilir ("mauled_survives", :68-74).

## LLD - Yazdigi/Okudugu Alanlar

FieldOwnershipRegistry dili (FieldOwnershipRegistry.cs:15-53):

**Yazilanlar**
- `Actor.Vitals` — deklarasyonlu yazarlar YALNIZCA `living.predation@Hourly:40`,
  `living.witness@Hourly:45`, `living.companion_guard@Hourly:42` (:32-37).
  DEKLARE EDILMEMIS yazarlar (frame-suruslu, tick registry'si disinda):
  `CombatActionResolver.Resolve` (fatigue :61, health :72), `SpellEffectResolutionService`
  (6 verb, :73-103), `SpellCastingService.CommitPreparedCast` (mana :91), adapter
  `TakePlayerDamage` (Combat.cs:36), `RespawnAfterDeath`/tavern/temple Refill'leri
  (WorldEncounter.cs:344, 588, 612). Ledger'in "her yazar deklare" iddiasi savas/buyu
  yolu icin tutmuyor — sinif (g) tarzi tutarlilik borcu.
- `Actor.Position` — `TickHostileAi` kovalama adimlari ve `RecallGate`/respawn `MoveTo`'lari
  da deklarasyonsuz yazarlardir (WorldEncounter.cs:258-277, 352; Combat.Spells.cs:370).
- `World.PlayerSpellCooldowns` — `SpellCooldownService.StartCooldown` (cast) + `MagicStep@PerTick:20` (decay).
- `World.PlayerShieldBuffs` — `ApplyShieldBuffs` (cast), `MagicStep` (decay), `AbsorbWithPlayerWard` (tuketim).
- `World.PlayerGold/PlayerXp/PlayerBountyGold/PlayerReputation` — `SettleWorldEncounterIfOver`,
  `RespawnAfterDeath`, suc dali (Combat.Melee.cs:150-157).

**Okunanlar**
- `WorldState.Actors` (rol/pozisyon/vitals), `WorldState.NpcSeeds` (rol: Outlaw/Guard),
  `WorldState.Time` + `Events.Count` (RNG seed'leri), `PlayerEquipment`/`PlayerInventory`
  (silah bonuslari, F16), `PlayerKnownSpellIds`, `WorldSpellCatalog` (statik).

## LLD - Urettigi/Tukettigi Olaylar

| Olay | Ureten | Format/Not |
|---|---|---|
| `WorldEventKind.CombatResolved` (=20, WorldEventKind.cs:34) | CombatActionResolver.cs:50-56, 74-79 | `combat_resolved action:{id} attacker:{id} defender:{id} hit:{bool} damage:{n}` veya `rejected:insufficient_stamina` |
| `WorldEventKind.SpellResolved` (=21, WorldEventKind.cs:35) | SpellResolver.cs:54-59, 87-92 (`spell_resolved id:.. ops:.. total:..`/`status:failed`); adapter Combat.Spells.cs:329-334 (`slice_spell_cast id:{templateId} mana:{n}`) | iki farkli format ayni kind altinda |
| `WorldEventKind.GuardResponded` (=31) | CascadeSystems.cs:28-29 | `guard_strikes_hunter target:{id}` |
| `NeedChanged` (mauled) | CascadeSystems.cs:72-73 | `mauled_survives by:{id}` |
| One-shot sinyaller (event log'da degil) | `WorldEncounterSignal` / `WorldEncounterStingFeed` (WorldEncounter.cs:12-32); `WorldCombatFeedbackFeed.RaiseHit/RaiseEnemyStrike/RaiseFelled` (Combat.Melee.cs:233, WorldEncounter.cs:327, 978) | UI/audio/vfx fan-out |
| Mirror'lar | `RuntimeBattleMirror.Active/BossActive` (CombatScreen.cs:397-403), `RuntimeSpellFxMirror.LastCastTemplate/LightUntil/HasteUntil/RecallRequested` (Combat.Spells.cs:357-371) | Presentation statik kanallari |

## Testler

Buyu (Assets/Tests/EditMode/Magic/): SpellDefinitionTests, WorldSpellCatalogTests,
SpellSchoolF28Tests, SpellCastingServiceTests, SpellCooldownServiceTests,
SpellTargetValidatorTests, SpellSuccessChanceServiceTests, SpellCastRollServiceTests,
SpellCostCalculatorTests, SpellExecutionServiceTests, SpellEffectResolutionServiceTests
(+ .Restore/.DirectMana/.DirectFatigue/.Validation partial'lari), ShieldBuff* (state,
registry, service, absorption, sweep, batch — 10 dosya), MagicTickDriverTests,
MagicTickDriverRegistryTests, EffectHandlerTests, EffectPrimitivesTests.

Savas (Assets/Tests/EditMode/Combat/): CombatMathServiceTests, CombatPrimitivesTests,
EncounterTurnServiceTests, BodyPartSelectorTests, RealtimeDamageServiceTests,
RealtimeCombatActionSchedulerTests.

Kesitler: Audit/SelectSpellTargetTests.cs (adapter hedef secimi),
Acceptance/FazSixToTwelveBackendAcceptanceTests.cs:140 (SpellResolver'in tek "canli-benzeri"
kullanimi), Presentation/PlayableLoopCraftQuestTests.cs:410 (`ProofRunEncounterLeg`),
Save/SpellCooldownSaveMapperTests + ShieldBuffSaveMapperTests, Living/CascadeSystemsTests
+ GuardPursuitTests, Composition/FieldOwnershipRegistryTests (ledger lint'i),
World/PlayerLevelUpServiceTests + PlayerRestServiceTests.

## Bilinen Borclar + Kacak Kapilari

1. **Cift buyu boru hatti, yanlis yon levhasi**: SpellExecutionService.cs:20-29'daki doc
   "canli TryCastSpell yeni SpellResolver+EffectOperationHandlers hattina gider" der; gercek
   kod legacy hatti kullanir (Combat.Spells.cs:316-322) ve `SpellResolver`'in uretim cagirani
   YOKTUR (grep: yalniz testler). "Yeni buyuler EffectDefinition olarak gelsin" kurali fiilen
   olu — F28'in 5 yeni buyusu legacy `SpellEffectCode` open-set'iyle geldi. Sinif (a)
   olu-kod / yanilticidokuman ailesi.
2. **Fizzle zari canli oyunda yok**: `TryExecuteWithRoll` + SpellSuccessChanceService +
   SpellCastRollService komple test-only; canli cast %100 tutar. Tasarlanan Tier-3 roll
   sistemi devreye alinmamis (SpellExecutionService.cs:74-93).
3. **Uc savas cekirdegi**: CombatActionResolver (canli), CombatMathService+EncounterTurnService
   (Sprint 1 slice, uretim cagirani yok), RealtimeDamageService+Scheduler (yalniz
   CombatInputAdapter/CombatPlayground). Formuller birbirinden farkli (baz 50 vs 45 vs 50+AC)
   — davranis tutarliligi borcu, sinif (b).
4. **FieldOwnershipRegistry savas/buyu yazarlarini bilmiyor**: `Actor.Vitals` icin frame-suruslu
   adapter/resolver yazimlarinin hicbiri deklare degil (FieldOwnershipRegistry.cs:32-37) —
   ledger'in "undeclared second writer becomes a CI event" vaadi bu sistemde bos.
5. **StaminaCost=0 kacagi**: canli melee ve dusman vurusu `staminaCost: 0` ile kurulur
   (Combat.Melee.cs:169, WorldEncounter.cs:307) — resolver'in stamina kapisi ve fatigue
   dusumu (CombatActionResolver.cs:49-61) canli oyunda hic tetiklenmez; fatigue havuzu
   savasta dekoratif.
6. **Cift mana kapisi**: adapter TryCastSpell once kendisi mana kontrolu yapar
   (Combat.Spells.cs:285-289), sonra TryPrepareCast ayni kontrolu tekrarlar — zararsiz ama
   refusal mesaji iki farkli yerden gelebilir.
7. **`SpellCastingService(_ => spell)` katalog bypass'i**: cast servisi tek-buyuluk lambda
   lookup'la kurulur (Combat.Spells.cs:317) — WorldSpellCatalog.Find yerine; bilinirlik
   listesi ile katalog ayrisirsa sessiz tutarsizlik riski.
8. **Cooldown UI'da gorunmez**: `ReadCombatScreenState` spell satirini yalniz mana ile
   enable/disable eder (CombatScreen.cs:420); cooldown'daki buyu UI'da "hazir" gorunur,
   cast aninda reddedilir. SpellBar.cs'de cooldown okuma yok (grep dogrulandi).
9. **Area hedefleri kapali**: SpellTargetKind.AreaAroundCaster/AreaAtRange validator'da
   acik reddedilir (SpellTargetValidator.cs:36-41) — bilincli escape hatch, "area resolution
   lands later".
10. **Ceset kalicilik celismesi (dogrulanmadi)**: NightCurfewView.cs:53-54 "corpses are
    despawned by the death sweep" der; Haunters.cs:39/127 ve WorldProjection.cs:163 cesetlerin
    kalici oldugunu ve respawn'i blokladigini soyler. "Death sweep" adinda bir sistem
    bulunamadi — yorum bayat olabilir, kod davranisi kalicilik yonunde.
11. **Chest-opened state save'e girmiyor**: LootDungeonChest kendi honest-limit notuyla
    (WorldEncounter.cs:413) — F22 alani.
12. **Tek dusman siniri**: ayni anda yalniz BIR encounter bind edilir (WorldEncounter.cs:217
    "honest limit"); coklu-dusman melee F18 chamber fight'a itilmis.
13. **RNG seed yamalari uzerine yamalar**: melee seed'i dort ayri Codex/loop-test duzeltmesinin
    tortusudur (Combat.Melee.cs:173-201) — time+eventCount+session-local serial. Save/load
    replay ayni snapshot'tan tutarli, ancak `_meleeStrikeSerial` bilinçli olarak persist
    edilmez (:122-124); farkli oturum gecmisi farkli zar dizisi uretir (kabul edilmis
    tasarim, potansiyel surpriz).
