
//  Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Office.Interop.Excel;
using Microsoft.Research.CommunityTechnologies.AppLib;
using Microsoft.WpfGraphicsLib;
using Microsoft.NodeXL.Core;
using Microsoft.NodeXL.Layouts;
using Microsoft.NodeXL.Visualization.Wpf;
using Microsoft.NodeXL.ApplicationUtil;
using System.Windows.Input;

namespace Microsoft.NodeXL.ExcelTemplate
{
//*****************************************************************************
//  Class: TaskPane
//
/// <summary>
/// User control that implements the NodeXL custom task pane.
/// </summary>
///
/// <remarks>
/// This Excel 2007 custom task pane contains a NodeXLControl that displays a
/// NodeXL graph, along with controls for manipulating the graph.  It utilizes
/// the task pane support provided by Visual Studio 2005 Tools for the Office
/// System SE.
/// </remarks>
//*****************************************************************************

public partial class TaskPane : UserControl
{
    //*************************************************************************
    //  Constructor: TaskPane()
    //
    /// <summary>
    /// Initializes a new instance of the <see cref="TaskPane" /> class.
    /// </summary>
    ///
    /// <param name="thisWorkbook">
    /// The workbook.
    /// </param>
    ///
    /// <param name="ribbon">
    /// The application's ribbon.
    /// </param>
    //*************************************************************************

    public TaskPane
    (
        ThisWorkbook thisWorkbook,
        Ribbon ribbon
    )
    {
        Debug.Assert(thisWorkbook != null);
        Debug.Assert(ribbon != null);

        InitializeComponent();

        m_oWorkbook = thisWorkbook.InnerObject;
        m_oRibbon = ribbon;

        // The WpfImageUtil uses the screen DPI in its image handling.

        Graphics oGraphics = this.CreateGraphics();
        WpfImageUtil.ScreenDpi = oGraphics.DpiX;
        oGraphics.Dispose();

        // Get the template version from the per-workbook settings.

        PerWorkbookSettings oPerWorkbookSettings =
            this.PerWorkbookSettings;

        m_iTemplateVersion = oPerWorkbookSettings.TemplateVersion;

        m_bHandlingLayoutChanged = false;
        m_iEnableGraphControlsCount = 0;
        m_oEdgeRowIDDictionary = null;
        m_oVertexRowIDDictionary = null;
        m_oSaveGraphImageFileDialog = null;
        m_oDynamicFilterDialog = null;

        GeneralUserSettings oGeneralUserSettings = new GeneralUserSettings();
        LayoutUserSettings oLayoutUserSettings = new LayoutUserSettings();

        LayoutType eInitialLayout = oLayoutUserSettings.Layout;

        // Instantiate an object that populates the sbLayout
        // ToolStripSplitButton and handles its LayoutChanged event.

        m_oLayoutManagerForToolStripSplitButton =
            new LayoutManagerForToolStripSplitButton();

        m_oLayoutManagerForToolStripSplitButton.AddItems(this.sbLayout);
        m_oLayoutManagerForToolStripSplitButton.Layout = eInitialLayout;

        // Instantiate an object that populates the msiContextLayout
        // context menu and handles the Clicked events on the child menu items.

        m_oLayoutManagerForContextMenu = new LayoutManagerForMenu();
        m_oLayoutManagerForContextMenu.AddMenuItems(this.msiContextLayout);
        m_oLayoutManagerForContextMenu.Layout = eInitialLayout;

        m_oLayoutManagerForToolStripSplitButton.LayoutChanged +=
            new EventHandler(this.LayoutManager_LayoutChanged);

        m_oLayoutManagerForContextMenu.LayoutChanged +=
            new EventHandler(this.LayoutManager_LayoutChanged);


        thisWorkbook.VisualAttributeSetInWorkbook +=
            new EventHandler(ThisWorkbook_VisualAttributeSetInWorkbook);

        thisWorkbook.CollapseOrExpandGroups +=
            new CollapseOrExpandGroupsEventHandler(
                ThisWorkbook_CollapseOrExpandGroups);

        thisWorkbook.WorksheetContextMenuManager.RequestVertexCommandEnable +=
            new RequestVertexCommandEnableEventHandler(
                WorksheetContextMenuManager_RequestVertexCommandEnable);

        thisWorkbook.WorksheetContextMenuManager.RequestEdgeCommandEnable +=
            new RequestEdgeCommandEnableEventHandler(
                WorksheetContextMenuManager_RequestEdgeCommandEnable);

        thisWorkbook.WorksheetContextMenuManager.RunVertexCommand +=
            new RunVertexCommandEventHandler(
                WorksheetContextMenuManager_RunVertexCommand);

        thisWorkbook.WorksheetContextMenuManager.RunEdgeCommand +=
            new RunEdgeCommandEventHandler(
                WorksheetContextMenuManager_RunEdgeCommand);

        // There is no Closing event on the TaskPane and no parent form whose
        // Closing event can be handled.  Instead, using the Shutdown event on
        // the workbook to perform closing tasks.

        thisWorkbook.Shutdown += new EventHandler(ThisWorkbook_Shutdown);

        m_oRibbon.Layout = eInitialLayout;

        m_oRibbon.RibbonControlsChanged +=
            new RibbonControlsChangedEventHandler(
                Ribbon_RibbonControlsChanged);

        m_oRibbon.RunRibbonCommand += new RunRibbonCommandEventHandler(
            Ribbon_RunRibbonCommand);

        CreateNodeXLControl(oGeneralUserSettings);
        CreateGraphZoomAndScaleControl();

        oNodeXLControl.GraphScale =
            ( new GraphZoomAndScaleUserSettings() ).GraphScale;

        ApplyGeneralUserSettings(oGeneralUserSettings);
        ApplyLayoutUserSettings(oLayoutUserSettings);

        this.ShowGraphLegend = m_oRibbon.ShowGraphLegend;

        UpdateAutoFillResultsLegend(oPerWorkbookSettings);
        UpdateDynamicFiltersLegend();
        UpdateAxes(oPerWorkbookSettings);

        AssertValid();
    }

    //*************************************************************************
    //  Method: SetSelectedEdgesByRowID()
    //
    /// <summary>
    /// Sets which edges are selected given a collection of row IDs.
    /// </summary>
    ///
    /// <param name="rowIDs">
    /// Collection of IDs of the edges to select.  These are the IDs stored in
    /// the edge worksheet, not IEdge.ID values.
    /// </param>
    //*************************************************************************

    public void
    SetSelectedEdgesByRowID
    (
        ICollection<Int32> rowIDs
    )
    {
        Debug.Assert(rowIDs != null);
        AssertValid();

        SetSelectionByRowIDs(rowIDs, null);
    }

    //*************************************************************************
    //  Method: SetSelectedVerticesByRowID()
    //
    /// <summary>
    /// Sets which vertices are selected given a collection of row IDs.
    /// </summary>
    ///
    /// <param name="rowIDs">
    /// Collection of IDs of the vertices to select.  These are the IDs stored
    /// in the vertex worksheet, not IVertex.ID values.
    /// </param>
    //*************************************************************************

    public void
    SetSelectedVerticesByRowID
    (
        ICollection<Int32> rowIDs
    )
    {
        Debug.Assert(rowIDs != null);
        AssertValid();

        SetSelectionByRowIDs(null, rowIDs);
    }

    //*************************************************************************
    //  Method: GetSelectedEdgeRowIDs()
    //
    /// <summary>
    /// Gets the row IDs of the edges that are selected.
    /// </summary>
    ///
    /// <returns>
    /// Collection of row IDs of the edges that are selected.  These are the
    /// row IDs stored in the edge worksheet, not IEdge.ID values.
    /// </returns>
    //*************************************************************************

    public ICollection<Int32>
    GetSelectedEdgeRowIDs()
    {
        AssertValid();

        return ( NodeXLControlUtil.GetSelectedEdgeRowIDs(oNodeXLControl) );
    }

    //*************************************************************************
    //  Method: GetSelectedVertexRowIDs()
    //
    /// <summary>
    /// Gets the row IDs of the vertices that are selected.
    /// </summary>
    ///
    /// <returns>
    /// Collection of IDs of the vertices that are selected.  These are the IDs
    /// stored in the vertex worksheet, not IVertex.ID values.
    /// </returns>
    ///
    /// <remarks>
    /// The returned collection does not include selected vertices that
    /// represent collapsed groups.  Use <see
    /// cref="GetSelectedCollapsedGroupNames" /> to get those.
    /// </remarks>
    //*************************************************************************

    public ICollection<Int32>
    GetSelectedVertexRowIDs()
    {
        AssertValid();

        return ( NodeXLControlUtil.GetSelectedVertexRowIDs(oNodeXLControl) );
    }

    //*************************************************************************
    //  Method: IsCollapsedGroup()
    //
    /// <summary>
    /// Determines whether a group is collapsed.
    /// </summary>
    ///
    /// <param name="groupName">
    /// The name of the group to check.
    /// </param>
    ///
    /// <returns>
    /// true if the specified group is collapsed.
    /// </returns>
    //*************************************************************************

    public Boolean
    IsCollapsedGroup
    (
        String groupName
    )
    {
        Debug.Assert( !String.IsNullOrEmpty(groupName) );
        AssertValid();

        return ( oNodeXLControl.IsCollapsedGroup(groupName) );
    }

    //*************************************************************************
    //  Method: SelectCollapsedGroup()
    //
    /// <summary>
    /// Selects the vertex that represents a collapsed group.
    /// </summary>
    ///
    /// <param name="groupName">
    /// The name of the group to select.
    /// </param>
    //*************************************************************************

    public void
    SelectCollapsedGroup
    (
        String groupName
    )
    {
        Debug.Assert( !String.IsNullOrEmpty(groupName) );
        AssertValid();

        oNodeXLControl.SelectCollapsedGroup(groupName);
    }

    //*************************************************************************
    //  Method: GetSelectedCollapsedGroupNames()
    //
    /// <summary>
    /// Gets the names of the collapsed groups that are selected.
    /// </summary>
    ///
    /// <returns>
    /// The names of the collapsed groups.  The names are the group names
    /// stored in the group worksheet.
    /// </returns>
    //*************************************************************************

    public ICollection<String>
    GetSelectedCollapsedGroupNames()
    {
        AssertValid();

        return ( oNodeXLControl.GetSelectedCollapsedGroupNames() );
    }

    //*************************************************************************
    //  Event: SelectionChangedInGraph
    //
    /// <summary>
    /// Occurs when the selection state of the NodeXL graph changes.
    /// </summary>
    //*************************************************************************

    public event EventHandler SelectionChangedInGraph;


    //*************************************************************************
    //  Event: VertexAttributesEditedInGraph
    //
    /// <summary>
    /// Occurs when vertex attributes are edited in the NodeXL graph.
    /// </summary>
    //*************************************************************************

    public event VertexAttributesEditedEventHandler
        VertexAttributesEditedInGraph;


    //*************************************************************************
    //  Event: GraphLaidOut
    //
    /// <summary>
    /// Occurs after graph drawing completes.
    /// </summary>
    ///
    /// <remarks>
    /// Graph layout occurs asynchronously.  This event fires when the graph
    /// is successfully laid out.
    /// </remarks>
    //*************************************************************************

    public event GraphLaidOutEventHandler GraphLaidOut;


    //*************************************************************************
    //  Event: VerticesMoved
    //
    /// <summary>
    /// Occurs after one or more vertices are moved to new locations in the
    /// graph.
    /// </summary>
    ///
    /// <remarks>
    /// This event is fired when the user releases the mouse button after
    /// dragging one or more vertices to new locations in the graph.
    /// </remarks>
    //*************************************************************************

    public event VerticesMovedEventHandler2 VerticesMoved;


    //*************************************************************************
    //  Property: GraphRectangle
    //
    /// <summary>
    /// Gets the graph rectangle, in WPF units.
    /// </summary>
    ///
    /// <value>
    /// The graph rectangle, in WPF units.
    /// </value>
    ///
    /// <remarks>
    /// The width and height, which are Double in WPF units, are truncated to
    /// Int32s.
    /// </remarks>
    //*************************************************************************

    protected System.Drawing.Rectangle
    GraphRectangle
    {
        get
        {
            return ( new System.Drawing.Rectangle(0, 0,
                (Int32)oNodeXLControl.ActualWidth,
                (Int32)oNodeXLControl.ActualHeight) );
        }
    }

    //*************************************************************************
    //  Property: NonEmptyWorkbookRead
    //
    /// <summary>
    /// Gets a flag indicating whether a non-empty graph has been successfully
    /// read from the workbook.
    /// </summary>
    ///
    /// <value>
    /// true if a non-empty graph has been successfully read from the workbook.
    /// </value>
    //*************************************************************************

    protected Boolean
    NonEmptyWorkbookRead
    {
        get
        {
            return (m_oEdgeRowIDDictionary != null &&
                oNodeXLControl.Graph.Vertices.Count > 0);
        }
    }

    //*************************************************************************
    //  Property: PerWorkbookSettings
    //
    /// <summary>
    /// Gets the per-workbook settings.
    /// </summary>
    ///
    /// <value>
    /// The per-workbook settings.
    /// </value>
    //*************************************************************************

    protected PerWorkbookSettings
    PerWorkbookSettings
    {
        get
        {
            // AssertValid();
            Debug.Assert(m_oWorkbook != null);

            return ( new PerWorkbookSettings(m_oWorkbook) );
        }
    }

    //*************************************************************************
    //  Property: ShowGraphLegend
    //
    /// <summary>
    /// Sets a flag indicating whether the graph legend should be shown.
    /// </summary>
    ///
    /// <value>
    /// true to show the graph legend.
    /// </value>
    //*************************************************************************

    protected Boolean
    ShowGraphLegend
    {
        set
        {
            this.splLegend.Panel2Collapsed = !value;

            // AssertValid();
        }
    }

    //*************************************************************************
    //  Property: ShowGraphAxes
    //
    /// <summary>
    /// Sets a flag indicating whether the graph axes should be shown.
    /// </summary>
    ///
    /// <value>
    /// true to show the graph axes.
    /// </value>
    //*************************************************************************

    protected Boolean
    ShowGraphAxes
    {
        set
        {
            Debug.Assert(m_oNodeXLWithAxesControl != null);

            m_oNodeXLWithAxesControl.ShowAxes = value;

            // AssertValid();
        }
    }

    //*************************************************************************
    //  Property: LayoutIsNull
    //
    /// <summary>
    /// Gets a flag indicating whether the selected layout is LayoutType.Null.
    /// </summary>
    ///
    /// <value>
    /// true if the selected layout is LayoutType.Null.
    /// </value>
    //*************************************************************************

    protected Boolean
    LayoutIsNull
    {
        get
        {
            // Either layout manager will work.  Arbitrarily use one of them.

            return (m_oLayoutManagerForToolStripSplitButton.Layout ==
                LayoutType.Null);
        }
    }

    //*************************************************************************
    //  Method: CreateNodeXLControl()
    //
    /// <summary>
    /// Creates a NodeXLControl, hooks up its events, and assigns it as the
    /// child of an ElementHost.
    /// </summary>
    ///
    /// <param name="oGeneralUserSettings">
    /// The user's general settings.
    /// </param>
    //*************************************************************************

    protected void
    CreateNodeXLControl
    (
        GeneralUserSettings oGeneralUserSettings
    )
    {
        Debug.Assert(oGeneralUserSettings != null);

        // AssertValid();

        // Control hierarchy:
        //
        // 1. ehElementHost contains a NodeXLWithAxesControl.
        //
        // 2. The NodeXLWithAxesControl contains an ExcelTemplateNodeXLControl.
        //
        // 3. The ExcelTemplateNodeXLControl is derived from NodeXLControl.

        oNodeXLControl = new ExcelTemplateNodeXLControl();

        m_oNodeXLWithAxesControl = new NodeXLWithAxesControl(oNodeXLControl);
        m_oNodeXLWithAxesControl.ShowAxes = oGeneralUserSettings.ShowGraphAxes;

        oNodeXLControl.SelectionChanged +=
            new System.EventHandler(this.oNodeXLControl_SelectionChanged);

        oNodeXLControl.VerticesMoved += new VerticesMovedEventHandler(
            this.oNodeXLControl_VerticesMoved);

        oNodeXLControl.GraphMouseUp += new GraphMouseButtonEventHandler(
            this.oNodeXLControl_GraphMouseUp);

        oNodeXLControl.LayingOutGraph += new System.EventHandler(
            this.oNodeXLControl_LayingOutGraph);

        oNodeXLControl.GraphLaidOut += new AsyncCompletedEventHandler(
            this.oNodeXLControl_GraphLaidOut);

        ehNodeXLControlHost.Child = m_oNodeXLWithAxesControl;
    }

    //*************************************************************************
    //  Method: CreateGraphZoomAndScaleControl()
    //
    /// <summary>
    /// Creates the control that can zoom and scale the NodeXLControl.
    /// </summary>
    //*************************************************************************

    protected void
    CreateGraphZoomAndScaleControl()
    {
        Debug.Assert(oNodeXLControl != null);

        GraphZoomAndScaleControl oGraphZoomAndScaleControl =
            new GraphZoomAndScaleControl();

        oGraphZoomAndScaleControl.NodeXLControl = oNodeXLControl;

        // The control lives within the graph navigation ToolStrip.

        this.tsGraphNavigation.Items.Add( new ToolStripControlHost(
            oGraphZoomAndScaleControl) );
    }

    //*************************************************************************
    //  Method: ReadWorkbook()
    //
    /// <overloads>
    /// Transfers data from the workbook to the NodeXLControl.
    /// </overloads>
    ///
    /// <summary>
    /// Transfers data from the workbook to the NodeXLControl and lays out the
    /// graph.
    /// </summary>
    //*************************************************************************

    protected void
    ReadWorkbook()
    {
        AssertValid();

        ReadWorkbook(true);
    }

    //*************************************************************************
    //  Method: ReadWorkbook()
    //
    /// <summary>
    /// Transfers data from the workbook to the NodeXLControl and optionally
    /// lays out the graph.
    /// </summary>
    ///
    /// <param name="bLayOutGraph">
    /// true to lay out the graph.
    /// </param>
    //*************************************************************************

