using System.Collections.Generic;
using EmberCrpg.Domain.Inventory;

namespace EmberCrpg.Presentation.VisualLayer
{
    /// <summary>
    /// Read-only snapshot of inventory contents aggregated by templateId for
    /// Unity HUD stockpile readouts. Pure C#: no UnityEngine, no mutation.
    /// Faz 11 Atom 6.
    /// </summary>
    public sealed class InventoryStockpileSnapshot
    {
        private readonly IReadOnlyList<StockpileRow> _rows;

        public InventoryStockpileSnapshot(IReadOnlyList<StockpileRow> rows)
        {
            // PR#150 bot review fix: defensively copy so a caller that keeps and
            // mutates the original list cannot retroactively change the
            // snapshot's contents (e.g. a recycled buffer reused on the next
            // tick). Snapshots are immutable view models by contract.
            if (rows == null)
            {
                _rows = new StockpileRow[0];
                return;
            }
            var copy = new StockpileRow[rows.Count];
            for (int i = 0; i < rows.Count; i++)
                copy[i] = rows[i];
            _rows = copy;
        }

        public IReadOnlyList<StockpileRow> Rows => _rows;

        public static InventoryStockpileSnapshot FromInventory(InventoryState inventory)
        {
            if (inventory == null)
                return new InventoryStockpileSnapshot(new StockpileRow[0]);

            // Aggregate by template id; preserve deterministic first-seen order.
            var index = new Dictionary<string, int>();
            var ordered = new List<(string template, string display, int qty)>();
            foreach (var item in inventory.Items)
            {
                if (item == null) continue;
                var key = item.TemplateId ?? string.Empty;
                if (index.TryGetValue(key, out var i))
                {
                    var existing = ordered[i];
                    ordered[i] = (existing.template, existing.display, existing.qty + item.Quantity);
                }
                else
                {
                    index[key] = ordered.Count;
                    ordered.Add((key, item.DisplayName ?? key, item.Quantity));
                }
            }

            var rows = new List<StockpileRow>(ordered.Count);
            foreach (var row in ordered)
                rows.Add(new StockpileRow(row.template, row.display, row.qty));
            return new InventoryStockpileSnapshot(rows);
        }
    }

    /// <summary>One aggregated stockpile entry: how many of one templateId are held.</summary>
    public readonly struct StockpileRow
    {
        public StockpileRow(string templateId, string displayName, int quantity)
        {
            TemplateId = templateId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Quantity = quantity;
        }

        public string TemplateId { get; }
        public string DisplayName { get; }
        public int Quantity { get; }
    }
}
