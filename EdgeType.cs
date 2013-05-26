﻿using System;
using System.Linq;
using System.Collections.Generic;
using VelocityDb;
using VelocityDb.Session;
using ElementId = System.Int32;
using PropertyTypeId = System.Int32;
using PropertyId = System.Int32;
using TypeId = System.Int32;
using VelocityDb.Collection.BTree;

namespace VelocityGraph
{
  class EdgeType : OptimizedPersistable, IComparable<EdgeType>, IEqualityComparer<EdgeType>
  {
    string typeName;
    TypeId typeId;
    BTreeMap<string, PropertyTypeBase> stringToPropertyType;
    bool directed;
    bool neighbors;
    ElementId edgeCt;
    VertexType tailType;
    VertexType headType;

    public EdgeType(TypeId aTypeId, string aTypeName, bool directed, bool neighbors, SessionBase session, bool restricted = false)
    {
      this.directed = directed;
      //   if (directed == false)
      //     edgeHeadToTail = new Dictionary<long, long>();
      //   edgeTailToHead = new Dictionary<long, long>();
      this.neighbors = neighbors;
      typeId = aTypeId;
      typeName = aTypeName;
      stringToPropertyType = new BTreeMap<string, PropertyTypeBase>(null, session);
      edgeCt = 0;
      tailType = null;
      headType = null;
    }

    public EdgeType(TypeId aTypeId, string aTypeName, VertexType tail, VertexType head, bool directed, bool neighbors, SessionBase session)
    {
      this.directed = directed;
      //   if (directed == false)
      //     edgeHeadToTail = new Dictionary<long, long>();
      //   edgeTailToHead = new Dictionary<long, long>();
      this.neighbors = neighbors;
      typeId = aTypeId;
      typeName = aTypeName;
      stringToPropertyType = new BTreeMap<string, PropertyTypeBase>(null, session);
      edgeCt = 0;
      tailType = tail;
      headType = head;
    }

    /// <summary>
    /// Compares two EdgeType objects by id
    /// </summary>
    /// <param name="obj">The object to compare with</param>
    /// <returns>a negative number if less, 0 if equal or else a positive number</returns>
    public int CompareTo(EdgeType obj)
    {
      return typeId.CompareTo(obj.typeId);
    }

    public static int Compare(EdgeType aId, EdgeType bId)
    {
      return aId.typeId.CompareTo(bId.typeId);
    }

    public bool Equals(EdgeType x, EdgeType y)
    {
      return Compare(x, y) == 0;
    }

    public int GetHashCode(EdgeType aIssue)
    {
      return typeId.GetHashCode();
    }

    /// <summary>
    /// Creates a new Property. 
    /// </summary>
    /// <param name="name">Unique name for the new Property.</param>
    /// <param name="dt">Data type for the new Property.</param>
    /// <param name="kind">Property kind.</param>
    /// <returns>Unique Property identifier.</returns>
    public PropertyId NewProperty(ref PropertyTypeBase[] propertyType, string name, DataType dt, PropertyKind kind)
    {
      PropertyTypeBase aType;
      if (stringToPropertyType.TryGetValue(name, out aType) == false)
      {
        int pos = propertyType.Length;
        Array.Resize(ref propertyType, pos + 1);
        switch (dt)
        {
          case DataType.Boolean:
            aType = new PropertyType<bool>(this.TypeId, pos, name, kind, Session);
            break;
          case DataType.Integer:
            aType = new PropertyType<int>(this.TypeId,pos, name, kind, Session);
            break;
          case DataType.Long:
            aType = new PropertyType<long>(this.TypeId,pos, name, kind, Session);
            break;
          case DataType.Double:
            aType = new PropertyType<double>(this.TypeId,pos, name, kind, Session);
            break;
          case DataType.DateTime:
            aType = new PropertyType<DateTime>(this.TypeId,pos, name, kind, Session);
            break;
          case DataType.String:
            aType = new PropertyType<string>(this.TypeId,pos, name, kind, Session);
            break;
          case DataType.Object:
            aType = new PropertyType<object>(this.TypeId,pos, name, kind, Session);
            break;
          case DataType.OID:
            aType = new PropertyType<long>(this.TypeId,pos, name, kind, Session);
            break;
        }
        propertyType[pos] = aType;
        stringToPropertyType.Add(name, aType);
      }
      return aType.PropertyId;
    }

    public Edge NewEdge(Vertex tail, VertexType tailType, Vertex head, VertexType headType, SessionBase session)
    {
      Update();
      Edge edge = new Edge(typeId, edgeCt++);
      tailType.NewTailToHeadEdge(this, edge, tail.VertexId, head.VertexId, headType, session);
      if (directed == false)
        headType.NewHeadToTailEdge(this, edge, tail.VertexId, head.VertexId, tailType, session);
      return edge;
    }

    public Edge NewEdgeX(PropertyTypeBase[] propertyType, PropertyId tailAttr, object tailV, PropertyId headAttr, object headV, SessionBase session)
    {
      PropertyTypeBase tailPropertyType = propertyType[tailAttr];
      PropertyTypeBase headPropertyType = propertyType[headAttr];
      throw new NotImplementedException("don't yet know what it is supposed to do");
    }

    public TypeId TypeId
    {
      get
      {
        return typeId;
      }
    }

    public PropertyId FindProperty(string name)
    {
      PropertyTypeBase anPropertyType;
      if (stringToPropertyType.TryGetValue(name, out anPropertyType))
      {
        return anPropertyType.PropertyId;
      }
      return -1;
    }

    public object GetPropertyValue(PropertyTypeBase[] propertyType, ElementId elementId, PropertyId property)
    {
      PropertyTypeBase anPropertyType = propertyType[property];
      return anPropertyType.GetPropertyValue(elementId);
    }

    public void SetPropertyValue(PropertyTypeBase[] propertyType, ElementId elementId, PropertyId property, object v)
    {
      PropertyTypeBase anPropertyType = propertyType[property];
      anPropertyType.SetPropertyValue(elementId, v);
    }
  }
}
