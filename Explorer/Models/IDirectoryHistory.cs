using System;
using System.Collections.Generic;

namespace Explorer.Models
{
    internal interface IDirectoryHistory : IEnumerable<DirectoryNode>
    {
        bool CanMoveBack { get; }

        bool CanMoveForward { get; }

        DirectoryNode Current { get; }

        event EventHandler HistoryChanged;

        void MoveBack();

        void MoveForward();

        void Add(string filePath);
    }

    internal class DirectoryNode
    {
        public DirectoryNode(string directoryPath)
        {
            this.DirectoryPath = directoryPath;
        }

        public DirectoryNode PreviousNode { get; set; }

        public DirectoryNode NextNode { get; set; }

        public string DirectoryPath { get; }
    }
}