using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Collections;

namespace Chisel.Core
{
    public struct IndexOrder : IEquatable<IndexOrder>
    {
        public CompactNodeID compactNodeID;
        public int nodeOrder;

        public bool Equals(IndexOrder other)
        {
            return compactNodeID == other.compactNodeID;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is IndexOrder))
                return false;
            return Equals((IndexOrder)obj);
        }

        public override int GetHashCode()
        {
            return compactNodeID.GetHashCode();
        }
    }

    public struct BrushPair : IEquatable<BrushPair>, IEqualityComparer<BrushPair>, IComparable<BrushPair>, IComparer<BrushPair>
    {
        public int              brushNodeOrder0;
        public int              brushNodeOrder1;
        public IntersectionType type;

        public void Flip()
        {
            if      (type == IntersectionType.AInsideB) type = IntersectionType.BInsideA;
            else if (type == IntersectionType.BInsideA) type = IntersectionType.AInsideB;
            { var t = brushNodeOrder0; brushNodeOrder0 = brushNodeOrder1; brushNodeOrder1 = t; }
        }

        #region Equals
        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is BrushPair))
                return false;

            var other = (BrushPair)obj;
            return Equals(other);
        }

        public bool Equals(BrushPair x, BrushPair y) { return x.Equals(y); }

        public bool Equals(BrushPair other)
        {
            return ((brushNodeOrder0 == other.brushNodeOrder0) && 
                    (brushNodeOrder1 == other.brushNodeOrder1));
        }
        #endregion

        #region Compare
        public int Compare(BrushPair x, BrushPair y) { return x.CompareTo(y); }

        public int CompareTo(BrushPair other)
        {
            if (brushNodeOrder0 < other.brushNodeOrder0)
                return -1;
            if (brushNodeOrder0 > other.brushNodeOrder0)
                return 1;
            if (brushNodeOrder1 < other.brushNodeOrder1)
                return -1;
            if (brushNodeOrder1 > other.brushNodeOrder1)
                return 1;
            if (type < other.type)
                return -1;
            if (type > other.type)
                return 1;
            return 0;
        }
        #endregion

        #region GetHashCode
        public override int GetHashCode()
        {
            return GetHashCode(this);
        }

        public int GetHashCode(BrushPair obj)
        {
            return ((ulong)obj.brushNodeOrder0 + ((ulong)obj.brushNodeOrder1 << 32)).GetHashCode();
        }
        #endregion
    }
    
    public struct BrushIntersectWith
    {
        public int              brushNodeOrder1;
        public IntersectionType type;
    }

    public struct BrushPair2 : IEquatable<BrushPair2>, IEqualityComparer<BrushPair2>, IComparable<BrushPair2>, IComparer<BrushPair2>
    {
        public IndexOrder       brushIndexOrder0;
        public IndexOrder       brushIndexOrder1;
        public IntersectionType type;

        public void Flip()
        {
            if      (type == IntersectionType.AInsideB) type = IntersectionType.BInsideA;
            else if (type == IntersectionType.BInsideA) type = IntersectionType.AInsideB;
            { var t = brushIndexOrder0; brushIndexOrder0 = brushIndexOrder1; brushIndexOrder1 = t; }
        }

        #region Equals
        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is BrushPair2))
                return false;

            var other = (BrushPair2)obj;
            return Equals(other);
        }

        public bool Equals(BrushPair2 x, BrushPair2 y) { return x.Equals(y); }

        public bool Equals(BrushPair2 other)
        {
            return ((brushIndexOrder0.nodeOrder == other.brushIndexOrder0.nodeOrder) && 
                    (brushIndexOrder1.nodeOrder == other.brushIndexOrder1.nodeOrder));
        }
        #endregion

        #region Compare
        public int Compare(BrushPair2 x, BrushPair2 y) { return x.CompareTo(y); }

        public int CompareTo(BrushPair2 other)
        {
            if (brushIndexOrder0.nodeOrder < other.brushIndexOrder0.nodeOrder)
                return -1;
            if (brushIndexOrder0.nodeOrder > other.brushIndexOrder0.nodeOrder)
                return 1;
            if (brushIndexOrder1.nodeOrder < other.brushIndexOrder1.nodeOrder)
                return -1;
            if (brushIndexOrder1.nodeOrder > other.brushIndexOrder1.nodeOrder)
                return 1;
            if (type < other.type)
                return -1;
            if (type > other.type)
                return 1;
            return 0;
        }
        #endregion

        #region GetHashCode
        public override int GetHashCode()
        {
            return GetHashCode(this);
        }

        public int GetHashCode(BrushPair2 obj)
        {
            return ((ulong)obj.brushIndexOrder0.nodeOrder + ((ulong)obj.brushIndexOrder1.nodeOrder << 32)).GetHashCode();
        }
        #endregion
    }

    struct CompactHierarchyNode
    {
        // TODO: combine bits
        public CSGNodeType      Type;
        public CSGOperationType Operation;
        public CompactNodeID    nodeID;
        public int              childCount;
        public int              childOffset;

        public override string ToString() { return $"({nameof(Type)}: {Type}, {nameof(childCount)}: {childCount}, {nameof(childOffset)}: {childOffset}, {nameof(Operation)}: {Operation}, {nameof(nodeID)}: {nodeID})"; }
    }

    struct BrushAncestorLegend
    {
        public int  ancestorStartIDValue;
        public int  ancestorEndIDValue;

        public override string ToString() { return $"({nameof(ancestorStartIDValue)}: {ancestorStartIDValue}, {nameof(ancestorEndIDValue)}: {ancestorEndIDValue})"; }
    }

    struct CompactTree
    {
        public BlobArray<CompactHierarchyNode>      compactHierarchy;
        public BlobArray<BrushAncestorLegend>       brushAncestorLegend;
        public BlobArray<int>                       brushAncestors;

        public int                                  minBrushIDValue;
        public BlobArray<int>                       brushIDValueToAncestorLegend;
        public int                                  minNodeIDValue;
        public int                                  maxNodeIDValue;



        struct CompactTopDownBuilderNode
        {
            public CSGTreeNode  treeNode;
            public int          compactHierarchyindex;
        }

        static readonly List<BrushAncestorLegend>        s_BrushAncestorLegend    = new List<BrushAncestorLegend>();
        static readonly List<int>                        s_BrushAncestorsIDValues = new List<int>();
        static readonly Queue<CompactTopDownBuilderNode> s_NodeQueue              = new Queue<CompactTopDownBuilderNode>();
        static readonly List<CompactHierarchyNode>       s_HierarchyNodes         = new List<CompactHierarchyNode>();
        static int[]    s_BrushIDValueToAncestorLegend;
        static int[]    s_BrushIDValueToOrder;

        internal static BlobAssetReference<CompactTree> Create(List<CompactNodeID> nodes, List<CSGTreeBrush> brushes, NodeID treeNodeID)
        {
            if (brushes.Count == 0)
                return BlobAssetReference<CompactTree>.Null;

            s_BrushAncestorLegend.Clear();
            s_BrushAncestorsIDValues.Clear();

            var minNodeIDValue = int.MaxValue;
            var maxNodeIDValue = 0;
            for (int b = 0; b < nodes.Count; b++)
            {
                var nodeID = nodes[b];
                if (nodeID == CompactNodeID.Invalid)
                    continue;

                var nodeIDValue = nodeID.value;
                minNodeIDValue = math.min(nodeIDValue, minNodeIDValue);
                maxNodeIDValue = math.max(nodeIDValue, maxNodeIDValue);
            }

            if (minNodeIDValue == int.MaxValue)
                minNodeIDValue = 0;

            var minBrushIDValue = int.MaxValue;
            var maxBrushIDValue = 0;
            for (int b = 0; b < brushes.Count; b++)
            {
                var brushCompactNodeID = CompactHierarchyManager.GetCompactNodeID(brushes[b].NodeID);
                if (brushCompactNodeID == CompactNodeID.Invalid)
                    continue;

                var brushCompactNodeIDValue = brushCompactNodeID.value;
                minBrushIDValue = math.min(brushCompactNodeIDValue, minBrushIDValue);
                maxBrushIDValue = math.max(brushCompactNodeIDValue, maxBrushIDValue);
            }

            if (minBrushIDValue == int.MaxValue)
                minBrushIDValue = 0;

            var desiredBrushIDValueToBottomUpLength = (maxBrushIDValue + 1) - minBrushIDValue;
            if (s_BrushIDValueToAncestorLegend == null ||
                s_BrushIDValueToAncestorLegend.Length < desiredBrushIDValueToBottomUpLength)
            {
                s_BrushIDValueToAncestorLegend = new int[desiredBrushIDValueToBottomUpLength];
                s_BrushIDValueToOrder = new int[desiredBrushIDValueToBottomUpLength];
            }

            // Bottom-up -> per brush list of all ancestors to root
            for (int b = 0; b < brushes.Count; b++)
            {
                var brush = brushes[b];
                if (!brush.Valid)
                    continue;

                var parentStart = s_BrushAncestorsIDValues.Count;

                var parent      = brush.Parent;
                while (parent.Valid && parent.NodeID != treeNodeID)
                {
                    var parentCompactNodeID = CompactHierarchyManager.GetCompactNodeID(parent.NodeID);
                    var parentCompactNodeIDValue = parentCompactNodeID.value;
                    s_BrushAncestorsIDValues.Add(parentCompactNodeIDValue);
                    parent = parent.Parent;
                }

                var brushCompactNodeID = CompactHierarchyManager.GetCompactNodeID(brush.NodeID);
                var brushCompactNodeIDValue = brushCompactNodeID.value;
                s_BrushIDValueToAncestorLegend[brushCompactNodeIDValue - minBrushIDValue] = s_BrushAncestorLegend.Count;
                s_BrushIDValueToOrder[brushCompactNodeIDValue - minBrushIDValue] = b;
                s_BrushAncestorLegend.Add(new BrushAncestorLegend()
                {
                    ancestorEndIDValue   = s_BrushAncestorsIDValues.Count,
                    ancestorStartIDValue = parentStart
                });
            }

            if (s_BrushAncestorLegend.Count == 0)
                return BlobAssetReference<CompactTree>.Null;

            // Top-down
            s_NodeQueue.Clear();
            s_HierarchyNodes.Clear(); // TODO: set capacity to number of nodes in tree

            s_NodeQueue.Enqueue(new CompactTopDownBuilderNode() { treeNode = new CSGTreeNode() { nodeID = treeNodeID }, compactHierarchyindex = 0 });
            s_HierarchyNodes.Add(new CompactHierarchyNode()
            {
                Type        = CSGNodeType.Tree,
                Operation   = CSGOperationType.Additive,
                nodeID      = CompactHierarchyManager.GetCompactNodeID(treeNodeID)
            });

            while (s_NodeQueue.Count > 0)
            {
                var parent      = s_NodeQueue.Dequeue();
                var nodeCount   = parent.treeNode.Count;
                if (nodeCount == 0)
                {
                    var item = s_HierarchyNodes[parent.compactHierarchyindex];
                    item.childOffset = -1;
                    item.childCount = 0;
                    s_HierarchyNodes[parent.compactHierarchyindex] = item;
                    continue;
                }

                int firstCompactTreeIndex = 0;
                // Skip all nodes that are not additive at the start of the branch since they will never produce any geometry
                for (; firstCompactTreeIndex < nodeCount && parent.treeNode[firstCompactTreeIndex].Valid &&
                                    (parent.treeNode[firstCompactTreeIndex].Operation != CSGOperationType.Additive &&
                                     parent.treeNode[firstCompactTreeIndex].Operation != CSGOperationType.Copy); firstCompactTreeIndex++)
                    // NOP
                    ;

                var firstChildIndex = s_HierarchyNodes.Count;
                for (int i = firstCompactTreeIndex; i < nodeCount; i++)
                {
                    var child = parent.treeNode[i];
                    // skip invalid nodes (they don't contribute to the mesh)
                    if (!child.Valid)
                        continue;

                    var childType = child.Type;
                    if (childType != CSGNodeType.Brush)
                        s_NodeQueue.Enqueue(new CompactTopDownBuilderNode()
                        {
                            treeNode = child,
                            compactHierarchyindex = s_HierarchyNodes.Count
                        });
                    var nodeID      = child.NodeID;
                    s_HierarchyNodes.Add(new CompactHierarchyNode()
                    {
                        Type        = childType,
                        Operation   = child.Operation,
                        nodeID      = CompactHierarchyManager.GetCompactNodeID(nodeID)
                    });
                }

                {
                    var item = s_HierarchyNodes[parent.compactHierarchyindex];
                    item.childOffset = firstChildIndex;
                    item.childCount = s_HierarchyNodes.Count - firstChildIndex;
                    s_HierarchyNodes[parent.compactHierarchyindex] = item;
                }
            }

            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<CompactTree>();
            builder.Construct(ref root.compactHierarchy, s_HierarchyNodes);
            builder.Construct(ref root.brushAncestorLegend, s_BrushAncestorLegend);
            builder.Construct(ref root.brushAncestors,      s_BrushAncestorsIDValues);
            root.minBrushIDValue = minBrushIDValue;
            root.minNodeIDValue = minNodeIDValue;
            root.maxNodeIDValue = maxNodeIDValue;
            builder.Construct(ref root.brushIDValueToAncestorLegend, s_BrushIDValueToAncestorLegend, desiredBrushIDValueToBottomUpLength);
            var compactTree = builder.CreateBlobAssetReference<CompactTree>(Allocator.Persistent);
            builder.Dispose();

            return compactTree;
        }
    }

    public struct ChiselSurfaceRenderBuffer
    {
        public int              surfaceIndex;
        public SurfaceLayers    surfaceLayers;

        public int              vertexCount;
        public int              indexCount;

        public uint             geometryHash;
        public uint             surfaceHash;

        public float3           min, max;  

        public BlobArray<Int32>		    indices;
        public BlobArray<RenderVertex>	renderVertices;
        public BlobArray<float3>	    colliderVertices;
    };

    public struct ChiselQuerySurface
    {
        public int      surfaceIndex;
        public int      surfaceParameter;

        public int      vertexCount;
        public int      indexCount;

        public uint     geometryHash;
        public uint     surfaceHash;
    }

    public struct ChiselQuerySurfaces
    {
        public CompactNodeID                    brushNodeID;
        public BlobArray<ChiselQuerySurface>    surfaces;
    }

    public struct ChiselBrushRenderBuffer
    {
        public BlobArray<ChiselSurfaceRenderBuffer> surfaces;
        public BlobArray<ChiselQuerySurfaces>       querySurfaces;
        public int surfaceOffset;
        public int surfaceCount;
    };

    // Note: Stored in BlobAsset at runtime/editor-time
    struct BrushIntersection
    {
        public IndexOrder       nodeIndexOrder;
        public IntersectionType type;
        public int              bottomUpStart;
        public int              bottomUpEnd;

        public override string ToString() { return $"({nameof(nodeIndexOrder.compactNodeID)}: {nodeIndexOrder.compactNodeID}, {nameof(type)}: {type}, {nameof(bottomUpStart)}: {bottomUpStart}, {nameof(bottomUpEnd)}: {bottomUpEnd})"; }
    }

    struct BrushesTouchedByBrush
    {
        public BlobArray<BrushIntersection> brushIntersections;
        public BlobArray<uint>              intersectionBits;
        public int BitCount;
        public int BitOffset;

        public IntersectionType Get(CompactNodeID nodeID)
        {
            var idValue = nodeID.value;
            idValue -= BitOffset;
            if (idValue < 0 || idValue >= BitCount)
            {
                //Debug.Assert(false);
                return IntersectionType.InvalidValue;
            }

            idValue <<= 1;
            var int32Index  = idValue >> 5;	// divide by 32
            var bitIndex    = idValue & 31;	// remainder
            var twoBit      = ((uint)3) << bitIndex;

            var bitShifted  = (uint)intersectionBits[int32Index] & (uint)twoBit;
            var value       = (IntersectionType)((uint)bitShifted >> (int)bitIndex);
            Debug.Assert(value != IntersectionType.InvalidValue);
            return value;
        }
    }
        
    public struct NodeTransformations
    {
        public float4x4 nodeToTree;
        public float4x4 treeToNode;
    };

    // Note: Stored in BlobAsset at runtime/editor-time
    public struct SurfaceInfo
    {
        public ushort               basePlaneIndex;
        public CategoryGroupIndex   interiorCategory;
    }

    public struct IndexSurfaceInfo
    {
        public IndexOrder           brushIndexOrder;
        public ushort               basePlaneIndex;
        public CategoryGroupIndex   interiorCategory;
    }

    // Note: Stored in BlobAsset at runtime/editor-time
    struct BasePolygon
    {
        public IndexOrder       nodeIndexOrder;
        public SurfaceInfo      surfaceInfo;
        public int              startEdgeIndex;
        public int              endEdgeIndex;
    }

    // Note: Stored in BlobAsset at runtime/editor-time
    struct BaseSurface
    {
        public SurfaceLayers    layers;
        public float4           localPlane;
        public UVMatrix         UV0;
    }

    internal struct BasePolygonsBlob
    {
        public BlobArray<BasePolygon>   polygons;
        public BlobArray<Edge>          edges;
        public BlobArray<float3>        vertices;
        public BlobArray<BaseSurface>   surfaces;
    }
    
    public enum IntersectionType : byte
    {
        NoIntersection,
        Intersection,
        AInsideB,
        BInsideA,

        InvalidValue
    };


    public struct BrushIntersectionLoop
    {
        public IndexOrder           indexOrder0;
        public IndexOrder           indexOrder1;
        public SurfaceInfo          surfaceInfo;
        public int                  loopVertexIndex;
        public int                  loopVertexCount;
    }

    internal sealed unsafe class ChiselTreeLookup : ScriptableObject
    {
        public unsafe class Data
        {
            public NativeList<CompactNodeID>    brushIDValues;
            public ChiselLayerParameters        parameters1;
            public ChiselLayerParameters        parameters2;
            public HashSet<int>                 allKnownBrushMeshIndices    = new HashSet<int>();
            public Dictionary<int, int>         previousMeshIDGeneration    = new Dictionary<int, int>();

            public NativeList<BlobAssetReference<BasePolygonsBlob>>             basePolygonCache;
            public NativeList<MinMaxAABB>                                       brushTreeSpaceBoundCache;
            public NativeList<BlobAssetReference<BrushTreeSpaceVerticesBlob>>   treeSpaceVerticesCache;
            public NativeList<BlobAssetReference<RoutingTable>>                 routingTableCache;
            public NativeList<BlobAssetReference<BrushTreeSpacePlanes>>         brushTreeSpacePlaneCache;
            public NativeList<BlobAssetReference<BrushesTouchedByBrush>>        brushesTouchedByBrushCache;
            public NativeList<NodeTransformations>                              transformationCache;
            public NativeList<BlobAssetReference<ChiselBrushRenderBuffer>>      brushRenderBufferCache;
            
            public NativeHashMap<CompactNodeID, MinMaxAABB>                                     brushTreeSpaceBoundLookup;
            public NativeHashMap<CompactNodeID, BlobAssetReference<ChiselBrushRenderBuffer>>    brushRenderBufferLookup;

            public BlobAssetReference<CompactTree>                              compactTree;

            internal void Initialize()
            {
                brushIDValues               = new NativeList<CompactNodeID>(1000, Allocator.Persistent);
                
                brushTreeSpaceBoundLookup    = new NativeHashMap<CompactNodeID, MinMaxAABB>(1000, Allocator.Persistent);
                brushRenderBufferLookup      = new NativeHashMap<CompactNodeID, BlobAssetReference<ChiselBrushRenderBuffer>>(1000, Allocator.Persistent);

                // brushIndex
                basePolygonCache            = new NativeList<BlobAssetReference<BasePolygonsBlob>>(1000, Allocator.Persistent);
                brushTreeSpaceBoundCache    = new NativeList<MinMaxAABB>(1000, Allocator.Persistent);
                treeSpaceVerticesCache      = new NativeList<BlobAssetReference<BrushTreeSpaceVerticesBlob>>(1000, Allocator.Persistent);
                routingTableCache           = new NativeList<BlobAssetReference<RoutingTable>>(1000, Allocator.Persistent);
                brushTreeSpacePlaneCache    = new NativeList<BlobAssetReference<BrushTreeSpacePlanes>>(1000, Allocator.Persistent);
                brushesTouchedByBrushCache  = new NativeList<BlobAssetReference<BrushesTouchedByBrush>>(1000, Allocator.Persistent);
                transformationCache         = new NativeList<NodeTransformations>(1000, Allocator.Persistent);
                brushRenderBufferCache      = new NativeList<BlobAssetReference<ChiselBrushRenderBuffer>>(1000, Allocator.Persistent);

                parameters1.Initialize();
                parameters2.Initialize();
            }

            internal void EnsureCapacity(int brushCount)
            {
                if (brushTreeSpaceBoundLookup.Capacity < brushCount)
                    brushTreeSpaceBoundLookup.Capacity = brushCount;

                if (brushRenderBufferLookup.Capacity < brushCount)
                    brushRenderBufferLookup.Capacity = brushCount;

                if (brushIDValues.Capacity < brushCount)
                    brushIDValues.Capacity = brushCount;

                if (basePolygonCache.Capacity < brushCount)
                    basePolygonCache.Capacity = brushCount;

                if (brushTreeSpaceBoundCache.Capacity < brushCount)
                    brushTreeSpaceBoundCache.Capacity = brushCount;

                if (treeSpaceVerticesCache.Capacity < brushCount)
                    treeSpaceVerticesCache.Capacity = brushCount;

                if (routingTableCache.Capacity < brushCount)
                    routingTableCache.Capacity = brushCount;

                if (brushTreeSpacePlaneCache.Capacity < brushCount)
                    brushTreeSpacePlaneCache.Capacity = brushCount;

                if (brushesTouchedByBrushCache.Capacity < brushCount)
                    brushesTouchedByBrushCache.Capacity = brushCount;

                if (transformationCache.Capacity < brushCount)
                    transformationCache.Capacity = brushCount;

                if (brushRenderBufferCache.Capacity < brushCount)
                    brushRenderBufferCache.Capacity = brushCount;
            }

            internal void Dispose()
            {
                if (brushIDValues.IsCreated)
                    brushIDValues.Dispose();
                brushIDValues = default;
                if (basePolygonCache.IsCreated)
                {
                    foreach (var item in basePolygonCache)
                    {
                        if (item.IsCreated)
                            item.Dispose();
                    }
                    basePolygonCache.Clear();
                    basePolygonCache.Dispose();
                }
                basePolygonCache = default;
                if (brushTreeSpaceBoundCache.IsCreated)
                {
                    brushTreeSpaceBoundCache.Clear();
                    brushTreeSpaceBoundCache.Dispose();
                }
                brushTreeSpaceBoundCache = default;
                if (treeSpaceVerticesCache.IsCreated)
                {
                    foreach (var item in treeSpaceVerticesCache)
                    {
                        if (item.IsCreated)
                            item.Dispose();
                    }
                    treeSpaceVerticesCache.Clear();
                    treeSpaceVerticesCache.Dispose();
                }
                treeSpaceVerticesCache = default;
                if (routingTableCache.IsCreated)
                {
                    foreach (var item in routingTableCache)
                    {
                        if (item.IsCreated)
                            item.Dispose();
                    }
                    routingTableCache.Clear();
                    routingTableCache.Dispose();
                }
                routingTableCache = default;
                if (brushTreeSpacePlaneCache.IsCreated)
                {
                    foreach (var item in brushTreeSpacePlaneCache)
                    {
                        if (item.IsCreated)
                            item.Dispose();
                    }
                    brushTreeSpacePlaneCache.Clear();
                    brushTreeSpacePlaneCache.Dispose();
                }
                brushTreeSpacePlaneCache = default;
                if (brushesTouchedByBrushCache.IsCreated)
                {
                    foreach (var item in brushesTouchedByBrushCache)
                    {
                        if (item.IsCreated)
                            item.Dispose();
                    }
                    brushesTouchedByBrushCache.Clear();
                    brushesTouchedByBrushCache.Dispose();
                }
                brushesTouchedByBrushCache = default;
                if (transformationCache.IsCreated)
                {
                    transformationCache.Clear();
                    transformationCache.Dispose();
                }
                transformationCache = default;
                if (brushRenderBufferCache.IsCreated)
                {
                    foreach (var item in brushRenderBufferCache)
                    {
                        if (item.IsCreated)
                            item.Dispose();
                    }
                    brushRenderBufferCache.Clear();
                    brushRenderBufferCache.Dispose();
                }
                brushRenderBufferCache = default;
                if (brushTreeSpaceBoundLookup.IsCreated)
                    brushTreeSpaceBoundLookup.Dispose();
                brushTreeSpaceBoundLookup = default;
                if (brushRenderBufferLookup.IsCreated)
                    brushRenderBufferLookup.Dispose();
                brushRenderBufferLookup = default;
                if (compactTree.IsCreated)
                    compactTree.Dispose();
                compactTree = default;

                parameters1.Dispose();
                parameters1 = default;
                parameters2.Dispose();
                parameters2 = default;
            }
        }

        static ChiselTreeLookup _singleton;

        static void UpdateValue()
        {
            if (_singleton == null)
            {
                _singleton = ScriptableObject.CreateInstance<ChiselTreeLookup>();
                _singleton.hideFlags = HideFlags.HideAndDontSave;
            }
        }

        public static ChiselTreeLookup Value
        {
            get
            {
                if (_singleton == null)
                    UpdateValue();
                return _singleton;
            }
        }

        public Data this[NodeID nodeID]
        {
            get
            {
                if (!chiselTreeLookup.TryGetValue(nodeID, out int dataIndex))
                {
                    dataIndex = chiselTreeData.Count;
                    chiselTreeLookup[nodeID] = dataIndex;
                    chiselTreeData.Add(new Data());
                    chiselTreeData[dataIndex].Initialize();
                }
                return chiselTreeData[dataIndex];
            }
        }

        readonly Dictionary<NodeID, int>    chiselTreeLookup    = new Dictionary<NodeID, int>();
        readonly List<Data>                 chiselTreeData      = new List<Data>();

        public void Remove(NodeID nodeID)
        {
            if (!chiselTreeLookup.TryGetValue(nodeID, out int dataIndex))
                return;

            var data = chiselTreeData[dataIndex];
            data.Dispose();
            // TODO: remove null entry and fix up indices
            chiselTreeData[dataIndex] = default;
            chiselTreeLookup.Remove(nodeID);
        }

        public void Clear()
        {
            if (_singleton == null)
                return;
            foreach (var data in chiselTreeData)
            {
                if (data != null)
                    data.Dispose();
            }
            chiselTreeData.Clear();
            chiselTreeLookup.Clear();
            DestroyImmediate(_singleton);
            _singleton = null;
        }

        internal void OnDisable()
        {
            foreach (var data in chiselTreeData)
            {
                if (data != null)
                    data.Dispose();
            }
            chiselTreeData.Clear();
            chiselTreeLookup.Clear();
            if (_singleton == this)
                _singleton = null;
        }
    }

    public struct ChiselLayerParameterIndex
    {
        public int count;
        public int index;
    }

    // TODO: store this PER TREE
    public struct ChiselLayerParameters
    {
        public NativeHashMap<int, ChiselLayerParameterIndex> uniqueParameters;
        public int uniqueParameterCount;

        internal void UnregisterParameter(int parameter)
        {
            if (parameter == 0)
                return;
            if (!uniqueParameters.TryGetValue(parameter, out var item))
                return;
            item.count--;
            if (item.count < 0)
                item.count = 0;
            uniqueParameters[parameter] = item;
            
            // TODO: have some way to remove parameters (swap with last index? would need to swap things outside of this class as well somehow)
        }

        internal bool RegisterParameter(int parameter)
        {
            if (parameter == 0)
                return false;
            if (!uniqueParameters.TryGetValue(parameter, out var item))
            {
                var index = uniqueParameterCount;
                uniqueParameterCount++;
                if (uniqueParameters.Capacity < uniqueParameterCount)
                    uniqueParameters.Capacity = Mathf.CeilToInt(uniqueParameterCount * 1.5f);
                uniqueParameters.Add(parameter, 
                    new ChiselLayerParameterIndex 
                    { 
                        count = 1, 
                        index = index 
                    });
                return true;
            } else
            {
                item.count++;
                uniqueParameters[parameter] = item;
                return false;
            }
        }

        internal void Initialize()
        {
            uniqueParameters = new NativeHashMap<int, ChiselLayerParameterIndex>(1000, Allocator.Persistent);
            uniqueParameterCount = 0;
        }

        internal void Dispose()
        {
            if (uniqueParameters.IsCreated) { uniqueParameters.Dispose(); uniqueParameters = default; }
            uniqueParameterCount = 0;
        }

        internal void Clear()
        {
            uniqueParameters.Clear();
            uniqueParameterCount = 0;
        }
    }


    internal sealed unsafe class ChiselMeshLookup : ScriptableObject
    {
        public unsafe class Data
        {
            public readonly HashSet<int> brushMeshUpdateList = new HashSet<int>();
            public NativeHashMap<int, BlobAssetReference<BrushMeshBlob>> brushMeshBlobs;
            public NativeHashMap<int, int> brushMeshBlobGeneration;

            internal void Initialize()
            {
                brushMeshBlobs          = new NativeHashMap<int, BlobAssetReference<BrushMeshBlob>>(1000, Allocator.Persistent);
                brushMeshBlobGeneration = new NativeHashMap<int, int>(1000, Allocator.Persistent);
            }

            public void EnsureCapacity(int capacity)
            {
                if (brushMeshBlobs.Capacity < capacity)
                {
                    brushMeshBlobs.Capacity = capacity;
                    brushMeshBlobGeneration.Capacity = capacity;
                }
            }

            internal void Dispose()
            {
                if (brushMeshBlobs.IsCreated)
                {
                    using (var items = brushMeshBlobs.GetValueArray(Allocator.Temp))
                    {
                        brushMeshBlobs.Clear();
                        brushMeshBlobs.Dispose();
                        foreach (var item in items)
                        {
                            if (item.IsCreated)
                                item.Dispose();
                        }
                    }
                    brushMeshBlobGeneration.Dispose();
                    brushMeshBlobs = default;
                    brushMeshBlobGeneration = default;
                }
                // temporary hack
                CompactHierarchyManager.ClearOutlines();
            }
        }

        public static void Update() 
        {
            var instance                = ChiselMeshLookup.Value;
            var brushMeshBlobGeneration = instance.brushMeshBlobGeneration;
            var brushMeshBlobs          = instance.brushMeshBlobs;
            foreach (var brushMeshIndex in Value.brushMeshUpdateList)
            {
                var brushMeshID = brushMeshIndex + 1;
                var brushMesh   = BrushMeshManager.GetBrushMesh(brushMeshID); //<-- should already be blobs
                if (brushMesh == null)
                {
                    brushMeshBlobs[brushMeshIndex] = BlobAssetReference<BrushMeshBlob>.Null;
                } else
                {
                    var newBrushMesh = BrushMeshBlob.Build(brushMesh);
                    brushMeshBlobs[brushMeshIndex] = newBrushMesh;
                }
                if (!brushMeshBlobGeneration.TryGetValue(brushMeshIndex, out var currentGeneration))
                    brushMeshBlobGeneration[brushMeshIndex] = 1;
                else
                {
                    var newGeneration = currentGeneration + 1;
                    if (newGeneration == 0)
                        newGeneration++;
                    brushMeshBlobGeneration[brushMeshIndex] = newGeneration;
                }
            }
            instance.brushMeshUpdateList.Clear();
        }

        static ChiselMeshLookup _singleton;

        static void UpdateValue()
        {
            if (_singleton == null)
            {
                _singleton = ScriptableObject.CreateInstance<ChiselMeshLookup>();
                _singleton.hideFlags = HideFlags.HideAndDontSave;
            }
        }

        public static Data Value
        {
            get
            {
                if (_singleton == null)
                    UpdateValue();
                return _singleton.data;
            }
        }

        readonly Data data = new Data();

        internal void OnEnable()
        {
            data.Initialize();
        }

        internal void OnDisable()
        {
            data.Dispose();
            _singleton = null;
        }
    }
}
