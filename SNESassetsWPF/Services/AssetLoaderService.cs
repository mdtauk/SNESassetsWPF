using SNESassetsWPF.Formats;
using SNESassetsWPF.Models;
using System;
using System.Diagnostics;
using static SNESassetsWPF.Formats.ColFileReadResult;





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

                // -------------------------------------------------
                // 1. Classify the COL file
                // -------------------------------------------------
                ColFileParser.ClassifyColStructure( readResult );

                // -------------------------------------------------
                // 2. Parse based on classification
                // -------------------------------------------------
                ColFile col;

                switch ( readResult.Format )
                {
                    case ColFormatType.Valid:
                        col = ColFileParser.ParseStrict( readResult );
                        break;

                    case ColFormatType.Warn:
                        col = ColFileParser.ParsePartial( readResult );
                        break;

                    case ColFormatType.Fail:
                    default:
                        Debug.WriteLine( "COL format invalid or unsupported." );
                        return null;
                }


                // -------------------------------------------------
                // 3. Optional: dump summary for debugging
                // -------------------------------------------------
                ColVerify.DumpSummary( readResult , col );

                return new LoadedAsset<ColFile>( path , col , readResult );
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

                // 1. Classify structure
                CgxFileParser.ClassifyCgxStructure( readResult );

                if ( readResult.Format == CgxFileReadResult.CgxFormatType.Fail )
                {
                    Debug.WriteLine( "CGX format invalid or unsupported." );
                    return null;
                }

                // 2. Parse based on classification
                CgxFile cgx;
                switch ( readResult.Format )
                {
                    case CgxFileReadResult.CgxFormatType.Valid:
                        cgx = CgxFileParser.ParseStrict( readResult );
                        break;

                    case CgxFileReadResult.CgxFormatType.Warn:
                        cgx = CgxFileParser.ParsePartial( readResult );
                        break;

                    default:
                        Debug.WriteLine( "CGX format unknown or unsupported." );
                        return null;
                }

                if ( cgx == null )
                {
                    Debug.WriteLine( "CGX parse returned null." );
                    return null;
                }

                CgxVerify.DumpSummary( cgx );

                Debug.WriteLine( "CGX LOADER: ReadResult attached = " + ( readResult != null ) );


                return new LoadedAsset<CgxFile>( path , cgx , readResult );
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

                // -------------------------------------------------
                // 1. Classify BEFORE parsing
                // -------------------------------------------------
                ScrFileParser.ClassifyScrStructure( readResult.RawFile , readResult );

                if ( readResult.Format == ScrFileReadResult.ScrFormatType.Unreadable )
                {
                    Debug.WriteLine( "SCR unreadable: " + readResult.ErrorMessage );
                    return null;
                }

                // -------------------------------------------------
                // 2. Parse based on classification
                // -------------------------------------------------
                ScrFile scr;

                switch ( readResult.Format )
                {
                    case ScrFileReadResult.ScrFormatType.Strict:
                        scr = ScrFileParser.ParseStrict( readResult.RawFile );
                        break;

                    case ScrFileReadResult.ScrFormatType.Partial:
                        scr = ScrFileParser.ParsePartial( readResult.RawFile );
                        break;

                    default:
                        Debug.WriteLine( "SCR format unknown or unsupported." );
                        return null;
                }

                // -------------------------------------------------
                // 3. Optional: dump summary for debugging
                // -------------------------------------------------
                ScrVerify.DumpSummary( readResult , scr );

                return new LoadedAsset<ScrFile>( path , scr , readResult );
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
