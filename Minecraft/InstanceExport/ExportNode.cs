using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Windows;

namespace PCL.Core.Minecraft.InstanceExport
{
    public partial class ExportNode : ObservableObject
    {
        [ObservableProperty]
        private List<ExportNode> _children = [];
        [ObservableProperty]
        private ExportNode _parent;
        [ObservableProperty]
        private string _title;
        [ObservableProperty]
        private string _description;
        [ObservableProperty]
        private Visibility _visibility = Visibility.Visible;
        private bool? _isChecked = false;
        public bool? IsChecked
        {
            get => _isChecked;
            set
            {
                _SetIsChecked(value, true, true);
            }
        }

        public void AddChild(ExportNode child)
        {
            child.Parent = this;
            Children.Add(child);
        }

        public void AddChildRange(IEnumerable<ExportNode> children)
        {
            foreach (var child in children)
            {
                child.Parent = this;
                Children.Add(child);
            }
        }

        private void _SetIsChecked(bool? value, bool updateChildren, bool updateParent)
        {
            if (value == IsChecked) return;

            IsChecked = value;

            if (updateChildren && IsChecked.HasValue)
            {
                this.Children.ForEach(child =>
                {
                    child._SetIsChecked(value, true, false);
                });
            }

            if (updateParent && Parent is not null)
            {
                _CheckParentState();
            }

            this.OnPropertyChanged("IsChecked");
        }

        private void _CheckParentState()
        {
            bool? state = null;

            for (int i = 0; i < this.Children.Count; ++i)
            {
                bool? current = this.Children[i].IsChecked;
                if (i == 0)
                {
                    state = current;
                }
                else if (state != current)
                {
                    state = null;
                    break;
                }
            }
            this._SetIsChecked(state, false, true);
        }
    }
}
