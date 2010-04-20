﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SharpMap.Samples;
using WinFormSamples.Samples;

namespace WinFormsSample
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            mapImage1.Map = GradiantThemeSample.InitializeMap();
            mapImage1.Transform.Center = mapImage1.Map.GetExtents().GetCentroid();
            mapImage1.Transform.Resolution = 1;
            this.Load += new EventHandler(Form1_Load);
        }

        void Form1_Load(object sender, EventArgs e)
        {
            mapImage1.Refresh();
        }
    }
}