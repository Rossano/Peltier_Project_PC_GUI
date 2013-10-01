using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Peltier_Graph
{
    public partial class zedGraphUserControl : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="zedGraphUserControl"/> class.
        /// Entry point to create a graph
        /// </summary>
        public zedGraphUserControl()
        {
            //  Initialize the ZedGraph Form
            InitializeComponent();          
            //  Create the graphic area
            //
            //  Nope postpone it in order to allow to read the configuration from the languages xml
            //CreateGraph(Graph);  
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="zedGraphUserControl"/> class.
        /// Entry point to create a graphic area
        /// </summary>
        /// <param name="X">The X dimension.</param>
        /// <param name="Y">The Y the dimension.</param>
        public zedGraphUserControl(int X, int Y)
        {
            //  Initialize the ZedGraph Form
            InitializeComponent();
            //  Create the graphic area
            //
            //  Nope postpone it in order to allow to read the configuration from the languages xml
            //CreateGraph(Graph);
            //  Set the dimensions and update the graphic area
            Width = X;
            Height = Y;
            Graph.Width = Y;
            Graph.Height = X;
        }
    }
}
