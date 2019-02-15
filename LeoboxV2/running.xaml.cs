using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.IO;
using ShellBoost.Core.Utilities;
using ShellBoost.Core.WindowsPropertySystem;
using ShellBoost.Core;
using ShellBoost.Core.WindowsShell;
using System.Threading;
using System.ComponentModel;
using RestSharp;
using Newtonsoft.Json;
using RestSharp.Extensions;

namespace LeoboxV2
{
    /// <summary>
    /// Logique d'interaction pour running.xaml
    /// </summary>
    public partial class running : Window
    {
        public running()
        {
            InitializeComponent();
        }

        static string tempFolderPath = System.IO.Path.GetTempPath();
        static FileSystemWatcher changementWatcher = new FileSystemWatcher();
        static FileSystemWatcher watcher = new FileSystemWatcher();
        private static FileSystemWatcher _dirWatcher;
        static List<node> ln = new List<node>();
        private static ManualResetEvent mrse = new ManualResetEvent(false);

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var client = new RestClient("http://leobox.org:8080/v1/file/" + globalUser.Name);
            var request = new RestRequest(Method.GET);
            request.AddHeader("cache-control", "no-cache");
            request.AddHeader("ApiKeyUser", globalUser.User_token);
            IRestResponse response = client.Execute(request);

            node nodes = JsonConvert.DeserializeObject<node>(response.Content);
            foreach (node n in nodes.sub_dir)
            {
                ln.Add(n);
            }
            DirectoryInfo di = Directory.CreateDirectory(tempFolderPath + @"Leobox");

            new Thread(() =>
            {

                Console.WriteLine("here");
                var info = new DirectoryInfo(System.IO.Path.GetFullPath(tempFolderPath + @"Leobox"));
                Console.WriteLine(info);
                using (var server = new MyShellFolderServer(info))
                {
                    var config = new ShellFolderConfiguration();   // this class is located in ShellBoost.Core
                    ShellFolderServer.RegisterNativeDll(RegistrationMode.User);
                    server.Start(config); // start the server
                    Console.WriteLine("Started. Press ESC to stop.");
                    while (true)
                    {
                        Thread.Sleep(10000);
                    }

                }


            }).Start();

            iterateNode(ln).ContinueWith(task => startThreads());

            
        }
        

        //Starting THREADS 
        private static void startThreads()
        {
            //Thread.Sleep(1000);
            new Thread(() =>
            {
                mrse.WaitOne();
                watcher.IncludeSubdirectories = true;
                watcher.Path = tempFolderPath + @"Leobox";
                watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.FileName
                | NotifyFilters.DirectoryName | NotifyFilters.CreationTime |
               NotifyFilters.Size;
                watcher.Filter = "*.*";
                watcher.Renamed += new RenamedEventHandler(OnRenamed);
                watcher.Created += new FileSystemEventHandler(OnCreated);
                watcher.Deleted += new FileSystemEventHandler(OnDeleted);

                watcher.EnableRaisingEvents = true;
                while (true)
                {
                    Thread.Sleep(10000);
                }

            }).Start();

            new Thread(() =>
            {
                changementWatcher.IncludeSubdirectories = true;
                changementWatcher.Path = tempFolderPath + @"Leobox";
                changementWatcher.NotifyFilter = NotifyFilters.LastWrite;
                changementWatcher.Filter = "*.*";
                changementWatcher.Changed += new FileSystemEventHandler(OnChanged);

                _dirWatcher = new FileSystemWatcher(tempFolderPath + @"Leobox");
                _dirWatcher.IncludeSubdirectories = true;
                _dirWatcher.NotifyFilter = NotifyFilters.DirectoryName;
                _dirWatcher.EnableRaisingEvents = true;
                _dirWatcher.Deleted += OnChanged;


                changementWatcher.EnableRaisingEvents = true;
                while (true)
                {
                    Thread.Sleep(10000);
                }

            }).Start();
            
            new Thread(() =>
            {
                while (true)
                {
                    Thread.Sleep(60000);
                    Console.WriteLine("REFRESH TIME");
                    MAJ();
                }

            }).Start();


        }
        

