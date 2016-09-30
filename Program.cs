using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml;
using System.Diagnostics;

namespace iarbatchbuild
{
    class Program
    {
        static readonly string usage = "iarbatchbuild <EWW file> <batch build name> <-build|-clean|-make>";
        static void Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.Write(usage);
                return;
            }

            string ewwfile = args[0];
            string batch = args[1];
            string build = args[2];

            if (!File.Exists(ewwfile))
            {
                Console.Write("Cannot find the workspace file\r\n");
            }

            string ws_dir = Path.GetDirectoryName(Path.GetFullPath(ewwfile));

            XmlReader reader = XmlReader.Create(ewwfile);
            XmlDocument eww = new XmlDocument();
            eww.Load(reader);
            reader.Close();

            XmlNode workspace = eww.DocumentElement;

            // Find the configuration in args[1] in the eww file
            XmlNodeList projects = workspace.SelectNodes("project");
            Dictionary<string, string> ewplist = new Dictionary<string, string>();

            foreach (XmlNode node in projects)
            {
                XmlNode path = node.SelectSingleNode("path");
                string name = Path.GetFileNameWithoutExtension(path.InnerText);
                string ewp = path.InnerText.Replace("$WS_DIR$", ws_dir);

                if (!ewplist.ContainsKey(name))
                {
                    ewplist.Add(name, ewp);
                }
            }

            XmlNode batchbuild = workspace.SelectSingleNode("descendant::batchBuild/batchDefinition[name='" + batch + "']");
            if (batchbuild == null)
            {
                Console.Write("Cannot find the batch build definition\r\n");
                return;
            }

            XmlNodeList members = batchbuild.SelectNodes("member");
            foreach (XmlNode node in members)
            {
                try
                {
                    string projname = node.SelectSingleNode("project").InnerText;
                    string ewp = ewplist[projname];
                    string config = node.SelectSingleNode("configuration").InnerText;

                    ProcessStartInfo psi = new ProcessStartInfo();
                    psi.FileName = "iarbuild.exe";
                    psi.Arguments = ewp + " " + build + " " + config;
                    psi.UseShellExecute = false;
                    

                    Process iarbuild = Process.Start(psi);
                    iarbuild.WaitForExit();
                }
                catch (Exception ex)
                {
                    Console.Write(ex.Message);
                }
            }
        }
    }
}
