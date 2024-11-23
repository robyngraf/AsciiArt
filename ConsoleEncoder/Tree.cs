using System.Diagnostics;
using System.Text;

namespace ConsoleEncoder
{
    public class Tree<TKey, TValue> : Dictionary<TKey, Tree<TKey, TValue>> where TKey : notnull, IEquatable<TKey> where TValue : IEquatable<TValue>
    {
        public TValue? Value { get; set; } = default;

        public TValue? this[TKey[] keys]
        {
            get
            {
                if (keys == null)
                    throw new ArgumentNullException(nameof(keys));

                if (keys.Length == 0)
                    return Value;

                if (TryGetValue(keys[0], out var node))
                    return node[keys[1..]];

                throw new KeyNotFoundException();
            }
            set
            {
                if (keys == null)
                    throw new ArgumentNullException(nameof(keys));

                if (keys.Length == 0)
                    Value = value;
                else
                    this.GetOrNew(keys[0])[keys[1..]] = value;
            }
        }

        public bool IsLeaf() => Count == 0 || Values.All(n => n == this);

        public IEnumerable<Tree<TKey, TValue>> AllNodes()
        {
            var nodes = new Queue<Tree<TKey, TValue>>();
            nodes.Enqueue(this);
            var visitedNodes = new HashSet<Tree<TKey, TValue>>();
            do
            {
                var node = nodes.Dequeue();
                if (visitedNodes.Contains(node))
                    continue;
                yield return node;
                visitedNodes.Add(node);
                foreach (var child in node.OrderBy(pair => pair.Key).Select(pair => pair.Value))
                    if (!visitedNodes.Contains(child))
                        nodes.Enqueue(child);
            } while (nodes.Count > 0);
        }

        private IEnumerable<KeyValuePair<TKey, Tree<TKey, TValue>>> AllNodesWithKeys()
        {
            var nodes = new Queue<Tree<TKey, TValue>>();
            nodes.Enqueue(this);
            var nodePairs = new Queue<KeyValuePair<TKey, Tree<TKey, TValue>>>();
            var visitedNodes = new HashSet<Tree<TKey, TValue>>();
            do
            {
                var node = nodes.Dequeue();
                var isNotFirst = nodePairs.TryDequeue(out var nodePair);
                if (visitedNodes.Contains(node))
                    continue;
                if (isNotFirst)
                    yield return nodePair;
                else
                    yield return new KeyValuePair<TKey, Tree<TKey, TValue>>(nodePair.Key, node);
                visitedNodes.Add(node);
                foreach (var child in node.OrderBy(pair => pair.Key).Select(pair => pair.Value))
                    if (!visitedNodes.Contains(child))
                        nodes.Enqueue(child);
            } while (nodes.Count > 0);
        }

        public int DeepCount() => AllNodes().Count(node => node.HasValue);

        public TValue? FirstValue()
        {
            var node = AllNodes().FirstOrDefault(node => node.HasValue);
            if (node is null)
                return default;
            return node.Value;
        }

        public void Trim()
        {
            if (Values.Count == 0)
                return;
            if(DeepCount() <= 1)
            {
                Value = FirstValue();
                Clear();
                return;
            }
            foreach (var node in Values)
                node.Trim();
        }

        public void Cap(IEnumerable<TKey> keys)
        {
            keys = keys.ToArray();
            foreach (var node in AllNodes())
                if (node.Count == 0)
                    foreach (var key in keys)
                        node.Add(key, node);
        }

        public void Pad(IEnumerable<TKey> keys, Func<TKey, TKey, int> distanceFunction)
        {
            keys = keys.ToArray();
            foreach (var node in AllNodes())
            {
                var presentKeys = node.Keys.ToArray();
                var missingKeys = keys.Except(presentKeys).ToArray();
                for(int i = 0; i < missingKeys.Length; i++)
                {
                    var missingKey = missingKeys[i];
                    TKey? nearestKey = default;
                    int shortestDistance = int.MaxValue;
                    for (int j = 0; j < presentKeys.Length; j++)
                    {
                        var presentKey = presentKeys[j];
                        var distance = distanceFunction(missingKey, presentKey);
                        if (distance >= shortestDistance)
                            continue;
                        shortestDistance = distance;
                        nearestKey = presentKey;
                    }
                    if (nearestKey != null && shortestDistance < int.MaxValue)
                        node.Add(missingKey, node[nearestKey]);
                }
            }
        }

        public bool HasValue => Value != null && !Value.Equals(default);

        public string ToString(Func<TValue?, string> valueRenderer)
        {
            var sb = new StringBuilder();
            ToString(sb, valueRenderer, 0);
            return sb.ToString();
        }

        private void ToString(StringBuilder sb, Func<TValue?, string> valueRenderer, int depth)
        {
            if (HasValue)
                sb.AppendLine(depth, $"Value: {valueRenderer(Value)}");
            Dictionary<Tree<TKey, TValue>, TKey> previousNodes = new();
            foreach (var pair in this.OrderBy(p => p.Key))
            {
                var key = pair.Key;
                var node = pair.Value;

                if (node == this)
                {
                    sb.AppendLine(depth, $"Key: {key} -> self");
                }
                else if(previousNodes.TryGetValue(node, out var previousKey))
                {
                    sb.AppendLine(depth, $"Key: {key} -> {previousKey}");
                }
                else
                {
                    previousNodes.Add(node, key);
                    sb.AppendLine(depth, $"Key: {key}");
                    sb.AppendLine(depth, "{");
                    node.ToString(sb, valueRenderer, depth + 1);
                    sb.AppendLine(depth, "}");
                }
            }
        }

        /*
        public override int GetHashCode()
        {
            var hasher = new HashCode();
            if (HasValue)
                hasher.Add(Value);
            foreach (var node in AllNodes().Where(n => n != this))
            {
                hasher.Add(node.GetHashCode());
            }
            return hasher.ToHashCode();
        }

        public override bool Equals(object? other) => Equals(other as Tree<TKey, TValue>);

        public bool Equals(Tree<TKey, TValue>? other)
        {
            if (other == null)
                return false;

            var mine = AllNodesWithKeys();
            var theirs = other.AllNodesWithKeys();
            var otherEnumerator = theirs.GetEnumerator();
            foreach (var pair in mine)
            {
                if (!otherEnumerator.MoveNext())
                    return false;
                var otherPair = otherEnumerator.Current;

                if (!pair.Key.Equals(otherPair.Key))
                    return false;
                var myNode = pair.Value;
                var otherNode = otherPair.Value;
                if (myNode.HasValue)
                {
                    if (!otherNode.HasValue)
                        return false;
                    Debug.Assert(myNode.Value != null);
                    if (!myNode.Value.Equals(otherNode.Value))
                        return false;
                }

                if (myNode.Count != otherNode.Count)
                    return false;
            }
            if (otherEnumerator.MoveNext())
                return false;
            return true;
        }
        
        public static bool operator ==(Tree<TKey, TValue>? b1, Tree<TKey, TValue>? b2) => b1 is null ? b2 is null : b1.Equals(b2);

        public static bool operator !=(Tree<TKey, TValue>? b1, Tree<TKey, TValue>? b2) => !(b1 == b2);
        */
    }
}
