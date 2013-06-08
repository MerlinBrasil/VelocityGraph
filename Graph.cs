using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VelocityDb;
using VelocityDb.Collection.BTree;
using VelocityDb.Session;
using VertexId = System.Int32;
using EdgeId = System.Int32;
using PropertyTypeId = System.Int32;
using PropertyId = System.Int32;
using VertexTypeId = System.Int32;
using EdgeTypeId = System.Int32;

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

  public enum Order
  {
    Ascendent,
    Descendent
  }

  public enum PropertyKind
  {
    Indexed,
    Unique
  }

  public class Graph : OptimizedPersistable
  {
    BTreeMap<string, VertexType> stringToVertexType;
    internal VertexType[] vertexType;
    BTreeMap<string, EdgeType> stringToEdgeType;
    internal EdgeType[] edgeType;
    BTreeMap<string, EdgeType> stringToRestrictedEdgeType;
    internal EdgeType[] restrictedEdgeType;
    int nodeOrEdgeTypeCt;
    internal PropertyTypeBase[] propertyType;
    [NonSerialized]
    SessionBase session;

    public Graph(SessionBase session)
    {
      nodeOrEdgeTypeCt = 0;
      stringToVertexType = new BTreeMap<string, VertexType>(null, session);
      vertexType = new VertexType[0];
      stringToEdgeType = new BTreeMap<string, EdgeType>(null, session);
      edgeType = new EdgeType[0];
      stringToRestrictedEdgeType = new BTreeMap<string, EdgeType>(null, session);
      restrictedEdgeType = new EdgeType[0];
      propertyType = new PropertyTypeBase[0];
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

    public static Dictionary<Vertex, HashSet<Edge>> CombineIntersection(Dictionary<Vertex, HashSet<Edge>> objs1, Dictionary<Vertex, HashSet<Edge>> objs2)
    {
      Dictionary<Vertex, HashSet<Edge>> clone = new Dictionary<Vertex, HashSet<Edge>>();
      foreach (KeyValuePair<Vertex, HashSet<Edge>> p in objs1)
      {
        HashSet<Edge> edges;
        if (objs2.TryGetValue(p.Key, out edges))
        {
          HashSet<Edge> edgesAll = new HashSet<Edge>(edges);
          edgesAll.UnionWith(p.Value);
          clone.Add(p.Key, edgesAll);
        }
      }
      return clone;
    }

    public static HashSet<Edge> CombineIntersection(HashSet<Edge> objs1, HashSet<Edge> objs2)
    {
      HashSet<Edge> clone = new HashSet<Edge>(objs1);
      clone.IntersectWith(objs2);
      return clone;
    }

    /// <summary>
    /// Creates a new node type.
    /// </summary>
    /// <param name="name">Unique name for the new vertex type.</param>
    /// <returns>Unique graph type identifier.</returns>
    public VertexType NewVertexType(string name)
    {
      VertexType aType;
      if (stringToVertexType.TryGetValue(name, out aType) == false)
      {
        int pos = nodeOrEdgeTypeCt;
        Update();
        Array.Resize(ref vertexType, (int)++nodeOrEdgeTypeCt);
        aType = new VertexType(pos, name, Session);
        vertexType[pos] = aType;
        stringToVertexType.Add(name, aType);
      }
      return aType;
    }

    /// <summary>
    /// Creates a new edge type.
    /// </summary>
    /// <param name="name">Unique name for the new edge type.</param>
    /// <param name="directed">If true, this creates a directed edge type, otherwise this creates a undirected edge type.</param>
    /// <returns>Unique edge type.</returns>
    public EdgeType NewEdgeType(string name, bool directed)
    {
      EdgeType aType;
      if (stringToEdgeType.TryGetValue(name, out aType) == false)
      {
        int pos = nodeOrEdgeTypeCt;
        Update();
        Array.Resize(ref edgeType, ++nodeOrEdgeTypeCt);
        aType = new EdgeType(pos, name, directed, Session);
        edgeType[pos] = aType;
        stringToEdgeType.Add(name, aType);
      }
      return aType;
    }

    /// <summary>
    /// Creates a new restricted edge type.
    /// </summary>
    /// <param name="name">Unique name for the new edge type.</param>
    /// <param name="tail">Tail node type identifier.</param>
    /// <param name="head">Head node type identifier.</param>
    /// <returns>Unique edge type.</returns>
    public EdgeType NewRestrictedEdgeType(string name, VertexType tail, VertexType head)
    {
      EdgeType aType;
      if (stringToRestrictedEdgeType.TryGetValue(name, out aType) == false)
      {
        int pos = nodeOrEdgeTypeCt;
        Update();
        Array.Resize(ref restrictedEdgeType, ++nodeOrEdgeTypeCt);
        aType = new EdgeType(pos, name, tail, head, true, Session);
        restrictedEdgeType[pos] = aType;
        stringToRestrictedEdgeType.Add(name, aType);
      }
      return aType;
    }

    /// <summary>
    /// Creates a new Property. 
    /// </summary>
    /// <param name="type">Node or edge type identifier.</param>
    /// <param name="name">Unique name for the new Property.</param>
    /// <param name="dt">Data type for the new Property.</param>
    /// <param name="kind">Property kind.</param>
    /// <returns>Unique Property identifier.</returns>
    public PropertyTypeBase NewVertexProperty(VertexType vertexType, string name, DataType dt, PropertyKind kind)
    {
      return vertexType.NewProperty(ref propertyType, name, dt, kind);
    }

    /// <summary>
    /// Creates a new Property. 
    /// </summary>
    /// <param name="type">Node or edge type identifier.</param>
    /// <param name="name">Unique name for the new Property.</param>
    /// <param name="dt">Data type for the new Property.</param>
    /// <param name="kind">Property kind.</param>
    /// <returns>Unique Property identifier.</returns>
    public PropertyTypeBase NewEdgeProperty(EdgeType edgeType, string name, DataType dt, PropertyKind kind)
    {
      return edgeType.NewProperty(ref propertyType, name, dt, kind);
    }

    public PropertyId NewProperty(VertexType type, string name, ElementType dt, PropertyKind kind, object defaultValue)
    {
      throw new NotImplementedException();
    }

    /// <summary>
    /// Creates a new node instance.
    /// </summary>
    /// <param name="type">Node type identifier.</param>
    /// <returns>Unique OID of the new node instance.</returns>
    public Vertex NewVertex(VertexType vertexType)
    {
      return vertexType.NewVertex(this);
    }

    /// <summary>
    /// Creates a new edge instance.
    /// </summary>
    /// <param name="type">Edge type identifier.</param>
    /// <param name="tail">Source OID.</param>
    /// <param name="head">Target OID. </param>
    /// <returns>Unique OID of the new edge instance.</returns>
    public Edge NewEdge(EdgeType edgeType, Vertex tail, Vertex head)
    {
      return edgeType.NewEdge(this, tail, tail.VertexType, head, head.VertexType, Session);
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
    /// <returns>Unique edge instance.</returns>
    public Edge NewEdge(EdgeType edgeType, PropertyTypeBase tailAttr, object tailV, PropertyTypeBase headAttr, object headV)
    {
      return edgeType.NewEdgeX(propertyType, tailAttr, tailV, headAttr, headV, Session);
    }

    /// <summary>
    /// Selects all neighbor Vertices from or to each of the node OID in the given collection and for the given edge type.
    /// </summary>
    /// <param name="Vertices">Vertex collection.</param>
    /// <param name="etype">Edge type identifier.</param>
    /// <param name="dir">Direction.</param>
    /// <returns>Dictionary of vertex keys with edges path to vertex</returns>
    public Dictionary<Vertex, HashSet<Edge>> Traverse(Dictionary<Vertex, HashSet<Edge>> vertices, EdgeType etype, EdgesDirection dir)
    {
      Dictionary<Vertex, HashSet<Edge>> result = new Dictionary<Vertex, HashSet<Edge>>();
      foreach (KeyValuePair<Vertex, HashSet<Edge>> p in vertices)
      {
        Dictionary<Vertex, HashSet<Edge>> t = p.Key.Traverse(etype, dir);
        foreach (KeyValuePair<Vertex, HashSet<Edge>> p2 in t)
        {
          HashSet<Edge> edges;
          if (result.TryGetValue(p2.Key, out edges))
            edges.UnionWith(p2.Value);
          else
            result[p2.Key] = p2.Value;
        }
      }
      return result;
    }

    /// <summary>
    /// Finds vertex having the given value for the given property. 
    /// </summary>
    /// <param name="property"></param>
    /// <param name="v"></param>
    /// <returns>the vertex matching</returns>
    public Vertex? FindVertex(PropertyTypeBase property, object v)
    {
      return property.GetPropertyVertex(v, this); ;
    }

    public long CountVertices()
    {
      return vertexType.Length;
    }

    public long CountEdges()
    {
      return edgeType.Length + restrictedEdgeType.Length;
    }

    public void Drop(Dictionary<Vertex, HashSet<Edge>> objs)
    {
      throw new NotImplementedException();
    }

    public void SetPropertyDefaultValue(PropertyTypeBase property, object v)
    {
      throw new NotImplementedException();
    }

    public long GetPropertyIntervalCount(PropertyTypeBase attr, object lower, bool includeLower, object higher, bool includeHigher)
    {
      throw new NotImplementedException();
    }

    /// <summary>
    /// Finds the type id associated with a particular edge type. Lookup by name.
    /// </summary>
    /// <param name="name">The name of the edge/node type being looked up</param>
    /// <returns>A node/edge type id or -1 if not found.</returns>
    public EdgeTypeId FindEdgeType(string name)
    {
      EdgeType eType;
      if (stringToRestrictedEdgeType.TryGetValue(name, out eType))
        return eType.TypeId;
      if (stringToEdgeType.TryGetValue(name, out eType))
        return eType.TypeId;
      return -1;
    }

    /// <summary>
    /// Finds the type id associated with a particular vertexe type. Lookup by name.
    /// </summary>
    /// <param name="name">The name of the edge/node type being looked up</param>
    /// <returns>A node/edge type id or -1 if not found.</returns>
    public VertexType FindVertexType(string name)
    {
      VertexType nType;
      if (stringToVertexType.TryGetValue(name, out nType))
        return nType;
      return null;
    }

    public void RemoveVertexType(VertexTypeId type)
    {
      throw new NotImplementedException();
    }

    public PropertyTypeBase FindVertexProperty(VertexType vertexType, string name)
    {
      return vertexType.FindProperty(name);
    }

    public void RemoveProperty(PropertyTypeBase attr)
    {
      throw new NotImplementedException();
    }

    public Dictionary<Vertex, HashSet<Edge>> Select(VertexType type)
    {
      throw new NotImplementedException();
    }

    public Dictionary<Vertex, HashSet<Edge>> Select(PropertyTypeBase attr, Condition cond, object v)
    {
      throw new NotImplementedException();
    }

    public Dictionary<Vertex, HashSet<Edge>> Select(PropertyTypeBase attr, Condition cond, object lower, object higher)
    {
      throw new NotImplementedException();
    }

    /// <summary>
    /// Selects all edges from or to each of the vertex ids in the given collection and for the given edge type. 
    /// </summary>
    /// <param name="Vertices">a set of Vertices</param>
    /// <param name="etype">the id of an EdgeType</param>
    /// <param name="dir">direction, one of: Ingoing, Outgoing, Any</param>
    /// <returns>a set of Vertex</returns>
    public Dictionary<Vertex, HashSet<Edge>> GetVertices(Dictionary<Vertex, HashSet<Edge>> vertices, EdgeTypeId etype, EdgesDirection dir)
    {
      throw new NotImplementedException();
    }

    public HashSet<Edge> Edges(EdgeTypeId etype, Vertex tail, Vertex head)
    {
      throw new NotImplementedException();
    }

    public Dictionary<Vertex, HashSet<Edge>> Tails(Dictionary<Vertex, HashSet<Edge>> edges)
    {
      throw new NotImplementedException();
    }

    public Dictionary<Vertex, HashSet<Edge>> Heads(Dictionary<Vertex, HashSet<Edge>> edges)
    {
      throw new NotImplementedException();
    }

    public void TailsAndHeads(HashSet<Edge> edges, Dictionary<Vertex, HashSet<Edge>> tails, Dictionary<Vertex, HashSet<Edge>> heads)
    {
      throw new NotImplementedException();
    }

    public VertexType[] FindVertexTypes()
    {
      throw new NotImplementedException();
    }

    public EdgeType[] FindEdgeTypes()
    {
      throw new NotImplementedException();
    }

    public object[] GetValues(PropertyTypeBase property)
    {
      throw new NotImplementedException();
    }
  }
}
