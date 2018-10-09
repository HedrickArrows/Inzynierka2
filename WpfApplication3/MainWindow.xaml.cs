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
using System.Windows.Forms;
using System.Numerics;
using Accord.MachineLearning.Performance;
using Accord.MachineLearning.Bayes;
using Accord.IO;
using Accord.Math;
using Accord.Statistics.Distributions.Univariate;
using Accord.Statistics.Models.Markov;
using Accord.MachineLearning.DecisionTrees;
using Accord.MachineLearning;
using Accord.Math.Optimization.Losses;
using Accord.Statistics.Analysis;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace WpfApplication3
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static Random rnd;
        public static int randomFocused(int min, int max, int focus) //generuje liczbe pseudolosową, z większą szansą na konkretny wynik
        {
            if (focus > max - 2) focus = max - 2;
            if (focus < min) focus = min;
            double A, res, r;

            r = rnd.Next(min, max - 1) + rnd.NextDouble();
            if (r < focus) A = -1; else A = 1;
            res = A * Math.Pow(r - focus, 2) + focus * focus;
            return (int)res / (max - 1);
        }

        public abstract class Attribute
        {
            public string range { get; set; }
            public abstract int genetare(int f);
            public abstract int getD();
        }
        public class BinaryAttribute : Attribute {
            public BinaryAttribute(int d) { defaultDensity = d; range = "%"; }
            public int defaultDensity;

            public override int getD()
            {
                return defaultDensity;
            }
            public override int genetare(int f)
            {
                return (rnd.Next(0, 100) < f ? 1 : 0);
            }
        }
        public class IntegerAttribute : Attribute {
                public int lowerLimit, higherLimit, defaultFocusPoint;
                public IntegerAttribute(int l, int h, int d) {
                lowerLimit = l; higherLimit = h; defaultFocusPoint = d;
                range = "<" + lowerLimit + ", " + higherLimit + ">"; }

            public override int genetare(int f)
            {
                return randomFocused(lowerLimit,higherLimit,f);
            }

            public override int getD()
            {
                return defaultFocusPoint;
            }

            //public override string getRange() { return "<" + lowerLimit + ", " + higherLimit + ">"; }

        }
        

        public List<KeyValuePair<string, int>> classes { get; set; }
        public List<KeyValuePair<string, Attribute>> attrs { get; set; }
        public List<List<int>> classAttrs { get; set; }
        //public List<int> selectedClassAttrs { get; set; }

        private Window clw;
        private Window attrw;


        public UpdateSourceTrigger UpdateSourceTrigger { get; set; }


        public MainWindow()
        {

            InitializeComponent();
            clw = null;
            attrw = null;

            foreach (DataGridColumn column in ClassAttrGrid.Columns)
            {
                column.CanUserSort = false;
            }
            foreach (DataGridColumn column in AttrGrid.Columns)
            {
                column.CanUserSort = false;
            }

            classes = new List<KeyValuePair<string, int>>();
            attrs = new List<KeyValuePair<string, Attribute>>();
            classAttrs = new List<List<int>>();

            rnd = new Random();

            ClassGrid.ItemsSource = classes;
            AttrGrid.ItemsSource = attrs;

            

        }

        private void ClassGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ClassGrid.SelectedIndex >= 0)
            {
                ClassAttrGrid.ItemsSource = null;
                ClassAttrGrid.ItemsSource = classAttrs.ElementAt(ClassGrid.SelectedIndex);
                
            }
        }

        private void Button1_Click(object sender, RoutedEventArgs e) //Add Class
        {
            clw = new AddClassWindow(classes, attrs, classAttrs, this);
            clw.Show();
        }

        private void Button3_Click(object sender, RoutedEventArgs e) //Add Attr
        {
            attrw = new AddAttrWindow(classes, attrs, classAttrs, this);
            attrw.Show();
        }

        private void Button2_Click(object sender, RoutedEventArgs e) //Del Class
        {
            int i = ClassGrid.SelectedIndex;
            if (i >= 0)
            {
                classes.Remove(classes.ElementAt(i));
                classAttrs.Remove(classAttrs.ElementAt(i));

                ClassGrid.ItemsSource = null;
                ClassGrid.ItemsSource = classes;
                ClassAttrGrid.ItemsSource =  null;
                ClassGrid.SelectedIndex = -1;
                AttrGrid.SelectedIndex = -1;
            }
        }

        private void Button4_Click(object sender, RoutedEventArgs e) //Del Attr
        {
            int i = AttrGrid.SelectedIndex;
            if (i >= 0)
            {
                attrs.Remove(attrs.ElementAt(i));
                foreach(List<int> l in classAttrs)
                l.Remove(l.ElementAt(i));

                AttrGrid.ItemsSource = null;
                AttrGrid.ItemsSource = attrs;
                ClassAttrGrid.ItemsSource = null;
                AttrGrid.SelectedIndex = -1;
                ClassGrid.SelectedIndex = -1;
            }
        }

        private void Button5_Click(object sender, RoutedEventArgs e) //Generate
        {
            if (classes.Count < 2) {
                var dialogResult = System.Windows.MessageBox.Show(
                "Please have at least two classes created to generate",
                "Data generating error", System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
                return;
            }
            if (attrs.Count < 1)
            {
                var dialogResult = System.Windows.MessageBox.Show(
                "Please have at least one attribute created to generate",
                "Data generating error", System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
                return;
            }
            using (var dirB = new System.Windows.Forms.SaveFileDialog()) {
                dirB.Filter = "Text Files | *.txt";
                dirB.DefaultExt = "txt";
                var res = dirB.ShowDialog();
                if (res == System.Windows.Forms.DialogResult.OK){
                    List<int[]> attrValues = new List<int[]>();
                    List<int> classValues = new List<int>();

                    using (var file = new System.IO.StreamWriter(dirB.FileName)) {
                        string line = "Class";
                        foreach (var v in attrs)
                            line += "," + v.Key;
                        file.WriteLine(line);
                        foreach (var v in classes)
                            for (int n = 0; n < v.Value; n++) {
                                line = v.Key;
                                classValues.Add(classes.IndexOf(v));
                                List<int> aVals = new List<int>(); 

                                int t = 0;
                                foreach (var a in classAttrs[classes.IndexOf(v)])
                                {
                                    int aVal = attrs[t].Value.genetare(v.Value);
                                    aVals.Add(aVal);
                                    line += "," + aVal.ToString();
                                    t++;
                                }

                                attrValues.Add(aVals.ToArray<int>());
                                file.WriteLine(line);
                            }

                    }

                    var dialogResult = System.Windows.MessageBox.Show("Do you want to test the generated data?", "Data testing - crossvalidation", System.Windows.MessageBoxButton.YesNo);
                    if (dialogResult == MessageBoxResult.Yes)
                    { 

                        int[][] inputs = attrValues.ToArray();
                        double[][] inputs_d = inputs.Select(xa => xa.Select(ya => (double)ya).ToArray()).ToArray();
                        int[] outputs = classValues.ToArray();


                        var learn = new NaiveBayesLearning();
                        NaiveBayes nb = learn.Learn(inputs, outputs);

                        var cv = CrossValidation.Create(
                            k: 10, 
                            learner: (p) => new NaiveBayesLearning(),                      
                            loss: (actual, expected, p) => new ZeroOneLoss(expected).Loss(actual),                        
                            fit: (teacher, x, y, w) => teacher.Learn(x, y, w),
                            x: inputs, y: outputs
                        );
                        
                        var result = cv.Learn(inputs, outputs);
                        
                        int numberOfSamples = result.NumberOfSamples; 
                        int numberOfInputs = result.NumberOfInputs;  
                        int numberOfOutputs = result.NumberOfOutputs; 

                        double trainingError = result.Training.Mean; 
                        double validationError = result.Validation.Mean;
                        GeneralConfusionMatrix gcm = result.ToConfusionMatrix(inputs, outputs);
                        double nb_accuracy = gcm.Accuracy;

                        //..................    

                        
                        var crossvalidation = CrossValidation.Create(
                            k: 3,
                            learner: (p) => new KNearestNeighbors(k: 4),
                            loss: (actual, expected, p) => new ZeroOneLoss(expected).Loss(actual),
                            fit: (teacher, x, y, w) => teacher.Learn(x, y, w),
                            x: inputs_d, y: outputs
                            );
                        var result2 = crossvalidation.Learn(inputs_d, outputs);
                        // We can grab some information about the problem:
                            numberOfSamples = result.NumberOfSamples;
                            numberOfInputs = result.NumberOfInputs;
                            numberOfOutputs = result.NumberOfOutputs;

                        trainingError = result2.Training.Mean;
                        validationError = result2.Validation.Mean;

                        // If desired, compute an aggregate confusion matrix for the validation sets:
                        gcm = result2.ToConfusionMatrix(inputs_d, outputs);
                        double knn_accuracy = gcm.Accuracy;

                        System.Windows.MessageBox.Show("Naive Bayes Accuracy: " + nb_accuracy + 
                        "\nk Nearest Neighbors Accuracy: " + knn_accuracy, "Data testing - crossvalidation", System.Windows.MessageBoxButton.OK);
                    }
                }
            }
            
        }

        private void ClassAttrGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {

            string k = ClassAttrGrid.SelectedCells[0].Item.ToString();
            string k2 = e.EditingElement.ToString();
            classAttrs[ClassGrid.SelectedIndex][ClassAttrGrid.SelectedIndex] = Int32.Parse(Regex.Replace(e.EditingElement.ToString(), @"[^\d-]", string.Empty));
            ClassAttrGrid.ItemsSource = null;
            ClassAttrGrid.ItemsSource = classAttrs[ClassGrid.SelectedIndex];
            
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Window dxw = new DataExtendWindow();
            dxw.Show();

        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var result = System.Windows.MessageBox.Show(
                "Do you want to close the application?", "Data Generator Exit", 
                System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                System.Windows.Application.Current.Shutdown();
            }
            else
                e.Cancel = true;
        }
    }
}
