﻿using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using System.Xml;
using System.Drawing;
using gHowl.Properties;
using Rhino.DocObjects;

namespace gHowl.KML
{
    public class KMLexportComponent_OBSOLETE : GH_Component
    {
        public KMLexportComponent_OBSOLETE() : base("KML Exporter [Obsolete]", "KML Out", "Export from Rhino model to KML format", "gHowl", "KML") { }
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            //pManager.Register_StringParam("File Path", "F", "File to write to *.KML", GH_ParamAccess.item);
            pManager.AddTextParameter("File Path", "F", "File to write to *.KML", GH_ParamAccess.item);
            //pManager.Register_GeometryParam("Geometry", "G", "Geometry to export", GH_ParamAccess.tree);
            pManager.AddGeometryParameter("Geometry", "G", "Geometry to export", GH_ParamAccess.tree);

            KMLStyleParameter_OBSOLETE param = new KMLStyleParameter_OBSOLETE();
            //param.AddPersistentData(new KMLStyleType());
            param.SetPersistentData(new KMLStyleType_OBSOLETE());
            //pManager.RegisterParam(param,"Style","S","KML Object Style",GH_ParamAccess.item);
            pManager.AddParameter(param, "Style", "S", "KML Object Style", GH_ParamAccess.item);
            //pManager.Register_GenericParam("Style", "S", "KML Object Attributes", GH_ParamAccess.item);

            //pManager.Register_PointParam("EAP_XYZ", "XYZ", "Anchor Point Model Coordinates", GH_ParamAccess.item);
            pManager.AddPointParameter("EAP_XYZ", "XYZ", "Anchor Point Model Coordinates", GH_ParamAccess.item);
            //pManager.Register_PointParam("EAP_GPS", "GEO", "Anchor Point Coordinates in D.D. Longitude, Latitude, Altitude", GH_ParamAccess.item);
            pManager.AddPointParameter("EAP_GPS", "GEO", "Anchor Point Coordinates in D.D. Longitude, Latitude, Altitude", GH_ParamAccess.item);
            //pManager.Register_StringParam("Altitude Mode", "A", "Altitude mode for KML File: absolute, clampToGround, or relativeToGround", "absolute", GH_ParamAccess.item);
            pManager.AddTextParameter("Altitude Mode", "A", "Altitude mode for KML File: absolute, clampToGround, or relativeToGround", GH_ParamAccess.item, "absolute");
            
