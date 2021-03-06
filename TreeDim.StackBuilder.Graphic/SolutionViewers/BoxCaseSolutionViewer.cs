﻿#region Using directives
using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

using treeDiM.StackBuilder.Basics;
using Sharp3D.Math.Core;
#endregion

namespace treeDiM.StackBuilder.Graphics
{
    /// <summary>
    /// This class displays a computed box/case analysis solution
    /// using a gdi+ graphics from a control, a memory bitmap....
    /// </summary>
    public class BoxCaseSolutionViewer
    {
        #region Data members
        private BoxCaseSolution _boxCaseSolution;
        private bool _showDimensions = true;
        #endregion

        #region Constructor
        public BoxCaseSolutionViewer(BoxCaseSolution boxCaseSolution)
        {
            _boxCaseSolution = boxCaseSolution;
        }
        #endregion

        #region Public methods
        /// <summary>
        ///  Use this method when solution does not refer an analysis (e.g. when displaying CaseOptimizer result)
        /// </summary>
        public void Draw(Graphics3D graphics)
        {
            if (null == _boxCaseSolution)
                throw new Exception("No box/case solution defined!");

            BoxCaseAnalysis boxCaseAnalysis = _boxCaseSolution.Analysis;
            // retrieve case properties 
            BProperties boxProperties = boxCaseAnalysis.BProperties;
            if (null == boxProperties) return;
            BoxProperties caseProperties = boxCaseAnalysis.CaseProperties;
            // draw case (inside)
            Case case_ = new Case(caseProperties);
            case_.DrawInside(graphics, Transform3D.Identity);
            // draw solution
            uint pickId = 0;
            foreach (ILayer layer in _boxCaseSolution)
            {
                Layer3DBox blayer = layer as Layer3DBox;
                if (null != blayer)
                {
                    foreach (BoxPosition bPosition in blayer)
                        graphics.AddBox(new Box(pickId++, boxProperties, bPosition));
                }
            }
            // get case analysis
            if (_showDimensions)
            {
                graphics.AddDimensions(new DimensionCube(
                    Vector3D.Zero
                    , caseProperties.Length, caseProperties.Width, caseProperties.Height
                    , System.Drawing.Color.Black
                    , true));
                graphics.AddDimensions(new DimensionCube(
                    _boxCaseSolution.LoadBoundingBox
                    , System.Drawing.Color.Red
                    , false));
            }
        }

        /// <summary>
        /// Draw a 2D representation of first (and second, if solution does not have homogeneous layers) layer(s)
        /// The images produced are used in 
        /// </summary>
        public void Draw(Graphics2D graphics)
        {
            // access case properties
            BoxCaseAnalysis boxCaseAnalysis = _boxCaseSolution.Analysis;
            BoxProperties caseProperties = boxCaseAnalysis.CaseProperties;
            BProperties boxProperties = boxCaseAnalysis.BProperties;
            // initialize Graphics2D object
            graphics.NumberOfViews = 1;
            graphics.SetViewport(0.0f, 0.0f, (float)caseProperties.Length, (float)caseProperties.Width);
            // access first layer
            Layer3DBox blayer = _boxCaseSolution.BoxLayerFirst;
            if (null != blayer)
            {
                graphics.SetCurrentView(0);
                graphics.DrawRectangle(Vector2D.Zero, new Vector2D(caseProperties.InsideLength, caseProperties.InsideWidth), Color.Black);
                uint pickId = 0;
                foreach (BoxPosition bPosition in blayer)
                    graphics.DrawBox(new Box(pickId++, boxProperties, bPosition));
            }
        }
        #endregion

        #region Public properties
        public BoxCaseSolution Solution
        {
            get { return _boxCaseSolution; }
            set { _boxCaseSolution = value; }
        }
        public bool ShowDimensions
        {
            get { return _showDimensions; }
            set { _showDimensions = value; }
        }
        #endregion
    }
}
