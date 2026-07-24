# W32-04 — ActionLog / LogManager: Her Faz Geçişi İçin TEK Deterministik Log Dikişi

> RUH_TESHIS kararının log ayağı: "Sistemler eylemi adım adım ilerletsin; UI `CurrentAction`'ı
> okusun." Adım adım ilerleyen bir şeyin her adımı **tek kapıdan** kayda geçmelidir.
> Kullanıcı talebi birebir: *"her action da log olacak, log manager olmali ki anlamaliyiz her adimi."*
>
> Kapsam: **yalnız EAT dilimi** (W32-01 `ActorActionState`, W32-02 action hiyerarşisi,
> W32-03 doğrulanmış dünya operasyonları ile aynı dilim). Genelleşecek yerler sonda işaretli.

---

## 0. Bugünün envanteri (gerçek kod, gerçek yaralar)

| Bulgu | Kanıt |
| --- | --- |
| `WorldEventLog` sınırsız append-only `List<WorldEvent>`; hiç trim yok | `Assets/Scripts/Domain/World/WorldEventLog.cs:26-56` |
| Save TÜM event'leri yazar (log ne kadar büyürse save o kadar büyür) | `Assets/Scripts/Data/Save/SliceJson/WorldSaveMapper.Narrative.cs:19-22` |
| Okuyucular sessiz cap ile atlıyor (B21): RumorMill `ScanCap = 256` | `Assets/Scripts/Simulation/Living/RumorMillSystem.cs:16,27-31` |
| HUD tail okuyucusu `maxRows` ile son N satırı alıyor | `Assets/Scripts/Presentation/Visual/WorldEventTailSnapshot.cs:25-58` |
| Hacim felaketi YAŞANDI: aktör-başına NeedsStep event'leri 90. günde ~1 GB log + Gen2 GC stutter üretti; saatlik özete indirilerek çözüldü | `Assets/Tests/EditMode/Composition/WorldTickDigestGoldenTests.cs:12-15` (re-baseline notu) |
| Adım spam'inin ikinci örneği: `ActorStepped` her adımda event yazar; rumor/ilgi filtreleri onu elle dışlıyor | `Assets/Scripts/Simulation/Process/PathfindingSystem.cs:11,57`, `Assets/Scripts/Presentation/Visual/WorldEventInterest.cs:12` |
| Chunking invariance testi event log'ların BİREBİR eşitliğini karşılaştırıyor — log determinizmin bedava kanıtı | `Assets/Tests/EditMode/Composition/CadenceChunkingInvarianceTests.cs:20-45` |
| Digest golden testi event log'u hash'e katıyor (event akışı değişirse re-baseline gerekir; 4 kez emsali var) | `Assets/Scripts/Simulation/Composition/WorldStateDigest.cs:50,442-451`, `WorldTickDigestGoldenTests.cs:27` |
| Proof driver grep'lenebilir `Debug.Log("[Proof] ...")` satırlarıyla çalışır | `Assets/Scripts/Presentation/Ember/Diagnostics/EmberProofScreenshotDriver.cs:45` |
| UI aktiviteyi TAHMİN ediyor (saat/konum/açlık heuristiği) — ruhsuzluğun 2.9 maddesi | `Assets/Scripts/Presentation/Ember/Adapters/DomainSimulationAdapter.WorldProjection.cs:110-142` (`DescribeActivity`) |
| Domain'de bugün hiçbir `LogManager` yok; faz kavramı da yok | `grep -rn "LogManager" Assets/Scripts` → boş |

Ders net: **faz adımlarını `WorldEventLog`'a yazmak daha önce denendi ve yaktı.**
Ama faz adımlarını hiç kaydetmemek de teşhisin 2.9'unu (görüntü katmanı fiil uyduruyor)
ve chunking testinin körlüğünü (fazlar karşılaştırılamıyor) yaşatır.

---

## 1. Karar: İKİ KATMAN — bounded ring (her faz) + WorldEvent (yalnız terminal)

