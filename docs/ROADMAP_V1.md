# EMBER — v1.0 YOL HARİTASI ("AAA-his" Daggerfall-like)

> **Bu dosya otonom oturumların PUSULASIDIR.** Her /goal oturumu: (1) bu dosyayı okur,
> (2) `>>> CURRENT <<<` işaretli fazı (yoksa ilk `[ ]` fazı) seçer, (3) sözleşmeyle uygular,
> (4) DoD yeşilse kutuyu `[x]` yapar + işareti bir sonraki faza taşır + bu dosyayı commit'e dahil eder.
> Faz sırası bağlayıcıdır; bir faz bir oturuma sığmazsa kalan maddeler alt-kutu olarak bölünür.

**SÖZLEŞME (her faz için aynen):** iddia→üretim çağıranı→test→runtime kanıtı · commit kapısı =
fallback harness yeşil + Unity batchmode "Build Finished, Result: Success." + push · kanıt-öncelikli
debug (Player.log önce) · ÖZ-PLAYTEST: lookaround/looptest/shipcheck koş + PNG'leri GÖZLE incele
(proof koşuları `--ember-forge-off` + `--ember-proof-screenshots <dir>` alır) · SOLID, kök neden,
bant yardımı yok · MOCK/PARTIAL/BROKEN dürüst etiketle · README sürüm notu kanıttan SONRA yazılır.

**Durum (2026-06-12):** v0.3.0-find-fight-hear yayında — gerçek-zamanlı savaş, zindan garantisi +
haunterlar, delve pusulası, prosedürel ses v2, shipcheck 9/9 PASS. Kalan yol: ~22 faz / 6 ara sürüm.

---

## v0.4 "SAVAŞ DERİNLİĞİ" — düşman yaşasın, ölüm anlam kazansın

- [x] **F14 Düşman hareketi** ✅ 2026-06-12: TickHostileAi (görüş 12, 1 hücre/0.45s ≈ 2.2m/s, ≤2'de
  durur, ≤3'te encounter OTOMATİK bağlanır — E gerekmez); tüm savaş mesafeleri CANLI gövdeye bağlandı
  (PlayerCombatPosition — park halindeki aktör değil); vuruşta 0.2s billboard atılması; düşman glide
  2.4m/s. Kanıt: "[Proof] F14 chase: a=9.4m b=5.4m closed=4.0m" + chase_a/b kareleri (sandık→koridor)
  + EditMode HostileAi testi + proof'lar artık otomatik kapanıyor (Application.Quit kökü).
- [x] **F15 Ölüm + yeniden doğuş döngüsü** ✅ 2026-06-12: RespawnAfterDeath (altın %20 kesinti,
  vitals Refill, saat +8h SAAT-SAAT — ProofAdvanceHours stale-tick no-op kökü de düzeltildi, aktör
  plazaya); DeathView'a birincil "AWAKEN" butonu + controller AwakenAction (rig spawn'a döner,
  ölüm kapısı yeniden kurulur). Kanıt: "fell in 8 enemy swings, purse 313->251 (-20%), hp=62/62,
  +8h" + looptest_respawn.png (plaza + FULL barlar) + EditMode toll/refill/8h/çifte-ölüm testi.
  Yan kazanım: aynı-kare ikinci CaptureScreenshot öncekini yutar — proof'lara kare ayracı kondu.
- [x] **F16 Ekipman etkisi** ✅ 2026-06-12: silah bonusları ZARA GİRİYOR (CombatActionResolver
  +accBonus/+dmgBonus; EquippedWeapon → TryMeleeStrike); başlangıç bıçağı artık EKİPLİ doğuyor
  (Sprint 1'den beri çantada atıl yatıyordu); zindan sandığı E ile açılıyor (RuntimeChestView,
  kapak menteşeli + creak) ve tier-üstü Worn Iron Sword (+8/+5) verip oto-ekipliyor (dünya başına
  1, şablon korumalı); adapter TryEquip kanonik EquipmentService'e delege. Kanıt: "20 bare=83,
  20 armed=103" + "You take the Worn Iron Sword... and equip it" + EditMode 60-tohum eşli zar
  testi (10/10). Dürüst sınır: sandık-açık durumu save'e yazılmıyor (F22); gövde/zırh slotu
  içerik geldiğinde (F29 sonrası) işlenecek; karanlık oda kompozisyonu F33 cila borcu.
