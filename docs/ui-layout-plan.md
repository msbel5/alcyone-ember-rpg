# Alcyone-Ember — UI Layout Plan (HTML-page discipline)

> Her ekran bir web sayfası gibi düşünülür: net katmanlar (backdrop / overlay / content),
> flex hiyerarşi, sabit genişlikler, gruplama + başlık, seçim geri bildirimi.
> Hiçbir element başka birinin üstüne **çakışmaz** — her şeyin belirli bir flex slotu vardır.
> UI Toolkit = UXML(HTML) + USS(CSS). Bu plan doğrudan USS layout'a çevrilir.
>
> Bu doküman Claude Design'a / referans olarak verilebilir. Backend'e dokunulmaz.

## Ortak kurallar (tüm ekranlar)
- **3 katman**: `backdrop` (absolute, z0) → `overlay` (absolute %40-50 siyah, z1, okunabilirlik) → `content` (flex, z2).
- **content** her zaman `flexGrow:1` + `alignItems:center` + `justifyContent:center` (veya space-between).
- **Gruplar başlıklı**: bir liste varsa üstünde section header (örn. "SINIF", "AHLAK").
- **Seçim geri bildirimi**: seçili öğe farklı renk + `[X]`; seçilmemiş `[ ]`. Asla sessiz kalmaz.
- **Sabit genişlik bloklar**: buton kolonu 380px, panel max 720px — ekrana yayılıp dağılmaz.
- **Tek aktif panel**: aynı anda yalnızca bir panel mount; geçişte öncekini `Unmount` + `Dismiss`.
- **Tipografi**: başlık 48-52, section header 20 bold, gövde 14-16, dipnot 12 dim.

---

## 1. Boot / Loading
```
┌──────────────────────────────────────────────┐
│ [splash_background full-bleed]  [overlay %50] │
│                                                │
│              EMBER CRPG  (logo)                │
│              ─────────────                     │
│              ▓▓▓▓▓▓▓░░░░  %62                  │  ← progress bar (orta, 480px)
│              "Entering area…"                  │  ← tek aktif durum satırı
│                                                │
│   ┌────────────────────────────────────────┐ │
│   │ [log] son 3 satır, küçük, dim, scroll   │ │  ← alt, sabit yükseklik 96px
│   └────────────────────────────────────────┘ │
└──────────────────────────────────────────────┘
```
- Kaldır: çift "Ember Boot" / "Entering area" (zaten yapıldı).
- thumbnail küçük preview YOK — sadece progress + tek durum + log.

## 2. MainMenu ✅ (kodlandı — 29a90c71)
```
┌──────────────────────────────────────────────┐
│ [splash] [overlay %50]                         │
│              EMBER CRPG (52px)                 │
│           A dark fantasy chronicle             │
│              ┌──────────────┐                  │
│              │  New Game     │ (380px kolon)   │
│              │  Resume       │                  │
│              │  Load Game    │                  │
│              │  Options      │                  │
│              │  Exit         │                  │
│              └──────────────┘                  │
│  v0.x·PR#214                                   │
└──────────────────────────────────────────────┘
```

## 3. CharacterCreation — ORTAK ÇERÇEVE (tüm 6 adım)
```
┌──────────────────────────────────────────────┐
│ COMMANDER CREATION          Step 4 / 6  ▓▓▓▓░░ │  ← üst bar: başlık + adım + progress
├───────────────────────┬──────────────────────┤
│  SOL: adım içeriği     │  SAĞ: özet panel      │
│  (adıma göre değişir)  │  - İsim               │
│                        │  - Sınıf / Ahlak      │
│                        │  - Stat blok          │
│                        │  - Portre (varsa)     │
├───────────────────────┴──────────────────────┤
│            [Back]            [Continue ▶]      │  ← alt bar, sabit. Continue kilitliyken dim + sebep
└──────────────────────────────────────────────┘
```
**Kilit kuralı**: Continue kilitliyse altında kırmızı küçük metin: "Eksik: ahlak seçimi".

### Step 4 — Class / Alignment / Skills (asıl sorun olan ekran)
Düz buton duvarı YERİNE 3 sütun, başlıklı, seçim vurgulu:
```
┌─ SINIF ─────────┬─ AHLAK ──────────┬─ YETENEK (5 seç) ─┐
│ [X] Diplomat ◄  │ [X] True Neutral │ [X] arcana        │
│ [ ] Warrior     │ [ ] Lawful Good  │ [X] history       │
│ [ ] Mage        │ [ ] Neutral Good │ [ ] athletics     │
│ [ ] Rogue       │ [ ] ...          │ [ ] deception     │
│ [ ] Scholar     │                  │ ... (scroll)      │
│ [ ] Wanderer    │                  │  3/5 seçildi      │
└─────────────────┴──────────────────┴───────────────────┘
```
- Seçili = dolu renk + `[X]` + sol kenar vurgu. Class seçince ahlak+skill default (zaten kodda).
- Skill başlığında sayaç "3/5".

## 4. Worldgen View
```
┌──────────────────────────────────────────────┐
│ WORLD GENERATION       Seed 42 · Pop 1.000.000 │
├───────────────────────┬──────────────────────┤
│  [log akışı, scroll]   │  [harita taslağı /    │
│  region/settlement/npc │   üretilen önizleme]  │
│                        │                       │
├───────────────────────┴──────────────────────┤
│  SORU MODALI (varsa): "Nerede başlamalı?"      │
│   [ capital gates ] [ trade road ] [ frontier ]│  ← yatay seçenek butonları
│                            [Continue ▶]        │
└──────────────────────────────────────────────┘
```
- "Auto-advance" + "Continue" çakışması düzelt: tek "Continue", auto-advance arka planda timer.
- Worldgen bitince → otomatik SmithingOverworld'e geçiş (LoadingScreen.Dismiss ile, zaten kodda).

## 5. In-game HUD (Worldspace — Daggerfall tarzı)
```
┌──────────────────────────────────────────────┐
│ Tick · Day · Region/Mood          [☰ menu]    │  ← üst bar dim
│                                                │
│        [3D Billboard Worldspace render]        │  ← merkez, dokunulmaz
│                                                │
│ ┌─────┐                            ┌─────────┐ │
│ │ HP  │                            │ Job/Log │ │  ← sol-alt stat, sağ-alt log
│ │ MP  │  [hotbar: 1-5 spell/item] │         │ │
│ └─────┘                            └─────────┘ │
└──────────────────────────────────────────────┘
```
- Alt paneller yarı-saydam, köşelere sabit. Merkez render hep açık.
- "Ask About" → NPC tıkla → dialog paneli (canlı LLM).

## 6. Dialog Panel (LLM)
```
┌────────────────────────────────┐
│ [NPC portre]  NPC Adı           │
│ ──────────────────────────────  │
│ "LLM yanıtı buraya akar…"       │  ← scroll, typewriter
│ ──────────────────────────────  │
│ [Ask About ▾]  [Trade] [Leave]  │  ← topic dropdown + aksiyonlar
└────────────────────────────────┘
```

---

## Uygulama sırası (öneri)
1. ✅ MainMenu (kodlandı)
2. CharCreation ortak çerçeve + Step 4 üç-sütun (en kritik — giriş tıkanıklığı)
3. Worldgen view (Auto-advance/Continue çakışması + modal)
4. Loading polish (zaten yarı yapıldı)
5. HUD + Dialog (Worldspace içi)

Her adım: USS layout → dotnet test → rebuild → **gerçek manuel playtest** (proof-driver değil) → commit.
