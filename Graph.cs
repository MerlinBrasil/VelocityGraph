using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VelocityDb;
using VelocityDb.Collection.BTree;
using VelocityDb.Session;

namespace VelocityGraph
{
  public enum Condition
  {
    Equal,
    GreaterEqual,
    GreaterThan,
    LessEqual,
    LessThan,
    NotEqual,
    Like,
    LikeNoCase,
    Between,
    RegExp
  }

  public enum DataType
  {
    Boolean,
    Integer,
    Long,
    Double,
    DateTime,
    String,
    Object,
    OID
  }

  public enum EdgesDirection
  {
    Ingoing,
    Outgoing,
    Any
  }

  public enum ObjectType
  {
    Node,
    Edge
  }

  public enum Order
  {
    Ascendent,
    Descendent
  }

  public enum PropertyKind
  {
    Basic,
    Indexed,
    Unique
  }

  public class Graph : OptimizedPersistable
  {
    Dictionary<string, NodeType> stringToNodeType;
    NodeType[] nodeType;
    Dictionary<string, EdgeType> stringToEdgeType;
    EdgeType[] edgeType;
    Dictionary<string, EdgeType> stringToRestrictedEdgeType;
    EdgeType[] restrictedEdgeType;
    int nodeOrEdgeTypeCt;
    [NonSerialized]
    SessionBase session;

    public Graph(SessionBase session)
    {
      nodeOrEdgeTypeCt = 0;
      stringToNodeType = new Dictionary<string, NodeType>();
      nodeType = new NodeType[0];
      stringToEdgeType = new Dictionary<string, EdgeType>();
      edgeType = new EdgeType[0];
      stringToRestrictedEdgeType = new Dictionary<string, EdgeType>();
      restrictedEdgeType = new EdgeType[0];
      this.session = session;
    }

    public override SessionBase Session
    {
      get
      {
        if (session != null)
          return session;
        return base.Session;
      }
    }

    /// <summary>
    /// Creates a new node type.
    /// </summary>
    /// <param name="name">Unique name for the new node type.</param>
    /// <returns>Unique graph type identifier.</returns>
    public int NewNodeType(string name)
    {
      NodeType aType;
      if (stringToNodeType.TryGetValue(name, out aType) == false)
      {
        int pos = nodeOrEdgeTypeCt;
        Update();
        Array.Resize(ref nodeType, (int) ++nodeOrEdgeTypeCt);
        aType = new NodeType(pos, name, Session);
        nodeType[pos] = aType;
        stringToNodeType.Add(name, aType);
      }
      return aType.TypeId;
    }

    /// <summary>
    /// Creates a new edge type.
    /// </summary>
    /// <param name="name">Unique name for the new edge type.</param>
    /// <param name="directed">If true, this creates a directed edge type, otherwise this creates a undirected edge type.</param>
    /// <param name="neighbors">If true, this indexes neighbor nodes, otherwise not.</param>
    /// <returns>Unique type identifier.</returns>
    public int NewEdgeType(string name, bool directed, bool neighbors)
    {
      EdgeType aType;
      if (stringToEdgeType.TryGetValue(name, out aType) == false)
      {
        int pos = nodeOrEdgeTypeCt;
        Update();
        Array.Resize(ref edgeType, ++nodeOrEdgeTypeCt);
        aType = new EdgeType(pos, name, directed, neighbors);
        edgeType[pos] = aType;
        stringToEdgeType.Add(name, aType);
      }
      return aType.TypeId;
    }

    /// <summary>
    /// Creates a new restricted edge type.
    /// </summary>
    /// <param name="name">Unique name for the new edge type.</param>
    /// <param name="tail">Tail node type identifier.</param>
    /// <param name="head">Head node type identifier.</param>
    /// <param name="neighbors">If true, this indexes neighbor nodes, otherwise not.</param>
    /// <returns>Unique type identifier.</returns>
    public int NewRestrictedEdgeType(string name, int tail, int head, bool neighbors)
    {
      EdgeType aType;
      if (stringToRestrictedEdgeType.TryGetValue(name, out aType) == false)
      {
        int pos = nodeOrEdgeTypeCt;
        Update();
        Array.Resize(ref restrictedEdgeType, ++nodeOrEdgeTypeCt);
        NodeType tailType = nodeType[tail];
        NodeType headType = nodeType[head];
        aType = new EdgeType(pos, name, tailType, headType, neighbors, true);
        restrictedEdgeType[pos] = aType;
        stringToRestrictedEdgeType.Add(name, aType);
      }
      return aType.TypeId;
    }