    protected void
    ReadWorkbook
    (
        Boolean bLayOutGraph
    )
    {
        AssertValid();

        if (oNodeXLControl.IsLayingOutGraph)
        {
            return;
        }

        if (
            !this.NonEmptyWorkbookRead
            && this.LayoutIsNull
            && !ShowLayoutTypeIsNullNotification()
            )
        {
            return;
        }

        // This is in case another open workbook has modified the user
        // settings.

        GeneralUserSettings oGeneralUserSettings = new GeneralUserSettings();
        ApplyGeneralUserSettings(oGeneralUserSettings);
        ApplyLayoutUserSettings( new LayoutUserSettings() );

        ReadWorkbookContext oReadWorkbookContext = new ReadWorkbookContext();

        oReadWorkbookContext.IgnoreVertexLocations = false;
        oReadWorkbookContext.GraphRectangle = this.GraphRectangle;
        oReadWorkbookContext.FillIDColumns = true;
        oReadWorkbookContext.ReadGroups = m_oRibbon.ReadGroups;
        oReadWorkbookContext.ReadVertexLabels = m_oRibbon.ReadVertexLabels;
        oReadWorkbookContext.ReadEdgeLabels = m_oRibbon.ReadEdgeLabels;
        oReadWorkbookContext.ReadVertexImages = true;

        oReadWorkbookContext.DefaultVertexImageSize =
            oGeneralUserSettings.VertexImageSize;

        oReadWorkbookContext.DefaultVertexShape =
            oGeneralUserSettings.VertexShape;

        // Populate the vertex worksheet.  This isn't strictly necessary, but
        // it does enable the vertex worksheet to be updated when the user
        // edits vertex attributes in the NodeXL graph.  (If the vertex
        // worksheet is missing, vertex attributes can still be edited in the
        // graph; the edits just won't get saved in the workbook.)

        oReadWorkbookContext.PopulateVertexWorksheet = true;

        WorkbookReader oWorkbookReader = new WorkbookReader();

        m_oEdgeRowIDDictionary = null;
        m_oVertexRowIDDictionary = null;

        EnableGraphControls(false);

        try
        {
            // Read the workbook into a Graph object.

            IGraph oGraph = oWorkbookReader.ReadWorkbook(
                m_oWorkbook, oReadWorkbookContext);

            // Save the edge and vertex dictionaries that were created by
            // WorkbookReader.

            m_oEdgeRowIDDictionary = oReadWorkbookContext.EdgeRowIDDictionary;

            m_oVertexRowIDDictionary =
                oReadWorkbookContext.VertexRowIDDictionary;

            // Load the NodeXLControl with the resulting graph.

            oNodeXLControl.Graph = oGraph;

            // Collapse any groups that are supposed to be collapsed.

            CollapseOrExpandGroups(GetGroupNamesToCollapse(oGraph), true,
                false);

            // Enable tooltips only if tooltips were specified in the workbook.

            oNodeXLControl.ShowVertexToolTips = oGraph.ContainsKey(
                ReservedMetadataKeys.ToolTipSet);

            // If the dynamic filter dialog is open, read the dynamic filter
            // columns it filled in.

            if (m_oDynamicFilterDialog != null)
            {
                ReadDynamicFilterColumns(false);
                ReadFilteredAlpha(false);
            }

            oNodeXLControl.DrawGraph(bLayOutGraph);

            PerWorkbookSettings oPerWorkbookSettings =
                this.PerWorkbookSettings;

            UpdateAutoFillResultsLegend(oPerWorkbookSettings);
            UpdateDynamicFiltersLegend();
            UpdateAxes(oPerWorkbookSettings);
        }
        catch (Exception oException)
        {
            // If exceptions aren't caught here, Excel consumes them without
            // indicating that anything is wrong.  This can result in the graph
            // controls remaining disabled, among other problems.

            ErrorUtil.OnException(oException);
        }
        finally
        {
            EnableGraphControls(true);
        }

        // Change the button text to indicate that if any of the buttons is
        // clicked again, the graph will be read again.

        tsbReadWorkbook.Text = msiContextReadWorkbook.Text =
            m_oRibbon.ReadWorkbookButtonText =
            "Refresh Graph";
    }

    //*************************************************************************
    //  Method: GetGroupNamesToCollapse()
    //
    /// <summary>
    /// Gets the names of groups that should be collapsed.
    /// </summary>
    ///
    /// <param name="oGraph">
    /// The graph that was read from the workbook.
    /// </param>
    ///
    /// <returns>
    /// A collection of the names of groups that should be collapsed.  The
    /// collection can be empty but is never null.
    /// </returns>
    //*************************************************************************

    protected ICollection<String>
    GetGroupNamesToCollapse
    (
        IGraph oGraph
    )
    {
        Debug.Assert(oGraph != null);

        LinkedList<String> oGroupNamesToCollapse = new LinkedList<String>();
        Object oGroupInformationAsObject;

        // The WorkbookReader object may have stored group information as
        // metadata on the graph.

        if ( oGraph.TryGetValue(ReservedMetadataKeys.GroupInformation,
            typeof( ICollection<GroupInformation> ),
            out oGroupInformationAsObject) )
        {
            foreach (GroupInformation oGroupInformation in
                ( ICollection<GroupInformation> )oGroupInformationAsObject)
            {
                if (oGroupInformation.IsCollapsed)
                {
                    oGroupNamesToCollapse.AddLast(oGroupInformation.Name);
                }
            }
        }

        return (oGroupNamesToCollapse);
    }

    //*************************************************************************
    //  Method: CollapseOrExpandGroups()
    //
    /// <summary>
    /// Collapses or expands one or more vertex groups.
    /// </summary>
    ///
    /// <param name="oGroupNames">
    /// Collection of the names of groups that should be collapsed or expanded.
    /// </param>
    ///
    /// <param name="bCollapse">
    /// true to collapse the groups, false to expand them.
    /// </param>
    ///
    /// <param name="bRedrawGroupImmediately">
    /// true to redraw the collapsed or expanded group immediately.
    /// </param>
    //*************************************************************************

    protected void
    CollapseOrExpandGroups
    (
        ICollection<String> oGroupNames,
        Boolean bCollapse,
        Boolean bRedrawGroupImmediately
    )
    {
        Debug.Assert(oGroupNames != null);
        AssertValid();

        if (oGroupNames.Count == 0)
        {
            return;
        }

        // Should the following event handlers be permanent?  Right now, edges
        // and vertices are removed and added only when groups are collapsed
        // and expanded.  For now, keep the event handlers local and temporary.

        EdgeEventHandler oEdgeAddedEventHandler =
        delegate(Object sender, EdgeEventArgs e)
        {
            IEdge oAddedEdge = e.Edge;

            if (oAddedEdge.Tag is Int32)
            {
                m_oEdgeRowIDDictionary.Add( (Int32)oAddedEdge.Tag,
                    oAddedEdge );
            }
        };

        EdgeEventHandler oEdgeRemovedEventHandler =
        delegate(Object sender, EdgeEventArgs e)
        {
            IEdge oRemovedEdge = e.Edge;

            if (oRemovedEdge.Tag is Int32)
            {
                m_oEdgeRowIDDictionary.Remove( (Int32)oRemovedEdge.Tag );
            }
        };

        VertexEventHandler oVertexAddedEventHandler =
        delegate(Object sender, VertexEventArgs e)
        {
            IVertex oAddedVertex = e.Vertex;

            if ( !GroupWorksheetReader.VertexIsCollapsedGroup(oAddedVertex) )
            {
                m_oVertexRowIDDictionary.Add( (Int32)oAddedVertex.Tag,
                    oAddedVertex );
            }
        };

        VertexEventHandler oVertexRemovedEventHandler =
        delegate(Object sender, VertexEventArgs e)
        {
            IVertex oRemovedVertex = e.Vertex;

            if ( !GroupWorksheetReader.VertexIsCollapsedGroup(oRemovedVertex) )
            {
                m_oVertexRowIDDictionary.Remove( (Int32)oRemovedVertex.Tag );
            }
        };

        IGraph oGraph = oNodeXLControl.Graph;
        IEdgeCollection oEdges = oGraph.Edges;
        IVertexCollection oVertices = oGraph.Vertices;

        oEdges.EdgeAdded += oEdgeAddedEventHandler;
        oEdges.EdgeRemoved += oEdgeRemovedEventHandler;
        oVertices.VertexAdded += oVertexAddedEventHandler;
        oVertices.VertexRemoved += oVertexRemovedEventHandler;

        foreach (String sGroupName in oGroupNames)
        {
            if (bCollapse)
            {
                oNodeXLControl.CollapseGroup(sGroupName,

                    VertexRowIDsToVertices(
                        NodeXLWorkbookUtil.GetVertexIDsInGroup(m_oWorkbook,
                            sGroupName) ),

                    bRedrawGroupImmediately);
            }
            else
            {
                oNodeXLControl.ExpandGroup(sGroupName,
                    bRedrawGroupImmediately);
            }
        }

        oEdges.EdgeAdded -= oEdgeAddedEventHandler;
        oEdges.EdgeRemoved -= oEdgeRemovedEventHandler;
        oVertices.VertexAdded -= oVertexAddedEventHandler;
        oVertices.VertexRemoved -= oVertexRemovedEventHandler;
    }

    //*************************************************************************
    //  Method: ForceLayout()
    //
    /// <summary>
    /// Forces the NodeXLControl to lay out and draw the graph again.
    /// </summary>
    //*************************************************************************

    protected void
    ForceLayout()
    {
        AssertValid();

        if (oNodeXLControl.IsLayingOutGraph)
        {
            return;
        }

        if (LayoutIsNull)
        {
            ShowLayoutTypeIsNullWarning("lay out the graph again");
            return;
        }

        // This is in case another open workbook has modified the user
        // settings.

        ApplyLayoutUserSettings( new LayoutUserSettings() );

        oNodeXLControl.DrawGraph(true);
    }

    //*************************************************************************
    //  Method: ForceLayoutSelected()
    //
    /// <summary>
    /// Forces the NodeXLControl to lay out selected vertices in the graph
    /// again.
    /// </summary>
    //*************************************************************************

    protected void
    ForceLayoutSelected()
    {
        AssertValid();

        ForceLayoutTheseVerticesOnly(oNodeXLControl.SelectedVertices,
            "lay out the selected vertices again");
    }

    //*************************************************************************
    //  Method: ForceLayoutSelectedWithinBounds()
    //
    /// <summary>
    /// Forces the NodeXLControl to lay out selected vertices in the graph
    /// again, within the vertices' bounding box.
    /// </summary>
    //*************************************************************************

    protected void
    ForceLayoutSelectedWithinBounds()
    {
        AssertValid();

        // The combination of the LayOutTheseVerticesWithinBounds key added to
        // the graph by this method and the LayOutTheseVerticesOnly key added
        // later cause the selected vertices to be laid out within their
        // bounding box.
        //
        // When the graph layout completes (which happens asynchronously),
        // oNodeXLControl_GraphLaidOut() removes both keys.

        oNodeXLControl.Graph.SetValue(
            ReservedMetadataKeys.LayOutTheseVerticesWithinBounds, null);

        ForceLayoutSelected();
    }

    //*************************************************************************
    //  Method: ForceLayoutVisible()
    //
    /// <summary>
    /// Forces the NodeXLControl to lay out visible vertices in the graph
    /// again.
    /// </summary>
    //*************************************************************************

    protected void
    ForceLayoutVisible()
    {
        AssertValid();

        ForceLayoutTheseVerticesOnly(
            NodeXLControlUtil.GetVisibleVertices(oNodeXLControl),
            "lay out the visible vertices again");
    }

    //*************************************************************************
    //  Method: ForceLayoutTheseVerticesOnly()
    //
    /// <summary>
    /// Forces the NodeXLControl to lay out visible vertices in the graph
    /// again.
    /// </summary>
    ///
    /// <param name="oVerticesToLayOut">
    /// The vertices to lay out.
    /// </param>
    ///
    /// <param name="sLayoutTypeIsNullWarning">
    /// Warning to show if the layout is LayoutType.Null.
    /// </param>
    //*************************************************************************

    protected void
    ForceLayoutTheseVerticesOnly
    (
        ICollection<IVertex> oVerticesToLayOut,
        String sLayoutTypeIsNullWarning
    )
    {
        Debug.Assert(oVerticesToLayOut != null);
        Debug.Assert( !String.IsNullOrEmpty(sLayoutTypeIsNullWarning) );
        AssertValid();

        if (oNodeXLControl.IsLayingOutGraph)
        {
            return;
        }

        if (LayoutIsNull)
        {
            ShowLayoutTypeIsNullWarning(sLayoutTypeIsNullWarning);
            return;
        }

        // This method works by adding a value to the graph specifying that
        // only the specified vertices should be laid out and all other
        // vertices should be completely ignored.
        //
        // When the graph layout completes (which happens asynchronously),
        // oNodeXLControl_GraphLaidOut() removes the value.

        oNodeXLControl.Graph.SetValue(
            ReservedMetadataKeys.LayOutTheseVerticesOnly, oVerticesToLayOut);

        ForceLayout();
    }

    //*************************************************************************
    //  Method: EditGeneralUserSettings()
    //
    /// <summary>
    /// Shows the dialog that lets the user edit his general settings.
    /// </summary>
    //*************************************************************************

    protected void
    EditGeneralUserSettings()
    {
        AssertValid();

        if (oNodeXLControl.IsLayingOutGraph)
        {
            return;
        }

        GeneralUserSettings oGeneralUserSettings = new GeneralUserSettings();

        GeneralUserSettingsDialog oGeneralUserSettingsDialog =
            new GeneralUserSettingsDialog(oGeneralUserSettings, m_oWorkbook);

        if (oGeneralUserSettingsDialog.ShowDialog() == DialogResult.OK)
        {
            oGeneralUserSettings.Save();
            ApplyGeneralUserSettings(oGeneralUserSettings);
            oNodeXLControl.DrawGraph();
        }
    }

    //*************************************************************************
    //  Method: EditLayoutUserSettings()
    //
    /// <summary>
    /// Shows the dialog that lets the user edit his layout settings.
    /// </summary>
    //*************************************************************************

    protected void
    EditLayoutUserSettings()
    {
        AssertValid();

        if (oNodeXLControl.IsLayingOutGraph)
        {
            return;
        }

        LayoutUserSettings oLayoutUserSettings = new LayoutUserSettings();

        LayoutUserSettingsDialog oLayoutUserSettingsDialog =
            new LayoutUserSettingsDialog(oLayoutUserSettings);

        if (oLayoutUserSettingsDialog.ShowDialog() == DialogResult.OK)
        {
            oLayoutUserSettings.Save();
            ApplyLayoutUserSettings(oLayoutUserSettings);
            oNodeXLControl.DrawGraph();
        }
    }

    //*************************************************************************
    //  Method: ShowLayoutTypeIsNullWarning()
    //
    /// <summary>
    /// Warns the user that the layout is LayoutType.Null.
    /// </summary>
    ///
    /// <param name="sAction">
    /// Action that the user can't perform because of the layout type.
    /// </param>
    ///
    /// <remarks>
    /// Call this when an action can't be performed because the layout is
    /// LayoutType.Null.
    /// </remarks>
    //*************************************************************************

    protected void
    ShowLayoutTypeIsNullWarning
    (
        String sAction
    )
    {
        Debug.Assert( !String.IsNullOrEmpty(sAction) );
        AssertValid();

        FormUtil.ShowWarning(
            "The Layout is set to None.  Before you can " + sAction + 
                ", you must select a different Layout.\r\n\r\n"
                + HowToSetLayoutType
            );
    }

    //*************************************************************************
    //  Method: ShowLayoutTypeIsNullNotification()
    //
    /// <summary>
    /// Notifies the user that the layout is LayoutType.Null.
    /// </summary>
    ///
    /// <returns>
    /// true if the user wants to read the workbook even though the layout is
    /// LayoutType.Null, false if he wants to cancel.
    /// </returns>
    ///
    /// <remarks>
    /// Call this when the user attempts to read the workbook when the layout
    /// is LayoutType.Null.
    /// </remarks>
    //*************************************************************************

    protected Boolean
    ShowLayoutTypeIsNullNotification()
    {
        AssertValid();

        NotificationUserSettings oNotificationUserSettings =
            new NotificationUserSettings();

        if (!oNotificationUserSettings.LayoutTypeIsNull)
        {
            // The user doesn't want to be notified.

            return (true);
        }

        Boolean bReturn = true;

        const String Message =
            "The Layout is set to None.  If the graph has never been laid"
            + " out, all the vertices will be placed at the upper-left corner"
            + " of the graph pane."
            + "\r\n\r\n"
            + "If you want to lay out the graph, click No and select a"
            + " different Layout.  "
            + HowToSetLayoutType
            + "\r\n\r\n"
            + "Do you want to read the workbook?"
            ;

        NotificationDialog oNotificationDialog = new NotificationDialog(
            "Layout is None", SystemIcons.Warning, Message);

        if (oNotificationDialog.ShowDialog() != DialogResult.Yes)
        {
            bReturn = false;
        }

        if (oNotificationDialog.DisableFutureNotifications)
        {
            oNotificationUserSettings.LayoutTypeIsNull = false;
            oNotificationUserSettings.Save();
        }

        return (bReturn);
    }

    //*************************************************************************
    //  Method: ApplyLayoutUserSettings()
    //
    /// <summary>
    /// Applies the user's layout settings to the NodeXLControl.
    /// </summary>
    ///
    /// <param name="oLayoutUserSettings">
    /// The user's layout settings.
    /// </param>
    //*************************************************************************

    protected void
    ApplyLayoutUserSettings
    (
        LayoutUserSettings oLayoutUserSettings
    )
    {
        Debug.Assert(oLayoutUserSettings != null);
        AssertValid();

        // Make sure the two layout managers are in sync.

        Debug.Assert(m_oLayoutManagerForToolStripSplitButton.Layout ==
            m_oLayoutManagerForContextMenu.Layout);

        // Either layout manager will work; arbitrarily use one of them to
        // create a layout.

        IAsyncLayout oLayout =
            m_oLayoutManagerForToolStripSplitButton.CreateLayout();

        oLayoutUserSettings.TransferToLayout(oLayout);
        oNodeXLControl.Layout = oLayout;
    }

    //*************************************************************************
    //  Method: ApplyGeneralUserSettings()
    //
    /// <summary>
    /// Applies the user's general settings to the NodeXLControl.
    /// </summary>
    ///
    /// <param name="oGeneralUserSettings">
    /// The user's general settings.
    /// </param>
    //*************************************************************************

    protected void
    ApplyGeneralUserSettings
    (
        GeneralUserSettings oGeneralUserSettings
    )
    {
        Debug.Assert(oGeneralUserSettings != null);
        AssertValid();

        oGeneralUserSettings.TransferToNodeXLWithAxesControl(
            m_oNodeXLWithAxesControl);
    }

    //*************************************************************************
    //  Method: EnableGraphControls()
    //
    /// <summary>
    /// Enables or disables the controls used to interact with the graph.
    /// </summary>
    ///
    /// <param name="bEnable">
    /// true to enable the controls, false to disable them.
    /// </param>
    //*************************************************************************

    protected void
    EnableGraphControls
    (
        Boolean bEnable
    )
    {
        AssertValid();

        // A m_iEnableGraphControlsCount value of zero or greater should enable
        // the controls.

        if (bEnable)
        {
            m_iEnableGraphControlsCount++;
        }
        else
        {
            m_iEnableGraphControlsCount--;
        }

        Boolean bEnable2 = (m_iEnableGraphControlsCount >= 0);

        if (bEnable2)
        {
            Boolean bNonEmptyWorkbookRead = this.NonEmptyWorkbookRead;

            tssbForceLayout.Enabled = bNonEmptyWorkbookRead;

            tsbShowDynamicFilters.Enabled =
                m_oRibbon.EnableShowDynamicFilters =
                msiContextShowDynamicFilters.Enabled =
                (bNonEmptyWorkbookRead && m_iTemplateVersion >= 58);
        }

        tsGeneral.Enabled = tsGraphNavigation.Enabled = bEnable2;

        this.UseWaitCursor = !bEnable2;

        // Setting this.UseWaitCursor affects the cursor when the mouse is
        // over a ToolStrip, but not when it's over the NodeXLControl.

        oNodeXLControl.Cursor =
            bEnable2 ? null : System.Windows.Input.Cursors.Wait;
    }

