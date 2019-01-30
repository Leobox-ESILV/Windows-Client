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

        
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            new Thread(() =>
            {

                Console.WriteLine("here");
                var info = new DirectoryInfo(System.IO.Path.GetFullPath("C:/Users/dilan/OneDrive/Bureau/root"));
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


            new Thread(() =>
            {

                FileSystemWatcher watcher = new FileSystemWatcher();
                watcher.IncludeSubdirectories = true;
                watcher.Path = @"C:\Users\dilan\OneDrive\Bureau\root";
                watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
               | NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.CreationTime | 
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

                FileSystemWatcher changementWatcher = new FileSystemWatcher();
                changementWatcher.IncludeSubdirectories = true;
                changementWatcher.Path = @"C:\Users\dilan\OneDrive\Bureau\root";
                changementWatcher.NotifyFilter = NotifyFilters.Size;
                changementWatcher.Filter = "*.*";
                changementWatcher.Changed += new FileSystemEventHandler(OnChanged);

                changementWatcher.EnableRaisingEvents = true;
                while (true)
                {
                    Thread.Sleep(10000);
                }

            }).Start();


        }

        private static void OnDeleted(object sender, FileSystemEventArgs e)
        {
           Console.WriteLine("change type: " + e.ChangeType + " | fullPath: " + e.FullPath + " | name: " + e.Name);
        }


        private static void OnCreated(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine("change type: "+e.ChangeType + " | fullPath: " + e.FullPath + " | name: " + e.Name);
        }

        private static void OnRenamed(object sender, RenamedEventArgs e)
        {
            Console.WriteLine("change type: "+e.ChangeType + " | fullPath: " + e.FullPath + " | name: " + e.Name + " | oldName: " + e.OldName + " | oldPath: " + e.OldFullPath);
        }

        private static void OnChanged(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine("change type: " + e.ChangeType + " | fullPath: " + e.FullPath + " | name: " + e.Name);
        }



        private void Window_Closed(object sender, EventArgs e)
        {
            ShellFolderServer.UnregisterNativeDll(RegistrationMode.User);
            Console.WriteLine("Stopped"); // end of program
        }

        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            ShellFolderServer.UnregisterNativeDll(RegistrationMode.User);
            Console.WriteLine("Stopped"); // end of program
        }

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
