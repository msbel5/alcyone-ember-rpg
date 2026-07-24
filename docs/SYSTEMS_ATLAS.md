# SYSTEMS ATLAS — tüm sistem, yüksek + alçak seviye (2026-07-24)

Kaynak: 7 Opus ajanı, 975k token derin araştırma — 4 referans reponun TÜM sistemleri +
bizim TÜM sistemlerimiz + hata-ailesi arkeolojisi (git log + kod içi fix yorumları).
Ham envanter (119 sistem, veri modeli + akış + dosya işaretçileriyle):
`D:/proofs/systems-atlas-raw.md`. Bu dosya sentezdir: **neden hep aynı sorunlar** sorusunun
yapısal cevabı ve reform planı.

---

## 1. BİZİM MİMARİ — gerçek hali

**Sağlam olan çekirdek (bunu koruyoruz):**
- Katmanlar asmdef'le zorlanmış: `Domain` (saf veri; Unity/IO/RNG yok) → `Simulation`
  (deterministik sistemler) → `Data` (save DTO mapper'ları) → `Presentation` (Unity).
- Determinizm anayasası: aynı seed + aynı tick dizisi → bayt-aynı WorldState + olay logu.
  `WorldTickComposer` üç kadans bandı (PerTick/Hourly/Daily), `MinutesPerTick` tek gerçek
  kaynak, catch-up SINIR damgalı (1 günlük ile 30 günlük ilerleme aynı logu üretir),
  RNG = sınır-damgası ⊕ aktör-id'leri.
- Adapter kontratı: `IDomainSimulationAdapter` 6 rol arayüzüne bölünmüş; 24 partial tek
  implementasyon; Presentation'a yalnız düz DTO'lar çıkar (Domain tipi sızmaz).
- Kanonik döngü: EmberTickDriver (0.83s/tick) → adapter.AdvanceTick → composer.Advance →
  mirror publish → WorldViewProjector → ActorView.SetTarget.

**Sistem envanteri (özet; tam liste ham dökümde):** zaman/takvim, ihtiyaçlar+tüketim
(+P0 varışta-yemek), takvim/işler/patika, kaskadlar (avcılık/tanıklık/+P0 takip),
ekonomi (stok/fiyat/ticaret/kıtlık/kervan), bitki/hasat(+el-şartı), tarih/kronik,
görevler, büyü, save dilimleri; sunum: dünya gerçekleştirme (director+builder'lar),
aktör görünümleri (spawner/sync/etiket/ikon/curfew), UI ekranları, ses forge'u, konuşma
(piper/SAPI), girdi.

## 2. KONTRATIN BEŞ KAÇIŞ KAPISI (sunum ajanının tam haritası)

1. **Statik mirror kanalları (~10 global):** RuntimeFieldMirror, WeatherMirror,
   CombatFeedbackFeed, EventEchoFeed, InteriorInfo... Adapter yazar, view'lar arayüzsüz
   yoklar. Varlık nedeni meşru (director adapter'ın ilk tick'inden ÖNCE koşar) ama her
   kanal elle tutulan defterdir.
2. **Sim'in hiç vermediği dekoratif koordinatlar:** plaza mobilyası, tarla kuşağı
   (seed%360 polar!), maden açısı, banner konumu; plot sayısı sim nüfusundan değil
   SABİT tablodan; tavern/temple/shop = "listedeki ilk üç bina".
3. **UnityEngine.Random ile üretilen saçılım/kozmetik hareket:** doğuş halkaları,
   idle salınım — Domain'den doğru şekilde yalıtılmış ama amaçlı yürüyüşü boyayan gürültü.
4. **Saat-tahmini vs takvim-gerçeği gerilimi:** etiketler sim fiilini okur (doğru);
   poz ikonları saati TAHMİN eder; DescribeActivity pencereleri "ScheduleSystem ile
   eşleşMEK ZORUNDA yoksa kelime yalan söyler" diye uyarır.
5. **Hava tamamen sunum kurgusu** + rol-string ayrıştırma ("outlaw" içeriyorsa düşman) —
   anlamsal veri kaynak-id string'inde kaçak taşınıyor.

## 3. HATA AİLELERİ — ve tek cümlelik kök teşhis

Arkeoloji (fix-yorumu + commit taraması) yedi aile çıkardı:
- (a) tek-nokta tasarımlar (tek yemek noktası, tek global stage);
- (b) sim↔görsel koordinat ayrılığı (tarla, plaza mobilyası);
- (c) kadans yazar çatışmaları (takip 1/saat vs posta-dönüş 1/tick);
- (d) gömülü/örtülen geometri aritmetiği (duvar-içi pencere ×3, kanat-içe-sızma);
- (e) API yanlış kullanımı (legacy Input 17k exception, COM);
- (f) akışlarda offset/durum-sıfırlama hataları (TTS ilk-cümle, dört dosyada
  kopyalanan istek-serisi deyimi);
- (g) diğer (unchecked sayaçlar, % n önyargısı).

**Kök teşhis (ajanın cümlesi, aynen):** *"Kod tabanında, yaşayan-dünya oyununun en çok
mutasyona uğrattığı iki şey için — bir şeyin NEREDE olduğu ve durumun NE ZAMAN yazıldığı —
tek bir otorite yok; her async/akış dikişi aynı kırılgan defteri elle yeniden tutuyor.
Düzeltmeler semptom-başına yapıldı (offset büyüt, seri ekle), yapısal üreteç kaldırılmadı —
her aile bir sonraki playtest açısında geri geldi."*

## 4. DÖRT REFERANSIN FELSEFELERİ

**Daggerfall Unity:** "Deterministik olan her şeyi YENİDEN ÜRET, yalnız deltaları sakla,
tek tam-sayı saat her simülasyonu sürsün." Üç katman: DaggerfallConnect (Unity'siz saf
veri okuyucular + TEK otoriter koordinat matematiği), Workshop (motor/içerik; ITerrainSampler
gibi takas-edilebilir dikişler), Game (oturum). classicUpdate bayrağı (16Hz) AI'ı kare
hızından ayırır; floating-origin map-pixel sınırında sıfırlanır → float hassasiyeti VE
save hizası aynı anda çözülür. Bina içi = model kayıtları + AYRI kapı-kayıt listesi.

**GemRB:** üretici değil YORUMLAYICI — otorlanmış içerik + kural VM'i. Diyalog = DLG durum
makinesi: state → transition'lar; seçilen tüketilir; trigger'lar koşullar; revisit kaldığı
state'ten. (Bizim LLM'li balon-diyaloğun deterministik iskeleti budur.)

