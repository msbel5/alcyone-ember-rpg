# W32 / 01 — ActorActionState: Aktörün Zihni

> RUH_TESHIS.md kararının ilk taşı: aktör niyetini ve mevcut eylemini KENDİSİ taşır
> (`docs/RUH_TESHIS.md` §2.1, §6). Bu belge yalnızca EAT dikey dilimi için tasarlar
> (§9 "İlk dikey dilim"); genellemeler en sonda ayrı başlıktadır.

## Karar özeti

| Konu | Karar |
|---|---|
| Durum tipi | Domain'de saf `readonly struct ActorActionState` (enum alanlı, davranışsız) |
| Davranış | Simulation'da `IActionAdvancer` sınıf hiyerarşisi (Strategy + Template Method) |
| Yerleşim | `ActorRecord.ActionState` + `ApplyActionState(...)` — `ScheduleState` deseninin aynısı |
| Save | `ActorSaveData`'ya 10 alan; `default == Idle == tüm alanlar 0` sayesinde presence bayrağı ve schema bump YOK |
| Determinizm | `WorldStateDigest` aktör satırına zihin alanları eklenir; golden roundtrip dünyasına non-default action state eklenir |
| Log | Her faz geçişi TEK `ActionLogManager` üzerinden (EmberLog sink'ine akar); advancer'lar doğrudan loglayamaz |

EAT dilimi kelime dağarcığı — bilerek asgari:

- Intent: `{ None, Eat }`
- Action: `{ None, MoveToFood, TakeFood, ConsumeFood }`

---

## 1. Domain tipleri

Yer: `Assets/Scripts/Domain/Actors/ActorActionState.cs` (+ `Domain/Core/ReservationId.cs`).
Domain kuralı: Unity yok, IO yok, RNG yok — mevcut `ActorScheduleState`
(`Assets/Scripts/Domain/Actors/ActorScheduleState.cs:16`) ile aynı disiplin.

```csharp
namespace EmberCrpg.Domain.Actors
{
    /// <summary>Aktörün üst niyeti. Save'e int olarak yazılır: değerler SABİTTİR, silme/yeniden numaralama yasak.</summary>
    public enum ActorIntent { None = 0, Eat = 1 }

    /// <summary>Tipli eylem kimliği. UI bu değeri VERBATIM okur (RUH_TESHIS §10: activity == CurrentAction).</summary>
    public enum ActorActionType { None = 0, MoveToFood = 1, TakeFood = 2, ConsumeFood = 3 }

    /// <summary>
    /// Eylem fazı. Succeeded/Failed TEK advancement'lık devri-teslim halleridir:
    /// bir sonraki advancement bunları tüketip ya zincirin sıradaki action'ını başlatır ya da Idle'a döner.
    /// </summary>
    public enum ActionPhase { None = 0, Running = 1, Succeeded = 2, Failed = 3 }

    /// <summary>Neden başarısız oldu — hikâyenin hammaddesi ("Mehmet ekmeği kaptı" = ReservationLost).</summary>
    public enum ActionFailureReason { None = 0, NoFoodFound = 1, ReservationLost = 2, Unreachable = 3, Interrupted = 4, TimedOut = 5 }

    /// <summary>Karar sistemi yeni intent atamadan önce buna bakmak ZORUNDADIR.</summary>
    public enum ActionInterruptPolicy { Interruptible = 0, NonInterruptible = 1 }
}
```

`ReservationId` — `ItemId` kalıbının kopyası (`Assets/Scripts/Domain/Core/ItemId.cs:11`):
`readonly struct ReservationId : IEquatable<ReservationId>`, `ulong Value`, `IsEmpty`, `Empty`.
Rezervasyon defterinin kendisi (ledger + sayaç) bu belgenin DIŞINDA — bkz. §8 ileri notlar;
burada yalnızca kimlik alanı tanımlanır.

### ActorActionState struct'ı

```csharp
namespace EmberCrpg.Domain.Actors
{
    // Design note:
    // ActorActionState is the actor's persistent mind for W32: intent + current action + phase.
    // CONSTRAINT (save/backward-compat): default(ActorActionState) MUST equal Idle and MUST be
    // the all-zero bit pattern — pre-W32 saves deserialize missing fields to 0 and load as Idle.
    // CONSTRAINT (determinism): pure data, no Unity/IO/RNG; transitions are pure functions.
    /// <summary>Kalıcı zihin durumu: niyet + mevcut eylem + faz + hedefler + ilerleme.</summary>
    public readonly struct ActorActionState : IEquatable<ActorActionState>
    {
        public static ActorActionState Idle => default;

        public ActorIntent CurrentIntent { get; }
        public ActorActionType CurrentAction { get; }
        public ActionPhase Phase { get; }
        /// <summary>TakeFood başarısında dünyaya doğan (mint edilen) yemek biriminin kimliği; öncesinde Empty.</summary>
        public ItemId TargetItemId { get; }
        /// <summary>Rezervasyonun yapıldığı stockpile'ın sitesi; MoveToFood'un varış hedefi buradan türetilir.</summary>
        public SiteId TargetSiteId { get; }
        public ReservationId ReservationId { get; }
        /// <summary>Yalnızca Running fazında, yalnızca advancer tarafından artar.</summary>
        public int ProgressTicks { get; }
        /// <summary>Eylemin başladığı GameTime.TotalMinutes; CurrentAction == None iken 0.</summary>
        public long StartedAtMinutes { get; }
        public ActionFailureReason FailureReason { get; }
        public ActionInterruptPolicy InterruptPolicy { get; }

        public bool IsIdle => CurrentIntent == ActorIntent.None && CurrentAction == ActorActionType.None;

        // Geçişler — hepsi yeni değer döndürür (immutable). Geçersiz geçiş exception atar:
        // sessiz düzeltme determinism kaçağıdır, gürültülü ölüm testte yakalanır.
        public static ActorActionState ForIntent(ActorIntent intent);                    // Idle -> intent seçildi (action henüz None)
        public ActorActionState Start(ActorActionType action, SiteId targetSite,
            ItemId targetItem, ReservationId reservation, long startedAtMinutes,
            ActionInterruptPolicy policy);                                               // -> Running, Progress=0
        public ActorActionState Advanced();                                              // Running -> Running, Progress+1
        public ActorActionState Succeeded();                                             // Running -> Succeeded
        public ActorActionState Failed(ActionFailureReason reason);                      // Running -> Failed(reason)
        public ActorActionState CarryingItem(ItemId item);                               // TakeFood başarısında hedef item'ı bağlar
    }
}
```

Neden struct, class değil: (a) `ActorScheduleState` emsali — save/copy/digest boru hattı
struct'ı zaten biliyor; (b) `default` = Idle = eski save'lerin bedava migrasyonu;
(c) heap tahsisi yok — hourly bantta yüzlerce aktör dolaşılıyor (W30e donma dersi);
(d) golden roundtrip reflection karşılaştırması alan-alan çalışır, referans kimliği derdi yok.

### Invariant: "None ⇒ tümü sıfır"

`CurrentAction == None` iken `TargetItemId/TargetSiteId/ReservationId/ProgressTicks/StartedAtMinutes/FailureReason`
sıfır OLMAK ZORUNDADIR (istisna: `ForIntent` sonrası `CurrentIntent != None` olabilir; `Failed`
sonrası `FailureReason` bir advancement boyunca yaşar). Bu kural struct kurucularında assert edilir
ve load normalizasyonunun temelidir (§4).

---

## 2. ActorRecord'a yerleşim

`Assets/Scripts/Domain/Actors/ActorRecord.cs` — mevcut desenin (`ScheduleState`, satır 33/74/162)
birebir devamı; SATIR SAYISI DÜŞÜK tutulur:

1. Kurucuya SONA eklenen opsiyonel parametre: `ActorActionState actionState = default`.
   Sona eklemek mevcut çağrıları kırmaz.
2. Property: `public ActorActionState ActionState { get; private set; }`
3. Setter sarmalayıcı (emsal `ApplyScheduleState`, satır 162):

```csharp
// CONSTRAINT (single writer): only ActionLifecycleSystem may call this in simulation code.
// Save/load (ActorSaveMapper) is the second legitimate caller. Anything else is a puppet-master
// regression — FieldOwnershipRegistry lists the owner; the W32 review gate greps call sites.
public void ApplyActionState(ActorActionState actionState)
{
    ActionState = actionState;
}
```

4. **`WithHomeAndAnchor` (satır 92-117) kopyasına `ActionState` taşınır.** Home/DayAnchor
   alanları vaktiyle mapper'da düşmüştü ("sleeping pile" bug'ı, `WorldSaveData.ActorDungeon.cs:77-80`
   yorumunda anlatılır); aynı sınıf hatayı kopya kurucuda tekrarlamamak için kopya sonrası
   `copy.ApplyActionState(ActionState);` satırı zorunludur.

