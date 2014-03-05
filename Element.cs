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

    public virtual IEnumerator<KeyValuePair<string, object>> GetEnumerator()
    {
      return GetPropertyKeys()
          .Select(property => new KeyValuePair<string, object>(property, GetProperty(property)))
          .GetEnumerator();
    }


    public void Remove(object key)
    {
      RemoveProperty(key.ToString());
    }


    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }


    public virtual void Add(KeyValuePair<string, object> item)
    {
      SetProperty(item.Key, item.Value);
    }


    public bool Contains(object key)
    {
      return ContainsKey(key.ToString());
    }


    public virtual void Add(object key, object value)
    {
      Add(new KeyValuePair<string, object>(key.ToString(), value));
    }


    public virtual void Clear()
    {
      foreach (var property in GetPropertyKeys())
        RemoveProperty(property);
    }


    struct DictionaryEnumerator : System.Collections.IDictionaryEnumerator
    {
      readonly IEnumerator<KeyValuePair<string, object>> _en;

      public DictionaryEnumerator(IEnumerator<KeyValuePair<string, object>> en)
      {
        _en = en;
      }


      public object Current
      {
        get
        {
          return Entry;
        }
      }


      public System.Collections.DictionaryEntry Entry
      {
        get
        {
          var kvp = _en.Current;
          return new System.Collections.DictionaryEntry(kvp.Key, kvp.Value);
        }
      }


      public bool MoveNext()
      {
        var result = _en.MoveNext();
        return result;
      }


      public void Reset()
      {
        throw new NotSupportedException();
      }


      public object Key
      {
        get
        {
          var kvp = _en.Current;
          return kvp.Key;
        }
      }


      public object Value
      {
        get
        {
          var kvp = _en.Current;
          return kvp.Value;
        }
      }
    }


    public bool Contains(KeyValuePair<string, object> item)
    {
      return ContainsKey(item.Key) && GetProperty(item.Key) == item.Value;
    }


    public virtual void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
    {
      foreach (var key in GetPropertyKeys())
      {
        if (arrayIndex >= array.Length) break;
        array.SetValue(new KeyValuePair<string, object>(key, GetProperty(key)), arrayIndex);
        arrayIndex++;
      }
    }


    public virtual bool Remove(KeyValuePair<string, object> item)
    {
      var result = (Contains(item));
      if (result)
        RemoveProperty(item.Key);
      return result;
    }


    public void CopyTo(Array array, int index)
    {
      foreach (var key in GetPropertyKeys())
      {
        if (index >= array.Length) break;
        array.SetValue(new System.Collections.DictionaryEntry(key, GetProperty(key)), index);
        index++;
      }
    }

    public virtual int Count
    {
      get { return GetPropertyKeys().Count(); }
    }


    public virtual object SyncRoot
    {
      get { return this; }
    }


    public virtual bool IsSynchronized { get; protected set; }

    public bool IsReadOnly { get; protected set; }
    public bool IsFixedSize { get; protected set; }


    public virtual bool ContainsKey(string key)
    {
      return GetPropertyKeys().Contains(key);
    }


    public virtual void Add(string key, object value)
    {
      SetProperty(key, value);
    }


    public virtual bool Remove(string key)
    {
      var result = ContainsKey(key);
      if (result)
        RemoveProperty(key);
      return result;
    }


    public virtual bool TryGetValue(string key, out object value)
    {
      var result = ContainsKey(key);
      value = result ? GetProperty(key) : null;
      return result;
    }


    public virtual object this[string key]
    {
      get { return GetProperty(key); }
      set { SetProperty(key, value); }
    }


    public virtual ICollection<string> Keys
    {
      get { return GetPropertyKeys().ToList(); }
    }

    public virtual ICollection<object> Values
    {
      get { return GetPropertyKeys().Select(GetProperty).ToList(); }
    }

    public abstract IEnumerable<string> GetPropertyKeys();

    public abstract object GetProperty(string key);

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
    public abstract void SetProperty<T>(string key, T value) where T : IComparable;
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

    /// <summary>
    /// An identifier that is unique to its inheriting class.
    /// All vertices of a graph must have unique identifiers.
    /// All edges of a graph must have unique identifiers.
    /// </summary>
    /// <returns>the identifier of the element</returns>
    public abstract object Id { get; }

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
