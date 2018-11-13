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
using System.Windows.Shapes;
using System.Windows.Forms;
using Accord.MachineLearning;
using Accord.MachineLearning.Bayes;
using System.Text.RegularExpressions;
using Accord.Math;
using Accord.Math.Optimization.Losses;
using Accord.MachineLearning.VectorMachines;
using Accord.MachineLearning.VectorMachines.Learning;
using Accord.Statistics.Kernels;
using Accord.MachineLearning.Performance;

namespace WpfApplication3
{
    /// <summary>
    /// Interaction logic for DataExtendWindow.xaml
    /// </summary>
    public partial class DataExtendWindow : Window
    {
        public Random rnd;
        public List<string[]> reading;
        List<int> clsColBoxSrc;
        public string[] attrType, attribTypes = { "integer", "double", "string" }, 
            precision = { "1", "2", "3", "4", "5", "6 ", "7", "8", "9", "10" }, 
            newDataAmt = { "100", "250", "500", "1000", "2000"};
        string fltPrec;
        int newData;
        int clsCol;
        int classes, attribs;
        double scoreH;
        Dictionary<string, Tuple<int, Dictionary<int, Dictionary<string, int>>>> values;
        public DataExtendWindow()
        {
            rnd = new Random();
            clsColBoxSrc = new List<int>();
            //ClsColBox.ItemsSource = clsColBoxSrc;
            reading = new List<string[]>();
            attrType = new string[0];
            classes = 0; attribs = 0;
            InitializeComponent();
            DataAmtBox.ItemsSource = newDataAmt;
            DataAmtBox.SelectedIndex = 0;
            FltPrecBox.ItemsSource = precision;
            FltPrecBox.SelectedIndex = 0;
            ScoreValue.Text = "100";
            scoreH = 100;
        }

        private void PathBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog k = new OpenFileDialog();
            if (k.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                InputDataGrid.ItemsSource = null;
                ClsColBox.ItemsSource = null;
                reading.Clear();
                clsCol = 0;
                var path = k.FileName;
                FilePath.Text = path;
                attrType = new string[0];
                using (var read = new System.IO.StreamReader(path))
                {
                    string s;
                    int tempI;
                    double tempD;
                    while (!string.IsNullOrEmpty(s = read.ReadLine()))
                    {
                        string[] temp = s.Split(',');
                        reading.Add(temp);
                        if (attrType.Length == 0){
                            attrType = new string[temp.Length];
                            for (int i = 0; i < attrType.Length; i++)
                                attrType[i] = "";
                        }
                        foreach (string t in temp)
                        { //myk na sprawdzanie typu każdego z atrybutów
                            if (!attrType[temp.IndexOf(t)].Equals("string")) {
                                if (int.TryParse(t, out tempI))
                                    attrType[temp.IndexOf(t)] = "integer";
                                else if (double.TryParse(t,
                                    System.Globalization.NumberStyles.AllowDecimalPoint,
                                    System.Globalization.NumberFormatInfo.InvariantInfo,
                                    out tempD))
                                    attrType[temp.IndexOf(t)] = "double";
                                else
                                {
                                    attrType[temp.IndexOf(t)] = "string";
                                }
                            }
                        }
                    }
                }
            }
            clsColBoxSrc.Clear();
            for (int i = 0; i < attrType.Length; i++)
                clsColBoxSrc.Add(i);
            ClsColBox.ItemsSource = clsColBoxSrc;
            ClsColBox.SelectedIndex = 0;
            InputDataGrid.Columns.Clear();
            
            var d = new List<Dictionary<string, string>>();
            foreach (var v in reading) {
                var d2 = new Dictionary<string, string>();
                for (int i = 0; i < v.Length; i++)
                    d2.Add(String.Format("Column{0}", i), v.ElementAt(i));
                d.Add(d2);
            }

            for (int i = 0; i < reading.ElementAt(0).Length; i++)
            {
                string propertyName = String.Format("Column{0}", i);
                var column = new DataGridTextColumn();
                column.Binding = new System.Windows.Data.Binding() { Converter = new MyConverter(), ConverterParameter = propertyName };
                column.Header = propertyName;
                InputDataGrid.Columns.Add(column);
            }

