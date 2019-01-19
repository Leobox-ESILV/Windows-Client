using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeoboxV2
{
    public static class globalUser
    {
        private static string name;
        private static string email;
        private static int expiration_token;
        private static string path_home;
        private static int quota;
        private static int used_space;
        private static string user_token;

        public static string Name { get => name; set => name = value; }

        public static string Email { get => email; set => email = value; }

        public static int Expiration_token { get => expiration_token; set => expiration_token = value; }

        public static string Path_home { get => path_home; set => path_home = value; }

        public static int Quota { get => quota; set => quota = value; }

        public static int Used_space { get => used_space; set => used_space = value; }

        public static string User_token { get => user_token; set => user_token = value; }

    }
}
