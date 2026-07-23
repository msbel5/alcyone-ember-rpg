# ROADMAP V2 — "CAN SUYU" (Living-World Rewiring)

**Teşhis (2026-07-22 denetimi):** v1.0-RC'nin görünür canlılığının ~%70'i sahneleme; tick yalnız
çapa-yürüyüşü + izlenmeyen sayı kayması üretiyor. Üç kök neden: (1) tüketim döngüsü yok
(NeedRecoverySystem 0 çağıran; açlık tek yönlü), (2) karar mekanizması yok (Needs/Mood/Memory
davranışı sürmüyor), (3) olay kaskadı yok (ShortageDetected'i kimse okumuyor; tek reaktif sistem
render pompasında ve oyuncu-odaklı). Süreç nedeni: 22 fazın 0'ı simülasyon-özelliği istedi —
kanıt sözleşmesi koreografiyi seçti. Tam denetim: oturum raporu 2026-07-22.

**YENİ KAPI SÖZLEŞMESİ (H5 — tüm V2 fazlarını bağlar):**
Sabit-saat/sabit-hücre kare-DoD YASAK. Her faz şu üç kapı ailesinden en az birine yeni assert ekler
(Assets/Tests/EditMode/CanSuyu/LivingWorldGateTests.cs):
- **Yaşanabilirlik:** N gün oyuncusuz koşu → ihtiyaç ortalamaları duvar altında + stoklar iki yönlü akar.
- **Müdahale duyarlılığı:** gün-0 pertürbasyonu (bir işçi ölür) → gün-N yörüngesi kontrolden sapar.
- **Scriptsiz olay oranı:** sistem-kaynaklı olay/gün ≥ eşik, sıfır oyuncu eylemiyle.
Kareler hâlâ alınır (görsel doğrulama) ama tek başına HİÇBİR fazı geçiremez.

## Hamleler

- [x] **H1 Tüketim devresi** (KANIT: Gate1+Gate3 yeşil; fallback 1477+; 3dk marathon PASS; commit fa56173e): NeedConsumptionSystem (aç→stoktan yer, gece→uyur) hourly band'e;
  ShortageResponseSystem (kıtlık→ekim işi, ilk kaskad) daily band'e. FarmingJobRequestFactory
  nihayet çağrılıyor. **DoD:** Gate1+Gate3 yeşil; fallback+build; lookaround'da "[Lunch]" yerine
  gerçek meal_eaten olay akışı loglarda.
- [x] **H2 Karar katmanı** (KANIT: pencere kodu SİLİNDİ; Gate4 dalga kapısı — 48 saatlik larder doluluk dalgası amplitüd ≥2, kendiliğinden; ChooseTarget karar-tablosu testleri; oranlar 24h yaşama ölçeklendi +8/+6/+5; masa-başı yemek reach=2): ScheduleSystem'in çapa-arayışı yerine utility seçici — en yüksek
  ihtiyaç kazanır (aç→yemek noktası, yorgun→ev, değilse iş/sosyal). Lunch penceresi SİLİNİR
  (öğle kalabalığı açlık ritminden kendiliğinden doğmalı). Referans: ember-rpg/frp-backend
  behavior_tree.py. **DoD:** Gate2 + "tavernaya öğlen scriptsiz toplanma" (pencere kodu yokken
  12-14 arası taverna sayımı > sabah sayımı — çünkü açlık sabah yükselir).
