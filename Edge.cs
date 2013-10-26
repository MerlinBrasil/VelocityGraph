using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TypeId = System.Int32;
using EdgeId = System.Int32;
using PropertyId = System.Int32;
using Frontenac.Blueprints;
using Frontenac.Blueprints.Util;

namespace VelocityGraph
{
  /// <summary>
  /// An Edge links two vertices. Along with its key/value properties, an edge has both a directionality and a label.
  /// The directionality determines which vertex is the tail vertex (out vertex) and which vertex is the head vertex (in vertex).
  /// The edge label determines the type of relationship that exists between the two vertices.
  /// Diagrammatically, outVertex ---label---> inVertex.
  /// </summary>
  public class Edge : Element, IEdge
  {
    EdgeType edgeType;
    Vertex tail;
    Vertex head;

    internal Edge(Graph g, EdgeType eType, EdgeId eId, Vertex h, Vertex t):base(eId, g)
    {
      edgeType = eType;
      tail = t;
      head = h;
    }

    public EdgeId EdgeId
    {
      get
      {
        return id;
      }
    }

    public EdgeType EdgeType
    {
      get
      {
        return edgeType;
      }
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
        UInt64 fullId = (UInt64)edgeType.TypeId;
        fullId <<= 32;
        fullId += (UInt64)id;
        return fullId;
      }
    }

    /// <summary>
    /// Return the label associated with the edge.
    /// </summary>
    /// <returns>the edge label</returns>
    string IEdge.Label
    {
      get
      {
        return edgeType.TypeName;
      }
    }

    /// <summary>
    /// Gets the Value for the given Property id
    /// </summary>
    /// <param name="property">Property type identifier.</param>
    /// <param name="v">Value for the given Property and for the given id.</param>
    public object GetProperty(PropertyType property)
    {
      return edgeType.GetPropertyValue(EdgeId, property);
    }
    
    /// <summary>
    /// Return the object value associated with the provided string key.
    /// If no value exists for that key, return null.
    /// </summary>
    /// <param name="key">the key of the key/value property</param>
    /// <returns>the object value related to the string key</returns>
    public override object GetProperty(string key)
    {
      PropertyType pt = edgeType.FindProperty(key);
      if (pt == null)
        return null;
      return edgeType.GetPropertyValue(id, pt);
    }

    /// <summary>
    /// Return all the keys associated with the element.
    /// </summary>
    /// <returns>the set of all string keys associated with the element</returns>
    public override IEnumerable<string> GetPropertyKeys()
    {
      foreach (string key in edgeType.GetPropertyKeys())
      {
        if (GetProperty(key) != null)
          yield return key;
      }
    }

    /// <summary>
    /// Return the tail/out or head/in vertex.
    /// ArgumentException is thrown if a direction of both is provided
    /// </summary>
    /// <param name="direction">whether to return the tail/out or head/in vertex</param>
    /// <returns>the tail/out or head/in vertex</returns>
    IVertex IEdge.GetVertex(Direction direction)
    {
      if (direction == Direction.In)
        return head;
      if (direction == Direction.Out)
        return tail;
      throw new ArgumentException("A direction of BOTH is not supported");
    }

    public Vertex Tail
    {
      get
      {
        return tail;        
      }
    }

    public Vertex Head
    {
      get
      {
        return head;
      }
    }

    /// <summary>
    /// Gets the other end for the given edge.
    /// </summary>
    /// <param name="vertex">A vertex, it must be one of the ends of the edge.</param>
    /// <returns>The other end of the edge.</returns>
    public Vertex GetEdgePeer(Vertex vertex)
    {
      if (head == vertex)
        return tail;
      if (tail == vertex)
        return head;
      throw new ArgumentException("Vertex argument must be either Head or Tail Vertex");
    }

    /// <summary>
    /// Remove the edge from the graph.
    /// </summary>
    public override void Remove()
    {
      edgeType.RemoveEdge(this);
    }

    /// <summary>
    /// Un-assigns a key/value property from the edge.
    /// The object value of the removed property is returned.
    /// </summary>
    /// <param name="key">the key of the property to remove from the edge</param>
    /// <returns>the object value associated with that key prior to removal</returns>
    public override object RemoveProperty(string key)
    {
      PropertyType pt = edgeType.FindProperty(key);
      return pt.RemovePropertyValue(id);
    }

    public void SetProperty(PropertyType property, object v)
    {
      if (edgeType != null)
        edgeType.SetPropertyValue(EdgeId, property, v);
      else
        throw new InvalidTypeIdException();
    }    

    /// <summary>
    /// Assign a key/value property to the edge.
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
      PropertyType pt = edgeType.FindProperty(key);
      if (pt == null)
        pt = edgeType.NewProperty(key, value, PropertyKind.Indexed);
      edgeType.SetPropertyValue(EdgeId, pt, value);
    }

    /// <summary>
    /// Assign a key/value property to the edge.
    /// If a value already exists for this key, then the previous key/value is overwritten.
    /// </summary>
    /// <param name="key">the string key of the property</param>
    /// <param name="value">the object value o the property</param>
    public override void SetProperty<T>(string key, T value)
    {
      PropertyType pt = edgeType.FindProperty(key);
      if (pt == null)
      {
        pt = edgeType.graph.NewEdgeProperty(edgeType, key, DataType.Object, PropertyKind.Indexed);
      }
      edgeType.SetPropertyValue(EdgeId, pt, value);
    }

    public override string ToString()
    {
      return "Edge: " + EdgeId + " " + edgeType.TypeName;
    }
  }
}
