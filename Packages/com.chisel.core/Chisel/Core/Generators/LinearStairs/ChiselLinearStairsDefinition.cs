using System;
using Debug = UnityEngine.Debug;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Burst;
using UnitySceneExtensions;
using Bounds  = UnityEngine.Bounds;
using Vector3 = UnityEngine.Vector3;

namespace Chisel.Core
{
    [Serializable]
    public enum StairsRiserType : byte
    {
        None,
        ThinRiser,
        ThickRiser,
//		Pyramid,
        Smooth,
        FillDown
    }

    [Serializable]
    public enum StairsSideType : byte
    {
        None,
        // TODO: better names
        Down,
        Up,
        DownAndUp
    }

    [Serializable]
    public struct ChiselLinearStairs : IBranchGenerator
    {
        // TODO: set defaults using attributes?
        public readonly static ChiselLinearStairs DefaultValues = new ChiselLinearStairs
        {
            stepHeight		= 0.20f,
            stepDepth		= 0.20f,
            treadHeight	    = 0.02f,
            nosingDepth	    = 0.02f,
            nosingWidth	    = 0.01f,

            Width			= 1,
            Height			= 1,
            Depth			= 1,

            plateauHeight	= 0,

            riserType		= StairsRiserType.ThinRiser,
            leftSide		= StairsSideType.None,
            rightSide		= StairsSideType.None,
            riserDepth		= 0.05f,
            sideDepth		= 0.125f,
            sideWidth		= 0.125f,
            sideHeight		= 0.5f
        };

        // TODO: add all spiral stairs improvements to linear stairs

        public MinMaxAABB bounds;

        [DistanceValue] public float	stepHeight;
        [DistanceValue] public float	stepDepth;

        [DistanceValue] public float	treadHeight;

        [DistanceValue] public float	nosingDepth;
        [DistanceValue] public float	nosingWidth;

        [DistanceValue] public float    plateauHeight;

        public StairsRiserType          riserType;
        [DistanceValue] public float	riserDepth;

        public StairsSideType           leftSide;
        public StairsSideType           rightSide;
        
        [DistanceValue] public float	sideWidth;
        [DistanceValue] public float	sideHeight;
        [DistanceValue] public float	sideDepth;

        //[NamedItems("Top", "Bottom", "Left", "Right", "Front", "Back", "Tread", "Step", overflow = "Side {0}", fixedSize = 8)]
        //public ChiselSurfaceDefinition  surfaceDefinition;

        #region Properties

        const float kStepSmudgeValue = BrushMeshFactory.LineairStairsData.kStepSmudgeValue;


        public float	Width  { get { return BoundsSize.x; } set { var size = BoundsSize; size.x = value; BoundsSize = size; } }
        public float	Height { get { return BoundsSize.y; } set { var size = BoundsSize; size.y = value; BoundsSize = size; } }
        public float	Depth  { get { return BoundsSize.z; } set { var size = BoundsSize; size.z = value; BoundsSize = size; } }

        public float3 Center
        {
            get { return (bounds.Max + bounds.Min) * 0.5f; }
            set
            {
                var newSize = math.abs(BoundsSize);
                var halfSize = newSize * 0.5f;
                bounds.Min = value - halfSize;
                bounds.Max = value + halfSize;
            }
        }

        public float3 BoundsSize
        {
            get { return bounds.Max - bounds.Min; }
            set
            {
                var newSize = math.abs(value);
                var halfSize = newSize * 0.5f;
                var center = this.Center;
                bounds.Min = center - halfSize;
                bounds.Max = center + halfSize;
            }
        }
        
        public float3   BoundsMin   { get { return math.min(bounds.Min, bounds.Max); } }
        public float3   BoundsMax   { get { return math.max(bounds.Min, bounds.Max); } }
        
        public float	AbsWidth  { get { return math.abs(BoundsSize.x); } }
        public float	AbsHeight { get { return math.abs(BoundsSize.y); } }
        public float	AbsDepth  { get { return math.abs(BoundsSize.z); } }

