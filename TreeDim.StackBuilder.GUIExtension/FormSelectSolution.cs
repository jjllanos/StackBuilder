﻿#region Using directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using log4net;


using Sharp3D.Math.Core;

using treeDiM.StackBuilder.Basics;
using treeDiM.StackBuilder.Graphics;
using treeDiM.StackBuilder.Reporting;

using treeDiM.StackBuilder.GUIExtension.Properties;
#endregion

namespace treeDiM.StackBuilder.GUIExtension
{
    public partial class FormSelectSolution : Form, IDrawingContainer
    {
        #region Constructor
        public FormSelectSolution(Document doc, CasePalletAnalysis analysis)
        {
            _document = doc;
            _analysis = analysis;
            InitializeComponent();
        }
        #endregion

        #region Form override
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            graphCtrlSolution.DrawingContainer = this;
            // fill grid
            FillGrid();

            gridSolutions.Selection.SelectionChanged += new SourceGrid.RangeRegionChangedEventHandler(onGridSolutionSelectionChanged);
        }
        #endregion

        #region SourceGrid related properties / methods
        /// <summary>
        /// Fill SourceGrid control with analysis solutions
        /// </summary>
        private void FillGrid()
        {
            // fill grid solution
            gridSolutions.Rows.Clear();

            // border
            DevAge.Drawing.BorderLine border = new DevAge.Drawing.BorderLine(Color.DarkBlue, 1);
            DevAge.Drawing.RectangleBorder cellBorder = new DevAge.Drawing.RectangleBorder(border, border);

            // views
            CellBackColorAlternate viewNormal = new CellBackColorAlternate(Color.LightBlue, Color.White);
            viewNormal.Border = cellBorder;
            CheckboxBackColorAlternate viewNormalCheck = new CheckboxBackColorAlternate(Color.LightBlue, Color.White);
            viewNormalCheck.Border = cellBorder;

            // column header view
            SourceGrid.Cells.Views.ColumnHeader viewColumnHeader = new SourceGrid.Cells.Views.ColumnHeader();
            DevAge.Drawing.VisualElements.ColumnHeader backHeader = new DevAge.Drawing.VisualElements.ColumnHeader();
            backHeader.BackColor = Color.LightGray;
            backHeader.Border = DevAge.Drawing.RectangleBorder.NoBorder;
            viewColumnHeader.Background = backHeader;
            viewColumnHeader.ForeColor = Color.White;
            viewColumnHeader.Font = new Font("Arial", 10, FontStyle.Bold);
            viewColumnHeader.ElementSort.SortStyle = DevAge.Drawing.HeaderSortStyle.None;

            // create the grid
            gridSolutions.BorderStyle = BorderStyle.FixedSingle;

            gridSolutions.ColumnsCount = 7;
            gridSolutions.FixedRows = 1;
            gridSolutions.Rows.Insert(0);

            // header
            SourceGrid.Cells.ColumnHeader columnHeader;

            columnHeader = new SourceGrid.Cells.ColumnHeader(Resources.ID_INDEX);
            columnHeader.AutomaticSortEnabled = false;
            columnHeader.View = viewColumnHeader;
            gridSolutions[0, 0] = columnHeader;

            columnHeader = new SourceGrid.Cells.ColumnHeader(Resources.ID_LAYERPATTERN);
            columnHeader.AutomaticSortEnabled = false;
            columnHeader.View = viewColumnHeader;
            gridSolutions[0, 1] = columnHeader;

            columnHeader = new SourceGrid.Cells.ColumnHeader(Resources.ID_CASECOUNT);
            columnHeader.AutomaticSortEnabled = false;
            columnHeader.View = viewColumnHeader;
            gridSolutions[0, 2] = columnHeader;

            columnHeader = new SourceGrid.Cells.ColumnHeader(Resources.ID_VOLUMEEFFICIENCY);
            columnHeader.AutomaticSortEnabled = false;
            columnHeader.View = viewColumnHeader;
            gridSolutions[0, 3] = columnHeader;

            columnHeader = new SourceGrid.Cells.ColumnHeader(string.Format(Resources.ID_PALLETWEIGHT, "kg"));
            columnHeader.AutomaticSortEnabled = false;
            columnHeader.View = viewColumnHeader;
            gridSolutions[0, 4] = columnHeader;

            columnHeader = new SourceGrid.Cells.ColumnHeader(string.Format(Resources.ID_PALLETHEIGHT, "mm"));
            columnHeader.AutomaticSortEnabled = false;
            columnHeader.View = viewColumnHeader;
            gridSolutions[0, 5] = columnHeader;

            columnHeader = new SourceGrid.Cells.ColumnHeader(Resources.ID_PALLETLIMIT);
            columnHeader.AutomaticSortEnabled = false;
            columnHeader.View = viewColumnHeader;
            gridSolutions[0, 6] = columnHeader;

            // data rows
            int iIndex = 0;
            foreach (CasePalletSolution sol in _analysis.Solutions)
            {
                // build case count string
                string sCaseCount = string.Empty;
                if (sol.HasSameCountLayers && sol.LimitReached == CasePalletSolution.Limit.LIMIT_MAXHEIGHTREACHED)
                    sCaseCount = string.Format("{0}\n({1} * {2})", sol.CaseCount, sol.CasePerLayerCount, sol.CaseLayersCount);
                else
                    sCaseCount = string.Format("{0}", sol.CaseCount);

                // insert row
                gridSolutions.Rows.Insert(++iIndex);

                // filling columns
                gridSolutions[iIndex, 0] = new SourceGrid.Cells.Cell(string.Format("{0}", iIndex));
                {
                    Graphics2DImage graphics = new Graphics2DImage(new Size(80, 40));
                    CasePalletSolutionViewer sv = new CasePalletSolutionViewer(sol);
                    sv.Draw(graphics);
                    gridSolutions[iIndex, 1] = new SourceGrid.Cells.Image(graphics.Bitmap);
                }
                gridSolutions[iIndex, 2] = new SourceGrid.Cells.Cell(sCaseCount);
                gridSolutions[iIndex, 3] = new SourceGrid.Cells.Cell(string.Format("{0:F}", sol.VolumeEfficiencyCases));
                gridSolutions[iIndex, 4] = new SourceGrid.Cells.Cell(string.Format("{0:F}", sol.PalletWeight));
                gridSolutions[iIndex, 5] = new SourceGrid.Cells.Cell(string.Format("{0:F}", sol.PalletHeight));
                gridSolutions[iIndex, 6] = new SourceGrid.Cells.Cell(PalletSolutionLimitToString(sol.LimitReached));

                gridSolutions[iIndex, 0].View = viewNormal;
                gridSolutions[iIndex, 1].View = viewNormal;
                gridSolutions[iIndex, 2].View = viewNormal;
                gridSolutions[iIndex, 3].View = viewNormal;
                gridSolutions[iIndex, 4].View = viewNormal;
                gridSolutions[iIndex, 5].View = viewNormal;
                gridSolutions[iIndex, 6].View = viewNormal;
            }
            gridSolutions.AutoStretchColumnsToFitWidth = true;
            gridSolutions.AutoSizeCells();
            gridSolutions.Columns.StretchToFit();

            // select first solution
            gridSolutions.Selection.SelectRow(1, true);
            graphCtrlSolution.Invalidate();
        }

