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
    TypeId edgeType;
    EdgeId edgetId;
    Graph graph;

    public Edge(Graph g, TypeId eType, EdgeId eId)
    {
      edgeType = eType;
      edgetId = eId;
      graph = g;
    }

    public EdgeId EdgeId
    {
      get
      {
        return edgetId;
      }
    }

    public TypeId EdgeType
    {
      get
      {
        return edgeType;
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

    public void SetProperty(PropertyId property, object v)
    {
      EdgeType anEdgeType = graph.edgeType[EdgeType];
      if (anEdgeType != null)
        anEdgeType.SetPropertyValue(graph.propertyType, EdgeId, property, v);
      else
        throw new InvalidTypeIdException();
    }
  }
}
