// Design note:
// CombatInputAdapter maps local Unity inputs into the pure RTWP combat scheduler.
// Inputs: Space/mouse/keyboard buttons plus Time.deltaTime.
// Outputs: queued combat actions and scheduler tick events for later UI/animation hooks.
// Bible reference: ARCHITECTURE.md RTWP combat lock, combat playground presentation seam.
using EmberCrpg.Domain.Combat;
using EmberCrpg.Domain.Core;
using EmberCrpg.Simulation.Combat;
using UnityEngine;
using EmberCrpg.Presentation.Ember.Inputs;

namespace EmberCrpg.Presentation.Combat
{
    /// <summary>Thin Unity input adapter that queues RTWP combat actions into pure simulation state.</summary>
    public sealed class CombatInputAdapter : MonoBehaviour
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
            if (EmberInput.KeyDown(pauseKey))
                combatState.TogglePaused();

            if (EmberInput.AttackClick)
                Queue(CombatActionKind.MeleeSwing);
            if (EmberInput.SecondaryClick)
                Queue(CombatActionKind.Block);
            if (EmberInput.KeyDown(dodgeKey))
                Queue(CombatActionKind.Dodge);
            if (EmberInput.KeyDown(castKey))
                Queue(CombatActionKind.Cast);

            lastTick = scheduler.Tick(combatState, Time.deltaTime);
        }

        public QueuedCombatAction Queue(CombatActionKind kind)
        {
            return scheduler.QueueAction(combatState, new ActorId((ulong)Mathf.Max(1, localActorId)), kind, new ActorId((ulong)Mathf.Max(0, defaultTargetActorId)));
        }
    }
}