### Sahiplik kaydı

`Assets/Scripts/Simulation/Composition/FieldOwnershipRegistry.cs:18-50` listesine yeni satır:

```
Actor.ActionState  ->  ActionLifecycleSystem   (TEK yazar)
```

EAT diliminde karar (decide) ve ilerletme (advance) AYNI sistemin iki fazıdır
(`ActionLifecycleSystem`): teşhisin §2.3 çok-yazarlı alan eleştirisine yeni bir
çok-yazarlı alan ekleyerek başlamayız. İleride karar ayrı sisteme çıkarsa
intent önerisi command kuyruğuyla taşınır, yazar yine tek kalır (§8).

---

## 3. Save eşlemesi — EXACT eklemeler

Aktörler `WorldSaveData.actors` (`ActorSaveData[]`, `WorldSaveData.cs:46`) üzerinden gider;
kök-seviye paralel diziler (pursuit/critter tarzı) DEĞİL, çünkü aktör başına DTO zaten var ve
`ActorSaveMapper` iki yönü tek dosyada tutuyor. Paralel dizi yüzeyi aktör-dışı kayıtlar içindir.

### 3.1 `WorldSaveData.ActorDungeon.cs` → `ActorSaveData` sınıfına eklenen blok

```csharp
// W32 EAT slice — persisted mind/action state (docs/ruh/w32/01-actor-action-state.md).
// CONSTRAINT: all-zero block == Idle == pre-W32 save. NO presence flag needed (contrast
// hasMood, where 0 was a legitimate live value): StartedAtMinutes=0 is only meaningful
// when currentAction != 0, so the zero block is unambiguous.
public int currentIntent;          // ActorIntent
public int currentAction;          // ActorActionType
public int actionPhase;            // ActionPhase
public long actionTargetItemId;    // 0 = ItemId.Empty
public long actionTargetSiteId;    // 0 = SiteId.Empty
public long actionReservationId;   // 0 = ReservationId.Empty
public int actionProgressTicks;
public long actionStartedAtMinutes;
public int actionFailureReason;    // ActionFailureReason
public int actionInterruptPolicy;  // ActionInterruptPolicy
```

