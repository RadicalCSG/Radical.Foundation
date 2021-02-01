/* * * * * * * * * * * * * * * * * * * * * *
Chisel.Editors.ChiselMaterialThumbnailRenderer.cs

License: MIT (https://tldrlegal.com/license/mit-license)
Author: Daniel Cornelius

* * * * * * * * * * * * * * * * * * * * * */

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// $TODO: unreachable code detected, ignoring because of debug toggle. maybe worth doing a different way
#pragma warning disable 162

namespace Chisel.Editors
{
    // slightly modified from http://answers.unity.com/answers/243291/view.html
    internal static class ChiselMaterialThumbnailRenderer
    {
        private const bool debug = false;

        private class RenderJob
        {
            // used for debug
            public string taskName;

            public Func<bool> Completed    { get; }
            public Action     ContinueWith { get; }

            public RenderJob( string name, Func<bool> completed, Action continueWith )
            {
                Completed    = completed;
                ContinueWith = continueWith;
                taskName     = name;
            }
        }

        private static readonly List<RenderJob> jobs = new List<RenderJob>();

        public static void Add( string name, Func<bool> completed, Action continueWith )
        {
            if( !jobs.Any() ) EditorApplication.update += Update;
            jobs.Add( new RenderJob( name, completed, continueWith ) );
        }

        private static void Update()
        {
            if( jobs.Count > 0 )
            {
                int i = 0;
                for( i = 0; i >= 0; i-- )
                {
                    if( jobs[i].Completed() )
                    {
                        if( debug )
                            Debug.Log( $"Completed thumbnail render task for [{jobs[i].taskName}]" );

                        jobs[i].ContinueWith();
                        jobs.RemoveAt( i );
                    }
                }
            }

            if( !jobs.Any() ) EditorApplication.update -= Update;
        }

        public static void CancelAll()
        {
            jobs.Clear();
        }
    }
}
