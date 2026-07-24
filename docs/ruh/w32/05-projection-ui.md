# W32 / DOC 5 — Projeksiyon Dürüstlüğü: UI Tahmin Etmez, `CurrentAction` Okur

> RUH_TESHIS.md §2.9: *"Oyun önce gerçek fiili simüle edip görüntülemiyor; görüntü katmanı duruma
> bakıp fiil uyduruyor."* Bu doküman o cümleyi kapatır — **EAT dilimi kapsamında**.
> Kabul kuralı (RUH_TESHIS.md §10): **"Activity etiketi `CurrentAction` ile bire bir aynıdır."**

Kapsam: yalnızca EAT dilimi (`MoveToFood`, `ConsumeFood`). Aksiyon sınıf hiyerarşisi ve
fazlar bu serinin aksiyon-modeli dokümanlarında (DOC 1–3) tanımlanır; burada varsayılan kontrat:

```csharp
// Domain (DOC 1-3'ün malı — burada sadece OKUNUR):
actor.CurrentAction            // ActorAction taban sınıfı; None = null
actor.CurrentAction.Kind       // kararlı string kimlik: "MoveToFood" | "ConsumeFood"
actor.CurrentAction.Phase      // ActionPhase (Starting/Active/Completing...) — DOC 2 tanımlar
```

---

## 1. Bugünkü tahmin hattı (üç ayrı tahmin sitesi)

Tahmin **tek yerde değil, üç yerde** yapılıyor; üçü de saat/konum/rol ipucundan fiil uyduruyor:

| # | Site | Kanıt | Uydurduğu şey |
|---|------|-------|----------------|
| G1 | `DescribeActivity` | `DomainSimulationAdapter.WorldProjection.cs:110-143` | Saat 12–14 + plaza'ya Chebyshev ≤3 → `"eating"`; hunger ≥55 → `"to the tavern"`; olgun bitkiye ≤ReachCells → `"harvesting"` / `"tending the field"`; saat <6/≥22 → `"sleeping"`/`"heading home"`; ≥20 → `"winding down"`; rol → `"on watch"`/`"hunting"`; kalan → `IsIdle ? null : "working"` |
| G2 | `IsAsleepAtHome` | `DomainSimulationAdapter.WorldProjection.cs:98-106` | Saat 22–06 + eve Chebyshev ≤1 → `Sleeping=true` (yatma pozu). Domain'de `SleepAction` yok; poz saat+konumdan türetiliyor |
| G3 | `NpcPoseIconView.Update` | `NpcPoseIconView.cs:32-44` | Adapter'a bile bakmadan `RuntimeFieldMirror.HourOfDay` (`DomainSimulationAdapter.Clock.cs:131` yazar) üzerinden: 12–14 → MUG (herkes yemekte), 8–18 işçi → HAMMER |

G1'in kendisi zaten üç playtest yaması yemiş ("hepsinin ustunde eating yaziyor, kimse masaya
oturmuyor", `WorldProjection.cs:115-116`): tahmini tahminle inceltmişiz. Aksiyon geldiği anda
bu yamaların EAT dalları **silinir**, inceltilmez.

## 2. Tüketici haritası — uydurulan etiket nereye akıyor?

Tam zincir (her tüketici, dosya:satır):