### 3.2 `ActorSaveMapper.ToSave` (`ActorSaveMapper.cs:21`) — nesne başlatıcıya eklenen satırlar

```csharp
// W32: mind state — mirror every field; the golden roundtrip test fails on any drop.
currentIntent = (int)actor.ActionState.CurrentIntent,
currentAction = (int)actor.ActionState.CurrentAction,
actionPhase = (int)actor.ActionState.Phase,
actionTargetItemId = (long)actor.ActionState.TargetItemId.Value,
actionTargetSiteId = (long)actor.ActionState.TargetSiteId.Value,
actionReservationId = (long)actor.ActionState.ReservationId.Value,
actionProgressTicks = actor.ActionState.ProgressTicks,
actionStartedAtMinutes = actor.ActionState.StartedAtMinutes,
actionFailureReason = (int)actor.ActionState.FailureReason,
actionInterruptPolicy = (int)actor.ActionState.InterruptPolicy,
```

### 3.3 `ActorSaveMapper.FromSave` (`ActorSaveMapper.cs:74`) — yükleme + normalizasyon

```csharp
// W32: rebuild the mind. Defensive rule: an out-of-range enum or a violated
// "None => all zero" invariant resets the WHOLE block to Idle (fail-safe, logged).
// A reset actor simply re-decides next tick — deterministic and story-preserving,
// unlike a half-restored action pointing at a target that no longer exists.
var actionState = ActorActionStateSaveReader.Read(save); // tek yerde normalizasyon
...
record.ApplyActionState(actionState);
```

`ActorActionStateSaveReader.Read` kuralları (küçük statik yardımcı, mapper dosyasında kalır):

