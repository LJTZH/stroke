using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.DataSourcesFile;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Carto;

namespace stroke
{
    public partial class Form1 : Form
    {
        string roadDataFullName = string.Empty;
        string shapeFileFullName = string.Empty;
        IGeometry pGPoint;
        List<IGeometry> pGeometryList = new List<IGeometry>();
        List<IGeometry> IWT = new List<IGeometry>();
        public Form1()
        {
            InitializeComponent();
        }

        private void btnOpen_Click(object sender, EventArgs e)
        {
            OpenFileDialog pOFD = new OpenFileDialog();
            pOFD.Multiselect = false;
            pOFD.Title = "打开路网文件";
            pOFD.InitialDirectory = System.IO.Directory.GetCurrentDirectory();
            pOFD.Filter = "路网文件(*.shp)|*.shp";
            if (pOFD.ShowDialog() == DialogResult.OK)
            {
                roadDataFullName = pOFD.FileName;
                this.textBox1.Text = roadDataFullName;
            }


        }

        private void btnConfirm_Click(object sender, EventArgs e)
        {
            pGeometryList = this.GetGeometry(roadDataFullName);
            this.strokeIt(pGeometryList);
        }
        private List<IGeometry> GetGeometry(string roadDataFullName)
        {
            List<IGeometry> pGList = new List<IGeometry>();
            int index = roadDataFullName.LastIndexOf('\\');
            string folder = roadDataFullName.Substring(0, index);
            shapeFileFullName = roadDataFullName.Substring(index + 1);
            IWorkspaceFactory pWSF = new ShapefileWorkspaceFactoryClass();
            IFeatureWorkspace pFWS = (IFeatureWorkspace)pWSF.OpenFromFile(folder, 0);
            IFeatureClass pFC = pFWS.OpenFeatureClass(shapeFileFullName);
            IFeatureCursor pFeatureCursor = pFC.Search(null, false);
            IFeature pFeature = pFeatureCursor.NextFeature();
            while (pFeature != null)
            {
                IGeometry pGeometry = pFeature.Shape as IGeometry;//通过shape得到Geometry
                pGList.Add(pGeometry);
                pFeature = pFeatureCursor.NextFeature();
                int i = pGList.Count;
            }
            return pGList;
        }
        private bool CheckCrosses(IGeometry pGeometry1, IGeometry pGeometry2)
        {

            IRelationalOperator pRO = pGeometry1 as IRelationalOperator;
            ITopologicalOperator pTO = pGeometry1 as ITopologicalOperator;
            if (pRO.Touches(pGeometry2))
            {
                pGPoint = pTO.Intersect(pGeometry2, esriGeometryDimension.esriGeometry0Dimension);
                return true;
            }
            else
                return false;
        }
        private double GetAngle(IGeometry pGeometry1,IGeometry pGeometry2)
        {
            double pAngle1;
            double pAngle2;
            double pAngle=0;

            IPolyline pPolyline1=pGeometry1 as IPolyline;
            IPoint pPF1 = pPolyline1.FromPoint;
            IGeometry pGeoPoint1 = pPF1 as IGeometry;
            IRelationalOperator pRO1 = pGeoPoint1 as IRelationalOperator;
            if (pRO1.Contains(pGPoint))
            {
                pAngle1=this.AngleFromPoint(pGeometry1);
            }
            else
            {
                pAngle1=this.AngleToPoint(pGeometry1);
            }

            IPolyline pPolyline2 = pGeometry2 as IPolyline;
            IPoint pPF2 = pPolyline2.FromPoint;
            IGeometry pGeoPoint2 = pPF2 as IGeometry;
            IRelationalOperator pRO2 = pGeoPoint2 as IRelationalOperator;
            if (pRO2.Contains(pGPoint))
            {
                pAngle2 = this.AngleFromPoint(pGeometry2);
            }
            else
            {
                pAngle2 = this.AngleToPoint(pGeometry2);
            }
            if ((pAngle1 * pAngle2) >= 0)
                pAngle = Math.Abs(pAngle2 - pAngle1);
            if ((pAngle1 * pAngle2) < 0)
            {
                pAngle = Math.Abs(pAngle1) + Math.Abs(pAngle2);
                if (pAngle > Math.PI)
                    pAngle = 2 * Math.PI - pAngle;
            }
            return pAngle;
        }
        private double AngleFromPoint(IGeometry pGeometry)
        {
            ISegmentCollection pSC = pGeometry as ISegmentCollection;//可能用到的东西：FromPoint、ToPoint、EnumCurve、EnumSegments---IEnumSegment
            IEnumSegment pEnumSegment = pSC.EnumSegments;
            pEnumSegment.Reset();
            ISegment pSegment;
            int partIndex = 0;
            int segmentIndex = 0;
            pEnumSegment.Next(out pSegment, ref partIndex, ref segmentIndex);
            ILine pLine = pSegment as ILine;
            double pAngle = pLine.Angle;
            return pAngle;

        }
        private double AngleToPoint(IGeometry pGeometry)
        {
            double pAngle=0;
            ISegmentCollection pSC = pGeometry as ISegmentCollection;//可能用到的东西：FromPoint、ToPoint、EnumCurve、EnumSegments---IEnumSegment
            IEnumSegment pEnumSegment = pSC.EnumSegments;
            pEnumSegment.Reset();
            ISegment pSegment;
            int partIndex = 0;
            int segmentIndex = 0;
            pEnumSegment.Next(out pSegment, ref partIndex, ref segmentIndex);
            int i = 0;
            while (!pEnumSegment.IsLastInPart())
            {
                pEnumSegment.SetAt(0, i);
                pEnumSegment.Next(out pSegment, ref partIndex, ref segmentIndex);
                i++;
            }
            pEnumSegment.SetAt(0, i - 1);
            pEnumSegment.Next(out pSegment, ref partIndex, ref segmentIndex);
            ILine pLine = pSegment as ILine;
            double a = pLine.Angle;
            if (a > 0 && a <= Math.PI)
            { pAngle = a - Math.PI; }
            if (a > -Math.PI && a <= 0)
            { pAngle = a + Math.PI; }
            return pAngle;
        }
        private void strokeIt(List<IGeometry> pGeometryList) {
            IGeometry r = pGeometryList[0];
            IGeometry s = pGeometryList[0];
            double angle = 2*Math.PI/3;
            int m = -1;
            for (int i = 0; i < pGeometryList.Count; i++) {
                for (int j = 0; j <= pGeometryList.Count; j++) {
                    if (j == i) {
                        j++;
                    }
                    if (j == pGeometryList.Count) {
                        if(i == m){
                            break;
                        }
                        IWT.Add(pGeometryList[i]);
                        pGeometryList.RemoveAt(i);
                        i--;
                        break;
                    }
                    if (CheckCrosses(pGeometryList[i], pGeometryList[j]))
                    {
                        double a = GetAngle(pGeometryList[i], pGeometryList[j]);
                        if (a > 2 * Math.PI / 3) {
                            m = i;
                        }
                        if (a > angle)
                        {
                            angle = a;
                            r = pGeometryList[i];
                            s = pGeometryList[j];
                        }
                    }                  
                }
            }
            if (pGeometryList.Count == 0)
            {
                return;
            }
            ITopologicalOperator wtf = r as ITopologicalOperator;
            IGeometry road = wtf.Union(s);
            pGeometryList.RemoveAt(pGeometryList.IndexOf(r));
            pGeometryList.RemoveAt(pGeometryList.IndexOf(s));
            pGeometryList.Add(road);            
            strokeIt(pGeometryList);
        }
    }
}
        #region GetAngle 方法一
        /*
        private double GetAngle(IGeometry pGeometry1, IGeometry pGeometry2)
        {
            //ILine pLine1 = pGeometry1 as ILine;//此处对象可以变为IPolyline
            // IPolyline pPolyline = pGeometry1 as IPolyline;//可能用到的东西：FromPoint、ToPoint
            double pAngle = 0;
            double pAngle1 = 0;
            double pAngle2 = 0;
            ISegmentCollection pSC = pGeometry1 as ISegmentCollection;//可能用到的东西：FromPoint、ToPoint、EnumCurve、EnumSegments---IEnumSegment
            IEnumSegment pEnumSegment = pSC.EnumSegments;
            pEnumSegment.Reset();
            ISegment pSegment;
            int partIndex = 0;
            int segmentIndex = 0;
            pEnumSegment.Next(out pSegment, ref partIndex, ref segmentIndex);
            /*
            object o = Type.Missing;
            ISegmentCollection pPath = new PathClass();
            pPath.AddSegment(pSegment, ref o, ref o);
            IGeometryCollection pPolyline = new PolylineClass();
            pPolyline.AddGeometry(pPath as IGeometry, ref o, ref o);
            *//*
            IGeometry pGS = pSegment as IGeometry;
            IRelationalOperator pRO = pGS as IRelationalOperator;
            if (pRO.Contains(pGPoint))
            {
                ILine pLine1 = pSegment as ILine;
                pAngle1 = pLine1.Angle;
            }
            else
            {
                int i = 0;
                while (!pEnumSegment.IsLastInPart())
                {
                    pEnumSegment.SetAt(0, i);
                    pEnumSegment.Next(out pSegment, ref partIndex, ref segmentIndex);
                    i++;
                }
                pEnumSegment.SetAt(0, i - 1);
                pEnumSegment.Next(out pSegment, ref partIndex, ref segmentIndex);
                ILine pLine1 = pSegment as ILine;
                double a = pLine1.Angle;
                if (a > 0 && a <= Math.PI)
                { pAngle1 = a - Math.PI; }
                if (a > -Math.PI && a <= 0)
                { pAngle1 = a + Math.PI; }
            }

            pSC = pGeometry2 as ISegmentCollection;
            pEnumSegment = pSC.EnumSegments;
            pEnumSegment.Reset();
            pEnumSegment.Next(out pSegment, ref partIndex, ref segmentIndex);
            pGS = pSegment as IGeometry;
            pRO = pGS as IRelationalOperator;
            if (pRO.Contains(pGPoint))
            {
                ILine pLine2 = pSegment as ILine;
                pAngle2 = pLine2.Angle;
            }
            else
            {
                int i = 0;
                while (!pEnumSegment.IsLastInPart())
                {
                    pEnumSegment.SetAt(0, i);
                    pEnumSegment.Next(out pSegment, ref partIndex, ref segmentIndex);
                    i++;
                }
                pEnumSegment.SetAt(0, i - 1);
                pEnumSegment.Next(out pSegment, ref partIndex, ref segmentIndex);
                ILine pLine2 = pSegment as ILine;
                double a = pLine2.Angle;
                if (a > 0 && a <= Math.PI)
                { pAngle2 = a - Math.PI; }
                if (a > -Math.PI && a <= 0)
                { pAngle2 = a + Math.PI; }
            }

            if ((pAngle1 * pAngle2) >= 0)
                pAngle = Math.Abs(pAngle2 - pAngle1);
            if ((pAngle1 * pAngle2) < 0)
            {
                pAngle = Math.Abs(pAngle1) + Math.Abs(pAngle2);
                if (pAngle > Math.PI)
                    pAngle = 2 * Math.PI - pAngle;
            }
           // return pAngle;
            /*将一个要素可视化，需要建立一个内存工作空间
            IFeatureClass pFeatureClass=new FeatureClass() as IFeatureClass ;
            IFeature pFeature = pFeatureClass.CreateFeature();
            pFeature = pSegment as IFeature;
            IFeatureLayer pFeatureLayer = new FeatureLayerClass();
            pFeatureLayer.FeatureClass = pFeatureClass;
            axMapControl1.AddLayer(pFeatureLayer);
            axMapControl1.Refresh();
            */
        //}
        #endregion
   
