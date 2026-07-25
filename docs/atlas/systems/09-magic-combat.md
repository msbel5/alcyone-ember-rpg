# 09-magic-combat

## HLD - Ne ve Neden (5-10 cümle)

Bu sistem oyuncunun canlı Ember sahnesindeki iki hareketi — büyü ve yakın-mesafe (melee) — Domain katmanına bağlar. Klavye 1-8 tuşları `EmberPlayerSpellCaster`'ı, F tuşu `EmberPlayerMeleeSwing`'i harekete geçirir; ikisi de aynı `EmberWorldHost.IsModalOpen()` kapısıyla diyalog/envanter modallerine yenilir (W31 canlı yara). Büyü hattı `DomainSimulationAdapter.TryCastSpell` üzerinden `SpellExecutionService.TryExecuteWithRoll`'a gider: `SpellCastingService` prepare/commit + `SpellTargetValidator` + `SpellEffectResolutionService` (instantaneous + `ApplyShieldBuffs`) + `SpellCastRollService` (deterministik XorShift zar). Melee hattı `TryMeleeStrike` → `CombatActionResolver` (accuracy_vs_dodge + base_minus_armor, `CombatResolved` olayı) — codex-6 sonrası artık auto-hit değil. Katalog `WorldSpellCatalog`'da 8 sabit `SpellDefinition`: flame_bolt / mending_touch / ember_ward / frost_lance / spark_arc / lantern_glow / wind_step / recall_gate. B12'nin fizzle/zar payı W32'de kapandı (adapter artık `TryExecuteWithRoll` çağırıyor), ama B12'nin ikinci yarısı — `SpellResolver` + `EffectDefinition` + `EffectOperationHandlers` data-driven boru hattı — hâlâ **canlı hatta bağlı DEĞİL** (yalnız `Assets/Tests/EditMode/Magic/EffectHandlerTests.cs` kullanıyor). Presentation katmanı ayrıca open-set VFX kodlarını (light / haste / recall) `RuntimeSpellFxMirror` üzerinden template-id anahtarıyla tetikler, çünkü pure resolver bu kodları no-op geçer.

## HLD - Akış (numaralı adımlar)