- enum int'leri tanımlı aralık dışındaysa → `Idle` + `ActionLogManager.Warn("reset-on-load", actorId)`
- `currentAction == 0` fakat diğer alanlar ≠ 0 → `Idle` (invariant §1)
- `actionReservationId != 0` ise ASILI KALMIŞ olabilir (ledger başka dokümanda): mapper bunu
  ÇÖZMEZ; ilk advancement `Failed(ReservationLost)` üretir — deterministik ve loglu (§5).

### 3.4 Golden roundtrip değişikliği

`Assets/Tests/EditMode/Save/WorldSaveMapperGoldenRoundtripTests.cs:21-36` temsilî dünyası
(seed 7) TÜM yeni alanlar sıfırken bir alan düşmesini GİZLER. Populate bloğuna eklenir:

```csharp
// W32: non-default mind state so a dropped action-state mapping fails field-by-field.
var eater = world.Actors.Records.First(a => a != null);
eater.ApplyActionState(
    ActorActionState.ForIntent(ActorIntent.Eat)
        .Start(ActorActionType.MoveToFood, targetSite: new SiteId(1),
               targetItem: ItemId.Empty, reservation: new ReservationId(9),
               startedAtMinutes: 123, policy: ActionInterruptPolicy.Interruptible)
        .Advanced()); // ProgressTicks=1: sıfır-olmayan ilerleme de tur atmalı
```

Ayrıca `Assets/Tests/EditMode/Save/ActorSaveMapperTests.cs` içine tek odaklı test:
Idle roundtrip == Idle, dolu state roundtrip == alan-alan eşit, bozuk enum → Idle.

### 3.5 Schema versiyonu

`WorldSaveMapper.CurrentSchemaVersion` (`WorldSaveMapper.cs:31`) 1'de KALIR. EMB-012 kuralı
"uyumsuz şekil değişiminde bump" der; buradaki ekleme saf additive'dir ve sıfır-blok = Idle
kodlaması eski save'leri kendiliğinden doğru yükler. Migration dalı gerekmez.

---

## 4. Determinizm: digest + replay kapsamı

`WorldStateDigest` aktör satırı (`Assets/Scripts/Simulation/Composition/WorldStateDigest.cs:82-99`)
bugün pozisyon/schedule/needs hash'ler. Zihin alanları eklenmezse iki dünya AYNI digest'e sahipken
farklı eylemde olabilir — replay ve chunking-invariance testleri (aynı seed+tick = aynı WorldState)
zihin sapmasına kör kalır. Satıra eklenir:

```csharp
sb.Append('|'); AppendIntField(sb, (int)actor.ActionState.CurrentIntent);
sb.Append('|'); AppendIntField(sb, (int)actor.ActionState.CurrentAction);
sb.Append('|'); AppendIntField(sb, (int)actor.ActionState.Phase);
sb.Append('|'); AppendUlongField(sb, actor.ActionState.TargetItemId.Value);
sb.Append('|'); AppendUlongField(sb, actor.ActionState.TargetSiteId.Value);
sb.Append('|'); AppendUlongField(sb, actor.ActionState.ReservationId.Value);
sb.Append('|'); AppendIntField(sb, actor.ActionState.ProgressTicks);
sb.Append('|'); AppendLongField(sb, actor.ActionState.StartedAtMinutes);
```

Not: digest değeri değişir; depolanmış sabit golden digest YOK (testler before/after'ı dinamik
hesaplıyor, `SaveLoadDigestRoundtripTests.cs:34-38`), dolayısıyla kırılan test beklenmiyor —
ama chunking-invariance ve marathon harness'in yeşil kaldığı W32 kapanış kapısında doğrulanır.

---

## 5. CopyFrom / EnsureInvariants

- **`WorldState.CopyFrom` (`WorldState.cs:200`): DEĞİŞİKLİK YOK.** ActionState `ActorRecord`
  içinde yaşar; `Actors = other.Actors;` referans kopyası onu zaten taşır. Kural
  ("bu tipe eklenen alan CopyFrom'a da eklenir") tetiklenmez çünkü `WorldState`'e alan eklenmiyor.
