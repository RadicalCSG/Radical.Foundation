/* * * * * * * * * * * * * * * * * * * * * *
Chisel.Editors.ChiselMaterialBrowserCache.cs

License:
Author: Daniel Cornelius

* * * * * * * * * * * * * * * * * * * * * */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json;

// $TODO: unreachable code detected, ignoring because of debug toggle. maybe worth doing a different way
#pragma warning disable 162

namespace Chisel.Editors
{
    [Serializable]
    internal class ChiselMaterialBrowserCache
    {
        [JsonProperty( "name", Required = Required.Always, Order = 0 )]
        public string Name => ConstructPath();

        [JsonIgnore, NonSerialized]
        private const bool debug = false;

        [Serializable]
        public struct CachedThumbnail
        {

            [JsonProperty( "name", Required = Required.Always, Order = 0 )]
            public string name;

            [JsonProperty( "hash", Required = Required.Always, Order = 1 )]
            public int hashCode;

            [JsonProperty( "thumbnail_b64", Required = Required.Always, Order = 2 )]
            public string data;

            public Texture2D GetThumbnail()
            {
                Texture2D temp = new Texture2D( 1, 1, TextureFormat.RGBA32, false, PlayerSettings.colorSpace == ColorSpace.Linear );

                if( !string.IsNullOrEmpty( data ) )
                {
                    temp.LoadImage( Convert.FromBase64String( data ) );
                    temp.Apply();

                    if( debug )
                        Debug.Log( $"Loaded thumbnail for material: [{this.name}]" );

                    return temp;
                }

                return ChiselEmbeddedTextures.TemporaryTexture;
            }
        }


        [JsonProperty( "stored_thumbnail_data", Required = Required.Always, Order = 1 )]
        public List<CachedThumbnail> storedPreviewTextures = new List<CachedThumbnail>();

        [JsonIgnore]
        public int NumEntries => storedPreviewTextures.Count;

        public bool HasEntry( int hash )
        {
            foreach( var e in storedPreviewTextures )
            {
                if( e.hashCode == hash ) return true;
            }

            return false;
        }

        public void AddEntry( CachedThumbnail entry )
        {
            if( !HasEntry( entry.hashCode ) )
            {
                storedPreviewTextures.Add( entry );

                if( debug )
                    Debug.Log( $"Added entry for material: [{entry.name}]" );
            }
        }

        public Texture2D GetThumbnail( int hash )
        {
            // if the name exists in the list, return its thumbnail
            return storedPreviewTextures.FirstOrDefault( e => e.hashCode == hash ).GetThumbnail();
        }

        public static string ConstructPath()
        {
            string path = Application.dataPath.Replace( "/", $"{Path.DirectorySeparatorChar}" );

            path = Directory.GetParent( path ).FullName;

            if( !path.EndsWith( $"{Path.DirectorySeparatorChar}" ) ) path += Path.DirectorySeparatorChar;

            return $"{path}material_browser_cache.json";
        }

        private static ChiselMaterialBrowserCache CreateDefault()
        {
            ChiselMaterialBrowserCache cache = new ChiselMaterialBrowserCache();

            cache.Save();

            if( debug )
                Debug.Log( $"Couldnt find cache, creating one." );

            return cache;
        }

        public void Save()
        {
            File.WriteAllText( ConstructPath(), JsonConvert.SerializeObject( this, Formatting.Indented ) );
        }

        public static ChiselMaterialBrowserCache Load()
        {
            if( !File.Exists( ConstructPath() ) ) { return CreateDefault(); }

            return JsonConvert.DeserializeObject<ChiselMaterialBrowserCache>( File.ReadAllText( ConstructPath(), Encoding.UTF8 ) );
        }
    }
}
