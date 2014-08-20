using Sando.UI.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Sando.UI.View
{
    /// <summary>
    /// Interaction logic for SandoMainView.xaml
    /// </summary>
    public partial class SandoMainView : UserControl
    {
        public SandoMainView()
        {

            SandoMainViewModel vm = new SandoMainViewModel();
            this.DataContext = vm;

            InitializeComponent();
        }
    }
}
