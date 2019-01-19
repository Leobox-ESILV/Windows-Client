using ShellBoost.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeoboxV2
{
    public class MyRootFolder : RootShellFolder
    {

        // we want to keep a reference on our custom ShellFolderServer
        public MyRootFolder(MyShellFolderServer server, ShellItemIdList idList)
            : base(idList)
        {
            Server = server;
        }
        
        public MyShellFolderServer Server { get; }



    }



}
