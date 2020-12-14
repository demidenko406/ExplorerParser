using System;
using System.Collections;
using System.Collections.Generic;

namespace Explorer.Models
{
    internal class DirectoryHistory : IDirectoryHistory
    {
        private readonly DirectoryNode head;

        public DirectoryHistory(string directoryPath)
        {
            head = new DirectoryNode(directoryPath);
            Current = head;
        }

        public DirectoryNode Current { get; private set; }
        public bool CanMoveBack => Current.PreviousNode != null;
        public bool CanMoveForward => Current.NextNode != null;

        public IEnumerator<DirectoryNode> GetEnumerator()
        {
            yield return Current;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public event EventHandler HistoryChanged;

        public void MoveBack()
        {
            var prev = Current.PreviousNode;
            Current = prev;
            RaiseHistoryChanged();
        }

        public void MoveForward()
        {
            var next = Current.NextNode;
            Current = next;
            RaiseHistoryChanged();
        }

        public void Add(string filePath)
        {
            var node = new DirectoryNode(filePath);
            RaiseHistoryChanged();
            Current.NextNode = node;
            node.PreviousNode = Current;
            Current = node;
        }

        private void RaiseHistoryChanged()
        {
            HistoryChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}