using System;
using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using ReadOnlyAttribute = Unity.Collections.ReadOnlyAttribute;
using Unity.Entities;
using Unity.Collections.LowLevel.Unsafe;

namespace Chisel.Core
{
    [BurstCompile(CompileSynchronously = true)]
    struct FindBrushPairsJob : IJob
    {
        struct Empty { }

        [NoAlias, ReadOnly] public NativeArray<IndexOrder>                                  treeBrushIndexOrders;
        [NoAlias, ReadOnly] public NativeArray<BlobAssetReference<BrushesTouchedByBrush>>   brushesTouchedByBrushes;
        [NoAlias, WriteOnly] public NativeList<BrushPair>                                   uniqueBrushPairs;

        // Per thread scratch memory
        [NativeDisableContainerSafetyRestriction] NativeHashMap<BrushPair, Empty> brushPairMap;

        public void Execute()
        {
            var maxPairs = GeometryMath.GetTriangleArraySize(treeBrushIndexOrders.Length);

            if (!brushPairMap.IsCreated)
            {
                brushPairMap = new NativeHashMap<BrushPair, FindBrushPairsJob.Empty>(maxPairs, Allocator.Temp);
            } else
            {
                brushPairMap.Clear();
                if (brushPairMap.Capacity < maxPairs)
                    brushPairMap.Capacity = maxPairs;
            }

            var empty = new Empty();
            for (int b0 = 0; b0 < treeBrushIndexOrders.Length; b0++)
            {
                var brushIndexOrder0        = treeBrushIndexOrders[b0];
                int brushNodeOrder0         = brushIndexOrder0.nodeOrder;

                var brushesTouchedByBrush   = brushesTouchedByBrushes[brushNodeOrder0];
                if (brushesTouchedByBrush == BlobAssetReference<BrushesTouchedByBrush>.Null)
                    continue;
                    
                ref var intersections = ref brushesTouchedByBrush.Value.brushIntersections;
                if (intersections.Length == 0)
                    continue;

                // Find all intersections between brushes
                for (int i = 0; i < intersections.Length; i++)
                {
                    var intersection        = intersections[i];
                    var brushIndexOrder1    = intersection.nodeIndexOrder;
                    int brushNodeOrder1     = brushIndexOrder1.nodeOrder;

                    var brushPair       = new BrushPair
                    {
                        type             = intersection.type,
                        brushIndexOrder0 = brushIndexOrder0,
                        brushIndexOrder1 = brushIndexOrder1
                    };

                    if (brushNodeOrder0 > brushNodeOrder1) // ensures we do calculations exactly the same for each brush pair
                        brushPair.Flip();

                    if (brushPairMap.TryAdd(brushPair, empty))
                    {
                        uniqueBrushPairs.AddNoResize(brushPair);
                    }
                }
            }
            brushPairMap.Dispose();
        }
    }
}
