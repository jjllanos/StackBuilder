﻿#region Using directives
using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Diagnostics;
using System.Linq;

using Sharp3D.Math.Core;
using treeDiM.StackBuilder.Basics;
#endregion

namespace treeDiM.StackBuilder.Graphics
{
    #region Box
    /// <summary>
    /// This class used to draw any brick with side parallel to axes and normal oriented to exterior
    /// Use method Graphics.Draw(Box box) to draw as ordered boxel
    /// </summary>
    public class Box : Drawable
    {
        #region Data members
        protected uint _pickId = 0;
        protected double[] _dim = new double[3];
        protected BoxPosition _boxPosition;
        private Color[] _colors;
        private List<Texture>[] _textureLists = new List<Texture>[6];
        /// <summary>
        /// Bundle properties
        /// </summary>
        private bool _isBundle = false;
        private int _noFlats = 0;
        /// <summary>
        /// Tape related properties
        /// </summary>
        private bool _showTape = false;
        private double _tapeWidth;
        private Color _tapeColor;
        /// <summary>
        /// Packable object replacable with in memory built image
        /// </summary>
        private Packable _packable; 
        #endregion

        #region Constructor
        public Box(uint pickId, double length, double width, double height)
        {
            _pickId = pickId;
            // dimensions
            _dim[0] = length;
            _dim[1] = width;
            _dim[2] = height;
            // box position
            _boxPosition = new BoxPosition(Vector3D.Zero);
            // colors & texturezs
            _colors = new Color[6];
            _colors[0] = Color.Red;
            _colors[1] = Color.Red;
            _colors[2] = Color.Green;
            _colors[3] = Color.Green;
            _colors[4] = Color.Blue;
            _colors[5] = Color.Blue;

            for (int i = 0; i < 6; ++i)
                _textureLists[i] = null;
        }
        public Box(uint pickId, double length, double width, double height, BoxPosition boxPosition)
        {
            _pickId = pickId;
            // dimensions
            _dim[0] = length;
            _dim[1] = width;
            _dim[2] = height;
            // box position
            _boxPosition = boxPosition;
            // colors & textures
            _colors = new Color[6];
            _colors[0] = Color.Red;
            _colors[1] = Color.Red;
            _colors[2] = Color.Green;
            _colors[3] = Color.Green;
            _colors[4] = Color.Blue;
            _colors[5] = Color.Blue;

            for (int i = 0; i < 6; ++i)
                _textureLists[i] = null;

        }
        public Box(uint pickId, PackableBrick packable)
        {
            _pickId = pickId;
            // dimensions
            _dim[0] = packable.Length;
            _dim[1] = packable.Width;
            _dim[2] = packable.Height;
            // box position
            _boxPosition = new BoxPosition(Vector3D.Zero, HalfAxis.HAxis.AXIS_X_P, HalfAxis.HAxis.AXIS_Y_P);
            // colors & textures
            _colors = Enumerable.Repeat<Color>(Color.Chocolate, 6).ToArray();

            if (packable is LoadedPallet)
            {
                _packable = packable;
            }
            else
            {
                BProperties bProperties = PackableToBProperties(packable);
                if (null != bProperties)
                {
                    _colors = bProperties.Colors;
                    // IsBundle ?
                    _isBundle = bProperties.IsBundle;
                    if (_isBundle)
                        _noFlats = (bProperties as BundleProperties).NoFlats;
                    // textures
                    BoxProperties boxProperties = bProperties as BoxProperties;
                    if (null != boxProperties)
                    {
                        List<Pair<HalfAxis.HAxis, Texture>> textures = boxProperties.TextureList;
                        foreach (Pair<HalfAxis.HAxis, Texture> tex in textures)
                        {
                            int iIndex = (int)tex.first;
                            if (null == _textureLists[iIndex])
                                _textureLists[iIndex] = new List<Texture>();
                            _textureLists[iIndex].Add(tex.second);
                        }
                        // tape
                        _showTape = boxProperties.ShowTape;
                        _tapeWidth = boxProperties.TapeWidth;
                        _tapeColor = boxProperties.TapeColor;
                    }
                }
            }
        }
        public Box(uint pickId, PalletCapProperties capProperties, Vector3D position)
        {
            _pickId = pickId;
            // dimensions
            _dim[0] = capProperties.Length;
            _dim[1] = capProperties.Width;
            _dim[2] = capProperties.Height;
            // box position
            _boxPosition = new BoxPosition(position, HalfAxis.HAxis.AXIS_X_P, HalfAxis.HAxis.AXIS_Y_P);
            // colors & textures
            _colors = new Color[6];
            this.SetAllFacesColor(capProperties.Color);
        }
        public Box(uint pickId, PackableBrick packable, BoxPosition bPosition)
        {
            // sanity checks
            CheckPosition(bPosition);
            // dimensions
            _pickId = pickId;
            _dim[0] = packable.Length;
            _dim[1] = packable.Width;
            _dim[2] = packable.Height;
            // set position
            _boxPosition = bPosition;
            // colors
            _colors = Enumerable.Repeat<Color>(Color.Chocolate, 6).ToArray();

            BProperties bProperties = PackableToBProperties(packable);
            if (null == bProperties)
                throw new Exception(string.Format("Type {0} cannot be handled by Box constructor", packable.GetType().ToString() ));

            _colors = bProperties.Colors;
            _isBundle = bProperties.IsBundle;
             // is box ?
            BoxProperties boxProperties = bProperties as BoxProperties;
            if (null != boxProperties)
            {
                List<Pair<HalfAxis.HAxis, Texture>> textures = boxProperties.TextureList;
                foreach (Pair<HalfAxis.HAxis, Texture> tex in textures)
                {
                    int iIndex = (int)tex.first;
                    if (null == _textureLists[iIndex])
                        _textureLists[iIndex] = new List<Texture>();
                    _textureLists[iIndex].Add(tex.second);
                }
                _showTape = boxProperties.ShowTape;
                _tapeWidth = boxProperties.TapeWidth;
                _tapeColor = boxProperties.TapeColor;
            }
            // is bundle ?
            else if (bProperties.IsBundle)
            {
                BundleProperties bundleProp = bProperties as BundleProperties;
                if (null != bundleProp)
                    _noFlats = bundleProp.NoFlats;
            }
        }

