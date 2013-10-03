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
using EdgeIdVertexId = System.UInt64;
using Frontenac.Blueprints;
using Frontenac.Blueprints.Util;
using System.Globalization;

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
    Single,
    DateTime,
    String,
    Object
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

  [Serializable]
  public class Graph : OptimizedPersistable, IGraph
  {
    BTreeMap<string, VertexType> stringToVertexType;
    internal VertexType[] vertexType;
    BTreeMap<string, EdgeType> stringToEdgeType;
    internal EdgeType[] edgeType;
    //BTreeMap<string, EdgeType> stringToRestrictedEdgeType;
    //internal EdgeType[] restrictedEdgeType;
    int vertexTypeCt;
    int edgeTypeCt;
    internal PropertyType[] propertyType;
    [NonSerialized]
    SessionBase session;
    static readonly Features features = new Features();

    static Graph()
    {
      features.SupportsDuplicateEdges = true;
      features.SupportsSelfLoops = true;
      features.SupportsSerializableObjectProperty = true;
      features.SupportsBooleanProperty = true;
      features.SupportsDoubleProperty = true;
      features.SupportsFloatProperty = true;
      features.SupportsIntegerProperty = true;
      features.SupportsPrimitiveArrayProperty = true;
      features.SupportsUniformListProperty = true;
      features.SupportsMixedListProperty = true;
      features.SupportsLongProperty = true;
      features.SupportsMapProperty = true;
      features.SupportsStringProperty = true;

      features.IgnoresSuppliedIds = true;
      features.IsPersistent = true;
      features.IsRdfModel = false;
      features.IsWrapper = false;

      features.SupportsIndices = false;
      features.SupportsKeyIndices = false;
      features.SupportsVertexKeyIndex = false;
      features.SupportsEdgeKeyIndex = false;
      features.SupportsVertexIndex = false;
      features.SupportsEdgeIndex = false;
      features.SupportsTransactions = true;
      features.SupportsVertexIteration = true;
      features.SupportsEdgeIteration = true;
      features.SupportsEdgeRetrieval = true;
      features.SupportsVertexProperties = true;
      features.SupportsEdgeProperties = true;
      features.SupportsThreadedTransactions = true;
    }

    public Graph(SessionBase session)
    {
      edgeTypeCt = 0;
      vertexTypeCt = 0;
      stringToVertexType = new BTreeMap<string, VertexType>(null, session);
      vertexType = new VertexType[0];
      stringToEdgeType = new BTreeMap<string, EdgeType>(null, session);
      edgeType = new EdgeType[0];
      //stringToRestrictedEdgeType = new BTreeMap<string, EdgeType>(null, session);
      //restrictedEdgeType = new EdgeType[0];
      propertyType = new PropertyType[0];
      this.session = session;
      NewVertexType("default");
      NewEdgeType("default", true); // not sure if we need "directed" or not as edge type parameter ???
    }

  /*  public void Dispose()
    {
      // not sure what can be done here, Session may be active with something else
      if (session != null)
      {
        session.Commit();
        session.Dispose(); // this is not safe to do but in order for tests to pass as as, this is required for now
        session = null;
      }
      GC.SuppressFinalize(this);
    }*/

    public static Graph Open(SessionBase session, int graphInstance = 0)
    {
      UInt32 dbNum = session.DatabaseNumberOf(typeof(Graph));
      Database db = session.OpenDatabase(dbNum, true, false);
      if (db != null)
      {
        int ct = 0;
        foreach (Graph g in db.AllObjects<Graph>())
        {
          if (ct == graphInstance)
            return g;
        }
      }
      return null;
    }

    /// <summary>
    /// Add an edge to the graph. The added edges requires a recommended identifier, a tail vertex, an head vertex, and a label.
    /// Like adding a vertex, the provided object identifier may be ignored by the implementation.
    /// </summary>
    /// <param name="id">the recommended object identifier</param>
    /// <param name="outVertex">the vertex on the tail of the edge</param>
    /// <param name="inVertex">the vertex on the head of the edge</param>
    /// <param name="label">the label associated with the edge</param>
    /// <returns>the newly created edge</returns>
    public virtual IEdge AddEdge(object id, IVertex outVertex, IVertex inVertex, string label)
    {
      EdgeType et;
      if (label != null && label.Length > 0)
      {
        EdgeTypeId etId = FindEdgeType(label);
        if (etId < 0)
          et = NewEdgeType(label, true);
        else
          et = edgeType[etId];
      }
      else if (id is UInt64)
      {
        UInt64 fullId = (UInt64)id;
        EdgeTypeId edgeTypeId = (EdgeTypeId)(fullId >> 32);
        et = edgeType[edgeTypeId];
      }
      else
        et = edgeType[0];
      Vertex tail = outVertex as Vertex;
      Vertex head = inVertex as Vertex;
      return et.NewEdge(this, tail, head, Session);
    }

    /// <summary>
    /// Create a new vertex (of VertexType "default"), add it to the graph, and return the newly created vertex.
    /// </summary>
    /// <param name="id">the recommended object identifier</param>
    /// <returns>the newly created vertex</returns>
    public virtual IVertex AddVertex(object id)
    {
      VertexType vt;
      if (id != null && id is UInt64)
      {
        UInt64 fullId = (UInt64)id;
        VertexTypeId vertexTypeId = (VertexTypeId)(fullId >> 32);
        vt = vertexType[vertexTypeId];
      }
      else
        vt = vertexType[0];
      return NewVertex(vt);
    }

    /// <summary>
    /// Return the edge referenced by the provided object identifier.
    /// If no edge is referenced by that identifier, then return null.
    /// </summary>
    /// <param name="id">the identifier of the edge to retrieved from the graph</param>
    /// <returns>the edge referenced by the provided identifier or null when no such edge exists</returns>
    public IEdge GetEdge(object id)
    {
      if (id == null)
        throw new ArgumentException("id may not be null, it should be a UInt64");
      if (id is UInt64)
      {
        UInt64 fullId = (UInt64)id;
        EdgeTypeId edgeTypeId = (EdgeTypeId)(fullId >> 32);
        EdgeType et = edgeType[edgeTypeId];
        EdgeTypeId edgeId = (EdgeTypeId)fullId;
        Edge edge = et.GetEdge(this, edgeId);
        return edge;
      }
      return null;
    }

    /// <summary>
    /// Return an iterable to all the edges in the graph.
    /// </summary>
    /// <returns>an iterable reference to all edges in the graph</returns>
    public IEnumerable<IEdge> GetEdges()
    {
      foreach (EdgeType et in edgeType)
        foreach (IEdge edge in et.GetEdges(this))
          yield return edge;
    }

    /// <summary>
    /// Return an iterable to all the edges in the graph that have a particular key/value property.
    /// </summary>
    /// <param name="key">the key of the edge</param>
    /// <param name="value">the value of the edge</param>
    /// <returns>an iterable of edges with provided key and value</returns>
    public virtual IEnumerable<IEdge> GetEdges(string key, object value)
    {
      switch (Type.GetTypeCode(value.GetType()))
      {
        case TypeCode.Boolean:
          return GetEdges<bool>(key, (bool)value);
        case TypeCode.Single:
          return GetEdges<float>(key, (float)value);
        case TypeCode.Double:
          return GetEdges<double>(key, (double)value);
        default:
          return GetEdges<object>(key, value);
      };
    }

    /// <summary>
    /// Return an iterable to all the edges in the graph that have a particular key/value property.
    /// </summary>
    /// <param name="key">the key of the edge</param>
    /// <param name="value">the value of the edge</param>
    /// <returns>an iterable of edges with provided key and value</returns>
    public virtual IEnumerable<IEdge> GetEdges<T>(string key, T value)
    {
      foreach (EdgeType et in edgeType)
      {
        PropertyType pt = et.FindProperty(key);
        if (pt != null)
          foreach (IEdge edge in pt.GetPropertyEdges(value, this))
            yield return edge;
      }
    }

    public virtual Features Features
    {
      get
      {
        return features;
      }
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

    public void Clear()
    {
      foreach (Edge edge in GetEdges())
      {
        edge.Remove();
      }
      foreach (Vertex vertex in GetVertices())
      {
        vertex.Remove();
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
        int pos = vertexTypeCt;
        Update();
        Array.Resize(ref vertexType, (int)++vertexTypeCt);
        aType = new VertexType(pos, name, this);
        if (IsPersistent)
          Session.Persist(aType);
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
    /// <returns>a new edge type</returns>
    public EdgeType NewEdgeType(string name, bool directed)
    {
      EdgeType aType;
      if (stringToEdgeType.TryGetValue(name, out aType) == false)
      {
        int pos = edgeTypeCt;
        Update();
        Array.Resize(ref edgeType, ++edgeTypeCt);
        aType = new EdgeType(pos, name, null, null, directed, this);
        if (IsPersistent)
          Session.Persist(aType);
        edgeType[pos] = aType;
        stringToEdgeType.Add(name, aType);
      }
      return aType;
    }

    /// <summary>
    /// Creates a new edge type.
    /// </summary>
    /// <param name="name">Unique name for the new edge type.</param>
    /// <param name="directed">If true, this creates a directed edge type, otherwise this creates a undirected edge type.</param>
    /// <returns>Unique edge type.</returns>
    /// <param name="headType">a fixed head VertexType</param>
    /// <param name="tailType">a fixed tail VertexType</param>
    /// <returns>a new edge type</returns>
    public EdgeType NewEdgeType(string name, bool directed, VertexType headType, VertexType tailType)
    {
      EdgeType aType;
      if (stringToEdgeType.TryGetValue(name, out aType) == false)
      {
        int pos = edgeTypeCt;
        Update();
        Array.Resize(ref edgeType, ++edgeTypeCt);
        aType = new EdgeType(pos, name, tailType, headType, directed, this);
        if (IsPersistent)
          Session.Persist(aType);
        edgeType[pos] = aType;
        stringToEdgeType.Add(name, aType);
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
      return vertexType.NewProperty(name, dt, kind);
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
      return edgeType.NewProperty(name, dt, kind);
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
      return edgeType.NewEdge(this, tail, head, Session);
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
      return edgeType.Length; // +restrictedEdgeType.Length;
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
    public void Shutdown()
    {
      if (Session.InTransaction)
        Session.Commit();
    }

    /// <summary>
    /// Finds the type id associated with a particular edge type. Lookup by name.
    /// </summary>
    /// <param name="name">The name of the edge/node type being looked up</param>
    /// <returns>A node/edge type id or -1 if not found.</returns>
    public EdgeTypeId FindEdgeType(string name)
    {
      EdgeType eType;
      //if (stringToRestrictedEdgeType.TryGetValue(name, out eType))
      //  return eType.TypeId;
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
    IQuery IGraph.Query()
    {
      return new DefaultGraphQuery(this);
    }

    /// <summary>
    /// Remove the provided edge from the graph.
    /// </summary>
    /// <param name="edge">the edge to remove from the graph</param>
    void IGraph.RemoveEdge(IEdge edge)
    {
      Edge e = edge as Edge;
      e.EdgeType.RemoveEdge(e);
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
      Vertex v = vertex as Vertex;
      v.VertexType.RemoveVertex(v);
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
    /// <param name="id">the identifier of the vertex to retrieved from the graph, must be a UInt64</param>
    /// <returns>the vertex referenced by the provided identifier or null when no such vertex exists</returns>
    public IVertex GetVertex(object id)
    {
      if (id == null)
        throw new ArgumentException("id may not be null, it should be a UInt64");
      if (id is UInt64)
      {
        UInt64 fullId = (UInt64)id;
        VertexTypeId vertexTypeId = (VertexTypeId)(fullId >> 32);
        VertexType vt = vertexType[vertexTypeId];
        VertexTypeId vertexId = (VertexTypeId)fullId;
        Vertex vertex = vt.GetVertex(vertexId);
        return vertex;
      }
      if (id is string)
      {
        UInt64 fullId;
        if (UInt64.TryParse(id as string, out fullId))
        {
          VertexTypeId vertexTypeId = (VertexTypeId)(fullId >> 32);
          VertexType vt = vertexType[vertexTypeId];
          VertexTypeId vertexId = (VertexTypeId)fullId;
          Vertex vertex = vt.GetVertex(vertexId);
          return vertex;
        }
      }
      return null; 
    }

    /// <summary>
    /// Return an iterable to all the vertices in the graph.
    /// </summary>
    /// <returns>an iterable reference to all vertices in the graph</returns>
    public IEnumerable<IVertex> GetVertices()
    {
      foreach (VertexType vt in vertexType)
        foreach (IVertex vertex in vt.GetVertices(this))
          yield return vertex;
    }

    /// <summary>
    /// Return an iterable to all the vertices in the graph that have a particular key/value property.
    /// </summary>
    /// <param name="key">the key of vertex</param>
    /// <param name="value">the value of the vertex</param>
    /// <returns>an iterable of vertices with provided key and value</returns>
    IEnumerable<IVertex> IGraph.GetVertices(string key, object value)
    {
        foreach (VertexType vt in vertexType)
        {
          PropertyType pt = vt.FindProperty(key);
          foreach (Vertex vertex in pt.GetPropertyVertices(value, vt))
            yield return vertex;
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

    public override string ToString()
    {
      return StringFactory.GraphString(this, string.Concat("vertices:", CountVertices().ToString(CultureInfo.InvariantCulture), " edges:", CountEdges().ToString(CultureInfo.InvariantCulture)));
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

    public override UInt64 Persist(Placement place, SessionBase session, bool persistRefs = true, bool disableFlush = false, Queue<IOptimizedPersistable> toPersist = null)
    {
      if (IsPersistent)
        return Id;
      session.RegisterClass(typeof(PropertyType));
      session.RegisterClass(typeof(VertexType));
      session.RegisterClass(typeof(EdgeType));
      session.RegisterClass(typeof(BTreeSet<VertexId>));
      session.RegisterClass(typeof(BTreeSet<EdgeType>));
      session.RegisterClass(typeof(BTreeSet<EdgeIdVertexId>));
      session.RegisterClass(typeof(BTreeMap<EdgeId, VertexId[]>));
      session.RegisterClass(typeof(BTreeMap<string, PropertyType>));
      session.RegisterClass(typeof(BTreeMap<string, EdgeType>));
      session.RegisterClass(typeof(BTreeMap<string, VertexType>));
      session.RegisterClass(typeof(BTreeMap<VertexId, BTreeSet<EdgeIdVertexId>>));
      session.RegisterClass(typeof(BTreeMap<VertexType, BTreeMap<VertexId, BTreeSet<EdgeIdVertexId>>>));
      session.RegisterClass(typeof(BTreeMap<EdgeType, BTreeMap<VertexType, BTreeMap<VertexId, BTreeSet<EdgeIdVertexId>>>>));
      session.RegisterClass(typeof(PropertyTypeT<bool>));
      session.RegisterClass(typeof(PropertyTypeT<int>));
      session.RegisterClass(typeof(PropertyTypeT<long>));
      session.RegisterClass(typeof(PropertyTypeT<double>));
      session.RegisterClass(typeof(PropertyTypeT<DateTime>));
      session.RegisterClass(typeof(PropertyTypeT<string>));
      session.RegisterClass(typeof(PropertyTypeT<object>));
      return base.Persist(place, session, persistRefs, disableFlush, toPersist);
    }
  }
}
