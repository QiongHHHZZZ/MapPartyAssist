using Lumina.Excel.Sheets;
using MapPartyAssist.Types;
using System;
using System.Collections.Generic;

namespace MapPartyAssist.Helper {
    internal static class LootAggregationHelper {
        internal static int Accumulate(
            Dictionary<LootResultKey, LootResultValue> summary,
            LootResult loot,
            bool selfObtained,
            int participantCount,
            Func<uint, Item?> itemResolver,
            Func<LootResultKey, int?> priceResolver) {

            if(summary is null) {
                throw new ArgumentNullException(nameof(summary));
            }

            participantCount = Math.Max(1, participantCount);

            bool isGil = loot.ItemId == 1;
            int droppedQuantity = isGil ? 1 : loot.Quantity;
            int obtainedQuantity = selfObtained ? (isGil ? 1 : loot.Quantity) : 0;

            var key = new LootResultKey {
                ItemId = loot.ItemId,
                IsHQ = loot.IsHQ
            };

            int? price = priceResolver(key);
            if(isGil) {
                price = loot.Quantity;
            }

            int? droppedValueContribution = price.HasValue
                ? (isGil ? price.Value * participantCount : price.Value * loot.Quantity)
                : null;
            int? obtainedValueContribution = price.HasValue
                ? price.Value * obtainedQuantity
                : null;

            if(!summary.TryGetValue(key, out var aggregate)) {
                var itemRow = itemResolver(loot.ItemId);
                if(itemRow is null) {
                    return CalculateContribution();
                }

                aggregate = new LootResultValue {
                    DroppedQuantity = droppedQuantity,
                    ObtainedQuantity = obtainedQuantity,
                    Rarity = itemRow.Value.Rarity,
                    ItemName = itemRow.Value.Name.ToString(),
                    Category = itemRow.Value.ItemUICategory.Value.Name.ToString(),
                    AveragePrice = price,
                    DroppedValue = droppedValueContribution,
                    ObtainedValue = obtainedValueContribution,
                };

                summary.Add(key, aggregate);
            } else {
                aggregate.DroppedQuantity += droppedQuantity;
                aggregate.ObtainedQuantity += obtainedQuantity;

                if(price.HasValue) {
                    aggregate.AveragePrice = price;
                    aggregate.DroppedValue = (aggregate.DroppedValue ?? 0) + (droppedValueContribution ?? 0);
                    aggregate.ObtainedValue = (aggregate.ObtainedValue ?? 0) + (obtainedValueContribution ?? 0);
                }
            }

            return CalculateContribution();

            int CalculateContribution() {
                if(isGil) {
                    return participantCount * loot.Quantity;
                }

                return (price ?? 0) * loot.Quantity;
            }
        }
    }
}
