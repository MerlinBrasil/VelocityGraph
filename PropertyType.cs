using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VelocityDb.Collection.BTree;
using VelocityDb.Session;
using ElementId = System.Int32;
using PropertyTypeId = System.Int32;
using PropertyId = System.Int32;
using TypeId = System.Int32;
using VertexId = System.Int32;
using EdgeId = System.Int32;

namespace VelocityGraph
{
  public class PropertyType<T> : PropertyTypeBase
  {
    Dictionary<ElementId, T> propertyValue;
    BTreeMap<T, ElementId[]> valueIndex;
    BTreeMap<T, ElementId> valueIndexUnique;

    internal PropertyType(bool isVertexProp, TypeId typeId, PropertyId propertyId, string name, PropertyKind kind, SessionBase session)
      : base(isVertexProp, typeId, propertyId, name)
    {
      propertyValue = new Dictionary<ElementId, T>();
      switch (kind)
      {
        case PropertyKind.Indexed:
          valueIndex = new BTreeMap<T, ElementId[]>(null, session);
          break;
        case PropertyKind.Unique:
          valueIndexUnique = new BTreeMap<T, ElementId>(null, session);
          break;
      }
    }

    T GetPropertyValueT(ElementId oid)
    {
      T pv = propertyValue[oid];
      return pv;
    }

    void SetPropertyValueX(ElementId element, T aValue)
    {
      Update();
      propertyValue[element] = aValue;
      if (valueIndex != null)
      {
        ElementId[] oidArray = new ElementId[1];
        if (!valueIndex.TryGetKey(aValue, ref aValue))
        {
          oidArray[0] = element;
          valueIndex.Add(aValue, oidArray);
        }
        else
        {
          oidArray = valueIndex[aValue];
          int pos = oidArray.Length;
          Array.Resize(ref oidArray, pos + 1);
          oidArray[pos] = element;
        }
      }
      else if (valueIndexUnique != null)
        valueIndexUnique.Add(aValue, element);
    }

    public Vertex? GetPropertyVertex(T value, Graph g)
    {
      VertexId elementId = -1;
      if (valueIndexUnique == null || valueIndexUnique.TryGetValue(value, out elementId) == false)
      {
        ElementId[] elementIds;
        if (valueIndex != null && valueIndex.TryGetValue(value, out elementIds))
          elementId = elementIds[0];
      }
      if (elementId == -1)
        return null;
      VertexType vertexType = g.vertexType[TypeId];
      return vertexType.GetVertex(g, elementId);
    }

    public override Vertex? GetPropertyVertex(object value, Graph g)
    {
      if (IsVertexProperty == false)
        throw new InvalidTypeIdException();
      return GetPropertyVertex((T) value, g);
    }

    public Edge? GetPropertyEdge(T value, Graph g)
    {
      EdgeId elementId = -1;
      if (valueIndexUnique == null || valueIndexUnique.TryGetValue(value, out elementId) == false)
      {
        ElementId[] elementIds;
        if (valueIndex != null && valueIndex.TryGetValue(value, out elementIds))
          elementId = elementIds[0];
      }
      if (elementId == -1)
        return null;
      EdgeType edgeType = g.edgeType[TypeId];
      return edgeType.GetEdge(g, elementId);
    }

    public override Edge? GetPropertyEdge(object value, Graph g)
    {
      if (IsVertexProperty == false)
        throw new InvalidTypeIdException();
      return GetPropertyEdge((T)value, g);
    }

    public override object GetPropertyValue(ElementId element)
    {
      return GetPropertyValueT(element);
    }

    public override void SetPropertyValue(ElementId element, object value)
    {
      SetPropertyValueX(element, (T)value);
    }
  }
}