        public Box(uint pickId, PackableBrick packable, LayerPosition bPosition)
        {
            // sanity checks
            CheckPosition(bPosition);
            // dimensions
            _pickId = pickId;
            _dim[0] = packable.Length;
            _dim[1] = packable.Width;
            _dim[2] = packable.Height;
            // set position
            _boxPosition = new BoxPosition( bPosition.Position, bPosition.LengthAxis, bPosition.WidthAxis);
            // colors
            _colors = Enumerable.Repeat<Color>(Color.Chocolate, 6).ToArray();

            BProperties bProperties = PackableToBProperties(packable);
            if (null != bProperties)
            {
                _colors = bProperties.Colors;

                BoxProperties boxProperties = bProperties as BoxProperties;
                if (null != boxProperties)
                {
                    List<Pair<HalfAxis.HAxis, Texture>> textures = boxProperties.TextureList;
                    foreach (Pair<HalfAxis.HAxis, Texture> tex in textures)
                    {
                        int iIndex = (int)tex.first;
                        if (null == _textureLists[iIndex])
                            _textureLists[iIndex] = new List<Texture>();
                        _textureLists[iIndex].Add(tex.second);
                    }
                    _showTape = boxProperties.ShowTape;
                    _tapeWidth = boxProperties.TapeWidth;
                    _tapeColor = boxProperties.TapeColor;
                }
                // IsBundle ?
                _isBundle = bProperties.IsBundle;
                if (bProperties.IsBundle)
                {
                    BundleProperties bundleProp = packable as BundleProperties;
                    if (null != bundleProp)
                        _noFlats = bundleProp.NoFlats;
                }
            }
            PackProperties packProperties = packable as PackProperties;
            if (null != packProperties)
            { 
            }            
        }