        private string PalletSolutionLimitToString(CasePalletSolution.Limit limit)
        {
            switch (limit)
            {
                case CasePalletSolution.Limit.LIMIT_MAXHEIGHTREACHED: return Resources.ID_PALLETMAXHEIGHT;
                case CasePalletSolution.Limit.LIMIT_MAXWEIGHTREACHED: return Resources.ID_PALLETMAXWEIGHT;
                case CasePalletSolution.Limit.LIMIT_MAXNUMBERREACHED: return Resources.ID_PALLETMAXNUMBER;
                default: return string.Empty;
            }
        }

        /// <summary>
        /// Index of solution selected in SourceGrid control
        /// </summary>
        private int CurrentSolutionIndex
        {
            get
            {
                SourceGrid.RangeRegion region = gridSolutions.Selection.GetSelectionRegion();
                int[] indexes = region.GetRowsIndex();
                // no selection -> exit
                if (indexes.Length == 0) return -1;
                // return index
                return indexes[0] - 1;
            }
        }
        /// <summary>
        /// Solution selected in SourceGrid control
        /// </summary>
        private CasePalletSolution CurrentSolution
        {
            get
            {
                int iIndexSol = CurrentSolutionIndex;
                if (-1 == iIndexSol) return null;
                else return _analysis.Solutions[iIndexSol];
            }
        }

        /// <summary>
        /// SourceGrid row selection change handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void onGridSolutionSelectionChanged(object sender, SourceGrid.RangeRegionChangedEventArgs e)
        {
            SourceGrid.RangeRegion region = gridSolutions.Selection.GetSelectionRegion();
            int[] indexes = region.GetRowsIndex();
            // no selection -> exit
            if (indexes.Length == 0) return;
            // redraw
            graphCtrlSolution.Invalidate();
        }
        #endregion

