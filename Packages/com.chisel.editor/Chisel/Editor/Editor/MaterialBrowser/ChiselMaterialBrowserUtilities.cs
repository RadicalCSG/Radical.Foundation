/* * * * * * * * * * * * * * * * * * * * * *
Chisel.Editors.ChiselMaterialBrowserCache.cs

License: MIT (https://tldrlegal.com/license/mit-license)
Author: Daniel Cornelius

$TODO: Do we want to filter by label, too? it would allow user-ignored materials.
* * * * * * * * * * * * * * * * * * * * * */

using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Chisel.Editors
{
    internal static class ChiselMaterialBrowserUtilities
    {
        // checks a path and returns true/false if a material is ignored or not
        public static bool IsValidEntry( ChiselMaterialBrowserTile tile )
        {
            // these are here to clean things up a little bit and make it easier to read

            bool PathContains( string path )
            {
                return tile.path.ToLower().Contains( path );
            }

            // checks for any shaders we want to exclude
            bool HasInvalidShader()
            {
                string shader = tile.shaderName.ToLower();

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

        // step val by powers of two
        public static int GetPow2( int val )
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


        // gets all materials and the labels on them in the project, compares them against a filter,
        // and then adds them to the list of materials to be used in this window
        public static void GetMaterials( ref List<ChiselMaterialBrowserTile> materials,
                                         ref List<string>                    labels,
                                         //ref ChiselMaterialBrowserCache      cache,
                                         bool   usingLabel,
                                         string searchLabel = "",
                                         string searchText  = "" )
        {
            if( usingLabel && searchLabel == string.Empty )
                Debug.LogError( $"usingLabel set to true, but no search term was given. This may give undesired results." );

            materials.Clear();

            ChiselMaterialThumbnailRenderer.CancelAll();

            // exclude the label search tag if we arent searching for a specific label right now
            string search = usingLabel ? $"l:{searchLabel} {searchText}" : $"{searchText}";

            string[] guids = AssetDatabase.FindAssets( $"t:Material {search}" );

            AssetPreview.SetPreviewTextureCacheSize( materials.Capacity = guids.Length + 1 );

            // assemble preview tiles
            foreach( var id in guids )
            {
                ChiselMaterialBrowserTile browserTile = new ChiselMaterialBrowserTile( id ); //, ref cache );

                if( labels != null )
                {
                    // add any used labels we arent currently storing
                    foreach( string label in browserTile.labels )
                    {
                        if( !labels.Contains( label ) )
                            labels.Add( label );
                    }
                }

                // check each entry against a filter to exclude certain entries
                if( IsValidEntry( browserTile ) )
                {
                    // if we have the material already, skip, else add it
                    materials.Add( browserTile );
                }
            }

            //Debug.Log( $"Found {materials.Count} materials with applied filters." );
            //cache.Save();
        }

        public static Texture2D GetAssetPreviewFromGUID( string guid )
        {
            MethodInfo info = typeof( AssetPreview ).GetMethod(
                    "GetAssetPreviewFromGUID",
                    BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod,
                    null,
                    new [] { typeof( string ) },
                    null
            );

            return info.Invoke( null, new object[] { guid } ) as Texture2D;
        }
    }
} //