Üç seçenek değerlendirildi:

| Seçenek | Karar | Neden |
| --- | --- | --- |
| **A** — Her faz geçişi `WorldEventLog` satırı | RED | Hacim emsali: aktör-başına event spam'i ~1 GB/90 gün + GC stutter yaşattı (`WorldTickDigestGoldenTests.cs:12-15`). Save'e tamamen yazıldığı için save de şişer (B21). Rumor/HUD okuyucuları bir kirlilik filtresi daha kazanırdı (`ActorStepped` emsali). |
| **B** — Yalnız in-memory ring + verbose bayrağı | RED | Terminal sonuçlar (yemek yendi / eylem başarısız) dünyanın hikâye yüzeyleridir: RumorMill, RuntimeHistory, quest ve chronicle **WorldEventLog okur** (`RumorMillSystem.cs:20`, `CascadeSystems.cs:142`). Terminali event'e yazmazsak Ayşe'nin yemeği dedikoduya, tarihe ve save'e giremez. |
| **C** — **HER İKİSİ**: her faz geçişi → deterministik bounded ring; yalnız `Completed`/`Failed` → `WorldEvent` | **KABUL** | Adım ayrıntısı ucuz ve sınırlı kalır; hikâye yüzeyleri yapılandırılmış terminal event alır; chunking testi fazları da karşılaştırabilir. |

Tek dikiş kuralı: **ring'e ve event'e giden TEK yol `LogManager.Record`'dur** ve
`LogManager.Record`'un tek çağıranı W32-02'deki action taban sınıfının
`TransitionTo` template metodudur. Sistemler faz alanına dokunamaz →
"her adım loglanır" bir konvansiyon değil, derleyici garantisidir.

---

## 2. Tip tasarımı (Domain — Unity yok, IO yok, RNG yok)

Dosya evi: `Assets/Scripts/Domain/Actors/Actions/` (W32-02 hiyerarşisiyle aynı klasör).

### 2.1 `ActionLogEntry` — satır, struct, sıfır alokasyon

```csharp
// Kısıt: hot path'te string YOK — her alan sayı/enum; metin ancak sink/UI
// katmanında, tembel olarak üretilir. Parallel-array save dostu.
public readonly struct ActionLogEntry
{
    public readonly long TickMinutes;      // GameTime.TotalMinutes — event'lerle ayni saat
    public readonly ActorId Actor;
    public readonly ActionKind Action;     // W32-02: Eat (EAT diliminde tek üye)
    public readonly ActionPhase From;      // W32-02 faz kümesi (Seek/Move/Take/Consume/...)
    public readonly ActionPhase To;
    public readonly ulong TargetId;        // item/site/actor id; 0 = hedefsiz
    public readonly ActionLogReason Reason;
}
```

`ActionLogReason` EAT dilimi için kapalı küçük enum:
`None, TargetSelected, ReservationAcquired, ReservationLost, Arrived, PathBlocked,
ProgressTicked, Completed, TargetGone, InterruptPreempted`.
Yeni action'lar kendi sebeplerini **tüketicileriyle birlikte** ekler
(agent-rules-v2 kural 2: spekülatif üye yok — `WorldEventKind.cs:6-8` ile aynı disiplin).

### 2.2 `ActionLogRing` — WorldState'te yaşayan deterministik sınırlı halka

```csharp
// Kısıt: kapasite SABİT (Capacity = 1024) ve deterministik — ayni seed + ayni
// tick sayisi => birebir ayni halka içerigi. Push O(1), alokasyonsuz.
// TotalPushed monoton sayaç: halka dolup taşsa da "kaçıncı geçiş" kimliği kalıcı.
public sealed class ActionLogRing
{
    public const int Capacity = 1024;
    public long TotalPushed { get; }
    public void Push(in ActionLogEntry entry);
    public int Count { get; }
    public ActionLogEntry At(int indexFromOldest);   // UI/debug tail okuma
}
```

