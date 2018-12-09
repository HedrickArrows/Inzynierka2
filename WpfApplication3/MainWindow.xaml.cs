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
using Accord.MachineLearning.VectorMachines.Learning;
using Accord.Statistics.Kernels;

namespace WpfApplication3
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static Random rnd;
        public static float randomFocused(float min, float max, float focus) //generuje liczbe pseudolosową, z większą szansą na konkretny wynik
        {
            if (focus > max) focus = max - 0.1f;
            if (focus < min) focus = min;
            double A, res, r;// fMin, fMax, ratio;

            r = (max - min) * rnd.NextDouble() + min;
            if (r < focus) A = -1; else A = 1;
            res = A * Math.Pow(r - focus, 2) + focus*focus;
            //fMin = -1 * Math.Pow(min - focus, 2) + focus * focus;
            //fMax = Math.Pow(max - focus, 2) + focus * focus;
            //ratio = (max - min) / (fMax - fMin);

            res = Math.Sqrt(res); //(res - min)/(max-min) + min;

            res = res > max ? max : res < min ? min : res;

            float rounded = (float)(Math.Round(res, 2));
            return rounded;
        }

        public abstract class Attribute
        {
            public string range { get; set; }
            public abstract float genetare(float f);
            public abstract float getD();
        }
        public class BinaryAttribute : Attribute {
            public BinaryAttribute(int d) { defaultDensity = d; range = "%"; }
            public int defaultDensity;

            public override float getD()
            {
                return defaultDensity;
            }
            public override float genetare(float f)
            {
                return (rnd.Next(0, 100) < f ? 1 : 0);
            }
        }
        public class IntegerAttribute : Attribute {
                public int lowerLimit, higherLimit, defaultFocusPoint;
                public IntegerAttribute(int l, int h, int d) {
                lowerLimit = l; higherLimit = h; defaultFocusPoint = d;
                range = "<" + lowerLimit + ", " + higherLimit + ">"; }

            public override float genetare(float f)
            {
                return (float)Math.Round(randomFocused(lowerLimit,higherLimit,f));
            }

            public override float getD()
            {
                return defaultFocusPoint;
            }
        }

        public class FloatAttribute : Attribute
        {
            public float lowerLimit, higherLimit, defaultFocusPoint;
            public FloatAttribute(float l, float h, float d)
            {
                lowerLimit = l; higherLimit = h; defaultFocusPoint = d;
                range = "<" + lowerLimit.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    + ", " + higherLimit.ToString(System.Globalization.CultureInfo.InvariantCulture) + ">";
            }

            public override float genetare(float f)
            {
                return randomFocused(lowerLimit, higherLimit, f);
            }

            public override float getD()
            {
                return defaultFocusPoint;
            }
        }


        public List<KeyValuePair<string, float>> classes { get; set; }
        public List<KeyValuePair<string, Attribute>> attrs { get; set; }
        public List<List<float>> classAttrs { get; set; }
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

            classes = new List<KeyValuePair<string, float>>();
            attrs = new List<KeyValuePair<string, Attribute>>();
            classAttrs = new List<List<float>>();

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
                foreach(List<float> l in classAttrs)
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
                    List<float[]> attrValues = new List<float[]>();
                    List<int> classValues = new List<int>();

                    using (var file = new System.IO.StreamWriter(dirB.FileName)) {
                        string line;// = "Class";
                        //foreach (var v in attrs)
                        //    line += "," + v.Key;
                        //file.WriteLine(line);
                        for(int v = 0; v < classes.Count; v++)
                        //foreach (var v in classes)
                            for (int n = 0; n < classes[v].Value; n++) {
                                line = classes[v].Key;
                                classValues.Add(v);
                                List<float> aVals = new List<float>(); 

                                for(int t = 0; t < classAttrs[v].Count; t++)
                                {
                                    float aVal = attrs[t].Value.genetare(classAttrs[v][t]);
                                    aVals.Add(aVal);
                                    line += "," + aVal.ToString(System.Globalization.CultureInfo.InvariantCulture);
                                }

                                attrValues.Add(aVals.ToArray<float>());
                                file.WriteLine(line);
                            }

                    }

                    var dialogResult = System.Windows.MessageBox.Show("Do you want to test the generated data?", "Data testing - crossvalidation", System.Windows.MessageBoxButton.YesNo);
                    if (dialogResult == MessageBoxResult.Yes)
                    { 

                        float[][] inputs = attrValues.ToArray();
                        double[][] inputs_d = inputs.Select(xa => xa.Select(ya => (double)ya).ToArray()).ToArray();
                        int[][] inputs_i = inputs.Select(xa => xa.Select(ya => (int)Math.Round(ya*100)).ToArray()).ToArray();
                        int[] outputs = classValues.ToArray();


                        //var learn = new NaiveBayesLearning();
                        //NaiveBayes nb = learn.Learn(inputs, outputs);

                        var cv = CrossValidation.Create(
                            k: 4, 
                            learner: (p) => new NaiveBayesLearning(),                      
                            loss: (actual, expected, p) => new ZeroOneLoss(expected).Loss(actual),                        
                            fit: (teacher, x, y, w) => teacher.Learn(x, y, w),
                            x: inputs_i, y: outputs
                        );
                        
                        var result = cv.Learn(inputs_i, outputs);
                        
                        int numberOfSamples = result.NumberOfSamples; 
                        int numberOfInputs = result.NumberOfInputs;  
                        int numberOfOutputs = result.NumberOfOutputs; 

                        double trainingError = result.Training.Mean; 
                        double validationError = result.Validation.Mean;
                        GeneralConfusionMatrix gcm = result.ToConfusionMatrix(inputs_i, outputs);
                        double nb_accuracy = gcm.Accuracy;

                        //..................    
                        int classesSqrt = (int)Math.Round(Math.Sqrt(outputs.Length));
                        
                        var crossvalidation = CrossValidation.Create(
                            k: 4,
                            learner: (p) => new KNearestNeighbors(k: classesSqrt),
                            loss: (actual, expected, p) => new ZeroOneLoss(expected).Loss(actual),
                            fit: (teacher, x, y, w) => teacher.Learn(x, y, w),
                            x: inputs_d, y: outputs
                            );
                        var result2 = crossvalidation.Learn(inputs_d, outputs);
                        // We can grab some information about the problem:
                            numberOfSamples = result2.NumberOfSamples;
                            numberOfInputs = result2.NumberOfInputs;
                            numberOfOutputs = result2.NumberOfOutputs;

                        trainingError = result2.Training.Mean;
                        validationError = result2.Validation.Mean;

                        // If desired, compute an aggregate confusion matrix for the validation sets:
                        gcm = result2.ToConfusionMatrix(inputs_d, outputs);
                        double knn_accuracy = gcm.Accuracy;

                        //............................

                        var crossvalidationsvm = CrossValidation.Create(
                            k: 4,
                            learner: (p) => new MulticlassSupportVectorLearning<Gaussian>()
                            {
                                Learner = (param) => new SequentialMinimalOptimization<Gaussian>()
                                { UseKernelEstimation = true }
                            },
                            loss: (actual, expected, p) => new ZeroOneLoss(expected).Loss(actual),
                            fit: (teacher, x, y, w) => teacher.Learn(x, y, w),
                            x: inputs_d, y: outputs
                            );
                        //crossvalidationReadsvm.ParallelOptions.MaxDegreeOfParallelism = 1;
                        var resultsvm = crossvalidationsvm.Learn(inputs_d, outputs);
                        // We can grab some information about the problem:
                        var numberOfSamplessvm = resultsvm.NumberOfSamples;
                        var numberOfInputssvm = resultsvm.NumberOfInputs;
                        var numberOfOutputssvm = resultsvm.NumberOfOutputs;

                        var trainingErrorsvm = resultsvm.Training.Mean;
                        var validationErrorsvm = resultsvm.Validation.Mean;

                        var CMsvm = resultsvm.ToConfusionMatrix(inputs_d, outputs);
                        double svm_accuracy = CMsvm.Accuracy;


                        System.Windows.MessageBox.Show("Naive Bayes Accuracy: " + (nb_accuracy* 100)
                            .ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
                             + "%\n" + 
                            "\nk Nearest Neighbors Accuracy: " + (knn_accuracy*100)
                            .ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
                            + "%\n" +
                            "\nSupport Vector Machine Accuracy: " + (svm_accuracy * 100)
                            .ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
                            + "%\n", "Data testing - crossvalidation", System.Windows.MessageBoxButton.OK);
                        using (var write = new System.IO.StreamWriter("TestDataDump.txt"))
                        {
                            write.WriteLine("GeneratedDataAmt," + outputs.Length);
                            write.WriteLine("Accuracy," +
                                    (100.0 * knn_accuracy).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + "," +
                                    (100.0 * nb_accuracy).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + "," +
                                    (100.0 * svm_accuracy).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));

                        }
                        System.Diagnostics.Process.Start("TestDataDump.txt");
                        dialogResult = System.Windows.MessageBox.Show("Do you want to open the file with generated data?", "Data testing - extended data", System.Windows.MessageBoxButton.YesNo);
                        if (dialogResult == MessageBoxResult.Yes)
                        {
                            System.Diagnostics.Process.Start(dirB.FileName);
                        }
                    }
                }
            }
            
        }

        private void ClassAttrGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {

            string k = ClassAttrGrid.SelectedCells[0].Item.ToString();
            string k2 = e.EditingElement.ToString();
            var v = k2.Split(':');
            k2 = v[v.Length - 1];
            classAttrs[ClassGrid.SelectedIndex][ClassAttrGrid.SelectedIndex] =
                float.Parse(Regex.Replace(k2, @"[^\d-,.]", string.Empty), 
                    System.Globalization.NumberStyles.AllowDecimalPoint,
                    System.Globalization.NumberFormatInfo.InvariantInfo);
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
