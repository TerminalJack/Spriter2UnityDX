// Modifications Copyright (c) 2026 TerminalJack
// Licensed under the MIT License. See the LICENSE.TXT file in the project root for details.
//
// Portions of this file are derived from the Spriter2UnityDX project.
// The original author provided an open-use permission statement, preserved in THIRD_PARTY_NOTICES.md.

using System.Collections;
using UnityEngine;
using System.Linq;

namespace Stui
{
    using Importing;
    using EntityInfo;

    [CreateAssetMenu(fileName = "NewZIndicesAnimationTask", menuName = "Inspection Tasks/Animation Z-Indices Task", order = 5)]
    public class ZIndicesAnimationTask : ScmlInspectorAnimationTask
    {
        public override IEnumerator ProcessAnimation(ScmlObject scmlObject, SpriterEntityInfo entityInfo, Entity entity,
            Animation animation, IBuildTaskContext inspectionCtx)
        {
            yield return $"Entity: '{entity.name}', animation: '{animation.name}', z-index/visibility info:";

            var zIndexInfos =
            (
                from timeline in animation.timelines
                where timeline.objectType == ObjectType.sprite
                from mlk in animation.mainlineKeys

                // Find the timeline key (if any) that this mainline key references
                let tlk =
                    (from k in timeline.keys
                    where mlk.objectRefs.Any(or =>
                        or.timelineId == timeline.id &&
                        or.timelineKeyId == k.id)
                    select k
                    ).FirstOrDefault()

                // Find the matching objectRef (if any) so we can get z_index
                let oref =
                    mlk.objectRefs.FirstOrDefault(or =>
                        or.timelineId == timeline.id &&
                        or.timelineKeyId == tlk?.id)

                select new
                {
                    timelineName = timeline.name,
                    mlk.time_s,
                    tlk,  // may be null
                    isVisible = tlk != null, // Does this sprite exist at this time?
                    sortingOrder = tlk != null && oref != null ? Ref.ZIndexToSortingOrder(oref.z_index) : -1
                }
            )
            .ToList();

            // Group them by animation and timeline.
            var groupedKeyZIndex = zIndexInfos
                .GroupBy(entry => new { entry.timelineName })
                .Select(g => new
                {
                    g.Key.timelineName,

                    infos = g.Select(x => new
                    {
                        x.time_s,
                        x.isVisible,
                        x.sortingOrder
                    }).ToList()
                })
                .ToList();

            foreach (var timelineGroup in groupedKeyZIndex)
            {
                var timelineName = timelineGroup.timelineName;
                var infos = timelineGroup.infos;  // List of { time_s, isVisible, sortingOrder }

                yield return "----------";

                foreach (var info in infos)
                {
                    var isVisibleString = info.isVisible ? "yes" : "no";
                    var sortingOrderString = info.isVisible ? info.sortingOrder.ToString() : "N/A";

                    yield return $"   timelineName: {timelineName}, time: {info.time_s}, isVisible?: {isVisibleString}, " +
                        $"sortingOrder: {sortingOrderString}";
                }
            }
        }
    }
}