        #region Toolbar handlers
        private void ToolsGenerateReport(object sender, EventArgs e)
        {
            try
            {
                FormDefineReport formReport = new FormDefineReport();
                formReport.ProjectName = _analysis.Name;
                if (DialogResult.OK != formReport.ShowDialog())
                    return;
                // selected solution
                SelCasePalletSolution selSolution = new SelCasePalletSolution(null, _analysis, CurrentSolution);
                ReportData reportData = new ReportData(_analysis, selSolution);

                Reporter.CompanyLogo = string.Empty;
                Reporter.ImageSizeSetting = Reporter.eImageSize.IMAGESIZE_DEFAULT;
                Reporter reporter;
                if (formReport.FileExtension == "doc")
                {
                   // create "MS Word" report file
                    reporter = new ReporterMSWord(
                        reportData
                        , Settings.Default.ReportTemplatePath
                        , formReport.FilePath
                        , new Margins());
                }
                else if (formReport.FileExtension == "html")
                {
                    // create "html" report file
                    reporter = new ReporterHtml(
                        reportData
                        , Settings.Default.ReportTemplatePath
                        , formReport.FilePath);
                }
                else
                    return;

                // open file
                if (formReport.OpenGeneratedFile)
                    Process.Start(new ProcessStartInfo(formReport.FilePath));
            }
            catch (Exception ex)
            {
                _log.Error(ex.ToString());
            }    
        }
        private void ToolsGenerateStackBuilderProject(object sender, EventArgs e)
        {
            try
            {
                // create new selected solution
                _analysis.SelectSolutionByIndex(CurrentSolutionIndex);
                // show "save as" dialog 
                if (DialogResult.OK == saveFileDialogAsStb.ShowDialog())
                {
                    // save as stb document
                    _document.Write(saveFileDialogAsStb.FileName);
                    // open file
                    Process.Start(new ProcessStartInfo(saveFileDialogAsStb.FileName));
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex.ToString());
            }
        }
        #endregion

        #region Handlers
        private void onRedrawNeeded(object sender, EventArgs e)
        {
            graphCtrlSolution.Invalidate();
        }
        #endregion

        #region Drawing
        public void Draw(Graphics3DControl ctrl, Graphics3D graphics)
        {
            try
            {
                // instantiate solution viewer
                CasePalletSolutionViewer sv = new CasePalletSolutionViewer(CurrentSolution);
                sv.Draw(graphics);
            }
            catch (Exception ex)
            {
                _log.Error(ex.ToString());
            }
        }
        #endregion

        #region Data members
        private Document _document;
        private CasePalletAnalysis _analysis;
        /// <summary>
        /// log4net
        /// </summary>
        static readonly ILog _log = LogManager.GetLogger(typeof(FormSelectSolution));
        #endregion
    }

    #region CellBackColorAlternate and CellBackColorAlternateCheck
    internal class CellBackColorAlternate : SourceGrid.Cells.Views.Cell
    {
        public CellBackColorAlternate(Color firstColor, Color secondColor)
        {
            FirstBackground = new DevAge.Drawing.VisualElements.BackgroundSolid(firstColor);
            SecondBackground = new DevAge.Drawing.VisualElements.BackgroundSolid(secondColor);
        }

        private DevAge.Drawing.VisualElements.IVisualElement mFirstBackground;
        public DevAge.Drawing.VisualElements.IVisualElement FirstBackground
        {
            get { return mFirstBackground; }
            set { mFirstBackground = value; }
        }

        private DevAge.Drawing.VisualElements.IVisualElement mSecondBackground;
        public DevAge.Drawing.VisualElements.IVisualElement SecondBackground
        {
            get { return mSecondBackground; }
            set { mSecondBackground = value; }
        }

        protected override void PrepareView(SourceGrid.CellContext context)
        {
            base.PrepareView(context);

            if (Math.IEEERemainder(context.Position.Row, 2) == 0)
                Background = FirstBackground;
            else
                Background = SecondBackground;
        }
    }

    internal class CheckboxBackColorAlternate : SourceGrid.Cells.Views.CheckBox
    {
        public CheckboxBackColorAlternate(Color firstColor, Color secondColor)
        {
            FirstBackground = new DevAge.Drawing.VisualElements.BackgroundSolid(firstColor);
            SecondBackground = new DevAge.Drawing.VisualElements.BackgroundSolid(secondColor);
        }

        private DevAge.Drawing.VisualElements.IVisualElement mFirstBackground;
        public DevAge.Drawing.VisualElements.IVisualElement FirstBackground
        {
            get { return mFirstBackground; }
            set { mFirstBackground = value; }
        }

        private DevAge.Drawing.VisualElements.IVisualElement mSecondBackground;
        public DevAge.Drawing.VisualElements.IVisualElement SecondBackground
        {
            get { return mSecondBackground; }
            set { mSecondBackground = value; }
        }

        protected override void PrepareView(SourceGrid.CellContext context)
        {
            base.PrepareView(context);

            if (Math.IEEERemainder(context.Position.Row, 2) == 0)
                Background = FirstBackground;
            else
                Background = SecondBackground;
        }
    }
    #endregion
}
