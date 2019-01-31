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
using RestSharp;

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

        
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string mail = txtLogin.Text;
            string pwd = txtPwd.Password.ToString();
            string rePwd = txtPwd2.Password.ToString();
            string username = txtUsername.Text.ToString();

            if(mail == "")
            {
                msgErreur.Text = "";
                msgErreur.Text = "Mail's field is empty.";
            }
            else if(username == "")
            {
                msgErreur.Text = "";
                msgErreur.Text = "Username's field is empty.";
            }
            else if (pwd == "")
            {
                msgErreur.Text = "";
                msgErreur.Text = "Password's field is empty.";
            }
            else if (rePwd == "")
            {
                msgErreur.Text = "";
                msgErreur.Text = "Re password's field is empty";
            }
            else if (pwd != rePwd)
            {
                msgErreur.Text = "";
                msgErreur.Text = "Passwords not matching.";
            }
            else
            {
                var client = new RestClient("http://leobox.org:8080/v1/user/create?email=" + mail + "&username=" + username + "&password=" + pwd);
                var request = new RestRequest(Method.POST);
                request.AddHeader("cache-control", "no-cache");

                IRestResponse response = client.Execute(request);

                var res = JsonConvert.DeserializeObject<Dictionary<string, string>>(response.Content);
                var comment = "";
                var status = "";

                foreach (KeyValuePair<string, string> kvp in res)
                {
                    if (kvp.Key == "comment")
                    {
                        comment = kvp.Value;
                    }
                    else if (kvp.Key == "is_status")
                    {
                        status = kvp.Value;
                    }
                }
                
                if(status != "200")
                {
                    msgErreur.Text = "";
                    msgErreur.Text = comment;
                }
                else
                {
                    MessageBox.Show(comment + ", you can now login in.");
                    _NavigationFrame.Navigate(new login());
                }

            }
        }
    }
}