        //EVENTS on files
        private static void OnDeleted(object sender, FileSystemEventArgs e)
        {
            string rName = giveFileName(e.FullPath, e.Name);
            if (rName.StartsWith("~"))
            {

            }
            else
            {
                string name = giveFileName(e.FullPath, e.Name);
                string path = giveFilePath(e.FullPath, e.Name);
                path = path.Replace(@"\", "/");
                int idToDelete = findNodeId(ln, path, name);

                Console.WriteLine("change type: " + e.ChangeType + " | fullPath: " + e.FullPath + " | name: " + e.Name);

                var client = new RestClient("http://leobox.org:8080/v1/file/"+globalUser.Name+"/"+ idToDelete.ToString());
                var request = new RestRequest(Method.DELETE);
                request.AddHeader("cache-control", "no-cache");
                request.AddHeader("ApiKeyUser", globalUser.User_token);
                request.AddHeader("Accept", "application/json");
                IRestResponse response = client.Execute(request);

                var status = "";
                var comment = "";
                var res = JsonConvert.DeserializeObject<Dictionary<string, string>>(response.Content);
                int idDeleted;

                foreach (KeyValuePair<string, string> kvp in res)
                {
                    if (kvp.Key == "is_status")
                    {
                        status = kvp.Value;
                    }
                    if (kvp.Key == "id")
                    {
                        idDeleted = Convert.ToInt16(kvp.Value);
                    }
                }


                if (status != "200")
                {
                    MessageBox.Show(comment);
                }
                else
                {
                    removeFromList(ln, idToDelete);
                }

            }
        }
        
        private static void OnCreated(object sender, FileSystemEventArgs e)
        {
            Thread.Sleep(1000);
            string rName = giveFileName(e.FullPath, e.Name);

            if(rName.StartsWith("~"))
            {

            }
            else
            {
                Console.WriteLine("change type: " + e.ChangeType + " | fullPath: " + e.FullPath + " | name: " + e.Name);
                FileAttributes attr = File.GetAttributes(e.FullPath);
                if (attr.HasFlag(FileAttributes.Directory))
                {
                    //created file is directory
                    Console.WriteLine("DIRECTORY");

                    DirectoryInfo di = new DirectoryInfo(e.FullPath);
                    string[] entries = Directory.GetFileSystemEntries(e.FullPath, "*", SearchOption.AllDirectories);
                    FileAttributes fa;

                    if (entries.Length == 0)
                    {
                        string currentFileName = giveFileName(e.FullPath, e.Name);
                        string path2upload = giveFilePath(e.FullPath, e.Name);
                        path2upload = path2upload.Replace(@"\", "/");
                        var client = new RestClient("http://leobox.org:8080/v1/file/" + globalUser.Name + "/createdir?path_dir=" + path2upload + currentFileName);
                        var request = new RestRequest(Method.POST);
                        request.AddHeader("cache-control", "no-cache");
                        request.AddHeader("ApiKeyUser", globalUser.User_token);
                        request.AddHeader("accept", "application/json");
                        request.AddHeader("content-type", "multipart/form-data");
                        IRestResponse response = client.Execute(request);

                        var status = "";
                        var comment = "";
                        var res = JsonConvert.DeserializeObject<Dictionary<string, string>>(response.Content);
                        node newNode = new node();

                        foreach (KeyValuePair<string, string> kvp in res)
                        {
                            if (kvp.Key == "is_status")
                            {
                                status = kvp.Value;
                            }
                            if (kvp.Key == "comment")
                            {
                                comment = kvp.Value;
                            }
                            if (kvp.Key == "id")
                            {
                                newNode.id = Convert.ToInt16(kvp.Value);
                            }
                            if (kvp.Key == "mime_type")
                            {
                                newNode.mime_type = kvp.Value;
                            }
                            if (kvp.Key == "name")
                            {
                                newNode.name = kvp.Value;
                            }
                            if (kvp.Key == "path_file")
                            {
                                newNode.path_file = kvp.Value;
                            }
                            if (kvp.Key == "size")
                            {
                                newNode.size = Convert.ToInt64(kvp.Value);
                            }
                            if (kvp.Key == "storage_mtime")
                            {
                                newNode.storage_mtime = Convert.ToInt64(kvp.Value);
                            }
                            if (kvp.Key == "type")
                            {
                                newNode.type = kvp.Value;
                            }
                        }


                        if (status != "200")
                        {
                            MessageBox.Show(comment);
                        }
                        else
                        {
                            addToList(ln, newNode);
                        }
                    }
                    else
                    {
                        string currentFileName = giveFileName(e.FullPath, e.Name);
                        string path2upload = giveFilePath(e.FullPath, e.Name);
                        path2upload = path2upload.Replace(@"\", "/");
                        var client = new RestClient("http://leobox.org:8080/v1/file/" + globalUser.Name + "/createdir?path_dir=" + path2upload + currentFileName);
                        var request = new RestRequest(Method.POST);
                        request.AddHeader("cache-control", "no-cache");
                        request.AddHeader("ApiKeyUser", globalUser.User_token);
                        request.AddHeader("accept", "application/json");
                        request.AddHeader("content-type", "multipart/form-data");
                        IRestResponse response = client.Execute(request);

                        var status = "";
                        var comment = "";
                        var res = JsonConvert.DeserializeObject<Dictionary<string, string>>(response.Content);
                        node newNode = new node();

                        foreach (KeyValuePair<string, string> kvp in res)
                        {
                            if (kvp.Key == "is_status")
                            {
                                status = kvp.Value;
                            }
                            if (kvp.Key == "comment")
                            {
                                comment = kvp.Value;
                            }
                            if (kvp.Key == "id")
                            {
                                newNode.id = Convert.ToInt16(kvp.Value);
                            }
                            if (kvp.Key == "mime_type")
                            {
                                newNode.mime_type = kvp.Value;
                            }
                            if (kvp.Key == "name")
                            {
                                newNode.name = kvp.Value;
                            }
                            if (kvp.Key == "path_file")
                            {
                                newNode.path_file = kvp.Value;
                            }
                            if (kvp.Key == "size")
                            {
                                newNode.size = Convert.ToInt64(kvp.Value);
                            }
                            if (kvp.Key == "storage_mtime")
                            {
                                newNode.storage_mtime = Convert.ToInt64(kvp.Value);
                            }
                            if (kvp.Key == "type")
                            {
                                newNode.type = kvp.Value;
                            }
                        }


                        if (status != "200")
                        {
                            MessageBox.Show(comment);
                        }
                        else
                        {
                            addToList(ln, newNode);
                        }



                        foreach (string en in entries)
                        {
                            string en2 = en.Replace("\\", "/");
                            fa = File.GetAttributes(en2);
                            if (fa.HasFlag(FileAttributes.Directory))
                            {
                                int index = en2.LastIndexOf("/");
                                string nom = (en2.Substring(en2.LastIndexOf("/"))).Replace("/", "");
                                currentFileName = giveFileName(en2, nom);
                                Console.WriteLine("current file name : " + currentFileName);
                                path2upload = giveFilePath(en2, nom);
                                path2upload = path2upload.Replace(@"\", "/");
                                Console.WriteLine("folder path to add to request : " + path2upload);
                                Console.WriteLine("e.name : " + nom);
                                Console.WriteLine("e.fullpath : " + en2);

                                client = new RestClient("http://leobox.org:8080/v1/file/" + globalUser.Name + "/createdir?path_dir=" + path2upload + currentFileName);
                                request = new RestRequest(Method.POST);
                                request.AddHeader("cache-control", "no-cache");
                                request.AddHeader("ApiKeyUser", globalUser.User_token);
                                request.AddHeader("accept", "application/json");
                                request.AddHeader("content-type", "multipart/form-data");
                                response = client.Execute(request);

                                status = "";
                                comment = "";
                                res = JsonConvert.DeserializeObject<Dictionary<string, string>>(response.Content);
                                newNode = new node();

                                foreach (KeyValuePair<string, string> kvp in res)
                                {
                                    if (kvp.Key == "is_status")
                                    {
                                        status = kvp.Value;
                                    }
                                    if (kvp.Key == "comment")
                                    {
                                        comment = kvp.Value;
                                    }
                                    if (kvp.Key == "id")
                                    {
                                        newNode.id = Convert.ToInt16(kvp.Value);
                                    }
                                    if (kvp.Key == "mime_type")
                                    {
                                        newNode.mime_type = kvp.Value;
                                    }
                                    if (kvp.Key == "name")
                                    {
                                        newNode.name = kvp.Value;
                                    }
                                    if (kvp.Key == "path_file")
                                    {
                                        newNode.path_file = kvp.Value;
                                    }
                                    if (kvp.Key == "size")
                                    {
                                        newNode.size = Convert.ToInt64(kvp.Value);
                                    }
                                    if (kvp.Key == "storage_mtime")
                                    {
                                        newNode.storage_mtime = Convert.ToInt64(kvp.Value);
                                    }
                                    if (kvp.Key == "type")
                                    {
                                        newNode.type = kvp.Value;
                                    }
                                }


                                if (status != "200")
                                {
                                    MessageBox.Show(comment);
                                }
                                else
                                {
                                    addToList(ln, newNode);
                                }
                            }
                            else
                            {
                                en2 = en.Replace("\\", "/");
                                string nom = (en2.Substring(en2.LastIndexOf("/"))).Replace("/", "");
                                currentFileName = giveFileName(en2, nom);
                                Console.WriteLine("current file name : " + currentFileName);
                                path2upload = giveFilePath(en2, nom);
                                path2upload = path2upload.Replace(@"\", "/");
                                Console.WriteLine("folder path to add to request : " + path2upload);
                                Console.WriteLine("e.name : " + nom);
                                Console.WriteLine("e.fullpath : " + en2);


                                client = new RestClient("http://leobox.org:8080/v1/file/" + globalUser.Name + "/upload?path_file=" + path2upload);
                                request = new RestRequest(Method.POST);
                                request.AddHeader("Content-Type", "multipart/form-data");
                                request.AddHeader("ApiKeyUser", globalUser.User_token);
                                request.AddHeader("accept", "application/json");
                                request.AddParameter("Content-Disposition: form-data", "name=\"file\"", ParameterType.RequestBody);
                                request.AddFile("file", en2);
                                response = client.Execute(request);

                                status = "";
                                comment = "";
                                res = JsonConvert.DeserializeObject<Dictionary<string, string>>(response.Content);
                                newNode = new node();

                                foreach (KeyValuePair<string, string> kvp in res)
                                {
                                    if (kvp.Key == "is_status")
                                    {
                                        status = kvp.Value;
                                    }
                                    if (kvp.Key == "comment")
                                    {
                                        comment = kvp.Value;
                                    }
                                    if (kvp.Key == "id")
                                    {
                                        newNode.id = Convert.ToInt16(kvp.Value);
                                    }
                                    if (kvp.Key == "mime_type")
                                    {
                                        newNode.mime_type = kvp.Value;
                                    }
                                    if (kvp.Key == "name")
                                    {
                                        newNode.name = kvp.Value;
                                    }
                                    if (kvp.Key == "path_file")
                                    {
                                        newNode.path_file = kvp.Value;
                                    }
                                    if (kvp.Key == "size")
                                    {
                                        newNode.size = Convert.ToInt64(kvp.Value);
                                    }
                                    if (kvp.Key == "storage_mtime")
                                    {
                                        newNode.storage_mtime = Convert.ToInt64(kvp.Value);
                                    }
                                    if (kvp.Key == "type")
                                    {
                                        newNode.type = kvp.Value;
                                    }
                                }


                                if (status != "200")
                                {
                                    MessageBox.Show(comment);
                                }
                                else
                                {
                                    addToList(ln, newNode);
                                }
                            }

                        }
                    }

                }
                else
                {
                    Console.WriteLine("FILE !!!");
                    string currentFileName = giveFileName(e.FullPath, e.Name);
                    Console.WriteLine("current file name : " + currentFileName);
                    string path2upload = giveFilePath(e.FullPath, e.Name);
                    path2upload = path2upload.Replace(@"\", "/");
                    Console.WriteLine("folder path to add to request : " + path2upload);
                    Console.WriteLine("e.name : " + e.Name);
                    Console.WriteLine("e.fullpath : " + e.FullPath);


                    var client = new RestClient("http://leobox.org:8080/v1/file/" + globalUser.Name + "/upload?path_file=" + path2upload);
                    var request = new RestRequest(Method.POST);
                    request.AddHeader("Content-Type", "multipart/form-data");
                    request.AddHeader("ApiKeyUser", globalUser.User_token);
                    request.AddHeader("accept", "application/json");
                    request.AddHeader("content-type", "multipart/form-data");
                    request.AddParameter("Content-Disposition: form-data", "name=\"file\"", ParameterType.RequestBody);
                    request.AddFile("file", e.FullPath);
                    IRestResponse response = client.Execute(request);

                    var status = "";
                    var comment = "";
                    var res = JsonConvert.DeserializeObject<Dictionary<string, string>>(response.Content);
                    node newNode = new node();

                    foreach (KeyValuePair<string, string> kvp in res)
                    {
                        if (kvp.Key == "is_status")
                        {
                            status = kvp.Value;
                        }
                        if (kvp.Key == "comment")
                        {
                            comment = kvp.Value;
                        }
                        if (kvp.Key == "id")
                        {
                            newNode.id = Convert.ToInt16(kvp.Value);
                        }
                        if (kvp.Key == "mime_type")
                        {
                            newNode.mime_type = kvp.Value;
                        }
                        if (kvp.Key == "name")
                        {
                            newNode.name = kvp.Value;
                        }
                        if (kvp.Key == "path_file")
                        {
                            newNode.path_file = kvp.Value;
                        }
                        if (kvp.Key == "size")
                        {
                            newNode.size = Convert.ToInt64(kvp.Value);
                        }
                        if (kvp.Key == "storage_mtime")
                        {
                            newNode.storage_mtime = Convert.ToInt64(kvp.Value);
                        }
                        if (kvp.Key == "type")
                        {
                            newNode.type = kvp.Value;
                        }
                    }


                    if (status != "200")
                    {
                        MessageBox.Show(comment);
                    }
                    else
                    {
                        addToList(ln, newNode);
                    }

                }
            }

            
        }
        
        private static void OnRenamed(object sender, RenamedEventArgs e)
        {
            string rName = giveFileName(e.FullPath, e.Name);
            if (rName.StartsWith("~"))
            {

            }
            else
            {
                string newName = giveFileName(e.FullPath, e.Name);
                string name = giveFileName(e.FullPath, e.OldName);
                string path = giveFilePath(e.FullPath, e.Name);
                path = path.Replace(@"\", "/");
                int idNodeRenamed = findNodeId(ln, path, name);


                Console.WriteLine("change type: " + e.ChangeType + " | fullPath: " + e.FullPath + " | name: " + e.Name + " | oldName: " + e.OldName + " | oldPath: " + e.OldFullPath);
                var client = new RestClient("http://leobox.org:8080/v1/file/"+globalUser.Name+"/"+idNodeRenamed+ "?action=1&path_file="+newName+"");
                var request = new RestRequest(Method.PUT);
                request.AddHeader("cache-control", "no-cache");
                request.AddHeader("ApiKeyUser", globalUser.User_token);
                request.AddHeader("Accept", "application/json");
                request.AddHeader("Content-Type", "multipart/form-data");
                IRestResponse response = client.Execute(request);

                var status = "";
                var comment = "";
                var res = JsonConvert.DeserializeObject<Dictionary<string, string>>(response.Content);

                foreach (KeyValuePair<string, string> kvp in res)
                {
                    if (kvp.Key == "is_status")
                    {
                        status = kvp.Value;
                    }
                    if (kvp.Key == "comment")
                    {
                        comment = kvp.Value;
                    }
                }


                if (status != "200")
                {
                    MessageBox.Show(comment);
                }
                else
                {
                    renameNode(ln, idNodeRenamed, newName);
                }


            }
        }
        
        static DateTime _lastTimeFileWatcherEventRaised;
        private static void OnChanged(object sender, FileSystemEventArgs e)
        {
            string rName = giveFileName(e.FullPath, e.Name);
            if (rName.StartsWith("~"))
            {

            }
            else
            {
                if(File.Exists(e.FullPath))
                {
                    FileAttributes attr = File.GetAttributes(e.FullPath);
                    if (attr.HasFlag(FileAttributes.Directory))
                    {

                    }
                    else
                    {
                        if (sender != _dirWatcher)
                        {
                            if (e.ChangeType == WatcherChangeTypes.Changed)
                            {
                                if (DateTime.Now.Subtract(_lastTimeFileWatcherEventRaised).TotalMilliseconds < 500)
                                {
                                    return;
                                }

                                _lastTimeFileWatcherEventRaised = DateTime.Now;
                                Console.WriteLine("change type: " + e.ChangeType + " | fullPath: " + e.FullPath + " | name: " + e.Name);
                                string currentFileName = giveFileName(e.FullPath, e.Name);
                                Console.WriteLine("current file name : " + currentFileName);
                                string path2upload = giveFilePath(e.FullPath, e.Name);
                                Console.WriteLine("path to upload : " + path2upload);

                                //////////
                                ///
                                string name = giveFileName(e.FullPath, e.Name);
                                string path = giveFilePath(e.FullPath, e.Name);
                                path = path.Replace(@"\", "/");
                                int idToUpdate = findNodeId(ln, path, name);
                                var client = new RestClient("http://leobox.org:8080/v1/file/"+globalUser.Name+"/"+ idToUpdate + "?action=3");
                                var request = new RestRequest(Method.PUT);
                                request.AddHeader("Content-Type", "multipart/form-data");
                                request.AddHeader("ApiKeyUser", globalUser.User_token);
                                request.AddHeader("accept", "application/json");
                                request.AddHeader("content-type", "multipart/form-data");
                                request.AddParameter("Content-Disposition: form-data", "name=\"file\"", ParameterType.RequestBody);
                                request.AddFile("file", e.FullPath);
                                IRestResponse response = client.Execute(request);

                                var status = "";
                                var comment = "";
                                var res = JsonConvert.DeserializeObject<Dictionary<string, string>>(response.Content);
                                node newNode = new node();

                                foreach (KeyValuePair<string, string> kvp in res)
                                {
                                    if (kvp.Key == "is_status")
                                    {
                                        status = kvp.Value;
                                    }
                                    if (kvp.Key == "comment")
                                    {
                                        comment = kvp.Value;
                                    }
                                    if (kvp.Key == "id")
                                    {
                                        newNode.id = Convert.ToInt16(kvp.Value);
                                    }
                                    if (kvp.Key == "mime_type")
                                    {
                                        newNode.mime_type = kvp.Value;
                                    }
                                    if (kvp.Key == "name")
                                    {
                                        newNode.name = kvp.Value;
                                    }
                                    if (kvp.Key == "path_file")
                                    {
                                        newNode.path_file = kvp.Value;
                                    }
                                    if (kvp.Key == "size")
                                    {
                                        newNode.size = Convert.ToInt64(kvp.Value);
                                    }
                                    if (kvp.Key == "storage_mtime")
                                    {
                                        newNode.storage_mtime = Convert.ToInt64(kvp.Value);
                                    }
                                    if (kvp.Key == "type")
                                    {
                                        newNode.type = kvp.Value;
                                    }
                                }


                                if (status != "200")
                                {
                                    MessageBox.Show(comment);
                                }
                                else
                                {
                                   
                                }


                            }
                        }
                    }
                }

                
            }


            
            
        }








        //VERIFICATION EVERY MINUTES

        private static async Task MAJ()
        {
            //getting last updates
            List<node> nAJ = new List<node>();
            var client = new RestClient("http://leobox.org:8080/v1/file/" + globalUser.Name);
            var request = new RestRequest(Method.GET);
            request.AddHeader("cache-control", "no-cache");
            request.AddHeader("ApiKeyUser", globalUser.User_token);
            IRestResponse response = client.Execute(request);

            node nodes = JsonConvert.DeserializeObject<node>(response.Content);
            foreach (node n in nodes.sub_dir)
            {
                nAJ.Add(n);
            }

            //COMPARAISON
            mrse.Reset();
            await recursiveComparaison(nAJ).ContinueWith(task => recursiveDelete(nAJ));
            ln = nAJ;
            mrse.Set();
        }


        private static async Task recursiveComparaison(List<node> nAJ)
        {
            foreach (node naj in nAJ)
            {
                if(findNode(ln,naj.id, naj.storage_mtime) == 0)
                {
                    //alors on DL
                    List<node> nouvelList = new List<node>();
                    nouvelList.Add(naj);
                    await iterateNode(nouvelList);
                }
            }
            
        }

        private static void recursiveDelete(List<node> nAJ)
        {
            foreach (node n in ln)
            {
                if(findNode(nAJ, n.id, n.storage_mtime) == 0)
                {
                    //alors on supprime
                    string realPath = tempFolderPath + @"Leobox/" + n.path_file;
                    realPath = realPath.Replace(@"\\", @"/");
                    File.Delete(realPath);
                }
            }
        }

        //FUNCTIONS 

        private static int findNode(List<node> no, int id, Int64 storageTime)
        {
            int idReturned = 0;
            
                foreach (node n in no)
                {
                    if (n.id == id && n.storage_mtime == storageTime)
                    {
                        return n.id;
                    }
                    else
                    {
                        int lol = findNode(n.sub_dir, id, storageTime);
                        if (lol != 0)
                        {
                            return lol;
                        }
                    }
                }

            return idReturned;
        }

        private static int findNodeId(List<node> no, string path, string name)
        {
            int idReturned = 0;
            if(path == "/")
            {
                foreach(node nn in no)
                {
                    if(nn.name == name)
                    {
                        return nn.id;
                    }
                }
            }
            else
            {
                foreach (node n in no)
                {
                    string realPath = path.Substring(1);
                    if (n.name == name && n.path_file == realPath+name)
                    {
                        return n.id;
                    }
                    else
                    {
                       int lol = findNodeId(n.sub_dir, path, name);
                       if( lol != 0)
                        {
                            return lol;
                        }
                    }
                }
            }
            
            return idReturned;
        }

        private static void renameNode(List<node> no, int idNode, string newName)
        {
            foreach (node n in no)
            {
                if (n.id == idNode)
                {
                    n.name = newName;
                    break;
                }
                else
                {
                    renameNode(n.sub_dir, idNode, newName);
                }
            }
        }
       
        private static void removeFromList(List<node> no, int idFile)
        {
            foreach (node n in no)
            {
                if (n.id == idFile)
                {
                    no.Remove(n);
                    break;
                }
                else
                {
                    removeFromList(n.sub_dir, idFile);
                }
            }
        }


        private static void addToList(List<node> no, node noAdded)
        {
            if (noAdded.name == noAdded.path_file)
            {
                no.Add(noAdded);
                return;
            }

            foreach (node n in no)
            {
                if((n.type == "Folder") && (n.path_file == (noAdded.path_file).Replace("/"+noAdded.name, "")) )
                {
                    n.sub_dir.Add(noAdded);
                    return;
                }
                else
                {
                    addToList(n.sub_dir, noAdded);
                }
            }
        }
        
        private static async Task iterateNode(List<node> no)
        {
            foreach (node n in no)
            {
                if (n.type == "Folder")
                {
                    if (n.name == n.path_file)
                    {
                        DirectoryInfo di = Directory.CreateDirectory(tempFolderPath + @"Leobox\" + n.name);
                    }
                    else
                    {
                        DirectoryInfo di = Directory.CreateDirectory(tempFolderPath + @"Leobox\" + n.path_file);
                    }
                    await iterateNode(n.sub_dir);
                }
                else
                {
                    if (n.name == n.path_file)
                    {
                        //dl to root
                        var client = new RestClient("http://leobox.org:8080/v1/file/"+globalUser.Name+"/" + n.id);
                        var request = new RestRequest(Method.GET);
                        request.AddHeader("cache-control", "no-cache");
                        request.AddHeader("accept", "multipart/form-data");
                        request.AddHeader("ApiKeyUser", globalUser.User_token);
                        await Task.Factory.StartNew(() => client.DownloadData(request).SaveAs(tempFolderPath + @"Leobox\" + n.name));

                    }
                    else
                    {
                        //dl to path
                        var client = new RestClient("http://leobox.org:8080/v1/file/"+ globalUser.Name + "/" + n.id);
                        var request = new RestRequest(Method.GET);
                        request.AddHeader("cache-control", "no-cache");
                        request.AddHeader("accept", "multipart/form-data");
                        request.AddHeader("ApiKeyUser", globalUser.User_token);
                        await Task.Factory.StartNew(() => client.DownloadData(request).SaveAs(tempFolderPath + @"Leobox\" + n.path_file));
                        
                    }
                }
            }
        }

        private static string giveFilePath(string filePath, string fileName)
        {
            int lengthTmpPath = tempFolderPath.Length + 6;
            string fp = (filePath).Substring(lengthTmpPath, (filePath).Length - lengthTmpPath);
            int pos = (fileName).LastIndexOf(@"\") + 1;
            string currentFileName = (fileName).Substring(pos, (fileName).Length - pos);

            int lengthCurrentFile = (currentFileName).Length;
            fp = fp.Remove(fp.Length - lengthCurrentFile);
            return fp;
        }


        private static string giveFileName(string filePath, string fileName)
        {
            int lengthTmpPath = tempFolderPath.Length + 6;
            string fp = (filePath).Substring(lengthTmpPath, (filePath).Length - lengthTmpPath);
            int pos = (fileName).LastIndexOf(@"\") + 1;
            string currentFileName = (fileName).Substring(pos, (fileName).Length - pos);
            return currentFileName;
        }


        //EVENTS on app closing
        private void Window_Closed(object sender, EventArgs e)
        {
            ShellFolderServer.UnregisterNativeDll(RegistrationMode.User);
            DirectoryInfo di = Directory.CreateDirectory(tempFolderPath + @"Leobox");
            di.Delete(true);
            Console.WriteLine("Stopped"); // end of program
        }

        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            ShellFolderServer.UnregisterNativeDll(RegistrationMode.User);
            Console.WriteLine("Stopped"); // end of program
        }

        //SHELL FOLDER

        public class MyShellFolderServer : ShellFolderServer // this base class is located in ShellBoost.Core
        {
            private MyRootFolder _root;
            public DirectoryInfo Info { get; }


            public MyShellFolderServer(DirectoryInfo info)
            {
                if (info == null)
                    throw new ArgumentNullException(nameof(info));

                if (!info.Exists)
                    throw new ArgumentException(null, nameof(info));

                Info = info;
            }

            // only the Shell knows our root folder PIDL
            protected override RootShellFolder GetRootFolder(ShellItemIdList idl)
            {
                if (_root == null)
                {
                    _root = new MyRootFolder(this, idl);
                }
                return _root;
            }
        }

        public class MyRootFolder : RootShellFolder  // this base class is located in ShellBoost.Core
        {
            public MyRootFolder(MyShellFolderServer server, ShellItemIdList idList)
                : base(idList)
            {
                if (server == null)
                    throw new ArgumentNullException(nameof(server));

                Server = server;
            }

            public MyShellFolderServer Server { get; }

            public override IEnumerable<ShellItem> EnumItems(SHCONTF options)
            {
                //yield return new ShellFolder(this, new StringKeyShellItemId("My First Folder"));
                //yield return new ShellItem(this, new StringKeyShellItemId("My First Item"));
                foreach (var fi in LocalShellFolder.EnumerateFileSystemItems(Server.Info, "*"))
                {
                    if (fi is DirectoryInfo di)
                    {
                        yield return new LocalShellFolder(this, di);
                    }
                    else
                    {
                        yield return new LocalShellItem(this, (FileInfo)fi);
                    }
                }
            }

        }

        public class LocalShellItem : ShellItem
        {
            public LocalShellItem(ShellFolder parent, FileInfo info)
                : base(parent, info) // there is a specific overload for FileInfo
            {
                CanCopy = true;
                CanDelete = true;
                CanLink = true;
                CanMove = true;
                CanPaste = true;
                CanRename = true;
                Info = info;
            }

            public FileInfo Info { get; }
        }

        public class LocalShellFolder : ShellFolder
        {
            public LocalShellFolder(ShellFolder parent, DirectoryInfo info)
                : base(parent, info) // there is a specific overload for DirectoryInfo
            {
                CanCopy = true;
                CanDelete = true;
                CanLink = true;
                CanMove = true;
                CanPaste = true;
                CanRename = true;
                Info = info;
            }

            public DirectoryInfo Info { get; }

            // we export this as internal so the root folder shares this behavior
            internal static IEnumerable<FileSystemInfo> EnumerateFileSystemItems(DirectoryInfo info, string searchPattern)
            {
                // for demonstration purpose, we hide any file or directory that has "hidden" in its name
                foreach (var child in info.EnumerateFileSystemInfos(searchPattern))
                {
                    if (child.Name.IndexOf("hidden", StringComparison.OrdinalIgnoreCase) >= 0)
                        continue;

                    yield return child;
                }
            }

            protected override IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(DirectoryInfo info, SHCONTF options, string searchPattern) => EnumerateFileSystemItems(info, searchPattern);

            protected override ShellItem CreateFileSystemFolder(DirectoryInfo info) => new LocalShellFolder(this, info);

            private List<string> GetPaths(DragDropTargetEventArgs e)
            {
                var list = new List<string>();
                if (e.DataObject[ShellDataObjectFormat.CFSTR_SHELLIDLIST]?.ConvertedData is IEnumerable<ShellItemIdList> idls)
                {
                    foreach (var idl in idls)
                    {
                        string path;
                        var item = Root.GetItem(idl);
                        if (item != null)
                        {
                            // this comes from ourselves
                            path = item.FileSystemPath;
                        }
                        else
                        {
                            // check it's a file system pidl
                            path = idl.GetFileSystemPath();
                        }

                        if (path != null)
                        {
                            list.Add(path);
                        }
                    }
                }
                return list;
            }

            protected override void OnDragDropTarget(DragDropTargetEventArgs e)
            {
                e.HResult = ShellUtilities.S_OK;
                var paths = GetPaths(e);
                if (paths.Count > 0)
                {
                    e.Effect = System.Windows.Forms.DragDropEffects.All;
                }

                if (e.Type == DragDropTargetEventType.DragDrop)
                {
                    // file operation events need an STA thread
                    WindowsUtilities.DoModelessAsync(() =>
                    {
                        using (var fo = new FileOperation(true))
                        {
                            fo.PostCopyItem += (sender, e2) =>
                            {
                                // we could add some logic here
                            };

                            if (paths.Count == 1)
                            {
                                fo.CopyItem(paths[0], FileSystemPath, null);
                            }
                            else
                            {
                                fo.CopyItems(paths, FileSystemPath);
                            }
                            fo.SetOperationFlags(FOF.FOF_ALLOWUNDO | FOF.FOF_NOCONFIRMMKDIR | FOF.FOF_RENAMEONCOLLISION);
                            fo.PerformOperations();
                        }
                    });
                }
            }

        }

        
    }
}
