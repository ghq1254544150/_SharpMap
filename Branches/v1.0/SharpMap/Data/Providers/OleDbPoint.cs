// Copyright 2006 - Morten Nielsen (www.iter.dk)
//
// This file is part of SharpMap.
// SharpMap is free software; you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
// 
// SharpMap is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.

// You should have received a copy of the GNU Lesser General Public License
// along with SharpMap; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA 

using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.OleDb;
using GeoAPI.Geometries;

namespace SharpMap.Data.Providers
{
    /// <summary>
    /// The OleDbPoint provider is used for rendering point data from an OleDb compatible datasource.
    /// </summary>
    /// <remarks>
    /// <para>The data source will need to have two double-type columns, xColumn and yColumn that contains the coordinates of the point,
    /// and an integer-type column containing a unique identifier for each row.</para>
    /// <para>To get good performance, make sure you have applied indexes on ID, xColumn and yColumns in your datasource table.</para>
    /// </remarks>
    public class OleDbPoint : IProvider
    {
        private readonly IGeometryFactory _factory;
        
        private string _connectionString;
        private string _defintionQuery;
        private bool _isOpen;
        private string _objectIdColumn;
        private int _srid = -1;
        private string _table;
        private string _xColumn;
        private string _yColumn;

        private OleDbPoint()
        {
            _factory = SharpMap.Geometries.SharpMapGeometryFactory.Instance;
        }

        /// <summary>
        /// Initializes a new instance of the OleDbPoint provider
        /// </summary>
        /// <param name="connectionStr"></param>
        /// <param name="tablename"></param>
        /// <param name="oidColumnName"></param>
        /// <param name="xColumn"></param>
        /// <param name="yColumn"></param>
        public OleDbPoint(string connectionStr, string tablename, string oidColumnName, string xColumn, string yColumn)
            :this()
        {
            Table = tablename;
            XColumn = xColumn;
            YColumn = yColumn;
            ObjectIdColumn = oidColumnName;
            ConnectionString = connectionStr;
        }

        /// <summary>
        /// Data table name
        /// </summary>
        public string Table
        {
            get { return _table; }
            set { _table = value; }
        }


        /// <summary>
        /// Name of column that contains the Object ID
        /// </summary>
        public string ObjectIdColumn
        {
            get { return _objectIdColumn; }
            set { _objectIdColumn = value; }
        }

        /// <summary>
        /// Name of column that contains X coordinate
        /// </summary>
        public string XColumn
        {
            get { return _xColumn; }
            set { _xColumn = value; }
        }

        /// <summary>
        /// Name of column that contains Y coordinate
        /// </summary>
        public string YColumn
        {
            get { return _yColumn; }
            set { _yColumn = value; }
        }

        /// <summary>
        /// Connectionstring
        /// </summary>
        public string ConnectionString
        {
            get { return _connectionString; }
            set { _connectionString = value; }
        }

        /// <summary>
        /// Definition query used for limiting dataset
        /// </summary>
        public string DefinitionQuery
        {
            get { return _defintionQuery; }
            set { _defintionQuery = value; }
        }

        #region IProvider Members

        /// <summary>
        /// Returns geometries within the specified bounding box
        /// </summary>
        /// <param name="bbox"></param>
        /// <returns></returns>
        public Collection<IGeometry> GetGeometriesInView(GeoAPI.Geometries.Envelope bbox)
        {
            Collection<IGeometry> features = new Collection<IGeometry>();
            using (OleDbConnection conn = new OleDbConnection(_connectionString))
            {
                string strSQL = "Select " + XColumn + ", " + YColumn + " FROM " + Table + " WHERE ";
                if (!String.IsNullOrEmpty(_defintionQuery))
                    strSQL += _defintionQuery + " AND ";
                //Limit to the points within the boundingbox
                strSQL += XColumn + " BETWEEN " + bbox.MinX.ToString(Map.NumberFormatEnUs) + " AND " +
                          bbox.MaxX.ToString(Map.NumberFormatEnUs) + " AND " +
                          YColumn + " BETWEEN " + bbox.MinY.ToString(Map.NumberFormatEnUs) + " AND " +
                          bbox.MaxY.ToString(Map.NumberFormatEnUs);

                using (OleDbCommand command = new OleDbCommand(strSQL, conn))
                {
                    conn.Open();
                    using (OleDbDataReader dr = command.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            if (dr[0] != DBNull.Value && dr[1] != DBNull.Value)
                                features.Add(new Point((double) dr[0], (double) dr[1]));
                        }
                    }
                    conn.Close();
                }
            }
            return features;
        }

