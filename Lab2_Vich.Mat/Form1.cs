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

        ManualSpline manualSpline;

        public Form1()
        {
            InitializeComponent();
            FillDefaultPoints();
            InitManualSpline(); 
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
        void InitManualSpline()
        {
            double[] xNodes = { 5, 7, 9, 11, 12 };

            // коэффициенты из СЛАУ
            double[] a = { 3, -2, -2, 4 };
            double[] b = { -3, -1, 0, 8 };
            double[] c = { 0, 0, -0, 4 };
            double[] d = { 0, -0, 0, -1 };

            manualSpline = new ManualSpline(xNodes, a, b, c, d);
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

        // произвольный кубический многочлен =====
        double CubicPoly(double x, double A, double B, double C, double D)
        {
            return A + B * x + C * x * x + D * x * x * x;
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

            // Точки
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

            // Интерполяционный многочлен
            var aInterp = InterpolationPolynomial();
            var sInterp = chart1.Series.Add("Интерполяционный многочлен");
            sInterp.ChartType = SeriesChartType.Line;
            sInterp.Color = System.Drawing.Color.LightGreen;
            sInterp.BorderWidth = 7;

            foreach (double x in grid)
                sInterp.Points.AddXY(x, PolyEval(aInterp, x));

            // Лагранж
            var sLag = chart1.Series.Add("Лагранж");
            sLag.ChartType = SeriesChartType.Line;
            sLag.Color = System.Drawing.Color.Yellow;
            sLag.BorderWidth = 3;
            sLag.BorderDashStyle = ChartDashStyle.Dash;

            foreach (double x in grid)
                sLag.Points.AddXY(x, Lagrange(x));

            // Ньютон
            var aNewton = NewtonCoeffs();
            var sNewt = chart1.Series.Add("Ньютон");
            sNewt.ChartType = SeriesChartType.Line;
            sNewt.Color = System.Drawing.Color.Red;
            sNewt.BorderWidth = 1;
            sNewt.BorderDashStyle = ChartDashStyle.Dash;

            foreach (double x in grid)
                sNewt.Points.AddXY(x, NewtonEval(x, aNewton));

            // ===== СПЛАЙН ПО МЕТОДИЧКЕ =====
            if (X.Count >= 2)
            {
                var spline = CubicSpline.Build(X.ToArray(), Y.ToArray());

                var sSpline = chart1.Series.Add("Кубический сплайн");
                sSpline.ChartType = SeriesChartType.Line;
                sSpline.Color = System.Drawing.Color.Blue;
                sSpline.BorderWidth = 3;

                foreach (double x in grid)
                    sSpline.Points.AddXY(x, spline.Evaluate(x));
            }

            // ===== СПЛАЙН ИЗ СЛАУ (ручной) =====
            if (manualSpline != null)
            {
                var sManual = chart1.Series.Add("Сплайн из СЛАУ");
                sManual.ChartType = SeriesChartType.Line;
                sManual.Color = System.Drawing.Color.Orange;
                sManual.BorderWidth = 3;

                foreach (double x in grid)
                    sManual.Points.AddXY(x, manualSpline.Evaluate(x));
            }

            // ===== ПРОИЗВОЛЬНЫЙ КУБИЧЕСКИЙ МНОГОЧЛЕН =====
            double Acoef, Bcoef, Ccoef, Dcoef;

            bool okA = double.TryParse(txtA.Text, out Acoef);
            bool okB = double.TryParse(txtB.Text, out Bcoef);
            bool okC = double.TryParse(txtC.Text, out Ccoef);
            bool okD = double.TryParse(txtD.Text, out Dcoef);

            if (okA && okB && okC && okD)
            {
                var sPoly = chart1.Series.Add("Произвольный многочлен");
                sPoly.ChartType = SeriesChartType.Line;
                sPoly.Color = System.Drawing.Color.Purple;
                sPoly.BorderWidth = 3;

                foreach (double x in grid)
                    sPoly.Points.AddXY(x, CubicPoly(x, Acoef, Bcoef, Ccoef, Dcoef));
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Plot();
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e) { }
        private void chart1_Click(object sender, EventArgs e) { }

        private void label1_Click(object sender, EventArgs e) { }
        private void label2_Click(object sender, EventArgs e) { }
        private void label3_Click(object sender, EventArgs e) { }
        private void label4_Click(object sender, EventArgs e) { }

        private void txtA_TextChanged(object sender, EventArgs e) { }
        private void txtB_TextChanged(object sender, EventArgs e) { }
        private void txtC_TextChanged(object sender, EventArgs e) { }
        private void txtD_TextChanged(object sender, EventArgs e) { }

        private void btnPoly_Click(object sender, EventArgs e)
        {
            Plot();
        }
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

    // ===== КУБИЧЕСКИЙ СПЛАЙН ПО МЕТОДИЧКЕ =====
    public class CubicSpline
    {
        private readonly double[] x;  // узлы
        private readonly double[] a;  // a_i
        private readonly double[] b;  // b_i
        private readonly double[] c;  // c_i
        private readonly double[] d;  // d_i

        private CubicSpline(double[] x, double[] a, double[] b, double[] c, double[] d)
        {
            this.x = x;
            this.a = a;
            this.b = b;
            this.c = c;
            this.d = d;
        }

        public static CubicSpline Build(double[] x, double[] f)
        {
            if (x == null || f == null)
                throw new ArgumentNullException("x или f == null");

            if (x.Length != f.Length)
                throw new ArgumentException("Длины массивов x и f должны совпадать");

            int n = x.Length - 1; // количество отрезков

            if (n < 1)
                throw new ArgumentException("Нужно минимум два узла");

            for (int i = 1; i < x.Length; i++)
                if (x[i] <= x[i - 1])
                    throw new ArgumentException("Узлы x должны быть строго возрастающими");

            // h_i = x_i - x_{i-1}, i = 1..n
            double[] h = new double[n + 1];
            for (int i = 1; i <= n; i++)
                h[i] = x[i] - x[i - 1];

            // c[0] = c_1, ..., c[n] = c_{n+1}
            double[] c = new double[n + 1];
            c[0] = 0.0;
            c[n] = 0.0;

            if (n > 1)
            {
                int m = n - 1;
                double[] A = new double[m];
                double[] B = new double[m];
                double[] C = new double[m];
                double[] D = new double[m];

                for (int i = 2; i <= n; i++)
                {
                    int idx = i - 2;

                    double h_im1 = h[i - 1];
                    double h_i = h[i];

                    A[idx] = h_im1;
                    B[idx] = 2.0 * (h_im1 + h_i);
                    C[idx] = h_i;

                    D[idx] = 3.0 * (
                        (f[i] - f[i - 1]) / h_i
                        - (f[i - 1] - f[i - 2]) / h_im1
                    );
                }

                double[] alpha = new double[m];
                double[] beta = new double[m];

                alpha[0] = -C[0] / B[0];
                beta[0] = D[0] / B[0];

                for (int i = 1; i < m; i++)
                {
                    double denom = B[i] + A[i] * alpha[i - 1];
                    alpha[i] = -C[i] / denom;
                    beta[i] = (D[i] - A[i] * beta[i - 1]) / denom;
                }

                c[n - 1] = beta[m - 1];
                for (int i = m - 2; i >= 0; i--)
                    c[i + 1] = alpha[i] * c[i + 2] + beta[i];
            }

            double[] a = new double[n + 1];
            double[] b = new double[n + 1];
            double[] d = new double[n + 1];

            for (int i = 1; i <= n; i++)
            {
                double h_i = h[i];

                a[i] = f[i - 1]; // (3.3)

                double c_i = c[i - 1];
                double c_ip1 = c[i];

                d[i] = (c_ip1 - c_i) / (3.0 * h_i); // (3.9)

                b[i] = (f[i] - f[i - 1]) / h_i - (c_ip1 + 2.0 * c_i) * h_i / 3.0; // (3.10)
            }

            double[] aSeg = new double[n];
            double[] bSeg = new double[n];
            double[] cSeg = new double[n];
            double[] dSeg = new double[n];

            for (int i = 0; i < n; i++)
            {
                aSeg[i] = a[i + 1];
                bSeg[i] = b[i + 1];
                cSeg[i] = c[i];
                dSeg[i] = d[i + 1];
            }

            return new CubicSpline(x, aSeg, bSeg, cSeg, dSeg);
        }

        public double Evaluate(double xQuery)
        {
            int n = x.Length - 1;

            int i = 0;
            if (xQuery <= x[0])
                i = 0;
            else if (xQuery >= x[n])
                i = n - 1;
            else
            {
                for (int k = 0; k < n; k++)
                    if (xQuery >= x[k] && xQuery <= x[k + 1])
                    {
                        i = k;
                        break;
                    }
            }

            double t = xQuery - x[i];
            return a[i] + b[i] * t + c[i] * t * t + d[i] * t * t * t;
        }
    }

    // =====РУЧНОЙ СПЛАЙН ИЗ СЛАУ =====
    public class ManualSpline
    {
        private readonly double[] x;
        private readonly double[] a;
        private readonly double[] b;
        private readonly double[] c;
        private readonly double[] d;

        public ManualSpline(double[] x, double[] a, double[] b, double[] c, double[] d)
        {
            this.x = x;
            this.a = a;
            this.b = b;
            this.c = c;
            this.d = d;
        }

        public double Evaluate(double xQuery)
        {
            int n = x.Length - 1;
            int i = 0;

            if (xQuery <= x[0])
                i = 0;
            else if (xQuery >= x[n])
                i = n - 1;
            else
            {
                for (int k = 0; k < n; k++)
                    if (xQuery >= x[k] && xQuery <= x[k + 1])
                    {
                        i = k;
                        break;
                    }
            }

            double t = xQuery - x[i];
            return a[i] + b[i] * t + c[i] * t * t + d[i] * t * t * t;
        }
    }
}