using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TypeId = System.Int32;
using VertexId = System.Int32;
using VertexTypeId = System.Int32;
using PropertyId = System.Int32;
using EdgeTypeId = System.Int32;
using Frontenac.Blueprints;
using Frontenac.Blueprints.Util;

namespace VelocityGraph
{
  /// <summary>
  /// A vertex maintains pointers to both a set of incoming and outgoing edges.
  /// The outgoing edges are those edges for which the vertex is the tail.
  /// The incoming edges are those edges for which the vertex is the head.
  /// Diagrammatically, ---inEdges---> vertex ---outEdges--->
  /// </summary>
  public class Vertex : Element, IVertex
  {
    VertexType vertexType;

    /// <summary>
    /// Normally you should use <see cref="VertexType.GetVertex"/> but if you need a reference to a Vertex that has no yet been created, this constructor may be used (but know what you are doing!)
    /// </summary>
    /// <param name="g">the owning graph</param>
    /// <param name="eType">the type of the Vertex</param>
    /// <param name="eId">the Id of the Vertex</param>
    public Vertex(Graph g, VertexType eType, VertexId eId)
      : base(eId, g)
    {
      vertexType = eType;
    }

    /// <summary>
    /// Add an edge from this Vertex to inVertex of edge type looked up from label, if edge type does not yet exist it is created.
    /// </summary>
    /// <param name="label">The type of edge to create</param>
    /// <param name="inVertex">The head of the new edge</param>
    /// <returns>the new edge</returns>
    public IEdge AddEdge(string label, IVertex inVertex)
    {
      EdgeType edgeType = Graph.FindEdgeType(label);
      if (edgeType == null)
        edgeType = Graph.NewEdgeType(label, true);
      return Graph.NewEdge(edgeType, this, inVertex as Vertex);
    }

    /// <summary>
    /// An identifier that is unique to its inheriting class.
    /// All vertices of a graph must have unique identifiers.
    /// All edges of a graph must have unique identifiers.
    /// </summary>
    /// <returns>the identifier of the element</returns>
    public override object Id
    {
      get
      {
        UInt64 fullId = (UInt64)vertexType.TypeId;
        fullId <<= 32;
        fullId += (UInt64)id;
        return fullId;
      }
    }

    /// <summary>
    /// Gets the number of edges from or to this vertex and for the given edge type. 
    /// </summary>
    /// <param name="edgeType">an EdgeType</param>
    /// <param name="dir">direction, one of: Out, In, Both</param>
    /// <returns>The number of edges.</returns>
    public long GetNumberOfEdges(EdgeType edgeType, Direction dir)
    {
      return vertexType.GetNumberOfEdges(edgeType, this.VertexId, dir);
    }

    /// <summary>
    /// Return the object value associated with the provided string key.
    /// If no value exists for that key, return null.
    /// </summary>
    /// <param name="key">the key of the key/value property</param>
    /// <returns>the object value related to the string key</returns>
    public override object GetProperty(string key)
    {
      PropertyType pt = vertexType.FindProperty(key);
      return vertexType.GetPropertyValue(id, pt);
    }

    /// <summary>
    /// Return all the keys associated with the vertex.
    /// </summary>
    /// <returns>the set of all string keys associated with the vertex</returns>
    public override IEnumerable<string> GetPropertyKeys()
    {
      foreach (string key in vertexType.GetPropertyKeys())
      {
        if (GetProperty(key) != null)
          yield return key;
      }
    }

    /// <summary>
    /// Gets the Value for the given Property id
    /// </summary>
    /// <param name="property">Property type identifier.</param>
    /// <param name="v">Value for the given Property and for the given id.</param>
    public object GetProperty(PropertyType property)
    {
      return vertexType.GetPropertyValue(VertexId, property);
    }

    /// <summary>
    /// Selects all edges from or to this vertex and for the given edge type. 
    /// </summary>
    /// <param name="etype">the id of an EdgeType</param>
    /// <param name="dir">direction, one of: Out, In, Both</param>
    /// <returns>a set of Edge</returns>
    public IEnumerable<IEdge> GetEdges(EdgeType edgeType, Direction dir)
    {
      return vertexType.GetEdges(edgeType, this, dir);
    }

