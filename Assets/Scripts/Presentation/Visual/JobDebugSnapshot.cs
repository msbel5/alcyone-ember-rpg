using System.Collections.Generic;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;

namespace EmberCrpg.Presentation.Visual
{
    /// <summary>
    /// Read-only snapshot of pending and claimed jobs for Unity debug surfaces.
    /// Pure C#: no UnityEngine, no mutation. Phase 11 Atom 1.
    /// </summary>
    public sealed class JobDebugSnapshot
    {
        private readonly IReadOnlyList<JobDebugRow> _rows;

        public JobDebugSnapshot(IReadOnlyList<JobDebugRow> rows)
        {
            _rows = rows ?? new JobDebugRow[0];
        }

        public IReadOnlyList<JobDebugRow> Rows => _rows;

        public static JobDebugSnapshot FromStores(ActorStore actors, JobBoard board, WorksiteStore worksites)
        {
            var rows = new List<JobDebugRow>();
            foreach (var request in board.Requests)
            {
                var actorId = board.GetClaimedBy(request.Id);
                var status = board.GetStatus(request.Id);
                var queueIndex = board.GetQueueIndex(request.Id);

                string actorName = string.Empty;
                if (!actorId.IsEmpty && actors != null)
                {
                    var actor = actors.Get(actorId);
                    if (actor != null)
                        actorName = actor.Name ?? string.Empty;
                }

                string worksiteTag = WorksiteTagFor(request, worksites);

                rows.Add(new JobDebugRow(
                    actorId,
                    request.Id,
                    actorName,
                    request.Kind.ToString(),
                    status.Code,
                    worksiteTag,
                    queueIndex));
            }
            return new JobDebugSnapshot(rows);
        }

        private static string WorksiteTagFor(JobRequest request, WorksiteStore worksites)
        {
            // Codex review (2026-05-21): previously the worksites parameter was
            // ignored in both branches — dead abstraction. Resolve the actual
            // worksite at the request's coordinates so the debug HUD can
            // distinguish "no worksite there" from "worksite exists but is
            // inactive" instead of always echoing the raw WorksiteKind.
            var baseLabel = request.WorksiteKind.ToString();
            if (worksites == null) return baseLabel;
            if (!worksites.TryGet(request.SiteId, request.WorksitePosition, out var record))
                return baseLabel + "(missing)";
            return record.IsActive ? baseLabel : baseLabel + "(inactive)";
        }
    }

    /// <summary>
    /// One row in <see cref="JobDebugSnapshot"/>. Carries everything a debug
    /// HUD needs without exposing live simulation objects.
    /// </summary>
    public readonly struct JobDebugRow
    {
        public JobDebugRow(
            ActorId actorId,
            JobId jobId,
            string actorName,
            string jobKindCode,
            string statusCode,
            string worksiteTag,
            int queueIndex)
        {
            ActorId = actorId;
            JobId = jobId;
            ActorName = actorName ?? string.Empty;
            JobKindCode = jobKindCode ?? string.Empty;
            StatusCode = statusCode ?? string.Empty;
            WorksiteTag = worksiteTag ?? string.Empty;
            QueueIndex = queueIndex;
        }

        public ActorId ActorId { get; }
        public JobId JobId { get; }
        public string ActorName { get; }
        public string JobKindCode { get; }
        public string StatusCode { get; }
        public string WorksiteTag { get; }
        public int QueueIndex { get; }
    }
}