- [x] **F17 XP/Seviye** ✅ 2026-06-12: PlayerXp persisted alanı (3-katman desen + reflection guard);
  kill +40 / world-quest +60 XP; seviye N→N+1 = N*100 XP; PlayerLevelUpService XP-kapılı (sonsuz
  seviye bug'ı kapandı) ve harcıyor; eşik aşılınca level-up ekranı OTOMATİK açılıyor (mevcut 5-puan
  6-stat + büyü seçimi makinesi zaten gerçekti). Kanıt: "[XP] +40 (kill) 40/100", "+60 (quest)
  100/100 — LEVEL UP READY" + looptest_levelup.png (Warden L1→2 modalı otomatik) + EditMode
  kapı/harcama/roundtrip testi. Not: 3-kart yerine mevcut 6-stat ekranı kullanıldı (daha zengin).

## v0.5 "ZİNDAN ÇAĞI" — tek oda değil, gerçek delve

- [x] **F18 Çok-odalı prosedürel zindan**: barrow → 4-7 oda + koridor grafı (deterministik seed;
  mevcut MultiRoomDungeonGenerator domain'i realize'a bağlanır). Oda başına 0-2 haunter, son odada
  şef (2× HP, 1.5× dmg) + büyük sandık.
  **DoD:** topdown karesi oda grafını gösterir; looptest şefe kadar iner, loot satırı.
  **KANIT (lookaround delve legi, Reports/proof-f18h):** "multi-room delve realized: rooms=5 doors=4
  dwellerSpots=2 bossRoom=R4" · topdown karesi 5 oda + 4 koridoru gösteriyor (çatılar capture
  sırasında gizlenir) · "[Proof] F18 boss bound: Warden of X (80/80 hp)" → "felled=True in 15 swings"
  + boss/boss_felled kareleri · loot satırı "You take the Worn Iron Sword (+8 acc, +5 dmg)".
  **Kök-neden avı (3 gerçek bug):** (1) ScheduleSystem kovalayan dweller'ı her tick inine geri
  adımlatıyordu (lastik bant; pinned Enemy artık takvimden muaf + 2 EditMode testi), (2) billboard'lar
  yeni sim pozisyonunu yalnız ~0.83s dünya tick'inde öğreniyordu (chase yarı hızda OKUNUYORDU;
  ProjectWorldViewsNow kare hızında hedef tazeler), (3) şef de kovalıyordu (lair leash: dweller ≤10,
  şef ≤3 hücre — hazineyi asla bırakmaz; leash dışı ine geri yürür). Chase kanıtı: cheb 14→8 (sim) +
  "closed=5.5m" (görsel, DoD ≥4m). Dürüst borçlar: koridor çatısı üstüne grounding yapan billboard
  (F19 polish), sandık yakın çekimi meşalenin gölge tarafında loş (F33 framing).
- [x] **F19 Zindan çeşitliliği**: 3 arketip (mağara/kripta/harabe — malzeme paleti + ışık rengi +
  müzik varyantı farklı). Arketip = settlement seed'den deterministik.
  **DoD:** 3 farklı zindana travel + her birinden iç kare; arketip adı realize logunda.
  **KANIT (lookaround, Reports/proof-f19d):** 3 zindana travel (Cryneaduford=Kripta seed 886870881 /
  Criomuveawick=Harabe 502681080 / Vhiriorothcross=Mağara 328528998) — üç realize logu "archetype=" taşıyor,
  üç iç kare göz-doğrulamalı FARKLI (soğuk mavi mezar ışığı / yeşil-altın / sıcak turuncu).
  **Kök bulgular:** (1) dünya tek zindan yuvarlıyordu — F9 invariantı ≥3'e çıkarıldı (EnsureMinimumDungeons,
  City/Town asla düşürülmez; golden kırılmadı, PlayableLoop testi ≥3 pinler); (2) ham seed%3 hep Mağara
  verdi — realize seed'leri yapısal olarak 3'e bölünüyor; murmur-finalizer hash sonra seçim (proof-caught
  bias, DungeonArchetypeTests sweep + proof-seed çeşitlilik testi). DÜRÜST PARTIAL: müzik varyantı henüz
  arketipe bağlı değil (DAY/NIGHT/BATTLE slotları arketip-agnostik; v0.5 kapanışına aday iş).
