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

        private void GenerateBasedOnData(string path) {
            Dictionary<string, Tuple<int, Dictionary<int,Dictionary<string, int>>>> values 
                = new Dictionary<string, Tuple<int, Dictionary<int, Dictionary<string, int>>>>();
            int classes = 0, attribs = 0;
            //podstawowy słownik - klasy //tuple - zliczanie wystąpień   //drugi słownik - atrybuty
            //trzeci słownik - unikatowe wartości i zliczanie ich wystąpień      //zjebany kod
            using (var read = new System.IO.StreamReader(path)) {
                string s;
                while (!string.IsNullOrEmpty(s = read.ReadLine()))
                {
                    string[] res = s.Split(',');
                    if (!res[0].Equals("Class")) { //jeśli jest linia opisująca, pomiń (z tego generatora jest)
                        if (!values.ContainsKey(res[0]))
                            values.Add(res[0], new Tuple<int, Dictionary<int, Dictionary<string, int>>>(0,
                                new Dictionary<int, Dictionary<string, int>>()));
                            //dodanie grupy jeśli nie istnieje, zaczęcie zliczania osobników
                        int i = 0;
                            foreach (string at in res){
                                if (!at.Equals(res[0]))
                                {
                                    if (!values[res[0]].Item2.ContainsKey(i))
                                        values[res[0]].Item2.Add(i, new Dictionary<string, int>());
                                    //dodanie atrybutu dla klasy jeśli nie istnieje

                                    if (values[res[0]].Item2[i].ContainsKey(at))
                                        values[res[0]].Item2[i][at]++;
                                    else
                                        values[res[0]].Item2[i].Add(at, 1);
                                    //zliczanie wartości dla atrybutu dla klasy
                                }
                                i++;
                            }
                        values[res[0]] = new Tuple<int, Dictionary<int, Dictionary<string, int>>>(
                            values[res[0]].Item1+1, values[res[0]].Item2); //przypisywanie nowej wartości do tupla jest do dupy
                                                                           //prawdopodobnie zmienię później jak mi się będzie chciało
                    }   //koniec "jeśli jest linia opisująca"
                    
                }//koniec pętli
            }//koniec czytania z pliku



            classes = values.Count();
            attribs = values.ElementAt(0).Value.Item2.Count();

            //czas generować ten szajs
            var newStuff = new string[classes * 30, attribs + 1]; ;
            for (int it = 0; it<classes*30;it++) {
                int cl = rnd.Next(classes); //rnd to zadelkarowany wcześniej Random //losowanie klasy
                newStuff[it,0] = values.ElementAt(cl).Key;
                for (int v = 1; v <= attribs; v++) {
                    int val = rnd.Next(values.ElementAt(cl).Value.Item1);  //losowanie wartości atrybutu
                    int b = 0;
                    foreach (var a in values.ElementAt(cl).Value.Item2[v]) {
                        if(val < (b+= a.Value)){
                            newStuff[it, v ] = a.Key; //na Monte Carlo
                            break;
                        }
                    }//koniec losowania wartości atrybutu
                }//koniec generowania obiektu
            }//koniec całego generowania

            //tu dać zapis do pliku
            using (var dirB = new System.Windows.Forms.SaveFileDialog())
            {
                dirB.Filter = "Text Files | *.txt";
                dirB.DefaultExt = "txt";
                var res = dirB.ShowDialog();
                if (res == System.Windows.Forms.DialogResult.OK)
                {
                    using (var write = new System.IO.StreamWriter(dirB.FileName))
                    {
                        for (int x = 0; x < newStuff.GetLength(0); x++){
                            string line = "";
                            for (int y = 0; y < newStuff.GetLength(1); y++){
                                line += newStuff[x, y] + ',';
                            }
                            write.WriteLine(line.Remove(line.Length -1));
                        }
                    }
                }
            }
            //tu dać walidację wygenerowanych danych

            var dialogResult = System.Windows.MessageBox.Show("Do you want to test the generated data?", "Data testing - crossvalidation", System.Windows.MessageBoxButton.YesNo);
            if (dialogResult == MessageBoxResult.Yes)
            {
                using (var read = new System.IO.StreamReader(path))
                {
                    List<string[]> reading = new List<string[]>();
                    Dictionary<string, int> counting = new Dictionary<string, int>(), 
                        grpToInt = new Dictionary<string, int>();
                    string s;
                    while (!string.IsNullOrEmpty(s = read.ReadLine())) //to zgarnia dane oryginalne, bo jestem taki dobry
                    {
                        reading.Add(s.Split(','));
                        if (counting.ContainsKey(s.Split(',')[0]))
                            counting[s.Split(',')[0]]++;
                        else
                            counting.Add(s.Split(',')[0], 1);
                        if (!grpToInt.ContainsKey(s.Split(',')[0]))
                            grpToInt.Add(s.Split(',')[0], grpToInt.Count);
                    }

                    List<string[]> trainSet = new List<string[]>(); //to wydziela zestaw treningowy
                    foreach (var v in counting){
                        int temp = v.Value / 2 + 1;
                        for (int i = temp; i > 0; i--) {
                            var ror = reading.Where(el =>el[0].Equals(v.Key)).Select(el => el).First();
                            reading.Remove(ror);
                            trainSet.Add(ror);
                        }
                    }

                    var trainOutputs = new int[trainSet.Count]; //ten kawałek kodu przerabia oryginalne dane do trenowania na coś co 
                    int[][] trainInputs = new int[trainSet.Count, trainSet.ElementAt(0).Length].ToJagged(); //klasyfikator skuma
                    for (int x = 0; x < trainSet.Count; x++)
                        { for (int y = 0; y < trainSet.ElementAt(0).Length; y++)
                            { if (y == 0) trainOutputs[x] = grpToInt[trainSet.ElementAt(x)[0]];
                            else trainInputs[x][y - 1] = int.Parse(trainSet.ElementAt(x)[y]); } }
                    double[][] trainInputs_d = trainInputs.Select(xa => xa.Select(ya => (double)ya).ToArray()).ToArray();

                    var outputs = new int[reading.Count]; //ten kawałek kodu przerabia oryginalne dane do testowania na coś co 
                    int[][] inputs = new int[reading.Count, reading.ElementAt(0).Length -1].ToJagged(); //klasyfikator skuma
                    for (int x = 0; x < reading.Count; x++)
                        {for (int y = 0; y < reading.ElementAt(0).Length; y++)
                            {if (y == 0) outputs[x] = grpToInt[reading.ElementAt(x)[0]];
                             else inputs[x][y - 1] = int.Parse(reading.ElementAt(x)[y]); } }
                    double[][] inputs_d = inputs.Select(xa => xa.Select(ya => (double)ya).ToArray()).ToArray();

                    var genoutputs = new int[newStuff.GetLength(0)]; //ten kawałek kodu przerabia nowo wygenerowane dane do testowania na coś co 
                    int[][] geninputs = new int[newStuff.GetLength(0), newStuff.GetLength(1) - 1].ToJagged(); //klasyfikator skuma
                    for (int x = 0; x < newStuff.GetLength(0); x++)  {
                        for (int y = 0; y < newStuff.GetLength(1); y++){
                            if (y == 0) genoutputs[x] = grpToInt[newStuff[x,0]];
                            else geninputs[x][y - 1] = int.Parse(newStuff[x,y]);} }
                    double[][] geninputs_d = geninputs.Select(xa => xa.Select(ya => (double)ya).ToArray()).ToArray();


                    var learn = new NaiveBayesLearning();
                    NaiveBayes nb = learn.Learn(trainInputs, trainOutputs);
                    /*
                    var cv = CrossValidation.Create(
                        k: 10,
                        learner: (p) => new NaiveBayesLearning(),
                        loss: (actual, expected, p) => new ZeroOneLoss(expected).Loss(actual),
                        fit: (teacher, x, y, w) => teacher.Learn(x, y, w),
                        x: trainInputs, y: trainOutputs
                    );
                    
                    var result = cv.Learn(inputs, outputs);
                    */

                    int original_correct = 0, original_incorrect = 0, new_correct = 0, new_incorrect = 0;
                    
                   // var test = 

                    foreach (var v in outputs) {
                        var r = nb.Decide(inputs[outputs.IndexOf(v)]);
                        if (v.Equals(r))
                            original_correct++;
                        else
                            original_incorrect++;
                    }
                    var r2 = nb.Decide(geninputs);
                    foreach (var v in genoutputs)
                    {
                        if (v.Equals(r2[genoutputs.IndexOf(v)]))
                            new_correct++;
                        else
                            new_incorrect++;
                    }

                    System.Windows.MessageBox.Show("Naive Bayes Classification:\n Original data: "+ original_correct + " correct, "
                       + original_incorrect+" wrong\nGenerated data: " + new_correct + " correct, " + new_incorrect + " wrong", 
                        "Data Testing - extending dataset" , 
                        System.Windows.MessageBoxButton.OK);
                    /*
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
                    */
                }


            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog k = new OpenFileDialog();
            if (k.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                GenerateBasedOnData(k.FileName);
        }
    }
}
