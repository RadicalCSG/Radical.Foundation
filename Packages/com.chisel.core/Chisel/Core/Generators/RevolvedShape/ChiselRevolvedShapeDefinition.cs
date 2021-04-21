using System;
using Bounds = UnityEngine.Bounds;
using Vector3 = UnityEngine.Vector3;
using Debug = UnityEngine.Debug;
using Mathf = UnityEngine.Mathf;
using UnitySceneExtensions;
using UnityEngine.Profiling;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;

namespace Chisel.Core
{
    [Serializable]
    public struct ChiselRevolvedShapeDefinition : IChiselGenerator
    {
        public const string kNodeTypeName = "Revolved Shape";

        public const int				kDefaultCurveSegments	= 8;
        public const int                kDefaultRevolveSegments = 8;
        public static readonly Curve2D	kDefaultShape			= new Curve2D(new[]{ new CurveControlPoint2D(-1,-1), new CurveControlPoint2D( 1,-1), new CurveControlPoint2D( 1, 1), new CurveControlPoint2D(-1, 1) });

        public Curve2D  shape;
        public int	    curveSegments;
        public int	    revolveSegments;
        public float    startAngle;
        public float    totalAngle;

        //[NamedItems(overflow = "Surface {0}")]
        //public ChiselSurfaceDefinition  surfaceDefinition;

        public void Reset()
        {
            // TODO: create constants
            shape			= kDefaultShape;
            startAngle		= 0.0f;
            totalAngle		= 360.0f;
            curveSegments	= kDefaultCurveSegments;
            revolveSegments	= kDefaultRevolveSegments;
        }

        public void Validate(ref ChiselSurfaceDefinition surfaceDefinition)
        {
            if (surfaceDefinition == null)
                surfaceDefinition = new ChiselSurfaceDefinition();

            curveSegments	= Mathf.Max(curveSegments, 2);
            revolveSegments	= Mathf.Max(revolveSegments, 1);

            totalAngle		= Mathf.Clamp(totalAngle, 1, 360); // TODO: constants

            surfaceDefinition.EnsureSize(6);
        }

        public JobHandle Generate(ref ChiselSurfaceDefinition surfaceDefinition, ref CSGTreeNode node, int userID, CSGOperationType operation)
        {
            var branch = (CSGTreeBranch)node;
            if (!branch.Valid)
            {
                node = branch = CSGTreeBranch.Create(userID: userID, operation: operation);
            } else
            {
                if (branch.Operation != operation)
                    branch.Operation = operation;
            }

            Validate(ref surfaceDefinition);

            using (var curveBlob = ChiselCurve2DBlob.Convert(shape, Allocator.Temp))
            {
                // TODO: brushes must be part of unique hierarchy, otherwise it'd be impossible to create brushes safely inside a job ....

                ref var curve = ref curveBlob.Value;
                if (!curve.ConvexPartition(curveSegments, out var polygonVerticesList, out var polygonVerticesSegments, Allocator.Temp))
                {
                    this.ClearBrushes(branch);
                    return default;
                }

                BrushMeshFactory.Split2DPolygonAlongOriginXAxis(polygonVerticesList, polygonVerticesSegments);

                int requiredSubMeshCount = polygonVerticesSegments.Length * revolveSegments;
                if (requiredSubMeshCount == 0)
                {
                    this.ClearBrushes(branch);
                    return default;
                }

                if (branch.Count != requiredSubMeshCount)
                    this.BuildBrushes(branch, requiredSubMeshCount);

                using (var pathMatrices = BrushMeshFactory.GetCircleMatrices(revolveSegments, new float3(0,1,0), Allocator.Temp))
                using (var surfaceDefinitionBlob = BrushMeshManager.BuildSurfaceDefinitionBlob(in surfaceDefinition, Allocator.Temp))
                {
                    using (var brushMeshes = new NativeArray<BlobAssetReference<BrushMeshBlob>>(requiredSubMeshCount, Allocator.Temp))
                    {
                        if (!BrushMeshFactory.GenerateExtrudedShape(brushMeshes,
                                                                    in polygonVerticesList,
                                                                    in polygonVerticesSegments,
                                                                    in pathMatrices,
                                                                    in surfaceDefinitionBlob,
                                                                    Allocator.Persistent))
                        {
                            this.ClearBrushes(branch);
                            return default;
                        }

                        for (int i = 0; i < requiredSubMeshCount; i++)
                        {
                            var brush = (CSGTreeBrush)branch[i];
                            brush.LocalTransformation = float4x4.identity;
                            brush.BrushMesh = new BrushMeshInstance { brushMeshHash = BrushMeshManager.RegisterBrushMesh(brushMeshes[i]) };
                        }
                        return default;
                    }
                }
            }
        }


