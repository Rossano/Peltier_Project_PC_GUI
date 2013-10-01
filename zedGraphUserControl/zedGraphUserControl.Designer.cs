using System;
using System.Windows;
using System.Windows.Forms;
using ZedGraph;

namespace Peltier_Graph
{
    partial class zedGraphUserControl : UserControl
    {

        #region Constants

        private const int MAX_POINTS = 1440;        //  24H x 60 Minutes = 1440

        #endregion

        #region

        public ZedGraphControl zgc;
        //private PointPairList D1;
        //private PointPairList D2;
        private RollingPointPairList RoomTemperatureData;
        private RollingPointPairList PeltierTemperatureData;        
        public string TitleLabel;
        public string XLabel;
        public string YLabel;
        public string PeltierCurveLabel;
        public string RoomCurveLabel;
        public string timeLabel;
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
        public ZedGraph.ZedGraphControl Graph;
        
        #endregion
        
         /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.Graph = new ZedGraph.ZedGraphControl();
            this.SuspendLayout();
            // 
            // Graph
            // 
            this.Graph.Location = new System.Drawing.Point(0, 3);
            this.Graph.Name = "Graph";
            this.Graph.ScrollGrace = 0D;
            this.Graph.ScrollMaxX = 0D;
            this.Graph.ScrollMaxY = 0D;
            this.Graph.ScrollMaxY2 = 0D;
            this.Graph.ScrollMinX = 0D;
            this.Graph.ScrollMinY = 0D;
            this.Graph.ScrollMinY2 = 0D;
            //this.Graph.Size = new System.Drawing.Size(297, 266);
            this.Graph.Size = new System.Drawing.Size(450, 350);
            this.Graph.TabIndex = 0;
            // 
            // zedGraphUserControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.Graph);
            this.Name = "zedGraphUserControl";
            this.Size = new System.Drawing.Size(300, 269);
            this.ResumeLayout(false);

        }

        #endregion


        public new int Width
        {
            get { return Graph.Width; }
            set { Graph.Width = value; }
        }

        public new int Height
        {
            get { return Graph.Height; }
            set { Graph.Height = value; }
        }

        #region Methods
        
        /// <summary>
        /// Creates the graphic area.
        /// </summary>
        /// <param name="zgcGraph">The ZGC graph data object.</param>
        public void CreateGraph (ZedGraphControl zgcGraph)
        {           
            //  Initialize the graphic area
            zgc = zgcGraph;
            GraphPane myPane = zgc.GraphPane;
            //
            //  Set the tiles
            //
            myPane.Title.Text = TitleLabel; // "Cella di Peltier";
            myPane.XAxis.Title.Text = XLabel;   // "Tempo [minuti]";
            myPane.YAxis.Title.Text = YLabel;   // "Temperatura\n[°C]";
            //
            //  Create data storage
            //
            double time, temp1, temp2;  //  Main variable to plot
            double err, c1, c2;         //  PID coefficients for debugging
            //PointPairList tempList1 = new PointPairList();
            //PointPairList tempList2 = new PointPairList();
            PeltierTemperatureData = new RollingPointPairList(MAX_POINTS);
            RoomTemperatureData = new RollingPointPairList(MAX_POINTS);
            //TimeSpan minutes = new TimeSpan();
            //TimeSpan oneMinute = new TimeSpan(0, 1, 0);
                        
            #region Connectionless
            
            //for (int i = 0; i < 360; i++)
            //{
            //    try
            //    {
            //        minutes.Add(oneMinute);
            //    }
            //    catch (Exception e)
            //    { }
            //    time = (double)i / 60;
            //    //time = (double)(minutes.Minutes + minutes.Hours * 60 + minutes.Days * 144);
            //    temp1 = (double)Math.Sin(6.28 * time) + 0.5;
            //    temp2 = (double)(1 * Math.Sin(6.28 * time) + 0.25 * Math.Sin(2 * 6.28 * time + 0.178));
            //    tempList1.Add(time, temp1);
            //    tempList2.Add(time, temp2);
            //}

            #endregion
            
            //
            //  Generate Curve
            //
            //LineItem tempCurve1 = myPane.AddCurve("Temperature Cella", tempList1, System.Drawing.Color.Red);
            //LineItem tempCurve2 = myPane.AddCurve("Temperatura Ambiente", tempList2, System.Drawing.Color.Blue);
            LineItem tempCurve1 = myPane.AddCurve(PeltierCurveLabel, PeltierTemperatureData, System.Drawing.Color.Red, SymbolType.None);
            LineItem tempCurve2 = myPane.AddCurve(RoomCurveLabel, RoomTemperatureData, System.Drawing.Color.Blue, SymbolType.None);            
            //LineItem tempCurve1 = myPane.AddCurve("Temperature Cella", PeltierTemperatureData, System.Drawing.Color.Red, SymbolType.None);
            //LineItem tempCurve2 = myPane.AddCurve("Temperatura Ambiente", RoomTemperatureData, System.Drawing.Color.Blue, SymbolType.None);
            Axis xAxis = myPane.XAxis;
            xAxis.Scale.MajorUnit = DateUnit.Second;//DateUnit.Minute;
            //xAxis.Scale.Max = 360;
            //xAxis.Scale.Min = 0;
            xAxis.Title = new AxisLabel(XLabel, "arial", 10, System.Drawing.Color.Black, false, false, false);
            Axis yAxis = myPane.YAxis;
            yAxis.Scale.MaxAuto = true;
            //
            //  Replot
            //
            zgc.AxisChange();
        }

