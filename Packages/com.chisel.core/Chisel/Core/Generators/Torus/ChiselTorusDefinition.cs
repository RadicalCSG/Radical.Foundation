using System;
using Debug = UnityEngine.Debug;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Burst;
using UnitySceneExtensions;
using Vector3 = UnityEngine.Vector3;
using AOT;
using System.Runtime.InteropServices;

namespace Chisel.Core
{
    [Serializable]
    [BurstCompile()]
    public struct TorusSettings
    {
        public const float kMinTubeDiameter = 0.1f;

        public float    outerDiameter;
        public float    InnerDiameter { get { return math.max(0, outerDiameter - (tubeWidth * 2)); } set { tubeWidth = math.max(kMinTubeDiameter, (outerDiameter - InnerDiameter) * 0.5f); } }
        public float    tubeWidth;
        public float    tubeHeight;
        public float    tubeRotation;
        public float    startAngle;
        public float    totalAngle;
        public int      verticalSegments;
        public int      horizontalSegments;

        [MarshalAs(UnmanagedType.U1)]
        public bool     fitCircle;

    }

    public struct ChiselTorusGenerator : IChiselBranchTypeGenerator<TorusSettings>
    {
        public int PrepareAndCountRequiredBrushMeshes(ref TorusSettings settings)
        {
            return settings.horizontalSegments;
        }

        public bool GenerateMesh(ref TorusSettings settings, BlobAssetReference<NativeChiselSurfaceDefinition> surfaceDefinitionBlob, NativeList<BlobAssetReference<BrushMeshBlob>> brushMeshes, Allocator allocator)
        {
            using (var vertices = BrushMeshFactory.GenerateTorusVertices(settings.outerDiameter,
                                                                         settings.tubeWidth,
                                                                         settings.tubeHeight,
                                                                         settings.tubeRotation,
                                                                         settings.startAngle,
                                                                         settings.totalAngle,
                                                                         settings.verticalSegments,
                                                                         settings.horizontalSegments,
                                                                         settings.fitCircle,
                                                                         Allocator.Temp))
            {
                if (!BrushMeshFactory.GenerateTorus(brushMeshes,
                                                    in vertices,
                                                    settings.verticalSegments,
                                                    settings.horizontalSegments, 
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
        }

        public void Dispose(ref TorusSettings settings) {}

        public void FixupOperations(CSGTreeBranch branch, TorusSettings settings) { }        
    }

    [Serializable]
    public struct ChiselTorusDefinition : IChiselBranchGenerator<ChiselTorusGenerator, TorusSettings>
    {
        public const string kNodeTypeName = "Torus";

        public const int kDefaultHorizontalSegments = 8;
        public const int kDefaultVerticalSegments = 8;

        // TODO: add scale the tube in y-direction (use transform instead?)
        // TODO: add start/total angle of tube

        [HideFoldout] public TorusSettings settings;


        //[NamedItems(overflow = "Surface {0}")]
        //public ChiselSurfaceDefinition  surfaceDefinition;

        public void Reset()
        {
            // TODO: create constants
            settings.tubeWidth = 0.5f;
            settings.tubeHeight = 0.5f;
            settings.outerDiameter = 1.0f;
            settings.tubeRotation = 0;
            settings.startAngle = 0.0f;
            settings.totalAngle = 360.0f;
            settings.horizontalSegments = kDefaultHorizontalSegments;
            settings.verticalSegments = kDefaultVerticalSegments;

            settings.fitCircle = true;
        }

        public int RequiredSurfaceCount { get { return 6; } }

        public void UpdateSurfaces(ref ChiselSurfaceDefinition surfaceDefinition) { }

        public void Validate()
        {
            settings.tubeWidth			= math.max(settings.tubeWidth,  TorusSettings.kMinTubeDiameter);
            settings.tubeHeight			= math.max(settings.tubeHeight, TorusSettings.kMinTubeDiameter);
            settings.outerDiameter		= math.max(settings.outerDiameter, settings.tubeWidth * 2);

            settings.horizontalSegments	= math.max(settings.horizontalSegments, 3);
            settings.verticalSegments	= math.max(settings.verticalSegments, 3);

            settings.totalAngle			= math.clamp(settings.totalAngle, 1, 360); // TODO: constants
        }

        public TorusSettings GenerateSettings()
        {
            return settings;
        }

        #region OnEdit
        //
        // TODO: code below needs to be cleaned up & simplified 
        //


        const float kLineDash					= 2.0f;
        const float kVertLineThickness			= 0.75f;
        const float kHorzLineThickness			= 1.0f;
        const float kCapLineThickness			= 2.0f;
        const float kCapLineThicknessSelected   = 2.5f;

        static void DrawOutline(IChiselHandleRenderer renderer, ChiselTorusDefinition definition, float3[] vertices, LineMode lineMode)
        {
            var horzSegments	= definition.settings.horizontalSegments;
            var vertSegments	= definition.settings.verticalSegments;
            
            if (definition.settings.totalAngle != 360)
                horzSegments++;
            
            var prevColor		= renderer.color;
            prevColor.a *= 0.8f;
            var color			= prevColor;
            color.a *= 0.6f;

            renderer.color = color;
            for (int i = 0, j = 0; i < horzSegments; i++, j += vertSegments)
                renderer.DrawLineLoop(vertices, j, vertSegments, lineMode: lineMode, thickness: kVertLineThickness);

            for (int k = 0; k < vertSegments; k++)
            {
                for (int i = 0, j = 0; i < horzSegments - 1; i++, j += vertSegments)
                    renderer.DrawLine(vertices[j + k], vertices[j + k + vertSegments], lineMode: lineMode, thickness: kHorzLineThickness);
            }
            if (definition.settings.totalAngle == 360)
            {
                for (int k = 0; k < vertSegments; k++)
                {
                    renderer.DrawLine(vertices[k], vertices[k + ((horzSegments - 1) * vertSegments)], lineMode: lineMode, thickness: kHorzLineThickness);
                }
            }
            renderer.color = prevColor;
        }

        public void OnEdit(IChiselHandles handles)
        {
            var normal			= Vector3.up;

            float3[] vertices = null;
            if (BrushMeshFactory.GenerateTorusVertices(this, ref vertices))
            {
                var baseColor = handles.color;
                handles.color = handles.GetStateColor(baseColor, false, false);
                DrawOutline(handles, this, vertices, lineMode: LineMode.ZTest);
                handles.color = handles.GetStateColor(baseColor, false, true);
                DrawOutline(handles, this, vertices, lineMode: LineMode.NoZTest);
                handles.color = baseColor;
            }

            var outerRadius = settings.outerDiameter * 0.5f;
            var innerRadius = settings.InnerDiameter * 0.5f;
            var topPoint	= normal * (settings.tubeHeight * 0.5f);
            var bottomPoint	= normal * (-settings.tubeHeight * 0.5f);

            handles.DoRadiusHandle(ref outerRadius, normal, float3.zero);
            handles.DoRadiusHandle(ref innerRadius, normal, float3.zero);
            handles.DoDirectionHandle(ref bottomPoint, -normal);
            handles.DoDirectionHandle(ref topPoint, normal);
            if (handles.modified)
            {
                settings.outerDiameter	= outerRadius * 2.0f;
                settings.InnerDiameter	= innerRadius * 2.0f;
                settings.tubeHeight		= (topPoint.y - bottomPoint.y);
                // TODO: handle sizing down
            }
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