    /// <summary>
    /// Return the edges incident to the vertex according to the provided direction and edge labels.
    /// </summary>
    /// <param name="direction">the direction of the edges to retrieve</param>
    /// <param name="labels">the labels of the edges to retrieve</param>
    /// <returns>an IEnumerable of incident edges</returns>
    public IEnumerable<IEdge> GetEdges(Direction direction, params string[] labels)
    {
      switch (direction)
      {
        case Direction.Out:
          foreach (IEdge edge in GetOutEdges(labels).ToArray())
            yield return edge;
          break;
        case Direction.In:
            foreach (IEdge edge in GetInEdges(labels).ToArray())
              yield return edge;
          break;
        default:
            foreach (IEdge edge in GetInEdges(labels).ToArray())
              yield return edge;
            foreach (IEdge edge in GetOutEdges(labels).ToArray())
              yield return edge;
          break;
       };
    }

    IEnumerable<IEdge> GetInEdges(params string[] labels)
    {
      if (labels.Length == 0)
        foreach (IEdge edge in vertexType.GetEdges(Graph, this, Direction.In))
          yield return edge;
      else
      {
        foreach (string label in labels)
        {
          EdgeType edgeType = Graph.FindEdgeType(label);
          if (edgeType != null)
          {
            foreach (IEdge edge in vertexType.GetEdges(edgeType, this, Direction.In))
              yield return edge;
          }
        }
      }
    }

    IEnumerable<IEdge> GetOutEdges(params string[] labels)
    {
      if (labels.Length == 0)
      {
        foreach (IEdge edge in vertexType.GetEdges(Graph, this, Direction.Out))
          yield return edge;
      }
      else
      {
        foreach (string label in labels)
        {
          EdgeType edgeType = Graph.FindEdgeType(label);
          foreach (IEdge edge in vertexType.GetEdges(edgeType, this, Direction.Out))
            yield return edge;
        }
      }
    }

    /// <summary>
    /// Return the vertices adjacent to the vertex according to the provided direction and edge labels.
    /// This method does not remove duplicate vertices (i.e. those vertices that are connected by more than one edge).
    /// </summary>
    /// <param name="direction">the direction of the edges of the adjacent vertices</param>
    /// <param name="labels">the labels of the edges of the adjacent vertices</param>
    /// <returns>an IEnumerable of adjacent vertices</returns>
    public IEnumerable<IVertex> GetVertices(Direction direction, params string[] labels)
    {
      if (labels.Length == 0)
      {
        HashSet<EdgeType> edgeTypes = new HashSet<EdgeType>();
        foreach (Edge edge in GetEdges(direction, labels))
          edgeTypes.Add(edge.EdgeType);
        foreach (EdgeType edgeType in edgeTypes)
          foreach (IVertex vertex in vertexType.GetVertices(Graph, edgeType, this, direction))
            yield return vertex;
      }
      else
      {
        foreach (string label in labels)
        {
          EdgeType edgeType = Graph.FindEdgeType(label);
          foreach (IVertex vertex in vertexType.GetVertices(Graph, edgeType, this, direction))
            yield return vertex;
        }
      }
    }

    public VertexId VertexId
    {
      get
      {
        return id;
      }
    }

    internal VertexType VertexType
    {
      get
      {
        return vertexType;
      }
    }

    /// <summary>
    /// Selects all neighbor Vertices from or to this vertex and for the given edge type.
    /// </summary>
    /// <param name="etype">Edge type identifier.</param>
    /// <param name="dir">Direction</param>
    /// <returns>Dictionary of vertex key with edge path to vertex</returns>
    public Dictionary<Vertex, Edge> Traverse(EdgeType etype, Direction dir)
    {
      return vertexType.Traverse(Graph, this, etype, dir);
    }

    struct PathInfo
    {
      public PathInfo(Vertex node, List<Edge> edgePath)
      {
        this.node = node;
        this.edgePath = edgePath;
      }
      public Vertex node;
      public List<Edge> edgePath;
    }