        public Box(uint pickId, PackProperties packProperties, BoxPosition bPosition)
        {
            // sanity checks
            CheckPosition(bPosition);
            // dimensions
            _pickId = pickId;
            _dim[0] = packProperties.Length;
            _dim[1] = packProperties.Width;
            _dim[2] = packProperties.Height;
            // colors
            _colors = Enumerable.Repeat<Color>(Color.Chocolate, 6).ToArray();
            // set position
            _boxPosition = bPosition;
        }

        public Box(uint pickId, InterlayerProperties interlayerProperties)
        {
            // dimensions
            _pickId = pickId;
            _dim[0] = interlayerProperties.Length;
            _dim[1] = interlayerProperties.Width;
            _dim[2] = interlayerProperties.Thickness;
            // colors
            _colors = Enumerable.Repeat<Color>(interlayerProperties.Color, 6).ToArray();
        }

        public Box(uint pickId, InterlayerProperties interlayerProperties, BoxPosition bPosition)
        {
            // dimensions
            _pickId = pickId;
            _dim[0] = interlayerProperties.Length;
            _dim[1] = interlayerProperties.Width;
            _dim[2] = interlayerProperties.Thickness;
            // colors
            _colors = Enumerable.Repeat<Color>(interlayerProperties.Color, 6).ToArray();
            // set position
            _boxPosition = bPosition;
        }

        public Box(uint pickId, BundleProperties bundleProperties)
        {
            // dimensions
            _pickId = pickId;
            _dim[0] = bundleProperties.Length;
            _dim[1] = bundleProperties.Width;
            _dim[2] = bundleProperties.Height;
            // colors
            _colors = Enumerable.Repeat<Color>(bundleProperties.Color, 6).ToArray();
            // specific
            _noFlats = bundleProperties.NoFlats;
            _isBundle = bundleProperties.IsBundle;
        }
        #endregion

        #region Helpers
        private BProperties PackableToBProperties(Packable packable)
        {
            if (packable is BProperties)
                return packable as BProperties;
            else if (packable is LoadedCase)
            {
                LoadedCase loadedCase = packable as LoadedCase;
                return PackableToBProperties(loadedCase.Container);
            }
            else if (packable is LoadedPallet)
            {
                LoadedPallet loadedPallet = packable as LoadedPallet;
                Vector3D dim = loadedPallet.OuterDimensions;
                BoxProperties boxProperties = new BoxProperties(loadedPallet.ParentDocument, dim.X, dim.Y, dim.Z);
                boxProperties.SetColor(Color.Chocolate);
                return boxProperties;
            }
            else
                return null;
        }
        #endregion

        #region Public properties
        public uint PickId
        {
            get { return _pickId; }
        }
        public Vector3D Position
        {
            get { return _boxPosition.Position; }
            set { _boxPosition.Position = value; }
        }
        public double Length
        {
            get { return _dim[0]; }
        }
        public double Width
        {
            get { return _dim[1]; }
        }
        public double Height
        {
            get { return _dim[2]; }
        }
        public HalfAxis.HAxis HLengthAxis
        {
            get { return _boxPosition.DirectionLength;  }
            set { _boxPosition.DirectionLength = value; }
        }
        public HalfAxis.HAxis HWidthAxis
        {
            get { return _boxPosition.DirectionWidth;  }
            set { _boxPosition.DirectionWidth = value; }
        }
        public Vector3D LengthAxis
        {   get { return HalfAxis.ToVector3D(_boxPosition.DirectionLength); } }
        public Vector3D WidthAxis
        {   get { return HalfAxis.ToVector3D(_boxPosition.DirectionWidth); } }
        public Vector3D HeightAxis
        { get { return Vector3D.CrossProduct(LengthAxis, WidthAxis); } }

        public Color[] Colors
        {
            get { return _colors; }
        }

