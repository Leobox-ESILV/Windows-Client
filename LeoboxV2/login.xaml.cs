using Newtonsoft.Json;
using ShellBoost.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ShellBoost.Core.WindowsShell;
using System.IO;
using ShellBoost.Core.Utilities;
using ShellBoost.Core.WindowsPropertySystem;
using System.Windows.Forms;
using System.Security.Permissions;
using RestSharp;

namespace LeoboxV2
{
    /// <summary>
    /// Logique d'interaction pour login.xaml
    /// </summary>
    /// 
    public partial class login : Page
    {
        public login()
        {
            InitializeComponent();

        }

        private const string URL = "http://leobox.org:8080/v1/user/login";
        private string urlParameters = "";


        private void Button_Click(object sender, RoutedEventArgs e)
        {

            string login = txtLogin.Text;
            string pwd = txtPwd.Password.ToString();

            var client = new RestClient("http://leobox.org:8080/v1/user/login?username="+login+"&password="+pwd);
            var request = new RestRequest(Method.GET);
            request.AddHeader("cache-control", "no-cache");
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
                if(kvp.Key == "comment")
                {
                    comment = kvp.Value;
                }
            }

            if (status == "true")
            {
                msgErreur.Text = "";
                msgErreur.Text = comment;
                
                user currentUser = JsonConvert.DeserializeObject<user>(response.Content);
                
                //save current user info on global
                globalUser.Name = currentUser.Name;
                globalUser.Email = currentUser.Email;
                globalUser.Expiration_token = currentUser.Expiration_token;
                globalUser.Path_home = currentUser.Path_home;
                globalUser.Quota = currentUser.Quota;
                globalUser.Used_space = currentUser.Used_space;
                globalUser.User_token = currentUser.User_token;

                Console.WriteLine("Registered");
                
            }
            else
            {
                msgErreur.Text = "";
                msgErreur.Text = comment;
            }

        }


        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            //running ru = new running();
            //ru.Show();
        }

        /*[PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public static void watch()
        {
            FileSystemWatcher watcher = new FileSystemWatcher();
            watcher.Path = @"C:\Users\dilan\OneDrive\Bureau\root";
            watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
           | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            watcher.Filter = "*.*";
            watcher.Changed += new FileSystemEventHandler(OnChanged);
            watcher.Renamed += new RenamedEventHandler(OnRenamed);
            watcher.EnableRaisingEvents = true;

            Console.WriteLine("Press \'q\' to quit the sample.");
            while (Console.Read() != 'q') ;

        }

        private static void OnRenamed(object sender, RenamedEventArgs e)
        {
            Console.WriteLine("something changed...");
        }

        private static void OnChanged(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine("something changed...");
        }*/

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            _NavigationFrame.Navigate(new inscription());
        }

        private void Hyperlink_Click_1(object sender, RoutedEventArgs e)
        {
            _NavigationFrame.Navigate(new resetPwd());
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
