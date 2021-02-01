using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using Chisel.Core;
using UnityEditor.ShortcutManagement;

namespace Chisel.Editors
{
    public class ChiselKeyboardDefaults
    {
        public const KeyCode kCancelKey = KeyCode.Escape;

        public const string     ShortCutCreateBase      = "Chisel/Create/";
        public const string     ShortCutEditModeBase    = "Chisel/Edit Mode/";

        public const KeyCode SwitchToCreateEditMode     = KeyCode.F1;
        //public const KeyCode SwitchToObjectEditMode   = KeyCode.F2;
        public const KeyCode SwitchToShapeEditMode      = KeyCode.F2;
        public const KeyCode SwitchToUVMoveMode         = KeyCode.F3;
        public const KeyCode SwitchToUVRotateMode       = KeyCode.F4;
        public const KeyCode SwitchToUVScaleMode        = KeyCode.F5;
        public const KeyCode SwitchToPivotEditMode      = KeyCode.F6;


        // TODO: assign reasonable keys
        public const KeyCode            FreeBuilderModeKey                  = KeyCode.F6;
        public const ShortcutModifiers  FreeBuilderModeModifiers            = ShortcutModifiers.None;
        public const KeyCode            BoxBuilderModeKey                   = KeyCode.F7;
        public const ShortcutModifiers  BoxBuilderModeModifiers             = ShortcutModifiers.None;
        public const KeyCode            CylinderBuilderModeKey              = KeyCode.F8;
        public const ShortcutModifiers  CylinderBuilderModeModifiers        = ShortcutModifiers.None;
        public const KeyCode            CapsuleBuilderModeKey               = KeyCode.F9;
        public const ShortcutModifiers  CapsuleBuilderModeModifiers         = ShortcutModifiers.None;
        public const KeyCode            StadiumBuilderModeKey               = KeyCode.None;
        public const ShortcutModifiers  StadiumBuilderModeModifiers         = ShortcutModifiers.None;
        public const KeyCode            HemisphereBuilderModeKey            = KeyCode.F10;
        public const ShortcutModifiers  HemisphereBuilderModeModifiers      = ShortcutModifiers.None;
        public const KeyCode            SphereBuilderModeKey                = KeyCode.F11;
        public const ShortcutModifiers  SphereBuilderModeModifiers          = ShortcutModifiers.None;
        public const KeyCode            TorusBuilderModeKey                 = KeyCode.None;
        public const ShortcutModifiers  TorusBuilderModeModifiers           = ShortcutModifiers.None;

        public const KeyCode            LinearStairsBuilderModeKey          = KeyCode.F12;
        public const ShortcutModifiers  LinearStairsBuilderModeModifiers    = ShortcutModifiers.None;
        public const KeyCode            SpiralStairsBuilderModeKey          = KeyCode.F13;
        public const ShortcutModifiers  SpiralStairsBuilderModeModifiers    = ShortcutModifiers.None;
        public const KeyCode            PathedStairsBuilderModeKey          = KeyCode.None;
        public const ShortcutModifiers  PathedStairsBuilderModeModifiers    = ShortcutModifiers.None;

        public const KeyCode            RevolvedShapeBuilderModeKey         = KeyCode.None;
        public const ShortcutModifiers  RevolvedShapeBuilderModeModifiers   = ShortcutModifiers.None;


        public const KeyCode            HalfGridSizeKey                 = KeyCode.LeftBracket;
        public const ShortcutModifiers  HalfGridSizeModifiers           = ShortcutModifiers.None;
        public const KeyCode            DoubleGridSizeKey               = KeyCode.RightBracket;
        public const ShortcutModifiers  DoubleGridSizeModifiers         = ShortcutModifiers.None;

        public const KeyCode            ToggleShowGridKey               = KeyCode.G;
        public const ShortcutModifiers  ToggleShowGridModifiers         = ShortcutModifiers.Shift;

        public const KeyCode            ToggleBoundsSnappingKey         = KeyCode.B;
        public const ShortcutModifiers  ToggleBoundsSnappingModifiers   = ShortcutModifiers.Shift;
        public const KeyCode            TogglePivotSnappingKey          = KeyCode.P;
        public const ShortcutModifiers  TogglePivotSnappingModifiers    = ShortcutModifiers.Shift;

        public const KeyCode           ApplyLastSelectedMaterialKey      = KeyCode.A;
        public const ShortcutModifiers ApplyLastSelectedMaterialModifier = ShortcutModifiers.Shift;
    }
}