        public bool IsBundle
        {
            get { return _isBundle; }
            set { _isBundle = value; }
        }
        public int BundleFlats
        {
            get
            {
                return _noFlats; 
            }
        }

        public Face TopFace
        {
            get
            {
                Face[] faces = Faces;
                Face topFace = faces[0];
                foreach (Face face in faces)
                    if (face.Center.Z > topFace.Center.Z)
                        topFace = face;
                return topFace;
            }
        }

        public Vector3D[] Oriented(Vector3D pt0, Vector3D pt1, Vector3D pt2, Vector3D pt3, Vector3D pt)
        {
            Vector3D crossProduct = Vector3D.CrossProduct(pt1 - pt0, pt2 - pt0);
            if (Vector3D.DotProduct(crossProduct, pt - pt0) < 0)
                return new Vector3D[] { pt0, pt1, pt2, pt3 };
            else
                return new Vector3D[] { pt3, pt2, pt1, pt0 };
        }

        public Vector3D Center
        {
            get
            {
                return _boxPosition.Position
                    + 0.5 * _dim[0] * LengthAxis
                    + 0.5 * _dim[1] * WidthAxis
                    + 0.5 * _dim[2] * HeightAxis;
            }
        }

        public bool ShowTape
        {
            get { return _showTape; }
            set { _showTape = value; }
        }

        public double TapeWidth
        {
            get { return _tapeWidth; }
        }

        public Color TapeColor
        {
            get { return _tapeColor; }
        }

        public Vector3D[] TapePoints
        {
            get
            {
                Vector3D lengthAxis = LengthAxis;
                Vector3D widthAxis  = WidthAxis;
                Vector3D heightAxis = HeightAxis;
                Vector3D[] points = new Vector3D[4];
                points[0] = Position + 0.0 * lengthAxis     + 0.5 * (_dim[1] - _tapeWidth) * widthAxis + _dim[2] * heightAxis;
                points[1] = Position + _dim[0] * lengthAxis + 0.5 * (_dim[1] - _tapeWidth) * widthAxis + _dim[2] * heightAxis;
                points[2] = Position + _dim[0] * lengthAxis + 0.5 * (_dim[1] + _tapeWidth) * widthAxis + _dim[2] * heightAxis;
                points[3] = Position + 0.0 * lengthAxis     + 0.5 * (_dim[1] + _tapeWidth) * widthAxis + _dim[2] * heightAxis;
                return points;
            }
        }
        #endregion

        #region XMin / XMax / YMin / YMax / ZMin
        public double XMin
        {
            get
            {
                double xmin = double.MaxValue;
                foreach (Vector3D v in this.Points)
                {
                    if (v.X < xmin)
                        xmin = v.X;
                }
                return xmin;
            }
        }
        public double XMax
        {
            get
            {
                double xmax = double.MinValue;
                foreach (Vector3D v in this.Points)
                {
                    if (v.X > xmax)
                        xmax = v.X;
                }
                return xmax;
            }
        }
        public double YMin
        {
            get
            {
                double ymin = double.MaxValue;
                foreach (Vector3D v in this.Points)
                {
                    if (v.Y < ymin)
                        ymin = v.Y;
                }
                return ymin;
            }
        }
        public double YMax
        {
            get
            {
                double ymax = double.MinValue;
                foreach (Vector3D v in this.Points)
                {
                    if (v.Y > ymax)
                        ymax = v.Y;
                }
                return ymax;
            }
        }

        public double ZMin
        {
            get
            {
                double zmin = double.MaxValue;
                foreach (Vector3D v in this.Points)
                    zmin = Math.Min(v.Z, zmin);
                return zmin;
            }
        }
        public BBox3D BBox
        {
            get
            {
                BBox3D bbox = new BBox3D();
                foreach (Vector3D v in this.Points)
                    bbox.Extend(v);
                return bbox;
            }
        }
        #endregion