```text
G1 DescribeActivity ┐
G2 IsAsleepAtHome   ┴→ ProjectActor (WorldProjection.cs:85-94)
                        → ActorViewState.Activity / .Sleeping (ActorView.cs:270-288)
                          → IDomainSimulationAdapter.TryReadActor(name|id)   (IDomainSimulationAdapter.cs:66,77)
                            ├─ DomainSimulationAdapter.TryReadActor          (WorldProjection.cs:23-47)
                            └─ UnavailableSimulationAdapter.TryReadActor→false (UnavailableSimulationAdapter.cs:46-47)  [fallback harness — etiket hiç doğmaz]
                          → WorldViewProjector.Project()                     (WorldViewProjector.cs:41-53, her tick)
                            → ActorView.SetTarget                            (ActorView.cs:124-136)
                              ├─ NpcActivityLabelView.SetActivity            (NpcActivityLabelView.cs:38-42)  → TextMesh — EKRANDAKİ kelime
                              └─ NightCurfewView.SetSleeping                 (NightCurfewView.cs:27-28,48)   → yatma pozu + collider off + ExternalPoseOverride (ActorView.cs:122)
                          → EmberProofScreenshotDriver.RunAgentCheck         (EmberProofScreenshotDriver.cs:766-785) — bugün sadece pozisyon driftini okuyor
G3 NpcPoseIconView (bağımsız saat tahmini; ActorViewState'i HİÇ okumaz)      (NpcPoseIconView.cs:32-44)
```

Bağlama noktaları (etiketin kimlere takıldığı — assertion kapsamı budur):

- `EmberGeneratedActorSpawner.cs:227-228` — **yalnızca hostil-olmayan üretilmiş NPC'lere**
  `NpcPoseIconView` + `NpcActivityLabelView` takılır. Hostiller ve authored sahne aktörlerinde
  etiket yoktur (`ActorView.cs:130` `GetComponent` null döner, `_activityLabel?.` sessiz geçer).
- `ActorView` yürüme/idle animasyonu (`ActorView.cs:212-245`) pozisyon deltasından beslenir,
  etiketi okumaz — bu tasarımın dışında.
- `NpcEventEchoView` (spawner :195) gerçek event'leri yüzdürür — zaten dürüst, dokunulmaz.
- HUD/InGameUi `Activity` okumuyor (repo taraması: tek tüketici `ActorView.cs:134`).

## 3. Tasarım — verbatim projeksiyon

### 3.1 Tek çeviri tablosu: `ActionVerbTable`

Domain İngilizce kelime taşımaz (kind taşır); kelime sunum sözlüğüdür. Tek tablo = tek gerçek
(SRP; yeni aksiyon türü = bir satır, Open/Closed). Presentation katmanına konur, Domain'e değil:

```csharp
// Assets/Scripts/Presentation/Ember/Adapters/ActionVerbTable.cs  (YENİ, ~20 satır)
// CONSTRAINT: pure static data — no clock, no position, no needs. A verb may ONLY be
// derived from CurrentAction.Kind. Adding an hour/position input here recreates RUH_TESHIS §2.9.
internal static class ActionVerbTable
{
    public static string Verb(string kind) => kind switch
    {
        "MoveToFood"  => "seeking food",
        "ConsumeFood" => "eating",
        // CONSTRAINT: unknown kind NEVER falls back to a guess — loud sentinel + one warn.
        _ => Unknown(kind)
    };
    private static readonly HashSet<string> _warned = new();      // presentation-only state
    private static string Unknown(string kind)
    {
        if (_warned.Add(kind))
            LogManager.Warn("projection", $"no verb for action kind '{kind}'"); // tek LogManager kuralı
        return "(" + kind + ")"; // ekranda görünür kalır ki eksik satır playtest'te yakalansın
    }
}
```

### 3.2 `DescribeActivity` yeni hâli — önce aksiyon, sonra (küçülmüş) takvim kelimesi

```csharp
// DomainSimulationAdapter.WorldProjection.cs — DescribeActivity gövdesi değişir:
// W32 DOC5: the verb is a PROJECTION of CurrentAction, never an inference from hour or
// position. Guess branches survive ONLY for actors that cannot carry an action yet
// (see §4) and each one is tagged with the slice that retires it.
private string DescribeActivity(ActorRecord actor)
{
    var action = actor.CurrentAction;                 // DOC 1: ActorRecord kalıcı aksiyon taşır
    if (action != null) return ActionVerbTable.Verb(action.Kind);  // VERBATIM — ipucu girdisi YOK
    return DescribeScheduleWord(actor);               // None → takvim kelimesi (§4)
}
```