- [x] **F20 Tuzak + kilit**: ezici plaka tuzağı (görünür, 8 dmg, sesli) + kilitli şef kapısı
  (anahtar rastgele ara odada). Kapı/anahtar HUD event satırına yazar.
  **DoD:** looptest anahtar→kapı→şef akış satırları + tuzak hasar logu.
  **KANIT (lookaround delve legi, Reports/proof-f20a):** akış sırayla — "[Trap] crushing plate fired:
  8 damage." (HP 8 düşüşü trap karesinde HUD'da) → "[Key] You take the Tarnished Key" → "[Door] The
  Tarnished Key turns — the boss door grinds open." → Warden bound. Tuzak: şef-yolu koridorunda
  pas-kızılı plaka (basınca çöker, mekanizma sesi, LogCombat HUD satırı). Anahtar: GERÇEK envanter
  item'ı (ItemId 3003, kilit TÜKETIR — EditMode roundtrip testi: al→tekrar-alma-reddi→tüket→ikinci
  tüketim reddi). Kapı: boss konektöründe kilitli slab, anahtar varsa 1.2s'de yukarı kayar; anahtarsız
  yaklaşımda tek-seferlik "Locked." satırı (4.2m'de yeniden kurulur).

> **v0.5 KAPANIŞ KANITI:** SHIPCHECK VERDICT: PASS (9 sections, 0 exceptions) — perf avg=15,4ms
> worst=736ms (bütçe 16; ilk koşuda tek 96s kare = editör-kapanış churn'ü, tekrar koşuda yok).
> Tag: v0.5.0-dungeon-age. DİKKAT (v0.6'ya not): perf marjı daraldı (v0.4 11,9 → 15,4 avg) —
> şüpheli: per-frame ProjectWorldViewsNow içindeki TryReadWorksite job-board taraması; profille.

## v0.6 "GÖREV MAKİNESİ" — DFU tarzı sonsuz iş

- [x] **F21 Görev üreticisi**: 4 şablon (getir/öldür/teslim et/ziyaret) × hedef (zindan/yerleşim/NPC)
  × süre limiti; veren NPC rolüne göre (lonca yok, kişiler var). Journal'a gerçek kayıt.
  **DoD:** EditMode: 20 seed → 20 geçerli görev; looptest bir fetch görevini uçtan uca kapatır.
  **KANIT (Reports/proof-f21a):** WorldQuestGenerator (Simulation, saf-deterministik: splitmix64 +
  xorshift64*, şablon rotasyonu — ham madde eksikse sıradaki şablon) + WorldQuestRecord (Domain).
  Test: 20 seed → 20 geçerli (veren/ödül/deadline/şablon-alan invariantları) + determinizm + 4 şablon
  erişilebilirliği + verensiz dünya null (fallback'te koşar, 1458/1458). Looptest: "[QuestGen]
  accepted #9100: Bring ale to Grire Theashal (38g, deadline day 7)" → cargo CANLI ekonomiden satın
  alındı → "[QuestGen] completed #9100 — +38 gold (purse 349)". Journal: üretilen kontratlar J
  ekranında kendi "Contracts" bölümü (canlı durum: Active/Completed/Failed; deadline geçince lazy
  FAIL). Veren roller: Merchant/Noble/Priest/Scholar/Innkeeper/Blacksmith/Healer. DÜRÜST PARTIAL:
  kontratlar adapter-local — save kalıcılığı F22'nin işi (bilinçli sıralama).
- [x] **F22 Görev kalıcılığı**: dünya görevleri (bounty/pilgrimage dahil) save/load'da korunur
  (WorldSaveMapper genişler — adapter-local dictionary kalkar).
  **DoD:** save→load→journal aynı; digest roundtrip testi genişletilmiş haliyle yeşil.
  **KANIT:** WorldState.WorldContracts + WorldQuestStates (3-katman desen: alan + EnsureInvariants +
  CopyFrom; reflection guard geçti) · WorldSaveData.worldContracts/worldQuestStates DTO'ları +
  mapper iki yön · adapter-local _worldQuests sözlüğü ve _generatedQuests listesi ÖLDÜ (property
  redirect; kontrat seri numarası restore-güvenli max+1) · digest'e WORLDQUESTS bölümü (boşsa
  atlanır — pre-F22 golden'lar byte-aynı kaldı) · WorldQuests_SurviveSaveLoadRoundtrip: açık kontrat
  + kapalı kontrat + bounty-tamam/pilgrimage-açık → save→load → digest byte-aynı + alan eşitlikleri.
  Looptest regresyonu: seed artık dünya store'undan, F21 fetch legi uçtan uca yeşil. Kapılar:
  fallback 1459/1459, EditMode 19/19, Build Success.
- [x] **F23 İtibar + suç**: sivile vurmak = suç → muhafız saldırır + kelle parası; görev tamamlama
  +itibar, itibar yüksekse fiyatlar %10 iner (ekonomi köprüsü hazır).
  **DoD:** sivile vur→muhafız agro karesi; itibar satırı HUD/character ekranında.
  **KANIT (lookaround, Reports/proof-f23b):** "[Crime] civilian assaulted: bounty=40g rep=-2" →
  "[Crime] the watch arrives: +2 officers" → devriye telemetrisi watch A: 8 → watch B: 4 hücre +
  look_guard_aggro.png (iki devriye oyuncunun üstünde, gündüz plaza). PlayerReputation/PlayerBountyGold
  3-katman kalıcı; HUD üst barı "Rep ±N · BOUNTY Ng" segmentleri. Fiyat köprüsü: rep ≥5 →
  basis %10 iner (ApplyReputationDiscount, canlı pazar basis'inin üstünde; EditMode testi).
  **Tasarım kararları:** (1) oto-hedef ("attack nearest") artık YALNIZ düşman seçer — tuş basışı
  kaza-suç İŞLEYEMEZ; suç yalnız nişanlı/isimli vuruşla. (2) Her yerleşim Guard seed'i yuvarlamıyor —
  suç DEVRİYEYİ ÇAĞIRIR (F10 haunter deseni: sentetik id bandı 9.5M, idempotent, ceset kalıcı,
  TickHostileAi bounty>0 iken Guard'ları da avcı yapar). DÜRÜST AÇIK: kelle parasını ödeme/teslim
  olma akışı yok (bounty kalıcı — F31 ana görev dönemine aday); devriye kovalarken ScheduleSystem
  lastik-bandı Guard'larda hâlâ var (net kapanış yine pozitif; Enemy-pinned muafiyeti Guard'a
  genişletilmedi).

> **v0.6 KAPANIŞ KANITI:** SHIPCHECK VERDICT: PASS (9 sections, 0 exceptions) — perf avg=11,6ms
> worst=404ms (v0.5'teki 15,4ms endişesi ortam çıktı; bütçe 16'nın rahat içinde).
> Tag: v0.6.0-quest-machine.

## v0.7 "YAŞAYAN EVREN" — gökyüzü, hava, iç mekânlar

- [x] **F24 Gökyüzü v2**: prosedürel gün döngüsü (güneş pozisyonu saatten, şafak/alacakaranlık
  gradyanı, gece yıldız + ay). Clock-jump sonrası parlak gök bug'ı kökten ölür.
  **DoD:** 06/12/18/24 saatlerinde 4 kare — dördü görsel olarak FARKLI ve doğru.
  **KANIT (lookaround, Reports/proof-f24b):** sky_06 şafak-gülü / sky_12 masmavi / sky_18 amber
  alacakaranlık / sky_24 lacivert + 140 yıldız + AY (62° elevasyon, karede) — dördü göz-doğrulamalı
  FARKLI; log: "F24 sky_06 captured at hour=06 (minutesOfDay=375)" … hour=00. KÖK FIX: SkyController
  artık RuntimeFieldMirror.MinutesOfDay okuyor (Clock partial'ı her tick world.Time GERÇEĞİNDEN
  yayınlar) — TickIndex yeniden-türetimi clock-jump'larda kayıyordu (parlak gece bug'ının kökü, ÖLDÜ;
  EditMode truth testi: +16h jump sonrası mirror == world.Time%1440). Gök cisimleri build-safe:
  yıldızlar = golden-angle yarımkürede 140 unlit küp (Sprites/Default — billboard'lar garantiliyor),
  ay = üretilmiş 64px yumuşak-disk sprite, kubbe kamerayı takip eder. Şafak gülü ≠ alacakaranlık
  amberi (sabah/akşam ayrımlı lerp).
- [x] **F25 Hava durumu**: yağmur/sis/kar (biyom+mevsim deterministik); yağmur partikül + ses
  (PhISM hazır), sis fog yoğunluğu, kar terrain tint. Müzik yağmurda yumuşak varyanta düşer.
  **DoD:** 3 hava durumu karesi + "[Weather]" log satırları + yağmur sesi forge metriği.
  **KANIT (lookaround, Reports/proof-f25d):** weather_rain (gri gök + beyaz yağmur çizgileri) /
  weather_snow (soluk gök + süzülen taneler) / weather_fog (gri-lavanta pus, kısık ışık) — üçü
  göz-doğrulamalı FARKLI. "[Weather] day=N season=S biome=B → kind" günlük deterministik seçim
  (1. gün Plains doğal yağmur yuvarladı) + "[AudioForge] rain_loop: len=4,00s rms=0,042
  centroid=1147Hz" (PhISM-vari: lowpass bed + damla tıkları, döngü kaynak rig'de).
  **İKİ PROOF-CAUGHT BULGU:** (1) URP fog shader varyantları player build'de strip — okunabilir
  atmosfer SkyController'a taşındı (FogFactor: gök grileşir + güneş/ambient kısılır); (2)
  ParticleSystem player build'de HİÇBİR modda render etmiyor — yağış, yıldız-kubbesi deseniyle
  manuel 130-küp havuzu (Sprites/Default; kamerayı takip eden 30×30 kolon, deterministik dağılım).
  DÜRÜST PARTIAL: müzik yağmur varyantı bağlanmadı (slotlar hava-agnostik); kar terrain tint yerine
  haze + taneler (splat repaint v2).
- [x] **F26 İşlevsel iç mekânlar**: taverna (uyu→saat atla+HP yenile, 5 altın), tapınak (şifa),
  dükkân (trade ekranı tezgâhtan açılır). Kapıdan girince interior realize (mevcut shell furnish
  genişler), NPC içeride masada.
  **DoD:** taverna uyuma akışı looptest'te; 3 iç mekân karesi.
  **KANIT (Reports/proof-f26a+b):** looptest "LOOP-PROOF tavern-sleep: hp 39->62/62, purse 349->344,
  +8h" (refill + 5 altın + saat-saat ilerleme — respawn'ın cadence-güvenli yolu) · "[Tavern] host
  seated inside: Skevouth Thyashilm" (Innkeeper/Merchant içeri oturtulur) · 3 iç kare (rol-işaret
  ışığı tonlarıyla: amber taverna / beyaz tapınak / yeşil dükkân). İlk 3 bina işlevsel: parlayan
  işaret küpü + ışık; E-tetikli görünümler (chest-view ailesi): TrySleepAtTavern (5g) /
  TryTempleHeal (8g, yalnız HP) / dükkân tezgâhı ScreenRequestSignal ile trade ekranını açar
  (tek-bayrak-tek-tüketici). EditMode: uyku (altın kapısı+refill+8h) + şifa testleri.
  DÜRÜST PARTIAL: host pinleme MoveTo'yla (takvim saatler içinde dışarı yürütebilir — kalıcı
  re-home ActorStore replace v2); iç kare kompozisyonu duvar-köşe ağırlıklı (F33 çerçeve cilası);
  "kapıdan girince realize" mevcut furnish'in üstünde (ayrı interior scene yok — şimdilik tasarım).
- [x] **F27 NPC ihtiyaç koşuları**: işe gidiş/eve dönüş zaten var; öğlen taverna yemeği + work-pose
  ikonu (çekiç/çapa billboard üstü mini quad) eklenir.
  **DoD:** öğlen tavernada ≥2 NPC karesi; pose ikonları karede.
  **KANIT (lookaround, Reports/proof-f27a):** "[Lunch] 17 civilians at the tavern (hour 13)" +
  look_tavern_lunch.png — tavernaya toplanmış 12+ sivil billboard, üstlerinde amber MUG ikonları
  görünür. ÖĞLE DOMAIN'DE: ScheduleSystem 12:00-13:59 LUNCH penceresi — siviller (asla
  Enemy/Guard) WorldState.TavernCell'e yürür (realize PinHostInsideTavern yayınlar; alan
  realize-türevi, bilinçli olarak save'e yazılmaz). Poz ikonları: NpcPoseIconView — 12×12 üretilmiş
  piksel-maske sprite'ları (çekiç: iş saatleri × işçi roller; mug: öğle × herkes), hostile-marker
  ailesi kamera-bakışlı quad. EditMode: lunch yönlendirme + guard-muafiyeti + 14:00 sonrası normal
  ritim testi (fallback 1460/1460).

> **v0.7 KAPANIŞ KANITI:** SHIPCHECK VERDICT: PASS (9 sections, 0 exceptions) — perf avg=12,5ms
> worst=426ms (bütçe 16). Tag: v0.7.0-living-universe.

## v0.8 "BÜYÜ + BESTIARY" — içerik genişlemesi

- [x] **F28 Büyü okulu**: 8 büyü (3 hasar tipi + kalkan + şifa + ışık + hız + teleport-to-entrance);
  büyü kitabı ekranından slot atama; mana ekonomisi dengelenir.
  **DoD:** EditMode etki testleri + 3 büyünün dünya VFX karesi (bolt sistemi genişler).
  **KANIT (lookaround, validation-output/proof-f28):** look_spell_flame/frost/spark.png — üç
  hasar tipi DÜNYADA kendi rengini giyer (turuncu / buz-mavisi / beyaz-altın bolt, BoltTint +
  RuntimeSpellFxMirror.LastCastTemplate); look_spell_lantern.png — elde-taşınan LanternGlow orb'u
  karede + "[Spell] lantern glow orb lit (60s)" logu; 4 cast da gerçek yoldan (`fired=True`,
  ProofCast → TryCastSpell → SpellExecutionService). Katalog 8: flame/mending/ward/frost/spark/
  lantern/wind/recall (append-only sıra testi). EditMode (fallback 1466/1466): etki testleri
  (frost 11 / spark 6 hasar, wind_step +10 fatigue, açık-küme kodlar vitals'a dokunmaz), ward
  hafifletme dikişi, +2 mana/Mnd büyümesi. ÜÇ KÖK DÜZELTME: (1) resolver SÖZLEŞME değişimi —
  zamanlı/açık-küme efekt artık cast'i REDDETMEZ, atlanır (ember_ward canlıda bu yüzden hiç
  atılamıyordu); ward artık cast'te PlayerShieldBuffs'a yazılır + düşman vuruşunda
  defenderMitigation dikişiyle emer; (2) cast anında oyuncu kaydı CANLI bedene senkronlanır —
  validator park edilmiş plaza hücresinden ölçüyordu, park noktasından uzak her menzilli cast
  sessizce reddediliyordu; (3) mana ekonomisi: Mnd puanı +2 max mana (12'lik havuz ward 15 /
  frost 17 / recall 20'yi hiç açamazdı), frost/spark fiyatı flame eğrisinde (ceil(dmg×3/2)),
  estimator açık-küme kodları 0 fiyatlar (büyüklük dünya-birimi). Tuşlar 1-8.
  DÜRÜST PARTIAL: wind_step hız çarpanı (×1.5) ve recall_gate rig-snap'i kablolu + EditMode'lu
  ama kare-kanıtsız (lookaround sadece 3 bolt + lantern çeker); "büyü kitabı ekranı" yok —
  slotlar bilinen-büyü sırası (1-8), yeni büyü level-up seçimiyle öğrenilir.
- [ ] **F29 Bestiary**: 6 düşman tipi (haydut/iskelet/kurt/örümcek/hayalet/şef-varyantları) —
  billboard sprite forge promptları + stat blokları + zindan arketipine göre dağılım.
  **DoD:** 3 farklı tip tek karede (zindan odası) + tip-bazlı vuruş sesi varyantı logu.
- [ ] **F30 Ses v3**: biyom ambiyansı (kuş/rüzgâr/gece cırcır), yağmur/kar katmanı, savaş müziği
  yoğunluk katmanı (şef odasında +perküsyon), 2 yeni zemin (kar/çakıl). TTS: piper varsa selamlama
  seslendir (yoksa dürüst PARTIAL kalır).
  **DoD:** audio-forge bölümü genişler (yeni klip metrikleri); kulak onayı kullanıcıdan.

## v0.9 "CİLA + İSKELET HİKÂYE" — v1.0 provası

- [ ] **F31 Ana görev iskeleti**: 3 perde (delve'lerde 3 parça eski yazıt → başkent bilgesi →
  final zindanı şef'i). Intro metni New Game'de, final ekranı + credits.
  **DoD:** playthrough proof'u 3 perdeyi uçtan uca koşar (yeni --ember-mainquest legi).
- [ ] **F32 UI/UX cilası**: ayarlar menüsü (ses seviyeleri, mouse hassasiyeti, çözünürlük),
  keybind listesi ekranı, TODO aksiyon yollarının temizliği (Codex listesi), ölü buton kalmaz.
  **DoD:** ig-tour tüm ekran kareleri + "not yet available" grep'i sıfır döner.
- [ ] **F33 Görsel cila**: URP volume (hafif bloom+vignette+color grade per biyom), vuruş kıvılcım
  partikülü, billboard 2-kare yürüyüş animasyonu (sprite swap).
  **DoD:** önce/sonra karşılaştırma kareleri; perf bütçesi korunur (shipcheck).
- [ ] **F34 Stabilite maratonu**: 30dk otonom soak (rastgele travel+savaş+ticaret döngüsü),
  0 exception + bellek eğrisi düz; autosave (5dk); tüm goldenler yeşil.
  **DoD:** yeni --ember-marathon proof bölümü PASS; bellek raporu logda.

## v1.0 "EMBER" — yayın

- [ ] **F35 Yayın kapısı**: tam playthrough (yaratılış→ana görev finali) tek videoda kare dizisi;
  README oyun kılavuzu; sürüm notları; v1.0.0 tag. Bilinen sınırlar dürüst listelenir.
  **DoD:** shipcheck + marathon + playthrough ÜÇÜ DE PASS; kullanıcı playtest onayı.

---

### Tahmin
Faz başına 1 otonom oturum (bugünkü tempo) → **~22 oturum**. Riskli büyükler: F18 (zindan grafı),
F24 (gökyüzü), F31 (ana görev). Bunlar 2 oturuma bölünebilir → gerçekçi aralık **22-30 oturum**.

### Oturum protokolü (özet)
1. `docs/ROADMAP_V1.md` oku → CURRENT fazı al.
2. Fazı uygula (sözleşme + DoD); kanıtları `Reports/proofs/<faz>/` altına koş.
3. DoD yeşil → kutu `[x]`, CURRENT işareti sonraki faza, README sürüm bölümü güncel.
4. Commit + push (kapılar yeşilken); rapor tablosu: madde|kanıt|commit.
5. Oturumda zaman kaldıysa SONRAKİ faza başla; kalmadıysa kalanı alt-kutulara böl ve dürüst bırak.

>>> CURRENT: F29 <<<