        #region Points / Faces
        public Vector3D[] Points
        {
            get
            {
                Vector3D position = _boxPosition.Position;
                Vector3D lengthAxis = LengthAxis;
                Vector3D widthAxis = WidthAxis;
                Vector3D heightAxis = HeightAxis;
                Vector3D[] points = new Vector3D[8];
                points[0] = position;
                points[1] = position + _dim[0] * lengthAxis;
                points[2] = position + _dim[0] * lengthAxis + _dim[1] * widthAxis;
                points[3] = position + _dim[1] * widthAxis;

                points[4] = position + _dim[2] * heightAxis;
                points[5] = position + _dim[2] * heightAxis + _dim[0] * lengthAxis;
                points[6] = position + _dim[2] * heightAxis + _dim[0] * lengthAxis + _dim[1] * widthAxis;
                points[7] = position + _dim[2] * heightAxis + _dim[1] * widthAxis;

                return points;
            }
        }

        public Vector3D[] PointsSmallOffset
        {
            get
            {
                Vector3D lengthAxis = LengthAxis;
                Vector3D widthAxis = WidthAxis;
                Vector3D heightAxis = HeightAxis;
                const double offset = 3.0;
                Vector3D[] points = new Vector3D[8];
                Vector3D origin = new Vector3D(offset, offset, offset);
                points[0] = origin;
                points[1] = origin + (_dim[0] - 2.0 * offset) * lengthAxis;
                points[2] = origin + (_dim[0] - 2.0 * offset) * lengthAxis + (_dim[1] - 2.0 * offset) * widthAxis;
                points[3] = origin + (_dim[1] - 2.0 * offset) * widthAxis;

                points[4] = origin + (_dim[2]- 2.0 * offset) * heightAxis;
                points[5] = origin + (_dim[2]- 2.0 * offset) * heightAxis + (_dim[0]- 2.0 * offset) * lengthAxis;
                points[6] = origin + (_dim[2]- 2.0 * offset) * heightAxis + (_dim[0]- 2.0 * offset) * lengthAxis + (_dim[1]- 2.0 * offset) * widthAxis;
                points[7] = origin + (_dim[2]- 2.0 * offset) * heightAxis + (_dim[1]- 2.0 * offset) * widthAxis;

                return points;
            }
        }

        public Vector3D[] Normals
        {
            get
            {
                Vector3D[] normals = new Vector3D[6];
                normals[0] = -Vector3D.XAxis;
                normals[1] = Vector3D.XAxis;
                normals[2] = -Vector3D.YAxis;
                normals[3] = Vector3D.YAxis;
                normals[4] = -Vector3D.ZAxis;
                normals[5] = Vector3D.ZAxis;
                return normals;
            }
        }
        public Vector2D[] UVs
        {
            get
            {
                Vector2D[] uvs = new Vector2D[4];
                uvs[0] = new Vector2D(0.0, 0.0);
                uvs[1] = new Vector2D(1.0, 0.0);
                uvs[2] = new Vector2D(1.0, 1.0);
                uvs[3] = new Vector2D(0.0, 1.0);
                return uvs;
            }
        }

