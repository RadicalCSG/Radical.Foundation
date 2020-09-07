﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Chisel.Core;
using Chisel.Components;
using UnitySceneExtensions;
using UnityEditor.ShortcutManagement;

namespace Chisel.Editors
{
    public sealed class ChiselExtrudedShapeSettings : ScriptableObject, IChiselShapePlacementSettings<ChiselExtrudedShapeDefinition>
    {
        const string    kToolName   = "Free Draw";
        public string   ToolName    => kToolName;
        public string   Group       => "Freeform";

        #region Keyboard Shortcut
        const string kToolShotcutName = ChiselKeyboardDefaults.ShortCutCreateBase + "Free Drawn Shape";
        [Shortcut(kToolShotcutName, ChiselKeyboardDefaults.FreeBuilderModeKey, ChiselKeyboardDefaults.FreeBuilderModeModifiers, displayName = kToolShotcutName)]
        public static void StartGeneratorMode() { ChiselGeneratorManager.GeneratorType = typeof(ChiselExtrudedShapeGeneratorMode); }
        #endregion

        public void OnCreate(ref ChiselExtrudedShapeDefinition definition, Curve2D shape)
        {
            definition.path     = new ChiselPath(ChiselPath.Default);
            definition.shape    = new Curve2D(shape);
        }

        public void OnUpdate(ref ChiselExtrudedShapeDefinition definition, float height)
        {
            definition.path.segments[1].position = ChiselPathPoint.kDefaultDirection * height;
        }

        public void OnPaint(IGeneratorHandleRenderer renderer, Curve2D shape, float height)
        {
            renderer.RenderShape(shape, height);
        }
    }

    public sealed class ChiselExtrudedShapeGeneratorMode : ChiselShapePlacementTool<ChiselExtrudedShapeSettings, ChiselExtrudedShapeDefinition, ChiselExtrudedShape>
    {
    }
}
