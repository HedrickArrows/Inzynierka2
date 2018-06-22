using System;
using System.Collections.Generic;
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
    /// Interaction logic for AddAttrWindow.xaml
    /// </summary>
    public partial class AddAttrWindow : Window
    {
        public List<KeyValuePair<string, int>> cl;
        public List<KeyValuePair<string, MainWindow.Attribute>> a;
        public List<List<int>> cla;
        public MainWindow p;
        public List<string> at;
        public AddAttrWindow(List<KeyValuePair<string, int>> classes, List<KeyValuePair<string, MainWindow.Attribute>> attributes,
            List<List<int>> lists, MainWindow parent)
        {
            cl = classes;
            p = parent;
            a = attributes;
            cla = lists;
            InitializeComponent();
            at = new List<string> { "Binary", "Integer" };
            AttrType.ItemsSource = at;
            AttrType.SelectedIndex = 0;
        }

        private void AttrType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AttrType.SelectedIndex.Equals(1))
            {
                LinearGradientBrush myBrush = new LinearGradientBrush();
                myBrush.GradientStops.Add(new GradientStop(Colors.White, 1.0));
                lowLimit.IsReadOnly = false;
                lowLimit.Background = myBrush;
                hiLimit.IsReadOnly = false;
                hiLimit.Background = myBrush;
                dVal.Text = "Def Value";
                upLim.Text = "Upper Limit";
                loLim.Text = "Lower Limit";
            }
            else {
                LinearGradientBrush myBrush = new LinearGradientBrush();
                myBrush.GradientStops.Add(new GradientStop(Colors.LightGray, 0.25));
                lowLimit.IsReadOnly = true;
                lowLimit.Background = myBrush;
                hiLimit.IsReadOnly = true;
                hiLimit.Background = myBrush;
                dVal.Text = "Def Density";
                upLim.Text = "";
                loLim.Text = "";
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        

        private void AddAttr_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                /*
                if (!String.IsNullOrEmpty(defValue.Text) &&
                    Int32.Parse(defValue.Text) >= Int32.Parse(hiLimit.Text) 
                    && AttrType.SelectedIndex.Equals(1))
                    defValue.Text = (Int32.Parse(hiLimit.Text) - 1).ToString();
                if (!String.IsNullOrEmpty(defValue.Text) &&
                    Int32.Parse(defValue.Text) < Int32.Parse(lowLimit.Text) 
                    && AttrType.SelectedIndex.Equals(1))
                    defValue.Text = lowLimit.Text;
                */

                MainWindow.Attribute attr = null;
                if (AttrType.SelectedIndex.Equals(0))
                {
                    attr = new MainWindow.BinaryAttribute(Int32.Parse(defValue.Text));
                }
                else
                {
                    attr = new MainWindow.IntegerAttribute(Int32.Parse(lowLimit.Text),
                        Int32.Parse(hiLimit.Text), Int32.Parse(defValue.Text));

                }
                a.Add(new KeyValuePair<string, MainWindow.Attribute>(AttrName.Text, attr));
                foreach (List<int> list in cla)
                {
                    list.Add(attr.getD());
                }
                p.AttrGrid.ItemsSource = null;
                p.AttrGrid.ItemsSource = a;
                p.ClassAttrGrid.ItemsSource = null;
                this.Close();
            }
            catch (Exception) { }
        }

        private void ValidateNumberText(TextBox txt) {
            txt.Text = Regex.Replace(txt.Text, @"[^\d-]", string.Empty);
            txt.SelectionStart = txt.Text.Length; // add some logic if length is 0
            txt.SelectionLength = 0;
        }

        private void defValue_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateNumberText(defValue);
        }

        private void lowLimit_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateNumberText(lowLimit);
            /*
            if (!String.IsNullOrEmpty(defValue.Text) &&
                Int32.Parse(defValue.Text) < Int32.Parse(lowLimit.Text))
                defValue.Text = lowLimit.Text;
            if (!String.IsNullOrEmpty(hiLimit.Text) &&
                Int32.Parse(hiLimit.Text) <= Int32.Parse(lowLimit.Text))
                lowLimit.Text = (Int32.Parse(hiLimit.Text) - 1).ToString();
                */
        }

        private void hiLimit_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateNumberText(hiLimit);
            /*
            if (!String.IsNullOrEmpty(defValue.Text) &&
                Int32.Parse(defValue.Text) >= Int32.Parse(hiLimit.Text))
                defValue.Text = (Int32.Parse(hiLimit.Text) - 1).ToString();
            if (!String.IsNullOrEmpty(lowLimit.Text) &&
                Int32.Parse(lowLimit.Text) >= Int32.Parse(hiLimit.Text))
                hiLimit.Text = (Int32.Parse(lowLimit.Text) + 1).ToString();
                */
        }
    }
}
