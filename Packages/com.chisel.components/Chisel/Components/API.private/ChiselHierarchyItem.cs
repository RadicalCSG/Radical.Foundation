using Chisel.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Chisel.Components
{

    public sealed class ChiselSceneHierarchy
    {
        public Scene                                Scene;
        public ChiselModel                          DefaultModel;	// TODO: create this, but only when necessary.
        public readonly List<ChiselHierarchyItem>   RootItems	    = new List<ChiselHierarchyItem>();
    }

    public sealed class ChiselHierarchyItem
    {
        public static readonly Bounds EmptyBounds = new Bounds();

        public ChiselHierarchyItem(ChiselNode node) { Component = node; }

        public ChiselHierarchyItem                  Parent;
        public readonly List<int>                   SiblingIndices      = new List<int>();
        public readonly List<ChiselHierarchyItem>   Children            = new List<ChiselHierarchyItem>();

        public ChiselSceneHierarchy sceneHierarchy;
        public Scene                Scene;
        public Transform            Transform;
        public GameObject           GameObject;
        public readonly ChiselNode  Component;
        
        // TODO: should cache this instead
        public ChiselModel Model
        {
            get
            {
                var iterator = this;
                do
                {
                    var model = iterator.Component as ChiselModel;
                    if (!Equals(model, null))
                        return model;
                    iterator = iterator.Parent;
                } while (!Equals(iterator, null));
                return null;
            }
        }

        public Bounds Bounds
        {
            get
            {
                UpdateBounds();
                return SelfWithChildrenBounds;
            }
        }

        private Bounds				SelfBounds			    = EmptyBounds;
        private Bounds              SelfWithChildrenBounds  = EmptyBounds;
        private bool				BoundsDirty			    = true;
        private bool                ChildBoundsDirty	    = true;

        public bool                 Registered			    = false;
        public bool                 IsOpen				    = true;

        public Matrix4x4            LocalToWorldMatrix      = Matrix4x4.identity;
        public Matrix4x4            WorldToLocalMatrix      = Matrix4x4.identity;

        // TODO: Move bounds handling code to separate class, keep this clean
        public void					UpdateBounds()
        {
            if (BoundsDirty)
            {
                if (Component)
                {
                    if (!Transform)
                        Transform = Component.transform;
                    var generator = Component as ChiselGeneratorComponent;
                    if (generator)
                        SelfBounds = ChiselBoundsUtility.CalculateBounds(generator);
                    ChildBoundsDirty = true;
                    BoundsDirty = false;
                }
            }
            if (ChildBoundsDirty)
            {
                SelfWithChildrenBounds = SelfBounds;
                // TODO: make this non-iterative
                for (int i = 0; i < Children.Count; i++)
                    Children[i].EncapsulateBounds(ref SelfWithChildrenBounds);
                ChildBoundsDirty = false;
            }
        }

        // TODO: Move bounds handling code to separate class, keep this clean
        public Bounds CalculateBounds(Matrix4x4 transformation)
        {
            var gridBounds = new Bounds();
            if (Component)
            {
                if (!Transform)
                    Transform = Component.transform;
                var generator = Component as ChiselGeneratorComponent;
                if (generator)
                    SelfBounds = ChiselBoundsUtility.CalculateBounds(generator, transformation);
            }

            // TODO: make this non-iterative
            for (int i = 0; i < Children.Count; i++)
                Children[i].EncapsulateBounds(ref gridBounds, transformation);
            return gridBounds;
        }

        public void		EncapsulateBounds(ref Bounds outBounds)
        {
            UpdateBounds();
            if (SelfWithChildrenBounds.size.sqrMagnitude != 0)
            {
                float magnitude = SelfWithChildrenBounds.size.sqrMagnitude;
                if (float.IsInfinity(magnitude) ||
                    float.IsNaN(magnitude))
                {
                    var transformation = LocalToWorldMatrix;
                    var center = transformation.GetColumn(3);
                    SelfWithChildrenBounds = new Bounds(center, Vector3.zero);
                }
                if (outBounds.size.sqrMagnitude == 0) outBounds = SelfWithChildrenBounds;
                else								  outBounds.Encapsulate(SelfWithChildrenBounds);
            }
        }
        
        public void		EncapsulateBounds(ref Bounds outBounds, Matrix4x4 transformation)
        {
            var gridBounds = CalculateBounds(transformation);
            if (gridBounds.size.sqrMagnitude != 0)
            {
                float magnitude = gridBounds.size.sqrMagnitude;
                if (float.IsInfinity(magnitude) ||
                    float.IsNaN(magnitude))
                {
                    var center = LocalToWorldMatrix.GetColumn(3);
                    gridBounds = new Bounds(center, Vector3.zero);
                }
                if (outBounds.size.sqrMagnitude == 0) outBounds = gridBounds;
                else								  outBounds.Encapsulate(gridBounds);
            }
        }

        public void		SetChildBoundsDirty()
        {
            if (ChildBoundsDirty)
                return;

            ChildBoundsDirty = true;
            if (Parent != null)
                Parent.SetChildBoundsDirty();
        }

        public void		SetBoundsDirty()
        {
            if (BoundsDirty)
                return;

            BoundsDirty = true;
            if (Parent != null)
                Parent.SetChildBoundsDirty();
        }
    }

}