using ShellBoost.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeoboxV2
{
    public class MyShellFolderServer : ShellFolderServer
    {

        private MyRootFolder _root;

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
}
