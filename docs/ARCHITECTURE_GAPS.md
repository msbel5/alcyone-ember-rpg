# ARCHITECTURE GAPS — "neden bu haldeyiz" dosyası (2026-07-24)

Kaynaklar: canlı playtest raporları + 5 ajanlık referans madenciliği
(Daggerfall-Unity, GemRB, Dwarf Fortress legacy, Mountaincore) + kendi kodumuzun
satır-satır otopsisi. Her bölüm: **şu an ne oluyor (kanıtlı) → referans ne yapıyor →
ne inşa edeceğiz**.

---

## 1. Meydan yığılması — "hepsi üzerime geldi"

**Şu an (otopsi):** Evler ve gündüz-çapaları GERÇEK ve dağınık (hash-spread,
`Worldgen.Npcs.cs:72-99`); takvim her tick 1 hücre yürütüyor. Yığılmayı üç mekanizma
üretiyor: (a) açlık ≥55 olunca eat skoru işi/boşluğu HER beraberlikte yener ve herkesi
TEK yemek noktasına (site MERKEZ hücresi = plaza) yollar; (b) yürüyüş PerTick ama yemek
HOURLY çözülür — masaya saniyeler içinde varıp bir oyun saati AYAKTA beklerler;
(c) görsel katman billboard'ları OYUNCU merkezli halkalarda doğurur ve ±2.2m kozmetik
salınım ekler — amaçlı yürüyüş bile başıboş görünür. Oyuncu da plazada doğar: kalabalık
matematiksel olarak oyuncunun üstüne çöker.

**Referans (DF/Mountaincore):** yemek yerleri ÇOĞULdur (tavernalar, kilerler,
stok-yığınları); ajanlar en yakın uygun İSTASYONA gider; bekleme değil rezervasyon vardır.

**İnşa planı (P0):**
- FoodSpots: site merkezi yerine STALL/TAVERN worksite hücreleri (çoğul; en yakını seç).
- TryEat'i varışta çözülür yap (adjacency + PerTick), Hourly adım yalnız metabolizma.
- Spawn halkasını oyuncudan değil SİM pozisyonundan başlat; kozmetik salınımı 0.8m'e indir.
- Gate etkisi: Gate1/meals sayaçları korunmalı — eşik/etki testleri güncellenerek pinlenir.

## 2. Muhafız takibi görünmüyor

**Şu an (otopsi):** PredationSystem muhafızı YÜRÜTMEZ (yalnız 2 hücrede vurur);
WitnessResponse takibi SAATTE 1 hücre; ScheduleSystem ise HER TICK muhafızı karakoluna
çeker (~60× hızlı) — takip aritmetik olarak silinir. Kod düşmanlar için bu bug sınıfını
belgeleyip muaf tutmuş (`ScheduleSystem.cs:57-61`), muhafızları unutmuş. Oyuncu-saldırısı
takibi yalnız bounty yolunda.

**Referans (DFU Crimes):** suç → tanık LOS konisi (77.5m/95°) → muhafız-tanık ANINDA
müdahale, sivil tanık 5-10 sn'de eskalasyon; bölgesel LegalRep −10 altında devriye doğurur.

**İnşa planı (P0):** ActorScheduleState'e `PursuitTargetId + PursuitUntil` (save'e girer);
WitnessResponse rapor anında doldurur; ScheduleSystem guard branch'i pursuit doluyken
hedefi SALDIRGANIN GÜNCEL hücresi yapar (PerTick — rubber-band biter), süre dolunca posta
döner. TDD: "rapor edilen saldırgan 12 hücre öteden 12 tick'te yakalanır".

## 3. Diyalog statik — "5-6 aynı soru, hiçbir şey değişmiyor, kaldığı yerden sürmüyor"

**Şu an:** Sabit topic listesi; cevap listeyi DEĞİŞTİRMİYOR; farewell → içerik sıfır
(hafıza var ama AKIŞ yok); menü etiketi TTS'e "gate" diye okunuyordu (W17'de düzeldi).

**Referans — iki sistemin evliliği:**
- **GemRB DLG durum makinesi:** dialog = STATE'ler; her state metin + TRANSITION listesi;
  seçilen transition TÜKETİLİR, yeni state yeni seçenekler getirir; trigger'lar seçenekleri
  koşullar (tanışıklık, quest bayrağı); revisit son state'ten sürer. Kullanıcının "balon
  patlar, yerine bağlama uygun yenisi gelir" tarifi birebir DLG modelidir.
- **DFU TalkManager:** topic HAVUZU dinamik kurulur (yerel binalar, quest kaynakları,
  dedikodu değirmeni); bilgi/tepki zarları deterministik (hash(npc)^hash(topic)); "aynı
  binadaysa kesin bilir / bölge dışıysa bilmez" kuralları bilgiyi bedavaya mekânsal yapar.