Boyutlandırma: EAT dilimi ~30 aktör × 2-3 öğün/gün × ~6 faz geçişi ≈ **540 satır/gün**;
1024 ≈ son ~2 günün izi. Struct ~40 bayt → halka ~40 KB, save'e katkısı önemsiz.

`WorldState`'e tek alan eklenir (`WorldState.cs:40`'taki `Events`'in yanına):

```csharp
public ActionLogRing ActionLog = new ActionLogRing();
```

### 2.3 `IActionLogSink` — gözlemci, ASLA mutasyoncu

```csharp
// Kısıt: sink SALT GÖZLEMCİDİR — dünya durumuna dokunamaz, exception atamaz,
// determinizme katılamaz. Domain arayüzü tanımlar; implementasyon dışarıda
// (Presentation: Debug.Log; test: capture list; fallback harness: Console/hiç).
public interface IActionLogSink { void OnPhase(in ActionLogEntry entry); }
```

### 2.4 `LogManager` — TEK dikiş

```csharp
// Kısıt: ring + WorldEvent + sink'lere giden TEK kapı. Tek çağıran:
// ActorAction.TransitionTo (W32-02 template metodu). Determinizm: ring ve
// event yazımı state'tir; sink çağrısı gözlemdir — sink listesi boşken
// maliyet ring push + terminal kontrolünden ibarettir.
public sealed class LogManager
{
    public LogManager(params IActionLogSink[] sinks);
    public void Record(WorldState world, in ActionLogEntry entry)
    {
        world.ActionLog.Push(entry);
        if (entry.To == ActionPhase.Completed || entry.To == ActionPhase.Failed)
            world.Events.Append(ToTerminalEvent(entry));   // 33/34, bkz. §3
        for (int i = 0; i < _sinks.Length; i++) _sinks[i].OnPhase(entry);
    }
}
```

Sahiplik: `WorldTickComposer`'ı kuran kompozisyon kökü (`DomainSimulationAdapter` /
fallback harness / test) `LogManager`'ı kurar ve action ilerletme sistemine
(W32-03'ün action-advancement bandı) ctor'dan verir. `WorldState` **sink taşımaz** —
save'e sızacak referans yok.

---

## 3. Terminal `WorldEvent` sözleşmesi

`WorldEventKind.cs`'e iki üye (`ChronicleEvent = 32`'den sonra):

```csharp
// W32 RUH: eylemler birinci sınıf — yalnız TERMİNAL sonuç event olur.
// Faz adımları ActionLogRing'dedir; buraya adım yazmak B21/1GB emsalini geri getirir.
ActionCompleted = 33,
ActionFailed = 34,
```

`Reason` alanı (zaten string, `WorldEvent.cs:21-37`) tek biçimli üretilir:

```
eat:consume completed target=item:418 t=2881
eat:move failed reason=PathBlocked target=item:418 t=2874
```

- Hacim: terminal event ≈ tamamlanan/başarısız eylem sayısı (~60-90/gün) —
  bugünkü saatlik özet düzeniyle aynı mertebe; adım spam'inin 1/6'sından az.
- `RumorMillSystem.Distill` bilinmeyen kind'a `null` döner → dedikodu kirlenmez;
  istenirse `ActionCompleted(eat)` için "someone had a proper meal" satırı sonra eklenir.
- `NeedConsumptionSystem`'in bugünkü `meal_eaten` yazımı EAT dilimi devreye girince
  bu terminal event'e **devrolur** (çift kayıt yasak — tek gerçek: action sonucu).
- Digest golden testi event akışını hash'lediği için (`WorldStateDigest.cs:50`)
  `BaselineHash` re-baseline edilir — emsalli, meşru dünya-davranış değişimi
  (`WorldTickDigestGoldenTests.cs:12-26` dört emsal sayıyor).

---

## 4. Save / golden roundtrip

