/*
 * Created by SharpDevelop.
 * User: Christian
 * Date: 29.04.2006
 * Time: 10:06
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using DelftTools.Utils.IO;
using GeoAPI.Extensions.Feature;
using GeoAPI.Geometries;
using log4net;
using OSGeo.OGR;
using SharpMap.Converters.Geometries;
using SharpMap.Converters.WellKnownBinary;
using SharpMap.Data;
using SharpMap.Data.Providers;
using System.IO;

namespace SharpMap.Extensions.Data.Providers
{
    /// <summary>
    /// OgrFeatureProvider provider for SharpMap
    /// using the C# SWIG wrapper of GDAL/OGR
    /// <code>
    /// SharpMap.Layers.VectorLayer vLayerOgr = new SharpMap.Layers.VectorLayer("MapInfoLayer");
    /// vLayerOgr.DataSource = new SharpMap.Data.Providers.OgrFeatureProvider(@"D:\GeoData\myWorld.tab");
    /// </code>
    /// </summary>
    public class OgrFeatureProvider : IFileBasedFeatureProvider
    {
        private static readonly ILog log = LogManager.GetLogger(typeof (OgrFeatureProvider));
        private bool _IsOpen;
        private int _SRID = -1;

        private IEnvelope envelope;
        private long id;

        private DataSource ogrDataSource;
        private Layer ogrLayer;

        #region constructor

        public OgrFeatureProvider()
        {
            Ogr.RegisterAll();
        }


        /// <summary>
        /// Loads a OgrFeatureProvider datasource with the specified layer
        /// </summary>
        /// <param name="path">datasource</param>
        /// <param name="layerName">name of layer</param>
        public OgrFeatureProvider(string path, string layerName) : this()
        {
            this.path = path;
            this.layerName = layerName;
                ogrDataSource = Ogr.Open(path, 1);
                ogrLayer = ogrDataSource.GetLayerByName(layerName);
            Name = layerName;
            _IsOpen = true;
        }

        /// <summary>
        /// Loads a OgrFeatureProvider datasource with the specified layer
        /// </summary>
        /// <param name="path">datasource</param>
        /// <param name="layerNumber">number of layer</param>
        public OgrFeatureProvider(string path, int layerNumber) : this()
        {
            this.path = path;
            ogrDataSource = Ogr.Open(path, 0);
            ogrLayer = ogrDataSource.GetLayerByIndex(layerNumber);
            this.layerName = ogrLayer.GetName();
            Name = LayerName;
            _IsOpen = true;
            
        }

        /// <summary>
        /// Loads a OgrFeatureProvider datasource with the specified layer
        /// </summary>
        /// <param name="path">datasource</param>
        /// <param name="layerNumber">number of layer</param>
        /// <param name="name">Returns the name of the loaded layer</param>
        public OgrFeatureProvider(string path, int layerNumber, out string name) : this(path, layerNumber)
        {
            name = ogrLayer.GetName();
            layerName = name;
        }

        /// <summary>
        /// Loads a OgrFeatureProvider datasource with the first layer
        /// </summary>
        /// <param name="path">datasource</param>
        public OgrFeatureProvider(string path) : this(path, 0)
        {
        }

        /// <summary>
        /// Loads a OgrFeatureProvider datasource with the first layer
        /// </summary>
        /// <param name="path">datasource</param>
        /// <param name="name">Returns the name of the loaded layer</param>
        public OgrFeatureProvider(string path, out string name) : this(path, 0, out name)
        {
        }

       

        #endregion

        public static int GetNumberOfLayers(string path)
        {
            Ogr.RegisterAll();
            using (DataSource dataSource = Ogr.Open(path, 0))
            {
                return dataSource.GetLayerCount();
            }
        }

        /// <summary>
        /// Name of the layer in the OGR data source
        /// </summary>
        public virtual string Name { get; set; }

        #region IFeatureProvider Members

        public virtual long Id
        {
            get { return id; }
            set { id = value; }
        }

        public virtual Type FeatureType
        {
            get { return typeof (FeatureDataRow); }
        }

        public virtual IList Features
        {
            get { return GetFeatures(GetExtents()); }
            set { throw new NotImplementedException(); }
        }

        // TODO: improve performance
        public virtual bool Contains(IFeature feature)
        {
            return IndexOf(feature) != -1;
        }

        // TODO: improve performance
        public virtual int IndexOf(IFeature feature)
        {
            //check wether feature with the same id exists and has the same geometry.
            var fids = GetObjectIDsInView(GetExtents());
            if (!fids.Contains((int) feature.Id))
            {
                return -1;
            }

            using (var ogrFeature = ogrLayer.GetFeature((int) feature.Id)) 
            {
                if (ogrFeature != null)
                {
                    var geometry = GetGeometryByID((int) feature.Id);
                    {
                        if (geometry.Equals(feature.Geometry))
                        {
                            return ((Collection<int>)fids).IndexOf((int) feature.Id);
                        }
                    }
                }
            }
            return -1;
        }

        /// <summary>
        /// Boundingbox of the dataset
        /// </summary>
        /// <returns>boundingbox</returns>
        public virtual IEnvelope GetExtents()
        {
            if (this.envelope == null)
            {
                using (var ogrEnvelope = new Envelope())
                {
                    int i = ogrLayer.GetExtent(ogrEnvelope, 1);

                    envelope = GeometryFactory.CreateEnvelope(ogrEnvelope.MinX,
                                                              ogrEnvelope.MaxX,
                                                              ogrEnvelope.MinY,
                                                              ogrEnvelope.MaxY);
                }
            }


            return envelope;
        }

        /// <summary>
        /// Returns the number of features in the dataset
        /// </summary>
        /// <returns>number of features</returns>
        public virtual int GetFeatureCount()
        {
            return ogrLayer.GetFeatureCount(1);
        }

        /// <summary>
        /// Returns a FeatureDataRow based on a RowID
        /// </summary>
        /// <param name="index"></param>
        /// <returns>FeatureDataRow</returns>
        public virtual IFeature GetFeature(int index)
        {
            var fid = ((Collection<int>)GetObjectIDsInView(GetExtents()))[index];
            FeatureDataTable featureDataTable= CreateFeatureDataTableStructure();
            using (var ogrFeature = ogrLayer.GetFeature(fid))
            {
                FeatureDataRow feature = featureDataTable.NewRow();
                feature.Id = fid;
                SetFeatureValuesInRow(featureDataTable, ogrFeature, feature);
                feature.Geometry = ParseOgrGeometry(ogrFeature.GetGeometryRef());
                return feature;
            }
        }

        /// <summary>
        /// Returns geometry Object IDs whose bounding box intersects 'envelope'
        /// </summary>
        /// <param name="envelope"></param>
        /// <returns></returns>
        public virtual ICollection<int> GetObjectIDsInView(IEnvelope envelope)
        {
            ogrLayer.SetSpatialFilterRect(envelope.MinX, envelope.MinY, envelope.MaxX, envelope.MaxY);
            ogrLayer.ResetReading();

            var objectIDs = new Collection<int>();
            int featureCount = ogrLayer.GetFeatureCount(1);
            for (int i = 0; i < featureCount;i++ )
            {
                using(var ogrFeature= ogrLayer.GetNextFeature())
                {
                    if(ogrFeature==null) break;
                    objectIDs.Add(ogrFeature.GetFID());
                }
            }
            return objectIDs;
        }

        /// <summary>
        /// Returns the geometry corresponding to the Object ID
        /// </summary>
        /// <param name="oid">Object ID</param>
        /// <returns>geometry</returns>
        public virtual IGeometry GetGeometryByID(int oid)
        {
            using (Feature ogrFeature = ogrLayer.GetFeature(oid))
            {
                using (var ogrGeometry = ogrFeature.GetGeometryRef())
                {
                    return ParseOgrGeometry(ogrGeometry);
                }
            }
        }

        public virtual IFeature Add(IGeometry geometry)
        {
            throw new NotImplementedException();
        }

        public virtual Func<IFeatureProvider, IGeometry, IFeature> AddNewFeatureFromGeometryDelegate { get; set; }

        

        public virtual ICollection<IGeometry> GetGeometriesInView(IEnvelope bbox)
        {
            return GetGeometriesInView(bbox, -1);
        }

        /// <summary>
        /// Returns geometries within the specified bounding box
        /// </summary>
        /// <param name="bbox"></param>
        /// <returns></returns>
        public virtual ICollection<IGeometry> GetGeometriesInView(IEnvelope bbox, double minGeometrySize)
        {
            var geoms = new Collection<IGeometry>();

            ogrLayer.SetSpatialFilterRect(bbox.MinX, bbox.MinY, bbox.MaxX, bbox.MaxY);

            ogrLayer.ResetReading();
            int featureCount = ogrLayer.GetFeatureCount(1);

            for (var i=0; i < featureCount; i++)
            {
                using (var ogrFeature = ogrLayer.GetNextFeature())
                {
                    if(ogrFeature==null) break;
                    geoms.Add(ParseOgrGeometry(ogrFeature.GetGeometryRef()));
                }
            }
            return geoms;
        }

        /// <summary>
        /// The spatial reference ID (CRS)
        /// </summary>
        public virtual int SRID
        {
            get { return _SRID; }
            set { _SRID = value; }
        }


        /// <summary>
        /// Returns the data associated with all the geometries that are intersected by 'geom'
        /// </summary>
        /// <param name="bbox">Geometry to intersect with</param>
        public virtual IList GetFeatures(IEnvelope bbox)
        {
            ogrLayer.SetSpatialFilterRect(bbox.MinX, bbox.MinY, bbox.MaxX, bbox.MaxY);
            ogrLayer.ResetReading();
           FeatureDataTable featureDataTable =CreateFeatureDataTableStructure();

            try
            {
                int featureCount = ogrLayer.GetFeatureCount(1);
                for (int i = 0; i < featureCount; i++)
                {
                    using (var ogrFeature = ogrLayer.GetNextFeature())
                    {
                        if (ogrFeature == null) break;
                        FeatureDataRow feature = featureDataTable.NewRow();
                        feature.Id = ogrFeature.GetFID();
                        SetFeatureValuesInRow(featureDataTable, ogrFeature, feature);
                        feature.Geometry = ParseOgrGeometry(ogrFeature.GetGeometryRef());
                        featureDataTable.AddRow(feature);
                    }
                }
            }
            catch (ApplicationException e)
            {
                throw new IOException(string.Format("Unable to read the following file: {0}", path), e);
            }

            return featureDataTable;
        }

        /// <summary>
        /// Returns the data associated with all the geometries that are intersected by 'geom'
        /// </summary>
        /// <param name="geom">Geometry to intersect with</param>
        public virtual IList GetFeatures(IGeometry geom)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Disposers and finalizers

        private bool disposed;
        private string layerName;
        private string path;

        /// <summary>
        /// Disposes the object
        /// </summary>
        public virtual void Dispose()
        {

            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected internal virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing && ogrDataSource != null)
                {
                    Close();
                }
                disposed = true;
            }
        }

        /// <summary>
        /// Finalizer
        /// </summary>
        ~OgrFeatureProvider()
        {
            Close();
            Dispose();
        }

        #endregion

        #region private methods for data conversion sharpmap <--> ogr

        private static IGeometry ParseOgrGeometry(Geometry ogrGeometry)
        {
            var wkbBuffer = new byte[ogrGeometry.WkbSize()];
            int i = ogrGeometry.ExportToWkb(wkbBuffer);
            return GeometryFromWKB.Parse(wkbBuffer);
        }

        #endregion

        #region IFileBased Members
        public virtual void ReConnect()
        {

        }

        /// <summary>
        /// Opens the datasource
        /// </summary>
        public virtual void Open(string path)
        {
            if (_IsOpen &&  !this.path.Equals(path))
            {
                Close();
            }

            if (!_IsOpen)
            {
                this.path = path;
                ogrDataSource = Ogr.Open(path, 1);

                if (LayerName != null)
                {
                    OpenLayer();
                }
            }
            _IsOpen = true;
        }

        public virtual void RelocateTo(string newPath)
        {
            throw new NotImplementedException();
        }

        public virtual void CopyTo(string newPath)
        {
            throw new NotImplementedException();
        }

        private void OpenLayer()
        {
            ogrLayer = ogrDataSource.GetLayerByName(LayerName);
        }

        public virtual void OpenLayerWithSQL(string statement)
        {
            layerName = null;
            try
            {
                ogrLayer = ogrDataSource.ExecuteSQL(statement, null, null);
            }
            catch
            {
            }
        }


        private FeatureDataTable CreateFeatureDataTableStructure()
        {
            var featureDataTable = new FeatureDataTable();

            //reads the column definition of the layer/feature
            using (FeatureDefn ogrFeatureDefn = ogrLayer.GetLayerDefn())
            {
                int iField;

                var ogrTypesAndCSharpTypesMapping = new Dictionary<FieldType, Type>();
                ogrTypesAndCSharpTypesMapping[FieldType.OFTInteger] = typeof (int);
                ogrTypesAndCSharpTypesMapping[FieldType.OFTReal] = typeof (double);
                ogrTypesAndCSharpTypesMapping[FieldType.OFTString] = typeof (string);
                ogrTypesAndCSharpTypesMapping[FieldType.OFTWideString] = typeof (string);
                ogrTypesAndCSharpTypesMapping[FieldType.OFTDateTime] = typeof (DateTime);


                for (iField = 0; iField < ogrFeatureDefn.GetFieldCount(); iField++)
                {
                    using (FieldDefn ogrFldDef = ogrFeatureDefn.GetFieldDefn(iField))
                    {
                        
                        if (ogrTypesAndCSharpTypesMapping.ContainsKey(ogrFldDef.GetFieldType()))
                        {
                            featureDataTable.Columns.Add(ogrFldDef.GetName(),
                                                                              ogrTypesAndCSharpTypesMapping[ogrFldDef.GetFieldType()]);
                        }
                        else
                        {
                            //fdt.Columns.Add(_OgrFldDef.GetName(), System.Type.GetType("System.String"));
                            Debug.WriteLine("Not supported type: " + ogrFldDef.GetFieldType() + " [" +
                                            ogrFldDef.GetName() + "]");
                            break;
                        }
                    }
                }
            }
            return featureDataTable;
        }

        public virtual string Path { get{ return path;} set{ Open(value);} }

        public virtual string LayerName
        {
            get { return layerName; }
            set
            {
                layerName = value;
                if (IsOpen)
                {
                    if (ogrLayer != null)
                    {
                        ogrLayer.Dispose();
                    }
                    OpenLayer();
                }
            }
        }

        public virtual void Open()
        {
            if (!string.IsNullOrEmpty(path))
            {
                Open(path);
            }
        }


        public virtual  void CreateNew(string path)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Closes the datasource
        /// </summary>
        public virtual void Close()
        {
            //make sure to release unmanaged objects
            if (ogrDataSource != null)
            {
                ogrDataSource.Dispose();
                ogrDataSource = null;
            }
            if (ogrLayer != null)
            {
                ogrLayer.Dispose();
                ogrLayer = null;
            }
            _IsOpen = false;
        }

        /// <summary>
        /// Returns true if the datasource is currently open
        /// </summary>
        public virtual bool IsOpen
        {
            get { return _IsOpen; }
        }

        #endregion

/*        public virtual FeatureDataSet ExecuteQuery(string query)
        {
            return ExecuteQuery(query, null);
        }*/

