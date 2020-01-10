using Assets.State;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Utilities;

namespace Assets.Utilities.Model
{

    public class ObjectCollection<T> : List<T> where T : ObjectBase
    {
        public ObjectCollection() 
        {
            Claimed = new List<T>();
            Free = new List<T>();
            InUse = new List<T>();
        }

        private List<T> Claimed { get; }
        private List<T> Free { get; }
        private List<T> InUse { get; }

        public int UseCount { get => InUse.Count; }

        private bool init = true;
        public void Store(T item, bool claim = false, bool deactivate = true)
        {
            if (InUse.Contains(item))
                InUse.Remove(item);
            else Add(item);

            if (!claim) Free.Add(item);
            else Claimed.Add(item);

            if (deactivate && item.gameObject.activeSelf)
                item.gameObject.SetActive(false);

            init = false;
        }

        public bool Claim(out T item, Func<T, bool> filter = null)
        {
            lock (this)
            {
                if (Free.Any())
                {
                    IEnumerable<T> items = filter != null ? Free.Where(v => filter(v)) : Free;

                    item = Tools.RandomElement(items);

                    Free.Remove(item);
                    Claimed.Add(item);

                    return true;
                }
            }

            item = null;
            return false;
        }

        public void Release(T item)
        {
            Claimed.Remove(item);
            Free.Add(item);

            if (item.gameObject.activeSelf)
                item.gameObject.SetActive(false);
        }

        public void Use(T item)
        {
            Claimed.Remove(item);
            InUse.Add(item);

            if (!item.gameObject.activeSelf)
                item.gameObject.SetActive(true);

            if (item.gameObject.activeSelf && item is IAlive alive)
                alive.Breathe();
        }

        public T Extract(T item)
        {
            Claimed.Remove(item);
            Free.Remove(item);
            InUse.Remove(item);
            Remove(item);

            return item;
        }

        public bool IsViable() => init || Claimed.Any() || InUse.Any();

        public bool DestroyAll()
        {
            if (!IsViable())
            {
                T[] items = ToArray();
                Free.Clear();
                Clear();

                foreach (T item in items)
                    UnityEngine.Object.Destroy(item.gameObject);

                return true;
            }

            return false;
        }
    }
}
