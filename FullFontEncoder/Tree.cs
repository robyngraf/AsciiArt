namespace FullFontEncoder
{
    public class Tree<T>()
    {
        public bool AddIfNotPresent(IEnumerable<bool> keys, T value)
        {
            Node currentNode = Root;
            foreach (var key in keys)
            {
                var nextNode = key ? currentNode.Left : currentNode.Right;
                if (nextNode == null)
                {
                    nextNode = new();
                    if (key) currentNode.Left = nextNode;
                    else currentNode.Right = nextNode;
                }
                currentNode = nextNode;
            }
            if (currentNode.HasValue) return false;
            currentNode.Value = value;
            currentNode.HasValue = true;
            Count += 1;
            return true;
        }

        public T? Get(IEnumerable<bool> keys)
        {
            Node currentNode = Root;
            foreach (var key in keys)
            {
                var nextNode = key ? currentNode.Left : currentNode.Right;
                if (nextNode == null) return default;
                currentNode = nextNode;
            }
            if (!currentNode.HasValue) return default;
            return currentNode.Value;
        }

        public T? GetSimilarTo(IEnumerable<bool> keys)
        {
            Node currentNode = Root;
            foreach (var key in keys)
            {
                var nextNode = key ?
                    currentNode.Left ?? currentNode.Right :
                    currentNode.Right ?? currentNode.Left;
                if (nextNode == null) return default;
                currentNode = nextNode;
            }
            if (!currentNode.HasValue) return default;
            return currentNode.Value;
        }

        public IEnumerable<T> GetAllValues() => Tree<T>.GetAllValuesFromNode(Root);

        private static IEnumerable<T> GetAllValuesFromNode(Node? node)
        {
            if (node is null) yield break;
            if (node.HasValue) yield return node.Value!;
            foreach (var value in Tree<T>.GetAllValuesFromNode(node.Left))
                yield return value;
            foreach (var value in Tree<T>.GetAllValuesFromNode(node.Right))
                yield return value;
        }

        public int Count { get; private set; } = 0;

        private readonly Node Root = new();

        private class Node
        {
            public Node? Left { get; set; } = null;
            public Node? Right { get; set; } = null;
            public T? Value { get; set; }
            public bool HasValue { get; set; }
        }
    }
}
    