using System.Collections.Generic;
using EmberCrpg.Domain.Actors;

// Design note:
// DungeonRoom describes one node in the deterministic Sprint 4 dungeon graph.
// Inputs: generated id, graph coordinate, dimensions, template, and connected door ids.
// Outputs: pure room metadata for traversal, placement, and save/load tests.
// Bible reference: MASTER_MECHANICS_BIBLE.md §40/§41, Sprint 4 Faz 3 multi-room scope.
namespace EmberCrpg.Domain.World
{
    /// <summary>Pure generated room node with local-grid bounds.</summary>
    public sealed class DungeonRoom
    {
        public int Id;
        public int GridX;
        public int GridY;
        public int Width;
        public int Height;
        public string TemplateId;
        public List<int> DoorIds = new List<int>();

        public bool IsWalkable(GridPosition position)
        {
            return position.X > 0 && position.X < Width - 1 && position.Y > 0 && position.Y < Height - 1;
        }
    }
}