- **`WorldState.EnsureInvariants` (`WorldState.cs:72`): DEĞİŞİKLİK YOK.** ActionState struct'tır,
  null olamaz; store null'ları mevcut `Actors ??= new ActorStore()` ile zaten kapsanır.
  Semantik normalizasyon (bozuk enum, invariant ihlali) yükleme yolunda `ActorActionStateSaveReader`'da
  yapılır — EnsureInvariants "null'ları doldur" sözleşmesini şişirmeyiz.
- **Asılı rezervasyon** (save'de ReservationId var, ledger'da karşılığı yok): EnsureInvariants
  DÜZELTMEZ. İlk `ActionLifecycleSystem` advancement'ı doğrulamada `Failed(ReservationLost)` üretir;
  aktör bir sonraki advancement'ta yeniden karar verir. Böylece düzeltme tek yerde, loglu ve
  event üretebilen bir yoldadır — sessiz onarım değil.

---

## 6. Enum mu, sınıf hiyerarşisi mi? — İkisi de, doğru katmanlarda

Kullanıcı direktifi: kalıtım tabanlı action sınıf hiyerarşisi. Determinizm anayasası + save
basitliği ile uzlaşma şu bölmeyle sağlanır:

- **Domain = saf enum DURUM** (`ActorActionType` vb., §1). Save'e int yazılır; digest'e int girer;
  golden reflection testi alan-alan çalışır; `default` Idle'dır. Polimorfik durum nesnesi olsaydı:
  JsonUtility polimorfik grafı ancak `[SerializeReference]` ile taşır (kırılgan, DTO/paralel-dizi
  düzenini ve reflection golden testini bozar), digest'e tip kimliği sızdırmak gerekirdi ve
  `default == Idle` bedava-migrasyon garantisi kaybolurdu.
- **Simulation = kalıtımlı DAVRANIŞ** — Strategy + Template Method. İstenen hiyerarşi burada,
  davranışın gerçekten yaşadığı katmanda kurulur:

```csharp
namespace EmberCrpg.Simulation.Living.Actions
{
    /// <summary>Bir action tipini bir advancement adım ilerletir. STATE TAŞIMAZ (LSP + determinizm):
    /// tüm durum ActorActionState + WorldState'tedir; advancer'lar tekil ve yeniden kullanılabilir.</summary>
    public interface IActionAdvancer
    {
        ActorActionType Handles { get; }
        ActorActionState Advance(WorldState world, ActorRecord actor, ActorActionState state, GameTime now);
    }

    // Template Method: doğrulama -> adım -> LOG sırası burada SABİTLENİR; alt sınıf log'u atlayamaz.
    public abstract class ActionAdvancer : IActionAdvancer
    {
        public abstract ActorActionType Handles { get; }

        public ActorActionState Advance(WorldState world, ActorRecord actor, ActorActionState state, GameTime now)
        {
            var next = Step(world, actor, state, now);   // tek soyut nokta
            ActionLogManager.Phase(actor, state, next, now); // CONSTRAINT: her faz geçişi tek kanaldan
            return next;
        }

        protected abstract ActorActionState Step(WorldState world, ActorRecord actor, ActorActionState state, GameTime now);
    }

    public sealed class MoveToFoodAdvancer : ActionAdvancer { /* Handles = MoveToFood */ }
    public sealed class TakeFoodAdvancer   : ActionAdvancer { /* Handles = TakeFood   */ }
    public sealed class ConsumeFoodAdvancer: ActionAdvancer { /* Handles = ConsumeFood*/ }
}
```

- `ActionAdvancerRegistry`: `Dictionary<ActorActionType, IActionAdvancer>` — kayıt sırası sonucu
  etkilemez (tip bazlı lookup), determinizm riski yok. Yeni fiil = 1 enum üyesi + 1 alt sınıf +
  1 registry satırı; save/digest tarafı int olduğundan HİÇ değişmez (OCP).
- Dünya mutasyonu: advancer'lar `WorldState`'i keyfî yazmaz; teşhis §6'daki dar doğrulanmış
  operasyonları (`TryTakeReserved`, `TryCompleteConsume` …) çağırır. Operasyon yüzeyinin tasarımı
  W32/03'tedir; buradaki sözleşme "advancer yalnızca `ApplyActionState` + validated op" kuralıdır.
- UI: `DomainSimulationAdapter.WorldProjection` tahmin bloğu (`WorldProjection.cs:108-142`) yerine
  `actor.ActionState.CurrentAction` → etiket tablosu (birebir; teşhis §10 kabulü).

