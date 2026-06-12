# EMBER v1.0 — Sürüm Notları

Deterministik, tamamen runtime-prosedürel bir Daggerfall-esinli CRPG: 3D dünya + 2D billboard
sprite'lar, Unity 6 (URP), her iddia kanıt arkasında. Bu dosya v0.2'den v1.0'a giden sürümlerin
özetidir; her sürümün tam kanıt zinciri `docs/ROADMAP_V1.md` KANIT bloklarındadır.

## Sürüm geçmişi

| Sürüm | Tag | Öne çıkanlar |
|---|---|---|
| v0.2 "Living World" | v0.2.0-living-world | Gece sokağa çıkma yasağı, hasat zinciri, DOOM kanal havuzu, savaş müziği geçişi |
| v0.3 "Find, Fight, Hear" | v0.3.0 | Keşfedilebilirlik (delve pusulası), gerçek-zaman savaş, prosedürel ses v2 |
| v0.4 "Combat Depth" | — | Ekipman zarları, ölüm/respawn bedeli, XP-kapılı seviye |
| v0.5 "Zindan Çağı" | v0.5.0-dungeon-age | Çok-odalı delve grafı, şef + hazine, tuzak/anahtar/kapı, 3 zindan arketipi |
| v0.6 "Görev Makinesi" | v0.6.0-quest-machine | Üretilmiş görevler + journal, görev kalıcılığı, itibar/kelle sistemi |
| v0.7 "Yaşayan Evren" | v0.7.0-living-universe | Prosedürel gökyüzü (güneş/ay/yıldız), deterministik hava, işlevsel iç mekânlar, öğlen taverna koşusu |
| v0.8 "Büyü + Bestiary" | v0.8.0-spell-and-beast | 8 büyülük okul (renkli bolt'lar, ward, fener, recall), 6 tipli bestiary (arketipe dağılım, tipli vuruş sesleri), ses v3 (biyom katmanı, rain hush, şef perküsyonu) |
| v0.9 "Cila + İskelet Hikâye" | v0.9.0-polish-and-spine | 3 perdelik ana görev (yazıt→bilge→final Warden) + finale/krediler, ayarlar+keybind ekranları, sıfır ölü buton, URP post-FX + kıvılcım + yürüyüş animasyonu, 30dk stabilite maratonu + 5dk autosave |
| **v1.0 "EMBER"** | v1.0.0 (kullanıcı onayı sonrası) | Yayın kapısı: playthrough kare dizisi, oyun kılavuzu, bilinen sınırlar |

## v1.0 kanıt zinciri (makine tarafı)

- **Playthrough:** `--ember-mainquest` — yaratılış → 3 perde → finale tek koşuda; kare dizisi
  pt_01..pt_05 (dünya/intro, zindanlar, başkent bilgesi, final zindanı, finale+krediler).
- **Shipcheck:** 9/9 PASS, 0 exception, perf bütçe içinde (avg ~12ms / 16ms).
- **Marathon:** 30dk soak — 406 aksiyon, 0 exception, bellek düz (255→295MB).
- **Goldenler:** fallback harness 1478/1478.

## Bilinen sınırlar (dürüst liste)

1. **Forge-OFF görseller:** bestiary silüetleri bloklu-piksel; NPC sprite'ları kütüphaneden —
   SDXL forge-ON koşusu gerçek sprite'ları üretir, kanıtlar forge-OFF alınmıştır.
2. **Ses:** kulak onayı metriklerle değil kullanıcıyla tamamlanır; piper TTS kurulu değil
   (selamlamalar metin).
3. **Ana görev:** intro journal metnidir (ayrı intro ekranı yok); bilge danışması adapter
   yoludur (canlı E-tuşu sage diyaloğu yok); finale overlay'inde yeniden-başlat akışı yok.
4. **UI:** keybind listesi salt-okunur (rebinding yok); çözünürlük döngüleyici (dropdown değil).
5. **Dünya:** host NPC'leri MoveTo ile oturtulur (takvim saatler içinde dışarı yürütebilir);
   sandık-açık durumu save'e yazılmaz; kelle ödeme/teslim akışı yok.
6. **Animasyon:** billboard yürüyüşü mirror-frame'dir (gerçek 2. kare sprite forge işidir);
   still kare yürüyüşü kanıtlayamaz — hareket kanıtı oyuncunun gözüne kalır.
7. **Tek oyunculu, tek dünya-seed akışı:** "seed paylaş" altyapısı var, UI'ı yok.

## Çalıştırma

- Oyun: `Builds/Windows64/alcyone-ember-rpg.exe`
- Kanıt modları: `--ember-lookaround | --ember-looptest | --ember-shipcheck | --ember-igtour |
  --ember-mainquest | --ember-marathon [--ember-marathon-minutes N]`
  (hepsi `--ember-proof-screenshots <dir> --ember-forge-off` ile)