    //*************************************************************************
    //  Method: EnableContextMenuItems()
    //
    /// <summary>
    /// Enables the menu items on the the cmsNodeXLControl context menu.
    /// </summary>
    ///
    /// <param name="oClickedVertex">
    /// Vertex that was clicked, or null if a vertex wasn't clicked.
    /// </param>
    //*************************************************************************

    protected void
    EnableContextMenuItems
    (
        IVertex oClickedVertex
    )
    {
        AssertValid();

        // Base name of custom menu items.

        const String CustomMenuItemNameBase = "msiCustom";

        // Remove any custom menu items that were added in a previous call to
        // this method.

        ToolStripItemCollection oItems = cmsNodeXLControl.Items;
        Int32 iItems = oItems.Count;

        for (Int32 i = iItems - 1; i >= 0; i--)
        {
            ToolStripItem oItem = oItems[i];

            if ( oItem.Name.StartsWith(CustomMenuItemNameBase) )
            {
                oItems.Remove(oItem);
            }
        }

        Boolean bNonEmptyWorkbookRead = this.NonEmptyWorkbookRead;

        ICollection<IVertex> oSelectedVertices =
            oNodeXLControl.SelectedVertices;

        Int32 iSelectedVertices = oSelectedVertices.Count;

        // Selecting a vertex's incident edges makes sense only if they are not
        // automatically selected when the vertex is clicked.

        msiContextSelectIncidentEdges.Visible =
            !( new GeneralUserSettings() ).AutoSelect;

        // Selectively enable menu items.

        Boolean bEnableSelectAllVertices, bEnableDeselectAllVertices,
            bEnableSelectAdjacentVertices, bEnableDeselectAdjacentVertices,
            bEnableSelectIncidentEdges, bEnableDeselectIncidentEdges,
            bEnableEditVertexAttributes, bEnableSelectSubgraphs;

        GetVertexCommandEnableFlags(oClickedVertex,
            out bEnableSelectAllVertices, out bEnableDeselectAllVertices,
            out bEnableSelectAdjacentVertices,
            out bEnableDeselectAdjacentVertices,
            out bEnableSelectIncidentEdges,
            out bEnableDeselectIncidentEdges,
            out bEnableEditVertexAttributes,
            out bEnableSelectSubgraphs
            );

        Boolean bEnableSelectAllEdges, bEnableDeselectAllEdges, bNotUsed1,
            bNotUsed2;

        GetEdgeCommandEnableFlags(null, out bEnableSelectAllEdges,
            out bEnableDeselectAllEdges, out bNotUsed1, out bNotUsed2);

        msiContextSelectAllVertices.Enabled = bEnableSelectAllVertices;
        msiContextDeselectAllVertices.Enabled = bEnableDeselectAllVertices;

        msiContextSelectAdjacentVertices.Enabled =
            bEnableSelectAdjacentVertices;

        msiContextDeselectAdjacentVertices.Enabled =
            bEnableDeselectAdjacentVertices;

        msiContextSelectIncidentEdges.Enabled =
            bEnableSelectIncidentEdges;

        msiContextDeselectIncidentEdges.Enabled =
            bEnableDeselectIncidentEdges;

        msiContextEditVertexAttributes.Enabled = bEnableEditVertexAttributes;

        msiContextSelectSubgraphs.Enabled = bEnableSelectSubgraphs;

        MenuUtil.EnableToolStripMenuItems(
            bNonEmptyWorkbookRead && iSelectedVertices > 0,
            msiContextForceLayoutSelected
            );

        MenuUtil.EnableToolStripMenuItems(
            bNonEmptyWorkbookRead,
            msiContextForceLayout,
            msiContextForceLayoutVisible,
            msiContextSelectAll,
            msiContextDeselectAll
            );

        MenuUtil.EnableToolStripMenuItems(
            bNonEmptyWorkbookRead && iSelectedVertices > 1,
            msiContextForceLayoutSelectedWithinBounds
            );

        msiContextSelectAllEdges.Enabled = bEnableSelectAllEdges;
        msiContextDeselectAllEdges.Enabled = bEnableDeselectAllEdges;

        MenuUtil.EnableToolStripMenuItems(

            msiContextSelectAllVertices.Enabled ||
                msiContextSelectAllEdges.Enabled,

            msiContextSelectAllVerticesAndEdges
            );

        MenuUtil.EnableToolStripMenuItems(

            msiContextDeselectAllVertices.Enabled ||
                msiContextDeselectAllEdges.Enabled,

            msiContextDeselectAllVerticesAndEdges
            );

        // Store the clicked vertex (or null if no vertex was clicked) in the
        // menu items' Tag property for use by the menu item's Click handler.

        msiContextSelectAdjacentVertices.Tag =
            msiContextDeselectAdjacentVertices.Tag =
            msiContextSelectIncidentEdges.Tag =
            msiContextDeselectIncidentEdges.Tag =
            msiContextSelectSubgraphs.Tag =
            oClickedVertex;

        if (iSelectedVertices == 1)
        {
            // Look for custom menu item information in the vertex's metadata.

            Object oCustomMenuItemInformationAsObject;

            IVertex oSelectedVertex = oSelectedVertices.First();

            if ( oSelectedVertex.TryGetValue(
                    ReservedMetadataKeys.CustomContextMenuItems,
                    typeof( KeyValuePair<String, String>[] ),
                    out oCustomMenuItemInformationAsObject) )
            {
                // List of string pairs, one pair for each custom menu item to
                // add to the vertex's context menu.  The key is the custom
                // menu item text and the value is the custom menu item action.

                KeyValuePair<String, String> [] aoCustomMenuItemInformation =
                    ( KeyValuePair<String, String> [] )
                        oCustomMenuItemInformationAsObject;

                if (aoCustomMenuItemInformation.Length > 0)
                {
                    // Add a separator before the custom menu items.

                    ToolStripSeparator oToolStripSeparator =
                        new ToolStripSeparator();

                    oToolStripSeparator.Name =
                        CustomMenuItemNameBase + "Separator";

                    cmsNodeXLControl.Items.Add(oToolStripSeparator);
                }

                Int32 i = 0;

                foreach (KeyValuePair<String, String> oKeyValuePair in
                    aoCustomMenuItemInformation)
                {
                    // Add a custom menu item.  The tag stores the custom
                    // action.

                    String sCustomMenuItemText = oKeyValuePair.Key;
                    String sCustomMenuItemAction = oKeyValuePair.Value;

                    ToolStripMenuItem oToolStripMenuItem =
                        new ToolStripMenuItem(sCustomMenuItemText);

                    oToolStripMenuItem.Name =
                        CustomMenuItemNameBase + i.ToString();

                    oToolStripMenuItem.Tag = sCustomMenuItemAction;

                    oToolStripMenuItem.Click +=
                        new EventHandler(this.msiContextCustomMenuItem_Click);

                    cmsNodeXLControl.Items.Add(oToolStripMenuItem);

                    i++;
                }
            }
        }
    }

    //*************************************************************************
    //  Method: GetVertexCommandEnableFlags()
    //
    /// <summary>
    /// Determines which menu items should be enabled when a vertex is right-
    /// clicked.
    /// </summary>
    ///
    /// <param name="oClickedVertex">
    /// Vertex that was clicked, or null if a vertex wasn't clicked.
    /// </param>
    ///
    /// <param name="bEnableSelectAllVertices">
    /// Gets set to true if Select All Vertices should be enabled.
    /// </param>
    ///
    /// <param name="bEnableDeselectAllVertices">
    /// Gets set to true if Deselect All Vertices should be enabled.
    /// </param>
    ///
    /// <param name="bEnableSelectAdjacentVertices">
    /// Gets set to true if Select Adjacent Vertices should be enabled.
    /// </param>
    ///
    /// <param name="bEnableDeselectAdjacentVertices">
    /// Gets set to true if Deselect Adjacent Vertices should be enabled.
    /// </param>
    ///
    /// <param name="bEnableSelectIncidentEdges">
    /// Gets set to true if Select Incident Edges should be enabled.
    /// </param>
    ///
    /// <param name="bEnableDeselectIncidentEdges">
    /// Gets set to true if Deselect Incident Edges should be enabled.
    /// </param>
    ///
    /// <param name="bEnableEditVertexAttributes">
    /// Gets set to true if Edit Selected Vertex Attributes should be enabled.
    /// </param>
    ///
    /// <param name="bEnableSelectSubgraphs">
    /// Gets set to true if Select Subgraphs should be enabled.
    /// </param>
    //*************************************************************************

    protected void
    GetVertexCommandEnableFlags
    (
        IVertex oClickedVertex,
        out Boolean bEnableSelectAllVertices,
        out Boolean bEnableDeselectAllVertices,
        out Boolean bEnableSelectAdjacentVertices,
        out Boolean bEnableDeselectAdjacentVertices,
        out Boolean bEnableSelectIncidentEdges,
        out Boolean bEnableDeselectIncidentEdges,
        out Boolean bEnableEditVertexAttributes,
        out Boolean bEnableSelectSubgraphs
    )
    {
        AssertValid();

        if (oNodeXLControl.IsLayingOutGraph || !this.NonEmptyWorkbookRead)
        {
            bEnableSelectAllVertices = bEnableDeselectAllVertices =
                bEnableSelectAdjacentVertices =
                bEnableDeselectAdjacentVertices = bEnableSelectIncidentEdges =
                bEnableDeselectIncidentEdges = bEnableEditVertexAttributes =
                bEnableSelectSubgraphs =
                false;

            return;
        }

        Int32 iSelectedVertices = oNodeXLControl.SelectedVertices.Count;
        Boolean bVertexClicked = (oClickedVertex != null);

        bEnableSelectAllVertices = true;
        bEnableDeselectAllVertices = (iSelectedVertices > 0);
        bEnableSelectAdjacentVertices = bVertexClicked;
        bEnableDeselectAdjacentVertices = bVertexClicked;
        bEnableSelectIncidentEdges = bVertexClicked;
        bEnableDeselectIncidentEdges = bVertexClicked;
        bEnableEditVertexAttributes = (iSelectedVertices > 0);
        bEnableSelectSubgraphs = (iSelectedVertices > 0);
    }

    //*************************************************************************
    //  Method: GetEdgeCommandEnableFlags()
    //
    /// <summary>
    /// Determines which menu items should be enabled when an edge is right-
    /// clicked.
    /// </summary>
    ///
    /// <param name="oClickedEdge">
    /// Edge that was clicked, or null if an edge wasn't clicked.
    /// </param>
    ///
    /// <param name="bEnableSelectAllEdges">
    /// Gets set to true if Select All Edges should be enabled.
    /// </param>
    ///
    /// <param name="bEnableDeselectAllEdges">
    /// Gets set to true if Deselect All Edges should be enabled.
    /// </param>
    ///
    /// <param name="bEnableSelectAdjacentVertices">
    /// Gets set to true if Select Adjacent Vertices should be enabled.
    /// </param>
    ///
    /// <param name="bEnableDeselectAdjacentVertices">
    /// Gets set to true if Deselect Adjacent Vertices should be enabled.
    /// </param>
    //*************************************************************************

    protected void
    GetEdgeCommandEnableFlags
    (
        IEdge oClickedEdge,
        out Boolean bEnableSelectAllEdges,
        out Boolean bEnableDeselectAllEdges,
        out Boolean bEnableSelectAdjacentVertices,
        out Boolean bEnableDeselectAdjacentVertices
    )
    {
        AssertValid();

        if (oNodeXLControl.IsLayingOutGraph)
        {
            bEnableSelectAllEdges = bEnableDeselectAllEdges =
                bEnableSelectAdjacentVertices =
                bEnableDeselectAdjacentVertices =
                false;

            return;
        }

        Int32 iEdges = oNodeXLControl.Graph.Edges.Count;
        Int32 iSelectedEdges = oNodeXLControl.SelectedEdges.Count;
        Boolean bEdgeClicked = (oClickedEdge != null);

        bEnableSelectAllEdges = (iEdges > 0);
        bEnableDeselectAllEdges = (iSelectedEdges > 0);
        bEnableSelectAdjacentVertices = bEdgeClicked;
        bEnableDeselectAdjacentVertices = bEdgeClicked;
    }

    //*************************************************************************
    //  Method: CopyGraphBitmap()
    //
    /// <summary>
    /// Copies the graph bitmap to the clipboard.
    /// </summary>
    //*************************************************************************

    protected void
    CopyGraphBitmap()
    {
        AssertValid();

        if (oNodeXLControl.IsLayingOutGraph)
        {
            return;
        }

        // Get the size of the bitmap.

        Size oBitmapSize = ehNodeXLControlHost.ClientSize;
        Int32 iWidthPx = oBitmapSize.Width;
        Int32 iHeightPx = oBitmapSize.Height;

        if (iWidthPx == 0 || iHeightPx == 0)
        {
            // The size is unusable.

            FormUtil.ShowWarning(
                "The graph is too small to copy.  Make the graph window"
                + " larger."
                );

            return;
        }

        // Tell the NodeXLControl to copy its graph to a bitmap.

        Bitmap oBitmapCopy =
            oNodeXLControl.CopyGraphToBitmap(iWidthPx, iHeightPx);

        // Copy the bitmap to the clipboard.

        Clipboard.SetDataObject(oBitmapCopy);

        // Note: Do not call oBitmapCopy.Dispose().
    }

    //*************************************************************************
    //  Method: SetImageSize()
    //
    /// <summary>
    /// Shows a dialog for setting the size of images saved to a file.
    /// </summary>
    //*************************************************************************

    protected void
    SetImageSize()
    {
        AssertValid();

        if (oNodeXLControl.IsLayingOutGraph)
        {
            return;
        }

        // Allow the user to edit the graph image settings.

        GraphImageUserSettings oGraphImageUserSettings =
            new GraphImageUserSettings();

        Size oNodeXLControlSizePx = ehNodeXLControlHost.ClientSize;

        GraphImageUserSettingsDialog oGraphImageUserSettingsDialog =
            new GraphImageUserSettingsDialog(oGraphImageUserSettings,
                oNodeXLControlSizePx);

        if (oGraphImageUserSettingsDialog.ShowDialog() == DialogResult.OK)
        {
            // Save the new settings.

            oGraphImageUserSettings.Save();
        }
    }

    //*************************************************************************
    //  Method: SaveImage()
    //
    /// <summary>
    /// Saves the graph image to a file.
    /// </summary>
    //*************************************************************************

    protected void
    SaveImage()
    {
        AssertValid();

        if (oNodeXLControl.IsLayingOutGraph)
        {
            return;
        }

        // Get the size of the image.

        GraphImageUserSettings oGraphImageUserSettings =
            new GraphImageUserSettings();

        Int32 iWidth, iHeight;

        if (oGraphImageUserSettings.UseControlSize)
        {
            Size oNodeXLControlSizePx = ehNodeXLControlHost.ClientSize;
            iWidth = oNodeXLControlSizePx.Width;
            iHeight = oNodeXLControlSizePx.Height;

            if (iWidth == 0 || iHeight == 0)
            {
                // The size is unusable.

                FormUtil.ShowWarning(
                    "The graph is too small to save.  Make the graph window"
                    + " larger."
                    );

                return;
            }
        }
        else
        {
            Size oImageSize = oGraphImageUserSettings.ImageSize;
            iWidth = oImageSize.Width;
            iHeight = oImageSize.Height;
        }

        if (m_oSaveGraphImageFileDialog == null)
        {
            m_oSaveGraphImageFileDialog =
                new SaveGraphImageFileDialog(String.Empty, "GraphImage");

            m_oSaveGraphImageFileDialog.DialogTitle = "Save Image to File";
        }

        m_oSaveGraphImageFileDialog.ShowDialogAndSaveGraphImage(oNodeXLControl,
            iWidth, iHeight);
    }

    //*************************************************************************
    //  Method: SelectAllVertices()
    //
    /// <summary>
    /// Selects or deselects all vertices.
    /// </summary>
    ///
    /// <param name="bSelect">
    /// true to select, false to deselect.
    /// </param>
    //*************************************************************************

    protected void
    SelectAllVertices
    (
        Boolean bSelect
    )
    {
        AssertValid();

        oNodeXLControl.SetSelected(

            bSelect ? ( IEnumerable<IVertex> )oNodeXLControl.Graph.Vertices :
                ( IEnumerable<IVertex> )new IVertex[0],

            oNodeXLControl.SelectedEdges
            );
    }

    //*************************************************************************
    //  Method: SelectAllEdges()
    //
    /// <summary>
    /// Selects or deselects all edges.
    /// </summary>
    ///
    /// <param name="bSelect">
    /// true to select, false to deselect.
    /// </param>
    //*************************************************************************

    protected void
    SelectAllEdges
    (
        Boolean bSelect
    )
    {
        AssertValid();

        oNodeXLControl.SetSelected(

            oNodeXLControl.SelectedVertices,

            bSelect ? ( IEnumerable<IEdge> )oNodeXLControl.Graph.Edges :
                ( IEnumerable<IEdge> )new IEdge[0]
            );
    }

    //*************************************************************************
    //  Method: SelectAllVerticesAndEdges()
    //
    /// <summary>
    /// Selects or deselects all vertices and edges.
    /// </summary>
    ///
    /// <param name="bSelect">
    /// true to select, false to deselect.
    /// </param>
    //*************************************************************************

    protected void
    SelectAllVerticesAndEdges
    (
        Boolean bSelect
    )
    {
        AssertValid();

        if (bSelect)
        {
            oNodeXLControl.SelectAll();
        }
        else
        {
            oNodeXLControl.DeselectAll();
        }
    }

    //*************************************************************************
    //  Method: SelectIncidentEdges()
    //
    /// <summary>
    /// Selects or deselects the edges incident to a specified vertex.
    /// </summary>
    ///
    /// <param name="oVertex">
    /// Vertex whose incident edges should be selected or deselected.
    /// </param>
    ///
    /// <param name="bSelect">
    /// true to select, false to deselect.
    /// </param>
    //*************************************************************************

    protected void
    SelectIncidentEdges
    (
        IVertex oVertex,
        Boolean bSelect
    )
    {
        AssertValid();

        // Copy the selected edges to a HashSet.  The key is the IEdge.  A
        // HashSet is used to prevent the same edge from being selected twice.

        HashSet<IEdge> oSelectedEdges =
            NodeXLControlUtil.GetSelectedEdgesAsHashSet(oNodeXLControl);

        // Add or subtract the specified vertex's incident edges from the
        // HashSet of selected edges.

        foreach (IEdge oIncidentEdge in oVertex.IncidentEdges)
        {
            if (bSelect)
            {
                oSelectedEdges.Add(oIncidentEdge);
            }
            else
            {
                oSelectedEdges.Remove(oIncidentEdge);
            }
        }

        // Replace the selection.

        oNodeXLControl.SetSelected(oNodeXLControl.SelectedVertices,
            oSelectedEdges);
    }

