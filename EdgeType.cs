using System;
using System.Linq;
using System.Collections.Generic;
using VelocityDb;
using VelocityDb.Session;
using Element = System.Int64;
using ElementId = System.Int32;
using PropertyTypeId = System.Int32;
using PropertyId = System.Int32;
using TypeId = System.Int32;

namespace VelocityGraph
{
  class EdgeType : OptimizedPersistable
  {
    string typeName;
    TypeId typeId;
    Dictionary<string, PropertyTypeBase> stringToPropertyType;
    bool directed;
    bool neighbors;
    UInt32 edgeCt;
    NodeType tailType;
    NodeType headType;

    public EdgeType(TypeId aTypeId, string aTypeName, bool directed, bool neighbors, bool restricted = false)
    {
      this.directed = directed;
      //   if (directed == false)
      //     edgeHeadToTail = new Dictionary<long, long>();
      //   edgeTailToHead = new Dictionary<long, long>();
      this.neighbors = neighbors;
      typeId = aTypeId;
      typeName = aTypeName;
      stringToPropertyType = new Dictionary<string, PropertyTypeBase>();
      edgeCt = 0;
      tailType = null;
      headType = null;
    }

    public EdgeType(TypeId aTypeId, string aTypeName, NodeType tail, NodeType head, bool directed, bool neighbors)
    {
      this.directed = directed;
      //   if (directed == false)
      //     edgeHeadToTail = new Dictionary<long, long>();
      //   edgeTailToHead = new Dictionary<long, long>();
      this.neighbors = neighbors;
      typeId = aTypeId;
      typeName = aTypeName;
      stringToPropertyType = new Dictionary<string, PropertyTypeBase>();
      edgeCt = 0;
      tailType = tail;
      headType = head;
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

    public Element NewEdge(Element tail, NodeType tailType, Element head, NodeType headType, SessionBase session)
    {
      Update();
      Element edgeId = (Element)typeId;
      edgeId <<= 32;
      edgeId += edgeCt++;
      tailType.NewTailToHeadEdge(this, (ElementId)tail, (ElementId)head, headType, session);
      if (directed == false)
        headType.NewHeadToTailEdge(this, (ElementId)tail, (ElementId)head, tailType, session);
      return edgeId;
    }

    public long NewEdge(PropertyTypeBase[] propertyType, PropertyId tailAttr, object tailV, PropertyId headAttr, object headV, SessionBase session)
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