    /// <summary>
    /// Creates a new Property. 
    /// </summary>
    /// <param name="type">Node or edge type identifier.</param>
    /// <param name="name">Unique name for the new Property.</param>
    /// <param name="dt">Data type for the new Property.</param>
    /// <param name="kind">Property kind.</param>
    /// <returns>Unique Property identifier.</returns>
    public int NewProperty(int type, string name, DataType dt, PropertyKind kind)
    {
      NodeType aType = null;
      if (nodeType.Length > type)
        aType = nodeType[type];
      if (aType != null)
        return aType.NewProperty(name, dt, kind);
      EdgeType anEdgeType = edgeType[type];
      return anEdgeType.NewProperty(name, dt, kind);
    }

    public int NewProperty(int type, string name, ElementType dt, PropertyKind kind, object defaultValue)
    {
      throw new NotImplementedException();
    }

    public void SetProperty(long oid, int attr, object v)
    {
      UInt32 typeId = (UInt32) (oid >> 32);
      NodeType aType = null;
      if (nodeType.Length > typeId)
        aType = nodeType[(int) typeId];
      if (aType != null)
        aType.SetProperty((UInt32) oid, attr, v);
      else
      {
        EdgeType anEdgeType = edgeType[(int) typeId];
        anEdgeType.SetProperty((UInt32) oid, attr, v);
      }    
    }

    /// <summary>
    /// Creates a new node instance.
    /// </summary>
    /// <param name="type">Node type identifier.</param>
    /// <returns>Unique OID of the new node instance.</returns>
    public long NewNode(int type)
    {
      NodeType aType = nodeType[type];
      return (long) aType.NewNode();
    }

    /// <summary>
    /// Creates a new edge instance.
    /// </summary>
    /// <param name="type">Edge type identifier.</param>
    /// <param name="tail">Source OID.</param>
    /// <param name="head">Target OID. </param>
    /// <returns>Unique OID of the new edge instance.</returns>
    public long NewEdge(int type, long tail, long head)
    {
      ulong utail = (ulong)tail;
      UInt32 tailTypeId = (UInt32)(utail >> 32);
      NodeType tailType = nodeType[(int) tailTypeId];
      ulong uhead = (ulong)head;
      UInt32 headTypeId = (UInt32) (uhead >> 32);
      NodeType headType = nodeType[(int) headTypeId];
      EdgeType anEdgeType = null;
      if (edgeType.Length > type)
        anEdgeType = edgeType[type];
      if (anEdgeType != null)
        return anEdgeType.NewEdge(tail, tailType, head, headType, Session);
      anEdgeType = restrictedEdgeType[type];      

      return anEdgeType.NewEdge(tail, tailType, head, headType, Session);
    }

    /// <summary>
    /// Creates a new edge instance.
    /// The tail of the edge will be any node having the given tailV Value for the given tailAttr Property identifier,
    /// and the head of the edge will be any node having the given headV Value for the given headAttr Property identifier. 
    /// </summary>
    /// <param name="type">Node or edge type identifier.</param>
    /// <param name="tailAttr">Property identifier.</param>
    /// <param name="tailV">Tail value</param>
    /// <param name="headAttr">Property identifier.</param>
    /// <param name="headV">Head value</param>
    /// <returns>Unique OID of the new edge instance.</returns>
    public long NewEdge(int type, int tailAttr, object tailV, int headAttr, object headV)
    {
      EdgeType anEdgeType = null;
      if (edgeType.Length > type)
        anEdgeType = edgeType[type];
      if (anEdgeType != null)
        return anEdgeType.NewEdge(tailAttr, tailV, headAttr, headV, Session);
      anEdgeType = restrictedEdgeType[type];      
      return anEdgeType.NewEdge(tailAttr, tailV, headAttr, headV, Session);
    }

    /// <summary>
    /// Selects all neighbor nodes from or to the given node OID and for the given edge type.
    /// </summary>
    /// <param name="oid">Node OID</param>
    /// <param name="etype">Edge type identifier.</param>
    /// <param name="dir">Direction</param>
    /// <returns>Objects instance</returns>
    public Objects Neighbors(long oid, int etype, EdgesDirection dir)
    {
      UInt32 typeId = (UInt32) (oid >> 32);
      NodeType aNodeType = nodeType[(int) typeId];
      EdgeType anEdgeType = null;
      if (edgeType.Length > etype)
        anEdgeType = edgeType[etype];
      if (anEdgeType != null)
        return aNodeType.Neighbors((uint)oid, anEdgeType, dir);
      anEdgeType = restrictedEdgeType[etype];
      return aNodeType.Neighbors((uint)oid, anEdgeType, dir);
    }

    /// <summary>
    /// Selects all neighbor nodes from or to each of the node OID in the given collection and for the given edge type.
    /// </summary>
    /// <param name="objs">Node OID collection.</param>
    /// <param name="etype">Edge type identifier.</param>
    /// <param name="dir">Direction.</param>
    /// <returns>All neighbor nodes collection</returns>
    public Objects Neighbors(Objects objs, int etype, EdgesDirection dir)
    {
      Objects result = new Objects();
      foreach (long oid in objs)
      {
        Objects t = Neighbors(oid, etype, dir);
        result.Union(t);
      }
      return result;
    }

