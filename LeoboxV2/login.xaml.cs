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

namespace LeoboxV2
{
    /// <summary>
    /// Logique d'interaction pour login.xaml
    /// </summary>
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

            urlParameters = "?username=" + login + "&password=" + pwd;

            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(URL);

            // Add an Accept header for JSON format.
            client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

            // List data response.
            HttpResponseMessage response = client.GetAsync(urlParameters).Result;  // Blocking call! Program will wait here until a response is received or a timeout occurs.
            if (response.IsSuccessStatusCode)
            {

                client.Dispose();
                msgErreur.Text = "";
                msgErreur.Text = "OK good";

                var result = response.Content.ReadAsStringAsync();
                user currentUser = JsonConvert.DeserializeObject<user>(result.Result);


                //save current user info on global
                globalUser.Name = currentUser.Name;
                globalUser.Email = currentUser.Email;
                globalUser.Expiration_token = currentUser.Expiration_token;
                globalUser.Path_home = currentUser.Path_home;
                globalUser.Quota = currentUser.Quota;
                globalUser.Used_space = currentUser.Used_space;
                globalUser.User_token = currentUser.User_token;


                ShellFolderServer.RegisterNativeDll(RegistrationMode.User);
                using (var server = new MyShellFolderServer())
                {
                    var config = new ShellFolderConfiguration();   // this class is located in ShellBoost.Core
                    server.Start(config); // start the server
                    //ShellFolderServer.UnregisterNativeDll(RegistrationMode.User);

                   

                    //MyRootFolder MRF = new MyRootFolder(server, new ShellItemIdList());
                    
                   // new ShellFolder(, new StringKeyShellItemId("My First Folder"));
                    
                }

            }
            else
            {
                //Console.WriteLine("{0} ({1})", (int)response.StatusCode, response.ReasonPhrase);
                msgErreur.Text = "";
                msgErreur.Text = "non";
                client.Dispose();
            }

        }
        
    }



    
}