            InputDataGrid.ItemsSource = d;
            
        }

        public class MyConverter : IValueConverter //isnieje tylko po to żeby wyświetlać dane w okienku
        {
            #region IValueConverter Members 
            public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                return ((Dictionary<string, string>)value)[(string)parameter];
            }
            public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                throw new NotImplementedException();
            }
            #endregion
        }

        private void ClsColBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            if (ClsColBox.SelectedIndex >= 0)
            {
                if (ClsColBox.SelectedIndex != clsCol)
                {
                    foreach (var s in reading)
                    {
                        swap(ref s[0], ref s[clsCol]);
                        swap(ref s[0], ref s[ClsColBox.SelectedIndex]);
                    }
                    swap(ref attrType[0], ref attrType[clsCol]);
                    swap(ref attrType[0], ref attrType[ClsColBox.SelectedIndex]);
                }
                CountAppearences();
            }
            clsCol = ClsColBox.SelectedIndex;
        }

        private void CountAppearences() { //zlicza wystąpienia
            classes = 0; attribs = 0;
            values = new Dictionary<string, Tuple<int, Dictionary<int, Dictionary<string, int>>>>();
            foreach (string[] res in reading) { 
                if (!values.ContainsKey(res[0]))
                    values.Add(res[0], new Tuple<int, Dictionary<int, Dictionary<string, int>>>(0,
                        new Dictionary<int, Dictionary<string, int>>()));
                //dodanie grupy jeśli nie istnieje, zaczęcie zliczania osobników
                int i = 0;
                foreach (string at in res)
                {
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
                    values[res[0]].Item1 + 1, values[res[0]].Item2); //przypisywanie nowej wartości do tupla jest do dupy
                                                                     //prawdopodobnie zmienię później jak mi się będzie chciało
            }
            classes = values.Count;
            attribs = values.ElementAt(0).Value.Item2.Count;
        }

        private void OpenBtn_Click(object sender, RoutedEventArgs e)
        {
            if (FilePath.Text.IsEqual(""))
            {
                var dialogResult = System.Windows.MessageBox.Show(
                    "Please select a dataset to extend", 
                    "Data testing - extended data", System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }
            else
                GenerateBasedOnData();
        }

        public class Spline3Deg
        { //klasa funkcji sklejanej 3. stopnia, służącą za ciągłą funkcję prawdopodobieństwa
            private int n;
            private double[] a, b, c, d, x;

            public Spline3Deg(double[] x, double[] y)
            {
                n = x.Length - 1;
                Array.Copy(x, this.x = new double[n + 1], n + 1);
                Array.Copy(y, this.a = new double[n + 1], n + 1);
                b = new double[n];
                c = new double[n + 1];
                d = new double[n];

                double[] h = new double[n];
                for (int i = 0; i < n; i++)
                    h[i] = x[i + 1] - x[i];
                double[] alpha = new double[n - 1];
                for (int i = 1; i < n; i++)
                    alpha[i - 1] = (3 * (a[i + 1] - a[i]) / h[i] - 3 * (a[i] - a[i - 1]) / h[i - 1]);

                double[] l = new double[n + 1],
                         mu = new double[n + 1],
                         z = new double[n + 1];
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
            public double y(double x0)
            {
                for (int i = 0; i < n; i++)
                    if (x0 >= x[i] && x0 <= x[i + 1])
                        return Math.Pow(x0 - x[i], 3) * d[i] + Math.Pow(x0 - x[i], 2) * c[i] + (x0 - x[i]) * b[i] + a[i];
                return Double.NaN;
            }
            public Tuple<double, double> Limits() { return new Tuple<double, double>(x[0], x[n]); }
        }

        private string[] RemoveAt(string[] source, int index)
        {
            string[] dest = new string[source.Length - 1];
            if (index > 0)
                Array.Copy(source, 0, dest, 0, index);

            if (index < source.Length - 1)
                Array.Copy(source, index + 1, dest, index, source.Length - index - 1);

            return dest;
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ScoreValue_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateNumberText(ScoreValue);
            if (int.Parse(ScoreValue.Text) > 100)
                ScoreValue.Text = "100";
            if (int.Parse(ScoreValue.Text) < 0)
                ScoreValue.Text = "0";
            scoreH = int.Parse(ScoreValue.Text);
        }

        private void ValidateNumberText(System.Windows.Controls.TextBox txt)
        {
            txt.Text = Regex.Replace(txt.Text, @"[^\d-]", string.Empty);
            txt.SelectionStart = txt.Text.Length; // add some logic if length is 0
            txt.SelectionLength = 0;
        }

        private void FltPrecBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            fltPrec = "0.";
            for (int i = 0; i <= FltPrecBox.SelectedIndex; i++)
                fltPrec += "0";

        }

        private void DataAmtBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            newData = int.Parse(newDataAmt[DataAmtBox.SelectedIndex]);
        }

        private void swap(ref string a, ref string b)
        {
            string temp = a;
            a = b;
            b = temp;
        }

        private void GenerateBasedOnData()
        {
            List<string[]> generating = new List<string[]>(); // do ewentualnego sprawdzania

            var attrType = RemoveAt(this.attrType, 0);

            //tutaj dorzucam tworzenie wykresu ciągłego prawdopodobieństwa
            Spline3Deg[,] probabilities = new Spline3Deg[classes, attribs];
            for (int i = 0; i < attribs; i++)
                if (attrType[i].Equals("double") || attrType[i].Equals("integer"))
                    for (int j = 0; j < classes; j++)
                    {
                        int c = values.ElementAt(j).Value.Item2.ElementAt(i).Value.Count;
                        double[] y, x = new double[c];
                        SortedList<double, int> temp = new SortedList<double, int>();
                        foreach (var v in values.ElementAt(j).Value.Item2.ElementAt(i).Value)
                        {
                            int tI = v.Value; double tD = Double.Parse(v.Key.Replace(" ", string.Empty),
                                System.Globalization.NumberStyles.AllowDecimalPoint,
                                System.Globalization.NumberFormatInfo.InvariantInfo);
                            temp.Add(tD, tI);
                        }
                        y = temp.Keys.ToArray();
                        x[0] = 0;
                        for (int k = 1; k < temp.Count; k++)
                            x[k] = x[k - 1] + temp.ElementAt(k - 1).Value + temp.ElementAt(k).Value;
                        probabilities[j, i] = new Spline3Deg(x, y);
                    }


            //do sprawdzania punktacji później
            //podzielić dane wejściowe i wygenerowane na klasy i artybuty
            var readClass = new int[reading.Count];
            var readAttr_d = new double[reading.Count, reading.ElementAt(0).Length - 1].ToJagged();

            var stringIntCheatSheet = new Dictionary<string, int>[reading.ElementAt(0).Length];
            for (int i = 0; i < stringIntCheatSheet.Length; i++)
                stringIntCheatSheet[i] = new Dictionary<string, int>();

            for (int x = 0; x < reading.Count; x++)
            {
                for (int y = 0; y < reading.ElementAt(0).Length; y++)
                {
                    double rr = 0;
                    string ss = reading.ElementAt(x)[y];
                    if (!double.TryParse(ss, System.Globalization.NumberStyles.AllowDecimalPoint,
                                System.Globalization.NumberFormatInfo.InvariantInfo, out rr)
                            || y == 0)
                        {
                            if (!stringIntCheatSheet[y].ContainsKey(ss))
                                stringIntCheatSheet[y].Add(ss, stringIntCheatSheet[y].Count);
                            rr = stringIntCheatSheet[y][ss];
                        }
                        if (y == 0) readClass[x] = (int)rr;
                        else
                            readAttr_d[x][y - 1] = rr;
                    }
                }

            var learnKnn = new KNearestNeighbors(4);

            var knn = learnKnn.Learn(readAttr_d, readClass);

            double[] attrcr = new double[attribs];


            string[] bestattr = new string[attribs];
            double bestscore;

            //czas generować ten szajs
            var newStuff = new string[newData, attribs + 1];
            for (int it = 0; it < newStuff.GetLength(0); it++)
            {
                bestscore = 0;
                
                int cl = rnd.Next(classes); //rnd to zadelkarowany wcześniej Random //losowanie klasy
                newStuff[it, 0] = values.ElementAt(cl).Key;
                int safety = 0;
                do
                {
                    for (int v = 1; v <= attribs; v++)
                    {  //losowanie wartości atrybutu
                        if (attrType[v - 1].Equals("string"))
                        {  //funkcja dyskretna
                            int val = rnd.Next(values.ElementAt(cl).Value.Item1);
                            int b = 0;
                            foreach (var a in values.ElementAt(cl).Value.Item2[v])
                            {
                                if (val < (b += a.Value))
                                {
                                    newStuff[it, v] = a.Key; //na Monte Carlo
                                    break;
                                }
                            }
                        }
                        else
                        {  //funkcja ciągła
                            Tuple<double, double> extr = probabilities[cl, v - 1].Limits();
                            double val = rnd.Next((int)extr.Item1, (int)extr.Item2) + rnd.NextDouble();
                            double r = probabilities[cl, v - 1].y(val);
                            if (attrType[v - 1].Equals("double"))
                                newStuff[it, v] = r.ToString(fltPrec, System.Globalization.CultureInfo.InvariantCulture);
                            else //if (attrType[v - 1].Equals("integer"))
                                newStuff[it, v] = Math.Round(r).ToString();
                        }//koniec losowania wartości atrybutu
                        ///ekstra warunek bezpieczeństwa, bo czasami trafiają się NULLe
                        if (string.IsNullOrEmpty(newStuff[it, v])) {
                            v--;
                            continue; //jeśli atrybut ma nulla, powtórz pętlę
                        }
                        ///koniec ekstra warunku bespieczeństwa
                    }//koniec generowania obiektu


                    //do tabliczki do sprawdzenia punktacji
                    for (int v = 1; v <= attribs; v++) {
                        double rr = 0;
                        string ss = newStuff[it, v];
                        if (!double.TryParse(ss, System.Globalization.NumberStyles.AllowDecimalPoint,
                                System.Globalization.NumberFormatInfo.InvariantInfo, out rr)) {
                            if (!stringIntCheatSheet[v].ContainsKey(ss))
                                stringIntCheatSheet[v].Add(ss, stringIntCheatSheet[v].Count);
                            rr = stringIntCheatSheet[v][ss];
                        }
                        attrcr[v - 1] = rr;
                    }
                    if (knn.Score(attrcr, cl) > bestscore) {
                        for (int iter = 0; iter < attribs; iter++)
                            bestattr[iter] = newStuff[it, iter+1];
                    }

                } while (knn.Score(attrcr, cl) < scoreH /100 && ++safety < 1000);

                for (int iter = 0; iter < attribs; iter++)
                    newStuff[it, iter + 1] = bestattr[iter];
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
                        for (int x = 0; x < newStuff.GetLength(0); x++)
                        {
                            string line = "";
                            for (int y = 0; y < newStuff.GetLength(1); y++)
                            {
                                line += newStuff[x, y] + ',';
                            }
                            line = line.Remove(line.Length - 1);
                            string[] temp = line.Split(',');
                            generating.Add(line.Split(','));
                            swap(ref temp[0], ref temp[clsCol]);
                            line = "";
                            for (int y = 0; y < temp.Length; y++)
                            {
                                line += temp[y] + ',';
                            }
                            line = line.Remove(line.Length - 1);
                            write.WriteLine(line);
                        }
                    }
                }
                else return;
            }
            //tu dać walidację wygenerowanych danych

            var dialogResult = System.Windows.MessageBox.Show("Do you want to test the generated data?", "Data testing - extended data", System.Windows.MessageBoxButton.YesNo);
            if (dialogResult == MessageBoxResult.Yes)
            {

                var genClass = new int[generating.Count];
                //var genAttr = new int[generating.Count, generating.ElementAt(0).Length - 1].ToJagged();
                var genAttr_d = new double[generating.Count, generating.ElementAt(0).Length - 1].ToJagged();


                for (int x = 0; x < generating.Count; x++)
                {
                    for (int y = 0; y < generating.ElementAt(0).Length; y++)
                    {
                        double rr = 0;
                        string ss = generating.ElementAt(x)[y];
                        if (!double.TryParse(ss, System.Globalization.NumberStyles.AllowDecimalPoint,
                                    System.Globalization.NumberFormatInfo.InvariantInfo, out rr) || y == 0)
                        {
                            if (!stringIntCheatSheet[y].ContainsKey(ss))
                                stringIntCheatSheet[y].Add(ss, stringIntCheatSheet[y].Count);
                            rr = stringIntCheatSheet[y][ss];
                        }
                        if (y == 0) genClass[x] = (int)rr;
                        else genAttr_d[x][y - 1] = rr;

                    }
                }

                //przerobienie na tablicę intów, z przesunięciem dobli o precyzję
                var genAttr_i = new int[generating.Count, generating.ElementAt(0).Length - 1].ToJagged();
                var readAttr_i = new int[reading.Count, reading.ElementAt(0).Length - 1].ToJagged();

                int shift = (int)Math.Pow(10, FltPrecBox.SelectedIndex + 1);
                for (int x = 0; x < generating.Count; x++)
                {
                    for (int y = 0; y < generating.ElementAt(0).Length - 1; y++)
                    {
                        if (attrType[y].Equals("double"))
                            genAttr_i[x][y] = (int)(genAttr_d[x][y] * shift);
                        else
                            genAttr_i[x][y] = (int)genAttr_d[x][y];
                    }
                }
                for (int x = 0; x < reading.Count; x++)
                {
                    for (int y = 0; y < reading.ElementAt(0).Length - 1; y++)
                    {
                        if (attrType[y].Equals("double"))
                            readAttr_i[x][y] = (int)(readAttr_d[x][y] * shift);
                        else
                            readAttr_i[x][y] = (int)readAttr_d[x][y];
                    }
                }


                int correctnb = 0, incorrectnb = 0, correctknn = 0, incorrectknn = 0, correctsvm = 0, incorrectsvm = 0;

                var learn = new NaiveBayesLearning();
                NaiveBayes nb = learn.Learn(readAttr_i, readClass);
                var test = nb.Decide(genAttr_i);
                foreach (var v in test)
                {
                    if (v.Equals(genClass[test.IndexOf(v)]))
                        correctnb++;
                    else
                        incorrectnb++;
                }

                /////////////////////////////////////////////////////////////////////////

                var testknn = knn.Decide(genAttr_d);
                for (int i = 0; i < testknn.Length; i++)
                //foreach (var v in testknn)
                {
                    if (testknn[i].Equals(genClass[i]))
                        correctknn++;
                    else
                        incorrectknn++;
                }
                /////////////////////////////////////////////////////////////////////////


                var teach = new MulticlassSupportVectorLearning<Gaussian>()
                {
                    // Configure the learning algorithm to use SMO to train the
                    //  underlying SVMs in each of the binary class subproblems.
                    Learner = (param) => new SequentialMinimalOptimization<Gaussian>()
                    {
                        // Estimate a suitable guess for the Gaussian kernel's parameters.
                        // This estimate can serve as a starting point for a grid search.
                        UseKernelEstimation = true
                    }
                };
                var svm = teach.Learn(readAttr_d, readClass);
                
                var testsvm = svm.Decide(genAttr_d);
                for (int i = 0; i < testsvm.Length; i++)
                //foreach (var v in testknn)
                {
                    if (testsvm[i].Equals(genClass[i]))
                        correctsvm++;
                    else
                        incorrectsvm++;
                }
                ////////////////////////////////////////////////////////////

                double[][] mixAttr_d = new double[genAttr_d.GetLength(0) + readAttr_d.GetLength(0),
                   genAttr_d[0].Length].ToJagged();
                int[] mixClass = new int[genClass.Length + readClass.Length];

                Array.Copy(readClass, mixClass, readClass.Length);
                Array.Copy(genClass, 0, mixClass, readClass.Length, genClass.Length);

                Array.Copy(readAttr_d, mixAttr_d, readAttr_d.Length);
                Array.Copy(genAttr_d, 0, mixAttr_d, readAttr_d.Length, genAttr_d.Length);

                int[][] mixAttr_i = new int[genAttr_i.GetLength(0) + readAttr_i.GetLength(0),
                    genAttr_i[0].Length].ToJagged();

                Array.Copy(readAttr_i, mixAttr_i, readAttr_i.Length);
                Array.Copy(genAttr_i, 0, mixAttr_i, readAttr_i.Length, genAttr_i.Length);

                //KROSWALIDACJAAAAAAAAAAAAAAAAAA
                //KNN

                var crossvalidationRead = CrossValidation.Create(
                            k: 4,
                            learner: (p) => new KNearestNeighbors(k: 4),
                            loss: (actual, expected, p) => new ZeroOneLoss(expected).Loss(actual),
                            fit: (teacher, x, y, w) => teacher.Learn(x, y, w),
                            x: readAttr_d, y: readClass
                            );
                var resultRead = crossvalidationRead.Learn(readAttr_d, readClass);
                // We can grab some information about the problem:
                var numberOfSamplesRead = resultRead.NumberOfSamples;
                var numberOfInputsRead = resultRead.NumberOfInputs;
                var numberOfOutputsRead = resultRead.NumberOfOutputs;

                var trainingErrorRead = resultRead.Training.Mean;
                var validationErrorRead = resultRead.Validation.Mean;

                var readCM = resultRead.ToConfusionMatrix(readAttr_d, readClass);
                double readAccuracy = readCM.Accuracy;
                //////////////////////////////////////////////////////////
                var crossvalidationGen = CrossValidation.Create(
                            k: 4,
                            learner: (p) => new KNearestNeighbors(k: 4),
                            loss: (actual, expected, p) => new ZeroOneLoss(expected).Loss(actual),
                            fit: (teacher, x, y, w) => teacher.Learn(x, y, w),
                            x: genAttr_d, y: genClass
                            );
                var resultGen = crossvalidationGen.Learn(genAttr_d, genClass);
                // We can grab some information about the problem:
                var numberOfSamplesGen = resultGen.NumberOfSamples;
                var numberOfInputsGen = resultGen.NumberOfInputs;
                var numberOfOutputsGen = resultGen.NumberOfOutputs;

                var trainingErrorGen = resultGen.Training.Mean;
                var validationErrorGen = resultGen.Validation.Mean;
                var genCM = resultGen.ToConfusionMatrix(genAttr_d, genClass);
                double genAccuracy = genCM.Accuracy;
                //////////////////////////////////////////////////////////

                var crossvalidationMix = CrossValidation.Create(
                            k: 4,
                            learner: (p) => new KNearestNeighbors(k: 4),
                            loss: (actual, expected, p) => new ZeroOneLoss(expected).Loss(actual),
                            fit: (teacher, x, y, w) => teacher.Learn(x, y, w),
                            x: mixAttr_d, y: mixClass
                            );
                var resultMix = crossvalidationMix.Learn(readAttr_d, readClass);
                // We can grab some information about the problem:
                var numberOfSamplesMix = resultMix.NumberOfSamples;
                var numberOfInputsMix = resultMix.NumberOfInputs;
                var numberOfOutputsMix = resultMix.NumberOfOutputs;

                var trainingErrorMix = resultMix.Training.Mean;
                var validationErrorMix = resultMix.Validation.Mean;

                var mixCM = resultMix.ToConfusionMatrix(mixAttr_d, mixClass);
                double mixAccuracy = mixCM.Accuracy;

                //NB
                var crossvalidationReadnb = CrossValidation.Create(
                            k: 4,
                            learner: (p) => new NaiveBayesLearning(),
                            loss: (actual, expected, p) => new ZeroOneLoss(expected).Loss(actual),
                            fit: (teacher, x, y, w) => teacher.Learn(x, y, w),
                            x: readAttr_i, y: readClass
                            );
                var resultReadnb = crossvalidationReadnb.Learn(readAttr_i, readClass);
                // We can grab some information about the problem:
                var numberOfSamplesReadnb = resultReadnb.NumberOfSamples;
                var numberOfInputsReadnb = resultReadnb.NumberOfInputs;
                var numberOfOutputsReadnb = resultReadnb.NumberOfOutputs;

                var trainingErrorReadnb = resultReadnb.Training.Mean;
                var validationErrorReadnb = resultReadnb.Validation.Mean;

                var readCMnb = resultReadnb.ToConfusionMatrix(readAttr_i, readClass);
                double readAccuracynb = readCMnb.Accuracy;
                //////////////////////////////////////////////////////////
                var crossvalidationGennb = CrossValidation.Create(
                            k: 4,
                            learner: (p) => new NaiveBayesLearning(),
                            loss: (actual, expected, p) => new ZeroOneLoss(expected).Loss(actual),
                            fit: (teacher, x, y, w) => teacher.Learn(x, y, w),
                            x: genAttr_i, y: genClass
                            );
                var resultGennb = crossvalidationGennb.Learn(genAttr_i, genClass);
                // We can grab some information about the problem:
                var numberOfSamplesGennb = resultGennb.NumberOfSamples;
                var numberOfInputsGennb = resultGennb.NumberOfInputs;
                var numberOfOutputsGennb = resultGennb.NumberOfOutputs;

                var trainingErrorGennb = resultGennb.Training.Mean;
                var validationErrorGennb = resultGennb.Validation.Mean;
                var genCMnb = resultGennb.ToConfusionMatrix(genAttr_i, genClass);
                double genAccuracynb = genCMnb.Accuracy;
                //////////////////////////////////////////////////////////

                var crossvalidationMixnb = CrossValidation.Create(
                            k: 4,
                            learner: (p) => new NaiveBayesLearning(),
                            loss: (actual, expected, p) => new ZeroOneLoss(expected).Loss(actual),
                            fit: (teacher, x, y, w) => teacher.Learn(x, y, w),
                            x: mixAttr_i, y: mixClass
                            );
                var resultMixnb = crossvalidationMixnb.Learn(mixAttr_i, mixClass);
                // We can grab some information about the problem:
                var numberOfSamplesMixnb = resultMixnb.NumberOfSamples;
                var numberOfInputsMixnb = resultMixnb.NumberOfInputs;
                var numberOfOutputsMixnb = resultMixnb.NumberOfOutputs;

                var trainingErrorMixnb = resultMixnb.Training.Mean;
                var validationErrorMixnb = resultMixnb.Validation.Mean;

                var mixCMnb = resultMixnb.ToConfusionMatrix(mixAttr_i, mixClass);
                double mixAccuracynb = mixCMnb.Accuracy;

                //SVM
                
                
                var crossvalidationReadsvm = CrossValidation.Create(
                            k: 4,
                            learner: (p) => new MulticlassSupportVectorLearning<Gaussian>()
                            { Learner = (param) => new SequentialMinimalOptimization<Gaussian>()
                            { UseKernelEstimation = true  }
                            },
                            loss: (actual, expected, p) => new ZeroOneLoss(expected).Loss(actual),
                            fit: (teacher, x, y, w) => teacher.Learn(x, y, w),
                            x: readAttr_d, y: readClass
                            );
                //crossvalidationReadsvm.ParallelOptions.MaxDegreeOfParallelism = 1;
                var resultReadsvm = crossvalidationReadsvm.Learn(readAttr_d, readClass);
                // We can grab some information about the problem:
                var numberOfSamplesReadsvm = resultReadsvm.NumberOfSamples;
                var numberOfInputsReadsvm = resultReadsvm.NumberOfInputs;
                var numberOfOutputsReadsvm = resultReadsvm.NumberOfOutputs;

                var trainingErrorReadsvm = resultReadsvm.Training.Mean;
                var validationErrorReadsvm = resultReadsvm.Validation.Mean;

                var readCMsvm = resultReadsvm.ToConfusionMatrix(readAttr_d, readClass);
                double readAccuracysvm = readCMsvm.Accuracy;
                
                //////////////////////////////////////////////////////////
                var crossvalidationGensvm = CrossValidation.Create(
                            k: 4,
                            learner: (p) => new MulticlassSupportVectorLearning<Gaussian>()
                            {
                                Learner = (param) => new SequentialMinimalOptimization<Gaussian>()
                                { UseKernelEstimation = true }
                            },
                            loss: (actual, expected, p) => new ZeroOneLoss(expected).Loss(actual),
                            fit: (teacher, x, y, w) => teacher.Learn(x, y, w),
                            x: genAttr_d, y: genClass
                            );
                var resultGensvm = crossvalidationGensvm.Learn(genAttr_d, genClass);
                // We can grab some information about the problem:
                var numberOfSamplesGensvm = resultGensvm.NumberOfSamples;
                var numberOfInputsGensvm = resultGensvm.NumberOfInputs;
                var numberOfOutputsGensvm = resultGensvm.NumberOfOutputs;

                var trainingErrorGensvm = resultGensvm.Training.Mean;
                var validationErrorGensvm = resultGensvm.Validation.Mean;
                var genCMsvm = resultGensvm.ToConfusionMatrix(genAttr_d, genClass);
                double genAccuracysvm = genCMsvm.Accuracy;
                //////////////////////////////////////////////////////////

                var crossvalidationMixsvm = CrossValidation.Create(
                            k: 4,
                            learner: (p) => new MulticlassSupportVectorLearning<Gaussian>()
                            {
                                Learner = (param) => new SequentialMinimalOptimization<Gaussian>()
                                { UseKernelEstimation = true }
                            },
                            loss: (actual, expected, p) => new ZeroOneLoss(expected).Loss(actual),
                            fit: (teacher, x, y, w) => teacher.Learn(x, y, w),
                            x: mixAttr_d, y: mixClass
                            );
                var resultMixsvm = crossvalidationMixsvm.Learn(mixAttr_d, mixClass);
                // We can grab some information about the problem:
                var numberOfSamplesMixsvm = resultMixsvm.NumberOfSamples;
                var numberOfInputsMixsvm = resultMixsvm.NumberOfInputs;
                var numberOfOutputsMixsvm = resultMixsvm.NumberOfOutputs;

                var trainingErrorMixsvm = resultMixsvm.Training.Mean;
                var validationErrorMixsvm = resultMixsvm.Validation.Mean;

                var mixCMsvm = resultMixsvm.ToConfusionMatrix(mixAttr_d, mixClass);
                double mixAccuracysvm = mixCMsvm.Accuracy;
                
                /////////////////////////////////////////////////

                System.Windows.MessageBox.Show(
                   "K Nearest Neighbours Classification:\nGenerated Data Correct Ratio: " +
                   100.0 * correctknn / (correctknn + incorrectknn) + "%\n" +
                   "Original Data X-Validation Accuracy: "
                   + (100.0 * readAccuracy).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
                   + "%\n" + "Generated Data X-Validation Accuracy: "
                   + (100.0 * genAccuracy).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
                   + "%\n" + "Mixed Data X-Validation Accuracy: "
                   + (100.0 * mixAccuracy).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
                   + "%\n"
                   + "\n\n" + "Naive Bayes Classification:\nGenerated Data Correct Ratio: " +
                   100.0 * correctnb / (correctnb + incorrectnb) + "%\n" +
                   "Original Data X-Validation Accuracy: "
                   + (100.0 * readAccuracynb).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
                   + "%\n" + "Generated Data X-Validation Accuracy: "
                   + (100.0 * genAccuracynb).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
                   + "%\n" + "Mixed Data X-Validation Accuracy: "
                   + (100.0 * mixAccuracynb).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
                   + "%\n" +
                   "\n\n" + "Support Vector Machine Classification:\nGenerated Data Correct Ratio: " +
                   100.0 * correctsvm / (correctsvm + incorrectsvm) + "%\n" +
                   "Original Data X-Validation Accuracy: "
                   + (100.0 * readAccuracysvm).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
                   + "%\n" + "Generated Data X-Validation Accuracy: "
                   + (100.0 * genAccuracysvm).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
                   + "%\n" + "Mixed Data X-Validation Accuracy: "
                   + (100.0 * mixAccuracysvm).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
                   + "%\n",
                   "Data Testing - extending dataset",
                    System.Windows.MessageBoxButton.OK);


                
            }
            dialogResult = System.Windows.MessageBox.Show("Do you want to open the file with generated data?", "Data testing - extended data", System.Windows.MessageBoxButton.YesNo);
            if (dialogResult == MessageBoxResult.Yes)
            {
                System.Diagnostics.Process.Start(savefiledir);
            }
        }

    }
}