    /// <summary>
    /// Gets the Value for the given Property and OID.
    /// </summary>
    /// <param name="oid">OID</param>
    /// <param name="attr">Property type identifier.</param>
    /// <param name="v">Value for the given Property and for the given OID.</param>
    public object GetProperty(long oid, int attr)
    {
      UInt32 typeId = (UInt32)(oid >> 32);
      NodeType aNodeType = null;
      if (nodeType.Length > typeId)
        aNodeType = nodeType[(int)typeId];
      if (aNodeType != null)
      {
        return aNodeType.GetProperty((uint)oid, attr);
      }
      else
      {
        EdgeType anEdgeType = null;
        if (edgeType.Length > typeId)
          anEdgeType = edgeType[typeId];
        if (anEdgeType != null)
          return anEdgeType.GetProperty((uint)oid, attr);
        else
        {
          anEdgeType = restrictedEdgeType[typeId];
          return anEdgeType.GetProperty((uint)oid, attr);
        }
      }
    }

    public long CountNodes()
    {
      return nodeType.Length;
    }

    public long CountEdges()
    {
      return edgeType.Length + restrictedEdgeType.Length;
    }

    public EdgeData GetEdgeData(long edge)
    {
      throw new NotImplementedException();
    }

    public long GetEdgePeer(long edge, long node)
    {
      throw new NotImplementedException();
    }

    public void Drop(long oid)
    {
      throw new NotImplementedException();
    }

    public void Drop(Objects objs)
    {
      throw new NotImplementedException();
    }

    public int GetObjectType(long oid)
    {
      int typeId = (int) (oid >> 32);
      return typeId;
    }

    public int NewSessionProperty(int type, DataType dt, PropertyKind kind)
    {
      throw new NotImplementedException();
    }

    public int NewSessionProperty(int type, DataType dt, PropertyKind kind, object defaultValue)
    {
      throw new NotImplementedException();
    }

    public void SetPropertyDefaultValue(int attr, object v)
    {
      throw new NotImplementedException();
    }

    public void IndexProperty(int attr, PropertyKind kind)
    {
      throw new NotImplementedException();
    }

  /*  public PropertyStatistics GetPropertyStatistics(int attr, bool basic)
    {
      throw new NotImplementedException();
    }*/

    public long GetPropertyIntervalCount(int attr, object lower, bool includeLower, object higher, bool includeHigher)
    {
      throw new NotImplementedException();
    }

    public int FindType(string name)
    {
      throw new NotImplementedException();
    }

    public ElementType GetType(int type)
    {
      throw new NotImplementedException();
    }

    public void RemoveType(int type)
    {
      throw new NotImplementedException();
    }

    public int FindProperty(int type, string name)
    {
      throw new NotImplementedException();
    }

    public Property GetProperty(int attr)
    {
      throw new NotImplementedException();
    }

    public void RemoveProperty(int attr)
    {
      throw new NotImplementedException();
    }

    public long FindObject(int attr, object v)
    {
      throw new NotImplementedException();
    }

    public Objects Select(int type)
    {
      throw new NotImplementedException();
    }

    public Objects Select(int attr, Condition cond, object v)
    {
      throw new NotImplementedException();
    }

    public Objects Select(int attr, Condition cond, object lower, object higher)
    {
      throw new NotImplementedException();
    }

    public Objects Explode(long oid, int etype, EdgesDirection dir)
    {
      throw new NotImplementedException();
    }

    public Objects Explode(Objects objs, int etype, EdgesDirection dir)
    {
      throw new NotImplementedException();
    }

    public long Degree(long oid, int etype, EdgesDirection dir)
    {
      throw new NotImplementedException();
    }

    public Objects Edges(int etype, long tail, long head)
    {
      throw new NotImplementedException();
    }

    public long FindEdge(int etype, long tail, long head)
    {
      throw new NotImplementedException();
    }

    public Objects Tails(Objects edges)
    {
      throw new NotImplementedException();
    }

    public Objects Heads(Objects edges)
    {
      throw new NotImplementedException();
    }

    public void TailsAndHeads(Objects edges, Objects tails, Objects heads)
    {
      throw new NotImplementedException();
    }

    public ElementType[] FindNodeTypes()
    {
      throw new NotImplementedException();
    }

    public ElementType[] FindEdgeTypes()
    {
      throw new NotImplementedException();
    }

    public ElementType[] FindTypes()
    {
      throw new NotImplementedException();
    }

    public Property[] FindProperties(int type)
    {
      throw new NotImplementedException();
    }

    public Property[] GetProperties(long oid)
    {
      throw new NotImplementedException();
    }

    public object[] GetValues(int attr)
    {
      throw new NotImplementedException();
    }
  }
}
