using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WpfApplication3
{
    /// <summary>
    /// Interaction logic for AddClassWindow.xaml
    /// </summary>
    public partial class AddClassWindow : Window
    {
        public List<KeyValuePair<string, float>> cl;
        public List<KeyValuePair<string, MainWindow.Attribute>> a;
        public List<List<float>> cla;
        public MainWindow p;
        private List<float> temp { get; set; }
        public AddClassWindow(List<KeyValuePair<string, float>> classes,
            List<KeyValuePair<string, MainWindow.Attribute>> attributes,
            List<List<float>> lists, MainWindow parent)
        {
            cl = classes;
            p = parent;
            a = attributes;
            cla = lists;
            InitializeComponent();
        }

        private void Exit(object sender, RoutedEventArgs e)
        {
            this.Close();
        }


        private void AddClass(object sender, RoutedEventArgs e)
        {
            try
            {
                cl.Add(new KeyValuePair<string, float>(ClassName.Text, Int32.Parse(ClassSize.Text)));
                p.ClassGrid.ItemsSource = null;
                p.ClassGrid.ItemsSource = cl;
                temp = new List<float>();
                foreach (KeyValuePair<string, MainWindow.Attribute> k in a) {
                    temp.Add(k.Value.getD());
                }
                cla.Add(temp);
                this.Close();
            }
            catch (Exception) {

            }
        }


        private void ClassSize_TextChanged(object sender, TextChangedEventArgs e)
        {
            ClassSize.Text = Regex.Replace(ClassSize.Text, @"[^\d-]", string.Empty);
            ClassSize.SelectionStart = ClassSize.Text.Length; // add some logic if length is 0
            ClassSize.SelectionLength = 0; 
        }
    }
}