- [x] **H3 Olay kaskadı**: predasyon simülasyona taşındı (PredationSystem Hourly:40 — avcı en
  yakın sivili avlar, NPC-vs-NPC CombatResolved); WitnessResponseSystem (Hourly:45) son saatin
  saldırılarını tarar → 8 hücre içindeki siviller GERÇEK ActorMemory kaydı yazar
  ("witnessed_attack" — NpcMemory'nin ilk runtime yazarı) + WitnessRecorded olayı; 12 hücre
  içindeki muhafızlar saldırgana yakınsar, temas menzilinde GuardResponded + karşı vuruş.
  Not: MemoryWriteSystem değil ActorMemory kullanıldı — MemoryComponent/ActorMemory çift-beyin
  ayrığı keşfedildi, runtime yol NpcMemory. **KANIT:** Gate5_EventCascade yeşil (2 headless gün:
  Enemy CombatResolved + WitnessRecorded + witnessed_attack hafızada, derinlik ≥2);
  fallback 1480/1484, Build Finished Success.
- [x] **H4 Runtime tarih**: RuntimeHistorySystem (Daily:28 "world.runtime_history") — (a) dünkü
  SİMÜLASYON olayları ilişkileri sürükler (GuardResponded → watch_renown +1; ShortageDetected →
  grain_tension -1), (b) ay sonunda (gün 30, 60…) RoomSeed'e bağlı kronik olayı: festival
  (+4 bağ) / kervan dalgası (ambara gerçek buğday) / sınır anlaşmazlığı (-6) + diplomatik
  dalgalanma (±4) + ChronicleEvent(=32) kaydı. YAN KAZANÇ: catchup kontratı onarıldı — H1/H3/H4
  sistemleri artık world.Time değil sınır damgası (context.Stamp) ile damgalıyor; log artık
  damga-monoton olmadığından pencere taramaları break değil continue. **KANIT:** Gate6 yeşil
  (31 gün × 2 seed: ≥1 kronik + ilişki matrisi değişti + seed'ler FARKLI tarih yazdı);
  MultiDayCatchup invaryantı stamp düzeltmesiyle yeşil; ActiveDelta goldeni +28 re-baseline
  (günlük grain_tension gerçek); fallback 1481/1485, Build Finished Success.
- [x] **H5 Kapı genişletmesi**: Gate7_SeedDivergence (3 seed × 31 gün → sayım vektörleri
  [stok, öğün, olay sayısı, itibar matrisi, kronik] üçü de ikili FARKLI) + GateContractLintTests
  (kapı testleri zaman İLERLETMEK zorunda, tek-kare/screenshot kanıt token'ları CI'da red;
  roadmap DoD satırlarında sabit-saat/screenshot vaadi yasak). BONUS dürüstlük onarımı:
  maraton sürücüsü "adapter lost" iptalinde PASS basıyordu — verdict artık aborted/actions==0
  için FAIL; adapter için 120 sn'ye kadar bekleme (forge-on boot ~70 sn). **KANIT:** fallback
  1484/1488; Build Success; canlı 3 dk maraton PASS actions=37 (travel=11 fight=13 trade=11
  clock=2) 0 istisna bellek düz; lookaround ekran görüntüleri validation-output/h5-live-proof.

>>> 10/10 KOVALAMACASI (2026-07-23 basladi) — kullanici mandasi: "10 olana kadar devam" <<<

## Alıcı Rubriği (her döngüde yeniden puanlanır; EN DÜŞÜK kategoriye saldırılır)
| Kategori | 07-23 sabah | Hedef |
|---|---|---|
| Özgünlük (LLM DM, hafızalı yoldaş, yaşayan sim) | 9 | 10 |
| 2D sanat | 8 | 9 |
| 3D dünya görselliği | 2 | 8 |  <- AKTİF HEDEF
| Oynanış hissi (juice) | 4 | 8 |
| İçerik derinliği | 4 | 8 |
| Ses | 3 | 7 |
| İlk saat rehberliği | 3 | 8 |
TOPLAM ALICI PUANI: 4/10 -> hedef 10/10. Kural: puan iddiası KANITLA (kare/video/log) gelir.

- [x] R1: prosedürel doğa zemini (3-oktav biyom paletli gürültü, terrain katmanı + kasaba
  plakası); kum bandı 4m→1.5m (kasaba pedini kum boyuyordu); deniz malzemesi koyu/emisyonsuz;
  --ember-weather kanıt kolu. Ağaç çizgisi çevresinde doku artık okunuyor.
- [ ] R2 (AÇIK — tanı kanıtlı): MESAFE BEYAZLAMASI. Renk-sonda deneyi: saf mavi albedo pastel
  lavanta çizildi → parlama albedodan BAĞIMSIZ ~%55 beyaz katkı; diffuseRemap kolu etkisiz.
  Şüpheli: sis-kapalıyken bile aktif bir atmosfer/karışım katmanı (SkyController FogFactor
  tüketimi?). Yöntem: editörde canlı frame-debugger/materyal sorgusu — kör build döngüsü DEĞİL.

>>> V3.M2 YOLDAŞ DERİNLİĞİ (2026-07-23) — TAMAM <<<

## V3.M2 — Ölüm bir hikâye vuruşudur + yoldaş sesi
- [x] Düşen yoldaş kadrodan GÜRÜLTÜYLE ayrılır (companion_fell olayı; TickFollow süpürmesi).
- [x] Yoldaş PERSONASI: LLM promptuna "paylaşılan yolun aşinalığıyla konuş" eki — taşınan ortak
  anılar (RecallLines) anlamını bulur.
- [x] Gate10_CompanionLoyalty: tam simüle bir gün (açlık+program+predasyon) sonunda yoldaş
  hâlâ partide ve oyuncunun ≤2 hücresinde. KANIT: fallback 1505/1509, ilk koşu yeşil.

>>> V3.M1 YOLDAŞ SİSTEMİ (2026-07-23) — TAMAM: parti gerçek, hafızalı, kayitli <<<

## V3.M1 — Yoldaş sistemi (kullanıcının vizyonu: LLM'le konuşan, hatırlayan yol arkadaşları)
- [x] TDD ÖNCE: 6 birim test (işe alma/menzil/kota, heel-follow/jitter-yok, koruma vuruşu,
  ayrılma) — İLK koşuda yeşil. Yoldaş ROL değil DURUM: sivil kimliğini, sprite'ını ve
  ActorMemory'sini korur → diyalog boru hattı ortak yaşananları zaten hatırlıyor.
- [x] Sim: CompanionService (kota 2, menzil 3, olay logları) + CompanionSystem
  (PerTick:21 heel-follow — schedule'dan SONRA, jitter yok; Hourly:42 koruma vuruşu —
  predasyon zarlarıyla deterministik). Registry goldeni güncel.
- [x] Presentation: diyalogda "Travel with me / Part ways" konuları (sivil+kota koşullu),
  HUD'da PARTY adları, save/load kalıcılığı (WorldSaveData.companionIds).
- [x] CANLI KANIT (agentcheck): oyuncu Sage Nera'nın yanına yürüdü, konuyu seçti →
  "Sage Nera shoulders their pack. 'Lead on, then.'" → census companions=1.
  Menzil reddi de canlı doğrulandı (uzaktan istek "too far" ile reddedildi).
- Sıradaki (M2 adayları): yoldaş ölümü/yas olayları, yoldaş anılarının selamlamalarda öne
  çıkması (envelope zaten taşıyor), parti komutları (PartyAgentToolSurface işleyicileri).

>>> V2.4 SHIP KALITE TURU (2026-07-23) — TAMAM: 22 dogrulanmis bulgu kapatildi <<<

## V2.4 — 33-ajanlik SOLID/TDD/gozlemlenebilirlik taramasi + düzeltmeler
- [x] YÜKSEK oynanış hataları: diyalogda bayat-LLM-cevabı yarışı (istek-serili sıralama);
  kasaba güvenliğinin ödül avcısı muhafızları dondurması (muhafız muaf); muhafızların ödüllü
  oyuncuya BAĞLANAMAMASI (bounty artık gerçek tehdit); ölülerin acıkması (IsAlive).
- [x] SOLID/perf: olay taramaları derinlik-kapaklı (O(tarih) büyüme bitti — canlı koşuda 40k
  olay); eksen etiketleri paylaşılan sabitler; TryGetSiteCentre tek kaynak; StepToward/FoodTags
  mükerrerlik notları; sessiz catch'e niyet yorumu; hasat nihayet PlantHarvested yazıyor.
- [x] TDD: 11 yeni birim test (NeedConsumption yeme/menzil/en-yakın-ambar; Seat 25-benzersiz-
  koltuk; RuntimeHistory determinizm+drift; Cascade ihbar-dedup+muhafız-önce). İhbar dedup
  testi GERÇEK hata yakaladı: SubjectId(string) vs ActorSeen(ActorId) kıyası — üretimde ihbar
  spami 8692'den 301'e düştü.
- [x] Video kanıtı: timelapse-pan.mp4 (360° pan, ~6 oyun saati, HP 62/62 hayatta) +
  timelapse-6h.mp4; FPS kontrolcüsü çekim sırasında kapatılıyor (pan sahipliği).
- **KANIT:** fallback 1497/1501 (11 yeni test dahil); Build Success; maraton PASS 0 istisna,
  LIVING reported=301 (dedup üretimde doğrulandı).

## V2.3 — Vitrin fazı + kendim oynayıp doğrulama
- [x] TAVAN-SPAWN düzeltmesi: crest örneklemesi artık bina/maden çatısına değil TERRAIN'e çarpıyor;
  zindan girişi oyuncuyu kayıtlı ağız noktasına, giriş zemininin üstüne koyuyor.
- [x] Dokulu çatılar + saçak basamağı (roof_thatch/slate/clay/timber, duvar paletini izler);
  kozalaklı ağaç silüeti; biyom tintli terrain katmanları + orman ayrı zemin; taş meydan diski;
  şehir yükseklik artışı (3.2).
- [x] KASABA GÜVENLİĞİ (timelapse bulgusu: meydanda hareketsiz oyuncu ~3 oyun saatinde öldü):
  gezici düşmanlar güvenli yarıçap (30 hücre) içinde saldırı BAŞLATMAZ; in sakinleri inini korur;
  oyuncunun başlattığı dövüş normal işler. Doğrulama: yeni timelapse 6 saat, HP 62/62 tam.
- [x] --ember-timelapse (30 kare / 10 sn = ~6 oyun saati, kare-kare canlılık incelemesi) ve
  --ember-agentcheck (DM + NPC diyalog + envanter, cevaplar loga) kanıt modları.
- [x] AJAN DOĞRULAMASI: DM kehaneti GERÇEK Qwen çıktısı ("The frost grows thick, the harvest
  weaves in dread..."); Sage Nera selamlama+konu+serbest soruyu karakter içinde yanıtladı;
  envanter okundu (1/1 ash_training_blade). LLM zinciri uçtan uca canlı.

>>> V2.2 OYNANABILIRLIK (2026-07-22) — TAMAM: yaşayan dünya artık OYUNCUNUN dünyasında <<<

## V2.2 — Üretim dünyası canlandırma (kapılar yeşildi ama oyuncu göremiyordu)
- [x] Her yerleşime ambar (150 buğday) — tek çapa ambarı üretim kasabalarından erişilemezdi,
  ~750 NPC hiç yemek yiyemiyordu; artık aktörler EN YAKIN ambara yürüyor (nearest-pile routing).
- [x] HydrateFactions law/craft/trade eksenlerini siliyordu → ilk 3 üretilmiş faksiyon etiketlenir;
  RoomSeed dünya seed'ini takip eder (kronik artık dünyaya özgü).
- [x] Dedikodular ham log yerine ANLATI cümleleri (NarrateEvent, 10 olay türü); HUD'a saat eklendi.
- [x] DERİNLİK-4 kaskad: tanık muhafıza KOŞUP İHBAR ediyor (reported_attack hafızası + olayı).
- [x] ProofLivingCensus + maraton LIVING satırı — canlı koşular üretim dünyasının yaşam kanıtını taşır.
- **KANIT:** fallback 1486/1490; Build Success; playthrough (forge açık VE kapalı) yaratılış→dünya
  Complete; 6 dk canlı maraton PASS: meals=13691 witnessed=13837 reported=8692 guardResponses=467
  chronicle=3 shortages=984 aliveActors=772, 0 istisna, bellek düz.

>>> V2.1 DF KALIBRASYONU (2026-07-22) — TAMAM <<<

## V2.1 — Dwarf Fortress kalibrasyonu + hafızanın dile bağlanması
- [x] **Köy kadrosu**: WorldFactory.SeedVillagers — kapı dünyası artık 12 sivil + 2 muhafız +
  2 avcı (biri kasabaya işe gelip giden GEZGİN avcı — predasyon meydanda, tanıklar gerçek).
- [x] **Eşikler**: Gate1 açlık<70 + öğün ≥ sivil×3 + stok MİKTARI akıyor (etiket sayısı değil);
  Gate3 ≥60 olay/gün (16 tür); Gate4 dalga ≥5; Gate5 derinlik-3 ZORUNLU (saldırı → ≥2 tanık →
  GuardResponded) — DoD: hepsi yeşil, fallback 1486/1490.
- [x] **Gate9 Hafıza dile ulaşıyor**: tanığın diyalog bağlamı (NpcMemoryLlmEnvelope.RecallLines —
  canlı diyalog yolunun ÇAĞIRDIĞI fonksiyon) witnessed_attack kaydını içermek zorunda.
- [x] **Canlı LLM diyalog artık hafızalı**: 4 prompt inşa noktası (selamlama/konu × npc/ad-hoc)
  RecallLines'tan son 8 anıyı RecentTurns olarak taşıyor + sistem promptu anıları örmesini
  söylüyor; SelectTopic/AskFreeText konuşmayı NpcMemory'ye GERİ yazıyor (MarkDialogueSeen +
  player_asked InteractionEvent) — konuşmalar artık deneyim, sonraki cevap onları hatırlıyor.
- DÜRÜST TESPİT: Ask DM gerçek (d100 kader kovası + yerel Qwen LLM); NPC diyalog gerçek iki
  katman; PARTİ/YOLDAŞ SİSTEMİ YOK (recruit/follow/companion hiç yazılmamış — sadece işleyicisiz
  3 tool tanımı). Yoldaş sistemi ayrı bir faz gerektirir (V3 adayı).
- Sıradaki ufuk (kullanıcı onayladı, sıra: DF derinliği ÖNCE): kapı eşiklerini daha da yükseltme
  turları; SONRA bina geometrisi/yapı çeşitliliği + billboard yürüme animasyonu.

Ritüel V1 ile aynı (fallback→build→proof→commit), tek fark: kanıt = kapı testleri + log akışı;
kare destekleyici, asla tek başına yeterli değil. Token-tasarruf modu: kısa rapor, toplu işlem.
