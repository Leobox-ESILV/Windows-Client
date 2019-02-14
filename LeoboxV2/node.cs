using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeoboxV2
{
    public class node
    {
        public int id { get; set; }
        public string mime_type { get; set; }
        public string name { get; set; }
        public string path_file { get; set; }
        public Int64 size { get; set; }
        public Int64 storage_mtime { get; set; }
        public List<node> sub_dir { get; set; }
        public string type { get; set; }

        public node ()
        {
            this.sub_dir = new List<node>();
        }

    }
}