        public int StepCount
        {
            get
            {
                return math.max(1,
                            (int)math.floor((AbsHeight - plateauHeight + kStepSmudgeValue) / stepHeight));
            }
        }

        public float StepDepthOffset
        {
            get { return math.max(0, AbsDepth - (StepCount * stepDepth)); }
        }
        #endregion

        #region Generate
        [BurstCompile]
        public int PrepareAndCountRequiredBrushMeshes()
        {
            var size = BoundsSize;
            if (math.any(size == 0))
                return 0;

            var description = new BrushMeshFactory.LineairStairsData(bounds,
                                                                        stepHeight, stepDepth,
                                                                        treadHeight,
                                                                        nosingDepth, nosingWidth,
                                                                        plateauHeight,
                                                                        riserType, riserDepth,
                                                                        leftSide, rightSide,
                                                                        sideWidth, sideHeight, sideDepth);
            return description.subMeshCount;
        }

        [BurstCompile()]
        public bool GenerateMesh(BlobAssetReference<NativeChiselSurfaceDefinition> surfaceDefinitionBlob, NativeList<BlobAssetReference<BrushMeshBlob>> brushMeshes, Allocator allocator)
        {
            var description = new BrushMeshFactory.LineairStairsData(bounds,
                                                                        stepHeight, stepDepth,
                                                                        treadHeight,
                                                                        nosingDepth, nosingWidth,
                                                                        plateauHeight,
                                                                        riserType, riserDepth,
                                                                        leftSide, rightSide,
                                                                        sideWidth, sideHeight, sideDepth);
            const int subMeshOffset = 0;
            if (!BrushMeshFactory.GenerateLinearStairsSubMeshes(brushMeshes, 
                                                                subMeshOffset, 
                                                                in description, 
                                                                in surfaceDefinitionBlob, 
                                                                Allocator.Persistent))
            {
                for (int i = 0; i < brushMeshes.Length; i++)
                {
                    if (brushMeshes[i].IsCreated)
                        brushMeshes[i].Dispose();
                }
                return false;
            }
            return true;
        }

        public void Dispose() {}


        [BurstDiscard]
        public void FixupOperations(CSGTreeBranch branch) { }
        #endregion

        #region Surfaces
        public enum SurfaceSides : byte
        {
            Top     = 0,
            Bottom  = 1,
            Left    = 2,
            Right   = 3,
            Front   = 4,
            Back    = 5,
            Tread   = 6,
            Step    = 7,

            TotalSides
        }


        [BurstDiscard]
        public int RequiredSurfaceCount { get { return (int)SurfaceSides.TotalSides; } }

        [BurstDiscard]
        public void UpdateSurfaces(ref ChiselSurfaceDefinition surfaceDefinition)
        {
            var defaultRenderMaterial  = ChiselMaterialManager.DefaultWallMaterial;
            var defaultPhysicsMaterial = ChiselMaterialManager.DefaultPhysicsMaterial;

            surfaceDefinition.surfaces[(int)SurfaceSides.Top    ].brushMaterial = ChiselBrushMaterial.CreateInstance(ChiselMaterialManager.DefaultFloorMaterial, defaultPhysicsMaterial);
            surfaceDefinition.surfaces[(int)SurfaceSides.Bottom ].brushMaterial = ChiselBrushMaterial.CreateInstance(ChiselMaterialManager.DefaultFloorMaterial, defaultPhysicsMaterial);
            surfaceDefinition.surfaces[(int)SurfaceSides.Left   ].brushMaterial = ChiselBrushMaterial.CreateInstance(ChiselMaterialManager.DefaultWallMaterial,  defaultPhysicsMaterial);
            surfaceDefinition.surfaces[(int)SurfaceSides.Right  ].brushMaterial = ChiselBrushMaterial.CreateInstance(ChiselMaterialManager.DefaultWallMaterial,  defaultPhysicsMaterial);
            surfaceDefinition.surfaces[(int)SurfaceSides.Front  ].brushMaterial = ChiselBrushMaterial.CreateInstance(ChiselMaterialManager.DefaultWallMaterial,  defaultPhysicsMaterial);
            surfaceDefinition.surfaces[(int)SurfaceSides.Back   ].brushMaterial = ChiselBrushMaterial.CreateInstance(ChiselMaterialManager.DefaultWallMaterial,  defaultPhysicsMaterial);
            surfaceDefinition.surfaces[(int)SurfaceSides.Tread  ].brushMaterial = ChiselBrushMaterial.CreateInstance(ChiselMaterialManager.DefaultTreadMaterial, defaultPhysicsMaterial);
            surfaceDefinition.surfaces[(int)SurfaceSides.Step   ].brushMaterial = ChiselBrushMaterial.CreateInstance(ChiselMaterialManager.DefaultStepMaterial,  defaultPhysicsMaterial);

            for (int i = 0; i < surfaceDefinition.surfaces.Length; i++)
            {
                if (surfaceDefinition.surfaces[i].brushMaterial == null)
                    surfaceDefinition.surfaces[i].brushMaterial = ChiselBrushMaterial.CreateInstance(defaultRenderMaterial, defaultPhysicsMaterial);
            }
        }
        #endregion