    /// <summary>
    /// Traverses graph from this Vertex to a target Vertex using Breadth-first search like in Dijkstra's algorithm
    /// </summary>
    /// <param name="toVertex">the goal Vertex</param>
    /// <param name="et">the type of edges to follow</param>
    /// <param name="maxHops">maximum number of hops between this Vertex and to Vertex</param>
    /// <param name="all">find or not find all paths to goal Vertex</param>
    /// <returns>List of paths to goal Vertex</returns>
    public List<List<Edge>> Traverse(Vertex toVertex, EdgeType et, int maxHops, bool all)
    {
      Queue<PathInfo> q = new Queue<PathInfo>();
      SortedSet<Int32> visited = new SortedSet<int>();

      visited.Add(VertexId);
      Edge edge;
      List<Edge> path = new List<Edge>(10);
      List<List<Edge>> resultPaths = new List<List<Edge>>();

      PathInfo pathInfo = new PathInfo(this, path);
      q.Enqueue(pathInfo);
      while (q.Count > 0)
      {
        pathInfo = q.Dequeue();
        Dictionary<Vertex, Edge> friends = pathInfo.node.Traverse(et, Direction.Out);
        if (friends.TryGetValue(toVertex, out edge))
        {
          //Console.WriteLine(this + " and " + toVertex + " have a friendship link");
          List<Edge> edgePath = pathInfo.edgePath;
          edgePath.Add(edge);
          resultPaths.Add(edgePath);
          if (!all)
            return resultPaths;
        }
        if (pathInfo.edgePath.Count < maxHops)
          foreach (KeyValuePair<Vertex, Edge> v in friends)
          {
            if (visited.Contains(v.Key.VertexId) == false)
            {
              visited.Add(v.Key.VertexId);
              path = new List<Edge>(pathInfo.edgePath);
              path.Add(v.Value);
              pathInfo = new PathInfo(v.Key, path);
              q.Enqueue(pathInfo);
            }
          }
      }
      //if (all && resultPaths.Count == 0)
      //  Console.WriteLine(this + " and " + toVertex + " may not be connected by indirect frienship");
      return resultPaths;
    }


    public IVertexQuery Query()
    { // TO DO - Optimize
      return new DefaultVertexQuery(this);
    }

    public override void Remove()
    {
      vertexType.RemoveVertex(this);
    }

    /// <summary>
    /// Un-assigns a key/value property from the vertex.
    /// The object value of the removed property is returned.
    /// </summary>
    /// <param name="key">the key of the property to remove from the vertex</param>
    /// <returns>the object value associated with that key prior to removal</returns>
    public override object RemoveProperty(string key)
    {
      PropertyType pt = vertexType.FindProperty(key);
      if (pt == null)
        return null;
      return pt.RemovePropertyValue(id);
    }

    public void SetProperty(PropertyType property, IComparable v)
    {
      vertexType.SetPropertyValue(VertexId, property, v);
    }

    /// <summary>
    /// Assign a key/value property to the vertex.
    /// If a value already exists for this key, then the previous key/value is overwritten.
    /// </summary>
    /// <param name="key">the string key of the property</param>
    /// <param name="value">the object value o the property</param>
    public override void SetProperty(string key, object value)
    {
      if (key == null || key.Length == 0)
        throw new ArgumentException("Property key may not be null or be an empty string");
      if (value == null)
        throw new ArgumentException("Property value may not be null");
      if (key.Equals(StringFactory.Id))
        throw ExceptionFactory.PropertyKeyIdIsReserved();
      PropertyType pt = vertexType.FindProperty(key);
      if (pt == null)
        pt = vertexType.graph.NewVertexProperty(vertexType, key, DataType.Object, PropertyKind.Indexed);
      vertexType.SetPropertyValue(VertexId, pt, (IComparable) value);
    }

    /// <summary>
    /// Assign a key/value property to the vertex.
    /// If a value already exists for this key, then the previous key/value is overwritten.
    /// </summary>
    /// <param name="key">the string key of the property</param>
    /// <param name="value">the object value o the property</param>
    public override void SetProperty<T>(string key, T value)
    {
      PropertyType pt = vertexType.FindProperty(key);
      if (pt == null)
        pt = vertexType.graph.NewVertexProperty(vertexType, key, DataType.Object, PropertyKind.Indexed);
      vertexType.SetPropertyValue(VertexId, pt, value);
    }

    public override string ToString()
    {
      return "Vertex: " + VertexId + " " + vertexType.TypeName;
    }
  }
}
