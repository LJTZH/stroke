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
using System.IO;

namespace stroke
{
    public partial class Form1 : Form
    {
        string roadDataFullName = string.Empty;//"F:\\stroke\\New_Shapefile.shp"; //
        string shapeFileFullName = string.Empty;
        string SaveFullName = string.Empty;
        //IGeometry pGPoint;
        List<IGeometry> pGeometryList = new List<IGeometry>();
        List<IGeometry> pGeoSave = new List<IGeometry>();
        public Form1()
        {
            InitializeComponent();
        }

        private void btnOpen_Click(object sender, EventArgs e)//获取文件名
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
            

            /*for (int i = 0; i < pGeometryList.Count; i++)
            {
                for (int j = i + 1; j < pGeometryList.Count; j++)
                {
                    IGeometry pGeo1 = pGeometryList[i];
                    IGeometry pGeo2 = pGeometryList[j];
                    bool pCross = this.CheckCrosses(pGeo1, pGeo2);
                    if (pCross)
                    {

                    }
                }
            }*/
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Shape文件（*.shp)|*.shp";
            DialogResult dialogResult = saveFileDialog.ShowDialog();
            if (dialogResult == DialogResult.OK)
            {
                SaveFullName = saveFileDialog.FileName;
            }
            else
            {
                SaveFullName = null;
                return;
            }
            this.textBox2.Text = SaveFullName;
            this.Save(SaveFullName);
        }

        private void ChooseRoads(List<IGeometry> pGeo)
        {
            IGeometry r = new PolylineClass();
            IGeometry s = new PolylineClass();
            double angle = 2 * Math.PI / 3;
            int m = -1;
            for (int i = 0; i < pGeometryList.Count; i++)
            {
                for (int j = 0; j <= pGeometryList.Count; j++)
                {
                    if (j == i)
                    {
                        j++;
                    }
                    if (j == pGeometryList.Count)
                    {
                        if (i == m)
                        {
                            break;
                        }
                        pGeoSave.Add(pGeometryList[i]);
                        pGeometryList.RemoveAt(i);
                        i--;
                        break;
                    }
                    if (CheckCrosses(pGeometryList[i], pGeometryList[j]))
                    {
                        double a = GetAngle(pGeometryList[i], pGeometryList[j]);
                        if (a > 2 * Math.PI / 3)
                        {
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
            pGeometryList.Remove(r);
            pGeometryList.Remove(s);
            pGeometryList.Add(road);
            ChooseRoads(pGeometryList);
        }

        private List<IGeometry> strokeIt(List<IGeometry> pGeo)
        {
            List<List<double>> indexes = new List<List<double>>();
            List<List<int>> mummy = new List<List<int>>();
            List<IGeometry> pGeoSave = new List<IGeometry>();
            List<IGeometry> GeoList = new List<IGeometry>();
            double angle = 2 * Math.PI / 3;
            for (int i = 0; i < pGeo.Count; i++)
            {
                mummy.Add(new List<int>() { i });
                pGeoSave.Add(pGeo[i]);
                GeoList.Add(pGeo[i]);
                for (int j = i + 1; j < pGeo.Count; j++)
                {
                    if (CheckCrosses(pGeo[i], pGeo[j]))
                    {
                        double a = GetAngle(pGeo[i], pGeo[j]);
                        if (a > angle)
                        {
                            List<double> index = new List<double>();
                            index.Add(i * 1.0);
                            index.Add(j * 1.0);
                            index.Add(a);
                            indexes.Add(index);
                        }
                    }
                }
            }
            indexes.Sort((x, y) => x[2].CompareTo(y[2]));

            for (int k = 0; k < indexes.Count; k++)
            {
                List<double> index = indexes[k];
                int i = Convert.ToInt32(index[0]);
                int j = Convert.ToInt32(index[1]);
                List<int> I = mummy.Find(x => x[0] == i);
                List<int> J = mummy.Find(x => x[0] == j);
                IGeometry r = GeoList[I[I.Count - 1]];
                IGeometry s = GeoList[J[J.Count - 1]];
                if (CheckCrosses(r, s) && GetAngle(r, s) > angle) {
                    ITopologicalOperator wtf = r as ITopologicalOperator;
                    IGeometry road = wtf.Union(s);
                    pGeoSave.Remove(r);
                    pGeoSave.Remove(s);
                    pGeoSave.Add(road);
                    GeoList.Add(road);
                    int a = mummy.IndexOf(I);
                    int b = mummy.IndexOf(J);
                    I.Add(GeoList.Count - 1);
                    J.Add(GeoList.Count - 1);
                    mummy[a] = I;
                    mummy[b] = J;
                }                
            }

            return pGeoSave;
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
        }//得到shp文件里的每一个Geometry

        private bool CheckCrosses(IGeometry pGeometry1, IGeometry pGeometry2)
        {
            IRelationalOperator pRO = pGeometry1 as IRelationalOperator;
            return pRO.Touches(pGeometry2);
        }//检查是否相交

        private double GetAngle(IGeometry pGeometry1, IGeometry pGeometry2)
        {
            double pAngle1;
            double pAngle2;
            double pAngle = 0;

            IGeometry pGPoint = new PointClass();

            IRelationalOperator pRO = pGeometry1 as IRelationalOperator;
            ITopologicalOperator pTO = pGeometry1 as ITopologicalOperator;

            if (pRO.Touches(pGeometry2)) {
                pGPoint = pTO.Intersect(pGeometry2, esriGeometryDimension.esriGeometry0Dimension);
            }

            IPolyline pPolyline1 = pGeometry1 as IPolyline;
            IPoint pPF1 = pPolyline1.FromPoint;
            IGeometry pGeoPoint1 = pPF1 as IGeometry;
            IRelationalOperator pRO1 = pGeoPoint1 as IRelationalOperator;
            if (pRO1.Contains(pGPoint))
            {
                pAngle1 = this.AngleFromPoint(pGeometry1);
            }
            else
            {
                pAngle1 = this.AngleToPoint(pGeometry1);
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
        }//获取相交道路的夹角

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

        }//获取夹角的辅助

        private double AngleToPoint(IGeometry pGeometry)
        {
            double pAngle = 0;
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
        }//获取夹角的辅助

        private void Save(string outfileNamePath)
        {
            //打开工作空间
            int index = outfileNamePath.LastIndexOf('\\');
            string folder = outfileNamePath.Substring(0, index);
            SaveFullName= outfileNamePath.Substring(index + 1);
            IWorkspaceFactory pWSF = new ShapefileWorkspaceFactoryClass();
            IFeatureWorkspace pFWS = (IFeatureWorkspace)pWSF.OpenFromFile(folder, 0);


            if (File.Exists(outfileNamePath))
            {
                IFeatureClass featureClass = pFWS.OpenFeatureClass(SaveFullName);
                IDataset pDataset = (IDataset)featureClass;
                pDataset.Delete();
            }


            IFields pFields = new FieldsClass();
            IFieldsEdit pFieldsEdit;
            pFieldsEdit = (IFieldsEdit)pFields;
            IField pField = new FieldClass();
            IFieldEdit pFieldEdit = (IFieldEdit)pField;
            pFieldEdit.Name_2 = "Shape";
            pFieldEdit.Type_2 = esriFieldType.esriFieldTypeGeometry;
            //空间参考
            IGeometryDef pGeometryDef = new GeometryDefClass();
            IGeometryDefEdit pGDefEdit = (IGeometryDefEdit)pGeometryDef;
            pGDefEdit.GeometryType_2 = esriGeometryType.esriGeometryPolyline;
            pFieldEdit.GeometryDef_2 = pGeometryDef;
            pFieldsEdit.AddField(pField);

            IFeatureClass pFeatureClass;
            pFeatureClass = pFWS.CreateFeatureClass(SaveFullName, pFields, null, null, esriFeatureType.esriFTSimple, "Shape", "");
            pGeometryList = this.GetGeometry(roadDataFullName);
            pGeoSave = strokeIt(pGeometryList);
            for (int i = 0; i < pGeoSave.Count; i++)
            {
                IFeature pFeature = pFeatureClass.CreateFeature();
                pFeature.Shape = pGeoSave[i];
                pFeature.Store();
            }
        }
    }
}

