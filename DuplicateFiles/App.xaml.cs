using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace DuplicateFiles
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {


        static App()
        {
            AppDomain.CurrentDomain.AssemblyResolve += (x, y) =>
            {
                var asms = new List<Assembly>() { Assembly.GetExecutingAssembly() }; //,Assembly.GetEntryAssembly(),                    Assembly.GetCallingAssembly() };

                var Name = y.Name.Split(',')[0] + ".dll";
                    

                foreach (var a in asms)
                {
                    var stream = a.GetManifestResourceStream(string.Format("DuplicateFiles.Resources.{0}",Name));

                    if (stream == null)
                        continue;

                    byte[] raw = new byte[stream.Length];
                    stream.Read(raw, 0, (int)stream.Length);

                    return System.Reflection.Assembly.Load(raw);
                }

                return null;
            };

            AppDomain.CurrentDomain.UnhandledException += (x, y) =>
            {
                var exp = (Exception)y.ExceptionObject;
                MessageBox.Show(string.Format("Unhandeled Exception:{0}{1}{0}{2} ", Environment.NewLine, exp.Message, exp.StackTrace));
            };
        }
    }
}