    //*************************************************************************
    //  Method: SelectAdjacentVertices()
    //
    /// <summary>
    /// Selects or deselects the vertices adjacent to a specified vertex.
    /// </summary>
    ///
    /// <param name="oVertex">
    /// Vertex whose adjacent vertices should be selected or deselected.
    /// </param>
    ///
    /// <param name="bSelect">
    /// true to select, false to deselect.
    /// </param>
    //*************************************************************************

    protected void
    SelectAdjacentVertices
    (
        IVertex oVertex,
        Boolean bSelect
    )
    {
        Debug.Assert(oVertex != null);
        AssertValid();

        // Copy the selected vertices to a HashSet.  The key is the IVertex.
        // A HashSet is used to prevent the same vertex from being selected
        // twice.

        HashSet<IVertex> oSelectedVertices =
            NodeXLControlUtil.GetSelectedVerticesAsHashSet(oNodeXLControl);

        // Add or subtract the specified vertex's adjacent vertices from the
        // HashSet of selected vertices.

        foreach (IVertex oAdjacentVertex in oVertex.AdjacentVertices)
        {
            if (bSelect)
            {
                oSelectedVertices.Add(oAdjacentVertex);
            }
            else
            {
                oSelectedVertices.Remove(oAdjacentVertex);
            }
        }

        // Replace the selection.

        oNodeXLControl.SetSelected(oSelectedVertices,
            oNodeXLControl.SelectedEdges);
    }

    //*************************************************************************
    //  Method: SelectAdjacentVertices()
    //
    /// <summary>
    /// Selects or deselects the vertices adjacent to a specified edge.
    /// </summary>
    ///
    /// <param name="oEdge">
    /// Edge whose adjacent vertices should be selected or deselected.
    /// </param>
    ///
    /// <param name="bSelect">
    /// true to select, false to deselect.
    /// </param>
    //*************************************************************************

    protected void
    SelectAdjacentVertices
    (
        IEdge oEdge,
        Boolean bSelect
    )
    {
        Debug.Assert(oEdge != null);
        AssertValid();

        // Copy the selected vertices to a HashSet.  The key is the IVertex.
        // A HashSet is used to prevent the same vertex from being selected
        // twice.

        HashSet<IVertex> oSelectedVertices =
            NodeXLControlUtil.GetSelectedVerticesAsHashSet(oNodeXLControl);

        // Add or subtract the specified edge's adjacent vertices from the
        // HashSet of selected vertices.

        foreach (IVertex oAdjacentVertex in oEdge.Vertices)
        {
            if (bSelect)
            {
                oSelectedVertices.Add(oAdjacentVertex);
            }
            else
            {
                oSelectedVertices.Remove(oAdjacentVertex);
            }
        }

        // Replace the selection.

        oNodeXLControl.SetSelected(oSelectedVertices,
            oNodeXLControl.SelectedEdges);
    }

    //*************************************************************************
    //  Method: SelectSubgraphs()
    //
    /// <summary>
    /// Shows a dialog for selecting subgraphs.
    /// </summary>
    ///
    /// <param name="oClickedVertex">
    /// Vertex that was clicked, or null if a vertex wasn't clicked.
    /// </param>
    //*************************************************************************

    protected void
    SelectSubgraphs
    (
        IVertex oClickedVertex
    )
    {
        AssertValid();

        SelectSubgraphsDialog oSelectSubgraphsDialog =
            new SelectSubgraphsDialog(oNodeXLControl, oClickedVertex);

        oSelectSubgraphsDialog.ShowDialog();
    }

    //*************************************************************************
    //  Method: EditVertexAttributes()
    //
    /// <summary>
    /// Opens a dialog for editing the attributes of the selected vertices.
    /// </summary>
    //*************************************************************************

    protected void
    EditVertexAttributes()
    {
        AssertValid();

        VertexAttributesDialog oVertexAttributesDialog =
            new VertexAttributesDialog(oNodeXLControl);

        if (oVertexAttributesDialog.ShowDialog() != DialogResult.OK)
        {
            return;
        }

        VertexAttributesEditedEventHandler oVertexAttributesEditedInGraph =
            this.VertexAttributesEditedInGraph;

        if (oVertexAttributesEditedInGraph == null)
        {
            // There is no event handler, so there is nothing to do.

            return;
        }

        // Get a collection of the row IDs of the selected vertices.  These row IDs
        // came from the vertex worksheet's ID column.

        ICollection<Int32> oSelectedVertexRowIDs =
            NodeXLControlUtil.GetSelectedVertexRowIDs(oNodeXLControl);

        if (oSelectedVertexRowIDs.Count == 0)
        {
            return;
        }

        // Get the list of attributes that were applied to the selected
        // vertices.

        EditedVertexAttributes oEditedVertexAttributes =
            oVertexAttributesDialog.EditedVertexAttributes;

        oVertexAttributesEditedInGraph( this,
            new VertexAttributesEditedEventArgs(
                oSelectedVertexRowIDs.ToArray(), oEditedVertexAttributes ) );

        if (oEditedVertexAttributes.WorkbookMustBeReread)
        {
            ReadWorkbook();
        }
    }

    //*************************************************************************
    //  Method: ShowDynamicFilters()
    //
    /// <summary>
    /// Shows the dynamic filters dialog.
    /// </summary>
    //*************************************************************************

    private void
    ShowDynamicFilters()
    {
        AssertValid();

        if (oNodeXLControl.IsLayingOutGraph || !this.NonEmptyWorkbookRead)
        {
            return;
        }

        if (m_oDynamicFilterDialog == null)
        {
            // The dialog is created on demand.

            m_oDynamicFilterDialog = new DynamicFilterDialog(m_oWorkbook);

            m_oDynamicFilterDialog.DynamicFilterColumnsChanged +=
                new DynamicFilterColumnsChangedEventHandler(
                    m_oDynamicFilterDialog_DynamicFilterColumnsChanged);

            m_oDynamicFilterDialog.FilteredAlphaChanged +=
                new EventHandler(m_oDynamicFilterDialog_FilteredAlphaChanged);

            m_oDynamicFilterDialog.FormClosed +=
                new FormClosedEventHandler(
                    m_oDynamicFilterDialog_FormClosed);

            // Create a dictionary of edges that have been filtered by edge
            // filters, and a dictionary of vertices that have been filtered by
            // vertex filters.
            //
            // The key is the IEdge.ID or IVertex.ID and the value isn't used.
            // Store the dictionaries within the dialog so they are
            // automatically destroyed when the dialog is destroyed.

            m_oDynamicFilterDialog.Tag = new Dictionary<Int32, Char> [] {
                new Dictionary<Int32, Char>(),
                new Dictionary<Int32, Char>()
                };

            m_oDynamicFilterDialog.Show(this);

            // If the dialog throws an exception during initialization,
            // m_oDynamicFilterDialog gets set to null by
            // m_oDynamicFilterDialog_FormClosed().

            if (m_oDynamicFilterDialog != null)
            {
                ReadDynamicFilterColumns(false);
                ReadFilteredAlpha(true);
                UpdateDynamicFiltersLegend();
            }
        }
        else
        {
            m_oDynamicFilterDialog.Activate();
        }
    }

    //*************************************************************************
    //  Method: UpdateAutoFillResultsLegend()
    //
    /// <summary>
    /// Updates the autofill results legend control.
    /// </summary>
    ///
    /// <param name="oPerWorkbookSettings">
    /// Provides access to settings that are stored on a per-workbook basis.
    /// </param>
    //*************************************************************************

    protected void
    UpdateAutoFillResultsLegend
    (
        PerWorkbookSettings oPerWorkbookSettings
    )
    {
        Debug.Assert(oPerWorkbookSettings != null);
        AssertValid();

        // The autofill and "autofill with scheme" results are stored in the
        // per-workbook settings.  Transfer them to the visual attributes
        // legend control, which can display either set of results but not
        // both.
        //
        // (The PerWorkbookSettings property setter for the autofill results
        // clears the "autofill with scheme" results, and vice versa.)

        AutoFillWorkbookResults oAutoFillWorkbookResults =
            oPerWorkbookSettings.AutoFillWorkbookResults;

        AutoFillWorkbookWithSchemeResults oAutoFillWorkbookWithSchemeResults =
            oPerWorkbookSettings.AutoFillWorkbookWithSchemeResults;

        if (oAutoFillWorkbookResults.AutoFilledNonXYColumnCount > 0)
        {
            usrAutoFillResultsLegend.Update(oAutoFillWorkbookResults);
        }
        else if (oAutoFillWorkbookWithSchemeResults.SchemeType !=
            AutoFillSchemeType.None)
        {
            usrAutoFillResultsLegend.Update(
                oAutoFillWorkbookWithSchemeResults);
        }
        else
        {
            usrAutoFillResultsLegend.Clear();
        }
    }

    //*************************************************************************
    //  Method: UpdateDynamicFiltersLegend()
    //
    /// <summary>
    /// Updates the dynamic filters legend control.
    /// </summary>
    //*************************************************************************

    protected void
    UpdateDynamicFiltersLegend()
    {
        AssertValid();

        if (m_oDynamicFilterDialog == null)
        {
            usrDynamicFiltersLegend.Clear();
            return;
        }
 
        // Transfer the track bar information from the DynamicFilterDialog to
        // the dynamic filters legend control.

        ICollection<IDynamicFilterRangeTrackBar>
            oEdgeDynamicFilterRangeTrackBars,
            oVertexDynamicFilterRangeTrackBars;

        m_oDynamicFilterDialog.GetDynamicFilterRangeTrackBars(
            out oEdgeDynamicFilterRangeTrackBars,
            out oVertexDynamicFilterRangeTrackBars);

        usrDynamicFiltersLegend.Update(oEdgeDynamicFilterRangeTrackBars,
            oVertexDynamicFilterRangeTrackBars);
    }

    //*************************************************************************
    //  Method: UpdateAxes()
    //
    /// <summary>
    /// Updates the graph's axes.
    /// </summary>
    ///
    /// <param name="oPerWorkbookSettings">
    /// Provides access to settings that are stored on a per-workbook basis.
    /// </param>
    //*************************************************************************

    protected void
    UpdateAxes
    (
        PerWorkbookSettings oPerWorkbookSettings
    )
    {
        Debug.Assert(oPerWorkbookSettings != null);
        AssertValid();

        AutoFillWorkbookResults oAutoFillWorkbookResults =
            oPerWorkbookSettings.AutoFillWorkbookResults;

        String sXSourceColumnName, sYSourceColumnName;

        Double dXSourceCalculationNumber1, dXSourceCalculationNumber2,
            dXDestinationNumber1, dXDestinationNumber2,
            dYSourceCalculationNumber1, dYSourceCalculationNumber2,
            dYDestinationNumber1, dYDestinationNumber2;

        AutoFillNumericRangeColumnResults oVertexXResults =
            oAutoFillWorkbookResults.VertexXResults;

        AutoFillNumericRangeColumnResults oVertexYResults =
            oAutoFillWorkbookResults.VertexYResults;

        if (oVertexXResults.ColumnAutoFilled)
        {
            sXSourceColumnName = oVertexXResults.SourceColumnName;

            dXSourceCalculationNumber1 =
                oVertexXResults.SourceCalculationNumber1;

            dXSourceCalculationNumber2 =
                oVertexXResults.SourceCalculationNumber2;

            dXDestinationNumber1 = oVertexXResults.DestinationNumber1;
            dXDestinationNumber2 = oVertexXResults.DestinationNumber2;

            // The X and Y columns are always autofilled together.

            Debug.Assert(oVertexYResults.ColumnAutoFilled);

            sYSourceColumnName = oVertexYResults.SourceColumnName;

            dYSourceCalculationNumber1 =
                oVertexYResults.SourceCalculationNumber1;

            dYSourceCalculationNumber2 =
                oVertexYResults.SourceCalculationNumber2;

            dYDestinationNumber1 = oVertexYResults.DestinationNumber1;
            dYDestinationNumber2 = oVertexYResults.DestinationNumber2;

        }
        else
        {
            // The X and Y columns weren't autofilled.  Use default axis
            // values.

            sXSourceColumnName = "X";
            sYSourceColumnName = "Y";

            dXSourceCalculationNumber1 = dXDestinationNumber1 =
                dYSourceCalculationNumber1 = dYDestinationNumber1 =
                VertexLocationConverter.MinimumXYWorkbook;

            dXSourceCalculationNumber2 = dXDestinationNumber2 =
                dYSourceCalculationNumber2 = dYDestinationNumber2 =
                VertexLocationConverter.MaximumXYWorkbook;
        }

        UpdateAxis(m_oNodeXLWithAxesControl.XAxis, sXSourceColumnName,
            dXSourceCalculationNumber1, dXSourceCalculationNumber2,
            dXDestinationNumber1, dXDestinationNumber2);

        UpdateAxis(m_oNodeXLWithAxesControl.YAxis, sYSourceColumnName,
            dYSourceCalculationNumber1, dYSourceCalculationNumber2,
            dYDestinationNumber1, dYDestinationNumber2);
    }

    //*************************************************************************
    //  Method: UpdateAxis()
    //
    /// <summary>
    /// Updates one of the graph's axes.
    /// </summary>
    ///
    /// <param name="oAxis">
    /// The axis to update.
    /// </param>
    ///
    /// <param name="sAxisLabel">
    /// The axis label.
    /// </param>
    ///
    /// <param name="dSourceCalculationNumber1">
    /// The actual first source number used in the calculations.
    /// </param>
    ///
    /// <param name="dSourceCalculationNumber2">
    /// The actual second source number used in the calculations.
    /// </param>
    ///
    /// <param name="dDestinationNumber1">
    /// The first number used in the destination column.
    /// </param>
    ///
    /// <param name="dDestinationNumber2">
    /// The second number used in the destination column.
    /// </param>
    //*************************************************************************

    protected void
    UpdateAxis
    (
        Microsoft.WpfGraphicsLib.Axis oAxis,
        String sAxisLabel,
        Double dSourceCalculationNumber1,
        Double dSourceCalculationNumber2,
        Double dDestinationNumber1,
        Double dDestinationNumber2
    )
    {
        Debug.Assert(oAxis != null);
        Debug.Assert(sAxisLabel != null);
        Debug.Assert(oNodeXLControl != null);
        // AssertValid();

        oAxis.Label = sAxisLabel;

        Double dAxisLength =
            oAxis.IsXAxis ? oAxis.ActualWidth : oAxis.ActualHeight;

        if (dAxisLength == 0)
        {
            // The axis control hasn't been drawn yet.

            oAxis.SetRange(VertexLocationConverter.MinimumXYWorkbook, 0,
                VertexLocationConverter.MaximumXYWorkbook, 0);

            return;
        }

        // dSourceCalculationNumber1 and dDestinationNumber1 are not
        // necessarily at the near end of the axis.

        Double dNearSourceCalculationNumber, dNearDestinationNumber,
            dFarSourceCalculationNumber, dFarDestinationNumber,
            dNearOffset;

        if (dDestinationNumber2 >= dDestinationNumber1)
        {
            dNearSourceCalculationNumber = dSourceCalculationNumber1;
            dNearDestinationNumber = dDestinationNumber1;

            dFarSourceCalculationNumber = dSourceCalculationNumber2;
            dFarDestinationNumber = dDestinationNumber2;
        }
        else
        {
            dNearSourceCalculationNumber = dSourceCalculationNumber2;
            dNearDestinationNumber = dDestinationNumber2;

            dFarSourceCalculationNumber = dSourceCalculationNumber1;
            dFarDestinationNumber = dDestinationNumber1;
        }

        // Use the point-slope equation of a line to map destination units to
        // WPF units.

        Double dX1 = VertexLocationConverter.MinimumXYWorkbook;
        Double dY1 = 0;
        Double dX2 = VertexLocationConverter.MaximumXYWorkbook;
        Double dY2 = dAxisLength;

        Debug.Assert(dX1 != dX2);
        Double dM = (dY1 - dY2) / (dX1 - dX2);

        dNearOffset = dY1 + dM * (dNearDestinationNumber - dX1);
        Double dFarX = dY1 + dM * (dFarDestinationNumber - dX1);

        // Use Math.Max() to fix rounding errors that result in very small
        // negative offsets, which are prohibited.

        oAxis.SetRange( dNearSourceCalculationNumber, Math.Max(dNearOffset, 0),
            dFarSourceCalculationNumber, Math.Max(dAxisLength - dFarX, 0) );
    }

    //*************************************************************************
    //  Method: WorksheetContextMenuManagerRowIDToVertex()
    //
    /// <summary>
    /// Converts a vertex row ID received from the WorksheetContextMenuManager
    /// to an IVertex.
    /// </summary>
    ///
    /// <param name="iVertexRowID">
    /// Vertex row ID received from the WorksheetContextMenuManager.  This is
    /// either WorksheetContextMenuManager.NoRowID or a vertex row ID stored in
    /// the vertex worksheet.
    /// </param>
    ///
    /// <returns>
    /// The IVertex corresponding to the row ID, or null if the row ID is
    /// WorksheetContextMenuManager.NoRowID.
    /// </returns>
    //*************************************************************************

    protected IVertex
    WorksheetContextMenuManagerRowIDToVertex
    (
        Int32 iVertexRowID
    )
    {
        Debug.Assert(iVertexRowID == WorksheetContextMenuManager.NoRowID ||
            iVertexRowID > 0);

        AssertValid();

        IVertex oVertex = null;

        if (iVertexRowID != WorksheetContextMenuManager.NoRowID &&
            m_oVertexRowIDDictionary != null)
        {
            // Convert the row ID to an IVertex.

            IIdentityProvider oVertexAsIdentityProvider;

            if ( m_oVertexRowIDDictionary.TryGetValue(iVertexRowID,
                out oVertexAsIdentityProvider) )
            {
                Debug.Assert(oVertexAsIdentityProvider is IVertex);

                oVertex = (IVertex)oVertexAsIdentityProvider;
            }
        }

        return (oVertex);
    }

    //*************************************************************************
    //  Method: WorksheetContextMenuManagerRowIDToEdge()
    //
    /// <summary>
    /// Converts an edge row ID received from the WorksheetContextMenuManager
    /// to an IEdge.
    /// </summary>
    ///
    /// <param name="iEdgeRowID">
    /// Edge row ID received from the WorksheetContextMenuManager.  This is
    /// either WorksheetContextMenuManager.NoRowID or an edge row ID stored in
    /// the edge worksheet.
    /// </param>
    ///
    /// <returns>
    /// The IEdge corresponding to the row ID, or null if the row ID is
    /// WorksheetContextMenuManager.NoRowID.
    /// </returns>
    //*************************************************************************

