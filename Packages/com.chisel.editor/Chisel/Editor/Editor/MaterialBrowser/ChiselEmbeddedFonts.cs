/* * * * * * * * * * * * * * * * * * * * * *
Chisel.Editors.ChiselEmbeddedFonts.cs

License:
Author: Daniel Cornelius

* * * * * * * * * * * * * * * * * * * * * */

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Chisel.Editors
{
    public static class ChiselEmbeddedFonts
    {
        private static List<string> fonts = new List<string>();

        private static Font m_Consolas;
        public static Font Consolas
        {
            get
            {
                if( m_Consolas == null )
                {
                    m_Consolas = GetSystemFont( "Consolas", 24 );
                }

                return m_Consolas;
            }
        }

        private static Font GetSystemFont( string fontName, int defaultFontSize = 12 )
        {
            if( fonts.Count < 1 ) fonts = Font.GetOSInstalledFontNames().ToList();

            Font font = new Font();

            fonts.ForEach( x =>
            {
                if( x.Equals( fontName ) )
                {
                    font = Font.CreateDynamicFontFromOSFont( x, defaultFontSize );
                }
            } );

            return font;
        }
    }
}