            pManager[0].Optional = false;
            pManager[1].Optional = false;
            pManager[2].Optional = true;
            pManager[3].Optional = false;
            pManager[4].Optional = false;
            pManager[5].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
           // pManager.Register_StringParam("Message", "M", "Service Message");
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("{7fde1e88-728f-43b2-a217-e312462dfa10}"); }
        }

        protected override Bitmap Icon
        {
            get
            {
                return Resources.geIcon;
                //return null;
            }
        }


        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string file = "";
            GH_Structure<IGH_GeometricGoo> geoGoo, geo = new GH_Structure<IGH_GeometricGoo>();
            Point3d EAP_GPS = new Point3d();
            Point3d EAP_XYZ = new Point3d();
            KMLStyleType_OBSOLETE style = new KMLStyleType_OBSOLETE();
            string altMode = "";
            
            if ((!DA.GetData(0, ref file)) || file == null ||
                (!DA.GetDataTree(1, out geoGoo)) || geoGoo == null || geoGoo.IsEmpty ||
                (!DA.GetData(3, ref EAP_XYZ)) || EAP_XYZ == null ||
                (!DA.GetData(4, ref EAP_GPS)) || EAP_GPS == null ||
                (!DA.GetData(5, ref altMode))
                 )
            {

                return;
            }

            if(!DA.GetData(2,ref style))
            {
                style = new KMLStyleType_OBSOLETE();
            }

            //Set up EAP 
           
            EarthAnchorPoint eap = new EarthAnchorPoint();
            eap.EarthBasepointLongitude = EAP_GPS.X;
            eap.EarthBasepointLatitude = EAP_GPS.Y;
            eap.EarthBasepointElevation = EAP_GPS.Z;
            eap.ModelBasePoint = EAP_XYZ;
 
            Rhino.UnitSystem us = new Rhino.UnitSystem();
           
            Transform xf = eap.GetModelToEarthTransform(us);
          
            geo = geoGoo.Duplicate();

            XmlTextWriter writer = new XmlTextWriter(file, System.Text.Encoding.UTF8);
            writer.Formatting = Formatting.Indented;
            writer.WriteStartDocument();
            writer.WriteStartElement("kml", "http://earth.google.com/kml/2.0");
            writer.WriteStartElement("Document");

            writer.WriteElementString("name", "gHowl kml exporter");

            //write style
            writer.WriteStartElement("Style");
            writer.WriteAttributeString("id", "gh_default");
            writer.WriteStartElement("LineStyle");
            writer.WriteElementString("color", style.lineColor);
            writer.WriteElementString("width", style.lineWidth.ToString());
            writer.WriteEndElement();
            writer.WriteStartElement("PolyStyle");
            writer.WriteElementString("color", style.fillColor);
            writer.WriteEndElement();
            writer.WriteEndElement();

            for (int i = 0; i < geo.PathCount; i++)
            {

                for (int j = 0; j < geo.get_Branch(i).Count; j++)
                {
                   
                    IGH_GeometricGoo geoGooObj = geo.get_DataItem(geo.Paths[i], j);
                    GeometryBase geoObj = GH_Convert.ToGeometryBase(geoGooObj);

                    switch (geoObj.ObjectType)
                    {
                        case Rhino.DocObjects.ObjectType.Point:
                            Rhino.Geometry.Point pt = (Rhino.Geometry.Point)geoObj;
                            Point3d pt3d = new Point3d(pt.Location.X, pt.Location.Y, pt.Location.Z);
                            GH_Point ptOut = new GH_Point(processPt(pt3d, xf));
                            
                            //DA.SetData(0, "<Point> " + ptOut.Value.X.ToString() + "," + ptOut.Value.Y.ToString() + "," + ptOut.Value.Z.ToString());
                            writer.WriteStartElement("Placemark");
                            writer.WriteElementString("name", "PT_" + j.ToString());
                            writer.WriteElementString("Snippet", "Point");
                            writer.WriteElementString("description", "point location: " + ptOut.Value.X + "," + ptOut.Value.Y + "," + ptOut.Value.Z);
                            writer.WriteStartElement("Point");
                            writer.WriteElementString("coordinates", ptOut.Value.X + "," + ptOut.Value.Y + "," + ptOut.Value.Z);
                            writer.WriteEndElement();
                            writer.WriteEndElement();
                            break;
                        case Rhino.DocObjects.ObjectType.Curve:
                            Rhino.Geometry.Curve crv = (Rhino.Geometry.Curve)geoObj;
                            if (crv.IsClosed)
                            {
                                //Print("<linearRing>");
                                if (crv.IsPolyline())
                                {
                                    Rhino.Geometry.Polyline pLine;
                                    crv.TryGetPolyline(out pLine);
                                    Point3d[] ptArr = pLine.ToArray();
                                    //pLine.ToArray;
                                    writer.WriteStartElement("Placemark");
                                    writer.WriteElementString("name", "linearRing " + j);
                                    
                                    writer.WriteElementString("Snippet", "linearRing");
                                    writer.WriteElementString("styleUrl", "gh_default");
                                    writer.WriteStartElement("LinearRing");
                                    writer.WriteElementString("altitudeMode", altMode);
                                    string strCoo = "";
                                    for (int p = 0; p < ptArr.Length; p++)
                                    {
                                        Point3d ptOut2 = processPt(ptArr[p], xf);
                                        strCoo += ptOut2.X + "," + ptOut2.Y + "," + ptOut2.Z + " ";
                                        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, ptOut2.ToString());
                                    }
                                    writer.WriteElementString("coordinates", strCoo);
                                    writer.WriteEndElement(); // end ring
                                    writer.WriteEndElement(); // end placemark

                                }
                                else
                                {
                                    Point3d[] ptArr;
                                    crv.DivideByCount(25, true, out ptArr);
                                    writer.WriteStartElement("Placemark");
                                    writer.WriteElementString("name", "linearRing " + j);
                                    
                                    writer.WriteElementString("Snippet", "linearRing");
                                    writer.WriteElementString("styleUrl", "gh_default");
                                    writer.WriteStartElement("LinearRing");
                                    writer.WriteElementString("altitudeMode", altMode);
                                    string strCoo = "";
                                    for (int p = 0; p < ptArr.Length; p++)
                                    {
                                        Point3d ptOut2 = processPt(ptArr[p], xf);
                                        strCoo += ptOut2.X + "," + ptOut2.Y + "," + ptOut2.Z + " ";
                                    }
                                    writer.WriteElementString("coordinates", strCoo);
                                    writer.WriteEndElement(); // end ring
                                    writer.WriteEndElement(); // end placemark

                                }
                            }
                            else
                            {
                                //Print("<lineString>");
                                if (crv.IsPolyline())
                                {
                                    Rhino.Geometry.Polyline pLine;
                                    crv.TryGetPolyline(out pLine);
                                    Point3d[] ptArr = pLine.ToArray();
                                    //pLine.ToArray;
                                    writer.WriteStartElement("Placemark");
                                    writer.WriteElementString("name", "linearRing " + j);
                                    
                                    writer.WriteElementString("Snippet", "linearRing");
                                    writer.WriteElementString("styleUrl", "gh_default");
                                    writer.WriteStartElement("LinearRing");
                                    writer.WriteElementString("altitudeMode", altMode);
                                    string strCoo = "";
                                    for (int p = 0; p < ptArr.Length; p++)
                                    {
                                        Rhino.Geometry.Point3d ptOut2 = processPt(ptArr[p], xf);
                                        strCoo += ptOut2.X + "," + ptOut2.Y + "," + ptOut2.Z + " ";
                                    }
                                    writer.WriteElementString("coordinates", strCoo);
                                    writer.WriteEndElement(); // end ring
                                    writer.WriteEndElement(); // end placemark


                                }
                                else
                                {

                                    Point3d[] ptArr;
                                    crv.DivideByCount(25, true, out ptArr);
                                    writer.WriteStartElement("Placemark");
                                    writer.WriteElementString("name", "lineString " + j);
                                    
                                    writer.WriteElementString("Snippet", "lineString");
                                    writer.WriteElementString("styleUrl", "gh_default");
                                    writer.WriteStartElement("LineString");
                                    writer.WriteElementString("altitudeMode", altMode);
                                    string strCoo = "";
                                    for (int p = 0; p < ptArr.Length; p++)
                                    {
                                        Rhino.Geometry.Point3d ptOut2 = processPt(ptArr[p], xf);
                                        strCoo += ptOut2.X + "," + ptOut2.Y + "," + ptOut2.Z + " ";
                                    }
                                    writer.WriteElementString("coordinates", strCoo);
                                    writer.WriteEndElement(); // end ring
                                    writer.WriteEndElement(); // end placemark

                                }

                            }
                            break;
                        case Rhino.DocObjects.ObjectType.Surface:
                           // DA.SetData(0, "Surface Not Supported");
                            break;
                        case Rhino.DocObjects.ObjectType.Brep:

                            Rhino.Geometry.Brep brp = (Rhino.Geometry.Brep)geoObj;
                            Mesh[] m = Mesh.CreateFromBrep(brp, MeshingParameters.Default);
                            writer.WriteStartElement("Placemark");
                            writer.WriteElementString("name", "MultiGeometry " + j);
                            
                            writer.WriteElementString("Snippet", "MultiGeometry");
                            writer.WriteElementString("styleUrl", "gh_default");
                            writer.WriteStartElement("MultiGeometry");
                            for (int p = 0; p < m.Length; p++)
                            {
                                Rhino.Geometry.Collections.MeshFaceList mfl = m[p].Faces;
                                Rhino.Geometry.Collections.MeshVertexList mvl = m[p].Vertices;
                                for (int q = 0; q < mfl.Count; q++)
                                {
                                    Rhino.Geometry.MeshFace mf = mfl.GetFace(q);
                                    writer.WriteStartElement("Polygon");
                                    writer.WriteElementString("altitudeMode", altMode);
                                    writer.WriteStartElement("outerBoundaryIs");
                                    writer.WriteStartElement("LinearRing");
                                    //process pts of mesh face as a polygon
                                    string strCoo = "";
                                    if (mf.IsTriangle)
                                    {
                                        Point3d ptA = processPt(mvl[mf.A], xf);
                                        Point3d ptB = processPt(mvl[mf.B], xf);
                                        Point3d ptC = processPt(mvl[mf.C], xf);
                                        strCoo += ptA.X + "," + ptA.Y + "," + ptA.Z + " ";
                                        strCoo += ptB.X + "," + ptB.Y + "," + ptB.Z + " ";
                                        strCoo += ptC.X + "," + ptC.Y + "," + ptC.Z + " ";
                                        strCoo += ptA.X + "," + ptA.Y + "," + ptA.Z + " ";

                                    }
                                    else
                                    {
                                        Point3d ptA = processPt(mvl[mf.A], xf);
                                        Point3d ptB = processPt(mvl[mf.B], xf);
                                        Point3d ptC = processPt(mvl[mf.C], xf);
                                        Point3d ptD = processPt(mvl[mf.D], xf);
                                        strCoo += ptA.X + "," + ptA.Y + "," + ptA.Z + " ";
                                        strCoo += ptB.X + "," + ptB.Y + "," + ptB.Z + " ";
                                        strCoo += ptC.X + "," + ptC.Y + "," + ptC.Z + " ";
                                        strCoo += ptD.X + "," + ptD.Y + "," + ptD.Z + " ";
                                        strCoo += ptA.X + "," + ptA.Y + "," + ptA.Z + " ";

                                    }
                                    writer.WriteElementString("coordinates", strCoo);
                                    writer.WriteEndElement(); // end linestring
                                    writer.WriteEndElement(); // end outer
                                    writer.WriteEndElement(); // end polygon
                                }

                            }
                            writer.WriteEndElement();
                            writer.WriteEndElement();
                            /* Rhino.Geometry.Collections.BrepEdgeList bel = brp.Edges;
                             Rhino.Geometry.Collections.BrepFaceList bfl = brp.Faces;
                             List<Curve> crvList = new List<Curve>();
                             if (bfl.Count > 1) 
                             {

                                 //brep has more than one face, go through the faces

                             }
                             else //brep has only one face
                             {
                                 if (bfl[0].IsPlanar())
                                 {
                                     //get edge curves, test if they are linear
                                     int d = 0;
                                     for (int p = 0; p < bel.Count; p++)
                                     {
                                         crvList.Add(bel[p].DuplicateCurve());
                                         if (crvList[p].IsLinear())
                                         {
                                             d = d + 1;
                                         }   
                                     }
                                     if (d == bel.Count)
                                     {
                                         //All Edges are linear
                                         //TODO: Check for holes
                                         Point3d[] ptArr = brp.DuplicateVertices();
                                         writer.WriteStartElement("Placemark");
                                         writer.WriteElementString("name", "polygon" + j.ToString());
                                         writer.WriteStartElement("Polygon");
                                         writer.WriteElementString("altitudeMode", altMode);
                                         writer.WriteStartElement("outerBoundaryIs");
                                         writer.WriteStartElement("LinearRing");
                                         string strCoo = "";
                                         for (int q = 0; q < ptArr.Length; q++)
                                         {
                                             Rhino.Geometry.Point3d ptOut2 = processPt(ptArr[q], xf);
                                             strCoo += ptOut2.X + "," + ptOut2.Y + "," + ptOut2.Z + " ";
                                         }         
                                         writer.WriteElementString("coordinates", strCoo);
                                         writer.WriteEndElement(); // end linestring
                                         writer.WriteEndElement(); // end outer
                                         writer.WriteEndElement(); // end polygon
                                         writer.WriteEndElement(); // end placemark
                                     }
                                     else
                                     {
                                        // Print(d + "," + bel.Count.ToString());
                                         //not all edges are linear
                                     }

                                     //test if they are linear
                                     //if they are, get pts
                                     //if they are not, divide curve
                                     //processPts

                                 }
                                 else //brep is not planar
                                 { 

                                 }


                             }*/
                            break;
                        case Rhino.DocObjects.ObjectType.Mesh:
                            Rhino.Geometry.Mesh msh = (Rhino.Geometry.Mesh)geoObj;
                            writer.WriteStartElement("Placemark");
                            writer.WriteElementString("name", "MultiGeometry " + j);
                           
                            writer.WriteElementString("Snippet", "MultiGeometry");
                            writer.WriteElementString("styleUrl", "gh_default");
                            writer.WriteStartElement("MultiGeometry");
                            Rhino.Geometry.Collections.MeshFaceList mflm = msh.Faces;
                            Rhino.Geometry.Collections.MeshVertexList mvlm = msh.Vertices;
                            for (int q = 0; q < mflm.Count; q++)
                            {
                                Rhino.Geometry.MeshFace mf = mflm.GetFace(q);
                                writer.WriteStartElement("Polygon");
                                writer.WriteElementString("altitudeMode", altMode);
                                writer.WriteStartElement("outerBoundaryIs");
                                writer.WriteStartElement("LinearRing");
                                //process pts of mesh face as a polygon
                                string strCoo = "";
                                if (mf.IsTriangle)
                                {
                                    Point3d ptA = processPt(mvlm[mf.A], xf);
                                    Point3d ptB = processPt(mvlm[mf.B], xf);
                                    Point3d ptC = processPt(mvlm[mf.C], xf);
                                    strCoo += ptA.X + "," + ptA.Y + "," + ptA.Z + " ";
                                    strCoo += ptB.X + "," + ptB.Y + "," + ptB.Z + " ";
                                    strCoo += ptC.X + "," + ptC.Y + "," + ptC.Z + " ";
                                    strCoo += ptA.X + "," + ptA.Y + "," + ptA.Z + " ";

                                }
                                else
                                {
                                    Point3d ptA = processPt(mvlm[mf.A], xf);
                                    Point3d ptB = processPt(mvlm[mf.B], xf);
                                    Point3d ptC = processPt(mvlm[mf.C], xf);
                                    Point3d ptD = processPt(mvlm[mf.D], xf);
                                    strCoo += ptA.X + "," + ptA.Y + "," + ptA.Z + " ";
                                    strCoo += ptB.X + "," + ptB.Y + "," + ptB.Z + " ";
                                    strCoo += ptC.X + "," + ptC.Y + "," + ptC.Z + " ";
                                    strCoo += ptD.X + "," + ptD.Y + "," + ptD.Z + " ";
                                    strCoo += ptA.X + "," + ptA.Y + "," + ptA.Z + " ";

                                }

                                writer.WriteElementString("coordinates", strCoo);
                                writer.WriteEndElement(); // end linestring
                                writer.WriteEndElement(); // end outer
                                writer.WriteEndElement(); // end polygon

                            }


                            writer.WriteEndElement();
                            writer.WriteEndElement();
                            break;
                        default:
                            //DA.SetData(0, "Object Not Recognized");
                            break;

                    }

                }
            }

            

            writer.WriteEndElement(); //End Document
            writer.WriteEndElement(); //end KML
            writer.Flush();
            writer.Close();


        }

        public Point3d processPt(Point3d pt, Transform xf)
        {
            Point3d ptON = new Point3d(pt.X, pt.Y, pt.Z);
            ptON = xf * ptON; //where the magic happens
            Point3d ptOut = new Point3d(ptON.X, ptON.Y, ptON.Z);
            return ptOut;
        }

        public override GH_Exposure Exposure
        {
            get
            {
                return GH_Exposure.hidden;
            }
        }

        



    }
}
