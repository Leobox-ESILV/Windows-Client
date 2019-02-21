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

            if (status == "200")
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
                running ru = new running();
                App.Current.Windows[0].Close();
                ru.Show();
                
                
            }
            else
            {
                msgErreur.Text = "";
                msgErreur.Text = comment;
            }

        }


        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
           
        }
        

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            _NavigationFrame.Navigate(new inscription());
        }

        private void Hyperlink_Click_1(object sender, RoutedEventArgs e)
        {
            _NavigationFrame.Navigate(new resetPwd());
        }
        
       
    }



    
}