    protected IEdge
    WorksheetContextMenuManagerRowIDToEdge
    (
        Int32 iEdgeRowID
    )
    {
        Debug.Assert(iEdgeRowID == WorksheetContextMenuManager.NoRowID ||
            iEdgeRowID > 0);

        AssertValid();

        IEdge oEdge = null;

        if (iEdgeRowID != WorksheetContextMenuManager.NoRowID &&
            m_oEdgeRowIDDictionary != null)
        {
            // Convert the row ID to an IEdge.

            IIdentityProvider oEdgeAsIdentityProvider;

            if ( m_oEdgeRowIDDictionary.TryGetValue(iEdgeRowID,
                out oEdgeAsIdentityProvider) )
            {
                Debug.Assert(oEdgeAsIdentityProvider is IEdge);

                oEdge = (IEdge)oEdgeAsIdentityProvider;
            }
        }

        return (oEdge);
    }

    //*************************************************************************
    //  Method: GetVertexFromToolStripMenuItem()
    //
    /// <summary>
    /// Retrieves a vertex that was stored in a ToolStripMenuItem's Tag.
    /// </summary>
    ///
    /// <param name="oToolStripMenuItem">
    /// ToolStripMenuItem, as an Object.  The Tag must contain an IVertex.
    /// </param>
    ///
    /// <returns>
    /// The IVertex stored in the Tag of <paramref
    /// name="oToolStripMenuItem" />.
    /// </returns>
    //*************************************************************************

    protected IVertex
    GetVertexFromToolStripMenuItem
    (
        Object oToolStripMenuItem
    )
    {
        AssertValid();

        Debug.Assert(oToolStripMenuItem is ToolStripMenuItem);

        Debug.Assert( ( (ToolStripMenuItem)oToolStripMenuItem ).Tag is
            IVertex );

        return ( (IVertex)( ( (ToolStripMenuItem)oToolStripMenuItem ).Tag ) );
    }

    //*************************************************************************
    //  Method: ReadDynamicFilterColumns()
    //
    /// <summary>
    /// Updates the graph with the contents of the dynamic filter columns on
    /// the edge and vertex tables.
    /// </summary>
    ///
    /// <param name="bForceRedraw">
    /// true if the graph should be redrawn after the columns are read.
    /// </param>
    //*************************************************************************

    protected void
    ReadDynamicFilterColumns
    (
        Boolean bForceRedraw
    )
    {
        AssertValid();

        ReadEdgeDynamicFilterColumn(false);
        ReadVertexDynamicFilterColumn(bForceRedraw);
    }

    //*************************************************************************
    //  Method: ReadEdgeDynamicFilterColumn()
    //
    /// <summary>
    /// Updates the graph with the contents of the dynamic filter column on
    /// the edge table.
    /// </summary>
    ///
    /// <param name="bForceRedraw">
    /// true if the graph should be redrawn after the column is read.
    /// </param>
    //*************************************************************************

    protected void
    ReadEdgeDynamicFilterColumn
    (
        Boolean bForceRedraw
    )
    {
        AssertValid();

        ReadDynamicFilterColumn(WorksheetNames.Edges, TableNames.Edges,
            m_oEdgeRowIDDictionary, GetFilteredEdgeIDDictionary(),
            this.OnEdgeFiltered, bForceRedraw);
    }

    //*************************************************************************
    //  Method: ReadVertexDynamicFilterColumn()
    //
    /// <summary>
    /// Updates the graph with the contents of the dynamic filter column on
    /// the vertex table.
    /// </summary>
    ///
    /// <param name="bForceRedraw">
    /// true if the graph should be redrawn after the column is read.
    /// </param>
    //*************************************************************************

    protected void
    ReadVertexDynamicFilterColumn
    (
        Boolean bForceRedraw
    )
    {
        AssertValid();

        ReadDynamicFilterColumn(WorksheetNames.Vertices, TableNames.Vertices,
            m_oVertexRowIDDictionary, GetFilteredVertexIDDictionary(),
            this.OnVertexFiltered, bForceRedraw);
    }

    //*************************************************************************
    //  Method: ReadDynamicFilterColumn()
    //
    /// <summary>
    /// Updates the graph with the contents of the dynamic filter column on
    /// the edge or vertex table.
    /// </summary>
    ///
    /// <param name="sWorksheetName">
    /// Name of the worksheet containing the table.
    /// </param>
    ///
    /// <param name="sTableName">
    /// Name of the table.
    /// </param>
    ///
    /// <param name="oIDDictionary">
    /// Dictionary that maps IDs stored in the edge or vertex worksheet to edge
    /// or vertex objects in the graph.
    /// </param>
    ///
    /// <param name="oFilteredIDDictionary">
    /// Dictionary of edge or vertex IDs that have been filtered.  The
    /// dictionary is first cleared and then populated with the IEdge.ID or
    /// IVertex.ID of each edge or vertex that is filtered.
    /// </param>
    ///
    /// <param name="oOnEdgeOrVertexFiltered">
    /// Method to call as each edge or vertex is made visible or filtered.
    /// </param>
    ///
    /// <param name="bForceRedraw">
    /// true if the graph should be redrawn after the column is read.
    /// </param>
    //*************************************************************************

    protected void
    ReadDynamicFilterColumn
    (
        String sWorksheetName,
        String sTableName,
        Dictionary<Int32, IIdentityProvider> oIDDictionary,
        Dictionary<Int32, Char> oFilteredIDDictionary,
        EdgeOrVertexFilteredHandler oOnEdgeOrVertexFiltered,
        Boolean bForceRedraw
    )
    {
        Debug.Assert( !String.IsNullOrEmpty(sWorksheetName) );
        Debug.Assert( !String.IsNullOrEmpty(sTableName) );
        Debug.Assert(oIDDictionary != null);
        Debug.Assert(oFilteredIDDictionary != null);
        Debug.Assert(oOnEdgeOrVertexFiltered != null);
        AssertValid();

        if (oNodeXLControl.IsLayingOutGraph)
        {
            return;
        }

        oFilteredIDDictionary.Clear();

        // The dynamic filter column on the edge or vertex table contains
        // Booleans indicating whether the edge or vertex should be made
        // visible.

        // Get the data in the ID and dynamic filter columns.

        Object [,] oIDColumnValues, oDynamicFilterColumnValues;

        if ( !TryGetIDAndDynamicFilterValues(sWorksheetName, sTableName,
            out oIDColumnValues, out oDynamicFilterColumnValues) )
        {
            return;
        }

        Dictionary<Int32, Char> oFilteredVertexIDDictionary =
            GetFilteredVertexIDDictionary();

        Int32 iRows = oIDColumnValues.GetUpperBound(0);
        Debug.Assert( iRows == oDynamicFilterColumnValues.GetUpperBound(0) );

        for (Int32 iOneBasedRow = 1; iOneBasedRow <= iRows; iOneBasedRow++)
        {
            Object oID = oIDColumnValues[iOneBasedRow, 1];

            Object oDynamicFilter =
                oDynamicFilterColumnValues[iOneBasedRow, 1];

            IIdentityProvider oEdgeOrVertex;

            if (
                oID is Double
                &&
                oIDDictionary.TryGetValue( (Int32)(Double)oID,
                    out oEdgeOrVertex )
                &&
                oDynamicFilter is Boolean
                )
            {
                Debug.Assert(oEdgeOrVertex is IMetadataProvider);

                IMetadataProvider oEdgeOrVertex2 =
                    (IMetadataProvider)oEdgeOrVertex;

                Boolean bMakeVisible = (Boolean)oDynamicFilter;

                if (!bMakeVisible)
                {
                    oFilteredIDDictionary[oEdgeOrVertex.ID] = ' ';
                }

                if (oEdgeOrVertex2 is IEdge && bMakeVisible)
                {
                    IEdge oEdge = (IEdge)oEdgeOrVertex;

                    // Don't make the edge visible if either of its vertices
                    // was filtered by a vertex filter.

                    IVertex [] aoVertices = oEdge.Vertices;

                    if (
                        oFilteredVertexIDDictionary.ContainsKey(
                            aoVertices[0].ID)
                        ||
                        oFilteredVertexIDDictionary.ContainsKey(
                            aoVertices[1].ID)
                        )

                    {
                        bMakeVisible = false;
                    }
                }

                // Filter or make visible the edge or vertex, then call the
                // handler specified by the caller.

                DynamicallyFilterEdgeOrVertex(oEdgeOrVertex2, bMakeVisible);

                oOnEdgeOrVertexFiltered(oEdgeOrVertex2, bMakeVisible);
            }
        }

        if (bForceRedraw)
        {
            oNodeXLControl.DrawGraph();
        }
    }

    //*************************************************************************
    //  Method: ReadFilteredAlpha()
    //
    /// <summary>
    /// Reads the filtered alpha specified by the user in the
    /// m_oDynamicFilterDialog and applies it to the NodeXLControl.
    /// </summary>
    ///
    /// <param name="bForceRedraw">
    /// true if the graph should be redrawn after the filtered alpha is read.
    /// </param>
    //*************************************************************************

    protected void
    ReadFilteredAlpha
    (
        Boolean bForceRedraw
    )
    {
        AssertValid();

        oNodeXLControl.FilteredAlpha =
            ( new AlphaConverter() ).WorkbookToGraphAsByte(
                m_oDynamicFilterDialog.FilteredAlpha
                );

        if (bForceRedraw)
        {
            oNodeXLControl.DrawGraph();
        }
    }

    //*************************************************************************
    //  Method: TryGetIDAndDynamicFilterValues()
    //
    /// <summary>
    /// Tries to get the values from the ID and dynamic filter columns on a
    /// specified table.
    /// </summary>
    ///
    /// <param name="sWorksheetName">
    /// Name of the worksheet containing the table.
    /// </param>
    ///
    /// <param name="sTableName">
    /// Name of the table.
    /// </param>
    ///
    /// <param name="oIDColumnValues">
    /// Where the values from the ID column get stored.
    /// </param>
    ///
    /// <param name="oDynamicFilterColumnValues">
    /// Where the values from the dynamic filter column get stored.
    /// </param>
    ///
    /// <returns>
    /// true if successful.
    /// </returns>
    //*************************************************************************

    protected Boolean
    TryGetIDAndDynamicFilterValues
    (
        String sWorksheetName,
        String sTableName,
        out Object [,] oIDColumnValues,
        out Object [,] oDynamicFilterColumnValues
    )
    {
        Debug.Assert( !String.IsNullOrEmpty(sWorksheetName) );
        Debug.Assert( !String.IsNullOrEmpty(sTableName) );
        AssertValid();

        oIDColumnValues = oDynamicFilterColumnValues = null;

        ListObject oTable;
        Range oIDColumnData, oDynamicFilterColumnData;

        return (
            ExcelUtil.TryGetTable(m_oWorkbook, sWorksheetName, sTableName,
                out oTable)
            &&
            ExcelUtil.TryGetTableColumnDataAndValues(oTable,
                CommonTableColumnNames.ID, out oIDColumnData,
                out oIDColumnValues)
            &&
            ExcelUtil.TryGetTableColumnDataAndValues(oTable,
                CommonTableColumnNames.DynamicFilter,
                out oDynamicFilterColumnData, out oDynamicFilterColumnValues)
            );
    }

    //*************************************************************************
    //  Method: DynamicallyFilterEdgeOrVertex
    //
    /// <summary>
    /// Makes visible or filters an edge or vertex in response to a change in a
    /// dynamic filter.
    /// </summary>
    ///
    /// <param name="oEdgeOrVertex">
    /// The edge or vertex to make visible or filter.
    /// </param>
    ///
    /// <param name="bMakeVisible">
    /// true to make the edge or vertex visible, false to filter it.
    /// </param>
    //*************************************************************************

    protected void
    DynamicallyFilterEdgeOrVertex
    (
        IMetadataProvider oEdgeOrVertex,
        Boolean bMakeVisible
    )
    {
        Debug.Assert(oEdgeOrVertex != null);
        AssertValid();

        // Don't do anything if the edge or vertex is hidden.

        Object oVisibilityKeyValue;

        if (
            oEdgeOrVertex.TryGetValue(ReservedMetadataKeys.Visibility,
                typeof(VisibilityKeyValue), out oVisibilityKeyValue)
            &&
            (VisibilityKeyValue)oVisibilityKeyValue ==
                VisibilityKeyValue.Hidden
            )
        {
            return;
        }

        if (bMakeVisible)
        {
            // Remove the key that controls visibility.  Without the key, the
            // edge or vertex is visible by default.

            oEdgeOrVertex.RemoveKey(ReservedMetadataKeys.Visibility);
        }
        else
        {
            // Set the key to Filtered.

            oEdgeOrVertex.SetValue(ReservedMetadataKeys.Visibility,
                VisibilityKeyValue.Filtered);
        }
    }

    //*************************************************************************
    //  Delegate: EdgeOrVertexFilteredHandler
    //
    /// <summary>
    /// Represents a method that will be called by <see
    /// cref="ReadDynamicFilterColumn" /> when an edge or vertex is made
    /// visible or filtered.
    /// </summary>
    ///
    /// <param name="oEdgeOrVertex">
    /// The edge or vertex that was made visible or filtered.
    /// </param>
    ///
    /// <param name="bMadeVisible">
    /// true if the edge or vertex was made visible, false if it was filtered.
    /// </param>
    //*************************************************************************

    protected delegate void
    EdgeOrVertexFilteredHandler
    (
        Object oEdgeOrVertex,
        Boolean bMadeVisible
    );


    //*************************************************************************
    //  Method: OnEdgeFiltered
    //
    /// <summary>
    /// Gets called by <see cref="ReadDynamicFilterColumn" /> when an edge is
    /// made visible or filtered.
    /// </summary>
    ///
    /// <param name="oEdge">
    /// The edge that was made visible or filtered.
    /// </param>
    ///
    /// <param name="bMadeVisible">
    /// true if the edge was made visible, false if it was filtered.
    /// </param>
    //*************************************************************************

    protected void
    OnEdgeFiltered
    (
        Object oEdge,
        Boolean bMadeVisible
    )
    {
        AssertValid();

        // When an edge is filtered, its vertices should not be modified.  When
        // an edge is made visible, its vertices should also be made visible.

        if (bMadeVisible)
        {
            Debug.Assert(oEdge is IEdge);

            IEdge oEdge2 = (IEdge)oEdge;
            IVertex [] aoVertices = oEdge2.Vertices;

            DynamicallyFilterEdgeOrVertex(aoVertices[0], true);
            DynamicallyFilterEdgeOrVertex(aoVertices[1], true);
        }
    }

    //*************************************************************************
    //  Method: OnVertexFiltered
    //
    /// <summary>
    /// Gets called by <see cref="ReadDynamicFilterColumn" /> when a vertex is
    /// made visible or filtered.
    /// </summary>
    ///
    /// <param name="oVertex">
    /// The vertex that was made visible or filtered.
    /// </param>
    ///
    /// <param name="bMadeVisible">
    /// true if the vertex was made visible, false if it was filtered.
    /// </param>
    //*************************************************************************

    protected void
    OnVertexFiltered
    (
        Object oVertex,
        Boolean bMadeVisible
    )
    {
        AssertValid();

        // When a vertex is filtered, its incident edges should also be
        // filtered.  When a vertex is made visible, its incident edges should
        // also be made visible, unless 1) the edge was filtered by an edge
        // filter; or 2) the adjacent vertex was filtered by a vertex filter.

        Dictionary<Int32, Char> oFilteredEdgeIDDictionary =
            GetFilteredEdgeIDDictionary();

        Dictionary<Int32, Char> oFilteredVertexIDDictionary =
            GetFilteredVertexIDDictionary();

        Debug.Assert(oVertex is IVertex);

        IVertex oVertex2 = (IVertex)oVertex;

        foreach (IEdge oEdge in oVertex2.IncidentEdges)
        {
            if (bMadeVisible)
            {
                if (
                    oFilteredEdgeIDDictionary.ContainsKey(oEdge.ID)
                    ||
                    oFilteredVertexIDDictionary.ContainsKey(
                        oEdge.GetAdjacentVertex(oVertex2).ID)
                    )
                {
                    continue;
                }
            }

            DynamicallyFilterEdgeOrVertex(oEdge, bMadeVisible);
        }
    }

    //*************************************************************************
    //  Method: GetFilteredEdgeIDDictionary
    //
    /// <summary>
    /// Gets a dictionary of edges that have been dynamically filtered by edge
    /// filters.
    /// </summary>
    ///
    /// <returns>
    /// A dictionary of edges.  The key is the IEdge.ID and the value isn't
    /// used.
    /// </returns>
    ///
    /// <remarks>
    /// The dictionary is available only when the m_oDynamicFilterDialog dialog
    /// is open.
    /// </remarks>
    //*************************************************************************

    protected Dictionary<Int32, Char>
    GetFilteredEdgeIDDictionary()
    {
        AssertValid();

        Debug.Assert(m_oDynamicFilterDialog != null);

        Debug.Assert( m_oDynamicFilterDialog.Tag is
            Dictionary<Int32, Char>[] );

        return (
            ( ( Dictionary<Int32, Char>[] )m_oDynamicFilterDialog.Tag )[0]
            );
    }

    //*************************************************************************
    //  Method: GetFilteredVertexIDDictionary
    //
    /// <summary>
    /// Gets a dictionary of vertices that have been dynamically filtered by
    /// vertex filters.
    /// </summary>
    ///
    /// <returns>
    /// A dictionary of vertices.  The key is the IVertex.ID and the value
    /// isn't used.
    /// </returns>
    ///
    /// <remarks>
    /// The dictionary is available only when the m_oDynamicFilterDialog dialog
    /// is open.
    /// </remarks>
    //*************************************************************************

    protected Dictionary<Int32, Char>
    GetFilteredVertexIDDictionary()
    {
        AssertValid();

        Debug.Assert(m_oDynamicFilterDialog != null);

        Debug.Assert( m_oDynamicFilterDialog.Tag is
            Dictionary<Int32, Char>[] );

        return (
            ( ( Dictionary<Int32, Char>[] )m_oDynamicFilterDialog.Tag )[1]
            );
    }

    //*************************************************************************
    //  Method: SetSelectionByRowIDs()
    //
    /// <summary>
    /// Sets which edges and vertices are selected given a collection of row
    /// IDs from either the edge or vertex worksheet.
    /// </summary>
    ///
    /// <param name="oEdgeRowIDs">
    /// Collection of row IDs for the edges to select, or null.
    /// </param>
    ///
    /// <param name="oVertexRowIDs">
    /// Collection of row IDs for the vertices to select, or null.
    /// </param>
    ///
    /// <remarks>
    /// This method gets called when the selection is changed in either the
    /// edge or vertex worksheet.  One or the other parameter must be null.
    /// </remarks>
    //*************************************************************************

