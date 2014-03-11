using Frontenac.Blueprints;
using Frontenac.Blueprints.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ElementId = System.Int32;

namespace VelocityGraph
{
  public abstract class Element : DictionaryElement
  {
    protected readonly ElementId id;
    protected readonly Graph graph;

    protected Element(ElementId id, Graph graph)
    {
      this.graph = graph;
      this.id = id;
    }

    /// <summary>
    /// Assign a key/value property to the element.
    /// If a value already exists for this key, then the previous key/value is overwritten.
    /// </summary>
    /// <param name="key">the string key of the property</param>
    /// <param name="value">the T value o the property</param>
    public abstract void SetProperty<T>(string key, T value) where T : IComparable;

    public override int GetHashCode()
    {
      return id.GetHashCode();
    }
  }
}
