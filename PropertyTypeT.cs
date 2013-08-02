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
using Frontenac.Blueprints;

namespace VelocityGraph
{
  [Serializable]
  public class PropertyTypeT<T> : PropertyType
  {
    BTreeMap<ElementId, T> propertyValue;
    BTreeMap<T, ElementId[]> valueIndex;
    BTreeMap<T, ElementId> valueIndexUnique;

    internal PropertyTypeT(bool isVertexProp, TypeId typeId, PropertyId propertyId, string name, PropertyKind kind, SessionBase session)
      : base(isVertexProp, typeId, propertyId, name)
    {
      propertyValue = new BTreeMap<ElementId, T>(null, session);
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

    bool GetPropertyValueT(ElementId oid, ref T pv)
    {
      if (propertyValue.TryGetValue(oid, out pv))
        return true;
      return false;
    }    
    
    bool RemovePropertyValueT(ElementId oid, out T pv)
    {
      if (propertyValue.TryGetValue(oid, out pv))
      {
        propertyValue.Remove(oid);
        if (valueIndex != null)
        {
          if (valueIndex.TryGetKey(pv, ref pv))
          {
            ElementId[] oidArray = valueIndex[pv];
            if (oidArray.Length > 1)
            {
              ElementId[] oidArrayReduced = new ElementId[oidArray.Length - 1];
              int i = 0;
              foreach (ElementId eId in oidArray)
                if (eId != oid)
                  oidArrayReduced[i++] = eId;
              valueIndex[pv] = oidArrayReduced;
            }
            else
              valueIndex.Remove(pv);
          }
        }
        else if (valueIndexUnique != null)
          valueIndexUnique.Remove(pv);
        return true;
      }
      return false;
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
          valueIndex[aValue] = oidArray;
        }
      }
      else if (valueIndexUnique != null)
        valueIndexUnique.Add(aValue, element);
    }

    public Vertex GetPropertyVertex(T value, Graph g)
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
      return vertexType.GetVertex(elementId);
    }

    public override Vertex GetPropertyVertex(object value, Graph g)
    {
      if (IsVertexProperty == false)
        throw new InvalidTypeIdException();
      return GetPropertyVertex((T) value, g);
    }

    public IEnumerable<Vertex> GetPropertyVertices(T value, VertexType vertexType)
    {
      VertexId elementId = -1;
      if (valueIndexUnique == null || valueIndexUnique.TryGetValue(value, out elementId) == false)
      {
        ElementId[] elementIds;
        if (valueIndex != null && valueIndex.TryGetValue(value, out elementIds))
          foreach (ElementId eId in elementIds)
            yield return vertexType.GetVertex(eId);
      }
      if (elementId != -1)
        yield return vertexType.GetVertex(elementId);
    }

    public override IEnumerable<Vertex> GetPropertyVertices(object value, VertexType vertexType)
    {
      if (IsVertexProperty == false)
        throw new InvalidTypeIdException();
      return GetPropertyVertices((T)value, vertexType);
    }

    public Edge GetPropertyEdge(T value, Graph g)
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

   // public override IEnumerable<Edge> GetPropertyEdgesV<V>(V value, Graph g)
   // {
   //   return GetPropertyEdges(value, g);
   // }

    public IEnumerable<Edge> GetPropertyEdges(T value, Graph g)
   {
      EdgeId elementId = -1;
      EdgeType edgeType = g.edgeType[TypeId];
      if (valueIndexUnique == null || valueIndexUnique.TryGetValue(value, out elementId) == false)
      {
        ElementId[] elementIds;
        if (valueIndex != null && valueIndex.TryGetValue(value, out elementIds))
          foreach (ElementId eId in elementIds)
            yield return edgeType.GetEdge(g, eId);
      }
      if (elementId != -1)     
        yield return edgeType.GetEdge(g, elementId);
    }

    public override IEnumerable<Edge> GetPropertyEdges(object value, Graph g)
    {
      if (IsVertexProperty)
        throw new InvalidTypeIdException();
      return GetPropertyEdges((T)value, g);
    }

    public override Edge GetPropertyEdge(object value, Graph g)
    {
      if (IsVertexProperty == false)
        throw new InvalidTypeIdException();
      return GetPropertyEdge((T)value, g);
    }

    public override object GetPropertyValue(ElementId element)
    {
      T v = default(T);
      if (GetPropertyValueT(element, ref v))
        return v;
      return null;
    }

    public override object RemovePropertyValue(ElementId element)
    {
      T pv;
      if (RemovePropertyValueT(element, out pv))
        return pv;
      return null;
    }

   

    public override void SetPropertyValue(ElementId element, object value)
    {
      /*Type fromType = value.GetType();
      Type toType = typeof(T);
      if (fromType.IsValueType)
      {
        TypeCode tc = Type.GetTypeCode(fromType);
        switch (tc)
        {
          case TypeCode.Int32:
            SetPropertyValueX(element, (T)(int)value);
        }
      }*/
      SetPropertyValueX(element, (T) value);
    }
  }
}
