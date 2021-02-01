/* * * * * * * * * * * * * * * * * * * * * *
Chisel.Editors.ChiselMaterialBrowserTile.cs

License: MIT (https://tldrlegal.com/license/mit-license)
Author: Daniel Cornelius

* * * * * * * * * * * * * * * * * * * * * */

using System;
using System.Text;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using Object = System.Object;

namespace Chisel.Editors
{
    internal class ChiselMaterialBrowserTile : IDisposable
    {
        public readonly string   path;
        public readonly string   guid;
        public readonly string   shaderName;
        public readonly string   materialName;
        public readonly string[] labels;

        public  Texture2D Preview => m_Preview;
        private Texture2D m_Preview;

        /// <inheritdoc />
        public void Dispose()
        {
            m_Preview = null;
        }

        public bool CheckVisible( float yOffset, float thumbnailSize, Vector2 scrollPos, float scrollViewHeight )
        {
            if( scrollPos.y + scrollViewHeight < ( yOffset - thumbnailSize ) ) return false;
            if( yOffset     + thumbnailSize    < scrollPos.y ) return false;
            return true;
        }

        public ChiselMaterialBrowserTile( string instID )
        {
            path = AssetDatabase.GUIDToAssetPath( instID );

            Material m = AssetDatabase.LoadAssetAtPath<Material>( path );

            int id = m.GetInstanceID();
            guid         = instID;
            labels       = AssetDatabase.GetLabels( m );
            shaderName   = m.shader.name;
            materialName = m.name;

            m = null;

            if( !materialName.Contains( "Font Material" ) ) // dont even consider font materials
            {
                if( ChiselMaterialBrowserUtilities.IsValidEntry( this ) )
                {
                    ChiselMaterialThumbnailRenderer.Add( materialName, () => !AssetPreview.IsLoadingAssetPreview( id ),
                                                         () => { m_Preview = ChiselMaterialBrowserUtilities.GetAssetPreviewFromGUID( guid ); } );
                }
            }
        }
    }
}