        #region Validation
        public const float	kMinStepHeight			= 0.01f;
        public const float	kMinStepDepth			= 0.01f;
        public const float  kMinRiserDepth          = 0.01f;
        public const float  kMinSideWidth			= 0.01f;
        public const float	kMinWidth				= 0.0001f;

        public void Validate()
        {
            stepHeight		= math.max(kMinStepHeight, stepHeight);
            stepDepth		= math.clamp(stepDepth, kMinStepDepth, AbsDepth);
            treadHeight	    = math.max(0, treadHeight);
            nosingDepth	    = math.max(0, nosingDepth);
            nosingWidth	    = math.max(0, nosingWidth);

            Width			= math.max(kMinWidth, AbsWidth) * (Width < 0 ? -1 : 1);
            Depth			= math.max(stepDepth, AbsDepth) * (Depth < 0 ? -1 : 1);

            riserDepth		= math.max(kMinRiserDepth, riserDepth);
            sideDepth		= math.max(0, sideDepth);
            sideWidth		= math.max(kMinSideWidth, sideWidth);
            sideHeight		= math.max(0, sideHeight);

            var realHeight          = math.max(stepHeight, AbsHeight);
            var maxPlateauHeight    = realHeight - stepHeight;

            plateauHeight	= math.clamp(plateauHeight, 0, maxPlateauHeight);

            var totalSteps          = math.max(1, (int)math.floor((realHeight - plateauHeight + ChiselLinearStairs.kStepSmudgeValue) / stepHeight));
            var totalStepHeight     = totalSteps * stepHeight;

            plateauHeight	= math.max(0, realHeight - totalStepHeight);
            stepDepth		= math.clamp(stepDepth, kMinStepDepth, AbsDepth / totalSteps);
        }
        #endregion
    }

    // https://www.archdaily.com/892647/how-to-make-calculations-for-staircase-designs
    // https://inspectapedia.com/Stairs/2024s.jpg
    // https://landarchbim.com/2014/11/18/stair-nosing-treads-and-stringers/
    // https://en.wikipedia.org/wiki/Stairs
    [Serializable]
    public struct ChiselLinearStairsDefinition : ISerializedBranchGenerator<ChiselLinearStairs>
    {
        public const string kNodeTypeName = "Linear Stairs";

        [HideFoldout] public ChiselLinearStairs settings;

        //[NamedItems("Top", "Bottom", "Left", "Right", "Front", "Back", "Tread", "Step", overflow = "Side {0}", fixedSize = 8)]
        //public ChiselSurfaceDefinition  surfaceDefinition;

        public void Reset() { settings = ChiselLinearStairs.DefaultValues; }

        public int RequiredSurfaceCount { get { return settings.RequiredSurfaceCount; } }

