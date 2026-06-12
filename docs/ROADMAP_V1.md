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
- [ ] **F16 Ekipman etkisi**: envanterdeki silah/zırh statları vuruş zarına gerçekten girer
  (silah=baseDamage+acc bonus, zırh=armor; ekipman slotu: el+gövde). Başlangıç kiti + zindan
  sandığından 1 tier üstü silah düşer (chest loot tablosu).
  **DoD:** EditMode testi (silahlı vs silahsız TTK farkı) + sandıktan alınan silahla swing log farkı.
- [ ] **F17 XP/Seviye**: kill+quest XP → seviye → +acc/+dodge/+HP seçim ekranı (3 kart). Level
  HUD'da zaten var; gerçek beslensin.
  **DoD:** encounter sonrası XP satırı + level-up modal karesi + stat artışı save'de kalıcı.

## v0.5 "ZİNDAN ÇAĞI" — tek oda değil, gerçek delve

- [ ] **F18 Çok-odalı prosedürel zindan**: barrow → 4-7 oda + koridor grafı (deterministik seed;
  mevcut MultiRoomDungeonGenerator domain'i realize'a bağlanır). Oda başına 0-2 haunter, son odada
  şef (2× HP, 1.5× dmg) + büyük sandık.
  **DoD:** topdown karesi oda grafını gösterir; looptest şefe kadar iner, loot satırı.
- [ ] **F19 Zindan çeşitliliği**: 3 arketip (mağara/kripta/harabe — malzeme paleti + ışık rengi +
  müzik varyantı farklı). Arketip = settlement seed'den deterministik.
  **DoD:** 3 farklı zindana travel + her birinden iç kare; arketip adı realize logunda.
- [ ] **F20 Tuzak + kilit**: ezici plaka tuzağı (görünür, 8 dmg, sesli) + kilitli şef kapısı
  (anahtar rastgele ara odada). Kapı/anahtar HUD event satırına yazar.
  **DoD:** looptest anahtar→kapı→şef akış satırları + tuzak hasar logu.

## v0.6 "GÖREV MAKİNESİ" — DFU tarzı sonsuz iş

- [ ] **F21 Görev üreticisi**: 4 şablon (getir/öldür/teslim et/ziyaret) × hedef (zindan/yerleşim/NPC)
  × süre limiti; veren NPC rolüne göre (lonca yok, kişiler var). Journal'a gerçek kayıt.
  **DoD:** EditMode: 20 seed → 20 geçerli görev; looptest bir fetch görevini uçtan uca kapatır.
- [ ] **F22 Görev kalıcılığı**: dünya görevleri (bounty/pilgrimage dahil) save/load'da korunur
  (WorldSaveMapper genişler — adapter-local dictionary kalkar).
  **DoD:** save→load→journal aynı; digest roundtrip testi genişletilmiş haliyle yeşil.
- [ ] **F23 İtibar + suç**: sivile vurmak = suç → muhafız saldırır + kelle parası; görev tamamlama
  +itibar, itibar yüksekse fiyatlar %10 iner (ekonomi köprüsü hazır).
  **DoD:** sivile vur→muhafız agro karesi; itibar satırı HUD/character ekranında.

## v0.7 "YAŞAYAN EVREN" — gökyüzü, hava, iç mekânlar

- [ ] **F24 Gökyüzü v2**: prosedürel gün döngüsü (güneş pozisyonu saatten, şafak/alacakaranlık
  gradyanı, gece yıldız + ay). Clock-jump sonrası parlak gök bug'ı kökten ölür.
  **DoD:** 06/12/18/24 saatlerinde 4 kare — dördü görsel olarak FARKLI ve doğru.
- [ ] **F25 Hava durumu**: yağmur/sis/kar (biyom+mevsim deterministik); yağmur partikül + ses
  (PhISM hazır), sis fog yoğunluğu, kar terrain tint. Müzik yağmurda yumuşak varyanta düşer.
  **DoD:** 3 hava durumu karesi + "[Weather]" log satırları + yağmur sesi forge metriği.
- [ ] **F26 İşlevsel iç mekânlar**: taverna (uyu→saat atla+HP yenile, 5 altın), tapınak (şifa),
  dükkân (trade ekranı tezgâhtan açılır). Kapıdan girince interior realize (mevcut shell furnish
  genişler), NPC içeride masada.
  **DoD:** taverna uyuma akışı looptest'te; 3 iç mekân karesi.
- [ ] **F27 NPC ihtiyaç koşuları**: işe gidiş/eve dönüş zaten var; öğlen taverna yemeği + work-pose
  ikonu (çekiç/çapa billboard üstü mini quad) eklenir.
  **DoD:** öğlen tavernada ≥2 NPC karesi; pose ikonları karede.

## v0.8 "BÜYÜ + BESTIARY" — içerik genişlemesi

- [ ] **F28 Büyü okulu**: 8 büyü (3 hasar tipi + kalkan + şifa + ışık + hız + teleport-to-entrance);
  büyü kitabı ekranından slot atama; mana ekonomisi dengelenir.
  **DoD:** EditMode etki testleri + 3 büyünün dünya VFX karesi (bolt sistemi genişler).
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

>>> CURRENT: F16 <<<
