/* * * * * * * * * * * * * * * * * * * * * *
Chisel.Editors.ChiselMaterialBrowserWindow.cs

License: MIT (https://tldrlegal.com/license/mit-license)
Author: Daniel Cornelius

* * * * * * * * * * * * * * * * * * * * * */

#define CHISEL_MWIN_DEBUG_UI

using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace Chisel.Editors
{
    internal sealed partial class ChiselMaterialBrowserWindow
    {
        public GUIStyle tileButtonStyle;
        public GUIStyle assetLabelStyle;
        public GUIStyle toolbarStyle;
        public GUIStyle propsSectionBG;
        public GUIStyle tileLabelStyle;
        public GUIStyle tileLabelBGStyle;

        private Texture2D m_TileButtonBGTexHover;
        private Texture2D m_TileButtonBGTex;

        private GUIContent applyToSelectedFaceLabelContent;

        private void RebuildStyles()
        {
#if !CHISEL_MWIN_DEBUG_UI // update every frame while debugging, helps when changing color without the need to re-init the window
            if( m_TileButtonBGTexHover == null )
#endif
            {
                m_TileButtonBGTexHover = new Texture2D( 32, 32, TextureFormat.RGBA32, false, PlayerSettings.colorSpace == ColorSpace.Linear );

                for( int i = 0; i < 32; i++ )
                {
                    for( int j = 0; j < 32; j++ ) { m_TileButtonBGTexHover.SetPixel( i, j, new Color32( 50, 80, 65, 255 ) ); }
                }

                m_TileButtonBGTexHover.Apply();
            }

#if !CHISEL_MWIN_DEBUG_UI
            if( m_TileButtonBGTex == null )
#endif
            {
                m_TileButtonBGTex = new Texture2D( 32, 32, TextureFormat.RGBA32, false, PlayerSettings.colorSpace == ColorSpace.Linear );

                for( int i = 0; i < 32; i++ )
                {
                    for( int j = 0; j < 32; j++ ) { m_TileButtonBGTex.SetPixel( i, j, new Color32( 32, 32, 32, 255 ) ); }
                }

                m_TileButtonBGTex.Apply();
            }

#if CHISEL_MWIN_DEBUG_UI

            applyToSelectedFaceLabelContent = new GUIContent
            (
                    "Apply to Selected Face",
                    $"Apply the currently selected material to the face selected in the scene view. Shortcut: {ShortcutManager.instance.GetShortcutBinding( "Chisel/Material Browser/Apply Last Selected Material" )}"
            );

            assetLabelStyle = new GUIStyle( "assetLabel" ) { alignment = TextAnchor.UpperCenter };
            toolbarStyle    = new GUIStyle( "dragtab" ) { fixedHeight  = 0, fixedWidth = 0 };
            propsSectionBG  = new GUIStyle( "flow background" );

            tileLabelBGStyle = new GUIStyle( "box" )
            {
                    normal = { background = ChiselEmbeddedTextures.BlackTexture, scaledBackgrounds = new[] { ChiselEmbeddedTextures.BlackTexture } }
            };

            tileLabelStyle = new GUIStyle()
            {
                    //font      = ChiselEmbeddedFonts.Consolas,
                    fontSize  = 10,
                    fontStyle = FontStyle.Normal,
                    alignment = TextAnchor.MiddleLeft,
                    normal    = { textColor = Color.white },
                    clipping  = TextClipping.Clip
            };

            tileButtonStyle = new GUIStyle()
            {
                    margin        = new RectOffset( 2, 2, 2, 2 ),
                    padding       = new RectOffset( 2, 2, 2, 2 ),
                    contentOffset = new Vector2( 1, 0 ),
                    //border  = new RectOffset( 1, 0, 1, 1 ),
                    normal        = { background = m_TileButtonBGTex },
                    hover         = { background = m_TileButtonBGTexHover },
                    active        = { background = Texture2D.redTexture },
                    imagePosition = ImagePosition.ImageOnly
                    //onNormal = { background = Texture2D.grayTexture }
            };

#else
            applyToSelectedFaceLabelContent ??= new GUIContent
            (
                    "Apply to Selected Face",
                    $"Apply the currently selected material to the face selected in the scene view. Shortcut: {ShortcutManager.instance.GetShortcutBinding( "Chisel/Material Browser/Apply Last Selected Material" )}"
            );

            assetLabelStyle ??= new GUIStyle( "assetlabel" ) { alignment = TextAnchor.UpperCenter };
            toolbarStyle ??= new GUIStyle( "dragtab" ) { fixedHeight = 0, fixedWidth = 0 };
            propsSectionBG ??= new GUIStyle( "flow background" );

            tileLabelBGStyle ??= new GUIStyle( "box" )
            {
                    normal = { background = ChiselEmbeddedTextures.BlackTexture, scaledBackgrounds = new[] { ChiselEmbeddedTextures.BlackTexture } }
            };

            tileLabelStyle ??= new GUIStyle()
            {
                    font = ChiselEmbeddedFonts.Consolas,
                    fontSize = 12,
                    fontStyle = FontStyle.Normal,
                    alignment = TextAnchor.MiddleLeft,
                    normal = { textColor = Color.white },
                    clipping = TextClipping.Clip
            };

            tileButtonStyle ??= new GUIStyle()
            {
                    margin = new RectOffset( 2, 2, 2, 2 ),
                    padding = new RectOffset( 2, 2, 2, 2 ),
                    contentOffset = new Vector2( 1, 0 ),
                    //border  = new RectOffset( 1, 0, 1, 1 ),
                    normal = { background = m_TileButtonBGTex },
                    hover = { background = m_TileButtonBGTexHover },
                    active = { background = Texture2D.redTexture },
                    imagePosition = ImagePosition.ImageOnly
                    //onNormal = { background = Texture2D.grayTexture }
            };
#endif


            if( mouseOverWindow == this )
                Repaint();
        }

        private void SetButtonStyleBG( in int index )
        {
            if( tileButtonStyle != null )
            {
                if( index == lastSelectedMaterialIndex ) { tileButtonStyle.normal.background = m_TileButtonBGTexHover; }
                else
                {
                    if( tileButtonStyle.normal.background != m_TileButtonBGTex ) tileButtonStyle.normal.background = m_TileButtonBGTex;
                }
            }
        }
    }
}
