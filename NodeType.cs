using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VelocityDb;
using VelocityDb.Collection.BTree;
using VelocityDb.Session;

namespace VelocityGraph
{
  class NodeType : OptimizedPersistable
  {
    string typeName;
    UInt32 typeId;
    Dictionary<string, PropertyTypeBase> stringToPropertyType;
    Dictionary<EdgeType, BTreeMap<NodeType, BTreeMap<UInt32, BTreeSet<uint>>>> tailToHeadEdges;
    Dictionary<EdgeType, BTreeMap<NodeType, BTreeMap<UInt32, BTreeSet<uint>>>> headToTailEdges;
    PropertyTypeBase[] propertyType;
    UInt32 nodeCt;

    public NodeType(int aTypeId, string aTypeName, SessionBase session)
    {
      typeId = (UInt32)aTypeId;
      typeName = aTypeName;
      stringToPropertyType = new Dictionary<string, PropertyTypeBase>();
      propertyType = new PropertyTypeBase[0];
      tailToHeadEdges = new Dictionary<EdgeType, BTreeMap<NodeType, BTreeMap<uint, BTreeSet<uint>>>>();
      headToTailEdges = new Dictionary<EdgeType, BTreeMap<NodeType, BTreeMap<uint, BTreeSet<uint>>>>();
      nodeCt = 0;
    }

    public void NewTailToHeadEdge(EdgeType edgeType, uint tail, uint head, NodeType headType, SessionBase session)
    {
      BTreeMap<NodeType, BTreeMap<uint, BTreeSet<uint>>> map;
      BTreeMap<uint, BTreeSet<uint>> innerMap;
      BTreeSet<uint> set;
      if (!tailToHeadEdges.TryGetValue(edgeType, out map))
      {
        map = new BTreeMap<NodeType, BTreeMap<uint, BTreeSet<uint>>>(null, session);
        innerMap = new BTreeMap<uint, BTreeSet<uint>>(null, session);
        set = new BTreeSet<uint>(null, session);
        innerMap.Add(tail, set);
        map.Add(headType, innerMap);
        tailToHeadEdges.Add(edgeType, map);
      }
      else if (!map.TryGetValue(headType, out innerMap))
      {
        innerMap = new BTreeMap<uint, BTreeSet<uint>>(null, session);
        set = new BTreeSet<uint>(null, session);
        innerMap.Add(tail, set);
        map.Add(headType, innerMap);
      }
      else if (!innerMap.TryGetValue(tail, out set))
      {
        set = new BTreeSet<uint>(null, session);
        innerMap.Add(tail, set);
      }
      set.Add(head);
    }

    public void NewHeadToTailEdge(EdgeType edgeType, uint tail, uint head, NodeType tailType, SessionBase session)
    {
      BTreeMap<NodeType, BTreeMap<uint, BTreeSet<uint>>> map;
      BTreeMap<uint, BTreeSet<uint>> innerMap;
      BTreeSet<uint> set;
      if (!headToTailEdges.TryGetValue(edgeType, out map))
      {
        map = new BTreeMap<NodeType, BTreeMap<uint, BTreeSet<uint>>>(null, session);
        innerMap = new BTreeMap<uint, BTreeSet<uint>>(null, session);
        set = new BTreeSet<uint>(null, session);
        innerMap.Add(tail, set);
        map.Add(tailType, innerMap);
        headToTailEdges.Add(edgeType, map);
      }
      else if (!map.TryGetValue(tailType, out innerMap))
      {
        innerMap = new BTreeMap<uint, BTreeSet<uint>>(null, session);
        set = new BTreeSet<uint>(null, session);
        innerMap.Add(tail, set);
        map.Add(tailType, innerMap);
      }
      else if (!innerMap.TryGetValue(tail, out set))
      {
        set = new BTreeSet<uint>(null, session);
        innerMap.Add(tail, set);
      }
      set.Add(head);
    }

    public UInt64 NewNode()
    {
      Update();
      UInt64 nodeId = typeId;
      nodeId <<= 32;
      nodeId += nodeCt++;
      return nodeId;
    }

    public Objects Neighbors(UInt32 oid, EdgeType etype, EdgesDirection dir)
    {
      Objects result = new Objects();
      BTreeMap<NodeType, BTreeMap<uint, BTreeSet<uint>>> map;
      BTreeSet<uint> set;
      switch (dir)
      {
        case EdgesDirection.Outgoing:
          if (tailToHeadEdges.TryGetValue(etype, out map))
          {
            foreach (KeyValuePair<NodeType, BTreeMap<uint, BTreeSet<uint>>> pair in map)
            {
              BTreeMap<uint, BTreeSet<uint>> innerMap = pair.Value;
              if (innerMap.TryGetValue(oid, out set))
              {
                foreach (uint u in set)
                {
                  UInt64 otherId = (UInt64)pair.Key.TypeId;
                  otherId <<= 32;
                  otherId += u;
                  result.Add((long)otherId);
                }
              }
            }
          }
          break;
        case EdgesDirection.Ingoing:
          if (headToTailEdges.TryGetValue(etype, out map))
          {
            foreach (KeyValuePair<NodeType, BTreeMap<uint, BTreeSet<uint>>> pair in map)
            {
              BTreeMap<uint, BTreeSet<uint>> innerMap = pair.Value;
              if (innerMap.TryGetValue(oid, out set))
              {
                foreach (uint u in set)
                {
                  UInt64 otherId = (UInt64)pair.Key.TypeId;
                  otherId <<= 32;
                  otherId += u;
                  result.Add((long)otherId);
                }
              }
            }
          }
          break;
        case EdgesDirection.Any:
          if (tailToHeadEdges.TryGetValue(etype, out map))
          {
            foreach (KeyValuePair<NodeType, BTreeMap<uint, BTreeSet<uint>>> pair in map)
            {
              BTreeMap<uint, BTreeSet<uint>> innerMap = pair.Value;
              if (innerMap.TryGetValue(oid, out set))
              {
                foreach (uint u in set)
                {
                  UInt64 otherId = (UInt64)pair.Key.TypeId;
                  otherId <<= 32;
                  otherId += u;
                  result.Add((long)otherId);
                }
              }
            }
          }
          if (headToTailEdges.TryGetValue(etype, out map))
          {
            foreach (KeyValuePair<NodeType, BTreeMap<uint, BTreeSet<uint>>> pair in map)
            {
              BTreeMap<uint, BTreeSet<uint>> innerMap = pair.Value;
              if (innerMap.TryGetValue(oid, out set))
              {
                foreach (uint u in set)
                {
                  UInt64 otherId = (UInt64)pair.Key.TypeId;
                  otherId <<= 32;
                  otherId += u;
                  result.Add((long)otherId);
                }
              }
            }
          }
          break;
      }
      return result;
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

    public int TypeId
    {
      get
      {
        return (int)typeId;
      }
    }

    public object GetProperty(uint oid, int attr)
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
