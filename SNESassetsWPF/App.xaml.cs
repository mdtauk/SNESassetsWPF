using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Reflection;
using System.Windows;




namespace SNESassetsWPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup( e );

            // TEMP: Dump embedded resource names
            foreach ( var name in Assembly.GetExecutingAssembly().GetManifestResourceNames() )
            {
                Debug.WriteLine( "Embedded Resource: " + name );
            }

            // Your normal startup logic continues here...
        }
    }

}