---

## 7. LogManager: tek kanal

`Assets/Scripts/Simulation/Diagnostics/ActionLogManager.cs` — mevcut `EmberLog` dikişinin
(`EmberLog.cs:12`, sink Presentation'da bir kez atanır, headless'ta sessiz) üstünde dar bir yüz:

```csharp
// CONSTRAINT (observability != behavior): logging must never branch simulation. Sink may be null.
// CONSTRAINT (single channel): advancers/lifecycle NEVER call EmberLog directly for phases;
// the ActionAdvancer template method is the only producer, so a subclass cannot forget to log.
public static class ActionLogManager
{
    static readonly EmberLogger Log = EmberLog.For("Action");

    public static void Phase(ActorRecord actor, ActorActionState before, ActorActionState after, GameTime now)
    {
        if (before.CurrentAction == after.CurrentAction && before.Phase == after.Phase) return; // sadece geçişler
        Log.Info($"t={now.TotalMinutes} actor={actor.Id.Value} intent={after.CurrentIntent} " +
                 $"action={before.CurrentAction}->{after.CurrentAction} phase={before.Phase}->{after.Phase} " +
                 $"item={after.TargetItemId.Value} site={after.TargetSiteId.Value} res={after.ReservationId.Value} " +
                 $"prog={after.ProgressTicks} fail={after.FailureReason}");
    }

    public static void Warn(string what, ActorId actor) => Log.Warn(what + " actor=" + actor.Value);
}
```

Format sabittir; proof harness bu satırları grep'leyebilir (render-katmanı doğrulamasının yerine
geçmez — MEMORY: "verify at render layer" — ama zincir kanıtının veri ayağıdır:
`IntentChosen → Reserved → Arrived → Taken → Consumed`).

---

## 8. EAT dışına genellemeler (ŞİMDİ YAPILMAZ)

- **Plan / ActionQueue**: EAT'te zincir (`MoveToFood → TakeFood → ConsumeFood`) intent'ten türeyen
  sabit boru hattıdır ve `ActionLifecycleSystem` içinde koddadır — kuyruk SAVE EDİLMEZ. Çok adımlı
  planlar (iş, hasat-taşıma) geldiğinde `ActorSaveData`'ya `plannedActions:int[]` eklenir; sıfır-blok
  kuralı korunur.
- **Rezervasyon defteri**: `ReservationLedger` + deterministik `NextReservationId` sayacı
  `WorldState`'e alan olarak gelecek — O ZAMAN `CopyFrom` + `EnsureInvariants` + `WorldSaveData`
  paralel dizileri (`reservationIds/actorIds/siteIds/itemTags/expiresAtMinutes`) zorunlu (W32/03).
- **Intent çeşitlenmesi**: `Sleep`, `Work`, `Flee` intent'leri geldikçe karar ayrı `DecisionSystem`'e
  çıkar; intent önerileri command listesiyle taşınır, `Actor.ActionState` yazarı yine tek kalır.
- **Zaman aşımı emniyeti**: `ActionLifecycleSystem` `now - StartedAtMinutes > 1440` (1 oyun günü)
  gördüğünde `Failed(TimedOut)` üretir — W30e "donmuş maraton" sınıfı kilitlenmeler deterministik
  ve loglu biter. Sabit, sistemde yaşar; struct'ta değil.
- **InterruptPolicy zenginleşmesi**: bugün iki değer; savaş kesmeleri geldiğinde
  `InterruptibleBy(threat-level)` benzeri genişleme enum'a YENİ değer ekleyerek yapılır,
  mevcut değerler yeniden numaralanmaz.

## 9. Kabul (bu belgenin kapsamındaki kısım)

- Roundtrip: Idle ve dolu `ActorActionState` alan-alan aynı döner (golden + odak test, §3.4).
- Eski save (sıfır blok) → Idle yüklenir, hiçbir migration dalı çalışmaz.
- Aynı seed + tick → aynı digest; digest artık zihin alanlarını içerir (§4).
- Eylem tickler arasında aynı action + faz ile sürer (teşhis §10) — struct kalıcılığı bunu taşır.
- Her faz geçişi `[Action]` etiketiyle tek kanaldan loglanır; advancer'da doğrudan `EmberLog` yoktur.
