// SHAIQAS Core · Қарапайым тізім (C# және призрачный Java-да бірдей жұмыс істейді)
using UnityEngine;
public class Lst<T>
{
    object[] a = new object[8];
    int n = 0;

    public int Count() { return n; }
    public T Get(int i) { return (T)a[i]; }
    public void Set(int i, T v) { a[i] = v; }
    public void Add(T v)
    {
        if (n == a.Length) Grow();
        a[n] = v; n++;
    }
    void Grow()
    {
        object[] b = new object[a.Length * 2];
        for (int i = 0; i < n; i++) b[i] = a[i];
        a = b;
    }
    public void Insert(int idx, T v)
    {
        if (n == a.Length) Grow();
        for (int i = n; i > idx; i--) a[i] = a[i - 1];
        a[idx] = v; n++;
    }
    public void RemoveAt(int idx)
    {
        for (int i = idx; i < n - 1; i++) a[i] = a[i + 1];
        n--; a[n] = null;
    }
    public void Clear() { for (int i = 0; i < n; i++) a[i] = null; n = 0; }
    public int IndexOf(T v)
    {
        for (int i = 0; i < n; i++) if (a[i] != null && a[i].Equals(v)) return i;
        return -1;
    }
    public bool Contains(T v) { return IndexOf(v) >= 0; }
    public T Last() { return (T)a[n - 1]; }
}
