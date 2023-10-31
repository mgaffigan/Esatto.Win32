using System.Collections.ObjectModel;
using System.Runtime.Serialization;

namespace Esatto.Utilities
{
    public class ParentedList<TParent, TChild> : ObservableCollection<TChild> 
        where TChild : class, IChild<TParent>
    {
        private TParent _parent;
        public TParent Parent
        {
            get { return _parent; }
            set
            {
                this._parent = value;

                foreach (var child in this)
                {
                    if (child != null)
                    {
                        connect(child);
                    }
                }
            }
        }

        /// <summary>
        /// DCS constructor
        /// </summary>
        #nullable disable
        protected ParentedList()
        {
        }
        #nullable restore

        public ParentedList(TParent parent)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent), "Contract assertion not met: parent != null");
            }

            this.Parent = this._parent = parent;
        }

        public void AddRange(IEnumerable<TChild> children)
        {
            foreach (var child in children)
            {
                Add(child);
            }
        }

        protected override void InsertItem(int index, TChild item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            base.InsertItem(index, item);

            connect(item);
        }

        protected override void SetItem(int index, TChild item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            base.SetItem(index, item);

            connect(item);
        }

        private void connect(TChild item)
        {
            item.Parent = Parent;
        }
    }
}
