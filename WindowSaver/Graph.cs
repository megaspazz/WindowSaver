using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowSaver
{
	public class Graph
	{
		private Dictionary<IntPtr, Node> _nodes = new Dictionary<IntPtr, Node>();

		public void AddEdge(IntPtr hWnd1, IntPtr hWnd2)
		{
			Node n1 = _nodes.ContainsKey(hWnd1) ? _nodes[hWnd1] : new Node(hWnd1);
			Node n2 = _nodes.ContainsKey(hWnd2) ? _nodes[hWnd2] : new Node(hWnd2);
			n1.nexts.Add(n2);
			n2.prevs.Add(n1);
			_nodes[hWnd1] = n1;
			_nodes[hWnd2] = n2;
		}

		/**
		 * This method returns a list of the nodes after they have been topologically sorted.
		 * It will return null if it was impossible to do so.
		 * NOTE: This will destructively modify the graph!
		 */
		public List<Node> ToplogicalSort()
		{
			ICollection<Node> nodes = _nodes.Values;
			List<Node> lst = new List<Node>();
			HashSet<Node> starts = new HashSet<Node>();
			Queue<Node> q = new Queue<Node>();
			foreach (Node n in nodes)
			{
				if (n.prevs.Count == 0)
				{
					starts.Add(n);
					q.Enqueue(n);
				}
			}
			while (q.Count > 0)
			{
				Node n = q.Dequeue();
				if (!starts.Contains(n))
				{
					continue;
				}
				lst.Add(n);
				foreach (Node m in n.nexts)
				{
					m.prevs.Remove(n);
					if (m.prevs.Count == 0)
					{
						starts.Add(m);
						q.Enqueue(m);
					}
				}
				n.nexts.Clear();
			}
			foreach (Node n in nodes)
			{
				if ((n.prevs.Count > 0) || (n.nexts.Count > 0))
				{
					return null;
				}
			}
			return lst;
		}

		/**
		 * A Node containing information about incoming and outgoing edges.
		 */
		public class Node
		{
			public IntPtr Handle { get; set; }
			public HashSet<Node> nexts { get; private set; }
			public HashSet<Node> prevs { get; private set; }

			public Node(IntPtr hWnd)
			{
				this.Handle = hWnd;
				this.nexts = new HashSet<Node>();
				this.prevs = new HashSet<Node>();
			}
		}
	}
}
