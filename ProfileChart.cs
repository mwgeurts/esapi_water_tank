using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ProfileComparison
{
    internal class ProfileChart
    {
        private double chartWidth = 300;
        private double chartHeight = 300;
        private double xmin = 0;
        private double xmax = 6.5;
        private double ymin = -1.1;
        private double ymax = 1.1;
        private Polyline SolidLine = new Polyline();
        private Polyline DashLine = new Polyline();

        public ProfileChart(Canvas chartCanvas) {   
            this.chartCanvas = chartCanvas;
        }

        private Canvas chartCanvas;
        public Canvas ChartCanvas
        { 
            get { return chartCanvas; } 
            set { chartCanvas = value; }
        }


        private PointCollection solidLinePoints;
        public PointCollection SolidLinePoints
        {
            get { return solidLinePoints; }
            set { solidLinePoints = value; }
        }


        private PointCollection dashLinePoints;
        public PointCollection DashLinePoints
        {
            get { return dashLinePoints; }
            set { dashLinePoints = value; }
        }

        public void UpdateChart()
        {
            chartWidth = chartCanvas.ActualWidth;
            chartHeight = chartCanvas.ActualHeight;

            SolidLinePoints = new PointCollection();
            DashLinePoints = new PointCollection();
            double x = 0;
            double y = 0;
            double z = 0;
            for (int i = 0; i < 70; i++)
            {
                x = i / 5.0;
                y = Math.Sin(x);
                z = Math.Cos(x);

                DashLinePoints.Add(NormalizePoint(new Point(x, z)));
                SolidLinePoints.Add(NormalizePoint(new Point(x, y)));
            }

            
            SolidLine.Points = SolidLinePoints;
            this.chartCanvas.Children.Add(SolidLine);

            DashLine.Points = DashLinePoints;
            this.chartCanvas.Children.Add(DashLine);
        }

        public void ResizeChart(double width, double height)
        {
            //
            // TO DO
            //
        }

        public Point NormalizePoint(Point pt)
        {
            var res = new Point();
            res.X = (pt.X - xmin) * chartWidth / (xmax - xmin);
            res.Y = chartHeight - (pt.Y - ymin) * chartHeight / (ymax - ymin);
            return res;
        }

    }
}
