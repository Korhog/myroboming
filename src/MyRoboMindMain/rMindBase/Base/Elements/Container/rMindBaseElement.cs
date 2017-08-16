﻿using rMind.Draw;
using rMind.Types;
using rMind.Theme;

using System.Collections.Generic;

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Shapes;
using Windows.UI.Xaml.Input;
using System;

namespace rMind.Elements
{
    using Nodes;
    /// <summary>
    /// Base scheme element 
    /// </summary>
    public class rMindBaseElement : rMindBaseItem, IDrawContainer
    {
        // Properties
        protected Border m_base;
        protected bool m_selected;
        protected Dictionary<string, rMindBaseNode> m_nodes_link;

        public rMindBaseElement(rMindBaseController parent) : base(parent)
        {
            m_nodes_link = new Dictionary<string, rMindBaseNode>();

            Init();
            SubscribeInput();
        }

        public void Delete()
        {
            UnsubscribeInput();
            m_parent.RemoveElement(this);
        }

        public override void Init()
        {
            base.Init();

            m_base = new Border()
            {
                Background = rMindScheme.Get().MainContainerBrush()
            };

            Template.Children.Add(m_base);
        }

        #region input        
        private void onPointerEnter(object sender, PointerRoutedEventArgs e)
        {
            e.Handled = true;
            Parent.SetOveredItem(this);
            if (m_selected)
                return;        
        }


        private void onPointerExit(object sender, PointerRoutedEventArgs e)
        {
            e.Handled = true;
            Parent.SetOveredItem(null);
            if (m_selected)
                return;         
        }

        private void onPointerUp(object sender, PointerRoutedEventArgs e)
        {            
            if (Parent.CheckIsOvered(this))
            {
                Parent.SetDragItem(null, e);
                SetSelected(true);
                Parent.SetSelectedItem(this, e.KeyModifiers == Windows.System.VirtualKeyModifiers.Shift);
            }            
        }

        private void onPointerPress(object sender, PointerRoutedEventArgs e)
        {
            e.Handled = true;
            if (Parent.CheckIsOvered(this))
            {
                Parent.SetDragItem(this, e);
            }            
        }

        void SubscribeInput()
        {
            m_base.PointerEntered += onPointerEnter;
            m_base.PointerExited += onPointerExit;
            m_base.PointerPressed += onPointerPress;
            m_base.PointerReleased += onPointerUp;
        }

        void UnsubscribeInput()
        {
            m_base.PointerEntered -= onPointerEnter;
            m_base.PointerExited -= onPointerExit;
            m_base.PointerPressed -= onPointerPress;
            m_base.PointerReleased -= onPointerUp;
        }
        #endregion

        #region nodes

        /// <summary> create new node for connection </summary>
        public virtual rMindBaseNode CreateNode()
        {
            var node = new rMindBaseNode(this);
            m_nodes_link["node" + m_nodes_link.Count.ToString()] = node;
            Template.Children.Add(node.Template);

            return node;
        }        

        public void RemoveNode(string ids)
        {
            if (!m_nodes_link.ContainsKey(ids))
                return;

            var node = m_nodes_link[ids];
            RemoveNode(node);
        }

        public void RemoveNode(rMindBaseNode node)
        {
            RenameNodes();
        }

        /// <summary> update IDSs nodes after remove </summary>
        void RenameNodes()
        {

        }


        #endregion

        public void SetSelected(bool state)
        {
            m_selected = state;
        }

        public override Vector2 GetOffset()
        {
            return Position;           
        }

        public override Vector2 SetPosition(Vector2 newPos)
        {
            var translation = base.SetPosition(newPos);

            foreach (var node in m_nodes_link.Values)
            {
                node.Update();
            }


            return translation;
        }
    }
}