Silinen dallar (EAT dilimiyle birlikte ölürler — inceltilmez, silinir):

- `WorldProjection.cs:117-124` — 12–14 plaza-mesafesi `"eating"` tahmini ve `"to the tavern"`
  hunger eşiği. Yemek fiili artık yalnızca `ConsumeFood` aksiyonundan doğar; tavern'e yürüyüş
  yalnızca `MoveToFood` aksiyonundan (`"seeking food"`).

`DescribeScheduleWord` = mevcut fonksiyonun kalan dalları, aynen (bkz. §4). Net satır sayısı
**azalır** (iki dal silinir, bir tablo dosyası gelir).

### 3.3 `ActorViewState` genişler: `ActionKind`

Görünüm ve harness "bu kelime aksiyondan mı, takvimden mi geldi?" ayrımını yapabilmeli
(poz ikonu ve assertion bunu ister). Struct'a tek alan eklenir; ctor parametresi opsiyonel
olduğundan mevcut çağrı yerleri ve `default(ActorViewState)` dönen fallback adapter
(`UnavailableSimulationAdapter.cs:46-47`) değişmeden derlenir — fallback harness yeşil kalır:

```csharp
// ActorView.cs:270-288 — ActorViewState'e eklenir:
/// <summary>W32 DOC5: stable CurrentAction kind ("MoveToFood"...), null when the actor
/// carries no action (schedule-word fallback). Views may branch on this; they may NOT
/// re-derive it from hour/position (that is the §2.9 disease).</summary>
public readonly string ActionKind;   // ctor: string actionKind = null
```

`ProjectActor` (`WorldProjection.cs:85-94`) doldurur: `actionKind: actor.CurrentAction?.Kind`.

### 3.4 Poz ikonu (G3): MUG saatten değil aksiyondan

`NpcPoseIconView` etiketle aynı besleme desenine geçer — `ActorView.SetTarget`
(`ActorView.cs:124-136`) `_poseIcon?.SetActionKind(state.ActionKind)` push'lar
(`_activityLabel` ile aynı `GetComponent` probe deseni, +3 satır):

- **MUG**: `ActionKind == "ConsumeFood"` iken görünür. Saat penceresi dalı (`NpcPoseIconView.cs:38,41`) silinir.
- **HAMMER**: işçi + 8–18 saat tahmini (`:39,42`) **kalır** — iş aksiyonu WORK diliminde gelecek
  (§4). Dal, `// GUESS(WORK slice): retire when PerformWorkAction lands` yorumunu alır.
- `RuntimeFieldMirror.HourOfDay` poll'u yalnızca hammer dalı için kalır.

### 3.5 Uyku pozu (G2): bu dilimde DOKUNULMAZ

`IsAsleepAtHome` (`WorldProjection.cs:98-106`) ve `NightCurfewView` zinciri aynen kalır.
`SleepAction` SLEEP diliminin işi; EAT dilimi gece davranışına aksiyon vermiyor. Tek değişiklik
yorum etiketi: `// GUESS(SLEEP slice): replace with CurrentAction.Kind == "Sleep"`.

### 3.6 Loglama ve determinizm