        /// <summary>
        /// Returns geometry Object IDs whose bounding box intersects 'bbox'
        /// </summary>
        /// <param name="bbox"></param>
        /// <returns></returns>
        public Collection<uint> GetObjectIDsInView(GeoAPI.Geometries.Envelope bbox)
        {
            Collection<uint> objectlist = new Collection<uint>();
            using (OleDbConnection conn = new OleDbConnection(_connectionString))
            {
                string strSQL = "Select " + ObjectIdColumn + " FROM " + Table + " WHERE ";
                if (!String.IsNullOrEmpty(_defintionQuery))
                    strSQL += _defintionQuery + " AND ";
                //Limit to the points within the boundingbox
                strSQL += XColumn + " BETWEEN " + bbox.MinX.ToString(Map.NumberFormatEnUs) + " AND " +
                          bbox.MaxX.ToString(Map.NumberFormatEnUs) + " AND " + YColumn +
                          " BETWEEN " + bbox.MinY.ToString(Map.NumberFormatEnUs) + " AND " +
                          bbox.MaxY.ToString(Map.NumberFormatEnUs);

                using (OleDbCommand command = new OleDbCommand(strSQL, conn))
                {
                    conn.Open();
                    using (OleDbDataReader dr = command.ExecuteReader())
                    {
                        while (dr.Read())
                            if (dr[0] != DBNull.Value)
                                objectlist.Add((uint) (int) dr[0]);
                    }
                    conn.Close();
                }
            }
            return objectlist;
        }

