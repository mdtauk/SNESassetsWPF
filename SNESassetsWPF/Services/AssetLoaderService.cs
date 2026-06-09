using SNESassetsWPF.Formats;
using SNESassetsWPF.Models;
using System;
using System.Diagnostics;





namespace SNESassetsWPF.Services
{
    public class AssetLoaderService
    {
        //
        // ─────────────────────────────────────────────────────────────
        //  COL
        // ─────────────────────────────────────────────────────────────
        //
        public LoadedAsset<ColFile> LoadCol(string path)
        {
            try
            {
                var reader = new ColFileReader();
                var readResult = reader.Read(path);

                if ( !readResult.Success )
                {
                    Debug.WriteLine( "COL read error: " + readResult.ErrorMessage );
                    return null;
                }

                var col = ColFileParser.Parse(readResult);   // ← use the reader’s slice, not RawFile

                return new LoadedAsset<ColFile>( path , col );
            }
            catch ( Exception ex )
            {
                Debug.WriteLine( $"COL load error: {ex.Message}" );
                return null;
            }
        }


        //
        // ─────────────────────────────────────────────────────────────
        //  CGX
        // ─────────────────────────────────────────────────────────────
        //
        public LoadedAsset<CgxFile> LoadCgx(string path)
        {
            try
            {
                var reader = new CgxFileReader();
                var readResult = reader.Read(path);

                if ( !readResult.Success )
                {
                    Debug.WriteLine( "CGX read error: " + readResult.ErrorMessage );
                    return null;
                }

                var parser = new CgxFileParser();
                var cgx = parser.Parse(readResult.RawFile);

                CgxVerify.DumpSummary( cgx );

                return new LoadedAsset<CgxFile>( path , cgx );
            }
            catch ( Exception ex )
            {
                Debug.WriteLine( $"CGX load error: {ex.Message}" );
                return null;
            }
        }

        //
        // ─────────────────────────────────────────────────────────────
        //  SCR
        // ─────────────────────────────────────────────────────────────
        //
        public LoadedAsset<ScrFile> LoadScr(string path)
        {
            try
            {
                var readResult = ScrFileReader.Load(path);

                if ( !readResult.Success )
                {
                    Debug.WriteLine( "SCR read error: " + readResult.ErrorMessage );
                    return null;
                }

                var scr = ScrFileParser.Parse(readResult.RawFile);

                ScrVerify.DumpSummary( scr );

                return new LoadedAsset<ScrFile>( path , scr );
            }
            catch ( Exception ex )
            {
                Debug.WriteLine( $"SCR load exception: {ex.Message}" );
                return null;
            }
        }




        //
        // ─────────────────────────────────────────────────────────────
        //  PNL
        // ─────────────────────────────────────────────────────────────
        //
        public LoadedAsset<PnlFile> LoadPnl(string path)
        {
            try
            {
                var reader = new PnlFileReader();
                var readResult = reader.Read(path);

                if ( !readResult.Success )
                {
                    Debug.WriteLine( "PNL read error: " + readResult.ErrorMessage );
                    return null;
                }

                // Parser now accepts PnlFileReadResult
                var pnl = PnlFileParser.Parse(readResult.RawFile);

                PnlVerify.DumpSummary( pnl );

                return new LoadedAsset<PnlFile>( path , pnl );
            }
            catch ( Exception ex )
            {
                Debug.WriteLine( $"PNL load exception: {ex.Message}" );
                return null;
            }
        }




        //
        // ─────────────────────────────────────────────────────────────
        //  MAP
        // ─────────────────────────────────────────────────────────────
        //
        public LoadedAsset<MapFile> LoadMap(string path)
        {
            try
            {
                var readResult = MapFileReader.Load(path);

                if ( !readResult.Success )
                {
                    Debug.WriteLine( "MAP read error: " + readResult.ErrorMessage );
                    return null;
                }

                var map = MapFileParser.Parse(readResult.RawFile);

                MapVerify.DumpSummary( map );

                return new LoadedAsset<MapFile>( path , map );

            }
            catch ( Exception ex )
            {
                Debug.WriteLine( $"MAP load exception: {ex.Message}" );
                return null;
            }
        }
    }
}