        public void UpdateSurfaces(ref ChiselSurfaceDefinition surfaceDefinition) { settings.UpdateSurfaces(ref surfaceDefinition); }

        public void Validate() { settings.Validate(); }


        public ChiselLinearStairs GetBranchGenerator() { return settings; }

        #region OnEdit
        //
        // TODO: code below needs to be cleaned up & simplified 
        //


        public void OnEdit(IChiselHandles handles)
        {
            var newDefinition = this;

            {
                var stepDepthOffset = settings.StepDepthOffset;
                var stepHeight      = settings.stepHeight;
                var stepCount       = settings.StepCount;
                var bounds          = settings.bounds;

                var steps		    = handles.moveSnappingSteps;
                steps.y			    = stepHeight;

                if (handles.DoBoundsHandle(ref bounds, snappingSteps: steps))
                    newDefinition.settings.bounds = bounds;

                var min			= math.min(bounds.Min, bounds.Max);
                var max			= math.min(bounds.Min, bounds.Max);

                var size        = (max - min);

                var heightStart = bounds.Max.y + (size.y < 0 ? size.y : 0);

                var edgeHeight  = heightStart - stepHeight * stepCount;
                var pHeight0	= new Vector3(min.x, edgeHeight, max.z);
                var pHeight1	= new Vector3(max.x, edgeHeight, max.z);

                var depthStart = bounds.Min.z - (size.z < 0 ? size.z : 0);

                var pDepth0		= new Vector3(min.x, max.y, depthStart + stepDepthOffset);
                var pDepth1		= new Vector3(max.x, max.y, depthStart + stepDepthOffset);

                if (handles.DoTurnHandle(ref bounds))
                    newDefinition.settings.bounds = bounds;

                if (handles.DoEdgeHandle1D(out edgeHeight, Axis.Y, pHeight0, pHeight1, snappingStep: stepHeight))
                {
                    var totalStepHeight = math.clamp((heightStart - edgeHeight), size.y % stepHeight, size.y);
                    const float kSmudgeValue = 0.0001f;
                    var oldStepCount = newDefinition.settings.StepCount;
                    var newStepCount = math.max(1, (int)math.floor((math.abs(totalStepHeight) + kSmudgeValue) / stepHeight));

                    newDefinition.settings.stepDepth     = (oldStepCount * newDefinition.settings.stepDepth) / newStepCount;
                    newDefinition.settings.plateauHeight = size.y - (stepHeight * newStepCount);
                }

                if (handles.DoEdgeHandle1D(out stepDepthOffset, Axis.Z, pDepth0, pDepth1, snappingStep: ChiselLinearStairs.kMinStepDepth))
                {
                    stepDepthOffset -= depthStart;
                    stepDepthOffset = math.clamp(stepDepthOffset, 0, settings.AbsDepth - ChiselLinearStairs.kMinStepDepth);
                    newDefinition.settings.stepDepth = ((settings.AbsDepth - stepDepthOffset) / settings.StepCount);
                }

                float heightOffset;
                var prevModified = handles.modified;
                {
                    var direction = Vector3.Cross(Vector3.forward, pHeight0 - pDepth0).normalized;
                    handles.DoEdgeHandle1DOffset(out var height0vec, Axis.Y, pHeight0, pDepth0, direction, snappingStep: stepHeight);
                    handles.DoEdgeHandle1DOffset(out var height1vec, Axis.Y, pHeight1, pDepth1, direction, snappingStep: stepHeight);
                    var height0 = Vector3.Dot(direction, height0vec);
                    var height1 = Vector3.Dot(direction, height1vec);
                    if (math.abs(height0) > math.abs(height1)) heightOffset = height0; else heightOffset = height1;
                }
                if (prevModified != handles.modified)
                {
                    newDefinition.settings.plateauHeight += heightOffset;
                }
            }
            if (handles.modified)
                this = newDefinition;
        }
        #endregion

        public bool HasValidState()
        {
            return true;
        }

        public void OnMessages(IChiselMessages messages)
        {
        }
    }
}