`WorldState`'e alan eklendi → reflection golden roundtrip (`Assets/Tests/EditMode/Save/
WorldSaveMapperGoldenRoundtripTests.cs`) map'lenmemiş alanı yakalar. Bu yüzden ring
**save-map'lenir** (sınırlı olduğu için ucuz; load-continue == kesintisiz koşu eşitliği de
ancak böyle korunur). `WorldSaveData`'ya mevcut parallel-array konvansiyonuyla:

```csharp
public long[]  actionLogTickMinutes;
public long[]  actionLogActorIds;
public int[]   actionLogActionKinds;
public int[]   actionLogFromPhases;
public int[]   actionLogToPhases;
public long[]  actionLogTargetIds;
public int[]   actionLogReasons;
public long    actionLogTotalPushed;
```

Mapper: `WorldSaveMapper.ActionLog.cs` partial'ı, `ToWorldEventLogData` deseninin
kopyası (`WorldSaveMapper.Narrative.cs:19-55`), en-eskiden-en-yeniye düzleştirip
yazar / okurken sırayla `Push`'lar. ~50 satır.

---

## 5. Üç müşteri

### 5.a Chunking invariance testi — fazların bedava determinizm kanıtı

Yeni test `ActionPhaseChunkingInvarianceTests`, mevcut deseni birebir kopyalar
(`CadenceChunkingInvarianceTests.cs:20-45`): aynı dünya tick-tick ve pürüzlü
chunk'larla ilerletilir; iki koşuya da **capture sink** takılır
(`List<ActionLogEntry>` biriktiren test-içi `IActionLogSink`) ve TAM geçiş akışı
satır satır karşılaştırılır. Halka 1024'te kırpsa da capture sink kırpmaz —
tam akış eşitliği kanıtlanır; halka içeriği eşitliği ise save-eşitliğinden bedava gelir.
Kırmızıya düşen mesaj mevcut testle aynı dili konuşur:
*"ragged advancement produced a DIFFERENT phase history — some system advances actions on the wrong clock."*

### 5.b Proof driver — grep'lenebilir satırlar

Presentation'a tek sınıf: `ActionLogDebugSink : IActionLogSink` —
`Debug.Log` ile **tek satır, key=value, sabit format**:

```
[ActionLog] t=2881 actor=12 act=Eat ph=Move->Take tgt=item:418 why=Arrived
```

Kayıt kuralı: `--ember-proof-screenshots` (proof master bayrağı,
`EmberProofScreenshotDriver.cs:26`) VEYA yeni `--ember-action-log` bayrağı varsa
`DomainSimulationAdapter` sink'i `LogManager`'a takar; normal oyunda sink listesi boş →
maliyet sıfıra yakın. Proof harness'in Player.log grep'i `[Proof]` deseniyle aynı
şekilde `[ActionLog]` arar; string üretimi YALNIZ sink içinde olduğundan
(invariant culture) determinizm ve GC etkilenmez.

### 5.c UI aktivite etiketi — tahmin değil, okuma

`DescribeActivity`'nin saat/konum/açlık heuristiği (`DomainSimulationAdapter.
WorldProjection.cs:110-142`) EAT dilimi aktörleri için silinir; etiket
`ActorActionState.CurrentAction`'dan **verbatim** okunur (W32-01/02):
`ActionKind+ActionPhase → string` tablosu Presentation'dadır ("Eat/Move" → "yemeğe gidiyor",
"Eat/Consume" → "yemek yiyor"). ActionLog'un buradaki rolü ikincil ama kritik:
etiket YANLIŞSA halka son 1024 geçişi tutar — "bu etikete hangi geçişle gelindi"
sorusu artık cevaplanabilir (bugün cevapsız). Kabul testi RUH_TESHIS §10 satırıyla aynı:
**"Activity etiketi `CurrentAction` ile bire bir aynıdır."**

---

## 6. B21 — WorldEventLog cap/rotasyon hikâyesi

Bugün: `WorldEventLog.cs:26-56` sınırsız; save tamamını yazar; RumorMill 256,
HUD tail N satır sessiz atlar. 1 GB vakası aktör-başına adım spam'indendi ve
**bu tasarımın 1. kararı (adımlar ring'de, event'te değil) o sınıfı kalıcı kapatır.**
Terminal event'ler eklendikten sonra büyüme ~yüzlerce satır/gün mertebesinde kalır.

Yine de sınırsızlık bir borçtur. Tasarlanan (W33'e ertelenen) mekanizma:

1. `WorldEventLog`'a `TotalAppended` (long, monoton) ve `FirstRetainedSeq` eklenir;
   `TrimOldest(maxRetained)` en eskiyi düşürür, seq kimliği korunur.
2. **Bütün imleçler index'ten seq'e taşınır** — kritik göçük: `RumorEventCursor`
   (`WorldState.cs:179`, `RumorMillSystem.cs:27-40`) bugün mutlak index'tir;
   trim mutlak index'i kaydırır. Seq tabanlı imleç trim'den etkilenmez.
3. Retention: `MaxRetained = 8192`, günlük bantta trim (deterministik: aynı toplam
   tick → aynı pencere → chunking testi yeşil kalır).
4. Save `firstRetainedSeq` + `totalAppended` alır; digest re-baseline gerekir.

W32'de AKTİF EDİLMEZ çünkü: (a) EAT dilimi event büyümesini artırmaz, azaltır
(adımlar ring'e gitti, `meal_eaten` devri net sıfır); (b) imleç göçü RumorMill +
RuntimeHistory + quest okuyucularına dokunur — "okyanusu kaynatma" sınırının dışı.

---

## 7. Satır bütçesi (LOW line count taahhüdü)

| Parça | ~Satır |
| --- | --- |
| `ActionLogEntry` + `ActionLogReason` | 45 |
| `ActionLogRing` | 70 |
| `IActionLogSink` | 10 |
| `LogManager` | 55 |
| `WorldEventKind` + terminal event üretimi | 25 |
| `WorldSaveData` alanları + `WorldSaveMapper.ActionLog.cs` | 60 |
| `ActionLogDebugSink` (Presentation) | 25 |
| `ActionPhaseChunkingInvarianceTests` + roundtrip eki | 120 |
| **Toplam** | **~410** |

---

## 8. Ayşe'nin EAT zinciri — beklenen iz (kabul örneği)

```
[ActionLog] t=1320 actor=12 act=Eat ph=None->Seek     tgt=0        why=TargetSelected
[ActionLog] t=1320 actor=12 act=Eat ph=Seek->Move     tgt=item:418 why=ReservationAcquired
[ActionLog] t=1334 actor=12 act=Eat ph=Move->Take     tgt=item:418 why=Arrived
[ActionLog] t=1335 actor=12 act=Eat ph=Take->Consume  tgt=item:418 why=ProgressTicked
[ActionLog] t=1341 actor=12 act=Eat ph=Consume->Completed tgt=item:418 why=Completed
WorldEvent: kind=ActionCompleted actor=12 reason="eat:consume completed target=item:418 t=1341"
```

Kesinti hikâyesi de aynı kapıdan: Mehmet ekmeği kaparsa
`ph=Move->Failed why=ReservationLost` + `ActionFailed` event'i → replan W32-02'nin işi,
ama izi buradadır.

---

## 9. Sonra genelleşecekler (EAT diliminde YAPILMAYACAK)

- `ActionKind`/`ActionLogReason` yeni action'larla (Sleep, Work, Harvest, Haul) büyür —
  her üye tüketicisiyle gelir.
- Halka üstünden F-tuşlu bir "aktör izi" debug paneli (`WorldEventTailSnapshot` deseni).
- Terminal event'lerin RumorMill/Narrator cümleleri.
- §6'daki seq tabanlı trim'in aktivasyonu + imleç göçü (W33 adayı).
- Sink çoğullaması (ör. dosyaya NDJSON proof dökümü) — arayüz hazır, ihtiyaç yok.