    protected void
    SetSelectionByRowIDs
    (
        ICollection<Int32> oEdgeRowIDs,
        ICollection<Int32> oVertexRowIDs
    )
    {
        Debug.Assert( (oEdgeRowIDs == null) != (oVertexRowIDs == null) );

        AssertValid();

        if (oNodeXLControl.IsLayingOutGraph || !this.NonEmptyWorkbookRead)
        {
            return;
        }

        Boolean bAutoSelect = ( new GeneralUserSettings() ).AutoSelect;

        // The row IDs get converted below to dictionaries of IEdge and IVertex
        // objects.  Dictionaries are used to prevent duplicates.  The key is
        // the IEdge.ID or IVertex.ID (NOT the row IDs) and the value is the
        // IEdge or IVertex.

        Dictionary<Int32, IEdge> oEdgesToSelect =
            new Dictionary<Int32, IEdge>();

        Dictionary<Int32, IVertex> oVerticesToSelect =
            new Dictionary<Int32, IVertex>();

        if (bAutoSelect)
        {
            // In AutoSelect mode, the selection in one or the other worksheet
            // determines the entire selection state.  For example, selecting a
            // vertex in the vertices worksheet selects the vertex and its
            // incident edges, but ignores any other edges that may have
            // already been selected in the edges worksheet.
        }
        else
        {
            // In manual mode, when a vertex is selected in the vertex
            // worksheet, the selection in the vertex worksheet determines
            // which vertices are selected, but the selected edges are left
            // alone.  Similiarly, when an edge is selected in the edge
            // worksheet, the selection in the edge worksheet determines which
            // edges are selected, but the selected vertices are left alone.

            if (oVertexRowIDs != null)
            {
                oEdgesToSelect =
                    NodeXLControlUtil.GetSelectedEdgesAsDictionary(
                        oNodeXLControl);
            }
            else if (oEdgeRowIDs != null)
            {
                oVerticesToSelect =
                    NodeXLControlUtil.GetSelectedVerticesAsDictionary(
                        oNodeXLControl);
            }
            else
            {
                Debug.Assert(false);
            }
        }

        if (oEdgeRowIDs != null)
        {
            foreach (Int32 iEdgeRowID in oEdgeRowIDs)
            {
                IIdentityProvider oEdge;

                if ( m_oEdgeRowIDDictionary.TryGetValue(iEdgeRowID,
                    out oEdge) )
                {
                    Debug.Assert(oEdge is IEdge);

                    IEdge oEdgeAsEdge = (IEdge)oEdge;

                    oEdgesToSelect[oEdgeAsEdge.ID] = oEdgeAsEdge;

                    if (bAutoSelect)
                    {
                        IVertex [] aoAdjacentVertices = oEdgeAsEdge.Vertices;

                        oVerticesToSelect[aoAdjacentVertices[0].ID]
                            = aoAdjacentVertices[0];

                        oVerticesToSelect[aoAdjacentVertices[1].ID]
                            = aoAdjacentVertices[1];
                    }
                }
            }
        }
        else
        {
            foreach (Int32 iVertexRowID in oVertexRowIDs)
            {
                IIdentityProvider oVertex;

                if ( m_oVertexRowIDDictionary.TryGetValue(iVertexRowID,
                    out oVertex) )
                {
                    Debug.Assert(oVertex is IVertex);

                    IVertex oVertexAsVertex = (IVertex)oVertex;

                    oVerticesToSelect[oVertexAsVertex.ID] = oVertexAsVertex;

                    if (bAutoSelect)
                    {
                        foreach (IEdge oIncidentEdge in
                            oVertexAsVertex.IncidentEdges)
                        {
                            oEdgesToSelect[oIncidentEdge.ID] = oIncidentEdge;
                        }
                    }
                }
            }
        }

        oNodeXLControl.SetSelected(oVerticesToSelect.Values,
            oEdgesToSelect.Values);
    }

    //*************************************************************************
    //  Method: VertexRowIDsToVertices()
    //
    /// <summary>
    /// Creates a collection of vertices from a collection of vertex row IDs.
    /// </summary>
    ///
    /// <param name="oVertexRowIDs">
    /// Collection of vertex row IDs from the vertex worksheet.
    /// </param>
    ///
    /// <returns>
    /// A collection of the corresponding IVertex objects.
    /// </returns>
    //*************************************************************************

    protected ICollection<IVertex>
    VertexRowIDsToVertices
    (
        IEnumerable<Int32> oVertexRowIDs
    )
    {
        Debug.Assert(oVertexRowIDs != null);
        AssertValid();

        LinkedList<IVertex> oVertices = new LinkedList<IVertex>();
        IIdentityProvider oVertexAsIdentityProvider;

        foreach (Int32 iVertexRowID in oVertexRowIDs)
        {
            if ( m_oVertexRowIDDictionary.TryGetValue(iVertexRowID,
                out oVertexAsIdentityProvider) )
            {
                Debug.Assert(oVertexAsIdentityProvider is IVertex);

                oVertices.AddLast( (IVertex)oVertexAsIdentityProvider );
            }
        }

        return (oVertices);
    }

    //*************************************************************************
    //  Method: ReadWorkbook_Click()
    //
    /// <summary>
    /// Handles the Click event on the tsbReadWorkbook ToolStripButton and
    /// msiContextReadWorkbook ToolStripMenuItem.
    /// </summary>
    ///
    /// <param name="sender">
    /// Standard event argument.
    /// </param>
    ///
    /// <param name="e">
    /// Standard event argument.
    /// </param>
    //*************************************************************************

    private void
    ReadWorkbook_Click
    (
        Object sender,
        EventArgs e
    )
    {
        AssertValid();

        ReadWorkbook();
    }

    //*************************************************************************
    //  Method: ForceLayout_Click()
    //
    /// <summary>
    /// Handles the Click event on the msiContextForceLayout ToolStripMenuItem
    /// and the the ButtonClick event on the tssbForceLayout
    /// ToolStripSplitButton.
    /// </summary>
    ///
    /// <param name="sender">
    /// Standard event argument.
    /// </param>
    ///
    /// <param name="e">
    /// Standard event argument.
    /// </param>
    //*************************************************************************

    private void
    ForceLayout_Click
    (
        object sender,
        EventArgs e
    )
    {
        AssertValid();

        ForceLayout();
    }

    //*************************************************************************
    //  Method: ForceLayoutSelected_Click()
    //
    /// <summary>
    /// Handles the Click event on the msiForceLayoutSelected and
    /// msiContextForceLayoutSelected menu items.
    /// </summary>
    ///
    /// <param name="sender">
    /// Standard event argument.
    /// </param>
    ///
    /// <param name="e">
    /// Standard event argument.
    /// </param>
    //*************************************************************************

    protected void
    ForceLayoutSelected_Click
    (
        object sender,
        EventArgs e
    )
    {
        AssertValid();

        ForceLayoutSelected();
    }

    //*************************************************************************
    //  Method: ForceLayoutSelectedWithinBounds_Click()
    //
    /// <summary>
    /// Handles the Click event on the msiForceLayoutSelectedWithinBounds and
    /// msiContextForceLayoutSelectedWithinBounds menu items.
    /// </summary>
    ///
    /// <param name="sender">
    /// Standard event argument.
    /// </param>
    ///
    /// <param name="e">
    /// Standard event argument.
    /// </param>
    //*************************************************************************

    protected void
    ForceLayoutSelectedWithinBounds_Click
    (
        object sender,
        EventArgs e
    )
    {
        AssertValid();

        ForceLayoutSelectedWithinBounds();
    }

    //*************************************************************************
    //  Method: ForceLayoutVisible_Click()
    //
    /// <summary>
    /// Handles the Click event on the msiForceLayoutVisible and
    /// msiContextForceLayoutVisible menu items.
    /// </summary>
    ///
    /// <param name="sender">
    /// Standard event argument.
    /// </param>
    ///
    /// <param name="e">
    /// Standard event argument.
    /// </param>
    //*************************************************************************

    protected void
    ForceLayoutVisible_Click
    (
        object sender,
        EventArgs e
    )
    {
        AssertValid();

        ForceLayoutVisible();
    }

    //*************************************************************************
    //  Method: ShowDynamicFilters_Click()
    //
    /// <summary>
    /// Handles the Click event on the tsbShowDynamicFilters ToolStripButton
    /// and msiContextShowDynamicFilters ToolStripMenuItem.
    /// </summary>
    ///
    /// <param name="sender">
    /// Standard event argument.
    /// </param>
    ///
    /// <param name="e">
    /// Standard event argument.
    /// </param>
    //*************************************************************************

    private void
    ShowDynamicFilters_Click
    (
        object sender,
        EventArgs e
    )
    {
        AssertValid();

        ShowDynamicFilters();
    }

    //*************************************************************************
    //  Method: Options_Click()
    //
    /// <summary>
    /// Handles the Click event on the tsbOptions ToolStripButton and
    /// msiContextOptions ToolStripMenuItem.
    /// </summary>
    ///
    /// <param name="sender">
    /// Standard event argument.
    /// </param>
    ///
    /// <param name="e">
    /// Standard event argument.
    /// </param>
    //*************************************************************************

    private void
    Options_Click
    (
        object sender,
        EventArgs e
    )
    {
        AssertValid();

        EditGeneralUserSettings();
    }

    //*************************************************************************
    //  Method: MouseModeButton_Click()
    //
    /// <summary>
    /// Handles the Click event on all the ToolStripButtons that correspond
    /// to values in the MouseMode enumeration.
    /// </summary>
    ///
    /// <param name="sender">
    /// Standard event argument.
    /// </param>
    ///
    /// <param name="e">
    /// Standard event argument.
    /// </param>
    //*************************************************************************

    private void
    MouseModeButton_Click
    (
        object sender,
        EventArgs e
    )
    {
        AssertValid();

        // tsGraphNavigation contains a ToolStripButton for each value in the
        // MouseMode enumeration.  They should act like radio buttons.

        foreach (ToolStripItem oToolStripItem in tsGraphNavigation.Items)
        {
            if (oToolStripItem is ToolStripButton)
            {
                ToolStripButton oToolStripButton =
                    (ToolStripButton)oToolStripItem;

                if (oToolStripButton.Tag is MouseMode)
                {
                    // Check the clicked button and uncheck the others.

                    Boolean bChecked = false;

                    if (oToolStripButton == sender)
                    {
                        oNodeXLControl.MouseMode =
                            (MouseMode)oToolStripButton.Tag;

                        bChecked = true;
                    }

                    oToolStripButton.Checked = bChecked;
                }
            }
        }
    }

    //*************************************************************************
    //  Method: tssbForceLayout_DropDownOpening()
    //
    /// <summary>
    /// Handles the DropDownOpening event on the tssbForceLayout
    /// ToolStripSplitButton.
    /// </summary>
    ///
    /// <param name="sender">
    /// Standard event argument.
    /// </param>
    ///
    /// <param name="e">
    /// Standard event argument.
    /// </param>
    //*************************************************************************

    private void
    tssbForceLayout_DropDownOpening
    (
        object sender,
        EventArgs e
    )
    {
        AssertValid();

        // If there are selected vertices, allow the user to lay them out.

        Int32 iSelectedVertices = oNodeXLControl.SelectedVertices.Count;

        MenuUtil.EnableToolStripMenuItems(
            iSelectedVertices > 0,
            msiForceLayoutSelected
            );

        MenuUtil.EnableToolStripMenuItems(
            iSelectedVertices > 1,
            msiForceLayoutSelectedWithinBounds
            );
    }

    //*************************************************************************
    //  Method: msiSnapVerticesToGrid_Click()
    //
    /// <summary>
    /// Handles the Click event on the msiSnapVerticesToGrid menu item.
    /// </summary>
    ///
    /// <param name="sender">
    /// Standard event argument.
    /// </param>
    ///
    /// <param name="e">
    /// Standard event argument.
    /// </param>
    //*************************************************************************

    protected void
    msiSnapVerticesToGrid_Click
    (
        object sender,
        EventArgs e
    )
    {
        AssertValid();

        if (oNodeXLControl.IsLayingOutGraph)
        {
            return;
        }

        this.UseWaitCursor = true;

        VertexGridSnapper.SnapVerticesToGrid(oNodeXLControl.Graph,
            ( new VertexGridSnapperUserSettings() ).GridSize);

        oNodeXLControl.DrawGraph(false);

        this.UseWaitCursor = false;
    }

    //*************************************************************************
    //  Method: msiGridSize_Click()
    //
    /// <summary>
    /// Handles the Click event on the msiGridSize menu item.
    /// </summary>
    ///
    /// <param name="sender">
    /// Standard event argument.
    /// </param>
    ///
    /// <param name="e">
    /// Standard event argument.
    /// </param>
    //*************************************************************************

    protected void
    msiGridSize_Click
    (
        object sender,
        EventArgs e
    )
    {
        AssertValid();

        if (oNodeXLControl.IsLayingOutGraph)
        {
            return;
        }

        VertexGridSnapperUserSettings oVertexGridSnapperUserSettings =
            new VertexGridSnapperUserSettings();

        VertexGridSnapperUserSettingsDialog
            oVertexGridSnapperUserSettingsDialog =
                new VertexGridSnapperUserSettingsDialog(
                    oVertexGridSnapperUserSettings);

        if (oVertexGridSnapperUserSettingsDialog.ShowDialog() ==
            DialogResult.OK)
        {
            oVertexGridSnapperUserSettings.Save();
        }
    }

    //*************************************************************************
    //  Method: msiLayoutOptions_Click()
    //
    /// <summary>
    /// Handles the Click event on the msiLayoutOptions ToolStripMenuItem.
    /// </summary>
    ///
    /// <param name="sender">
    /// Standard event argument.
    /// </param>
    ///
    /// <param name="e">
    /// Standard event argument.
    /// </param>
    //*************************************************************************

    private void
    msiLayoutOptions_Click
    (
        object sender,
        EventArgs e
    )
    {
        AssertValid();

        EditLayoutUserSettings();
    }

    //*************************************************************************
    //  Method: msiContextSelectAllVertices_Click()
    //
    /// <summary>
    /// Handles the Click event on the msiContextSelectAllVertices
    /// ToolStripMenuItem.
    /// </summary>
    ///
    /// <param name="sender">
    /// Standard event argument.
    /// </param>
    ///
    /// <param name="e">
    /// Standard event argument.
    /// </param>
    //*************************************************************************

    private void
    msiContextSelectAllVertices_Click
    (
        object sender,
        EventArgs e
    )
    {
        AssertValid();

        SelectAllVertices(true);
    }

    //*************************************************************************
    //  Method: msiContextSelectAllEdges_Click()
    //
    /// <summary>
    /// Handles the Click event on the msiContextSelectAllEdges
    /// ToolStripMenuItem.
    /// </summary>
    ///
    /// <param name="sender">
    /// Standard event argument.
    /// </param>
    ///
    /// <param name="e">
    /// Standard event argument.
    /// </param>
    //*************************************************************************

    private void
    msiContextSelectAllEdges_Click
    (
        object sender,
        EventArgs e
    )
    {
        AssertValid();

        SelectAllEdges(true);
    }

    //*************************************************************************
    //  Method: msiContextSelectAllVerticesAndEdges_Click()
    //
    /// <summary>
    /// Handles the Click event on the msiContextSelectAllVerticesAndEdges
    /// ToolStripMenuItem.
    /// </summary>
    ///
    /// <param name="sender">
    /// Standard event argument.
    /// </param>
    ///
    /// <param name="e">
    /// Standard event argument.
    /// </param>
    //*************************************************************************

    private void
    msiContextSelectAllVerticesAndEdges_Click
    (
        object sender,
        EventArgs e
    )
    {
        AssertValid();

        SelectAllVerticesAndEdges(true);
    }

    //*************************************************************************
    //  Method: msiContextDeselectAllVertices_Click()
    //
    /// <summary>
    /// Handles the Click event on the msiContextDeselectAllVertices
    /// ToolStripMenuItem.
    /// </summary>
    ///
    /// <param name="sender">
    /// Standard event argument.
    /// </param>
    ///
    /// <param name="e">
    /// Standard event argument.
    /// </param>
    //*************************************************************************

    private void
    msiContextDeselectAllVertices_Click
    (
        object sender,
        EventArgs e
    )
    {
        AssertValid();

        SelectAllVertices(false);
    }

    //*************************************************************************
    //  Method: msiContextDeselectAllEdges_Click()
    //
    /// <summary>
    /// Handles the Click event on the msiContextDeselectAllEdges
    /// ToolStripMenuItem.
    /// </summary>
    ///
    /// <param name="sender">
    /// Standard event argument.
    /// </param>
    ///
    /// <param name="e">
    /// Standard event argument.
    /// </param>
    //*************************************************************************

    private void
    msiContextDeselectAllEdges_Click
    (
        object sender,
        EventArgs e
    )
    {
        AssertValid();

        SelectAllEdges(false);
    }

    //*************************************************************************
    //  Method: msiContextDeselectAllVerticesAndEdges_Click()
    //
    /// <summary>
    /// Handles the Click event on the msiContextDeselectAllVerticesAndEdges
    /// ToolStripMenuItem.
    /// </summary>
    ///
    /// <param name="sender">
    /// Standard event argument.
    /// </param>
    ///
    /// <param name="e">
    /// Standard event argument.
    /// </param>
    //*************************************************************************

    private void
    msiContextDeselectAllVerticesAndEdges_Click
    (
        object sender,
        EventArgs e
    )
    {
        AssertValid();

        SelectAllVerticesAndEdges(false);
    }

    //*************************************************************************
    //  Method: msiContextSelectIncidentEdges_Click()
    //
    /// <summary>
    /// Handles the Click event on the msiContextSelectIncidentEdges
    /// ToolStripMenuItem.
    /// </summary>
    ///
    /// <param name="sender">
    /// Standard event argument.
    /// </param>
    ///
    /// <param name="e">
    /// Standard event argument.
    /// </param>
    //*************************************************************************

    private void
    msiContextSelectIncidentEdges_Click
    (
        object sender,
        EventArgs e
    )
    {
        AssertValid();

        // The clicked vertex is stored in the menu item's Tag.

        SelectIncidentEdges(GetVertexFromToolStripMenuItem(sender), true);
    }

    //*************************************************************************
    //  Method: msiContextDeselectIncidentEdges_Click()
    //
    /// <summary>
    /// Handles the Click event on the msiContextDeselectIncidentEdges
    /// ToolStripMenuItem.
    /// </summary>
    ///
    /// <param name="sender">
    /// Standard event argument.
    /// </param>
    ///
    /// <param name="e">
    /// Standard event argument.
    /// </param>
    //*************************************************************************

    private void
    msiContextDeselectIncidentEdges_Click
    (
        object sender,
        EventArgs e
    )
    {
        AssertValid();

        // The clicked vertex is stored in the menu item's Tag.

        SelectIncidentEdges(GetVertexFromToolStripMenuItem(sender), false);
    }

    //*************************************************************************
    //  Method: msiContextSelectAdjacentVertices_Click()
    //
    /// <summary>
    /// Handles the Click event on the msiContextSelectAdjacentVertices
    /// ToolStripMenuItem.
    /// </summary>
    ///
    /// <param name="sender">
    /// Standard event argument.
    /// </param>
    ///
    /// <param name="e">
    /// Standard event argument.
    /// </param>
    //*************************************************************************