/*
        public virtual FeatureDataSet ExecuteQuery(string query, Geometry filter)
        {
            try
            {
                var featureDataSet = new FeatureDataSet();
                featureDataTable.Clear();

                using (Layer ogrLayer = ogrDataSource.ExecuteSQL(query, filter, ""))
                {
                    ogrLayer.ResetReading();
  
                    int featureCount = ogrLayer.GetFeatureCount(1);

                    for (int i = 0; i < featureCount;i++ )
                    {
                        using (var ogrFeature = ogrLayer.GetNextFeature())
                        {
                            FeatureDataRow feature = featureDataTable.NewRow();
                            feature.Id = ogrFeature.GetFID();
                            SetFeatureValuesInRow(featureDataTable, ogrFeature, feature);

                            using (var geometry = ogrFeature.GetGeometryRef())
                            {
                                feature.Geometry = ParseOgrGeometry(geometry);
                            }

                            featureDataTable.AddRow(feature);
                            ogrFeature.Dispose();//release memory, do not wait for gc to do this!

                        }
                    }
                    featureDataSet.Tables.Add(featureDataTable);
                    ogrDataSource.ReleaseResultSet(ogrLayer);

                    return featureDataSet;
                }
            }
            catch (Exception exception)
            {
                log.Error("Error while reading features", exception);
                return new FeatureDataSet();
            }
        }
*/

        private static void SetFeatureValuesInRow(FeatureDataTable featureDataTable, Feature ogrFeature,
                                                  FeatureDataRow feature)
        {
            for (int iField = 0; iField < ogrFeature.GetFieldCount(); iField++)
            {
                if (featureDataTable.Columns[iField].DataType == Type.GetType("System.String"))
                {
                    feature[iField] = ogrFeature.GetFieldAsString(iField);
                }
                else if (featureDataTable.Columns[iField].DataType == Type.GetType("System.Int32"))
                {
                    feature[iField] = ogrFeature.GetFieldAsInteger(iField);
                }
                else if (featureDataTable.Columns[iField].DataType == Type.GetType("System.Double"))
                {
                    feature[iField] = ogrFeature.GetFieldAsDouble(iField);
                }
                else if (featureDataTable.Columns[iField].DataType == Type.GetType("System.DateTime"))
                {
                    int year;
                    int month;
                    int day;
                    int hour;
                    int minute;
                    int second;
                    int TZflag;
                    ogrFeature.GetFieldAsDateTime(iField, out year, out month, out day, out hour, out minute, out second,
                                                  out TZflag);
                    if (year == 0 || month == 0 || day == 0)
                    {
                        feature[iField] = DBNull.Value;
                    }
                    else
                    {
                        feature[iField] = new DateTime(year, month, day, hour, minute, second);
                    }

                }
                else
                {
                    feature[iField] = ogrFeature.GetFieldAsString(iField);
                }
            }
        }

        /// <summary>
        /// Returns the data associated with all the geometries that is within 'distance' of 'geom'
        /// </summary>
        /// <param name="geom"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        [Obsolete("Use GetFeatures instead")]
        public virtual FeatureDataTable QueryFeatures(IGeometry geom, double distance)
        {
            throw new NotImplementedException();
        }

        public virtual string FileFilter
        {
            get { return "Geodatabase (*.mdb)|*.mdb"; }
        }

        public virtual bool IsRelationalDataBase
        {
            get{ return true; }
        }
    }
}