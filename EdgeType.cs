using System;
using System.Linq;
using System.Collections.Generic;
using VelocityDb;
using VelocityDb.Session;

namespace VelocityGraph
{
  class EdgeType : OptimizedPersistable
  {
    string typeName;
    UInt32 typeId;
    Dictionary<string, PropertyTypeBase> stringToPropertyType;
    PropertyTypeBase[] propertyType;
    bool directed;
    bool neighbors;
    UInt32 edgeCt;
    NodeType tailType;
    NodeType headType;

    public EdgeType(int aTypeId, string aTypeName, bool directed, bool neighbors, bool restricted = false)
    {
      this.directed = directed;
      //   if (directed == false)
      //     edgeHeadToTail = new Dictionary<long, long>();
      //   edgeTailToHead = new Dictionary<long, long>();
      this.neighbors = neighbors;
      typeId = (UInt32)aTypeId;
      typeName = aTypeName;
      stringToPropertyType = new Dictionary<string, PropertyTypeBase>();
      propertyType = new PropertyTypeBase[0];
      edgeCt = 0;
      tailType = null;
      headType = null;
    }

    public EdgeType(int aTypeId, string aTypeName, NodeType tail, NodeType head, bool directed, bool neighbors)
    {
      this.directed = directed;
      //   if (directed == false)
      //     edgeHeadToTail = new Dictionary<long, long>();
      //   edgeTailToHead = new Dictionary<long, long>();
      this.neighbors = neighbors;
      typeId = (UInt32)aTypeId;
      typeName = aTypeName;
      stringToPropertyType = new Dictionary<string, PropertyTypeBase>();
      propertyType = new PropertyTypeBase[0];
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
    public int NewProperty(string name, DataType dt, PropertyKind kind)
    {
      PropertyTypeBase aType;
      if (stringToPropertyType.TryGetValue(name, out aType) == false)
      {
        int pos = propertyType.Length;
        Array.Resize(ref propertyType, pos + 1);
        switch (dt)
        {
          case DataType.Boolean:
            aType = new PropertyType<bool>(pos, name, kind, Session);
            break;
          case DataType.Integer:
            aType = new PropertyType<int>(pos, name, kind, Session);
            break;
          case DataType.Long:
            aType = new PropertyType<long>(pos, name, kind, Session);
            break;
          case DataType.Double:
            aType = new PropertyType<double>(pos, name, kind, Session);
            break;
          case DataType.DateTime:
            aType = new PropertyType<DateTime>(pos, name, kind, Session);
            break;
          case DataType.String:
            aType = new PropertyType<string>(pos, name, kind, Session);
            break;
          case DataType.Object:
            aType = new PropertyType<object>(pos, name, kind, Session);
            break;
          case DataType.OID:
            aType = new PropertyType<long>(pos, name, kind, Session);
            break;
        }
        propertyType[pos] = aType;
        stringToPropertyType.Add(name, aType);
      }
      return aType.PropertyTypeId;
    }

    public long NewEdge(long tail, NodeType tailType, long head, NodeType headType, SessionBase session)
    {
      Update();
      UInt64 edgeId = typeId;
      edgeId <<= 32;
      edgeId += edgeCt++;
      tailType.NewTailToHeadEdge(this, (uint)tail, (uint)head, headType, session);
      if (directed == false)
        headType.NewHeadToTailEdge(this, (uint)tail, (uint)head, tailType, session);
      return (long)edgeId;
    }

    public long NewEdge(int tailAttr, object tailV, int headAttr, object headV, SessionBase session)
    {
      PropertyTypeBase tailPropertyType = propertyType[tailAttr];
      PropertyTypeBase headPropertyType = propertyType[headAttr];
      throw new NotImplementedException("don't yet know what it is supposed to do");
    }

    public int TypeId
    {
      get
      {
        return (int)typeId;
      }
    }

    public object GetProperty(UInt32 oid, int attr)
    {
      PropertyTypeBase anPropertyType = propertyType[attr];
      return anPropertyType.GetProperty(oid);
    }

    public void SetProperty(UInt32 oid, int attr, object v)
    {
      PropertyTypeBase anPropertyType = propertyType[attr];
      anPropertyType.SetProperty(oid, v);
    }
  }
}