    private void
    msiContextSelectAdjacentVertices_Click
    (
        object sender,
        EventArgs e
    )
    {
        AssertValid();

        // The clicked vertex is stored in the menu item's Tag.

        SelectAdjacentVertices(GetVertexFromToolStripMenuItem(sender), true);
    }

    //*************************************************************************
    //  Method: msiContextDeselectAdjacentVertices_Click()
    //
    /// <summary>
    /// Handles the Click event on the msiContextDeselectAdjacentVertices
    /// ToolStripMenuItem.
    /// </summary>
    ///
    /// <param name="sender">
    /// Standard event argument.
    /// </param>
    ///
    /// <param name="e">
    /// Standard event argument.
    /// </param>
    //*************************************************************************

    private void
    msiContextDeselectAdjacentVertices_Click
    (
        object sender,
        EventArgs e
    )
    {
        AssertValid();

        // The clicked vertex is stored in the menu item's Tag.

        SelectAdjacentVertices(GetVertexFromToolStripMenuItem(sender), false);
    }

    //*************************************************************************
    //  Method: msiContextSelectSubgraphs_Click()
    //
    /// <summary>
    /// Handles the Click event on the msiContextSelectSubgraphs
    /// ToolStripMenuItem.
    /// </summary>
    ///
    /// <param name="sender">
    /// Standard event argument.
    /// </param>
    ///
    /// <param name="e">
    /// Standard event argument.
    /// </param>
    //*************************************************************************

    private void
    msiContextSelectSubgraphs_Click
    (
        object sender,
        EventArgs e
    )
    {
        AssertValid();

        // If a vertex was clicked, it is stored in the menu item's Tag.

        Debug.Assert(sender is ToolStripMenuItem);

        Object oTag = ( (ToolStripMenuItem)sender ).Tag;

        Debug.Assert(oTag == null || oTag is IVertex);

        SelectSubgraphs( (IVertex)oTag );
    }

    //*************************************************************************
    //  Method: msiContextEditVertexAttributes_Click()
    //
    /// <summary>
    /// Handles the Click event on the msiContextEditVertexAttributes
    /// ToolStripMenuItem.
    /// </summary>
    ///
    /// <param name="sender">
    /// Standard event argument.
    /// </param>
    ///
    /// <param name="e">
    /// Standard event argument.
    /// </param>
    //*************************************************************************

    private void
    msiContextEditVertexAttributes_Click
    (
        object sender,
        EventArgs e
    )
    {
        AssertValid();

        EditVertexAttributes();
    }

    //*************************************************************************
    //  Method: msiContextCopyImageToClipboard_Click()
    //
    /// <summary>
    /// Handles the Click event on the msiContextCopyImageToClipboard
    /// ToolStripMenuItem.
    /// </summary>
    ///
    /// <param name="sender">
    /// Standard event argument.
    /// </param>
    ///
    /// <param name="e">
    /// Standard event argument.
    /// </param>
    //*************************************************************************

    private void
    msiContextCopyImageToClipboard_Click
    (
        object sender,
        EventArgs e
    )
    {
        AssertValid();

        CopyGraphBitmap();
    }

    //*************************************************************************
    //  Method: msiContextSetImageSize_Click()
    //
    /// <summary>
    /// Handles the Click event on the msiContextSetImageSize
    /// ToolStripMenuItem.
    /// </summary>
    ///
    /// <param name="sender">
    /// Standard event argument.
    /// </param>
    ///
    /// <param name="e">
    /// Standard event argument.
    /// </param>
    //*************************************************************************

    private void
    msiContextSetImageSize_Click
    (
        object sender,
        EventArgs e
    )
    {
        AssertValid();

        SetImageSize();
    }

    //*************************************************************************
    //  Method: msiContextSaveImage_Click()
    //
    /// <summary>
    /// Handles the Click event on the msiContextSaveImage ToolStripMenuItem.
    /// </summary>
    ///
    /// <param name="sender">
    /// Standard event argument.
    /// </param>
    ///
    /// <param name="e">
    /// Standard event argument.
    /// </param>
    //*************************************************************************

    private void
    msiContextSaveImage_Click
    (
        object sender,
        EventArgs e
    )
    {
        AssertValid();

        SaveImage();
    }

    //*************************************************************************
    //  Method: msiContextCustomMenuItem_Click()
    //
    /// <summary>
    /// Handles the Click event for custom menu items on the cmsNodeXLControl
    /// ContextMenuStrip.
    /// </summary>
    ///
    /// <param name="sender">
    /// Standard event argument.
    /// </param>
    ///
    /// <param name="e">
    /// Standard event argument.
    /// </param>
    //*************************************************************************

    private void
    msiContextCustomMenuItem_Click
    (
        object sender,
        EventArgs e
    )
    {
        Debug.Assert(sender is ToolStripMenuItem);
        AssertValid();

        // Run the custom action that was stored as a String in the menu item's
        // Tag.

        ToolStripMenuItem oItem = (ToolStripMenuItem)sender;

        Debug.Assert(oItem.Tag is String);

        String sCustomMenuItemAction = (String)oItem.Tag;

        Debug.Assert( !String.IsNullOrEmpty(sCustomMenuItemAction) );

        this.UseWaitCursor = true;

        try
        {
            Process.Start(sCustomMenuItemAction);

            this.UseWaitCursor = false;
        }
        catch (Exception oException)
        {
            this.UseWaitCursor = false;

            FormUtil.ShowWarning(String.Format(
                "A problem occurred while attempting to run this custom menu"
                + " item action:"
                + "\r\n\r\n"
                + "{0}"
                + "\r\n\r\n"
                + "Here are the details:"
                + "\r\n\r\n"
                + "\"{1}\""
                + "\r\n\r\n"
                + "Running a custom menu item action is similar to typing the"
                + " action into the Run dialog box of the Windows Start menu."
                + "  You might want to test your custom action using the Run"
                + " dialog before typing the action into the Custom Menu Item"
                + " Action column of the Vertices worksheet."
                ,
                sCustomMenuItemAction,
                oException.Message
                ) );
        }
    }

    //*************************************************************************
    //  Method: oNodeXLControl_GraphMouseUp()
    //
    /// <summary>
    /// Handles the GraphMouseUp event on the oNodeXLControl control.
    /// </summary>
    ///
    /// <param name="sender">
    /// Standard event argument.
    /// </param>
    ///
    /// <param name="e">
    /// Standard event argument.
    /// </param>
    //*************************************************************************

    private void
    oNodeXLControl_GraphMouseUp
    (
        object sender,
        GraphMouseButtonEventArgs e
    )
    {
        AssertValid();

        // A m_iEnableGraphControlsCount value of zero or greater should enable
        // the controls used to interact with the graph.

        if (m_iEnableGraphControlsCount < 0 ||
            e.ChangedButton != MouseButton.Right)
        {
            return;
        }

        // Enable the controls on the context menu, then display it at the
        // clicked location.

        EnableContextMenuItems(e.Vertex);

        cmsNodeXLControl.Show( WpfGraphicsUtil.WpfPointToPoint(
            oNodeXLControl.PointToScreen(
                e.GetPosition(oNodeXLControl) ) ) );

        // It shouldn't be necessary to give focus to the context menu, but if
        // this isn't done, the following bug occurs: Run the ExcelTemplate
        // project on a Vista or XP machine.  Detach the graph pane (Excel's
        // Document Actions pane) from the Excel window and right-click the
        // NodeXLControl.  A context menu pops up, but it behaves erratically:
        // if you click a menu item that has a submenu, the submenu doesn't
        // appear.  Instead, focus is given back to the Excel workbook, which
        // flashes a cell in an odd manner.
        //
        // This bug does not occur if the graph pane is not detached from the
        // Excel window.

        cmsNodeXLControl.Focus();
    }

    //*************************************************************************
    //  Method: oNodeXLControl_LayingOutGraph()
    //
    /// <summary>
    /// Handles the LayingOutGraph event on the oNodeXLControl control.
    /// </summary>
    ///
    /// <param name="sender">
    /// Standard event argument.
    /// </param>
    ///
    /// <param name="e">
    /// Standard event argument.
    /// </param>
    //*************************************************************************

    private void
    oNodeXLControl_LayingOutGraph
    (
        object sender,
        EventArgs e
    )
    {
        AssertValid();

        EnableGraphControls(false);
    }

    //*************************************************************************
    //  Method: oNodeXLControl_GraphLaidOut()
    //
    /// <summary>
    /// Handles the GraphLaidOut event on the oNodeXLControl control.
    /// </summary>
    ///
    /// <param name="sender">
    /// Standard event argument.
    /// </param>
    ///
    /// <param name="e">
    /// Standard event argument.
    /// </param>
    //*************************************************************************

    private void
    oNodeXLControl_GraphLaidOut
    (
        object sender,
        AsyncCompletedEventArgs e
    )
    {
        AssertValid();

        // Remove the keys that may have been added to the graph by
        // ForceLayoutSelected() and ForceLayoutSelectedWithinBounds().

        oNodeXLControl.Graph.RemoveKey(
            ReservedMetadataKeys.LayOutTheseVerticesOnly);

        oNodeXLControl.Graph.RemoveKey(
            ReservedMetadataKeys.LayOutTheseVerticesWithinBounds);

        if (e.Error == null && m_oEdgeRowIDDictionary != null)
        {
            // Forward the event.

            GraphLaidOutEventHandler oGraphLaidOut = this.GraphLaidOut;

            if (oGraphLaidOut != null)
            {
                Debug.Assert(m_oVertexRowIDDictionary != null);

                try
                {
                    oGraphLaidOut(this, new GraphLaidOutEventArgs(
                        this.GraphRectangle, m_oEdgeRowIDDictionary,
                        m_oVertexRowIDDictionary, oNodeXLControl
                        ) );
                }
                catch (Exception oException)
                {
                    ErrorUtil.OnException(oException);
                }
            }
        }

        EnableGraphControls(true);

        if (e.Error is OutOfMemoryException)
        {
            FormUtil.ShowError(
                "The computer does not have enough memory to lay out the"
                + " graph.  Try the following to fix the problem:"
                + "\r\n\r\n"
                + "1. Select a different layout algorithm."
                + "\r\n\r\n"
                + "2. Close other programs."
                + "\r\n\r\n"
                + "3. Restart the computer."
                + "\r\n\r\n"
                + "4. Reduce the number of edges in the graph."
                + "\r\n\r\n"
                + "5. Add more memory to the computer."
                );
        }
        else if (e.Error != null)
        {
            ErrorUtil.OnException(e.Error);
        }
    }

    //*************************************************************************
    //  Method: oNodeXLControl_VerticesMoved()
    //
    /// <summary>
    /// Handles the VerticesMoved event on the oNodeXLControl control.
    /// </summary>
    ///
    /// <param name="sender">
    /// Standard event argument.
    /// </param>
    ///
    /// <param name="e">
    /// Standard event argument.
    /// </param>
    //*************************************************************************

    private void
    oNodeXLControl_VerticesMoved
    (
        object sender,
        VerticesMovedEventArgs e
    )
    {
        AssertValid();

        VerticesMovedEventHandler2 oVerticesMoved = this.VerticesMoved;

        if (oVerticesMoved == null)
        {
            return;
        }

        LinkedList<VertexAndRowID> oVerticesAndRowIDs =
            new LinkedList<VertexAndRowID>();

        foreach (IVertex oMovedVertex in e.MovedVertices)
        {
            if ( !GroupWorksheetReader.VertexIsCollapsedGroup(oMovedVertex) )
            {
                oVerticesAndRowIDs.AddLast( new VertexAndRowID(
                    oMovedVertex, (Int32)oMovedVertex.Tag ) );
            }
        }

        this.UseWaitCursor = true;

        try
        {
            oVerticesMoved( this, new VerticesMovedEventArgs2(
                oVerticesAndRowIDs, this.GraphRectangle));
        }
        catch (Exception oException)
        {
            this.UseWaitCursor = false;
            ErrorUtil.OnException(oException);
        }
        finally
        {
            this.UseWaitCursor = false;
        }
    }

    //*************************************************************************
    //  Method: oNodeXLControl_SelectionChanged()
    //
    /// <summary>
    /// Handles the SelectionChanged event on the oNodeXLControl control.
    /// </summary>
    ///
    /// <param name="sender">
    /// Standard event argument.
    /// </param>
    ///
    /// <param name="e">
    /// Standard event argument.
    /// </param>
    //*************************************************************************

    protected void
    oNodeXLControl_SelectionChanged
    (
        object sender,
        EventArgs e
    )
    {
        AssertValid();

        EventHandler oSelectionChangedInGraph = this.SelectionChangedInGraph;

        if (oSelectionChangedInGraph != null)
        {
            try
            {
                oSelectionChangedInGraph(this, EventArgs.Empty);
            }
            catch (Exception oException)
            {
                ErrorUtil.OnException(oException);
            }
        }
    }

    //*************************************************************************
    //  Method: m_oDynamicFilterDialog_DynamicFilterColumnsChanged()
    //
    /// <summary>
    /// Handles the DynamicFilterColumnsChanged event on the
    /// m_oDynamicFilterDialog.
    /// </summary>
    ///
    /// <param name="sender">
    /// Standard event argument.
    /// </param>
    ///
    /// <param name="e">
    /// Standard event argument.
    /// </param>
    //*************************************************************************

    protected void
    m_oDynamicFilterDialog_DynamicFilterColumnsChanged
    (
        Object sender,
        DynamicFilterColumnsChangedEventArgs e
    )
    {
        AssertValid();

        if (oNodeXLControl.IsLayingOutGraph)
        {
            return;
        }

        if (e.DynamicFilterColumns ==
            (DynamicFilterColumns.EdgeTable | DynamicFilterColumns.VertexTable)
            )
        {
            ReadDynamicFilterColumns(true);
        }
        else if (e.DynamicFilterColumns == DynamicFilterColumns.EdgeTable)
        {
            ReadEdgeDynamicFilterColumn(true);
        }
        else if (e.DynamicFilterColumns == DynamicFilterColumns.VertexTable)
        {
            ReadVertexDynamicFilterColumn(true);
        }

        UpdateDynamicFiltersLegend();
    }

    //*************************************************************************
    //  Method: m_oDynamicFilterDialog_FilteredAlphaChanged()
    //
    /// <summary>
    /// Handles the FilteredAlphaChanged event on the m_oDynamicFilterDialog.
    /// </summary>
    ///
    /// <param name="sender">
    /// Standard event argument.
    /// </param>
    ///
    /// <param name="e">
    /// Standard event argument.
    /// </param>
    //*************************************************************************

    protected void
    m_oDynamicFilterDialog_FilteredAlphaChanged
    (
        Object sender,
        EventArgs e
    )
    {
        AssertValid();

        if (oNodeXLControl.IsLayingOutGraph)
        {
            return;
        }

        ReadFilteredAlpha(true);
    }

    //*************************************************************************
    //  Method: m_oDynamicFilterDialog_FormClosed()
    //
    /// <summary>
    /// Handles the FormClosed event on the m_oDynamicFilterDialog.
    /// </summary>
    ///
    /// <param name="sender">
    /// Standard event argument.
    /// </param>
    ///
    /// <param name="e">
    /// Standard event argument.
    /// </param>
    //*************************************************************************

    protected void
    m_oDynamicFilterDialog_FormClosed
    (
        Object sender,
        FormClosedEventArgs e
    )
    {
        AssertValid();

        m_oDynamicFilterDialog = null;
    }

    //*************************************************************************
    //  Method: ThisWorkbook_VisualAttributeSetInWorkbook()
    //
    /// <summary>
    /// Handles the VisualAttributeSetInWorkbook event on the ThisWorkbook
    /// workbook.
    /// </summary>
    ///
    /// <param name="sender">
    /// Standard event argument.
    /// </param>
    ///
    /// <param name="e">
    /// Standard event argument.
    /// </param>
    //*************************************************************************

    protected void
    ThisWorkbook_VisualAttributeSetInWorkbook
    (
        Object sender,
        EventArgs e
    )
    {
        AssertValid();

        if (oNodeXLControl.IsLayingOutGraph ||
            !( new GeneralUserSettings() ).AutoReadWorkbook)
        {
            return;
        }

        // If the workbook hasn't been read yet, read it and lay out the graph.
        // Otherwise, read it but don't lay it out again.

        ReadWorkbook(!this.NonEmptyWorkbookRead);
    }

    //*************************************************************************
    //  Method: ThisWorkbook_CollapseOrExpandGroups()
    //
    /// <summary>
    /// Handles the CollapseOrExpandGroups event on the ThisWorkbook workbook.
    /// </summary>
    ///
    /// <param name="sender">
    /// Standard event argument.
    /// </param>
    ///
    /// <param name="e">
    /// Standard event argument.
    /// </param>
    //*************************************************************************

    protected void
    ThisWorkbook_CollapseOrExpandGroups
    (
        Object sender,
        CollapseOrExpandGroupsEventArgs e
    )
    {
        AssertValid();

        if (oNodeXLControl.IsLayingOutGraph || !this.NonEmptyWorkbookRead)
        {
            return;
        }

        CollapseOrExpandGroups(e.GroupNames, e.Collapse, true);
    }

    //*************************************************************************
    //  Method: ThisWorkbook_Shutdown()
    //
    /// <summary>
    /// Handles the Shutdown event on the ThisWorkbook workbook.
    /// </summary>
    ///
    /// <param name="sender">
    /// Standard event argument.
    /// </param>
    ///
    /// <param name="e">
    /// Standard event argument.
    /// </param>
    //*************************************************************************

    protected void
    ThisWorkbook_Shutdown
    (
        Object sender,
        EventArgs e
    )
    {
        AssertValid();

        GraphZoomAndScaleUserSettings oGraphZoomAndScaleUserSettings =
            new GraphZoomAndScaleUserSettings();

        oGraphZoomAndScaleUserSettings.GraphScale =
            oNodeXLControl.GraphScale;

        oGraphZoomAndScaleUserSettings.Save();
    }

    //*************************************************************************
    //  Method: WorksheetContextMenuManager_RequestVertexCommandEnable()
    //
    /// <summary>
    /// Handles the RequestVertexCommandEnable event on the
    /// WorksheetContextMenuManager object.
    /// </summary>
    ///
    /// <param name="sender">
    /// Standard event argument.
    /// </param>
    ///
    /// <param name="e">
    /// Standard event argument.
    /// </param>
    //*************************************************************************

