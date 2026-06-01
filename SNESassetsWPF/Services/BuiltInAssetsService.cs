using SNESassetsWPF.Enums;
using SNESassetsWPF.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;




namespace SNESassetsWPF.Services
{
    public class BuiltInAssetService
    {
        /// <summary>
        /// Gets Built In
        /// </summary>
        /// <returns></returns>
        public FileNode GetBuiltInColFile()
        {
            const string resourceName = "SNESassetsWPF.Assets.test.col";

            using var stream = Assembly.GetExecutingAssembly()
        .GetManifestResourceStream(resourceName);

            if ( stream == null )
                throw new FileNotFoundException( $"Embedded resource not found: {resourceName}" );

            string tempPath = Path.Combine(
                                            Path.GetTempPath(),
                                            "SNESassets_test.col"
                                        );

            using ( var fs = new FileStream( tempPath , FileMode.Create , FileAccess.Write ) )
                stream.CopyTo( fs );

            return new FileNode( "Test COL" , tempPath , FileType.Col );
        }




        public FileNode GetBuiltInCgxFile()
        {
            const string resourceName = "SNESassetsWPF.Assets.test.cgx";

            using var stream = Assembly.GetExecutingAssembly()
                                        .GetManifestResourceStream(resourceName);

            if ( stream == null )
                throw new FileNotFoundException( $"Embedded resource not found: {resourceName}" );

            string tempPath = Path.Combine(
                        Path.GetTempPath(),
                        "SNESassets_test.cgx"
    );

            using ( var fs = new FileStream( tempPath , FileMode.Create , FileAccess.Write ) )
                stream.CopyTo( fs );

            return new FileNode( "Test CGX" , tempPath , FileType.Cgx )
            {
                IsBuiltIn = true
            };
        }

    }

}
