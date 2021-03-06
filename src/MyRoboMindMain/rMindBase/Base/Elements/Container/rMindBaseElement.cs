﻿using rMind.Draw;
using rMind.Types;
using rMind.Theme;

using System.Linq;
using System.Collections.Generic;

using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using Windows.UI.Xaml.Input;
using System;

namespace rMind.Elements
{
    using Storage;
    using ColorContainer;
    using Nodes;

    public enum rElementType
    {
        RET_NONE
    }

    /// <summary>
    /// Base controller element 
    /// </summary>   
    public partial class rMindBaseElement : rMindBaseItem, IDrawContainer, IStorageObject
    {
        protected bool m_storable = true;
        public bool Storable { get { return m_storable; } set { m_storable = value; } }

        protected rElementType m_element_type;
        public rElementType ElementType { get { return m_element_type; } }

        protected ulong? m_pointer_timestamp;

        protected rMindBaseController m_inner_controller;

        protected rMindBaseController InnerController
        {
            get
            {
                if (m_inner_controller == null)
                {
                    m_inner_controller = new rMindBaseController(GetController()?.CanvasController);
                    m_inner_controller.Name = "Element";
                }
                return m_inner_controller;
            }
        }

        rMindNodeTheme m_node_theme = null;
        public rMindNodeTheme NodeTheme
        {
            get { return m_node_theme; }
            set
            {
                m_node_theme = value;
                foreach (var node in m_nodes_link.Values)
                    node.UpdateAccentColor();
            }
        }
        // Properties        
        protected Color m_accent_color;
        protected Border m_base;
        protected Border m_selector;
        protected bool m_selected;
        protected Dictionary<string, rMindBaseNode> m_nodes_link;        
        
        public List<rMindBaseNode> Nodes
        {
            get
            {
                return m_nodes_link.Select(x => x.Value).ToList();
            }
        }        

        public rMindBaseElement(rMindBaseController parent) : base(parent)
        {
            m_element_type = rElementType.RET_NONE;
            m_nodes_link = new Dictionary<string, rMindBaseNode>();

            Init();
            SubscribeInput();
        }

        public void Delete()
        {
            UnsubscribeInput();
            foreach(var node in m_nodes_link.Values)
            {
                node.Detach();
            }

            m_parent.RemoveElement(this);
        }

        public override void Init()
        {
            base.Init();

            m_base = new Border()
            {
                Background = rMindScheme.Get().MainContainerBrush()
            };

            m_selector = new Border()
            {
                Background = rMindColors.GetInstance().GetSolidBrush(rMindColors.GetSelectorBrush()),
                IsHitTestVisible = false,
                Visibility = Visibility.Collapsed
            };

            Template.Children.Add(m_base);
            Template.Children.Add(m_selector);

            Canvas.SetZIndex(m_selector, 10);
        }

        #region input        
        private void onPointerEnter(object sender, PointerRoutedEventArgs e)
        {
            if (m_locked) return;

            e.Handled = true;
            Parent.SetOveredItem(this);
            if (m_selected)
                return;        
        }


        private void onPointerExit(object sender, PointerRoutedEventArgs e)
        {
            if (m_locked) return;
            // e.Handled = true;
            Parent.SetPointerTimestamp(e);
            Parent.SetOveredItem(null);

            if (m_selected)
                return;         
        }

        private void onPointerUp(object sender, PointerRoutedEventArgs e)
        {
            if (m_locked) return;
            if (Parent.CheckIsOvered(this))
            {
                Parent.SetDragItem(null, e);
            }            
        }

        private void onPointerPress(object sender, PointerRoutedEventArgs e)
        {
            if (m_locked) return;
            // e.Handled = true;
            if (Parent.CheckIsOvered(this))
            {                
                Parent.SetDragItem(this, e);
                SetSelected(true);
                Parent.SetSelectedItem(this, e.KeyModifiers == Windows.System.VirtualKeyModifiers.Shift);
            }
            m_has_translate = false;
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
            var desc = new rMindNodeDesc();
            return CreateNode(desc);
        }

        public virtual rMindBaseNode CreateNode(rMindNodeDesc desc)
        {
            var node = new rMindBaseNode(this)
            {
                ConnectionType = desc.ConnectionType
            };

            node.IDS = "node" + m_nodes_link.Count.ToString();
            m_nodes_link[node.IDS] = node;
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
            if (node == null)
                return;

            if (m_nodes_link.ContainsKey(node.IDS) )
            {
                node.Detach();
                m_nodes_link.Remove(node.IDS);
                UpdateNodes();

                Template.Children.Remove(node.Template);
            }
        }

        public void RemoveNodes(List<rMindBaseNode> nodes)
        {
            foreach (var node in nodes)
            {
                RemoveNode(node);
            }
        }

        /// <summary> update IDSs nodes after remove </summary>
        protected void UpdateNodes()
        {
            var nodes = m_nodes_link.Values.ToList();
            m_nodes_link.Clear();
            foreach(var node in nodes)
            {
                node.IDS = "node" + m_nodes_link.Count.ToString();
                m_nodes_link[node.IDS] = node;
                node.Update();
            }
        }


        #endregion

        public void SetSelected(bool state)
        {
            m_selected = state;
            m_selector.Visibility = state ? Visibility.Visible : Visibility.Collapsed;
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

        protected CornerRadius m_border_radius;
        
        protected virtual void SetBorderRadius(CornerRadius value)
        {
            m_border_radius = value;
            m_base.CornerRadius = value;
            m_selector.CornerRadius = value;
        }
        public CornerRadius BorderRadius { get { return m_border_radius; } set { SetBorderRadius(value); } }

        protected Thickness m_border_thickness;

        protected virtual void SetBorderThickness(Thickness value)
        {
            m_border_thickness = value;
            m_base.BorderThickness = value;
        }

        public Thickness BorderThickness { get { return m_border_thickness; } set { SetBorderThickness(value); } }

        public Color AccentColor {
            get { return m_accent_color; }
            set { SetAccentColor(value); }
        }

        protected virtual void SetAccentColor(Color color)
        {
            m_accent_color = color;
            var shades = ColorForge.ColorHelper.GetColorShades(color, 8);
            m_base.Background = rMindColors.GetInstance().GetSolidBrush(shades[7]);
            m_base.BorderBrush = rMindColors.GetInstance().GetSolidBrush(shades[5]);
        }

        public bool InRect(Rectangle rect)
        {
            var left = Canvas.GetLeft(rect);
            var right = left + rect.Width;
            var top = Canvas.GetTop(rect);
            var bottom = top + rect.Height;

            return Position.X > left && (Position.X + Width) < right && Position.Y > top && (Position.Y + Height < bottom);
        }

        public virtual void Reset()
        {
            m_inner_controller?.Reset();    
        }
    }
}