1. **Büyü girdisi** — `EmberPlayerSpellCaster.Update` her karede `EmberInput.NumberKeyDown(i+1)` ile 1-8 tuşlarını yoklar (F28: sekiz spell).
2. **Modal kapısı** — `Cast(slotIndex)` başında `EmberWorldHost.IsModalOpen()`; diyalog/envanter açıksa sessizce döner (codex 6. pas #8, W31).
3. **Adapter yönlendirme** — `EmberDomainAdapterLocator.Current.TryCastSpell(slotIndex)`; adapter null ise düşer.
4. **Slot → Definition** — `_world.PlayerKnownSpellIds` doluysa oradan, değilse `WorldSpellCatalog.All`'den slot okunur; `WorldSpellCatalog.Find(templateId)` `SpellDefinition` verir.
5. **Ön kapılar** — pure caster (Role.Player) çekilir; mana yetmezse `LogCombat("insufficient mana")` + false.
6. **Live position senk** — F28 root fix: `PlayerCombatPosition(player)` rig konumunu okur, `player.MoveTo` ile record güncellenir (parked plaza cell yerine gerçek beden).
7. **Target seçimi** — `SelectSpellTarget(spell, player)`: `TargetKind.CasterSelf/AreaAroundCaster` → caster; instantaneous effect kodlarına bakılıp "wantsFriendly" karar verilir (RestoreHealth/ShieldBuff/RestoreMana/RestoreFatigue → dost tarama, aksi → düşman); Manhattan mesafe + range/touch kısıtı; hostile null dönerse cast reddedilir (PLAYTEST BUG: eskiden caster'ı yakıyordu).
8. **Zar + commit + effect** — `SpellExecutionService.TryExecuteWithRoll(player, templateId, knownIds, target, castRng, _world.PlayerSpellCooldowns)`:
   - `SpellCastingService.TryPrepareCast` (mana + bilinen + cooldown gate)
   - `SpellTargetValidator.Validate`
   - `SpellEffectResolutionService.CanResolveInstantaneousEffects` (preview)
   - `SpellCastRollService.Roll(caster, spell, rng)` — B12 zar kapısı; XorShift seed = `(time*2654435761) ^ (casterId*40503) ^ (_meleeStrikeSerial*0x9E3779B9) ^ 0x5EE1_CA57`
   - `SpellCastingService.CommitPreparedCast` (mana harca + cooldown başlat)
   - `SpellEffectResolutionService.ResolveInstantaneousEffects` (health/mana/fatigue mutasyonu)
9. **Post-cast yan etkiler** — Adapter `SpellResolved` WorldEvent yazar; hostile isabet için `WorldCombatFeedbackFeed.RaiseHit(targetId, hitMaterial)`; başarılı cast'in timed `ShieldBuff` satırları `ApplyShieldBuffs(_world.PlayerShieldBuffs)` ile ward torbasına yazılır (F28 WARD).
10. **VFX seam** — `RuntimeSpellFxMirror.LastCastTemplate = templateId`; template-id'ye göre `LightUntilRealtime` / `HasteUntilRealtime` / `RecallRequested` bayrakları set edilir (recall ayrıca `player.MoveTo(CenterOfSite(SettlementSiteId(...)))`).
11. **Bolt uçuşu** — cast fired ise `FlyBolt` coroutine: `BoltTint(template)` renkte kamera-facing quad + Light 8-tile uzağa 0.28s'de gider; `ProofCast(slotIndex)` proof-harness için aynı adapter yolunu kullanır.
12. **Melee girdisi** — `EmberPlayerMeleeSwing.Update`: `IsModalOpen()` kapısı, `EmberInput.MeleeSwing` düğmesi, `_isSwinging` reentry engeli → `SwingRoutine`.
13. **Melee ses + kamera** — `RuntimeAudioDirector.PlaySwing(_eye.position)` (buyer-feel: her deneme kesim sesi), 0.1s kamera roll (10°).
14. **Melee raycast** — `Physics.Raycast(ray, out hit, combatOptions.MeleeRange)`; `IDamageSink` çekilir; codex-4 A-P1: **görsel önce, adapter sonra** yerine artık **adapter önce**.
15. **Target key resolution** — `ActorView.DomainActorKey` (codex PR#196 P1); yoksa `hit.collider.gameObject.name`; `adapter.TryMeleeStrike(targetName, combatOptions.MeleeRawDamage)`.
16. **Melee domain karar** — Adapter: `NearestStrikeTarget(maxRange:6)` (HUD boş target'ta) veya isim-eşleşme; F23 CRIME: Enemy dışına vuruş 40g bounty + rep-2 + watch summon; codex 6. pas #4 yolu → `CombatActionResolver.Resolve` (`accuracy_vs_dodge` + `base_minus_armor` + `CombatResolved` event); `_meleeStrikeSerial++`, seed `_tick + serial`.
17. **Hit feedback** — accepted ise `PunchFov` (-5° 0.14s), `IDamageSink.Apply(damage)`, `TakePlayerDamage(MeleeCounterDamage)`. Ward mitigation `AbsorbWithPlayerWard` seam'inden geçer.

## LLD - Veri Modeli (file:line)

- `Assets/Scripts/Presentation/Ember/Combat/EmberPlayerSpellCaster.cs:8-13` — `EmberPlayerSpellCaster : MonoBehaviour`; alanlar: `Transform _eye`, `Transform _bolt`, `Material _boltMaterial`, `Light _boltLight`.
- `Assets/Scripts/Presentation/Ember/Combat/EmberPlayerMeleeSwing.cs:10-13` — `EmberPlayerMeleeSwing : MonoBehaviour`; alanlar: `Transform _eye`, `bool _isSwinging`.
- `Assets/Scripts/Domain/Magic/SpellDefinition.cs:14` — `sealed class SpellDefinition` (TemplateId, DisplayName, School, TargetKind, ManaCost, RangeInTiles, CooldownTicks, Effects).
- `Assets/Scripts/Simulation/Magic/WorldSpellCatalog.cs:13-186` — static catalog; sabit template id sabitleri (`FlameBoltTemplateId` … `RecallGateTemplateId`), `ReadOnlyCollection<SpellDefinition> All`, `Find(id)`; cooldown sabitleri (FlameBolt=6, MendingTouch=4, EmberWard=30…); F28 8 spell.
- `Assets/Scripts/Simulation/Magic/SpellCastingService.cs:17-123` — prepare/commit; `Func<string,SpellDefinition> _catalogLookup`, `SpellCooldownService _cooldownService`; `SpellCastResult` (Ok/Fail).
- `Assets/Scripts/Simulation/Magic/SpellExecutionService.cs:31-202` — legacy orchestrator; `TryExecute`, `TryExecuteWithRoll` (B12 canlı yol); phase sırası: Prepare → Target → EffectPreview → Roll → Commit → Resolve.
- `Assets/Scripts/Simulation/Magic/SpellEffectResolutionService.cs:22-195` — instantaneous vitals resolver + `ApplyShieldBuffs(SpellCastResult, ShieldBuffState)` + actor-keyed overload (`ShieldBuffStateRegistry, actorId`); `IsSupported` yalnız `DirectDamage/RestoreHealth/RestoreFatigue/RestoreMana/DirectMana/DirectFatigue` kabul eder, gerisi (timed ShieldBuff + open-set kodlar) atlanır — RED değil.
- `Assets/Scripts/Simulation/Magic/SpellResolver.cs:14-138` — **YENİ** data-driven resolver (EffectDefinition + EffectOperationHandlers + SpellResolverContext); yalnız `SpellResolved` WorldEvent yazar; canlı hatta bağlı DEĞİL.
- `Assets/Scripts/Simulation/Magic/SpellCooldownService.cs:12`, `SpellCastRollService.cs`, `SpellSuccessChanceService.cs`, `SpellTargetValidator.cs`, `SpellCostCalculator.cs` — yardımcı deterministik servisler.
- `Assets/Scripts/Simulation/Magic/ShieldBuffService.cs:17-141` — ward torbası; `AdvanceTicks(state, ticks)`, `AbsorbDamage(state, incoming)`, actor-keyed `AdvanceTicksForAllActors(registry, ticks)`.
- `Assets/Scripts/Simulation/Magic/MagicTickDriver.cs:16-59` — single-bag ve registry overload; `SpellCooldownService.AdvanceTicks` + `ShieldBuffService.AdvanceTicks(ForAllActors)`.
- `Assets/Scripts/Domain/Magic/ShieldBuffState.cs:15` — `SetActiveBuff(templateId, durationTicks, magnitude)` + `ShieldBuffStateRegistry` (actorId → bag).
- `Assets/Scripts/Domain/Magic/EffectDefinition.cs:10-60`, `EffectOperation.cs:8-27`, `EffectOperationKind.cs:8-27` — data-driven yeni model (DirectDamage/DirectRestore/StatusApply/AreaApply/TerrainApply).
- `Assets/Scripts/Simulation/Magic/EffectRegistry.cs:8-35` — `EffectId → EffectDefinition` registry (test-only).
- `Assets/Scripts/Presentation/Ember/Adapters/DomainSimulationAdapter.Combat.Spells.cs:21-149` — `TryCastSpell(int slotIndex)`; catalog fallback (satır 25), mana precheck (46), live position sync (57-59), `SelectSpellTarget` (69), execution `TryExecuteWithRoll` (77-95), `SpellResolved` event (102-107), ward application (119-125), open-set FX (130-147).
- `Assets/Scripts/Presentation/Ember/Adapters/DomainSimulationAdapter.Combat.Helpers.cs:51-121` — `SelectSpellTarget(spell, player)`; codex 8. pas A-P0 friendly/hostile ayrımı effect-code sniffing (73-79); hostile null → refuse (119).
- `Assets/Scripts/Presentation/Ember/Adapters/DomainSimulationAdapter.Combat.Helpers.cs:162-170` — `AbsorbWithPlayerWard(incomingDamage)`; F28 defender-mitigation seam.
- `Assets/Scripts/Presentation/Ember/Adapters/DomainSimulationAdapter.Combat.Melee.cs:57-79` — `NearestStrikeTarget(maxRange)`; F14: PlayerCombatPosition, F23: sadece Enemy auto-target.
- `Assets/Scripts/Presentation/Ember/Adapters/DomainSimulationAdapter.Combat.Melee.cs:83` — `private uint _meleeStrikeSerial` (session-local, save persiste edilmez; spell RNG'si aynı serial'i kullanıyor).
- `Assets/Scripts/Presentation/Ember/Adapters/DomainSimulationAdapter.Combat.Melee.cs:85-` — `TryMeleeStrike(targetActorName, rawDamage)`; crime (109-116), `CombatActionResolver` yolu (117-140).
- `Assets/Scripts/Presentation/Ember/Adapters/DomainSimulationAdapter.WorldRows.cs:107-116` — `SpellSlots` public property (fallback: `WorldSpellCatalog.All`).
- `Assets/Scripts/Presentation/Ember/Adapters/IDomainSimulationAdapter.cs:54,130,148,154` — `SpellSlots`, `TakePlayerDamage`, `TryCastSpell`, `TryMeleeStrike` kontratı.
- `Assets/Scripts/Simulation/Combat/CombatActionResolver.cs:14-` — hit + damage + `CombatResolved` event.

## LLD - Fonksiyon Haritası (imza + file:line + 1 cümle)

- `EmberPlayerSpellCaster.Awake()` — `Combat/EmberPlayerSpellCaster.cs:15` — Eye kamerayı bulur, kamera-facing quad + light + material'i kurar (PLAYTEST FIX: LineRenderer değil, billboard).
- `EmberPlayerSpellCaster.Update()` — `Combat/EmberPlayerSpellCaster.cs:46` — 1-8 tuşlarını yoklar, `Cast(i)` çağırır (F28 sekiz slot).
- `EmberPlayerSpellCaster.Cast(int slotIndex)` — `Combat/EmberPlayerSpellCaster.cs:59` — Modal kapısı + adapter yönlendirmesi + bolt tetikleyicisi.
- `EmberPlayerSpellCaster.ProofCast(int)` — `Combat/EmberPlayerSpellCaster.cs:98` — Proof-harness için tuş basımına ihtiyaç duymadan aynı adapter yolunu çalıştırır.
- `EmberPlayerSpellCaster.BoltTint(string templateId)` — `Combat/EmberPlayerSpellCaster.cs:112` — Damage type'a göre renk (frost mavi, spark altın, aksi flame turuncu).
- `EmberPlayerSpellCaster.FlyBolt()` — `Combat/EmberPlayerSpellCaster.cs:122` — `_eye.forward` boyunca 8-tile 0.28s bolt uçuşu (flame_bolt range'iyle eşleşir).
- `EmberPlayerMeleeSwing.Awake()` — `Combat/EmberPlayerMeleeSwing.cs:15` — Eye kamera transform'unu tutar.
- `EmberPlayerMeleeSwing.Update()` — `Combat/EmberPlayerMeleeSwing.cs:20` — W31 modal kapısı + `EmberInput.MeleeSwing` yakalama.
- `EmberPlayerMeleeSwing.SwingRoutine()` — `Combat/EmberPlayerMeleeSwing.cs:31` — Kesim sesi, kamera roll, raycast, `TryMeleeStrike`, sink.Apply + counter (codex-4 A-P1: adapter önce, görsel sonra).
- `EmberPlayerMeleeSwing.PunchFov()` — `Combat/EmberPlayerMeleeSwing.cs:96` — Buyer-feel: -5° FOV 0.14s pulse.
- `DomainSimulationAdapter.TryCastSpell(int)` — `Adapters/DomainSimulationAdapter.Combat.Spells.cs:21` — Slot → SpellDefinition, mana/live-pos gate, target seçimi, `TryExecuteWithRoll`, ward + open-set FX.
- `DomainSimulationAdapter.SelectSpellTarget(SpellDefinition, ActorRecord)` — `Adapters/DomainSimulationAdapter.Combat.Helpers.cs:51` — Friendly/hostile ayrımı effect-koda dayalı, Manhattan mesafe + range gate.
- `DomainSimulationAdapter.AbsorbWithPlayerWard(int)` — `Adapters/DomainSimulationAdapter.Combat.Helpers.cs:162` — F28 defender-mitigation seam: ward `AbsorbDamage`, log, remaining döner.
- `DomainSimulationAdapter.TryMeleeStrike(string, int)` — `Adapters/DomainSimulationAdapter.Combat.Melee.cs:85` — Target çözüm, crime kaydı, `CombatActionResolver` üzerinden hit+damage.
- `DomainSimulationAdapter.NearestStrikeTarget(int)` — `Adapters/DomainSimulationAdapter.Combat.Melee.cs:57` — HUD boş target auto-target (Chebyshev, F23 sadece Enemy).
- `DomainSimulationAdapter.SpellSlots (get)` — `Adapters/DomainSimulationAdapter.WorldRows.cs:107` — HUD/hotbar için oyuncunun bilinen büyü id listesi (fallback: full catalog).
- `SpellExecutionService.TryExecuteWithRoll(...)` — `Simulation/Magic/SpellExecutionService.cs:84` — Prepare→Target→EffectPreview→Roll→Commit→Resolve; deterministik RNG argümanı alır.
- `SpellCastingService.TryPrepareCast(ActorRecord, string, IReadOnlyCollection<string>, SpellCooldownState)` — `Simulation/Magic/SpellCastingService.cs:43` — Caster/spell/known/cooldown/mana ön kapısı, mutasyonsuz preflight.
- `SpellCastingService.CommitPreparedCast(ActorRecord, SpellDefinition, SpellCooldownState)` — `Simulation/Magic/SpellCastingService.cs:74` — Mana harcar + cooldown başlatır (atomik commit).
- `SpellEffectResolutionService.ResolveInstantaneousEffects(SpellCastResult, ActorRecord)` — `Simulation/Magic/SpellEffectResolutionService.cs:42` — DirectDamage/RestoreHealth/RestoreFatigue/RestoreMana/DirectMana/DirectFatigue mutasyonu.
- `SpellEffectResolutionService.ApplyShieldBuffs(SpellCastResult, ShieldBuffState)` — `Simulation/Magic/SpellEffectResolutionService.cs:120` — Timed ShieldBuff satırlarını ward torbasına yazar.
- `SpellResolver.Resolve(EffectDefinition, int, GameTime, SiteId, WorldEventLog, SpellResolverContext)` — `Simulation/Magic/SpellResolver.cs:23` — Yeni data-driven yol; `SpellResolved` event, `EffectOperationHandlers` üzerinden atomik uygulama (test-only).
- `MagicTickDriver.AdvanceTicks(SpellCooldownState, ShieldBuffState|Registry, int)` — `Simulation/Magic/MagicTickDriver.cs:27,47` — Tick döngüsünde cooldown + ward bagi decay.
- `ShieldBuffService.AbsorbDamage(ShieldBuffState, int)` — `Simulation/Magic/ShieldBuffService.cs:70` — Ward magnitude'sini önce yer, kalan hasar döner.
- `WorldSpellCatalog.Find(string)` — `Simulation/Magic/WorldSpellCatalog.cs:29` — Template id → SpellDefinition (null tolerant).
- `CombatActionResolver.Resolve(...)` — `Simulation/Combat/CombatActionResolver.cs:14` — Melee tek çözüm noktası: hit roll + damage roll + `CombatResolved`.

## LLD - Yazdığı/Okuduğu Alanlar (FieldOwnershipRegistry dilinde)

Not: `Assets/Scripts/Simulation/Composition/FieldOwnershipRegistry.cs` (115 satır) bu sistemin combat/magic alanları için henüz **anahtar kaydına sahip değil** — büyü/melee mutasyonları `WorldState` üzerinden geçmesine rağmen registry'de owner satırı yok (Bilinen Borç 6).

- **Yazdıkları** (canlı hat):
  - `WorldState.PlayerSpellCooldowns` (SpellCooldownState) — `SpellCastingService.CommitPreparedCast` cooldown başlatır; `MagicTickDriver` decay eder.
  - `WorldState.PlayerShieldBuffs` (ShieldBuffState) — `SpellEffectResolutionService.ApplyShieldBuffs` yazar; `ShieldBuffService.AbsorbDamage` tüketir; `MagicTickDriver` decay eder.
  - `ActorRecord.Vitals.Mana` — commit sırasında `Damage(spell.ManaCost)`.
  - `ActorRecord.Vitals.Health/Mana/Fatigue` — instantaneous effect resolver + `TryMeleeStrike`.
  - `ActorRecord.Position` — F28 live-position sync (record'u rig konumuna zorlar); Recall Gate → `CenterOfSite(SettlementSiteId)`.
  - `WorldState.PlayerBountyGold`, `WorldState.PlayerReputation` — F23 crime.
  - `WorldState.Events` (WorldEventLog) — `SpellResolved`, `CombatResolved` satırları.
  - `RuntimeSpellFxMirror.LastCastTemplate`, `.LightUntilRealtime`, `.HasteUntilRealtime`, `.RecallRequested` (Presentation-only VFX state; realtime saatiyle sayılır).
- **Okudukları**:
  - `WorldState.PlayerKnownSpellIds` (fallback: `WorldSpellCatalog.All`).
  - `WorldState.Actors.Records` (target arama), `ActorRecord.Role`, `.IsAlive`, `.Position`, `.Vitals`.
  - `WorldState.Sites` (`ResolveCombatSiteId`, `SettlementSiteId`).
  - `EmberWorldHost.IsModalOpen()` (Presentation modal snapshot).
  - `EmberRuntimeOptionsProvider.Current.Combat` (`MeleeRange`, `MeleeRawDamage`, `MeleeCounterDamage`).
  - `ActorView.DomainActorKey` (Presentation view, PR#196 P1).

## LLD - Ürettiği/Tükettiği Olaylar

- **Ürettiği**:
  - `WorldEventKind.SpellResolved` — `Simulation/Magic/SpellResolver.cs:56,89` (yeni yol, test); `Adapters/DomainSimulationAdapter.Combat.Spells.cs:102-107` (canlı hat, "slice_spell_cast id:{template} mana:{spent}").
  - `WorldEventKind.CombatResolved` — `Simulation/Combat/CombatActionResolver.cs:53,76` (melee kanonik olay).
  - `WorldCombatFeedbackFeed.RaiseHit(actorIdValue, hitMaterial)` — Presentation feedback bus (billboard flash, F29 hit-material).
  - Log satırı: `LogCombat(...)` combat logu (HUD alt band'ına akar).
- **Tükettiği**:
  - `EmberInput.NumberKeyDown(1..8)` — büyü hotkey (`Presentation/Ember/Inputs/EmberInput.cs:55`).
  - `EmberInput.MeleeSwing` — F tuşu (`EmberInput.cs:45`).
  - `EmberWorldHost.IsModalOpen()` — W31 modal kapısı.
  - `IDamageSink.Apply(int)` — sahne target'ının hasar arayüzü (Presentation/Ember/Interaction).
  - `ActorView.DomainActorKey` — melee target adı çözümü.
  - `PlayerCombatPosition(player)` — F14 rig-live konum bağlantısı.

## Testler (bu sistemi pinleyen test dosyaları - W32-W36 hikâye-testleri dahil)

- `Assets/Tests/EditMode/Magic/WorldSpellCatalogTests.cs` — 8 template'in stabil id/ManaCost/cooldown/effects kontrolü.
- `Assets/Tests/EditMode/Magic/SpellDefinitionTests.cs` — SpellDefinition/SpellEffectSpec değişmezleri.
- `Assets/Tests/EditMode/Magic/SpellCastingServiceTests.cs` — prepare/commit, mana precheck, cooldown, unknown-spell reddi.
- `Assets/Tests/EditMode/Magic/SpellCastRollServiceTests.cs` — B12 fizzle zarı: threshold + Roll(rng) davranışı.
- `Assets/Tests/EditMode/Magic/SpellSuccessChanceServiceTests.cs` — başarı olasılığı formülü.
- `Assets/Tests/EditMode/Magic/SpellCostCalculatorTests.cs` — mana bütçesi hesaplayıcı.
- `Assets/Tests/EditMode/Magic/SpellCooldownServiceTests.cs` — AdvanceTicks + GetRemainingTicks.
- `Assets/Tests/EditMode/Magic/SpellTargetValidatorTests.cs` — target-kind + range doğrulama.
- `Assets/Tests/EditMode/Magic/SpellEffectResolutionServiceTests.cs` (+ `.DirectMana`, `.DirectFatigue`, `.Restore`, `.Validation`) — 6 instantaneous kod + ShieldBuff atlanan + validation davranışı.
- `Assets/Tests/EditMode/Magic/SpellExecutionServiceTests.cs` — Prepare→Target→…→Resolve orkestrasyonu, cooldown state ile.
- `Assets/Tests/EditMode/Magic/SpellSchoolF28Tests.cs` — F28 sekiz-spell school genişlemesi (frost/spark/lantern/wind/recall).
- `Assets/Tests/EditMode/Magic/ShieldBuffServiceTests.cs` + `Absorption` + `RegistryAbsorption` + `RegistryBatchAbsorption` + `RegistrySweep` — ward bag decay + absorb + actor-keyed genişleme.
- `Assets/Tests/EditMode/Magic/ShieldBuffApplicationServiceTests.cs`, `ShieldBuffActorKeyedApplicationServiceTests.cs` — ApplyShieldBuffs iki overload.
- `Assets/Tests/EditMode/Magic/ShieldBuffStateTests.cs`, `ShieldBuffStateRegistryTests.cs` — Domain state kabı.
- `Assets/Tests/EditMode/Magic/MagicTickDriverTests.cs`, `MagicTickDriverRegistryTests.cs` — cooldown + ward tick decay pinning.
- `Assets/Tests/EditMode/Magic/EffectHandlerTests.cs` — **YENİ boru hattı**: SpellResolver + EffectDefinition + EffectOperationHandlers (unhandled-op reddi, SpellResolved emisyon, TargetActor mutasyonu, TerrainApply). Canlı hatta bağlı olmadığı için sistemin bu bölümünü sadece testler pinliyor.
- `Assets/Tests/EditMode/Magic/EffectPrimitivesTests.cs` — EffectDefinition/EffectOperation immutability + validation.
- `Assets/Tests/EditMode/Audit/SelectSpellTargetTests.cs` — Adapter helper: friendly/hostile ayrımı, hostile null → refuse (PLAYTEST BUG pinlemesi), Touch mesafe.
- `Assets/Tests/EditMode/Audit/AuditFourthPassTailCoverageTests.cs` — codex 4. pas: adapter önce/görsel sonra melee sırası.
- `Assets/Tests/EditMode/Audit/AuditThirdPassCoverageTests.cs`, `AuditFifthPassCoverageTests.cs`, `AuditSeventhPassCoverageTests.cs` — sırasıyla TryCastSpell adapter binding, ApplyOperationToContext mutasyon-vs-sayı, SpellExecutionService route pinlemeleri.
- `Assets/Tests/EditMode/Acceptance/FazSixToTwelveBackendAcceptanceTests.cs` — F6-F12 kabul zinciri; SpellCastingService/SpellResolver çağrılarını dahil eder.
- Combat çekirdeği: `Assets/Tests/EditMode/Combat/CombatActionResolverTests.cs` + kardeşleri (CombatDamage/CombatHitRoll/CombatMath/EncounterTurn/RealtimeDamage) — hit + damage + CombatResolved event pin.

## W32-W36 Değişiklikleri (bu sistemin son 5 haftadaki büyük hareketleri)

- **W31 (`1e9b474b`)** — Sekiz canlı yara + tam atlas: `EmberPlayerMeleeSwing.Update` içine `EmberWorldHost.IsModalOpen()` kapısı düştü ("`f tusuna basarsam konustugum insana saldiriyor`"); `EmberPlayerSpellCaster.Cast` başındaki modal kapısı codex 6. pas #8'in devamı; PLAYTEST BUG "büyü kullanırsam kendi canım gidiyor" — hostile spell null target'ta artık refuse (`SelectSpellTarget` L119); PLAYTEST FIX bolt'u LineRenderer'dan kamera-facing billboard + Light'a çevirdi (`EmberPlayerSpellCaster.Awake` L19-43).
- **W32-pre (`79b707d8`)** — Input/perf pass, B15 storm; melee/spell input sürüklenmesi düzeltildi (fast-travel freeze).
- **W32 (`5049d445`, spot-fix `c477c217`)** — EAT-slice ana teması ama içeride **B12 wave 1**: `DomainSimulationAdapter.Combat.Spells.cs:82-94` `TryExecute` → `TryExecuteWithRoll`; deterministik XorShift seed `time*2654435761 ^ casterId*40503 ^ meleeSerial*0x9E3779B9 ^ 0x5EE1_CA57`; live cast artık spell success threshold'a tabi (fizzle mümkün).
- **W33 (`61e340f3`)** — FARM slice; magic/combat surface değişmedi; `_meleeStrikeSerial` seed pattern'ini de kullanan RNG semantics bu commit'te WorldFactory tarafından ilkelendirildi.
- **W34 (`3aa87cf6` + `9012485e`)** — SLEEP + WORK slice + story tests; magic/combat doğrudan dokunulmadı ama `WorldState.PlayerShieldBuffs`/`PlayerSpellCooldowns` save mapper'ında persist edildi (`Assets/Scripts/Data/Save/SliceJson/WorldSaveMapper.cs`).
- **W35 (`20a3b899`)** — ScheduleSystem shrink + ownership genişlemesi; RUH_TESHIS §8 = 10/10; magic/combat alanları FieldOwnershipRegistry'ye HÂLÂ eklenmedi (borç).
- **W36 (`f6c9e2d0`, RUH_TESHIS tail)** — BUG_REPORT_SCORECARD'da B12 "SHIPPED (fizzle bileşeni)" işaretlendi; asıl "hot-path bridge" (SpellResolver + EffectDefinition adapter bağlantısı) bu commit'te de gerçekleşmedi — dead-code-pipeline etiketi INDEX'te taşınıyor.

## Bilinen Borçlar + Kaçak Kapıları

1. **B12 hot-path bridge STILL OPEN** — `Simulation/Magic/SpellResolver.cs` + `EffectDefinition` + `EffectOperationHandlers` boru hattı yalnız `Assets/Tests/EditMode/Magic/EffectHandlerTests.cs` tarafından kullanılıyor. Canlı cast hâlâ `SpellExecutionService` + `SpellEffectResolutionService` (legacy `SpellEffectCode`) üzerinden geçiyor. Yeni spell eklemek için `WorldSpellCatalog`'a `SpellDefinition` yazmak gerekiyor, `EffectDefinition` satırı değil. Kaçak: her yeni büyü, `SpellEffectResolutionService.IsSupported` beyaz-listesine bir switch daha eklemeye zorluyor.
2. **Open-set VFX realtime saatinde** — `RuntimeSpellFxMirror.LightUntilRealtime` / `HasteUntilRealtime` `Time.unscaledTime + N`ile hesaplanıyor (`Combat.Spells.cs:133-140`). Save/pause/hızlandırma senaryosunda oyun-zamanı ile ayrışıyor; oyuncu kayıt sonrası aynı FX'i yeniden yaşamıyor.
3. **`_meleeStrikeSerial` save persiste değil** — Session-local (`Combat.Melee.cs:83`); spell RNG'si de aynı serial'i kullanıyor (`Combat.Spells.cs:89`). Save/load spell zar sonucunu değiştirebilir (aynı frame'de commit çakışması). Belge: F14 root-fix zamanı fark edildi, hâlâ açık.
4. **FieldOwnershipRegistry combat/magic satırları eksik** — `PlayerSpellCooldowns`, `PlayerShieldBuffs`, `PlayerBountyGold`, `PlayerReputation` mutasyonları registry'ye yazılı DEĞİL; W35 ownership genişlemesi bu satırları kaçırdı.
5. **`SelectSpellTarget` friendly seçimi effect-koda göre** — "wantsFriendly" karar effect kodlarını sniff'liyor (`Combat.Helpers.cs:66-80`); yeni bir healing kodu (örn. `RestoreArmor`) eklendiğinde bu blok güncellenmezse hostile hattına düşer ve caster'ı hedefler.
6. **`SpellExecutionService` "legacy" damgalı** — `Simulation/Magic/SpellExecutionService.cs:20` XML doc kendini "LEGACY spell pipeline" ilan ediyor ama `DomainSimulationAdapter.Combat.Spells.cs:77-95` hâlâ ondan yeni instance kuruyor (`new SpellExecutionService(...)` her cast'te). Amortized cost + zayıf ayrılım — resolver instantiation cast başı allocation üretiyor.
7. **`SpellEffectResolutionService` open-set kodları sessizce atlıyor** — F28 sonrası validation "hard reject" yerine "skip" yaptığından `light/haste/recall` gibi kodlar mana harcıyor ama ResolutionResult'da appliedCount=0 kalıyor; testler bunu pinliyor ama telemetri (SpellResolved event) magnitude alanında 0 yazıyor — analytics dashboard'ları için asymmetric.
8. **Melee `rawDamage` argümanı yarı kullanılıyor** — `TryMeleeStrike(target, rawDamage)` `combatOptions.MeleeRawDamage`'i alıyor ama `CombatActionResolver` `base_minus_armor` formülü kendi damage tablosunu kullanıyor; `rawDamage` yalnız `IDamageSink.Apply(rawDamage)` görsel/HP kaybı için. Domain hasar ile presentation hasar drift'i codex 6. pas dokümantasyonunda uyarıldı, birleştirme yapılmadı.
9. **`CameraFacingBillboard` singleton bağımlılığı** — Bolt her cast'te `AddComponent<CameraFacingBillboard>()` kullanıyor; `Camera.main` yoksa null-fallback yok, playtest'te editör kamerası kaybolduğunda fireball düz kalabilir.
10. **`SpellSlots` fallback catalog döndürüyor** — `WorldRows.cs:107-116` `PlayerKnownSpellIds` boşsa full catalog döner. Yeni-oyun kaydında oyuncu her sekiz büyüyü de biliyormuş gibi hotbar dolu görünüyor; asıl "learned spells" akışı yok.