**Dwarf Fortress:** HER-ŞEY-MATERYALDİR — kaya, kan, bira, çelik aynı ~40 alanlı struct;
TEK fizik/sıcaklık çözücüsü hepsine bakar; yenilik yeni kod yolu değil yeni katsayı satırı.
Sistemler birbirini kod üzerinden değil PAYLAŞILAN SEMBOL UZAYINDAN çağırır (token'lar).
Emergence'ın robustluğu az-özel-durumdan gelir. (Kedinin handa ölmesi: özel senaryo değil,
aynı çözücülerin kesişimi.)

**Mountaincore:** boot'ta dondurulan JSON sözlük katmanı (~70 dictionary) + TÜM oyun
durumu tek GameContext bean'inde + durumsuz render. Mod'lanabilirlik veri-önceliğinden
bedava geliyor.

**Ortak ders:** dördü de (1) veriyi koddan, (2) sim durumunu sunumdan, (3) koordinat/zaman
otoritesini TEK yerden yönetiyor. Bizim çekirdek anayasa aynı ruhta — kaçaklar sunum
katmanında birikti.

## 5. TOP-3 YAPISAL REFORM (hata ailelerini kökten öldüren)

1. **TEK MEKÂNSAL OTORİTE** *(b+d ailelerini ve a'nın yığılma yarısını öldürür)* —
   kural: görsel transform = sim pozisyonunun saf projeksiyonu (`BillboardOrigin(simCell)`).
   Paralel polar/halka dekoratif geometri silinir. CI'a iki invariant: (i) render edilen her
   aktör/bitki/işaretin XZ'i sim projeksiyonuna ε içinde eşit; (ii) kapsama/örtülme
   assert'i (duvar içindeki cam, su altındaki beden CI'da düşer, oyuncuda değil).
   Yerleştirme yardımcıları yüzey+normal alır, tahmini literal offset asla.
