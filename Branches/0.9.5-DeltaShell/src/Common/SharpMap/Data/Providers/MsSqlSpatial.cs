// Copyright 2006 - Ricardo Stuven (rstuven@gmail.com)
// Copyright 2006 - Morten Nielsen (www.iter.dk)
//
// MsSqlSpatial provider by Ricardo Stuven.
// Based on PostGIS provider by Morten Nielsen.
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
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using DelftTools.Utils.Data;
using DelftTools.Utils.IO;
using GeoAPI.CoordinateSystems;
using GeoAPI.Extensions.Feature;
using GeoAPI.Geometries;
using SharpMap.Api;


namespace SharpMap.Data.Providers
{
	/// <summary>
	/// Microsoft SQL Server 2005 / MsSqlSpatial dataprovider
	/// </summary>
	/// <example>
	/// Adding a datasource to a layer:
	/// <code lang="C#">
	/// SharpMap.Layers.VectorLayer myLayer = new SharpMap.Layers.VectorLayer("My layer");
	/// string ConnStr = @"Data Source=localhost\sqlexpress;Initial Catalog=myGisDb;Integrated Security=SSPI;";
	/// myLayer.DataSource = new SharpMap.Data.Providers.MsSqlSpatial(ConnStr, "myTable", "myId");
	/// </code>
	/// </example>
	[Serializable]
    public class MsSqlSpatial : Unique<long>, IFeatureProvider, IFileBased
	{
	    public Type FeatureType
	    {
            get { return typeof(FeatureDataRow); }
        }

	    public IList Features
	    {
	        get { throw new System.NotImplementedException(); }
	        set { throw new System.NotImplementedException(); }
	    }

        public bool IsReadOnly { get { return true; } }

	    /// <summary>
		/// Initializes a new connection to MsSqlSpatial
		/// </summary>
		/// <param name="connectionString">Connectionstring</param>
		/// <param name="tableName">Name of data table</param>
		/// <param name="geometryColumnName">Name of geometry column</param>
		/// /// <param name="identifierColumnName">Name of column with unique identifier</param>
		public MsSqlSpatial(string connectionString, string tableName, string geometryColumnName, string identifierColumnName)
		{
			this.ConnectionString = connectionString;
			this.Table = tableName;
			this.GeometryColumn = geometryColumnName;
			this.ObjectIdColumn = identifierColumnName;
		}

		/// <summary>
		/// Initializes a new connection to MsSqlSpatial
		/// </summary>
		/// <param name="ConnectionStr">Connectionstring</param>
		/// <param name="tablename">Name of data table</param>
		/// <param name="OID_ColumnName">Name of column with unique identifier</param>
		public MsSqlSpatial(string connectionString, string tableName, string identifierColumnName)
			: this(connectionString, tableName, "", identifierColumnName)
		{
			this.GeometryColumn = this.GetGeometryColumn();
		}

		private bool _IsOpen;

	    public void Open(string path)
	    {
	        throw new NotImplementedException();
	    }

	    /// <summary>
		/// Returns true if the datasource is currently open
		/// </summary>
		public bool IsOpen
		{
			get { return _IsOpen; }
		}

	    public virtual void ReConnect()
	    {
	        
	    }

        public virtual void Delete()
	    {
            File.Delete(Path);
	    }

        public IEnumerable<string> Paths
        {
            get
            {
                if (Path != null)
                    yield return Path;
            }
        }
	    public string Path
	    {
	        get { throw new NotImplementedException(); }
	        set { throw new NotImplementedException(); }
	    }

	    public void CreateNew(string path)
	    {
	        throw new NotImplementedException();
	    }

	    /// <summary>
		/// Closes the datasource
		/// </summary>
		public void Close()
		{
			//Don't really do anything. SqlClient's ConnectionPooling takes over here
			_IsOpen = false;
		}