        public TriangleIndices[] Triangles
        {
            get
            {
                TriangleIndices[] tri = new TriangleIndices[12];
                ulong n0 = (ulong)HalfAxis.ToHalfAxis(-LengthAxis);
                tri[0] = new TriangleIndices(0, 4, 3, n0, n0, n0, 1, 3, 0);
                tri[1] = new TriangleIndices(3, 4, 7, n0, n0, n0, 0, 3, 2);
                ulong n1 = (ulong)HalfAxis.ToHalfAxis(LengthAxis);
                tri[2] = new TriangleIndices(1, 2, 5, n1, n1, n1, 0, 1, 2);
                tri[3] = new TriangleIndices(5, 2, 6, n1, n1, n1, 1, 3, 2);
                ulong n2 = (ulong)HalfAxis.ToHalfAxis(-WidthAxis);
                tri[4] = new TriangleIndices(0, 1, 4, n2, n2, n2, 0, 1, 2);
                tri[5] = new TriangleIndices(4, 1, 5, n2, n2, n2, 2, 1, 3);
                ulong n3 = (ulong)HalfAxis.ToHalfAxis(WidthAxis);
                tri[6] = new TriangleIndices(7, 6, 2, n3, n3, n3, 3, 2, 0);
                tri[7] = new TriangleIndices(7, 2, 3, n3, n3, n3, 3, 0, 1);
                ulong n4 = (ulong)HalfAxis.ToHalfAxis(-HeightAxis);
                tri[8] = new TriangleIndices(0, 3, 1, n4, n4, n4, 2, 0, 3);
                tri[9] = new TriangleIndices(1, 3, 2, n4, n4, n4, 3, 0, 1);
                ulong n5 = (ulong)HalfAxis.ToHalfAxis(HeightAxis);
                tri[10] = new TriangleIndices(4, 5, 7, n5, n5, n5, 0, 1, 2);
                tri[11] = new TriangleIndices(7, 5, 6, n5, n5, n5, 2, 1, 3);
                return tri;
            }
        }
        public TriangleIndices[] TrianglesByFace(HalfAxis.HAxis axis)
        {
            TriangleIndices[] tri = new TriangleIndices[2];
            ulong n = (ulong)axis;
            switch (axis)
            {
                case HalfAxis.HAxis.AXIS_X_N:
                    tri[0] = new TriangleIndices(0, 4, 3, n, n, n, 1, 2, 0);
                    tri[1] = new TriangleIndices(3, 4, 7, n, n, n, 0, 2, 3);
                    break;
                case HalfAxis.HAxis.AXIS_X_P:
                    tri[0] = new TriangleIndices(1, 2, 5, n, n, n, 0, 1, 3);
                    tri[1] = new TriangleIndices(5, 2, 6, n, n, n, 3, 1, 2);
                    break;
                case HalfAxis.HAxis.AXIS_Y_N:
                    tri[0] = new TriangleIndices(0, 1, 4, n, n, n, 0, 1, 3);
                    tri[1] = new TriangleIndices(4, 1, 5, n, n, n, 3, 1, 2);
                    break;
                case HalfAxis.HAxis.AXIS_Y_P:
                    tri[0] = new TriangleIndices(7, 6, 2, n, n, n, 2, 3, 0);
                    tri[1] = new TriangleIndices(7, 2, 3, n, n, n, 2, 0, 1);
                    break;
                case HalfAxis.HAxis.AXIS_Z_N:
                    tri[0] = new TriangleIndices(0, 3, 1, n, n, n, 2, 0, 3);
                    tri[1] = new TriangleIndices(1, 3, 2, n, n, n, 3, 0, 1);
                    break;
                case HalfAxis.HAxis.AXIS_Z_P:
                    tri[0] = new TriangleIndices(4, 5, 7, n, n, n, 0, 1, 2);
                    tri[1] = new TriangleIndices(7, 5, 6, n, n, n, 2, 1, 3);
                    break;
                default:
                    Debug.Assert(false);
                    break;
            }
            return tri;
        }

