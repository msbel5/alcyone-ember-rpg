// Design note:
// Sprint4CombatInputAdapter maps local Unity inputs into the pure RTWP combat scheduler.
// Inputs: Space/mouse/keyboard buttons plus Time.deltaTime.
// Outputs: queued combat actions and scheduler tick events for later UI/animation hooks.
// Bible reference: ARCHITECTURE.md RTWP combat lock, Sprint 4 Faz 2 presentation seam.
using EmberCrpg.Domain.Combat;
using EmberCrpg.Domain.Core;
using EmberCrpg.Simulation.Combat;
using UnityEngine;

namespace EmberCrpg.Presentation.Sprint4
{
    /// <summary>Thin Unity input adapter that queues Sprint 4 RTWP combat actions into pure simulation state.</summary>
    public sealed class Sprint4CombatInputAdapter : MonoBehaviour
    {
        [Header("Actor Handles")]
        [SerializeField] private int localActorId = 1;
        [SerializeField] private int defaultTargetActorId = 2;

        [Header("Input")]
        [SerializeField] private KeyCode pauseKey = KeyCode.Space;
        [SerializeField] private KeyCode dodgeKey = KeyCode.LeftShift;
        [SerializeField] private KeyCode castKey = KeyCode.Alpha1;

        private readonly RealtimeCombatState combatState = new RealtimeCombatState();
        private readonly RealtimeCombatActionScheduler scheduler = new RealtimeCombatActionScheduler();
        private RealtimeCombatTickResult lastTick = new RealtimeCombatTickResult();

        public RealtimeCombatState CombatState => combatState;
        public RealtimeCombatTickResult LastTick => lastTick;

        private void Update()
        {
            if (Input.GetKeyDown(pauseKey))
                combatState.TogglePaused();

            if (Input.GetMouseButtonDown(0))
                Queue(CombatActionKind.MeleeSwing);
            if (Input.GetMouseButtonDown(1))
                Queue(CombatActionKind.Block);
            if (Input.GetKeyDown(dodgeKey))
                Queue(CombatActionKind.Dodge);
            if (Input.GetKeyDown(castKey))
                Queue(CombatActionKind.Cast);

            lastTick = scheduler.Tick(combatState, Time.deltaTime);
        }

        public QueuedCombatAction Queue(CombatActionKind kind)
        {
            return scheduler.QueueAction(combatState, new ActorId((ulong)Mathf.Max(1, localActorId)), kind, new ActorId((ulong)Mathf.Max(0, defaultTargetActorId)));
        }
    }
}
