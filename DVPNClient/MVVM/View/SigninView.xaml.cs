using DowngradVPN.MVVM.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
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

namespace DowngradVPN.MVVM.View
{
    /// <summary>
    /// Interaction logic for SigninView.xaml
    /// </summary>
    public partial class SigninView : UserControl
    {

      

        public SigninView() {
            InitializeComponent();
        }
        public string Username
        {
            get { return txtUsername.Text; }
        }

        public string Password
        {
            get { return txtPassword.Password; }
        }
    }
}
