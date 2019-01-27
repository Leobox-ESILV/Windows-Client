using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeoboxV2
{
    public class user
    {

        private string name;
        private string email;
        private int expiration_token;
        private string path_home;
        private long quota;
        private int used_space;
        private string user_token;

        [JsonProperty("display_name")]
        public string Name { get => name; set => name = value; }

        [JsonProperty("email")]
        public string Email { get => email; set => email = value; }

        [JsonProperty("expiration_token")]
        public int Expiration_token { get => expiration_token; set => expiration_token = value; }

        [JsonProperty("path_home")]
        public string Path_home { get => path_home; set => path_home = value; }

        [JsonProperty("quota")]
        public long Quota { get => quota; set => quota = value; }

        [JsonProperty("used_space")]
        public int Used_space { get => used_space; set => used_space = value; }

        [JsonProperty("user_token")]
        public string User_token { get => user_token; set => user_token = value; }

        public user(string display_name, string email, int expiration_token, string path_home, long quota, int used_space, string user_token)
        {
            this.name = display_name;
            this.email = email;
            this.expiration_token = expiration_token;
            this.path_home = path_home;
            this.quota = quota;
            this.used_space = used_space;
            this.user_token = user_token;
        }

    }
}
