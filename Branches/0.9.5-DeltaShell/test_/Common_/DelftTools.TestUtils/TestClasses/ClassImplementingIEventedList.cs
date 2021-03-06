using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using DelftTools.Utils;
using DelftTools.Utils.Aop;
using DelftTools.Utils.Aop.NotifyPropertyChanged;
using DelftTools.Utils.Collections;
using DelftTools.Utils.Collections.Generic;
using PostSharp;

namespace DelftTools.TestUtils.TestClasses
{
    public class TreeClient
    {
        private ClassImplementingIEventedList root;

        public TreeClient()
        {
            Root = new ClassImplementingIEventedList();
        }

        public ClassImplementingIEventedList Root
        {
            get { return root; }
            set
            {
                root = value;
                Post.Cast<ClassImplementingIEventedList, INotifyCollectionChanged>(root).CollectionChanged +=
                    root_CollectionChanged;
            }
        }

        void root_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (RootChanged != null)
                RootChanged(this, new EventArgs());
        }
        public event EventHandler<EventArgs> RootChanged;

    }

    [NotifyCollectionChanged]
    public class ClassImplementingIEventedList : IList<object>, ICloneable, INameable
    {
        private string name;

        private IEventedList<ClassImplementingIEventedList> children;

        [NoBubbling]
        private ClassImplementingIEventedList parent;

        private const string defaultName = "item";

        private long id = -1;

        /// <summary>
        /// Create folder with default name
        /// </summary>
        public ClassImplementingIEventedList()
            : this(defaultName)
        {
        }

        /// <summary>
        /// Create folder with specific name
        /// </summary>
        /// <param name="name"></param>
        public ClassImplementingIEventedList(string name)
        {
            this.name = name;

            //assign lists in constructor so event bubbling will be supported
            children = new EventedList<ClassImplementingIEventedList>();
            children.CollectionChanged += ChildCollectionChanged;
        }

        private void ChildCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // update owner of folder in case the folder belongs to the list of folders contained
            // by this folder.
            if (sender == children)
            {
                ClassImplementingIEventedList folder = (ClassImplementingIEventedList)e.Item;

                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Replace:
                    case NotifyCollectionChangedAction.Add:
                        folder.Parent = this;
                        break;

                    case NotifyCollectionChangedAction.Remove:
                        folder.Parent = null;
                        break;
                }
            }
        }

        /// <summary>
        /// Name of the folder presented to the end user.
        /// </summary>
        public virtual string Name
        {
            get { return name; }
            set { name = value; }
        }

        /// <summary>
        /// Folder that contains this folder.
        /// </summary>
        [NoNotifyPropertyChanged]
        public virtual ClassImplementingIEventedList Parent
        {
            get { return parent; }
            set { parent = value; }
        }


        /// <summary>
        /// Subfolders of the current folder
        /// </summary>
        public virtual IEventedList<ClassImplementingIEventedList> Children
        {
            get { return children; }
            set
            {
                children.CollectionChanged -= ChildCollectionChanged;
                children = value;
                // when list of folders is retrieved from nhibernate parent should be updated, since it is not stored
                // in the database
                foreach (var child in children)
                {
                    child.Parent = this;
                }
                children.CollectionChanged += ChildCollectionChanged;
            }
        }

        public virtual object Clone()
        {
            ClassImplementingIEventedList folderCopy = new ClassImplementingIEventedList();
            folderCopy.Name = Name;

            foreach (var child in Children)
            {
                folderCopy.children.Add((ClassImplementingIEventedList)child.Clone());
            }
            return folderCopy;
        }

        #region IList Members

        public virtual void Add(object o)
        {
            ClassImplementingIEventedList child;
            if ((child = o as ClassImplementingIEventedList) != null)
            {
                Children.Add(child);
            }
        }

        public virtual bool Remove(object o)
        {
            ClassImplementingIEventedList child = (ClassImplementingIEventedList)o;
            return Children.Remove(child);
        }


        public virtual void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        public virtual object this[int index]
        {
            get
            {
                if (index < Children.Count)
                {
                    return Children[index];
                }
                else
                {
                    throw new IndexOutOfRangeException();
                }
            }
            set
            {
                if (index < Children.Count)
                {
                    Children[index] = (ClassImplementingIEventedList)value;
                }
                else
                {
                    throw new IndexOutOfRangeException();
                }
            }
        }


        public virtual bool IsReadOnly
        {
            get { return true; } // change it after all object-based modification methods are implemented
        }

        public virtual bool IsFixedSize
        {
            get { return false; }
        }

        #endregion

        #region ICollection Members

        public virtual void CopyTo(Array array, int index)
        {
            throw new NotImplementedException();
        }

        public virtual int Count
        {
            get { return Children.Count; }
        }

        public virtual object SyncRoot
        {
            get { return null; }
        }

        public virtual bool IsSynchronized
        {
            get { return false; }
        }

        #endregion

        public virtual ClassImplementingIEventedList GetChildContaining(object item)
        {
            foreach (ClassImplementingIEventedList child in Children)
            {
                if (child == item)
                {
                    return this;
                }
                ClassImplementingIEventedList childFolder;
                if ((childFolder = child.GetChildContaining(item)) != null)
                {
                    return childFolder;
                }
            }
            return null;
        }

        public virtual void Insert(int index, object item)
        {
            // TODO: make it throw exception, change to ICollection in the future
            Add(item);
        }

        public virtual bool Contains(object item)
        {
            ClassImplementingIEventedList folder;
            if ((folder = item as ClassImplementingIEventedList) != null)
            {
                return Children.Contains(folder);
            }
            return false;
        }

        #region ICollection<object> Members

        public virtual void CopyTo(object[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        bool ICollection<object>.Remove(object item)
        {
            ClassImplementingIEventedList child;
            if ((child = item as ClassImplementingIEventedList) != null)
            {
                return Children.Remove(child);
            }
            else
            {
                throw new NotSupportedException(
                    String.Format(CultureInfo.InvariantCulture, "Type is not supported as a member of a folder: {0}",
                                  item));
            }
        }

        #endregion

        void ICollection<object>.Add(object item)
        {
            ClassImplementingIEventedList folder;
            if ((folder = item as ClassImplementingIEventedList) != null)
            {
                Children.Add(folder);
            }
            else
            {
                throw new NotSupportedException(
                    String.Format(CultureInfo.InvariantCulture, "Type is not supported as a member of a folder: {0}",
                                  item));
            }
        }

        public virtual void Clear()
        {
            Children.Clear();
        }

        public virtual int IndexOf(object item)
        {
            throw new NotImplementedException();
        }

        #region IEnumerable Members

        public virtual IEnumerator GetEnumerator()
        {
            return new FolderItemEnumerator();
        }

        public class FolderItemEnumerator : IEnumerator
        {
            private object current;

            #region IEnumerator Members

            public bool MoveNext()
            {
                throw new NotImplementedException();
            }

            public void Reset()
            {
                throw new NotImplementedException();
            }

            public object Current
            {
                get { return current; }
            }

            #endregion
        }

        #endregion

        #region IEnumerable<object> Members

        IEnumerator<object> IEnumerable<object>.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        #endregion

        public virtual event PropertyChangedEventHandler PropertyChanged;
    }
}