        /// <summary>
        /// Returns the geometry corresponding to the Object ID
        /// </summary>
        /// <param name="oid">Object ID</param>
        /// <returns>geometry</returns>
        public IGeometry GetGeometryByID(uint oid)
        {
            IGeometry geom = null;
            using (OleDbConnection conn = new OleDbConnection(_connectionString))
            {
                string strSQL = "Select " + XColumn + ", " + YColumn + " FROM " + Table + " WHERE " + ObjectIdColumn +
                                "=" + oid.ToString();
                using (OleDbCommand command = new OleDbCommand(strSQL, conn))
                {
                    conn.Open();
                    using (OleDbDataReader dr = command.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            //If the read row is OK, create a point geometry from the XColumn and YColumn and return it
                            if (dr[0] != DBNull.Value && dr[1] != DBNull.Value)
                                geom = new Point((double) dr[0], (double) dr[1]);
                        }
                    }
                    conn.Close();
                }
            }
            return geom;
        }

        /// <summary>
        /// Throws NotSupportedException. 
        /// </summary>
        /// <param name="geom"></param>
        /// <param name="ds">FeatureDataSet to fill data into</param>
        public void ExecuteIntersectionQuery(IGeometry geom, FeatureDataSet ds)
        {
            throw new NotSupportedException(
                "ExecuteIntersectionQuery(Geometry) is not supported by the OleDbPointProvider.");
            //When relation model has been implemented the following will complete the query
            /*
			ExecuteIntersectionQuery(geom.GetBoundingBox(), ds);
			if (ds.Tables.Count > 0)
			{
				for(int i=ds.Tables[0].Count-1;i>=0;i--)
				{
					if (!geom.Intersects(ds.Tables[0][i].Geometry))
						ds.Tables.RemoveAt(i);
				}
			}
			*/
        }

        /// <summary>
        /// Returns all features with the view box
        /// </summary>
        /// <param name="bbox">view box</param>
        /// <param name="ds">FeatureDataSet to fill data into</param>
        public void ExecuteIntersectionQuery(GeoAPI.Geometries.Envelope bbox, FeatureDataSet ds)
        {
            //List<Geometries.IGeometry> features = new List<SharpMap.Geometries.IGeometry>();
            using (OleDbConnection conn = new OleDbConnection(_connectionString))
            {
                string strSQL = "Select * FROM " + Table + " WHERE ";
                if (!String.IsNullOrEmpty(_defintionQuery))
                    //If a definition query has been specified, add this as a filter on the query
                    strSQL += _defintionQuery + " AND ";
                //Limit to the points within the boundingbox
                strSQL += XColumn + " BETWEEN " + bbox.MinX.ToString(Map.NumberFormatEnUs) + " AND " +
                          bbox.MaxX.ToString(Map.NumberFormatEnUs) + " AND " + YColumn +
                          " BETWEEN " + bbox.MinY.ToString(Map.NumberFormatEnUs) + " AND " +
                          bbox.MaxY.ToString(Map.NumberFormatEnUs);

                using (OleDbDataAdapter adapter = new OleDbDataAdapter(strSQL, conn))
                {
                    conn.Open();
                    DataSet ds2 = new DataSet();
                    adapter.Fill(ds2);
                    conn.Close();
                    if (ds2.Tables.Count > 0)
                    {
                        FeatureDataTable fdt = new FeatureDataTable(ds2.Tables[0]);
                        foreach (DataColumn col in ds2.Tables[0].Columns)
                            fdt.Columns.Add(col.ColumnName, col.DataType, col.Expression);
                        foreach (DataRow dr in ds2.Tables[0].Rows)
                        {
                            FeatureDataRow fdr = fdt.NewRow();
                            foreach (DataColumn col in ds2.Tables[0].Columns)
                                fdr[col.ColumnName] = dr[col];
                            if (dr[XColumn] != DBNull.Value && dr[YColumn] != DBNull.Value)
                                fdr.Geometry = new Point((double) dr[XColumn], (double) dr[YColumn]);
                            fdt.AddRow(fdr);
                        }
                        ds.Tables.Add(fdt);
                    }
                }
            }
        }

        /// <summary>
        /// Returns the number of features in the dataset
        /// </summary>
        /// <returns>Total number of features</returns>
        public int GetFeatureCount()
        {
            int count = 0;
            using (OleDbConnection conn = new OleDbConnection(_connectionString))
            {
                string strSQL = "Select Count(*) FROM " + Table;
                if (!String.IsNullOrEmpty(_defintionQuery))
                    //If a definition query has been specified, add this as a filter on the query
                    strSQL += " WHERE " + _defintionQuery;

                using (OleDbCommand command = new OleDbCommand(strSQL, conn))
                {
                    conn.Open();
                    count = (int) command.ExecuteScalar();
                    conn.Close();
                }
            }
            return count;
        }

        /// <summary>
        /// Returns a datarow based on a RowID
        /// </summary>
        /// <param name="rowId"></param>
        /// <returns>datarow</returns>
        public FeatureDataRow GetFeature(uint rowId)
        {
            using (OleDbConnection conn = new OleDbConnection(_connectionString))
            {
                string strSQL = "select * from " + Table + " WHERE " + ObjectIdColumn + "=" + rowId.ToString();

                using (OleDbDataAdapter adapter = new OleDbDataAdapter(strSQL, conn))
                {
                    conn.Open();
                    DataSet ds = new DataSet();
                    adapter.Fill(ds);
                    conn.Close();
                    if (ds.Tables.Count > 0)
                    {
                        FeatureDataTable fdt = new FeatureDataTable(ds.Tables[0]);
                        foreach (DataColumn col in ds.Tables[0].Columns)
                            fdt.Columns.Add(col.ColumnName, col.DataType, col.Expression);
                        if (ds.Tables[0].Rows.Count > 0)
                        {
                            DataRow dr = ds.Tables[0].Rows[0];
                            FeatureDataRow fdr = fdt.NewRow();
                            foreach (DataColumn col in ds.Tables[0].Columns)
                                fdr[col.ColumnName] = dr[col];
                            if (dr[XColumn] != DBNull.Value && dr[YColumn] != DBNull.Value)
                                fdr.Geometry = new Point((double) dr[XColumn], (double) dr[YColumn]);
                            return fdr;
                        }
                        else
                            return null;
                    }
                    else
                        return null;
                }
            }
        }

        /// <summary>
        /// Boundingbox of dataset
        /// </summary>
        /// <returns>boundingbox</returns>
        public GeoAPI.Geometries.Envelope GetExtents()
        {
            GeoAPI.Geometries.Envelope box = null;
            using (OleDbConnection conn = new OleDbConnection(_connectionString))
            {
                string strSQL = "Select Min(" + XColumn + ") as MinX, Min(" + YColumn + ") As MinY, " +
                                "Max(" + XColumn + ") As MaxX, Max(" + YColumn + ") As MaxY FROM " + Table;
                if (!String.IsNullOrEmpty(_defintionQuery))
                    //If a definition query has been specified, add this as a filter on the query
                    strSQL += " WHERE " + _defintionQuery;

                using (OleDbCommand command = new OleDbCommand(strSQL, conn))
                {
                    conn.Open();
                    using (OleDbDataReader dr = command.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            //If the read row is OK, create a point geometry from the XColumn and YColumn and return it
                            if (dr[0] != DBNull.Value && dr[1] != DBNull.Value && dr[2] != DBNull.Value &&
                                dr[3] != DBNull.Value)
                                box = new GeoAPI.Geometries.Envelope((double) dr[0], (double) dr[1], (double) dr[2], (double) dr[3]);
                        }
                    }
                    conn.Close();
                }
            }
            return box;
        }

        /// <summary>
        /// Gets the connection ID of the datasource
        /// </summary>
        public string ConnectionID
        {
            get { return _connectionString; }
        }

        /// <summary>
        /// Opens the datasource
        /// </summary>
        public void Open()
        {
            //Don't really do anything. OleDb's ConnectionPooling takes over here
            _isOpen = true;
        }

        /// <summary>
        /// Closes the datasource
        /// </summary>
        public void Close()
        {
            //Don't really do anything. OleDb's ConnectionPooling takes over here
            _isOpen = false;
        }

        /// <summary>
        /// Returns true if the datasource is currently open
        /// </summary>
        public bool IsOpen
        {
            get { return _isOpen; }
        }

        /// <summary>
        /// The spatial reference ID (CRS)
        /// </summary>
        public int SRID
        {
            get { return _srid; }
            set { _srid = value; }
        }

        #endregion

        #region Disposers and finalizers

        private bool disposed = false;

        /// <summary>
        /// Disposes the object
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        internal void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                }
                disposed = true;
            }
        }

        /// <summary>
        /// Finalizer
        /// </summary>
        ~OleDbPoint()
        {
            Dispose();
        }

        #endregion
    }
}