        public Face[] Faces
        {
            get
            {
                Vector3D position = Position;
                Vector3D lengthAxis = LengthAxis;
                Vector3D widthAxis = WidthAxis;
                Vector3D heightAxis = HeightAxis;

                Vector3D[] points = new Vector3D[8];
                points[0] = position;
                points[1] = position + _dim[0] * lengthAxis;
                points[2] = position + _dim[0] * lengthAxis + _dim[1] * widthAxis;
                points[3] = position + _dim[1] * widthAxis;

                points[4] = position + _dim[2] * heightAxis;
                points[5] = position + _dim[2] * heightAxis + _dim[0] * lengthAxis;
                points[6] = position + _dim[2] * heightAxis + _dim[0] * lengthAxis + _dim[1] * widthAxis;
                points[7] = position + _dim[2] * heightAxis + _dim[1] * widthAxis;

                Face[] faces = new Face[6];
                faces[0] = new Face(_pickId, new Vector3D[] { points[3], points[0], points[4], points[7] }, false); // AXIS_X_N
                faces[1] = new Face(_pickId, new Vector3D[] { points[1], points[2], points[6], points[5] }, false); // AXIS_X_P
                faces[2] = new Face(_pickId, new Vector3D[] { points[0], points[1], points[5], points[4] }, false); // AXIS_Y_N
                faces[3] = new Face(_pickId, new Vector3D[] { points[2], points[3], points[7], points[6] }, false); // AXIS_Y_P
                faces[4] = new Face(_pickId, new Vector3D[] { points[3], points[2], points[1], points[0] }, false); // AXIS_Z_N
                faces[5] = new Face(_pickId, new Vector3D[] { points[4], points[5], points[6], points[7] }, false); // AXIS_Z_P

                int i = 0;
                foreach (Face face in faces)
                {
                    face.ColorFill = _colors[i];
                    face.Textures = _textureLists[i];
                    ++i;
                }
                return faces;
            }
        }
        /// <summary>
        /// PointsImage
        /// </summary>
        /// <param name="texture">Texture (bitmap + Vector2D (position) + Vector2D(size) + double(angle))</param>
        /// <returns>array of Vector3D points</returns>
        public Vector3D[] PointsImage(int faceId, Texture texture)
        {
            Vector3D position = Position;
            Vector3D lengthAxis = LengthAxis;
            Vector3D widthAxis = WidthAxis;
            Vector3D heightAxis = HeightAxis;
            Vector3D[] points = new Vector3D[8];
            points[0] = position;
            points[1] = position + _dim[0] * lengthAxis;
            points[2] = position + _dim[0] * lengthAxis + _dim[1] * widthAxis;
            points[3] = position + _dim[1] * widthAxis;

            points[4] = position + _dim[2] * heightAxis;
            points[5] = position + _dim[2] * heightAxis + _dim[0] * lengthAxis;
            points[6] = position + _dim[2] * heightAxis + _dim[0] * lengthAxis + _dim[1] * widthAxis;
            points[7] = position + _dim[2] * heightAxis + _dim[1] * widthAxis;

            Vector3D vecI = Vector3D.XAxis, vecJ = Vector3D.YAxis, vecO = Vector3D.Zero;
            switch (faceId)
            {
                case 0: vecI = points[0] - points[3]; vecJ = points[7] - points[3]; vecO = points[3]; break;
                case 1: vecI = points[2] - points[1]; vecJ = points[5] - points[1]; vecO = points[1]; break;
                case 2: vecI = points[1] - points[0]; vecJ = points[4] - points[0]; vecO = points[0]; break;
                case 3: vecI = points[3] - points[2]; vecJ = points[6] - points[2]; vecO = points[2]; break;
                case 4: vecI = points[0] - points[1]; vecJ = points[2] - points[1]; vecO = points[1]; break;
                case 5: vecI = points[5] - points[4]; vecJ = points[7] - points[4]; vecO = points[4]; break;
                default: break;
            }
            vecI.Normalize();
            vecJ.Normalize();

            Vector3D[] pts = new Vector3D[4];
            double cosAngle = Math.Cos(texture.Angle * Math.PI / 180.0);
            double sinAngle = Math.Sin(texture.Angle * Math.PI / 180.0);

            pts[0] = vecO + texture.Position.X * vecI + texture.Position.Y * vecJ;
            pts[1] = vecO + (texture.Position.X + texture.Size.X * cosAngle) * vecI + (texture.Position.Y + texture.Size.X * sinAngle) * vecJ;
            pts[2] = vecO + (texture.Position.X + texture.Size.X * cosAngle - texture.Size.Y * sinAngle) * vecI + (texture.Position.Y + texture.Size.X * sinAngle + texture.Size.Y * cosAngle) * vecJ;
            pts[3] = vecO + (texture.Position.X - texture.Size.Y * sinAngle) * vecI + (texture.Position.Y + texture.Size.Y * cosAngle) * vecJ;
            return pts;
        }
        #endregion

