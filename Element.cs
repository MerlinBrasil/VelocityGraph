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
  public abstract class Element : IElement
  {
    protected readonly ElementId id;
    protected readonly Graph graph;

    protected Element(ElementId id, Graph graph)
    {
      this.graph = graph;
      this.id = id;
    }

    public abstract IEnumerable<string> GetPropertyKeys();

    public abstract object GetProperty(string key);
    public abstract T GetProperty<T>(string key);

    /// <summary>
    /// Assign a key/value property to the element.
    /// If a value already exists for this key, then the previous key/value is overwritten.
    /// </summary>
    /// <param name="key">the string key of the property</param>
    /// <param name="value">the object value o the property</param>
    public abstract void SetProperty(string key, object value);

    /// <summary>
    /// Assign a key/value property to the element.
    /// If a value already exists for this key, then the previous key/value is overwritten.
    /// </summary>
    /// <param name="key">the string key of the property</param>
    /// <param name="value">the T value o the property</param>
    public abstract void SetProperty<T>(string key, T value);
    /// <summary>
    /// Un-assigns a key/value property from the element.
    /// The object value of the removed property is returned.
    /// </summary>
    /// <param name="key">the key of the property to remove from the element</param>
    /// <returns>the object value associated with that key prior to removal</returns>
    public abstract object RemoveProperty(string key);

    public override int GetHashCode()
    {
      return id.GetHashCode();
    }

    public object GetId()
    {
      return id;
    }

    public override bool Equals(object obj)
    {
      return ElementHelper.AreEqual(this, obj);
    }

    /// <summary>
    /// Remove the element from the graph.
    /// </summary>
    public abstract void Remove();
  }
}
