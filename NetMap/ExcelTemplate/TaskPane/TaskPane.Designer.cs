
//	Copyright (c) Microsoft Corporation.  All rights reserved.

namespace Microsoft.NetMap.ExcelTemplate
{
    partial class TaskPane
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TaskPane));
            this.cmsNetMapControl = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.msiContextSelectAll = new System.Windows.Forms.ToolStripMenuItem();
            this.msiContextSelectAllVertices = new System.Windows.Forms.ToolStripMenuItem();
            this.msiContextSelectAllEdges = new System.Windows.Forms.ToolStripMenuItem();
            this.msiContextSelectAllVerticesAndEdges = new System.Windows.Forms.ToolStripMenuItem();
            this.msiContextDeselectAll = new System.Windows.Forms.ToolStripMenuItem();
            this.msiContextDeselectAllVertices = new System.Windows.Forms.ToolStripMenuItem();
            this.msiContextDeselectAllEdges = new System.Windows.Forms.ToolStripMenuItem();
            this.msiContextDeselectAllVerticesAndEdges = new System.Windows.Forms.ToolStripMenuItem();
            this.msiContextSelectAdjacentVertices = new System.Windows.Forms.ToolStripMenuItem();
            this.msiContextDeselectAdjacentVertices = new System.Windows.Forms.ToolStripMenuItem();
            this.msiContextSelectIncidentEdges = new System.Windows.Forms.ToolStripMenuItem();
            this.msiContextDeselectIncidentEdges = new System.Windows.Forms.ToolStripMenuItem();
            this.msiContextSelectSubgraphs = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
            this.msiContextEditVertexAttributes = new System.Windows.Forms.ToolStripMenuItem();
            this.tssContextEditVertexAttributes = new System.Windows.Forms.ToolStripSeparator();
            this.msiContextReadWorkbook = new System.Windows.Forms.ToolStripMenuItem();
            this.msiContextForceLayout = new System.Windows.Forms.ToolStripMenuItem();
            this.msiContextForceLayoutSelected = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
            this.msiContextLayout = new System.Windows.Forms.ToolStripMenuItem();
            this.msiContextOptions = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.msiContextCopyImageToClipboard = new System.Windows.Forms.ToolStripMenuItem();
            this.msiContextSaveImageToFile = new System.Windows.Forms.ToolStripMenuItem();
            this.tsToolStrip = new System.Windows.Forms.ToolStrip();
            this.tsbReadWorkbook = new System.Windows.Forms.ToolStripButton();
            this.tssbForceLayout = new System.Windows.Forms.ToolStripSplitButton();
            this.msiForceLayoutSelected = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.ddbLayout = new System.Windows.Forms.ToolStripDropDownButton();
            this.tsbOptions = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.oNetMapControl = new Microsoft.NetMap.Visualization.NetMapControl();
            this.cmsNetMapControl.SuspendLayout();
            this.tsToolStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // cmsNetMapControl
            // 
            this.cmsNetMapControl.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.msiContextSelectAll,
            this.msiContextDeselectAll,
            this.msiContextSelectAdjacentVertices,
            this.msiContextDeselectAdjacentVertices,
            this.msiContextSelectIncidentEdges,
            this.msiContextDeselectIncidentEdges,
            this.msiContextSelectSubgraphs,
            this.toolStripSeparator5,
            this.msiContextEditVertexAttributes,
            this.tssContextEditVertexAttributes,
            this.msiContextReadWorkbook,
            this.msiContextForceLayout,
            this.msiContextForceLayoutSelected,
            this.toolStripSeparator4,
            this.msiContextLayout,
            this.msiContextOptions,
            this.toolStripSeparator3,
            this.msiContextCopyImageToClipboard,
            this.msiContextSaveImageToFile});
            this.cmsNetMapControl.Name = "cmsNetMapControl";
            this.cmsNetMapControl.Size = new System.Drawing.Size(235, 380);
            // 
            // msiContextSelectAll
            // 
            this.msiContextSelectAll.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.msiContextSelectAllVertices,
            this.msiContextSelectAllEdges,
            this.msiContextSelectAllVerticesAndEdges});
            this.msiContextSelectAll.Name = "msiContextSelectAll";
            this.msiContextSelectAll.Size = new System.Drawing.Size(234, 22);
            this.msiContextSelectAll.Text = "Select &All";
            // 
            // msiContextSelectAllVertices
            // 
            this.msiContextSelectAllVertices.Name = "msiContextSelectAllVertices";
            this.msiContextSelectAllVertices.Size = new System.Drawing.Size(165, 22);
            this.msiContextSelectAllVertices.Text = "&Vertices";
            this.msiContextSelectAllVertices.ToolTipText = "Select all vertices";
            this.msiContextSelectAllVertices.Click += new System.EventHandler(this.msiContextSelectAllVertices_Click);
            // 
            // msiContextSelectAllEdges
            // 
            this.msiContextSelectAllEdges.Name = "msiContextSelectAllEdges";
            this.msiContextSelectAllEdges.Size = new System.Drawing.Size(165, 22);
            this.msiContextSelectAllEdges.Text = "&Edges";
            this.msiContextSelectAllEdges.ToolTipText = "Select all edges";
            this.msiContextSelectAllEdges.Click += new System.EventHandler(this.msiContextSelectAllEdges_Click);
            // 
            // msiContextSelectAllVerticesAndEdges
            // 
            this.msiContextSelectAllVerticesAndEdges.Name = "msiContextSelectAllVerticesAndEdges";
            this.msiContextSelectAllVerticesAndEdges.Size = new System.Drawing.Size(165, 22);
            this.msiContextSelectAllVerticesAndEdges.Text = "Vertices &and Edges";
            this.msiContextSelectAllVerticesAndEdges.ToolTipText = "Select all vertices and edges";
            this.msiContextSelectAllVerticesAndEdges.Click += new System.EventHandler(this.msiContextSelectAllVerticesAndEdges_Click);
            // 
            // msiContextDeselectAll
            // 
            this.msiContextDeselectAll.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.msiContextDeselectAllVertices,
            this.msiContextDeselectAllEdges,
            this.msiContextDeselectAllVerticesAndEdges});
            this.msiContextDeselectAll.Name = "msiContextDeselectAll";
            this.msiContextDeselectAll.Size = new System.Drawing.Size(234, 22);
            this.msiContextDeselectAll.Text = "&Deselect All";
            // 
            // msiContextDeselectAllVertices
            // 
            this.msiContextDeselectAllVertices.Name = "msiContextDeselectAllVertices";
            this.msiContextDeselectAllVertices.Size = new System.Drawing.Size(165, 22);
            this.msiContextDeselectAllVertices.Text = "&Vertices";
            this.msiContextDeselectAllVertices.ToolTipText = "Deselect all vertices";
            this.msiContextDeselectAllVertices.Click += new System.EventHandler(this.msiContextDeselectAllVertices_Click);
            // 
            // msiContextDeselectAllEdges
            // 
            this.msiContextDeselectAllEdges.Name = "msiContextDeselectAllEdges";
            this.msiContextDeselectAllEdges.Size = new System.Drawing.Size(165, 22);
            this.msiContextDeselectAllEdges.Text = "&Edges";
            this.msiContextDeselectAllEdges.ToolTipText = "Deselect all edges";
            this.msiContextDeselectAllEdges.Click += new System.EventHandler(this.msiContextDeselectAllEdges_Click);
            // 
            // msiContextDeselectAllVerticesAndEdges
            // 
            this.msiContextDeselectAllVerticesAndEdges.Name = "msiContextDeselectAllVerticesAndEdges";
            this.msiContextDeselectAllVerticesAndEdges.Size = new System.Drawing.Size(165, 22);
            this.msiContextDeselectAllVerticesAndEdges.Text = "Vertices &and Edges";
            this.msiContextDeselectAllVerticesAndEdges.ToolTipText = "Deselect all vertices and edges";
            this.msiContextDeselectAllVerticesAndEdges.Click += new System.EventHandler(this.msiContextDeselectAllVerticesAndEdges_Click);
            // 
            // msiContextSelectAdjacentVertices
            // 
            this.msiContextSelectAdjacentVertices.Name = "msiContextSelectAdjacentVertices";
            this.msiContextSelectAdjacentVertices.Size = new System.Drawing.Size(234, 22);
            this.msiContextSelectAdjacentVertices.Text = "Select Ad&jacent Vertices";
            this.msiContextSelectAdjacentVertices.ToolTipText = "Select the vertices adjacent to the clicked vertex";
            this.msiContextSelectAdjacentVertices.Click += new System.EventHandler(this.msiContextSelectAdjacentVertices_Click);
            // 
            // msiContextDeselectAdjacentVertices
            // 
            this.msiContextDeselectAdjacentVertices.Name = "msiContextDeselectAdjacentVertices";
            this.msiContextDeselectAdjacentVertices.Size = new System.Drawing.Size(234, 22);
            this.msiContextDeselectAdjacentVertices.Text = "Deselect Adjace&nt Vertices";
            this.msiContextDeselectAdjacentVertices.ToolTipText = "Deselect the vertices adjacent to the clicked vertex";
            this.msiContextDeselectAdjacentVertices.Click += new System.EventHandler(this.msiContextDeselectAdjacentVertices_Click);
            // 
            // msiContextSelectIncidentEdges
            // 
            this.msiContextSelectIncidentEdges.Name = "msiContextSelectIncidentEdges";
            this.msiContextSelectIncidentEdges.Size = new System.Drawing.Size(234, 22);
            this.msiContextSelectIncidentEdges.Text = "Select &Incident Edges";
            this.msiContextSelectIncidentEdges.ToolTipText = "Select the edges incident to the clicked vertex";
            this.msiContextSelectIncidentEdges.Visible = false;
            this.msiContextSelectIncidentEdges.Click += new System.EventHandler(this.msiContextSelectIncidentEdges_Click);
            // 
            // msiContextDeselectIncidentEdges
            // 
            this.msiContextDeselectIncidentEdges.Name = "msiContextDeselectIncidentEdges";
            this.msiContextDeselectIncidentEdges.Size = new System.Drawing.Size(234, 22);
            this.msiContextDeselectIncidentEdges.Text = "Deselec&t Incident Edges";
            this.msiContextDeselectIncidentEdges.ToolTipText = "Deselect the edges incident to the clicked vertex";
            this.msiContextDeselectIncidentEdges.Click += new System.EventHandler(this.msiContextDeselectIncidentEdges_Click);
            // 
            // msiContextSelectSubgraphs
            // 
            this.msiContextSelectSubgraphs.Name = "msiContextSelectSubgraphs";
            this.msiContextSelectSubgraphs.Size = new System.Drawing.Size(234, 22);
            this.msiContextSelectSubgraphs.Text = "Select S&ubgraphs...";
            this.msiContextSelectSubgraphs.ToolTipText = "Select one or more subgraphs";
            this.msiContextSelectSubgraphs.Click += new System.EventHandler(this.msiContextSelectSubgraphs_Click);
            // 
            // toolStripSeparator5
            // 
            this.toolStripSeparator5.Name = "toolStripSeparator5";
            this.toolStripSeparator5.Size = new System.Drawing.Size(231, 6);
            // 
            // msiContextEditVertexAttributes
            // 
            this.msiContextEditVertexAttributes.Image = ((System.Drawing.Image)(resources.GetObject("msiContextEditVertexAttributes.Image")));
            this.msiContextEditVertexAttributes.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.msiContextEditVertexAttributes.Name = "msiContextEditVertexAttributes";
            this.msiContextEditVertexAttributes.Size = new System.Drawing.Size(234, 22);
            this.msiContextEditVertexAttributes.Text = "&Edit Selected Vertex Attributes...";
            this.msiContextEditVertexAttributes.ToolTipText = "Edit the visual attributes of the selected vertices";
            this.msiContextEditVertexAttributes.Click += new System.EventHandler(this.msiContextEditVertexAttributes_Click);
            // 
            // tssContextEditVertexAttributes
            // 
            this.tssContextEditVertexAttributes.Name = "tssContextEditVertexAttributes";
            this.tssContextEditVertexAttributes.Size = new System.Drawing.Size(231, 6);
            // 
            // msiContextReadWorkbook
            // 
            this.msiContextReadWorkbook.Image = ((System.Drawing.Image)(resources.GetObject("msiContextReadWorkbook.Image")));
            this.msiContextReadWorkbook.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.msiContextReadWorkbook.Name = "msiContextReadWorkbook";
            this.msiContextReadWorkbook.Size = new System.Drawing.Size(234, 22);
            this.msiContextReadWorkbook.Text = "Read &Workbook";
            this.msiContextReadWorkbook.ToolTipText = "Read graph data from the workbook";
            this.msiContextReadWorkbook.Click += new System.EventHandler(this.ReadWorkbook_Click);
            // 
            // msiContextForceLayout
            // 
            this.msiContextForceLayout.Image = ((System.Drawing.Image)(resources.GetObject("msiContextForceLayout.Image")));
            this.msiContextForceLayout.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.msiContextForceLayout.Name = "msiContextForceLayout";
            this.msiContextForceLayout.Size = new System.Drawing.Size(234, 22);
            this.msiContextForceLayout.Text = "Lay Out A&gain";
            this.msiContextForceLayout.ToolTipText = "Lay out all vertices without reading the workbook again";
            this.msiContextForceLayout.Click += new System.EventHandler(this.ForceLayout_Click);
            // 
            // msiContextForceLayoutSelected
            // 
            this.msiContextForceLayoutSelected.Image = ((System.Drawing.Image)(resources.GetObject("msiContextForceLayoutSelected.Image")));
            this.msiContextForceLayoutSelected.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.msiContextForceLayoutSelected.Name = "msiContextForceLayoutSelected";
            this.msiContextForceLayoutSelected.Size = new System.Drawing.Size(234, 22);
            this.msiContextForceLayoutSelected.Text = "Lay Out &Selected Vertices Again";
            this.msiContextForceLayoutSelected.ToolTipText = "Lay out the selected vertices without reading the workbook again";
            this.msiContextForceLayoutSelected.Click += new System.EventHandler(this.ForceLayoutSelected_Click);
            // 
            // toolStripSeparator4
            // 
            this.toolStripSeparator4.Name = "toolStripSeparator4";
            this.toolStripSeparator4.Size = new System.Drawing.Size(231, 6);
            // 
            // msiContextLayout
            // 
            this.msiContextLayout.Image = ((System.Drawing.Image)(resources.GetObject("msiContextLayout.Image")));
            this.msiContextLayout.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.msiContextLayout.Name = "msiContextLayout";
            this.msiContextLayout.Size = new System.Drawing.Size(234, 22);
            this.msiContextLayout.Text = "&Layout Type";
            this.msiContextLayout.ToolTipText = "Select the algorithm used to lay out the graph";
            // 
            // msiContextOptions
            // 
            this.msiContextOptions.Image = ((System.Drawing.Image)(resources.GetObject("msiContextOptions.Image")));
            this.msiContextOptions.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.msiContextOptions.Name = "msiContextOptions";
            this.msiContextOptions.Size = new System.Drawing.Size(234, 22);
            this.msiContextOptions.Text = "&Options...";
            this.msiContextOptions.ToolTipText = "Specify options that control the graph\'s appearance";
            this.msiContextOptions.Click += new System.EventHandler(this.Options_Click);
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(231, 6);
            // 
            // msiContextCopyImageToClipboard
            // 
            this.msiContextCopyImageToClipboard.Image = ((System.Drawing.Image)(resources.GetObject("msiContextCopyImageToClipboard.Image")));
            this.msiContextCopyImageToClipboard.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.msiContextCopyImageToClipboard.Name = "msiContextCopyImageToClipboard";
            this.msiContextCopyImageToClipboard.Size = new System.Drawing.Size(234, 22);
            this.msiContextCopyImageToClipboard.Text = "&Copy Image to Clipboard";
            this.msiContextCopyImageToClipboard.ToolTipText = "Copy the graph image to the clipboard";
            this.msiContextCopyImageToClipboard.Click += new System.EventHandler(this.msiContextCopyImageToClipboard_Click);
            // 
            // msiContextSaveImageToFile
            // 
            this.msiContextSaveImageToFile.Image = ((System.Drawing.Image)(resources.GetObject("msiContextSaveImageToFile.Image")));
            this.msiContextSaveImageToFile.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.msiContextSaveImageToFile.Name = "msiContextSaveImageToFile";
            this.msiContextSaveImageToFile.Size = new System.Drawing.Size(234, 22);
            this.msiContextSaveImageToFile.Text = "Save Image to &File...";
            this.msiContextSaveImageToFile.ToolTipText = "Copy the graph image to a file";
            this.msiContextSaveImageToFile.Click += new System.EventHandler(this.msiContextSaveImageToFile_Click);
            // 
            // tsToolStrip
            // 
            this.tsToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsbReadWorkbook,
            this.tssbForceLayout,
            this.toolStripSeparator1,
            this.ddbLayout,
            this.tsbOptions,
            this.toolStripSeparator2});
            this.tsToolStrip.Location = new System.Drawing.Point(0, 0);
            this.tsToolStrip.Name = "tsToolStrip";
            this.tsToolStrip.Size = new System.Drawing.Size(575, 25);
            this.tsToolStrip.TabIndex = 3;
            this.tsToolStrip.Text = "toolStrip1";
            // 
            // tsbReadWorkbook
            // 
            this.tsbReadWorkbook.Image = ((System.Drawing.Image)(resources.GetObject("tsbReadWorkbook.Image")));
            this.tsbReadWorkbook.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbReadWorkbook.Margin = new System.Windows.Forms.Padding(0, 1, 2, 2);
            this.tsbReadWorkbook.Name = "tsbReadWorkbook";
            this.tsbReadWorkbook.Size = new System.Drawing.Size(103, 22);
            this.tsbReadWorkbook.Text = "Read Workbook";
            this.tsbReadWorkbook.ToolTipText = "Read graph data from the workbook";
            this.tsbReadWorkbook.Click += new System.EventHandler(this.ReadWorkbook_Click);
            // 
            // tssbForceLayout
            // 
            this.tssbForceLayout.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.msiForceLayoutSelected});
            this.tssbForceLayout.Image = ((System.Drawing.Image)(resources.GetObject("tssbForceLayout.Image")));
            this.tssbForceLayout.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tssbForceLayout.Name = "tssbForceLayout";
            this.tssbForceLayout.Size = new System.Drawing.Size(107, 22);
            this.tssbForceLayout.Text = "Lay Out Again";
            this.tssbForceLayout.ToolTipText = "Lay out all vertices without reading the workbook again";
            this.tssbForceLayout.ButtonClick += new System.EventHandler(this.ForceLayout_Click);
            this.tssbForceLayout.DropDownOpening += new System.EventHandler(this.tssbForceLayout_DropDownOpening);
            // 
            // msiForceLayoutSelected
            // 
            this.msiForceLayoutSelected.Image = ((System.Drawing.Image)(resources.GetObject("msiForceLayoutSelected.Image")));
            this.msiForceLayoutSelected.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.msiForceLayoutSelected.Name = "msiForceLayoutSelected";
            this.msiForceLayoutSelected.Size = new System.Drawing.Size(227, 22);
            this.msiForceLayoutSelected.Text = "Lay Out Selected Vertices Again";
            this.msiForceLayoutSelected.ToolTipText = "Lay out the selected vertices without reading the workbook again";
            this.msiForceLayoutSelected.Click += new System.EventHandler(this.ForceLayoutSelected_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
            // 
            // ddbLayout
            // 
            this.ddbLayout.Image = ((System.Drawing.Image)(resources.GetObject("ddbLayout.Image")));
            this.ddbLayout.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.ddbLayout.Margin = new System.Windows.Forms.Padding(2, 1, 2, 2);
            this.ddbLayout.Name = "ddbLayout";
            this.ddbLayout.Size = new System.Drawing.Size(96, 22);
            this.ddbLayout.Text = "Layout Type";
            this.ddbLayout.ToolTipText = "Select the algorithm used to lay out the graph";
            // 
            // tsbOptions
            // 
            this.tsbOptions.Image = ((System.Drawing.Image)(resources.GetObject("tsbOptions.Image")));
            this.tsbOptions.ImageTransparentColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(0)))), ((int)(((byte)(255)))));
            this.tsbOptions.Margin = new System.Windows.Forms.Padding(2, 1, 2, 2);
            this.tsbOptions.Name = "tsbOptions";
            this.tsbOptions.Size = new System.Drawing.Size(64, 22);
            this.tsbOptions.Text = "Options";
            this.tsbOptions.ToolTipText = "Specify options that control the graph\'s appearance";
            this.tsbOptions.Click += new System.EventHandler(this.Options_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(6, 25);
            // 
            // oNetMapControl
            // 
            this.oNetMapControl.AllowVertexDrag = true;
            this.oNetMapControl.BackColor = System.Drawing.SystemColors.Window;
            this.oNetMapControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.oNetMapControl.Location = new System.Drawing.Point(0, 25);
            this.oNetMapControl.Name = "oNetMapControl";
            this.oNetMapControl.ShowToolTips = false;
            this.oNetMapControl.Size = new System.Drawing.Size(575, 349);
            this.oNetMapControl.TabIndex = 1;
            this.oNetMapControl.GraphMouseUp += new Microsoft.NetMap.Visualization.NetMapControl.GraphMouseEventHandler(this.oNetMapControl_GraphMouseUp);
            this.oNetMapControl.GraphDrawn += new System.EventHandler(this.oNetMapControl_GraphDrawn);
            this.oNetMapControl.SelectionChanged += new System.EventHandler(this.oNetMapControl_SelectionChanged);
            this.oNetMapControl.VertexMoved += new Microsoft.NetMap.Core.VertexEventHandler(this.oNetMapControl_VertexMoved);
            this.oNetMapControl.DrawingGraph += new System.EventHandler(this.oNetMapControl_DrawingGraph);
            // 
            // TaskPane
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.Controls.Add(this.oNetMapControl);
            this.Controls.Add(this.tsToolStrip);
            this.Name = "TaskPane";
            this.Size = new System.Drawing.Size(575, 374);
            this.cmsNetMapControl.ResumeLayout(false);
            this.tsToolStrip.ResumeLayout(false);
            this.tsToolStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Microsoft.NetMap.Visualization.NetMapControl oNetMapControl;
        private System.Windows.Forms.ToolStrip tsToolStrip;
        private System.Windows.Forms.ToolStripButton tsbOptions;
        private System.Windows.Forms.ContextMenuStrip cmsNetMapControl;
        private System.Windows.Forms.ToolStripMenuItem msiContextCopyImageToClipboard;
        private System.Windows.Forms.ToolStripMenuItem msiContextForceLayout;
        private System.Windows.Forms.ToolStripMenuItem msiContextOptions;
        private System.Windows.Forms.ToolStripMenuItem msiContextLayout;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.ToolStripDropDownButton ddbLayout;
        private System.Windows.Forms.ToolStripButton tsbReadWorkbook;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripMenuItem msiContextReadWorkbook;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
        private System.Windows.Forms.ToolStripMenuItem msiContextEditVertexAttributes;
        private System.Windows.Forms.ToolStripSeparator tssContextEditVertexAttributes;
        private System.Windows.Forms.ToolStripSplitButton tssbForceLayout;
        private System.Windows.Forms.ToolStripMenuItem msiForceLayoutSelected;
        private System.Windows.Forms.ToolStripMenuItem msiContextForceLayoutSelected;
        private System.Windows.Forms.ToolStripMenuItem msiContextSaveImageToFile;
        private System.Windows.Forms.ToolStripMenuItem msiContextSelectAll;
        private System.Windows.Forms.ToolStripMenuItem msiContextDeselectAll;
        private System.Windows.Forms.ToolStripMenuItem msiContextSelectIncidentEdges;
        private System.Windows.Forms.ToolStripMenuItem msiContextDeselectIncidentEdges;
        private System.Windows.Forms.ToolStripMenuItem msiContextSelectAdjacentVertices;
        private System.Windows.Forms.ToolStripMenuItem msiContextDeselectAdjacentVertices;
        private System.Windows.Forms.ToolStripMenuItem msiContextSelectSubgraphs;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator5;
        private System.Windows.Forms.ToolStripMenuItem msiContextSelectAllVertices;
        private System.Windows.Forms.ToolStripMenuItem msiContextSelectAllEdges;
        private System.Windows.Forms.ToolStripMenuItem msiContextSelectAllVerticesAndEdges;
        private System.Windows.Forms.ToolStripMenuItem msiContextDeselectAllVertices;
        private System.Windows.Forms.ToolStripMenuItem msiContextDeselectAllEdges;
        private System.Windows.Forms.ToolStripMenuItem msiContextDeselectAllVerticesAndEdges;
    }
}