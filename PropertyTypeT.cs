﻿using System;
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
using VelocityDb.Collection;

namespace VelocityGraph
{
  [Serializable]
  public class PropertyTypeT<T> : PropertyType where T : IComparable
  {
    BTreeMap<ElementId, T> propertyValue;
    BTreeMap<T, BTreeSet<ElementId>> valueIndex;
    BTreeMap<T, ElementId> valueIndexUnique;

    internal PropertyTypeT(bool isVertexProp, TypeId typeId, PropertyId propertyId, string name, PropertyKind kind, Graph graph)
      : base(isVertexProp, typeId, propertyId, name, graph)
    {
      propertyValue = new BTreeMap<ElementId, T>(null, graph.Session);
      switch (kind)
      {
        case PropertyKind.Indexed:
          valueIndex = new BTreeMap<T, BTreeSet<ElementId>>(null, graph.Session);
          break;
        case PropertyKind.Unique:
          valueIndexUnique = new BTreeMap<T, ElementId>(null, graph.Session);
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
            BTreeSet<ElementId> oidArray = valueIndex[pv];
            if (oidArray.Count > 1)
              oidArray.Remove(oid);
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
        BTreeSet<ElementId> oidArray;
        if (!valueIndex.TryGetKey(aValue, ref aValue))
        {
          oidArray = new BTreeSet<ElementId>(null, graph.Session);
          oidArray.Add(element);
          valueIndex.AddFast(aValue, oidArray);
        }
        else
        {
          oidArray = valueIndex[aValue];
          oidArray.Add(element);
          valueIndex[aValue] = oidArray;
        }
      }
      else if (valueIndexUnique != null)
        valueIndexUnique.AddFast(aValue, element);
    }

    public Vertex GetPropertyVertex(T value, Graph g, bool errorIfNotFound = true)
    {
      VertexId elementId = -1;
      if (valueIndexUnique == null || valueIndexUnique.TryGetValue(value, out elementId) == false)
      {
        BTreeSet<ElementId> elementIds;
        if (valueIndex != null && valueIndex.TryGetValue(value, out elementIds))
          elementId = elementIds.First();
      }
      if (elementId == -1)
        return null;
      VertexType vertexType = g.vertexType[TypeId];
      return vertexType.GetVertex(elementId, false, errorIfNotFound);
    }

    public override Vertex GetPropertyVertex(IComparable value, Graph g, bool errorIfNotFound = true)
    {
      if (IsVertexProperty == false)
        throw new InvalidTypeIdException();
      return GetPropertyVertex((T) value, g, errorIfNotFound);
    }

    public IEnumerable<Vertex> GetPropertyVertices(T value, VertexType vertexType)
    {
      VertexId elementId = -1;
      if (valueIndexUnique == null || valueIndexUnique.TryGetValue(value, out elementId) == false)
      {
        BTreeSet<ElementId> elementIds;
        if (valueIndex != null && valueIndex.TryGetValue(value, out elementIds))
          foreach (ElementId eId in elementIds)
            yield return vertexType.GetVertex(eId);
      }
      if (elementId != -1)
        yield return vertexType.GetVertex(elementId);
    }

    public override IEnumerable<Vertex> GetPropertyVertices(IComparable value, VertexType vertexType)
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
        BTreeSet<ElementId> elementIds;
        if (valueIndex != null && valueIndex.TryGetValue(value, out elementIds))
          elementId = elementIds.First();
      }
      if (elementId == -1)
        return null;
      EdgeType edgeType = g.edgeType[TypeId];
      return edgeType.GetEdge(elementId);
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
        BTreeSet<ElementId> elementIds;
        if (valueIndex != null && valueIndex.TryGetValue(value, out elementIds))
          foreach (ElementId eId in elementIds)
            yield return edgeType.GetEdge(eId);
      }
      if (elementId != -1)     
        yield return edgeType.GetEdge(elementId);
    }

    public override IEnumerable<Edge> GetPropertyEdges(IComparable value, Graph g)
    {
      if (IsVertexProperty)
        throw new InvalidTypeIdException();
      return GetPropertyEdges((T)value, g);
    }

    public override Edge GetPropertyEdge(IComparable value, Graph g)
    {
      if (IsVertexProperty == false)
        throw new InvalidTypeIdException();
      return GetPropertyEdge((T)value, g);
    }

    public override IComparable GetPropertyValue(ElementId element)
    {
      T v = default(T);
      if (GetPropertyValueT(element, ref v))
        return v;
      return null;
    }

    public override IComparable RemovePropertyValue(ElementId element)
    {
      T pv;
      if (RemovePropertyValueT(element, out pv))
        return pv;
      return null;
    }



    public override void SetPropertyValue(ElementId element, IComparable value)
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

    /// <summary>
    /// Get the value Type
    /// </summary>
    public override Type ValueType 
    {
      get
      {
        return typeof(T);
      }
    }
  }
}
