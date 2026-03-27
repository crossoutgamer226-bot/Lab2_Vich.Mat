using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace Lab2_Vich.Mat
{
    public partial class Form1 : Form
    {
        List<double> X = new List<double>();
        List<double> Y = new List<double>();

        public Form1()
        {
            InitializeComponent();
            FillDefaultPoints();
        }

        void FillDefaultPoints()
        {
            dataGridView1.Rows.Clear();
            dataGridView1.Columns.Clear();

            dataGridView1.Columns.Add("colX", "X");
            dataGridView1.Columns.Add("colY", "Y");

            double[] X0 = { 5, 7, 9, 11, 12 };
            double[] Y0 = { 3, -2, -2, 4, 15 };

            for (int i = 0; i < X0.Length; i++)
                dataGridView1.Rows.Add(X0[i], Y0[i]);
        }

        void ReadTable()
        {
            X.Clear();
            Y.Clear();

            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.Cells[0].Value == null || row.Cells[1].Value == null)
                    continue;

                X.Add(Convert.ToDouble(row.Cells[0].Value));
                Y.Add(Convert.ToDouble(row.Cells[1].Value));
            }
        }

        double[] InterpolationPolynomial()
        {
            int n = X.Count;

            float[,] A = new float[n, n];
            float[] b = new float[n];

            for (int i = 0; i < n; i++)
            {
                float val = 1f;
                for (int j = 0; j < n; j++)
                {
                    A[i, j] = val;
                    val *= (float)X[i];
                }
                b[i] = (float)Y[i];
            }

            float[] res = GaussMethod.SelectByLine(A, b);

            double[] result = new double[n];
            for (int i = 0; i < n; i++)
                result[i] = res[i];

            return result;
        }

        double PolyEval(double[] a, double x)
        {
            double s = 0;
            double p = 1;
            for (int i = 0; i < a.Length; i++)
            {
                s += a[i] * p;
                p *= x;
            }
            return s;
        }

        double Lagrange(double x)
        {
            double sum = 0;
            int n = X.Count;

            for (int k = 0; k < n; k++)
            {
                double lk = 1;
                for (int j = 0; j < n; j++)
                    if (j != k)
                        lk *= (x - X[j]) / (X[k] - X[j]);

                sum += Y[k] * lk;
            }
            return sum;
        }

        double[] NewtonCoeffs()
        {
            int n = X.Count;
            double[,] dd = new double[n, n];
            double[] a = new double[n];

            for (int i = 0; i < n; i++)
                dd[i, 0] = Y[i];

            for (int j = 1; j < n; j++)
                for (int i = 0; i < n - j; i++)
                    dd[i, j] = (dd[i + 1, j - 1] - dd[i, j - 1]) / (X[i + j] - X[i]);

            for (int i = 0; i < n; i++)
                a[i] = dd[0, i];

            return a;
        }

        double NewtonEval(double x, double[] a)
        {
            double result = a[0];
            double prod = 1;

            for (int i = 1; i < a.Length; i++)
            {
                prod *= (x - X[i - 1]);
                result += a[i] * prod;
            }
            return result;
        }

        void Plot()
        {
            ReadTable();
            chart1.Series.Clear();

            
            var sPoints = chart1.Series.Add("Точки");
            sPoints.ChartType = SeriesChartType.Point;
            sPoints.MarkerSize = 10;
            sPoints.Color = System.Drawing.Color.Black;

            for (int i = 0; i < X.Count; i++)
                sPoints.Points.AddXY(X[i], Y[i]);

            double xmin = X.Min();
            double xmax = X.Max();
            List<double> grid = Enumerable.Range(0, 300)
                .Select(i => xmin + i * (xmax - xmin) / 299.0).ToList();

            
            var aInterp = InterpolationPolynomial();
            var sInterp = chart1.Series.Add("Интерполяционный многочлен");
            sInterp.ChartType = SeriesChartType.Line;
            sInterp.Color = System.Drawing.Color.LightGreen;
            sInterp.BorderWidth = 7;

            foreach (double x in grid)
                sInterp.Points.AddXY(x, PolyEval(aInterp, x));

            
            var sLag = chart1.Series.Add("Лагранж");
            sLag.ChartType = SeriesChartType.Line;
            sLag.Color = System.Drawing.Color.Yellow;
            sLag.BorderWidth = 3;
            sLag.BorderDashStyle = ChartDashStyle.Dash;

            foreach (double x in grid)
                sLag.Points.AddXY(x, Lagrange(x));


            var aNewton = NewtonCoeffs();
            var sNewt = chart1.Series.Add("Ньютон");
            sNewt.ChartType = SeriesChartType.Line;
            sNewt.Color = System.Drawing.Color.Red;
            sNewt.BorderWidth = 1;
            sNewt.BorderDashStyle = ChartDashStyle.Dash;

            foreach (double x in grid)
                sNewt.Points.AddXY(x, NewtonEval(x, aNewton));
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Plot();
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e) { }
        private void chart1_Click(object sender, EventArgs e) { }
    }

    class GaussMethod
    {
        public static float[] SelectByLine(float[,] A, float[] b)
        {
            int n = b.Length;
            float[,] a = (float[,])A.Clone();
            float[] bb = (float[])b.Clone();
            int[] colIndex = new int[n];

            for (int i = 0; i < n; i++)
                colIndex[i] = i;

            for (int k = 0; k < n - 1; k++)
            {
                int maxCol = k;
                float maxVal = Math.Abs(a[k, k]);

                for (int j = k + 1; j < n; j++)
                    if (Math.Abs(a[k, j]) > maxVal)
                    {
                        maxVal = Math.Abs(a[k, j]);
                        maxCol = j;
                    }

                if (maxCol != k)
                {
                    for (int i = 0; i < n; i++)
                    {
                        float temp = a[i, k];
                        a[i, k] = a[i, maxCol];
                        a[i, maxCol] = temp;
                    }

                    int tmpIdx = colIndex[k];
                    colIndex[k] = colIndex[maxCol];
                    colIndex[maxCol] = tmpIdx;
                }

                for (int m = k + 1; m < n; m++)
                {
                    float factor = a[m, k] / a[k, k];
                    for (int l = k; l < n; l++)
                        a[m, l] -= factor * a[k, l];
                    bb[m] -= factor * bb[k];
                }
            }

            float[] xTemp = new float[n];
            for (int i = n - 1; i >= 0; i--)
            {
                float sum = 0;
                for (int j = i + 1; j < n; j++)
                    sum += a[i, j] * xTemp[j];
                xTemp[i] = (bb[i] - sum) / a[i, i];
            }

            float[] x = new float[n];
            for (int i = 0; i < n; i++)
                x[colIndex[i]] = xTemp[i];

            return x;
        }
    }
}
