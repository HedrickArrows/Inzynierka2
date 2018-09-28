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

        public class Spline3Deg { //klasa funkcji sklejanej 3. stopnia, służącą za ciągłą funkcję prawdopodobieństwa
            private int n;
            private double[] a, b, c, d, x;

            public Spline3Deg(double[] x, double[] y) {
                n = x.Length - 1;
                Array.Copy(x, this.x = new double[n+1], n+1);
                Array.Copy(y, this.a = new double[n+1], n+1);
                b = new double[n];
                c = new double[n+1];
                d = new double[n];

                double[] h = new double[n];
                for (int i = 0; i < n; i++)
                    h[i] = x[i + 1] - x[i];
                double[] alpha = new double[n - 1];
                for (int i = 1; i < n; i++)
                    alpha[i-1] = (3 * (a[i + 1] - a[i]) / h[i] - 3 * (a[i] - a[i - 1]) / h[i - 1]);

                double[] l = new double[n+1], 
                         mu = new double[n+1], 
                         z = new double[n+1];
                l[0] = 1;
                mu[0] = 0;
                z[0] = 0;

                for (int i = 1; i < n; ++i)
                {
                    l[i] = 2 * (x[i + 1] - x[i - 1]) - h[i - 1] * mu[i - 1];
                    mu[i] = h[i] / l[i];
                    z[i] = (alpha[i - 1] - h[i - 1] * z[i - 1]) / l[i];
                }

                l[n] = 1;
                z[n] = 0;
                c[n] = 0;

                for (int j = n - 1; j >= 0; --j)
                {
                    c[j] = z[j] - mu[j] * c[j + 1];
                    b[j] = (a[j + 1] - a[j]) / h[j] - h[j] * (c[j + 1] + 2 * c[j]) / 3;
                    d[j] = (c[j + 1] - c[j]) / 3 / h[j];
                }
            }
            public double y(double x0) {
                for (int i = 0; i < n; i++)
                    if (x0 >= x[i] && x0 <= x[i+1])
                        return Math.Pow(x0 - x[i], 3) * d[i] + Math.Pow(x0 - x[i], 2) * c[i] + (x0 - x[i]) * b[i] + a[i];
                return Double.NaN;
            }
            public Tuple<double, double> Limits() { return new Tuple<double, double>(x[0], x[n]); }
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
            List<string[]> reading = new List<string[]>(), 
                generating = new List<string[]>(); // do ewentualnego sprawdzania
            using (var read = new System.IO.StreamReader(path)) {
                string s;
                while (!string.IsNullOrEmpty(s = read.ReadLine()))
                {
                    string[] res = s.Split(',');
                    reading.Add(res);
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

            //tutaj dorabiam myk na sprawdzanie typu każdego z atrybutów
            string[] attrType = new string[attribs];
            int tempI; double tempD;
            for (int i = 0; i < attribs; i++) {
                foreach (var v in values) {
                    foreach (var w in v.Value.Item2.ElementAt(i).Value) {
                        if (int.TryParse(w.Key, out tempI))
                            attrType[i] = "integer";
                        else if (double.TryParse(w.Key, 
                            System.Globalization.NumberStyles.AllowDecimalPoint, 
                            System.Globalization.NumberFormatInfo.InvariantInfo,
                            out tempD))
                            attrType[i] = "double";
                        else {
                            attrType[i] = "string";
                            break;
                        }
                    }
                    if (attrType[i].Equals("string")) break;
                }
            }

            //tutaj dorzucam tworzenie wykresu ciągłego prawdopodobieństwa
            Spline3Deg[,] probabilities = new Spline3Deg[classes, attribs];
            for (int i = 0; i < attribs; i++)
                if (attrType[i].Equals("double") || attrType[i].Equals("integer"))
                    for (int j = 0; j < classes; j++) {
                        int c = values.ElementAt(j).Value.Item2.ElementAt(i).Value.Count;
                        double[] y, x = new double[c];
                        SortedList<double, int> temp = new SortedList<double, int>();
                        foreach (var v in values.ElementAt(j).Value.Item2.ElementAt(i).Value){
                            int tI = v.Value; double tD = Double.Parse(v.Key, 
                                System.Globalization.NumberStyles.AllowDecimalPoint, 
                                System.Globalization.NumberFormatInfo.InvariantInfo);
                            temp.Add(tD, tI);
                        }
                        y = temp.Keys.ToArray();
                        x[0] = 0;
                        for (int k = 1; k < temp.Count; k++)
                            x[k] = x[k - 1] + temp.ElementAt(k - 1).Value + temp.ElementAt(k).Value;
                        probabilities[j,i] = new Spline3Deg(x,y);
                    }

            //czas generować ten szajs
            var newStuff = new string[classes * reading.Count * 20, attribs + 1]; 
            for (int it = 0; it<newStuff.GetLength(0); it++) {
                int cl = rnd.Next(classes); //rnd to zadelkarowany wcześniej Random //losowanie klasy
                newStuff[it,0] = values.ElementAt(cl).Key;
                for (int v = 1; v <= attribs; v++) {  //losowanie wartości atrybutu
                    if (attrType[v - 1].Equals("string")){  //funkcja dyskretna
                        int val = rnd.Next(values.ElementAt(cl).Value.Item1);  
                        int b = 0;
                        foreach (var a in values.ElementAt(cl).Value.Item2[v]){
                            if (val < (b += a.Value)){
                                newStuff[it, v] = a.Key; //na Monte Carlo
                                break;
                            }
                        }
                    }else{  //funkcja ciągła
                        Tuple<double, double> extr = probabilities[cl, v - 1].Limits();
                        double val = rnd.Next((int)extr.Item1, (int)extr.Item2) + rnd.NextDouble();
                        double r = probabilities[cl, v - 1].y(val);
                        if (attrType[v - 1].Equals("double"))
                            newStuff[it, v] = r.ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture);
                        else //if (attrType[v - 1].Equals("integer"))
                            newStuff[it, v] = Math.Round(r).ToString();
                    }//koniec losowania wartości atrybutu
                }//koniec generowania obiektu
            }//koniec całego generowania

            //tu dać zapis do pliku
            string savefiledir = "";
            using (var dirB = new System.Windows.Forms.SaveFileDialog())
            {
                dirB.Filter = "Text Files | *.txt";
                dirB.DefaultExt = "txt";
                var res = dirB.ShowDialog();
                if (res == System.Windows.Forms.DialogResult.OK)
                {
                    using (var write = new System.IO.StreamWriter(savefiledir = dirB.FileName))
                    {
                        for (int x = 0; x < newStuff.GetLength(0); x++){
                            string line = "";
                            for (int y = 0; y < newStuff.GetLength(1); y++){
                                line += newStuff[x, y] + ',';
                            }
                            line = line.Remove(line.Length - 1);
                            generating.Add(line.Split(','));
                            write.WriteLine(line);
                        }
                    }
                }
            }
            //tu dać walidację wygenerowanych danych

            var dialogResult = System.Windows.MessageBox.Show("Do you want to test the generated data?", "Data testing - extended data", System.Windows.MessageBoxButton.YesNo);
            if (dialogResult == MessageBoxResult.Yes)
            {

                //podzielić dane wejściowe i wygenerowane na klasy i artybuty
                var readClass = new int[reading.Count];
                var readAttr_d = new double[reading.Count, reading.ElementAt(0).Length-1].ToJagged();
                var readAttr = new int[reading.Count, reading.ElementAt(0).Length - 1].ToJagged();

                var stringIntCheatSheet = new Dictionary<string, int>[reading.ElementAt(0).Length];
                for(int i = 0; i < stringIntCheatSheet.Length;i++)
                    stringIntCheatSheet[i] = new Dictionary<string, int>();

                for (int x = 0; x < reading.Count; x++) {
                    for (int y = 0; y < reading.ElementAt(0).Length; y++) {
                        int res = 0;
                        string ss = reading.ElementAt(x)[y];
                        if (!int.TryParse(ss, out res) || y == 0) {
                            if (!stringIntCheatSheet[y].ContainsKey(ss))
                                stringIntCheatSheet[y].Add(ss, stringIntCheatSheet[y].Count);
                            res = stringIntCheatSheet[y][ss];
                        }
                        if (y == 0) readClass[x] = res;
                        else
                        {
                            readAttr[x][y - 1] = res;
                            double rr = 0;
                            if (!double.TryParse(ss, System.Globalization.NumberStyles.AllowDecimalPoint,
                                    System.Globalization.NumberFormatInfo.InvariantInfo, out rr))
                                readAttr_d[x][y - 1] = rr;
                            else readAttr_d[x][y - 1] = res;
                        }
                    }
                }

                var genClass = new int[generating.Count];
                var genAttr = new int[generating.Count, generating.ElementAt(0).Length - 1].ToJagged();
                var genAttr_d = new double[generating.Count, generating.ElementAt(0).Length - 1].ToJagged();


                for (int x = 0; x < generating.Count; x++)
                {
                    for (int y = 0; y < generating.ElementAt(0).Length; y++)
                    {
                        int res = 0;
                        string ss = generating.ElementAt(x)[y];
                        if (!int.TryParse(ss, out res) || y == 0)
                        {
                            if (!stringIntCheatSheet[y].ContainsKey(ss))
                                stringIntCheatSheet[y].Add(ss, stringIntCheatSheet[y].Count);
                            res = stringIntCheatSheet[y][ss];
                        }
                        if (y == 0) genClass[x] = res;
                        else
                        {
                            genAttr[x][y - 1] = res;
                            double rr = 0;
                            if (double.TryParse(ss, System.Globalization.NumberStyles.AllowDecimalPoint,
                                    System.Globalization.NumberFormatInfo.InvariantInfo, out rr))
                                genAttr_d[x][y - 1] = rr;
                            else genAttr_d[x][y - 1] = res;
                        }
                    }
                }

               


                int correct=0, incorrect=0, correctknn = 0, incorrectknn = 0;

                

                
                var learn = new NaiveBayesLearning();
                NaiveBayes nb = learn.Learn(readAttr, readClass);
                var test = nb.Decide(genAttr);
                foreach (var v in test)
                {
                    if (v.Equals(genClass[test.IndexOf(v)]))
                        correct++;
                    else
                        incorrect++;
                }

                var learnKnn = new KNearestNeighbors(4);
                    
                var knn = learnKnn.Learn(readAttr_d, readClass);

                var testknn = knn.Decide(genAttr_d);
                foreach (var v in testknn)
                {
                    if (v.Equals(genClass[testknn.IndexOf(v)]))
                        correctknn++;
                    else
                        incorrectknn++;
                }
                
                System.Windows.MessageBox.Show("Naive Bayes Classification:\nGenerated data accuracy: " + 
                    100.0 * correct / (correct+incorrect) + "%\n"+
                   "K Nearest Neighbours Classification:\nGenerated data accuracy: " + 
                   100.0 * correctknn / (correctknn + incorrectknn)
                   + "%\n", "Data Testing - extending dataset",
                    System.Windows.MessageBoxButton.OK);

                
                /*
                using (var read = new System.IO.StreamReader(path))
                {


                    //List<string[]> reading = new List<string[]>();
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

            
                */

            }
            dialogResult = System.Windows.MessageBox.Show("Do you want to open the file with generated data?", "Data testing - extended data", System.Windows.MessageBoxButton.YesNo);
            if (dialogResult == MessageBoxResult.Yes)
            {
                System.Diagnostics.Process.Start(savefiledir);
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