        //
        // TODO: code below needs to be cleaned up & simplified 
        //


        const float kLineDash					= 2.0f;
        const float kVertLineThickness			= 0.75f;
        const float kHorzLineThickness			= 1.0f;
        const float kCapLineThickness			= 2.0f;
        const float kCapLineThicknessSelected   = 2.5f;

        public void OnEdit(ref ChiselSurfaceDefinition surfaceDefinition, IChiselHandles handles)
        {
            var baseColor		= handles.color;
            var normal			= Vector3.up;

            var controlPoints	= shape.controlPoints;
            
            var shapeVertices		= new List<SegmentVertex>();
            BrushMeshFactory.GetPathVertices(this.shape, this.curveSegments, shapeVertices);

            
            var horzSegments			= this.revolveSegments;
            var horzDegreePerSegment	= this.totalAngle / horzSegments;
            var horzOffset				= this.startAngle;
            
            var noZTestcolor = handles.GetStateColor(baseColor, false, true);
            var zTestcolor	 = handles.GetStateColor(baseColor, false, false);
            for (int h = 1, pr = 0; h < horzSegments + 1; pr = h, h++)
            {
                var hDegree0	= (pr * horzDegreePerSegment) + horzOffset;
                var hDegree1	= (h  * horzDegreePerSegment) + horzOffset;
                var rotation0	= Quaternion.AngleAxis(hDegree0, normal);
                var rotation1	= Quaternion.AngleAxis(hDegree1, normal);
                for (int p0 = controlPoints.Length - 1, p1 = 0; p1 < controlPoints.Length; p0 = p1, p1++)
                {
                    var point0	= controlPoints[p0].position;
                    //var point1	= controlPoints[p1].position;
                    var vertexA	= rotation0 * new Vector3(point0.x, point0.y, 0);
                    var vertexB	= rotation1 * new Vector3(point0.x, point0.y, 0);
                    //var vertexC	= rotation0 * new Vector3(point1.x, 0, point1.y);

                    handles.color = noZTestcolor;
                    handles.DrawLine(vertexA, vertexB, lineMode: LineMode.NoZTest, thickness: kHorzLineThickness);//, dashSize: kLineDash);

                    handles.color = zTestcolor;
                    handles.DrawLine(vertexA, vertexB, lineMode: LineMode.ZTest,   thickness: kHorzLineThickness);//, dashSize: kLineDash);
                }

                for (int v0 = shapeVertices.Count - 1, v1 = 0; v1 < shapeVertices.Count; v0=v1, v1++)
                {
                    var point0	= shapeVertices[v0].position;
                    var point1	= shapeVertices[v1].position;
                    var vertexA	= rotation0 * new Vector3(point0.x, point0.y, 0);
                    var vertexB	= rotation0 * new Vector3(point1.x, point1.y, 0);

                    handles.color = noZTestcolor;
                    handles.DrawLine(vertexA, vertexB, lineMode: LineMode.NoZTest, thickness: kHorzLineThickness, dashSize: kLineDash);

                    handles.color = zTestcolor;
                    handles.DrawLine(vertexA, vertexB, lineMode: LineMode.ZTest,   thickness: kHorzLineThickness, dashSize: kLineDash);
                }
            }
            handles.color = baseColor;

            {
                // TODO: make this work non grid aligned so we can place it upwards
                handles.DoShapeHandle(ref shape, float4x4.identity);
                handles.DrawLine(normal * 10, normal * -10, dashSize: 4.0f);
            }
        }

        public void OnMessages(IChiselMessages messages)
        {
        }
    }
}