2. **ALAN-BAŞINA TEK YAZAR, İLAN EDİLMİŞ KADANSTA** *(c ailesini öldürür)* — tick
   registry'si sahiplik kaydına genişler: her mutable alanın (pozisyon, hedef, ihtiyaç)
   TEK yazar sistemi TEK kadansta; ikinci yazar derleme/test hatası. (Takip-vs-posta
   çatışması bu kuralda derlenmezdi bile.) Sınır-damgalı catch-up zorunlu; seed-replay
   gate'i alan-yazımı başına genişler.
3. **TİPLİ DİKİŞ PRİMİTİFLERİ + HER AKIŞA/MAPPER'A GOLDEN** *(f, g'nin çoğu, e'ye çit)* —
   dört elle-yazılmış diyalog serisi tek "latest-wins stream" primitifine iner (seri +
   dedupe + flush-sınırı); HER save-mapper alanına golden round-trip (düşen Home/DayAnchor
   CI'da yakalanırdı); tek önyargısız RNG cephesi; legacy Input/fake-null için
   banned-symbols analizörü.

## 6. P0→P2 DURUM + reform bağları

| İş | Durum | Reform |
|---|---|---|
| P0 Guard pursuit (PursuitRecord, PerTick) | ✅ `15b35398` | #2'nin ilk uygulaması |
| P0 Varışta-yemek + kozmetik kısma | ✅ `a56bdb82` | a-ailesi söküm |
| P0 Tarla sim↔görsel birleşmesi (SimFieldView) | ✅ `0e44f00a` | #1'in ilk uygulaması |
| P0 DialogStateMachine v1 (tüketilen balon + FOLLOWUPS + resume) | ✅ `966d184c` | GemRB iskeleti |
| P1 İç bölme + kapılar (WallWithGap portu) | ✅ `79d9eaca` | #1 invariant (ii) yolunda |
| P1 Ambient-yaşam (fare kileri GERÇEKTEN soyar, kedi avlar) | ✅ `df5f4d7a` | DF az-özel-durum dersi |
| P1 RumorMill + kalıcı "Any news?" | ✅ `0534be54` | olay-akışı okuyucusu |
| P2 Unrest defteri + kasaba SÜPÜRMESİ | ✅ W27 | DFU LegalRep-lite |
| Reform CI invariantları (mekânsal + tek-yazar + golden) | ✅ W29 `1fd3cbb9` | kadans testi İLK koşuda composer bug'ı yakaladı → tick-replay |
| W30 dört yara (delve içi, cross-site despawn, ring koltuk, sweep cooldown) | ✅ `8c16b572` | dördü de test-pinli |
| W30e TickPerf: EatOnArrival kuadratik öldürüldü (aktör×stok×site → tick-önbellek) | ✅ | canlı-ölçek perf pini (800 aç sivil < 3sn/gün) |
| Mekânsal invariant CANLI av #2: 3 NPC sim'i bina kabuğu İÇİNDEN yürüyor (AccessibilityGuard dışarı itiyor) | 🔴 açık borç | kök çözüm = sim bloke-hücre haritası (P2 devamı) |
| P2 devamı: LOS konisi, sahiplik/hırsızlık, sim bloke-hücre haritası | ⏳ | DFU Crimes tam modeli |

Ölçülen yan-kanıt: fare hırsızlığı testte kıtlık→ticaret→fraksiyon-itibar zincirini
kendiliğinden tetikledi — sistemler gerçekten BAĞLI.

Bu dosya her teslimatta güncellenir; ham envanter dondurulmuş referanstır.
