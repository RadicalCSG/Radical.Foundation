/* * * * * * * * * * * * * * * * * * * * * *
Chisel.Editors.MaterialBrowserWindow.cs

License: MIT (https://tldrlegal.com/license/mit-license)
Author: Daniel Cornelius

$TODO: Optimize away all the GUILayout logic
$TODO: Implement culling for tiles not visible (hide them if they arent within the viewable window area)
$TODO: Do we want to filter by label, too? it would allow user-ignored materials.
$TODO: Optimize optimize optimize

* * * * * * * * * * * * * * * * * * * * * */

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Chisel.Editors
{
    internal class MaterialBrowserWindow : EditorWindow
    {
        private Vector2 m_PreviewsScrollPosition = Vector2.zero;
        private Vector2 m_LabelsScrollPosition   = Vector2.zero;

        private static int    m_PreviewSize     = 128;
        private static string m_SearchFieldText = string.Empty;
        private static string m_LabelSearchText = string.Empty;

        private static List<MaterialBrowserTile> m_Materials = new List<MaterialBrowserTile>();
        private static List<string>              m_Labels    = new List<string>();

        private const string PREVIEW_SIZE_PREF_NAME = "chisel_matbrowser_pviewSize";

        [MenuItem( "Window/Chisel/Material Browser" )]
        private static void Init()
        {
            MaterialBrowserWindow window = EditorWindow.GetWindow<MaterialBrowserWindow>( false, "Material Browser" );
            window.maxSize = new Vector2( 1920, 2000 );
            window.minSize = new Vector2( 200,  100 );

            m_PreviewSize = EditorPrefs.GetInt( PREVIEW_SIZE_PREF_NAME, 128 );

            GetMaterials();
        }

        private void OnEnable()
        {
            m_PreviewSize = EditorPrefs.GetInt( PREVIEW_SIZE_PREF_NAME, 128 );

            if( m_Materials == null || m_Materials.Count < 1 )
                GetMaterials();
        }

        private void OnGUI()
        {
            Rect rect = this.position;

            // toolbar
            using( GUILayout.HorizontalScope hScope = new GUILayout.HorizontalScope( EditorStyles.toolbar, GUILayout.ExpandWidth( true ) ) )
            {
                if( GUILayout.Button( "Refresh", EditorStyles.toolbarButton ) )
                {
                    m_LabelSearchText = string.Empty;
                    GetMaterials();
                }

                GUILayout.FlexibleSpace();
                if( m_LabelSearchText.Length > 0 )
                {
                    if( GUILayout.Button( "x", EditorStyles.toolbarButton, GUILayout.Width( 24 ) ) )
                    {
                        m_LabelSearchText = string.Empty;
                        GetMaterials( false );
                    }

                    GUILayout.Label( $"Label Search: {m_LabelSearchText}", EditorStyles.toolbarButton, GUILayout.Width( 160 ) );
                }

                string lastText = "";
                m_SearchFieldText = EditorGUILayout.DelayedTextField( lastText = m_SearchFieldText, EditorStyles.toolbarSearchField );

                if( m_SearchFieldText != lastText )
                    GetMaterials();
            }

            // header bar
            GUILayout.BeginHorizontal();
            GUILayout.Label( "Asset Labels (used)", EditorStyles.toolbarButton, GUILayout.Width( 120 ) );
            GUILayout.Label( "",                    EditorStyles.toolbarButton, GUILayout.ExpandWidth( true ) );
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                // tag bar
                using( GUILayout.ScrollViewScope lScope = new GUILayout.ScrollViewScope( m_LabelsScrollPosition, false, false, GUILayout.ExpandHeight( true ), GUILayout.Width( 120 ) ) )
                {
                    m_LabelsScrollPosition = lScope.scrollPosition;
                    GUILayout.BeginVertical( GUILayout.ExpandHeight( true ), GUILayout.ExpandWidth( true ) );
                    {
                        foreach( string label in m_Labels )
                        {
                            if( GUILayout.Button( label ) )
                            {
                                m_LabelSearchText = label;
                                GetMaterials( true );
                            }
                        }
                    }
                    GUILayout.EndVertical();
                }

                // previews area
                GUILayout.BeginVertical( "GameViewBackground" );
                {
                    // view window
                    using( GUILayout.ScrollViewScope svScope = new GUILayout.ScrollViewScope( m_PreviewsScrollPosition, false, true, GUILayout.ExpandHeight( true ), GUILayout.ExpandWidth( true ) ) )
                    {
                        m_PreviewsScrollPosition = svScope.scrollPosition;

                        int idx = 0;
                        int numColumns = (int) ( ( rect.width - 130 ) / m_PreviewSize );

                        foreach( var entry in m_Materials )
                        {
                            if( idx == m_Materials.Count ) break;

                            GUILayout.BeginHorizontal();
                            for( int x = 0; x < numColumns; x++ )
                            {
                                if( idx == m_Materials.Count ) break;

                                if( m_Materials[idx].preview != null && m_Materials[idx].material != null )
                                {
                                    GUIContent previewContent = new GUIContent( m_Materials[idx].preview, $"{m_Materials[idx].material.name}\nIn: [{m_Materials[idx].path}]" );

                                    if( GUILayout.Button( previewContent, GUILayout.Height( m_PreviewSize ), GUILayout.Width( m_PreviewSize ) ) ) {}
                                }

                                idx++;
                            }

                            GUILayout.EndHorizontal();
                        }
                    }
                }
                GUILayout.EndVertical(); // previews area
            }
            GUILayout.EndHorizontal(); // tag & previews area

            // bottom toolbar
            using( GUILayout.HorizontalScope toolbarScope = new GUILayout.HorizontalScope( EditorStyles.toolbar, GUILayout.ExpandWidth( true ) ) )
            {
                GUILayout.Label( $"Materials: {m_Materials.Count}" );

                GUILayout.FlexibleSpace();

                int lastSize;
                m_PreviewSize = EditorGUILayout.IntSlider( new GUIContent( "", "Preview Size" ), lastSize = m_PreviewSize, 32, 128, GUILayout.Width( 200 ) );
                m_PreviewSize = GetPow2( m_PreviewSize );

                if( m_PreviewSize != lastSize )
                    EditorPrefs.SetInt( PREVIEW_SIZE_PREF_NAME, m_PreviewSize );
            }
        }

        // gets all materials and the labels on them in the project, compares them against a filter,
        // and then adds them to the list of materials to be used in this window
        private static void GetMaterials( bool usingLabel = false )
        {
            m_Materials.Clear();

            // exclude the label search tag if we arent searching for a specific label right now
            string search = usingLabel ? $"l:{m_LabelSearchText} {m_SearchFieldText}" : $"{m_SearchFieldText}";

            string[] guids = AssetDatabase.FindAssets( $"t:Material {search}" );

            // assemble preview tiles
            foreach( var id in guids )
            {
                MaterialBrowserTile browserTile = new MaterialBrowserTile();
                browserTile.path     = AssetDatabase.GUIDToAssetPath( id );
                browserTile.material = AssetDatabase.LoadAssetAtPath<Material>( browserTile.path );
                browserTile.preview  = AssetPreview.GetAssetPreview( browserTile.material );
                browserTile.labels   = AssetDatabase.GetLabels( browserTile.material );

                // add any used labels we arent currently storing
                foreach( string label in browserTile.labels )
                {
                    if( !m_Labels.Contains( label ) )
                        m_Labels.Add( label );
                }

                // check each entry against a filter to exclude certain entries
                if( IsValidEntry( browserTile ) )
                {
                    // if we have the material already, skip, else add it
                    if( m_Materials.Contains( browserTile ) ) break;
                    else m_Materials.Add( browserTile );
                }
            }
        }

        // checks a path and returns true/false if a material is ignored or not
        private static bool IsValidEntry( MaterialBrowserTile tile )
        {
            // these are here to clean things up a little bit and make it easier to read

            bool PathContains( string path )
            {
                return tile.path.ToLower().Contains( path );
            }

            // checks for any shaders we want to exclude
            bool HasInvalidShader()
            {
                string shader = tile.material.shader.name.ToLower();

                string[] excludedShaders = new string[]
                {
                        "skybox/"
                };

                return shader.Contains( excludedShaders[0] );
            }

            string chiselPath = "packages/com.chisel.components/package resources/";

            string[] ignoredEntries = new string[]
            {
                    "packages/com.unity.searcher/",    // 0, we ignore this to get rid of the built-in font materials
                    "packages/com.unity.entities/",    // 1, we ignore this to get rid of the entities materials
                    $"{chiselPath}preview materials/", // 2, these are tool textures, so we are ignoring them
            };

            // if the path contains any of the ignored paths, then this will return false
            bool valid = !PathContains( ignoredEntries[0] )
                         && !PathContains( ignoredEntries[1] )
                         && !PathContains( ignoredEntries[2] )
                         && !HasInvalidShader(); // also check the shader

            return valid;
        }

        // used by the preview size slider to step the preview size by powers of two
        private static int GetPow2( int val )
        {
            val--;
            val |= val >> 1;
            val |= val >> 2;
            val |= val >> 4;
            val |= val >> 8;
            val |= val >> 16;
            val++;

            return val;
        }
    }
}