- Projeksiyon **salt okurdur**: `DescribeActivity`/`ProjectActor` WorldState'e yazamaz, RNG
  kullanamaz, `UnityEngine.Time` okuyamaz (yalnız `_world.Time`). Aksiyon FAZ geçişlerinin
  logları Simulation tarafında, tek `LogManager` üzerinden atılır (DOC 2/3'ün kontratı);
  projeksiyonun log yüzeyi yalnızca `ActionVerbTable.Unknown` warn'ıdır — o da presentation
  sınırında aynı `LogManager`a akar, Domain'e log bağımlılığı girmez.
- Etiket `WorldState`in saf fonksiyonu kaldığı için determinizm anayasası etkilenmez; save/load
  sonrası `CurrentAction` (save mapping DOC'unun parallel-array işi) hydrate olur olmaz aynı
  kelime geri gelir — projeksiyonun kalıcı durumu yoktur (`_warned` seti kozmetiktir).

## 4. Hayatta kalan tahmin dalları (aksiyonsuz aktörler için takvim kelimesi)

`CurrentAction == null` iken kelime `DescribeScheduleWord`dan gelir. EAT dilimi yalnızca
sivil açlık zincirine aksiyon verdiği için şunlar **bilerek** tahmin kalır:

| Dal | Kanıt | Emekliye ayıran dilim |
|-----|-------|------------------------|
| `"on watch"` / `"hunting"` (rol) | `WorldProjection.cs:113-114` | GUARD/COMBAT aksiyonları |
| `"sleeping"` / `"heading home"` (gece) + `Sleeping` pozu | `:125`, `:98-106` | SLEEP dilimi |
| `"winding down"` (≥20) | `:126` | SLEEP dilimi |
| `"harvesting"` / `"tending the field"` (bitki yakınlığı) | `:127-139` | FARM/WORK dilimi (`HarvestAction`) |
| `IsIdle ? null : "working"` | `:142`, `ActorScheduleState.cs:52` | WORK dilimi (`PerformWorkAction`) |
| HAMMER poz ikonu (8–18 işçi) | `NpcPoseIconView.cs:39,42` | WORK dilimi |

Her kalan dal koda `// GUESS(<slice>):` önekiyle işaretlenir — sonraki dilimler grep ile bulur.
Kural: **yeni tahmin dalı eklenemez**; yeni fiil = yeni aksiyon türü + tablo satırı.

## 5. Agentcheck assertion'ı — render katmanında `label == CurrentAction`

Proje kuralı (memory: *verify-at-render-layer*): data-layer logu kanıt değildir; ekrandaki
TextMesh okunur. Mevcut `--ember-agentcheck` koşusuna (`EmberProofScreenshotDriver.cs:104-110`,
`RunAgentCheck :674`) REFORM #1 uzamsal döngüsünün (`:762-785`) hemen ardından üçüncü
invariant bloğu eklenir.

### 5.1 Render tarafını okunur yapmak

```csharp
// NpcActivityLabelView.cs — tek satır ekleme:
/// <summary>W32 DOC5 agentcheck: the ACTUAL TextMesh text — render-layer truth, not the pushed value.</summary>
public string RenderedText => _label != null ? _label.text : null;
```

22 m cull (`NpcActivityLabelView.cs:51-54`) yalnızca `_renderer.enabled`'ı kapatır; `_label.text`
mesafeden bağımsız güncel kalır — assertion kameradan uzak aktörlerde de geçerlidir.

### 5.2 Yarışsız örnekleme protokolü (double-read guard)

Sim tick'i coroutine frame'leri arasında ilerleyebilir; etiket `SetTarget` push'u ile TextMesh
arasında bir tick'lik bayatlık yarışı vardır. Çözüm: iki adapter okuması arasında sim
ilerlemediyse örnek geçerli sayılır:

```csharp
// RunAgentCheck içine, spatial bloğun deseniyle (id-first, key-fallback çözümleme :769-775):
int labelFails = 0, eatingSeen = 0;
foreach (var view in viewsInScene)
{
    var label = view.GetComponent<Views.NpcActivityLabelView>();
    if (label == null) continue;                                   // etiketsiz aktör kapsam dışı (§2)
    if (!TryResolve(view, commands, out var s1)) continue;         // spatial bloğun çözümleme yardımcı hâli
    string rendered = label.RenderedText ?? string.Empty;
    if (!TryResolve(view, commands, out var s2)) continue;
    if (s1.Activity != s2.Activity || s1.ActionKind != s2.ActionKind) continue; // tick araya girdi → örneği at

    // LEG A — render == projeksiyon (bayat/kopuk push'u yakalar):
    if (rendered != (s1.Activity ?? string.Empty))
    { labelFails++; Debug.LogError($"[Invariant] label '{view.name}' renders '{rendered}' but sim projects '{s1.Activity}'."); }

    // LEG B — projeksiyon == aksiyon, BAĞIMSIZ beklenti tablosuyla (tahminin geri sızmasını yakalar;
    // ActionVerbTable'ı çağırmak totoloji olurdu — harness kendi sözlüğünü taşır):
    if (s1.ActionKind == "MoveToFood"  && s1.Activity != "seeking food") { labelFails++; Debug.LogError(...); }
    if (s1.ActionKind == "ConsumeFood" && s1.Activity != "eating")       { labelFails++; Debug.LogError(...); }
    if (s1.ActionKind == "ConsumeFood") eatingSeen++;
}
Debug.LogError yerine Log: $"[Invariant] label checks done - fails={labelFails}, eating={eatingSeen}."
```

### 5.3 Boşluk (vacuous) koruması + ekran kanıtı

Bütün aktörler fallback'teyse Leg B hiç çalışmaz ve assertion boş geçer. Koşu, sim saatini öğle
penceresine sardıktan sonra (mevcut proof saat-sarma kancası; yoksa `ProofAdvanceToHour(12)`
tarzı tek diagnostik kanca — spatial bloktaki `ProofMovePlayerBeside` :744 ile aynı aile)
**en az bir** `ConsumeFood` örneği görmek zorundadır:

- `eatingSeen == 0` → `Debug.LogError("[Invariant] label assertion VACUOUS: no ConsumeFood observed.")`
- İlk `eating` etiketi doğrulandığı anda bir screenshot alınır (harness'ın mevcut poll-wait
  screenshot makinesi; launch protokolü memory'deki gibi: master flag `--ember-proof-screenshots`,
  windowed) — "shipped" iddiasının görsel kanıtı budur.

### 5.4 Fallback ve determinizm etkisi

- `UnavailableSimulationAdapter` altında `TryReadActor` false döner → örnek üretilmez,
  `labelFails=0`, koşu yeşil (harness kontratı korunur).
- Assertion salt okuma + `Debug.LogError`dur; sim durumuna yazmaz, determinizmi bozamaz.

## 6. Değişiklik yüzeyi (LOW line count özeti)

| Dosya | Değişim |
|-------|---------|
| `Presentation/Ember/Adapters/ActionVerbTable.cs` | YENİ, ~20 satır |
| `DomainSimulationAdapter.WorldProjection.cs:110-143` | EAT tahmin dalları silinir (−8), aksiyon okuma eklenir (+3), `ProjectActor`a `actionKind` (+1) |
| `Views/ActorView.cs` | `ActorViewState.ActionKind` (+2), pose-icon push (+3) |
| `Views/NpcActivityLabelView.cs` | `RenderedText` getter (+2) |
| `Views/NpcPoseIconView.cs` | mug saat dalı → `SetActionKind` (+6/−2) |
| `Diagnostics/EmberProofScreenshotDriver.cs` | label invariant bloğu (~45) |
| `NightCurfewView`, `WorldViewProjector`, spawner, `IDomainSimulationAdapter` | değişmez |

## 7. Kabul kriterleri

1. `DescribeActivity` içinde saat/konum/açlık girdisiyle üretilen hiçbir EAT fiili kalmadı;
   `"eating"`/`"seeking food"` yalnızca `CurrentAction.Kind` üzerinden doğuyor.
2. `--ember-agentcheck` koşusu `fails=0` ve `eating>=1` raporluyor; `eating` anına ait
   screenshot artefaktı var (render-layer kanıt).
3. Fallback harness (UnavailableSimulationAdapter) yeşil; chunking-invariance ve golden
   roundtrip testleri değişiklikten etkilenmiyor (projeksiyon salt okur).
4. Kalan her tahmin dalı `// GUESS(<slice>):` etiketi taşıyor; `grep "GUESS("` sonraki
   dilimlerin iş listesini veriyor.
