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
using DSOFile;
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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var client = new RestClient("http://leobox.org:8080/v1/file/test?username=" + globalUser.Name);
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

            iterateNode(ln);


            Thread.Sleep(10000);
            new Thread(() =>
            {
                watcher.IncludeSubdirectories = true;
                watcher.Path = tempFolderPath + @"Leobox";
                watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.FileName 
                | NotifyFilters.DirectoryName | NotifyFilters.CreationTime | 
               NotifyFilters.Size ;
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


        }






        private void iterateNode(List<node> no)
        {
            foreach (node n in no)
            {
                if(n.type == "Folder")
                {
                    if(n.name == n.path_file)
                    {
                        DirectoryInfo di = Directory.CreateDirectory(tempFolderPath + @"Leobox\" + n.name);
                    }
                    else
                    {
                        DirectoryInfo di = Directory.CreateDirectory(tempFolderPath + @"Leobox\" + n.path_file);
                    }
                    iterateNode(n.sub_dir);
                }
                else
                {
                    if(n.name == n.path_file)
                    {
                        //dl to root
                        var client = new RestClient("http://leobox.org:8080/v1/file/test/"+n.id);
                        var request = new RestRequest(Method.GET);
                        request.AddHeader("cache-control", "no-cache");
                        request.AddHeader("accept", "multipart/form-data");
                        request.AddHeader("ApiKeyUser", globalUser.User_token);
                        client.DownloadData(request).SaveAs(tempFolderPath + @"Leobox\" + n.name);

                    }
                    else
                    {
                        //dl to path
                        var client = new RestClient("http://leobox.org:8080/v1/file/test/" + n.id);
                        var request = new RestRequest(Method.GET);
                        request.AddHeader("cache-control", "no-cache");
                        request.AddHeader("accept", "multipart/form-data");
                        request.AddHeader("ApiKeyUser", globalUser.User_token);
                        client.DownloadData(request).SaveAs(tempFolderPath + @"Leobox\" + n.path_file);
                    }
                }
            }
        }


        //EVENTS on files
        private static void OnDeleted(object sender, FileSystemEventArgs e)
        {
           Console.WriteLine("change type: " + e.ChangeType + " | fullPath: " + e.FullPath + " | name: " + e.Name);
        }
        
        private static void OnCreated(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine("change type: "+e.ChangeType + " | fullPath: " + e.FullPath + " | name: " + e.Name);

            var client = new RestClient("http://leobox.org:8080/v1/file/test/upload?path_file=/");
            var request = new RestRequest(Method.POST);
            request.AddHeader("Content-Type", "multipart/form-data");
            request.AddHeader("ApiKeyUser", globalUser.User_token);
            request.AddHeader("accept", "application/json");
            request.AddHeader("content-type", "multipart/form-data");
            request.AddParameter("Content-Disposition: form-data", "name=\"file\"", ParameterType.RequestBody);
            request.AddFile("file", @"C:\Users\dilan\OneDrive\Images\Pellicule\ten.jpg");
            IRestResponse response = client.Execute(request);

            //request.AddFile("te.jpg", @"C:\Users\dilan\OneDrive\Images\Pellicule\te.jpg");
            //IRestResponse response = client.Execute(request);

            Console.WriteLine(response.Content);
        }

        private static void OnRenamed(object sender, RenamedEventArgs e)
        {
            Console.WriteLine("change type: "+e.ChangeType + " | fullPath: " + e.FullPath + " | name: " + e.Name + " | oldName: " + e.OldName + " | oldPath: " + e.OldFullPath);
        }
        
        static DateTime _lastTimeFileWatcherEventRaised;
        private static void OnChanged(object sender, FileSystemEventArgs e)
        {
            if (sender == _dirWatcher)
            {
                if (e.ChangeType == WatcherChangeTypes.Changed)
                {
                    if (DateTime.Now.Subtract(_lastTimeFileWatcherEventRaised).TotalMilliseconds < 500)
                    {
                        return;
                    }

                    _lastTimeFileWatcherEventRaised = DateTime.Now;
                    Console.WriteLine("change type: " + e.ChangeType + " | fullPath: " + e.FullPath + " | name: " + e.Name);
                    int lengthTmpPath = tempFolderPath.Length + 6;
                    string fp = (e.FullPath).Substring(lengthTmpPath, (e.FullPath).Length - lengthTmpPath);
                    int pos = (e.Name).LastIndexOf(@"\") + 1;
                    string currentFileName = (e.Name).Substring(pos, (e.Name).Length - pos);

                    int lengthCurrentFile = (currentFileName).Length;
                    fp = fp.Remove(fp.Length - lengthCurrentFile);
                    Console.WriteLine("path to upload : " + fp);



                }
            }


           

        }


        //FUNCTIONS 

        private string giveFileName(List<node> no)
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
                    iterateNode(n.sub_dir);
                }
                else
                {
                    if (n.name == n.path_file)
                    {
                        //dl to root
                        var client = new RestClient("http://leobox.org:8080/v1/file/test/" + n.id);
                        var request = new RestRequest(Method.GET);
                        request.AddHeader("cache-control", "no-cache");
                        request.AddHeader("accept", "multipart/form-data");
                        request.AddHeader("ApiKeyUser", globalUser.User_token);
                        client.DownloadData(request).SaveAs(tempFolderPath + @"Leobox\" + n.name);

                    }
                    else
                    {
                        //dl to path
                        var client = new RestClient("http://leobox.org:8080/v1/file/test/" + n.id);
                        var request = new RestRequest(Method.GET);
                        request.AddHeader("cache-control", "no-cache");
                        request.AddHeader("accept", "multipart/form-data");
                        request.AddHeader("ApiKeyUser", globalUser.User_token);
                        client.DownloadData(request).SaveAs(tempFolderPath + @"Leobox\" + n.path_file);
                    }
                }
            }

            return "";
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