        #region Validity
        public bool IsValid
        {
            get
            {
                return _dim[0] > 0.0 && _dim[1] > 0.0 && _dim[2] > 0.0 && (LengthAxis != WidthAxis);
            }
        }
        #endregion

        #region Public methods
        public override void Draw(Graphics2D graphics)
        {
            graphics.DrawBox(this);
        }
        public override void Draw(Graphics3D graphics)
        {
            foreach (Face face in Faces)
                graphics.AddFace(face);
        }

        public void SetAllFacesColor(Color color)
        {
            for (int i = 0; i < 6; ++i)
                _colors[i] = color;
        }

        public void SetFaceColor(HalfAxis.HAxis iFace, Color color)
        {
            _colors[(int)iFace] = color;
        }

        public void SetFaceTextures(HalfAxis.HAxis iFace, List<Texture> textures)
        {
            _textureLists[(int)iFace] = textures;
        }

        public bool PointInside(Vector3D pt)
        {
            foreach (Face face in Faces)
            {
                if (face.PointRelativePosition(pt) != -1)
                    return false;
            }
            return true;
        }

        public void ApplyElong(double d)
        {
            _dim[0] += d;
            _dim[1] += d;
            _dim[2] += d;
            _boxPosition.Position = _boxPosition.Position - new Vector3D(-0.5 * d, -0.5 * d, -0.5 * d);
        }

        public bool RayIntersect(Ray ray, out Vector3D ptInter)
        {
            List<Vector3D> listIntersections = new List<Vector3D>();
            foreach (Face f in Faces)
            {
                Vector3D pt;
                if (f.RayIntersect(ray, out pt))
                    listIntersections.Add(pt);
            }
            // instantiate intersection comparer
            RayIntersectionComparer comp = new RayIntersectionComparer(ray);
            // sort intersections
            listIntersections.Sort(comp);
            // save intersection point
            if (listIntersections.Count > 0)
                ptInter = listIntersections[0];
            else
                ptInter = Vector3D.Zero;
            // return true if an intersection was found
            return listIntersections.Count > 0;
        }
        #endregion

        #region Private helpers
        private void SetDimensions(PackableBrick packable)
        { 
            
        }
        private void CheckPosition(BoxPosition bPosition)
        {
            if (!bPosition.IsValid)
                throw new GraphicsException("Invalid BoxPosition: can not create box");
        }
        private void CheckPosition(LayerPosition bPosition)
        { 
            if (!bPosition.IsValid)
                throw new GraphicsException("Invalid BoxPosition: can not create box");
        }
        #endregion
    }
    #endregion
 
    #region TriangleIndices
    public class TriangleIndices
    {
        #region Constructor
        public TriangleIndices(ulong v0, ulong v1, ulong v2, ulong n0, ulong n1, ulong n2, ulong uv0, ulong uv1, ulong uv2)
        {
            _vertex[0] = v0; _vertex[1] = v1; _vertex[2] = v2;
            _normal[0] = n0; _normal[1] = n1; _normal[2] = n2;
            _UV[0] = uv0; _UV[1] = uv1; _UV[2] = uv2;
        }
        #endregion

        #region Convert to string
        public string ConvertToString(ulong iTriangleIndex)
        {
            return string.Format("{0} {1} {2} {3} {4} {5} {6} {7} {8} "
                , _vertex[0] + iTriangleIndex * 8, _normal[0], _UV[0]
                , _vertex[1] + iTriangleIndex * 8, _normal[1], _UV[1]
                , _vertex[2] + iTriangleIndex * 8, _normal[2], _UV[2]
                );
        }
        #endregion

        #region Data members
        public ulong[] _vertex = new ulong[3];
        public ulong[] _normal = new ulong[3];
        public ulong[] _UV = new ulong[3];
        #endregion
    }
    #endregion
}
