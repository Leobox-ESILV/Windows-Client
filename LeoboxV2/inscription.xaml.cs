using Newtonsoft.Json;
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

namespace LeoboxV2
{
    /// <summary>
    /// Logique d'interaction pour inscription.xaml
    /// </summary>
    public partial class inscription : Page
    {
        public inscription()
        {
            InitializeComponent();
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            _NavigationFrame.Navigate(new login());
        }


        private const string URL = "http://leobox.org:8080/v1/user/create";

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string mail = txtLogin.Text;
            string pwd = txtPwd.Password.ToString();
            string rePwd = txtPwd2.Password.ToString();
            string username = txtUsername.ToString();

            if(pwd != rePwd)
            {
                msgErreur.Text = "Passwords not matching.";
            }
            else
            {
                string urlParameters = "?email=" + mail + "&username=" + username + "&password=" + pwd;

                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(URL);

                // Add an Accept header for JSON format.
                client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

                // List data response.
                HttpResponseMessage response = client.GetAsync(urlParameters).Result;

                Console.WriteLine(response);

                if (response.IsSuccessStatusCode)
                {
                    client.Dispose();
                    msgErreur.Text = "";
                    msgErreur.Text = "You are now registered!";

                }
                else
                {

                    var result = response.Content.ReadAsStringAsync();
                    var ok = JsonConvert.DeserializeObject<Dictionary<string, string>>(result.Result);
                    var comment = "";

                    foreach (KeyValuePair<string, string> kvp in ok)
                    {
                        if (kvp.Key == "comment")
                        {
                            comment = kvp.Value;
                            break;
                        }
                    }
                    msgErreur.Text = "";
                    msgErreur.Text = comment;
                }

            }

        }
    }
}
