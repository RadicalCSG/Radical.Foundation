/* * * * * * * * * * * * * * * * * * * * * *
Chisel.Editors.ChiselMaterialBrowserWindow.cs

License: MIT (https://tldrlegal.com/license/mit-license)
Author: Daniel Cornelius

* * * * * * * * * * * * * * * * * * * * * */

using System;
using System.Collections.Generic;
using Chisel.Components;
using Chisel.Core;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace Chisel.Editors
{
    internal sealed partial class ChiselMaterialBrowserWindow : EditorWindow
    {
        private const string PREVIEW_SIZE_PREF_KEY = "chisel_matbrowser_pviewSize";
        private const string TILE_LABEL_PREF_KEY   = "chisel_matbrowser_pviewShowLabel";

        private static List<ChiselMaterialBrowserTile> m_Tiles  = new List<ChiselMaterialBrowserTile>();
        private static List<string>                    m_Labels = new List<string>();

        private static int     m_CurrentPropsTab         = 1;
        private static int     lastSelectedMaterialIndex = 0;
        private static int     tileSize                  = 64;
        private static Vector2 tileScrollPos             = Vector2.zero;
        private static bool    showNameLabels            = false;

        private readonly GUIContent[] m_PropsTabLabels = new GUIContent[]
        {
                new GUIContent( "Search\t    " ), new GUIContent( "Current Selection    " ),
        };

        [MenuItem( "Window/Chisel/Material Browser" )]
        private static void Init()
        {
            ChiselMaterialBrowserWindow window = EditorWindow.GetWindow<ChiselMaterialBrowserWindow>( false, "Material Browser" );
            window.maxSize = new Vector2( 3840, 2160 );
            window.minSize = new Vector2( 420,  342 );
        }

        private void OnDisable()
        {
            EditorPrefs.SetInt( PREVIEW_SIZE_PREF_KEY, tileSize );
            EditorPrefs.SetBool( TILE_LABEL_PREF_KEY, showNameLabels );

            m_Tiles.ForEach( e =>
            {
                e.Dispose();
                e = null;
            } );

            m_Tiles  = null;
            m_Labels = null;

            AssetDatabase.ReleaseCachedFileHandles();
            EditorUtility.UnloadUnusedAssetsImmediate( true );

            GC.Collect( GC.GetGeneration( this ), GCCollectionMode.Forced, false, true );
        }

        private void OnEnable()
        {
            tileSize       = EditorPrefs.GetInt( PREVIEW_SIZE_PREF_KEY, 64 );
            showNameLabels = EditorPrefs.GetBool( TILE_LABEL_PREF_KEY, false );

            ChiselMaterialBrowserUtilities.GetMaterials( ref m_Tiles, ref m_Labels /*, ref m_Cache*/, false );
        }

        public  Material   previewMaterial;
        public  Editor     previewEditor;
        private Rect       m_PropsAreaRect          = Rect.zero;
        private Rect       m_NotifRect              = Rect.zero;
        private Rect       m_ToolbarRect            = Rect.zero;
        private Rect       m_LabelScrollViewArea    = Rect.zero;
        private Vector2    m_LabelScrollPositon     = Vector2.zero;
        private bool       m_ShowPropsArea          = true;
        private GUIContent m_PropsAreaToggleContent = new GUIContent( "", "" );

        private void OnGUI()
        {
            // rebuild IMGUI styling
            RebuildStyles();

            if( previewMaterial == null )
                previewMaterial = GetPreviewMaterial( lastSelectedMaterialIndex );

            m_NotifRect.x      = ( position.width  * 0.5f ) - 200;
            m_NotifRect.y      = ( position.height * 0.5f ) - 60;
            m_NotifRect.width  = 400;
            m_NotifRect.height = 150;

            if( EditorApplication.isCompiling ) { GUI.Box( m_NotifRect, "Please wait...", "NotificationBackground" ); }
            if(EditorApplication.isPlaying || EditorApplication.isPaused) GUI.Box( m_NotifRect, "Exit Playmode to Edit Materials", "NotificationBackground" );

            if( !EditorApplication.isCompiling && !EditorApplication.isPlaying )
            {
                m_ToolbarRect.x      = 0;
                m_ToolbarRect.y      = 2;
                m_ToolbarRect.width  = position.width;
                m_ToolbarRect.height = 24;

                GUI.Label( m_ToolbarRect, "", toolbarStyle );

                m_ToolbarRect.y += 25;
                GUI.Label( m_ToolbarRect, "", toolbarStyle );

                m_ToolbarRect.y     = 2;
                m_ToolbarRect.width = 60;
                if( GUI.Button( m_ToolbarRect, "Refresh", EditorStyles.toolbarButton ) )
                {
                    ChiselMaterialBrowserUtilities.GetMaterials( ref m_Tiles, ref m_Labels, false );
                    previewEditor = Editor.CreateEditor( previewMaterial = GetPreviewMaterial( 0 ) );
                }

                if( m_ShowPropsArea )
                {
                    m_ToolbarRect.x      = position.width - 262;
                    m_ToolbarRect.y      = 26;
                    m_ToolbarRect.width  = 262;
                    m_ToolbarRect.height = 24;

                    GUI.Box( m_ToolbarRect, "", "dockHeader" );
                    m_ToolbarRect.y   += 2;
                    m_ToolbarRect.x   += 4;
                    m_CurrentPropsTab =  GUI.Toolbar( m_ToolbarRect, m_CurrentPropsTab, m_PropsTabLabels, EditorStyles.toolbarButton, GUI.ToolbarButtonSize.FitToContents );

                    switch( m_CurrentPropsTab )
                    {
                        case 0:
                        {
                            m_PropsAreaRect.x      = position.width - 256;
                            m_PropsAreaRect.y      = 50;
                            m_PropsAreaRect.width  = 240;
                            m_PropsAreaRect.height = position.height;


                            m_LabelScrollViewArea.x      = position.width - 256;
                            m_LabelScrollViewArea.y      = 50;
                            m_LabelScrollViewArea.width  = 240;
                            m_LabelScrollViewArea.height = 0;

                            m_LabelScrollPositon = GUI.BeginScrollView( m_PropsAreaRect, m_LabelScrollPositon, m_LabelScrollViewArea );
                            {
                                m_PropsAreaRect.height = 24;

                                for( int i = 0; i < m_Labels.Count; i++ )
                                {
                                    m_LabelScrollViewArea.height += 24;

                                    if( i > 0 )
                                        m_PropsAreaRect.y += 26;
                                    if( GUI.Button( m_PropsAreaRect, m_Labels[i] ) )
                                    {
                                        ChiselMaterialBrowserUtilities.GetMaterials( ref m_Tiles, ref m_Labels, true, m_Labels[i], "" );
                                        lastSelectedMaterialIndex = 0;

                                        previewEditor = Editor.CreateEditor( previewMaterial = GetPreviewMaterial( 0 ) );
                                    }
                                }
                            }
                            GUI.EndScrollView();

                            break;
                        }
                        case 1:
                        {
                            m_PropsAreaRect.x      = position.width - 260;
                            m_PropsAreaRect.y      = 50;
                            m_PropsAreaRect.width  = 260;
                            m_PropsAreaRect.height = position.height - 50;

                            GUI.BeginGroup( m_PropsAreaRect, propsSectionBG );
                            {
                                m_PropsAreaRect.x      = 0;
                                m_PropsAreaRect.y      = 0;
                                m_PropsAreaRect.height = 22;
                                GUI.Label( m_PropsAreaRect, $"{m_Tiles[lastSelectedMaterialIndex].materialName}", EditorStyles.toolbarButton );

                                m_PropsAreaRect.x      = 2;
                                m_PropsAreaRect.y      = 22;
                                m_PropsAreaRect.width  = 256;
                                m_PropsAreaRect.height = 256;

                                if( m_Tiles[lastSelectedMaterialIndex] != null && previewMaterial != null )
                                {
                                    if( previewEditor == null ) previewEditor = Editor.CreateEditor( previewMaterial, typeof( MaterialEditor ) );
                                    previewEditor.OnInteractivePreviewGUI( m_PropsAreaRect, EditorStyles.whiteLabel );
                                    previewEditor.Repaint();
                                }

                                if( position.height > 520 )
                                {
                                    m_PropsAreaRect.y      = 278;
                                    m_PropsAreaRect.x      = 2;
                                    m_PropsAreaRect.height = 188;
                                    m_PropsAreaRect.width  = 256;

                                    GUI.BeginGroup( m_PropsAreaRect );
                                    {
                                        m_PropsAreaRect.x      = 0;
                                        m_PropsAreaRect.y      = 0;
                                        m_PropsAreaRect.height = 22;
                                        GUI.Label( m_PropsAreaRect, $"Material", EditorStyles.toolbarButton );

                                        m_PropsAreaRect.y += 24;
                                        if( GUI.Button( m_PropsAreaRect, "Select In Project", "largebutton" ) )
                                        {
                                            EditorGUIUtility.PingObject( Selection.activeObject = GetPreviewMaterial( lastSelectedMaterialIndex ) );
                                        }

                                        m_PropsAreaRect.y += 24;
                                        if( GUI.Button( m_PropsAreaRect, applyToSelectedFaceLabelContent, "largebutton" ) )
                                        {
                                            ApplySelectedMaterial();
                                        }
                                    }
                                    GUI.EndGroup();

                                    if( position.height >= 400 )
                                    {
                                        m_PropsAreaRect.x      = 2;
                                        m_PropsAreaRect.y      = 369;
                                        m_PropsAreaRect.height = 200;
                                        GUI.BeginGroup( m_PropsAreaRect );
                                        {
                                            m_PropsAreaRect.x      = 0;
                                            m_PropsAreaRect.y      = 0;
                                            m_PropsAreaRect.height = 22;
                                            GUI.Label( m_PropsAreaRect, "Labels", EditorStyles.toolbarButton );

                                            float btnWidth = 60;
                                            int   idx      = 0;
                                            foreach( var l in m_Tiles[lastSelectedMaterialIndex].labels )
                                            {
                                                float xOffset = 2;
                                                float yOffset = 22;
                                                int   row     = 0;
                                                for( int i = 0; i < ( m_PropsAreaRect.width / btnWidth ); i++ )
                                                {
                                                    if( idx == m_Tiles[lastSelectedMaterialIndex].labels.Length ) break;

                                                    xOffset = ( 40 * i ) + 2;
                                                    if( GUI.Button( new Rect( xOffset, yOffset, btnWidth, 22 ), new GUIContent( l, $"Click to filter for label \"{l}\"" ), assetLabelStyle ) )
                                                    {
                                                        ChiselMaterialBrowserUtilities.GetMaterials( ref m_Tiles, ref m_Labels, true, l, "" );
                                                        lastSelectedMaterialIndex = 0;

                                                        previewEditor = Editor.CreateEditor( previewMaterial = GetPreviewMaterial( 0 ) );
                                                    }

                                                    idx++;
                                                }

                                                row     += 1;
                                                yOffset =  22 + ( 22 * row );
                                            }
                                        }
                                        GUI.EndGroup();
                                    }
                                }
                            }
                            GUI.EndGroup();

                            break;
                        }
                        default: break;
                    }
                }

                m_ToolbarRect.width  = 22;
                m_ToolbarRect.height = 24;
                m_ToolbarRect.x      = position.width - 24;
                m_ToolbarRect.y      = 28;

                m_PropsAreaToggleContent.text    = ( m_ShowPropsArea ) ? ">>" : "<<";
                m_PropsAreaToggleContent.tooltip = ( m_ShowPropsArea ) ? "Hide utility bar" : "Show utility bar";
                m_ShowPropsArea                  = GUI.Toggle( m_ToolbarRect, m_ShowPropsArea, m_PropsAreaToggleContent, EditorStyles.toolbarButton );

                m_ToolbarRect.x      = position.width - 104;
                m_ToolbarRect.y      = 2;
                m_ToolbarRect.width  = 100;
                m_ToolbarRect.height = 24;

                // $TODO: change styling on this... its fugly. Also needs tooltips.
                EditorGUI.BeginChangeCheck();
                showNameLabels = GUI.Toggle( m_ToolbarRect, showNameLabels, ( showNameLabels ) ? "Hide Labels" : "Show Labels", "ToolbarButton" );
                if( EditorGUI.EndChangeCheck() ) EditorPrefs.SetBool( TILE_LABEL_PREF_KEY, showNameLabels );

                DrawTileArea();
            }
        }

        public  float      tileScrollViewHeight;
        private Rect       m_TileContentRect = Rect.zero;
        private Rect       m_ScrollViewRect  = Rect.zero;
        private GUIContent m_TileContentText = new GUIContent();

        private void DrawTileArea()
        {
            int idx        = 0;
            int numColumns = (int) ( ( position.width - ( ( m_ShowPropsArea ) ? ( 264 + 10 ) : 0 ) ) / tileSize );

            if( m_ShowPropsArea )
            {
                m_ScrollViewRect.x      = 0;
                m_ScrollViewRect.y      = 52;
                m_ScrollViewRect.width  = position.width  - 260;
                m_ScrollViewRect.height = position.height - 72;

                m_TileContentRect.x      = 0;
                m_TileContentRect.y      = 0;
                m_TileContentRect.width  = position.width - 280;
                m_TileContentRect.height = tileScrollViewHeight;
            }
            else
            {
                m_ScrollViewRect.x      = 0;
                m_ScrollViewRect.y      = 52;
                m_ScrollViewRect.width  = position.width;
                m_ScrollViewRect.height = position.height - 72;

                m_TileContentRect.x      = 0;
                m_TileContentRect.y      = 0;
                m_TileContentRect.width  = position.width - 20;
                m_TileContentRect.height = tileScrollViewHeight;
            }

            tileScrollPos = GUI.BeginScrollView( m_ScrollViewRect, tileScrollPos, m_TileContentRect, false, true );
            {
                float xOffset = 0;
                float yOffset = 0;
                int   row     = 0;

                foreach( var entry in m_Tiles )
                {
                    if( idx == m_Tiles.Count ) break;

                    // begin horizontal
                    for( int x = 0; x < numColumns; x++ )
                    {
                        xOffset = ( tileSize * x ) + 4;

                        if( idx == m_Tiles.Count ) break;

                        m_TileContentRect.x      = xOffset;
                        m_TileContentRect.y      = yOffset;
                        m_TileContentRect.width  = tileSize - 2;
                        m_TileContentRect.height = tileSize - 4;

                        m_TileContentText.image   = m_Tiles[idx].Preview;
                        m_TileContentText.tooltip = $"{m_Tiles[idx].materialName}\n\nClick to apply to the currently selected surface.";

                        if( m_Tiles[idx].CheckVisible( yOffset, tileSize, tileScrollPos, tileScrollViewHeight ) )
                        {
                            SetButtonStyleBG( in idx );

                            //Debug.Log( $"IsInView for element {idx} is true, show thumbnail for material {m_Tiles[idx].materialName}" );
                            if( GUI.Button( m_TileContentRect, m_TileContentText, tileButtonStyle ) )
                            {
                                previewMaterial = GetPreviewMaterial( idx );
                                previewEditor   = null;

                                lastSelectedMaterialIndex = idx;

                                ApplySelectedMaterial();
                            }

                            if( !m_TileContentRect.Contains( Event.current.mousePosition ) && tileSize > 100 && showNameLabels )
                            {
                                m_TileContentRect.height = 22;
                                m_TileContentRect.y      = ( m_TileContentRect.y ) + tileSize - 22;

                                GUI.Box( m_TileContentRect, "", tileLabelBGStyle );

                                m_TileContentRect.x     += 4;
                                m_TileContentRect.width -= 8;

                                GUI.Label( m_TileContentRect, m_Tiles[idx].materialName, tileLabelStyle );
                            }
                        }
                        //else Debug.Log( $"IsInView for element {idx} is false, not drawing." );

                        idx++;
                    }

                    row                  += 1;
                    yOffset              =  ( tileSize * row );
                    tileScrollViewHeight =  yOffset;

                    // end horizontal
                }
            }
            GUI.EndScrollView();

            m_TileContentRect.x      = 0;
            m_TileContentRect.y      = position.height - 20;
            m_TileContentRect.width  = position.width;
            m_TileContentRect.height = 20;

            GUI.Box( m_TileContentRect, "", "toolbar" );

            m_TileContentRect.width = 400;
            GUI.Label( m_TileContentRect, $"Materials: {m_Tiles.Count} | Labels: {m_Labels.Count}" );

            // if the properties area is shown:
            // check for the window width, if its 500px or narrower, then move the slider all the way to the right
            // otherwise, align it with the tile view.
            m_TileContentRect.x      = ( m_ShowPropsArea ) ? ( ( position.width < 500 ) ? position.width - 90 : position.width - 360 ) : position.width - 90;
            m_TileContentRect.y      = position.height - 20;
            m_TileContentRect.width  = 80;
            m_TileContentRect.height = 24;

            EditorGUI.BeginChangeCheck();
            tileSize = (int) GUI.HorizontalSlider( m_TileContentRect, tileSize, 64, 122 );
            if( EditorGUI.EndChangeCheck() ) EditorPrefs.SetInt( PREVIEW_SIZE_PREF_KEY, tileSize );
        }

        private static Material GetPreviewMaterial( int index )
        {
            Material m = AssetDatabase.LoadAssetAtPath<Material>( AssetDatabase.GUIDToAssetPath( m_Tiles[index].guid ) );

            if( m == null ) return ChiselMaterialManager.DefaultMaterial;

            return m;
        }

        [Shortcut( "Chisel/Material Browser/Apply Last Selected Material", ChiselKeyboardDefaults.ApplyLastSelectedMaterialKey, ChiselKeyboardDefaults.ApplyLastSelectedMaterialModifier )]
        public static void ApplySelectedMaterial()
        {
            if( lastSelectedMaterialIndex > m_Tiles.Count )
                return;

            if( m_Tiles[lastSelectedMaterialIndex] != null ) { ApplyMaterial( GetPreviewMaterial( lastSelectedMaterialIndex ) ); }
        }

        private static void ApplyMaterial( Material material )
        {
            if( ChiselSurfaceSelectionManager.HaveSelection )
            {
                HashSet<SurfaceReference> selected = ChiselSurfaceSelectionManager.Selection;

                foreach( SurfaceReference surf in selected ) { surf.BrushMaterial.RenderMaterial = material; }
            }
            //else Debug.Log( "No surface selected, please select a surface before applying a material." );
        }
    }
}
