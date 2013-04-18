﻿using System;
using System.Data;
using DelftTools.Utils.Data;
using GeoAPI.Extensions.Feature;
using GeoAPI.Geometries;

namespace SharpMap.Data
{
    /// <summary>
    /// Represents a row of data in a FeatureDataTable.
    /// </summary>
    // [System.Diagnostics.DebuggerStepThrough()]
    [Serializable()]
    public class FeatureDataRow : DataRow, IFeature, IComparable
    {
        //private FeatureDataTable tableFeatureTable;

        internal FeatureDataRow(DataRowBuilder rb) : base(rb)
        {
        }
        private GeoAPI.Geometries.IGeometry geometry;
        private IFeatureAttributeCollection attributes;

        public virtual long Id { get; set; }

        public virtual Type GetEntityType()
        {
            return GetType();
        }

        /// <summary>
        /// The geometry of the current feature
        /// </summary>
        public GeoAPI.Geometries.IGeometry Geometry
        {
            get { return geometry; }
            set 
            {
                IGeometry oldGeometry = geometry;
                geometry = value;
                FeatureDataRowChangeEventArgs e = new FeatureDataRowChangeEventArgs(this, DataRowAction.Change);
                e.OldGeometry = oldGeometry;
                ((FeatureDataTable)Table).OnFeatureDataRowGeometryChanged(e);
            }
        }

        public IFeatureAttributeCollection Attributes
        {
            get { return attributes; }
            set { attributes = value; }
        }

        /// <summary>
        /// Returns true of the geometry is null
        /// </summary>
        /// <returns></returns>
        public bool IsFeatureGeometryNull()
        {
            return this.Geometry == null;
        }

        /// <summary>
        /// Sets the geometry column to null
        /// </summary>
        public void SetFeatureGeometryNull()
        {
            this.Geometry = null;
        }

        public int CompareTo(object obj)
        {
            FeatureDataRow row = (FeatureDataRow) obj;

            return Table.Rows.IndexOf(this).CompareTo(Table.Rows.IndexOf(row));
        }

        public object Clone()
        {
            throw new NotImplementedException();
        }
    }
}