        public void SwitchTo(string newPath)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(string newPath)
        {
            throw new NotImplementedException();
        }
	

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
					//Close();
				}
				disposed = true;
			}
		}

		/// <summary>
		/// Finalizer
		/// </summary>
		~MsSqlSpatial()
		{
			Dispose();
		}
		#endregion

		private string _ConnectionString;

		/// <summary>
		/// Connectionstring
		/// </summary>
		public string ConnectionString
		{
			get { return _ConnectionString; }
			set { _ConnectionString = value; }
		}

		private string _Table;

		/// <summary>
		/// Data table name
		/// </summary>
		public string Table
		{
			get { return _Table; }
			set { _Table = value; }
		}

		private string _GeometryColumn;

		/// <summary>
		/// Name of geometry column
		/// </summary>
		public string GeometryColumn
		{
			get { return _GeometryColumn; }
			set { _GeometryColumn = value; }
		}

		private string _GeometryExpression = "{0}";
		/// <summary>
		/// Expression template for geometry column evaluation.
		/// </summary>
		/// <example>
		/// You could, for instance, simplify your geometries before they're displayed.
		/// Simplification helps to speed the rendering of big geometries.
		/// Here's a sample code to simplify geometries using 100 meters of threshold.
		/// <code>
		/// datasource.GeometryExpression = "ST.Simplify({0}, 100)";
		/// </code>
		/// Also you could draw a 20 meters buffer around those little points:
		/// <code>
		/// datasource.GeometryExpression = "ST.Buffer({0}, 20)";
		/// </code>
		/// </example>
		public string GeometryExpression
		{
			get { return _GeometryExpression; }
			set { _GeometryExpression = value; }
		}

		private string _FeatureColumns = "*";
		/// <summary>
		/// List of columns or T-SQL expressions separated by comma.
		/// Using "*" (the value by default), all columns are selected.
		/// </summary>
		public string FeatureColumns
		{
			get { return _FeatureColumns; }
			set { _FeatureColumns = value; }
		}

		private string _ObjectIdColumn;
		/// <summary>
		/// Name of column that contains the Object ID
		/// </summary>
		public string ObjectIdColumn
		{
			get { return _ObjectIdColumn; }
			set { _ObjectIdColumn = value; }
		}

		private string _DefinitionQuery = String.Empty;
		/// <summary>
		/// Definition query used for limiting dataset (WHERE clause)
		/// </summary>
		public string DefinitionQuery
		{
			get { return _DefinitionQuery; }
			set { _DefinitionQuery = value; }
		}

		private string _OrderQuery = String.Empty;
		/// <summary>
		/// Columns or T-SQL expressions for sorting (ORDER BY clause)
		/// </summary>
		public string OrderQuery
		{
			get { return _OrderQuery; }
			set { _OrderQuery = value; }
		}

