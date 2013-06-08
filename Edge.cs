using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TypeId = System.Int32;
using EdgeId = System.Int32;
using PropertyId = System.Int32;

namespace VelocityGraph
{
  /// <summary>
  /// An Edge links two vertices. Along with its key/value properties, an edge has both a directionality and a label.
  /// The directionality determines which vertex is the tail vertex (out vertex) and which vertex is the head vertex (in vertex).
  /// The edge label determines the type of relationship that exists between the two vertices.
  /// Diagrammatically, outVertex ---label---> inVertex.
  /// </summary>
  public struct Edge
  {
    EdgeType edgeType;
    EdgeId edgetId;
    Vertex tail;
    Vertex head;
    Graph graph;

    internal Edge(Graph g, EdgeType eType, EdgeId eId, Vertex h, Vertex t)
    {
      edgeType = eType;
      edgetId = eId;
      tail = t;
      head = h;
      graph = g;
    }

    public EdgeId EdgeId
    {
      get
      {
        return edgetId;
      }
    }

    public EdgeType EdgeType
    {
      get
      {
        return edgeType;
      }
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
      throw new NotImplementedException();
    }

    public void SetProperty(PropertyTypeBase property, object v)
    {
      if (edgeType != null)
        edgeType.SetPropertyValue(EdgeId, property, v);
      else
        throw new InvalidTypeIdException();
    }
  }
}