**İnşa planı (P0 — DialogStateMachine v1):**
- `ConversationState { npcId, stateId, List<DialogOption> options, history }` — save'e girer;
  farewell state'i SAKLAR, yeniden açılış "kaldığımız yerde…" girişiyle son state'ten sürer.
- Seçenek tüketilir; cevap geldiğinde LLM'den AYNI istekte 2-3 takip sorusu istenir
  (yapılandırılmış kuyruk: `FOLLOWUPS: q1|q2|q3`), tükettiklerinin yerine geçer.
- NPC bize soru sorarsa options'a CEVAP şıkları gelir (LLM üretimi + serbest yazı).
- Topic havuzu DFU usulü mekânsal: yerel binalar/işler/aktif quest kaynakları + rumor mill.
- UI: balon ızgarası; tıklanan balon söner (animasyon), yenileri doğar. TTS doğal cümle
  (W17'deki NaturalQuestion) hem UI etiketi hem seslendirme olur.

## 4. Sim tarlası ↔ görsel tarla AYRI EVRENLERDE

**Şu an (otopsi):** Sim bitkileri `site.MinBound` grid hücresinde; görsel kuşak
seed-açılı POLAR süsleme (`GroundRadius+10`, nüfus tablosundan plot sayısı). Aralarında
yalnız {sayı, baskın-evre} skaleri akıyor — koordinat asla. "Tending the field" meydanda
yazıyor çünkü SİM tarlası oradaki hücrede.

**İnşa planı (P0):** Kuşak yerleşimi `PlantComponent.Position`'ın BillboardOrigin
projeksiyonundan üretilecek (bire bir stalk=plant); worksite hücreleri de aynı hücreler
olacak — hasatçı GERÇEKTEN görünür tarlada duracak. Mirror kanalına plot-başına evre
listesi eklenir (id → stage).

## 5. İç mekân: kapısız "ikinci oda" yanılsaması

**Şu an (otopsi):** Kasaba binası TEK odalı kabuk; yalnız giriş duvarı yarıklı. L-kanat
varyantı DOLU (collider'lı) bir küp — içeriden "girilemeyen bölme" diye okunuyor.
Gerçek çok-odalı örüntü repoda ZATEN var: `RuntimeDungeonBuilder` WallWithGap + kapı
kayıtları + koridorlar.

**Referans (DFU interiors):** bina içi tamamen VERİ: model kayıtları + ayrı KAPI kayıt
listesi (odalar arası menteşeli action-door) + amaç işaretleyicileri.

**İnşa planı (P1):** RuntimeBuildingBuilder'a dungeon'un WallWithGap dilini taşı:
büyük binalarda 1 iç bölme + kapı boşluğu; L-kanat içe sızmasın (offset 0.62→0.78,
tamamen dışarı) ve kanada kendi kapısı açılsın. (Hızlı yama bu commit'te: kanat dışarı.)

## 6. DF-derinliği: "kediler handa gezsin, fareler kileri bassın"

**Referans (DF legacy):** vermin = UCUZ ajanlar — tam patika/ihtiyaç yok; bölge-bağlı
doğma (kiler→fare, han→kedi), basit dürtü tablosu (yemeğe koş, kediden kaç), stok
ETKİSİ (fare kilerde N birim yer → shortage olayı → kedi varsa av). Sahiplik+hırsızlık
ve ihtiyaç-ziyaretleri (taverna) aynı ucuz ajan dilinde.

**İnşa planı (P1 — VerminSystem):** `VerminComponent {kind, siteId, cell, state}`;
Hourly tick: fare kiler stokundan çalar (gerçek stok düşer → mevcut shortage/price
zinciri bedava tepki verir), kedi fare avlar (event + census satırı LIVING'e girer);
görsel: 12px billboard'lar + event echo ikonları. Gate: "vermin olayları/gün ≥ N".

## 7. Rumor Mill (DFU) — "Any News?"

Tek global `List<RumorEntry>` {tip, bölge, fraksiyon, ömür, metin-varyantları};
üreticiler: dünya olayları (maul/hasat/kıtlık/kronik) + quest kancaları; tüketici:
diyalogda "Any news?" + meydan panosu. LIVING olay akışımız zaten var — değirmen
sadece OKUYUCU. (P1, DialogStateMachine'e takılır.)

## Sıralı yol haritası
- **P0:** DialogStateMachine v1 · guard pursuit · eat-spot çoğullaştırma + varışta yemek ·
  tarla sim↔görsel birleşmesi · spawn/kozmetik gürültü düşürme
- **P1:** iç bölme+kapılar · VerminSystem · RumorMill · bina içine amaç işaretleri
- **P2:** LOS-konili suç/tanıklık (DFU modeli) · LegalRep/devriye · sahiplik+hırsızlık

Her madde: TDD + gate (choreography yasağı sürüyor) + canlı kanıt (timelapse/probe) +
tek commit. Bu dosya her teslimatta güncellenir.