    protected void
    WorksheetContextMenuManager_RequestVertexCommandEnable
    (
        Object sender,
        RequestVertexCommandEnableEventArgs e
    )
    {
        AssertValid();

        // Get the vertex corresponding to the row the user right-clicked in
        // the vertex worksheet.  This can be null.

        IVertex oClickedVertex =
            WorksheetContextMenuManagerRowIDToVertex(e.VertexRowID);

        Boolean bEnableSelectAllVertices, bEnableDeselectAllVertices,
            bEnableSelectAdjacentVertices, bEnableDeselectAdjacentVertices,
            bEnableSelectIncidentEdges, bEnableDeselectIncidentEdges,
            bEnableEditVertexAttributes, bEnableSelectSubgraphs;

        GetVertexCommandEnableFlags(oClickedVertex,
            out bEnableSelectAllVertices,
            out bEnableDeselectAllVertices,
            out bEnableSelectAdjacentVertices,
            out bEnableDeselectAdjacentVertices,
            out bEnableSelectIncidentEdges,
            out bEnableDeselectIncidentEdges,
            out bEnableEditVertexAttributes,
            out bEnableSelectSubgraphs
            );

        e.EnableSelectAllVertices = bEnableSelectAllVertices;
        e.EnableDeselectAllVertices = bEnableDeselectAllVertices;
        e.EnableSelectAdjacentVertices = bEnableSelectAdjacentVertices;
        e.EnableDeselectAdjacentVertices = bEnableDeselectAdjacentVertices;
        e.EnableSelectIncidentEdges = bEnableSelectIncidentEdges;
        e.EnableDeselectIncidentEdges = bEnableDeselectIncidentEdges;
        e.EnableEditVertexAttributes = bEnableEditVertexAttributes;
        e.EnableSelectSubgraphs = bEnableSelectSubgraphs;
    }

    //*************************************************************************
    //  Method: WorksheetContextMenuManager_RequestEdgeCommandEnable()
    //
    /// <summary>
    /// Handles the RequestEdgeCommandEnable event on the
    /// WorksheetContextMenuManager object.
    /// </summary>
    ///
    /// <param name="sender">
    /// Standard event argument.
    /// </param>
    ///
    /// <param name="e">
    /// Standard event argument.
    /// </param>
    //*************************************************************************

    protected void
    WorksheetContextMenuManager_RequestEdgeCommandEnable
    (
        Object sender,
        RequestEdgeCommandEnableEventArgs e
    )
    {
        AssertValid();

        // Get the edge corresponding to the row the user right-clicked in the
        // edge worksheet.  This can be null.

        IEdge oClickedEdge =
            WorksheetContextMenuManagerRowIDToEdge(e.EdgeRowID);

        Boolean bEnableSelectAllEdges, bEnableDeselectAllEdges,
            bEnableSelectAdjacentVertices, bEnableDeselectAdjacentVertices;

        GetEdgeCommandEnableFlags(oClickedEdge,
            out bEnableSelectAllEdges,
            out bEnableDeselectAllEdges,
            out bEnableSelectAdjacentVertices,
            out bEnableDeselectAdjacentVertices
            );

        e.EnableSelectAllEdges = bEnableSelectAllEdges;
        e.EnableDeselectAllEdges = bEnableDeselectAllEdges;
        e.EnableSelectAdjacentVertices = bEnableSelectAdjacentVertices;
        e.EnableDeselectAdjacentVertices = bEnableDeselectAdjacentVertices;
    }

    //*************************************************************************
    //  Method: WorksheetContextMenuManager_RunVertexCommand()
    //
    /// <summary>
    /// Handles the RunVertexCommand event on the WorksheetContextMenuManager
    /// object.
    /// </summary>
    ///
    /// <param name="sender">
    /// Standard event argument.
    /// </param>
    ///
    /// <param name="e">
    /// Standard event argument.
    /// </param>
    //*************************************************************************

    protected void
    WorksheetContextMenuManager_RunVertexCommand
    (
        Object sender,
        RunVertexCommandEventArgs e
    )
    {
        AssertValid();

        // Ge the vertex corresponding to the row the user right-clicked in the
        // vertex worksheet.  This can be null.

        IVertex oClickedVertex =
            WorksheetContextMenuManagerRowIDToVertex(e.VertexRowID);

        WorksheetContextMenuManager.VertexCommand eVertexCommand =
            e.VertexCommand;

        switch (eVertexCommand)
        {
            case WorksheetContextMenuManager.VertexCommand.SelectAllVertices:
            case WorksheetContextMenuManager.VertexCommand.DeselectAllVertices:

                SelectAllVertices(eVertexCommand ==
                    WorksheetContextMenuManager.VertexCommand.
                    SelectAllVertices);

                break;

            case WorksheetContextMenuManager.VertexCommand.
                SelectAdjacentVertices:

            case WorksheetContextMenuManager.VertexCommand.
                DeselectAdjacentVertices:

                if (oClickedVertex != null)
                {
                    SelectAdjacentVertices(oClickedVertex, 
                        eVertexCommand == WorksheetContextMenuManager.
                            VertexCommand.SelectAdjacentVertices);
                }

                break;

            case WorksheetContextMenuManager.VertexCommand.SelectIncidentEdges:

            case WorksheetContextMenuManager.VertexCommand.
                DeselectIncidentEdges:

                if (oClickedVertex != null)
                {
                    SelectIncidentEdges(oClickedVertex, 
                        eVertexCommand == WorksheetContextMenuManager.
                            VertexCommand.SelectIncidentEdges);
                }

                break;

            case WorksheetContextMenuManager.VertexCommand.
                EditVertexAttributes:

                EditVertexAttributes();
                break;

            case WorksheetContextMenuManager.VertexCommand.SelectSubgraphs:

                SelectSubgraphs(oClickedVertex);
                break;

            default:

                Debug.Assert(false);
                break;
        }
    }

    //*************************************************************************
    //  Method: WorksheetContextMenuManager_RunEdgeCommand()
    //
    /// <summary>
    /// Handles the RunEdgeCommand event on the WorksheetContextMenuManager
    /// object.
    /// </summary>
    ///
    /// <param name="sender">
    /// Standard event argument.
    /// </param>
    ///
    /// <param name="e">
    /// Standard event argument.
    /// </param>
    //*************************************************************************

    protected void
    WorksheetContextMenuManager_RunEdgeCommand
    (
        Object sender,
        RunEdgeCommandEventArgs e
    )
    {
        AssertValid();

        // Ge the edge corresponding to the row the user right-clicked in the
        // edge worksheet.  This can be null.

        IEdge oClickedEdge = WorksheetContextMenuManagerRowIDToEdge(
            e.EdgeRowID);

        WorksheetContextMenuManager.EdgeCommand eEdgeCommand = e.EdgeCommand;

        switch (eEdgeCommand)
        {
            case WorksheetContextMenuManager.EdgeCommand.SelectAllEdges:
            case WorksheetContextMenuManager.EdgeCommand.DeselectAllEdges:

                SelectAllEdges(eEdgeCommand ==
                    WorksheetContextMenuManager.EdgeCommand.SelectAllEdges);

                break;

            case WorksheetContextMenuManager.EdgeCommand.
                SelectAdjacentVertices:

            case WorksheetContextMenuManager.EdgeCommand.
                DeselectAdjacentVertices:

                if (oClickedEdge != null)
                {
                    SelectAdjacentVertices(oClickedEdge, 
                        eEdgeCommand == WorksheetContextMenuManager.
                            EdgeCommand.SelectAdjacentVertices);
                }

                break;

            default:

                Debug.Assert(false);
                break;
        }
    }

    //*************************************************************************
    //  Method: Ribbon_RibbonControlsChanged()
    //
    /// <summary>
    /// Handles the RibbonControlsChanged event on the Ribbon object.
    /// </summary>
    ///
    /// <param name="sender">
    /// Standard event argument.
    /// </param>
    ///
    /// <param name="e">
    /// Standard event argument.
    /// </param>
    //*************************************************************************

    protected void
    Ribbon_RibbonControlsChanged
    (
        Object sender,
        RibbonControlsChangedEventArgs e
    )
    {
        AssertValid();

        RibbonControls eRibbonControls = e.RibbonControls;

        if ( (eRibbonControls & RibbonControls.Layout) != 0 )
        {
            LayoutManager_LayoutChanged(sender, e);
        }

        if ( (eRibbonControls & RibbonControls.ShowGraphLegend) != 0 )
        {
            this.ShowGraphLegend = m_oRibbon.ShowGraphLegend;
        }

        if ( (eRibbonControls & RibbonControls.ShowGraphAxes) != 0 )
        {
            this.ShowGraphAxes = m_oRibbon.ShowGraphAxes;
        }
    }

    //*************************************************************************
    //  Method: Ribbon_RunRibbonCommand()
    //
    /// <summary>
    /// Handles the RunRibbonCommand event on the Ribbon object.
    /// </summary>
    ///
    /// <param name="sender">
    /// Standard event argument.
    /// </param>
    ///
    /// <param name="e">
    /// Standard event argument.
    /// </param>
    //*************************************************************************

    protected void
    Ribbon_RunRibbonCommand
    (
        Object sender,
        RunRibbonCommandEventArgs e
    )
    {
        AssertValid();

        switch (e.RibbonCommand)
        {
            case RibbonCommand.ShowDynamicFilters:

                ShowDynamicFilters();
                break;

            case RibbonCommand.ReadWorkbook:

                ReadWorkbook();
                break;

            case RibbonCommand.EditLayoutUserSettings:

                EditLayoutUserSettings();
                break;

            default:

                Debug.Assert(false);
                break;
        }
    }

    //*************************************************************************
    //  Method: TaskPane_Resize()
    //
    /// <summary>
    /// Handles the Resize event.
    /// </summary>
    ///
    /// <param name="sender">
    /// Standard event argument.
    /// </param>
    ///
    /// <param name="e">
    /// Standard event argument.
    /// </param>
    //*************************************************************************

    private void
    TaskPane_Resize
    (
        object sender,
        EventArgs e
    )
    {
        // Until version 1.0.1.110, the splLegend SplitContainer control, which
        // occupies the entire space beneath the TaskPane's toolbar, had its
        // Anchor property set to Top|Left|Right|Bottom.  That usually worked
        // fine, but in some cases splLegend would end up with a height that
        // was greater than the TaskPane's height.  That resulted in the graph
        // being drawn below the bottom of the graph pane.
        //
        // Here is one repro case:
        //
        // 1. Create a graph.
        //
        // 2. Export the selection to a new workbook.
        //
        // The new workbook is where the problem could be seen.
        //
        // Is this a bug in the SplitContainer control?  There is at least one
        // other easily reproducible layout bug in that control (see the
        // comments in splLegend_Panel2_Resize() for details), so that is a
        // possibility.
        //
        // To work around this problem, the Anchor property of splLegend was
        // changed to the default Top|Left, and the following code was added to
        // do the right and bottom anchoring manually.

        splLegend.Size = new Size(this.Width, this.Height - splLegend.Top);
    }

    //*************************************************************************
    //  Method: splLegend_Panel2_Resize()
    //
    /// <summary>
    /// Handles the Resize event on the splLegend_Panel2 Panel.
    /// </summary>
    ///
    /// <param name="sender">
    /// Standard event argument.
    /// </param>
    ///
    /// <param name="e">
    /// Standard event argument.
    /// </param>
    //*************************************************************************

    private void
    splLegend_Panel2_Resize
    (
        object sender,
        EventArgs e
    )
    {
        // The usrAutoFillResultsLegend and usrDynamicFiltersLegend controls
        // are stacked within a scrollable Panel, splLegend.Panel2.  Adjust
        // their widths to make them fill the width of the Panel.
        //
        // Why not just set their Anchor to Top|Left|Right to have the Windows
        // Forms layout engine do this automatically?  That doesn't work, due
        // to the following bug:
        //
        // If you put a control in a SplitContainer's lower Panel, anchor the
        // control to the left and right edges of the Panel, and initially
        // collapse the Panel (SplitContainer.Panel2Collapsed = true), the
        // control will be wider than the Panel when the Panel is uncollapsed
        // at runtime.  This can be reproduced in a simple Windows Forms
        // application, so it is not limited to NodeXL.

        usrAutoFillResultsLegend.Width = usrDynamicFiltersLegend.Width
            = splLegend.Panel2.Width;
    }

    //*************************************************************************
    //  Method: usrAutoFillResultsLegend_Resize()
    //
    /// <summary>
    /// Handles the Resize event on the usrAutoFillResultsLegend control.
    /// </summary>
    ///
    /// <param name="sender">
    /// Standard event argument.
    /// </param>
    ///
    /// <param name="e">
    /// Standard event argument.
    /// </param>
    //*************************************************************************

    private void
    usrAutoFillResultsLegend_Resize
    (
        object sender,
        EventArgs e
    )
    {
        // Don't do this.  On large-font machines, the child controls get
        // resized before the TaskPane is fully initialized.

        // AssertValid();

        // The usrAutoFillResultsLegend and usrDynamicFiltersLegend controls
        // are stacked within a scrollable Panel, splLegend.Panel2.  Each
        // dynamically sets its height to accommodate its contents.  When the
        // upper control, usrAutoFillResultsLegend, changes height, the top of
        // the lower control must be adjusted.

        usrDynamicFiltersLegend.Top = usrAutoFillResultsLegend.Bottom + 1;
    }

    //*************************************************************************
    //  Method: LayoutManager_LayoutChanged()
    //
    /// <summary>
    /// Handles all LayoutChanged events.
    /// </summary>
    ///
    /// <param name="sender">
    /// Standard event argument.
    /// </param>
    ///
    /// <param name="e">
    /// Standard event argument.
    /// </param>
    //*************************************************************************

    protected void
    LayoutManager_LayoutChanged
    (
        object sender,
        EventArgs e
    )
    {
        AssertValid();

        if (m_bHandlingLayoutChanged)
        {
            // Prevent an endless loop when the other layout manager's Layout
            // property is set below.

            return;
        }

        m_bHandlingLayoutChanged = true;

        // The user just selected a layout via one of the Layout menus.
        // Synchronize the layout managers that manage those menus.

        if (sender == m_oLayoutManagerForToolStripSplitButton)
        {
            m_oRibbon.Layout =
            m_oLayoutManagerForContextMenu.Layout =
                m_oLayoutManagerForToolStripSplitButton.Layout;
        }
        else if (sender == m_oLayoutManagerForContextMenu)
        {
            m_oRibbon.Layout =
            m_oLayoutManagerForToolStripSplitButton.Layout =
                m_oLayoutManagerForContextMenu.Layout;
        }
        else if (sender == m_oRibbon)
        {
            m_oLayoutManagerForContextMenu.Layout =
            m_oLayoutManagerForToolStripSplitButton.Layout =
                m_oRibbon.Layout;
        }
        else
        {
            Debug.Assert(false);
        }

        // Save the new layout.

        LayoutUserSettings oLayoutUserSettings = new LayoutUserSettings();

        oLayoutUserSettings.Layout =
            m_oLayoutManagerForToolStripSplitButton.Layout;

        oLayoutUserSettings.Save();
        ApplyLayoutUserSettings(oLayoutUserSettings);

        // If the layout was just changed from Null to something else and the
        // X and Y columns were autofilled, the X and Y autofill results need
        // to be cleared.

        if (!this.LayoutIsNull)
        {
            PerWorkbookSettings oPerWorkbookSettings =
                this.PerWorkbookSettings;

            AutoFillWorkbookResults oAutoFillWorkbookResults =
                oPerWorkbookSettings.AutoFillWorkbookResults;
                
            if (oAutoFillWorkbookResults.VertexXResults.ColumnAutoFilled)
            {
                oAutoFillWorkbookResults.VertexXResults =
                    new AutoFillNumericRangeColumnResults();

                oAutoFillWorkbookResults.VertexYResults =
                    new AutoFillNumericRangeColumnResults();

                // The PerWorkbookSettings object doesn't persist its settings
                // to the workbook unless one of its own properties is set.

                oPerWorkbookSettings.AutoFillWorkbookResults =
                    oAutoFillWorkbookResults;
                
                UpdateAxes(oPerWorkbookSettings);
            }
        }

        m_bHandlingLayoutChanged = false;
    }


    //*************************************************************************
    //  Method: AssertValid()
    //
    /// <summary>
    /// Asserts if the object is in an invalid state.  Debug-only.
    /// </summary>
    //*************************************************************************

    [Conditional("DEBUG")]

    public void
    AssertValid()
    {
        Debug.Assert(oNodeXLControl != null);
        Debug.Assert(m_oNodeXLWithAxesControl != null);
        Debug.Assert(m_oWorkbook != null);
        Debug.Assert(m_oRibbon != null);
        Debug.Assert(m_iTemplateVersion > 0);

        Debug.Assert(m_oLayoutManagerForToolStripSplitButton != null);
        Debug.Assert(m_oLayoutManagerForContextMenu != null);

        // m_bHandlingLayoutChanged

        // m_iEnableGraphControlsCount
        // m_oEdgeRowIDDictionary
        // m_oVertexRowIDDictionary
        // m_oSaveGraphImageFileDialog

        if (m_oDynamicFilterDialog != null)
        {
            Debug.Assert( m_oDynamicFilterDialog.Tag is
                Dictionary<Int32, Char>[] );
        }
    }


    //*************************************************************************
    //  Protected constants
    //*************************************************************************

    /// Message that explains how to set the layout type.

    protected const String HowToSetLayoutType =
        "The Layout can be set in the Excel Ribbon using NodeXL, Graph,"
        + " Layout."
        ;


    //*************************************************************************
    //  Protected fields
    //*************************************************************************

    /// The control that displays the graph.

    protected ExcelTemplateNodeXLControl oNodeXLControl;

    /// The parent of oNodeXLControl.

    protected NodeXLWithAxesControl m_oNodeXLWithAxesControl;

    /// The workbook attached to this TaskPane.

    protected Microsoft.Office.Interop.Excel.Workbook m_oWorkbook;

    /// The application's ribbon.

    protected Ribbon m_oRibbon;

    /// Template version number.

    protected Int32 m_iTemplateVersion;

    /// Helper objects for managing layouts.

    protected LayoutManagerForToolStripSplitButton
        m_oLayoutManagerForToolStripSplitButton;
    ///
    protected LayoutManagerForMenu m_oLayoutManagerForContextMenu;

    /// true if the LayoutChanged event on a layout manager is being handled.

    protected Boolean m_bHandlingLayoutChanged;

    /// Gets incremented by EnableGraphControls(true) and decremented by
    /// EnableGraphControls(false).

    protected Int32 m_iEnableGraphControlsCount;

    /// Dictionary that maps edge row IDs stored in the edge worksheet to edge
    /// objects in the graph, or null if ReadWorkbook() hasn't been called.
    /// The edge row IDs stored in the worksheet are different from IEdge.ID,
    /// which the edge worksheet knows nothing about.

    protected Dictionary<Int32, IIdentityProvider> m_oEdgeRowIDDictionary;

    /// Ditto for vertices.

    protected Dictionary<Int32, IIdentityProvider> m_oVertexRowIDDictionary;

    /// Dialog for saving images, or null if an image hasn't been saved yet.
    /// This is kept as a field instead of being created each time an image is
    /// saved so that the dialog retains its file type setting.

    protected SaveGraphImageFileDialog m_oSaveGraphImageFileDialog;

    /// Modeless dialog for dynamically filtering vertices and edges in the
    /// graph.  The dialog is created on demand, so this is initially null,
    /// then gets set to a dialog when the user wants to use dynamic filters,
    /// then gets reset to null when the user closes the dialog.

    protected DynamicFilterDialog m_oDynamicFilterDialog;
}

}
