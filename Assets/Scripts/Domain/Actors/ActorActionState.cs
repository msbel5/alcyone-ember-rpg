using System;
using EmberCrpg.Domain.Core;

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
    /// <remarks>Append-only (save'e int yazılır). SourceDrained: rezervasyona rağmen pile harici yazarca boşaldı (W32-03 §7).</remarks>
    public enum ActionFailureReason { None = 0, NoFoodFound = 1, ReservationLost = 2, Unreachable = 3, Interrupted = 4, TimedOut = 5, SourceDrained = 6 }

    /// <summary>Karar sistemi yeni intent atamadan önce buna bakmak ZORUNDADIR.</summary>
    public enum ActionInterruptPolicy { Interruptible = 0, NonInterruptible = 1 }

    // Design note:
    // ActorActionState is the actor's persistent mind for W32: intent + current action + phase.
    // CONSTRAINT (save/backward-compat): default(ActorActionState) MUST equal Idle and MUST be
    // the all-zero bit pattern — pre-W32 saves deserialize missing fields to 0 and load as Idle.
    // CONSTRAINT (determinism): pure data, no Unity/IO/RNG; transitions are pure functions.
    /// <summary>Kalıcı zihin durumu: niyet + mevcut eylem + faz + hedefler + ilerleme.</summary>
    public readonly struct ActorActionState : IEquatable<ActorActionState>
    {
        // CONSTRAINT ("None => all zero"): with CurrentAction == None every action-scoped field
        // must be zero (CurrentIntent may be set: the ForIntent handover). Silent fixup would be
        // a determinism leak, so violations die loudly here and the load path normalizes to Idle.
        private ActorActionState(
            ActorIntent currentIntent,
            ActorActionType currentAction,
            ActionPhase phase,
            ItemId targetItemId,
            SiteId targetSiteId,
            ReservationId reservationId,
            int progressTicks,
            long startedAtMinutes,
            ActionFailureReason failureReason,
            ActionInterruptPolicy interruptPolicy)
        {
            if (currentAction == ActorActionType.None
                && (phase != ActionPhase.None || !targetItemId.IsEmpty || !targetSiteId.IsEmpty
                    || !reservationId.IsEmpty || progressTicks != 0 || startedAtMinutes != 0L
                    || failureReason != ActionFailureReason.None))
                throw new InvalidOperationException("ActorActionState invariant: None action requires all action fields zero.");

            CurrentIntent = currentIntent;
            CurrentAction = currentAction;
            Phase = phase;
            TargetItemId = targetItemId;
            TargetSiteId = targetSiteId;
            ReservationId = reservationId;
            ProgressTicks = progressTicks;
            StartedAtMinutes = startedAtMinutes;
            FailureReason = failureReason;
            InterruptPolicy = interruptPolicy;
        }

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

        /// <summary>Idle -> intent seçildi (action henüz None).</summary>
        public static ActorActionState ForIntent(ActorIntent intent)
        {
            if (intent == ActorIntent.None)
                throw new InvalidOperationException("ForIntent requires a non-None intent.");
            return new ActorActionState(intent, ActorActionType.None, ActionPhase.None,
                default(ItemId), default(SiteId), ReservationId.Empty, 0, 0L,
                ActionFailureReason.None, ActionInterruptPolicy.Interruptible);
        }

        /// <summary>-> Running, Progress=0. Geçerli: intent seçili ve action None ya da terminal fazda.</summary>
        public ActorActionState Start(ActorActionType action, SiteId targetSite,
            ItemId targetItem, ReservationId reservation, long startedAtMinutes,
            ActionInterruptPolicy policy)
        {
            if (action == ActorActionType.None)
                throw new InvalidOperationException("Start requires a non-None action.");
            if (CurrentIntent == ActorIntent.None)
                throw new InvalidOperationException("Start requires an intent (ForIntent first).");
            if (Phase == ActionPhase.Running)
                throw new InvalidOperationException($"Cannot start {action} while {CurrentAction} is Running.");
            return new ActorActionState(CurrentIntent, action, ActionPhase.Running,
                targetItem, targetSite, reservation, 0, startedAtMinutes,
                ActionFailureReason.None, policy);
        }

        /// <summary>Running -> Running, Progress+1.</summary>
        public ActorActionState Advanced()
        {
            if (Phase != ActionPhase.Running)
                throw new InvalidOperationException($"Advanced requires Running, was {Phase}.");
            return new ActorActionState(CurrentIntent, CurrentAction, ActionPhase.Running,
                TargetItemId, TargetSiteId, ReservationId, ProgressTicks + 1, StartedAtMinutes,
                ActionFailureReason.None, InterruptPolicy);
        }

        /// <summary>Running -> Succeeded.</summary>
        public ActorActionState Succeeded()
        {
            if (Phase != ActionPhase.Running)
                throw new InvalidOperationException($"Succeeded requires Running, was {Phase}.");
            return new ActorActionState(CurrentIntent, CurrentAction, ActionPhase.Succeeded,
                TargetItemId, TargetSiteId, ReservationId, ProgressTicks, StartedAtMinutes,
                ActionFailureReason.None, InterruptPolicy);
        }

        /// <summary>Running -> Failed(reason).</summary>
        public ActorActionState Failed(ActionFailureReason reason)
        {
            if (Phase != ActionPhase.Running)
                throw new InvalidOperationException($"Failed requires Running, was {Phase}.");
            if (reason == ActionFailureReason.None)
                throw new InvalidOperationException("Failed requires a concrete reason.");
            return new ActorActionState(CurrentIntent, CurrentAction, ActionPhase.Failed,
                TargetItemId, TargetSiteId, ReservationId, ProgressTicks, StartedAtMinutes,
                reason, InterruptPolicy);
        }

        /// <summary>TakeFood başarısında hedef item'ı bağlar.</summary>
        public ActorActionState CarryingItem(ItemId item)
        {
            if (CurrentAction == ActorActionType.None)
                throw new InvalidOperationException("CarryingItem requires an active action.");
            if (item.IsEmpty)
                throw new InvalidOperationException("CarryingItem requires a non-empty item.");
            return new ActorActionState(CurrentIntent, CurrentAction, Phase,
                item, TargetSiteId, ReservationId, ProgressTicks, StartedAtMinutes,
                FailureReason, InterruptPolicy);
        }

        /// <summary>
        /// Save yolu materyalizasyonu: enum aralıkları + "None => all zero" invariantı sağlanmıyorsa
        /// false döner (mapper Idle'a normalize eder — bozuk blok sessizce yarım yüklenmez).
        /// </summary>
        public static bool TryRestore(ActorIntent intent, ActorActionType action, ActionPhase phase,
            ItemId targetItem, SiteId targetSite, ReservationId reservation,
            int progressTicks, long startedAtMinutes,
            ActionFailureReason failureReason, ActionInterruptPolicy policy,
            out ActorActionState state)
        {
            state = Idle;
            if (intent < ActorIntent.None || intent > ActorIntent.Eat) return false;
            if (action < ActorActionType.None || action > ActorActionType.ConsumeFood) return false;
            if (phase < ActionPhase.None || phase > ActionPhase.Failed) return false;
            if (failureReason < ActionFailureReason.None || failureReason > ActionFailureReason.SourceDrained) return false;
            if (policy < ActionInterruptPolicy.Interruptible || policy > ActionInterruptPolicy.NonInterruptible) return false;
            if (progressTicks < 0 || startedAtMinutes < 0L) return false;
            if (action == ActorActionType.None
                && (phase != ActionPhase.None || !targetItem.IsEmpty || !targetSite.IsEmpty
                    || !reservation.IsEmpty || progressTicks != 0 || startedAtMinutes != 0L
                    || failureReason != ActionFailureReason.None))
                return false;
            // A started action always carries an owning intent and a live phase; a failure
            // reason only exists in the Failed phase. Anything else is transition-unreachable.
            if (action != ActorActionType.None && (intent == ActorIntent.None || phase == ActionPhase.None)) return false;
            if (failureReason != ActionFailureReason.None && phase != ActionPhase.Failed) return false;

            state = new ActorActionState(intent, action, phase, targetItem, targetSite,
                reservation, progressTicks, startedAtMinutes, failureReason, policy);
            return true;
        }

        /// <summary>Returns true when both states carry the same mind fields.</summary>
        public bool Equals(ActorActionState other)
        {
            return CurrentIntent == other.CurrentIntent
                && CurrentAction == other.CurrentAction
                && Phase == other.Phase
                && TargetItemId == other.TargetItemId
                && TargetSiteId == other.TargetSiteId
                && ReservationId == other.ReservationId
                && ProgressTicks == other.ProgressTicks
                && StartedAtMinutes == other.StartedAtMinutes
                && FailureReason == other.FailureReason
                && InterruptPolicy == other.InterruptPolicy;
        }

        /// <summary>Returns true when the object is an action state with the same mind fields.</summary>
        public override bool Equals(object obj)
        {
            return obj is ActorActionState other && Equals(other);
        }

        /// <summary>Returns a hash code derived from all mind fields.</summary>
        public override int GetHashCode()
        {
            return HashCode.Combine(
                HashCode.Combine(CurrentIntent, CurrentAction, Phase, TargetItemId, TargetSiteId, ReservationId),
                HashCode.Combine(ProgressTicks, StartedAtMinutes, FailureReason, InterruptPolicy));
        }

        /// <summary>Returns a compact debug label for this action state.</summary>
        public override string ToString()
        {
            return IsIdle ? "ActorActionState.Idle" : $"ActorActionState({CurrentIntent}, {CurrentAction}, {Phase})";
        }

        /// <summary>Returns true when states carry the same mind fields.</summary>
        public static bool operator ==(ActorActionState left, ActorActionState right)
        {
            return left.Equals(right);
        }

        /// <summary>Returns true when states carry different mind fields.</summary>
        public static bool operator !=(ActorActionState left, ActorActionState right)
        {
            return !left.Equals(right);
        }
    }
}
