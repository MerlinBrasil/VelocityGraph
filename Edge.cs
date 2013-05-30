using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TypeId = System.Int32;
using EdgeId = System.Int32;

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

    public Edge(TypeId eType, EdgeId eId)
    {
      edgeType = eType;
      edgetId = eId;
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
  }
}