        /// <summary>
        /// Sets the graphics size.
        /// </summary>
        /// <param name="X">The X axis dimension.</param>
        /// <param name="Y">The Y axis dimension.</param>
        public void SetSize(int X, int Y)
        {
            Graph.Location = new System.Drawing.Point(5, 5);            
            Graph.Size = new System.Drawing.Size(Y, X);
        }

        /// <summary>
        /// Updates the graphics area with new data.
        /// 
        /// The method retrives the data from the current graphic area and appen the new 2 data points
        /// </summary>
        /// <param name="P1">The Peltier Temperature New Data point.</param>
        /// <param name="P2">The Room temperature New Data point.</param>
        public void UpdateGraph(PointPair P1, PointPair P2)
        {
            //  Checks if there are any curve in the graphic area zgc, if not it exits
            if (zgc.GraphPane.CurveList.Count <= 0)
            {
                return;
            }
            //  Read the first curve, and check if exists, if not it exits
            LineItem curve1 = zgc.GraphPane.CurveList[0] as LineItem;
            if (curve1 == null)
            {
                return;
            }
            //  Read the data points in the curve of Peltier temperatures and checks if it exist, if not it exists
            IPointListEdit PeltierData = curve1.Points as IPointListEdit;
            if (PeltierData == null)
            {
                return;
            }
            //  Append the Peltier temperature data point to the curve
            PeltierData.Add(P1);

            //  Read the second curve and check if it exists, if not it exists
            LineItem curve2 = zgc.GraphPane.CurveList[1] as LineItem;
            if (curve2 == null)
            {
                return;
            }
            //  Read the data pointof the Room temperatures and checks if it exists, if no it exists
            IPointListEdit RoomData = curve2.Points as IPointListEdit;
            if (RoomData == null)
            {
                return;
            }
            //  Append the Room temperature Data point to the curve
            RoomData.Add(P2);

            //
            //  Scale the Graph
            //
            //XAxis xAxis=zgc.GraphPane.XAxis;            
            zgc.MasterPane.Title.Text = TitleLabel;
            //
            //  Make sure that the graph is updated with the new data
            //
            zgc.AxisChange();
            //
            //  Force a redrax
            //
            zgc.Invalidate();
        }

        #endregion
    }
}
