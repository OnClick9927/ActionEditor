using UnityEditor.Experimental.GraphView;
using System;
using UnityEngine.UIElements;
using System.Collections.Generic;
using UnityEngine;

namespace ActionEditor.Nodes
{
    public class GraphPort : Port
    {
        private class DefaultEdgeConnectorListener : IEdgeConnectorListener
        {
            private GraphViewChange m_GraphViewChange;

            private List<Edge> m_EdgesToCreate;

            private List<GraphElement> m_EdgesToDelete;

            public DefaultEdgeConnectorListener()
            {
                m_EdgesToCreate = new List<Edge>();
                m_EdgesToDelete = new List<GraphElement>();
                m_GraphViewChange.edgesToCreate = m_EdgesToCreate;
            }

            public void OnDropOutsidePort(Edge edge, Vector2 position)
            {
                GraphConnection con = edge as GraphConnection;
                GraphPort port = con.output != null ? con.output : con.input;
                port.node.view.OpenSearchPop(port, Vector2.zero);
            }

            public void OnDrop(GraphView graphView, Edge edge)
            {
                m_EdgesToCreate.Clear();
                m_EdgesToCreate.Add(edge);
                m_EdgesToDelete.Clear();
                if (edge.input.capacity == Capacity.Single)
                {
                    foreach (Edge connection in edge.input.connections)
                    {
                        if (connection != edge)
                        {
                            m_EdgesToDelete.Add(connection);
                        }
                    }
                }

                if (edge.output.capacity == Capacity.Single)
                {
                    foreach (Edge connection2 in edge.output.connections)
                    {
                        if (connection2 != edge)
                        {
                            m_EdgesToDelete.Add(connection2);
                        }
                    }
                }

                if (m_EdgesToDelete.Count > 0)
                {
                    graphView.DeleteElements(m_EdgesToDelete);
                }

                List<Edge> edgesToCreate = m_EdgesToCreate;
                if (graphView.graphViewChanged != null)
                {
                    edgesToCreate = graphView.graphViewChanged(m_GraphViewChange).edgesToCreate;
                }

                foreach (Edge item in edgesToCreate)
                {
                    graphView.AddElement(item);
                    edge.input.Connect(item);
                    edge.output.Connect(item);
                }
            }
        }
        private IEdgeConnectorListener listener;
        public new GraphNode node { get { return base.node as GraphNode; } }
        protected GraphPort(Orientation portOrientation, Direction portDirection, Capacity portCapacity, Type type) : base(portOrientation, portDirection, portCapacity, type)
        {
        }

        public static Port Create(Orientation orientation, Direction direction, Capacity capacity, Type type)
        {
            DefaultEdgeConnectorListener listener = new DefaultEdgeConnectorListener();
            GraphPort port = new GraphPort(orientation, direction, capacity, type)
            {
                m_EdgeConnector = new EdgeConnector<GraphConnection>(listener),
                listener = listener,
            };
            port.AddManipulator(port.m_EdgeConnector);
            return port;
        }

        public static void ValidConnection(GraphView view, GraphConnection connection)
        {
            connection.input.listener.OnDrop(view, connection);
        }

    }
}
