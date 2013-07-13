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
using Frontenac.Blueprints;
using Frontenac.Blueprints.Util;

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

  public class Graph : OptimizedPersistable, IGraph
  {
    BTreeMap<string, VertexType> stringToVertexType;
    internal VertexType[] vertexType;
    BTreeMap<string, EdgeType> stringToEdgeType;
    internal EdgeType[] edgeType;
    BTreeMap<string, EdgeType> stringToRestrictedEdgeType;
    internal EdgeType[] restrictedEdgeType;
    int nodeOrEdgeTypeCt;
    internal PropertyType[] propertyType;
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
      propertyType = new PropertyType[0];
      this.session = session;
    }

    public IEdge AddEdge(object id, IVertex outVertex, IVertex inVertex, string label)
    {
      throw new NotImplementedException();
    }

    public IVertex AddVertex(object id)
    {
      throw new NotImplementedException();
    }

    public IEdge GetEdge(object id)
    {
      throw new NotImplementedException();
    }

    /// <summary>
    /// Return an iterable to all the edges in the graph.
    /// </summary>
    /// <returns>an iterable reference to all edges in the graph</returns>
    public IEnumerable<IEdge> GetEdges()
    {
      List<IEnumerable<IEdge>> enums = new List<IEnumerable<IEdge>>();
      MultiIterable<IEdge> multi = new MultiIterable<IEdge>(enums);
      foreach (EdgeType et in edgeType)
      {
        enums.Add(et.GetEdges(this));
      }
      return multi;
    }

    public virtual IEnumerable<IEdge> GetEdges(string key, object value)
    {
      throw new NotImplementedException();
    }

    public Features GetFeatures()
    {
      throw new NotImplementedException();
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
    public PropertyType NewVertexProperty(VertexType vertexType, string name, DataType dt, PropertyKind kind)
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
    public PropertyType NewEdgeProperty(EdgeType edgeType, string name, DataType dt, PropertyKind kind)
    {
      return edgeType.NewProperty(ref propertyType, name, dt, kind);
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
    public IEdge NewEdge(EdgeType edgeType, PropertyType tailAttr, object tailV, PropertyType headAttr, object headV)
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
    public Dictionary<Vertex, HashSet<Edge>> Traverse(Dictionary<Vertex, HashSet<Edge>> vertices, EdgeType etype, Direction dir)
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
    public Vertex FindVertex(PropertyType property, object v)
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

    public void SetPropertyDefaultValue(PropertyType property, object v)
    {
      throw new NotImplementedException();
    }

    /// <summary>
    /// A shutdown function is required to properly close the graph.
    /// This is important for implementations that utilize disk-based serializations.
    /// </summary>
    void IGraph.Shutdown()
    {
      throw new NotImplementedException();
    }

    public long GetPropertyIntervalCount(PropertyType attr, object lower, bool includeLower, object higher, bool includeHigher)
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

    public PropertyType FindVertexProperty(VertexType vertexType, string name)
    {
      return vertexType.FindProperty(name);
    }

    /// <summary>
    /// Generate a query object that can be used to fine tune which edges/vertices are retrieved from the graph.
    /// </summary>
    /// <returns>a graph query object with methods for constraining which data is pulled from the underlying graph</returns>
    IGraphQuery IGraph.Query()
    {
      throw new NotImplementedException();
    }

    /// <summary>
    /// Remove the provided edge from the graph.
    /// </summary>
    /// <param name="edge">the edge to remove from the graph</param>
    void IGraph.RemoveEdge(IEdge edge)
    {
      throw new NotImplementedException();
    }

    public void RemoveProperty(PropertyType attr)
    {
      throw new NotImplementedException();
    }

    /// <summary>
    /// Remove the provided vertex from the graph.
    /// Upon removing the vertex, all the edges by which the vertex is connected must be removed as well.
    /// </summary>
    /// <param name="vertex">the vertex to remove from the graph</param>
    void IGraph.RemoveVertex(IVertex vertex)
    {
      throw new NotImplementedException();
    }

    public Dictionary<Vertex, HashSet<Edge>> Select(VertexType type)
    {
      throw new NotImplementedException();
    }

    public Dictionary<Vertex, HashSet<Edge>> Select(PropertyType attr, Condition cond, object v)
    {
      throw new NotImplementedException();
    }

    public Dictionary<Vertex, HashSet<Edge>> Select(PropertyType attr, Condition cond, object lower, object higher)
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
    public Dictionary<Vertex, HashSet<Edge>> GetVertices(Dictionary<Vertex, HashSet<Edge>> vertices, EdgeTypeId etype, Direction dir)
    {
      throw new NotImplementedException();
    }

    /// <summary>
    /// Return the vertex referenced by the provided object identifier.
    /// If no vertex is referenced by that identifier, then return null.
    /// </summary>
    /// <param name="id">the identifier of the vertex to retrieved from the graph</param>
    /// <returns>the vertex referenced by the provided identifier or null when no such vertex exists</returns>
    public IVertex GetVertex(object id)
    {
      long t = (long)id;
      VertexId vId = (VertexId) t;
      VertexTypeId vTypeId = (VertexTypeId)t >> 32;
      VertexType vt = vertexType[vTypeId];
      return vt.GetVertex(this, vId);
    }

    /// <summary>
    /// Return an iterable to all the vertices in the graph.
    /// </summary>
    /// <returns>an iterable reference to all vertices in the graph</returns>
    public IEnumerable<IVertex> GetVertices()
    {
      List<IEnumerable<IVertex>> enums = new List<IEnumerable<IVertex>>();
      MultiIterable<IVertex> multi = new MultiIterable<IVertex>(enums);
      foreach (VertexType vt in vertexType)
      {
        foreach (EdgeType et in vt.EdgeTypes)
          enums.Add(vt.GetVertices(this, et, Direction.Both));
      }
      return multi;
    }

    /// <summary>
    /// Return an iterable to all the vertices in the graph that have a particular key/value property.
    /// </summary>
    /// <param name="key">the key of vertex</param>
    /// <param name="value">the value of the vertex</param>
    /// <returns>an iterable of vertices with provided key and value</returns>
    IEnumerable<IVertex> IGraph.GetVertices(string key, object value)
    {
      foreach (IVertex iv in GetVertices())
      {
        Vertex v = iv as Vertex;
        PropertyType pt = v.VertexType.FindProperty(key);
        if (pt != null)
        {
          object aValue = v.VertexType.GetPropertyValue(v.VertexId, pt);
          if (aValue.Equals(value))
            yield return iv;
        }
      }
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
      return vertexType;
    }

    public EdgeType[] FindEdgeTypes()
    {
      return edgeType;
    }

    public object[] GetValues(PropertyType property)
    {
      throw new NotImplementedException();
    }
  }
}