/*
        private int _TargetSRID = -1;
        /// <summary>
        /// The target spatial reference ID (SRID). 
        /// It allows on-the-fly transformations in the server-side.
        /// </summary>
        public int TargetSRID
        {
            get { return _TargetSRID; }
            set { _TargetSRID = value; }
        }
*/

        private string TargetGeometryColumn
        {
            get
            {
/*
                if (this.SrsWkt > 0 && this.TargetSRID > 0 && this.SrsWkt != this.TargetSRID)
                    return "ST.Transform(" + this.GeometryColumn + "," + this.TargetSRID + ")";
                else
*/
                    return this.GeometryColumn;
            }
        }

        public IFeature Add(IGeometry geometry)
	    {
	        throw new System.NotImplementedException();
	    }

        public Func<IFeatureProvider, IGeometry, IFeature> AddNewFeatureFromGeometryDelegate { get; set; }
	    public event EventHandler FeaturesChanged;

	    /// <summary>
		/// Returns the geometry corresponding to the Object ID
		/// </summary>
		/// <param name="oid">Object ID</param>
		/// <returns>geometry</returns>
		public IGeometry GetGeometryByID(int oid)
		{
			GeoAPI.Geometries.IGeometry geom = null;
			using (SqlConnection conn = new SqlConnection(this.ConnectionString))
			{
				string strSQL = "SELECT ST.AsBinary(" + this.BuildGeometryExpression() + ") AS Geom FROM " + this.Table + " WHERE " + this.ObjectIdColumn + "='" + oid.ToString() + "'";
				conn.Open();
				using (SqlCommand command = new SqlCommand(strSQL, conn))
				{
					using (SqlDataReader dr = command.ExecuteReader())
					{
						while (dr.Read())
						{
							if (dr[0] != DBNull.Value)
								geom = SharpMap.Converters.WellKnownBinary.GeometryFromWKB.Parse((byte[])dr[0]);
						}
					}
				}
				conn.Close();
			}
			return geom;
		}

	    /// <summary>
		/// Returns all objects within a distance of a geometry
		/// </summary>
		/// <param name="geom"></param>
		/// <param name="distance"></param>
		/// <returns></returns>
		[Obsolete("Use GetFeatures instead")]
		public SharpMap.Data.FeatureDataTable QueryFeatures(GeoAPI.Geometries.IGeometry geom, double distance)
		{
			//List<Geometries.Geometry> features = new List<SharpMap.Geometries.Geometry>();
			using (SqlConnection conn = new SqlConnection(this.ConnectionString))
			{
				string strGeom;
/*
				if (this.TargetSRID > 0 && this.SrsWkt > 0 && this.SrsWkt != this.TargetSRID)
					strGeom = "ST.Transform(ST.GeomFromText('" + geom.AsText() + "'," + this.TargetSRID.ToString() + ")," + this.SrsWkt.ToString() + ")";
				else
*/
					strGeom = "ST.GeomFromText('" + geom.AsText() + "', " + this.SrsWkt.ToString() + ")";

				string strSQL = "SELECT " + this.FeatureColumns + ", ST.AsBinary(" + this.BuildGeometryExpression() + ") As sharpmap_tempgeometry ";
				strSQL += "FROM ST.IsWithinDistanceQuery" + this.BuildSpatialQuerySuffix() + "(" + strGeom + ", " + distance.ToString(Map.numberFormat_EnUS) + ")";

				if (!String.IsNullOrEmpty(this.DefinitionQuery))
					strSQL += " WHERE " + this.DefinitionQuery;

				if (!String.IsNullOrEmpty(this.OrderQuery))
					strSQL += " ORDER BY " + this.OrderQuery;

				using (SqlDataAdapter adapter = new SqlDataAdapter(strSQL, conn))
				{
					System.Data.DataSet ds = new System.Data.DataSet();
					conn.Open();
					adapter.Fill(ds);
					conn.Close();
					if (ds.Tables.Count > 0)
					{
						FeatureDataTable fdt = new FeatureDataTable(ds.Tables[0]);
						foreach (System.Data.DataColumn col in ds.Tables[0].Columns)
							if (col.ColumnName != this.GeometryColumn && !col.ColumnName.StartsWith(this.GeometryColumn + "_Envelope_") && col.ColumnName != "sharpmap_tempgeometry")
								fdt.Columns.Add(col.ColumnName,col.DataType,col.Expression);
						foreach (System.Data.DataRow dr in ds.Tables[0].Rows)
						{
							SharpMap.Data.FeatureDataRow fdr = fdt.NewRow();
							foreach(System.Data.DataColumn col in ds.Tables[0].Columns)
								if (col.ColumnName != this.GeometryColumn && !col.ColumnName.StartsWith(this.GeometryColumn + "_Envelope_") && col.ColumnName != "sharpmap_tempgeometry")
									fdr[col.ColumnName] = dr[col];
							if (dr["sharpmap_tempgeometry"] != DBNull.Value)
								fdr.Geometry = SharpMap.Converters.WellKnownBinary.GeometryFromWKB.Parse((byte[])dr["sharpmap_tempgeometry"]);
							fdt.AddRow(fdr);								
						}
						return fdt;
					}
					else return null;
				}
			}
		}

		/// <summary>
		/// Returns the features that intersects with 'geom'
		/// </summary>
		/// <param name="geom"></param>
		/// <param name="ds">FeatureDataSet to fill data into</param>
		public IEnumerable<IFeature> GetFeatures(GeoAPI.Geometries.IGeometry geom)
		{
            FeatureDataSet ds = new FeatureDataSet(); 
            List<IGeometry> features = new List<IGeometry>();
			using (SqlConnection conn = new SqlConnection(this.ConnectionString))
			{
				string strGeom;
/*
				if (this.TargetSRID > 0 && this.SrsWkt > 0 && this.SrsWkt != this.TargetSRID)
					strGeom = "ST.Transform(ST.GeomFromText('" + geom.AsText() + "'," + this.TargetSRID.ToString() + ")," + this.SrsWkt.ToString() + ")";
				else
*/
					strGeom = "ST.GeomFromText('" + geom.AsText() + "', " + this.SrsWkt.ToString() + ")";

				string strSQL = "SELECT " + this.FeatureColumns + ", ST.AsBinary(" + this.BuildGeometryExpression() + ") As sharpmap_tempgeometry ";
				strSQL += "FROM ST.RelateQuery" + this.BuildSpatialQuerySuffix() + "(" + strGeom + ", 'intersects')";

				if (!String.IsNullOrEmpty(this.DefinitionQuery))
					strSQL += " WHERE " + this.DefinitionQuery;

				if (!String.IsNullOrEmpty(this.OrderQuery))
					strSQL += " ORDER BY " + this.OrderQuery;

				using (SqlDataAdapter adapter = new SqlDataAdapter(strSQL, conn))
				{
					conn.Open();
					adapter.Fill(ds);
					conn.Close();
					if (ds.Tables.Count > 0)
					{
						FeatureDataTable fdt = new FeatureDataTable(ds.Tables[0]);
						foreach (System.Data.DataColumn col in ds.Tables[0].Columns)
							if (col.ColumnName != this.GeometryColumn && !col.ColumnName.StartsWith(this.GeometryColumn + "_Envelope_") && col.ColumnName != "sharpmap_tempgeometry")
								fdt.Columns.Add(col.ColumnName, col.DataType, col.Expression);
						foreach (System.Data.DataRow dr in ds.Tables[0].Rows)
						{
							SharpMap.Data.FeatureDataRow fdr = fdt.NewRow();
							foreach (System.Data.DataColumn col in ds.Tables[0].Columns)
								if (col.ColumnName != this.GeometryColumn && !col.ColumnName.StartsWith(this.GeometryColumn + "_Envelope_") && col.ColumnName != "sharpmap_tempgeometry")
									fdr[col.ColumnName] = dr[col];
							if (dr["sharpmap_tempgeometry"] != DBNull.Value)
								fdr.Geometry = SharpMap.Converters.WellKnownBinary.GeometryFromWKB.Parse((byte[])dr["sharpmap_tempgeometry"]);
							fdt.AddRow(fdr);
						}
						ds.Tables.Add(fdt);

					    return fdt;
					}
				}
			}

		    return null;
		}

		/// <summary>
		/// Returns the number of features in the dataset
		/// </summary>
		/// <returns>number of features</returns>
		public int GetFeatureCount()
		{
			int count = 0;
			using (SqlConnection conn = new SqlConnection(_ConnectionString))
			{
				string strSQL = "SELECT COUNT(*) FROM " + this.Table;
				if (!String.IsNullOrEmpty(this.DefinitionQuery))
					strSQL += " WHERE " + this.DefinitionQuery;
				using (SqlCommand command = new SqlCommand(strSQL, conn))
				{
					conn.Open();
					count = (int)command.ExecuteScalar();
					conn.Close();
				}				
			}
			return count;
		}

		#region IFeatureProvider Members

		/// <summary>
		/// Gets a collection of columns in the dataset
		/// </summary>
		public System.Data.DataColumnCollection Columns
		{
			get {
				throw new NotImplementedException();
				//using (SqlConnection conn = new SqlConnection(this.ConnectionString))
				//{
				//    System.Data.DataColumnCollection columns = new System.Data.DataColumnCollection();
				//    string strSQL = "SELECT column_name, udt_name FROM information_schema.columns WHERE table_name='" + this.Table + "' ORDER BY ordinal_position";
				//    using (SqlCommand command = new SqlCommand(strSQL, conn))
				//    {
				//        conn.Open();
				//        using (SqlDataReader dr = command.ExecuteReader())
				//        {
				//            while (dr.Read())
				//            {
				//                System.Data.DataColumn col = new System.Data.DataColumn((string)dr["column_name"]);
				//                switch((string)dr["udt_name"])
				//                {
				//                    case "int4":
				//                        col.DataType = typeof(Int32);
				//                        break;
				//                    case "int8":
				//                        col.DataType = typeof(Int64);
				//                        break;
				//                    case "varchar":
				//                        col.DataType = typeof(string);
				//                        break;
				//                    case "text":
				//                        col.DataType = typeof(string);
				//                        break;
				//                    case "bool":
				//                        col.DataType = typeof(bool);
				//                        break;
				//                    case "geometry":
				//                        col.DataType = typeof(SharpMap.Geometries.Geometry);
				//                        break;
				//                    default:
				//                        col.DataType = typeof(object);
				//                        break;
				//                }
				//                columns.Add(col);
				//            }
				//        }
				//    }
				//    return columns;
				//}
			}
		}

		private string srsProj4;

		/// <summary>
		/// Spacial Reference in PROJ.4 format
		/// </summary>
		public string SrsWkt
		{
			get {
				if (string.IsNullOrEmpty(srsProj4))
				{
					int dotPos = this.Table.IndexOf(".");
					string strSQL = "";
					if (dotPos == -1)
						strSQL = "select SRID from ST.GEOMETRY_COLUMNS WHERE F_TABLE_NAME='" + this.Table + "'";
					else
					{
						string schema = this.Table.Substring(0, dotPos);
						string table = this.Table.Substring(dotPos + 1);
						strSQL = "select SRID from ST.GEOMETRY_COLUMNS WHERE F_TABLE_SCHEMA='" + schema + "' AND F_TABLE_NAME='" + table + "'";
					}

					using (SqlConnection conn = new SqlConnection(_ConnectionString))
					{
						using (SqlCommand command = new SqlCommand(strSQL, conn))
						{
							try
							{
								conn.Open();

                                if (Map.CoordinateSystemFactory != null)
                                {
                                    var srid = (int)command.ExecuteScalar();
                                    CoordinateSystem = Map.CoordinateSystemFactory.CreateFromEPSG(srid);
                                    srsProj4 = CoordinateSystem.PROJ4;
                                }
                                conn.Close();
							}
							catch
							{
							    srsProj4 = string.Empty;
							    //srsWkt = -1;
							}
						}
					}
				}

				return srsProj4;
			}
			set
			{
                throw new NotImplementedException();
			}
		}

	    public IEnvelope GetBounds(int recordIndex)
	    {
	        return GetFeature(recordIndex).Geometry.EnvelopeInternal;
	    }

	    public ICoordinateSystem CoordinateSystem { get; set; }

	    /// <summary>
		/// Queries the MsSqlSpatial database to get the name of the Geometry Column. This is used if the columnname isn't specified in the constructor
		/// </summary>
		/// <remarks></remarks>
		/// <returns>Name of column containing geometry</returns>
		private string GetGeometryColumn()
		{
			string strSQL = "select F_GEOMETRY_COLUMN from ST.GEOMETRY_COLUMNS WHERE F_TABLE_NAME='" + this.Table + "'";
			using (SqlConnection conn = new SqlConnection(_ConnectionString))
				using (SqlCommand command = new SqlCommand(strSQL, conn))
				{
					conn.Open();
					object columnname = command.ExecuteScalar();
					conn.Close();
					if (columnname == System.DBNull.Value)
						throw new ApplicationException("Table '" + this.Table + "' does not contain a geometry column");
					return (string)columnname;					
				}
		}


		/// <summary>
		/// Returns a datarow based on a RowID
		/// </summary>
		/// <param name="index"></param>
		/// <returns>datarow</returns>
		public IFeature GetFeature(int index)
		{
			using (SqlConnection conn = new SqlConnection(_ConnectionString))
			{
				string strSQL = "select " + this.FeatureColumns + ", ST.AsBinary(" + this.BuildGeometryExpression() + ") As sharpmap_tempgeometry from " + this.Table + " WHERE " + this.ObjectIdColumn + "='" + index.ToString() + "'";
				using (SqlDataAdapter adapter = new SqlDataAdapter(strSQL, conn))
				{
					FeatureDataSet ds = new FeatureDataSet();
					conn.Open();
					adapter.Fill(ds);
					conn.Close();
					if (ds.Tables.Count > 0)
					{
						FeatureDataTable fdt = new FeatureDataTable(ds.Tables[0]);
						foreach (System.Data.DataColumn col in ds.Tables[0].Columns)
							if (col.ColumnName != this.GeometryColumn && !col.ColumnName.StartsWith(this.GeometryColumn + "_Envelope_") && col.ColumnName != "sharpmap_tempgeometry")
								fdt.Columns.Add(col.ColumnName, col.DataType, col.Expression);
						if(ds.Tables[0].Rows.Count>0)
						{
							System.Data.DataRow dr = ds.Tables[0].Rows[0];
							SharpMap.Data.FeatureDataRow fdr = fdt.NewRow();
							foreach (System.Data.DataColumn col in ds.Tables[0].Columns)
								if (col.ColumnName != this.GeometryColumn && !col.ColumnName.StartsWith(this.GeometryColumn + "_Envelope_") && col.ColumnName != "sharpmap_tempgeometry")
									fdr[col.ColumnName] = dr[col];
							if (dr["sharpmap_tempgeometry"] != DBNull.Value)
								fdr.Geometry = SharpMap.Converters.WellKnownBinary.GeometryFromWKB.Parse((byte[])dr["sharpmap_tempgeometry"]);
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

        public virtual bool Contains(IFeature feature)
	    {
            throw new NotImplementedException();
	    }

        public virtual int IndexOf(IFeature feature)
        {
            throw new NotImplementedException();
        }

	    /// <summary>
		/// Boundingbox of dataset
		/// </summary>
		/// <returns>boundingbox</returns>
		public GeoAPI.Geometries.IEnvelope GetExtents()
		{
			using (SqlConnection conn = new SqlConnection(_ConnectionString))
			{
				string strSQL = string.Format("SELECT ST.AsBinary(ST.EnvelopeQueryWhere('{0}', '{1}', '{2}'))", this.Table, this.GeometryColumn, this.DefinitionQuery.Replace("'", "''"));
				using (SqlCommand command = new SqlCommand(strSQL, conn))
				{
					conn.Open();
					object result = command.ExecuteScalar();
					conn.Close();
					if (result == System.DBNull.Value)
						return null;
					GeoAPI.Geometries.IEnvelope bbox = SharpMap.Converters.WellKnownBinary.GeometryFromWKB.Parse((byte[])result).EnvelopeInternal;
					return bbox;
				}
			}
		}

	    #endregion

		#region IFeatureProvider Members

	    /// <summary>
	    /// Returns all features with the view box
	    /// </summary>
	    /// <param name="bbox">view box</param>
	    /// <param name="d"></param>
	    /// <param name="ds">FeatureDataSet to fill data into</param>
	    public IEnumerable<IFeature> GetFeatures(IEnvelope bbox)
		{
			//List<GeoAPI.Geometries.IGeometry> features = new List<GeoAPI.Geometries.IGeometry>();
			using (SqlConnection conn = new SqlConnection(_ConnectionString))
			{
				string strSQL = "SELECT " + this.FeatureColumns + ", ST.AsBinary(" + this.BuildGeometryExpression() + ") AS sharpmap_tempgeometry ";
				strSQL += "FROM ST.FilterQuery" + this.BuildSpatialQuerySuffix() + "(" + this.BuildEnvelope(bbox) + ")";

				if (!String.IsNullOrEmpty(this.DefinitionQuery))
					strSQL += " WHERE " + this.DefinitionQuery;

				if (!String.IsNullOrEmpty(this.OrderQuery))
					strSQL += " ORDER BY " + this.OrderQuery;

				using (SqlDataAdapter adapter = new SqlDataAdapter(strSQL, conn))
				{
					conn.Open();
					System.Data.DataSet ds2 = new System.Data.DataSet();
					adapter.Fill(ds2);
					conn.Close();
					if (ds2.Tables.Count > 0)
					{
						FeatureDataTable fdt = new FeatureDataTable(ds2.Tables[0]);
						foreach (System.Data.DataColumn col in ds2.Tables[0].Columns)
							if (col.ColumnName != this.GeometryColumn && !col.ColumnName.StartsWith(this.GeometryColumn + "_Envelope_") && col.ColumnName != "sharpmap_tempgeometry")
								fdt.Columns.Add(col.ColumnName,col.DataType,col.Expression);
						foreach (System.Data.DataRow dr in ds2.Tables[0].Rows)
						{
							SharpMap.Data.FeatureDataRow fdr = fdt.NewRow();
							foreach(System.Data.DataColumn col in ds2.Tables[0].Columns)
								if (col.ColumnName != this.GeometryColumn && !col.ColumnName.StartsWith(this.GeometryColumn + "_Envelope_") && col.ColumnName != "sharpmap_tempgeometry")
									fdr[col.ColumnName] = dr[col];
							if (dr["sharpmap_tempgeometry"] != DBNull.Value)
								fdr.Geometry = SharpMap.Converters.WellKnownBinary.GeometryFromWKB.Parse((byte[])dr["sharpmap_tempgeometry"]);
							fdt.AddRow(fdr);								
						}
						return fdt;
					}
				}
			}
		    return null;
		}
		#endregion

		private string BuildSpatialQuerySuffix()
		{
			string schema;
			string table = this.Table;
			int dotPosition = table.IndexOf('.');
			if (dotPosition == -1)
			{
				schema = "dbo";
			}
			else
			{
				schema = table.Substring(0, dotPosition);
				table = table.Substring(dotPosition + 1);
			}
			return "#" + schema + "#" + table + "#" + this.GeometryColumn;
		}

		private string BuildGeometryExpression()
		{
			return string.Format(this.GeometryExpression, this.TargetGeometryColumn);
		}

		private string BuildEnvelope(GeoAPI.Geometries.IEnvelope bbox)
		{
/*
			if (this.TargetSRID > 0 && !string.IsNullOrEmpty(SrsWkt).SrsWkt > 0 && this.SrsWkt != this.TargetSRID)
				return string.Format(SharpMap.Map.numberFormat_EnUS,
					"ST.Transform(ST.MakeEnvelope({0},{1},{2},{3},{4}),{5})",
						bbox.MinX,
						bbox.MinY,
						bbox.MaxX,
						bbox.MaxY,
						this.TargetSRID,
						this.SrsWkt);
			else
*/
				return string.Format(SharpMap.Map.numberFormat_EnUS, 
					"ST.MakeEnvelope({0},{1},{2},{3},{4})",
					bbox.MinX,
					bbox.MinY,
					bbox.MaxX,
					bbox.MaxY,
					this.SrsWkt);
		}
	}
}

