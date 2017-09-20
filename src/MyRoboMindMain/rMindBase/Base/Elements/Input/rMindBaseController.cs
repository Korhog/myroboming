﻿using System;
using System.Linq;
using System.Collections.Generic;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.UI.Input;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.Foundation;

namespace rMind.Elements
{
    using Types;
    using Nodes;
    using Windows.UI.Xaml;

    public enum rMindManipulationMode
    {
        None,
        Scale,
        Scroll,
        Select
    }

    public struct ManipulationData
    {
        public Vector2 BeginVector;
        // vector from center of canvas to scale center
        public Vector2 CenterVector;
        public Vector2 CurrentScroll;
        public double BaseScale;
    }

    /// <summary>
    /// Base scheme controller : input
    /// </summary>
    public partial class rMindBaseController
    {
        protected Dictionary<uint, PointerPoint> m_touch_list;
        protected Vector2 m_first_touch;
        /// <summary>center of canvas</summary>
        protected Vector2 m_canvas_center;
        protected rMindManipulationMode m_manipulation_mode = rMindManipulationMode.None;
        protected ManipulationData m_manipulation_data;
        protected ulong? m_pointer_timestamp = null;

        TextBlock m_test;

        public void SubscribeInput()
        {
            if (m_touch_list == null)
                m_touch_list = new Dictionary<uint, PointerPoint>();

            m_test = new TextBlock()
            {
                VerticalAlignment = Windows.UI.Xaml.VerticalAlignment.Center,
                HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Center
            };
            m_canvas.Children.Add(m_test);


            // events            
            m_scroll.PointerMoved += onPointerMove;
            m_scroll.DoubleTapped += (s, e) => { };
            m_canvas_center = new Vector2(m_canvas.Width / 2.0, m_canvas.Height / 2.0);

            m_scroll.PointerReleased += onPointerUp;
            m_scroll.PointerPressed += onPointerPress;
            m_scroll.PointerWheelChanged += onWheel;

            m_scroll.PointerExited += onPointerExit;

            m_scroll.Loaded += onLoad;

            Window.Current.CoreWindow.KeyDown += onKeyDown;
            Window.Current.CoreWindow.KeyUp += onKeyUp;
        }

        void onLoad(object sender, Windows.UI.Xaml.RoutedEventArgs args)
        {
            m_scroll.ChangeView(
                (m_scroll.ExtentWidth - m_scroll.ViewportWidth) / 2.0,
                (m_scroll.ExtentHeight - m_scroll.ViewportHeight) / 2.0,
                1);
        }

        public void UnsubscribeInput()
        {
            // events                
            m_scroll.PointerMoved -= onPointerMove;

            m_scroll.PointerReleased -= onPointerUp;
            m_scroll.PointerPressed -= onPointerPress;
            m_scroll.PointerWheelChanged -= onWheel;

            m_scroll.Loaded -= onLoad;

            m_scroll.PointerExited -= onPointerExit;

            Window.Current.CoreWindow.KeyDown -= onKeyDown;
            Window.Current.CoreWindow.KeyUp -= onKeyUp;
        }

        // input
        protected virtual void onPointerUp(object sender, PointerRoutedEventArgs e)
        {
            if (m_manipulation_mode == rMindManipulationMode.Select)
            {
                // Пока просто отключаем рамку.
                StopSelection();
                return;
            }

            var point = e.GetCurrentPoint(m_scroll);
            m_pointer_timestamp = point.Timestamp;

            if (m_touch_list.ContainsKey(point.PointerId))
                m_touch_list.Remove(point.PointerId);

            if (m_overed_item == null && e.KeyModifiers != Windows.System.VirtualKeyModifiers.Shift)
            {
                SetSelectedItem(null);   
            }

            if (point.Properties.PointerUpdateKind == PointerUpdateKind.MiddleButtonReleased)
            {
                if (m_overed_item != null)
                {
                    m_overed_item.Delete();
                }
            }

            if (m_items_state.IsDragDot())
            {
                var attachNode = m_items_state.OveredNode ?? m_items_state.MagnetNode;

                if (attachNode == null)
                {
                    m_items_state.DragedWireDot.Wire.Delete();
                    m_items_state.DragedWireDot = null;
                }
                else
                {
                    attachNode.Attach(m_items_state.DragedWireDot);
                    m_items_state.DragedWireDot.Wire.SetEnabledHitTest(true);
                    SetDragWireDot(null, e);
                }

                m_magnet.Hide();
            }

            m_items_state.DragedItem = null;
            m_canvas.ManipulationMode = ManipulationModes.System;
            m_manipulation_mode = rMindManipulationMode.None;            
        }

        protected virtual void onPointerExit(object sender, PointerRoutedEventArgs e)
        {

        }

        protected bool CanControll()
        {
            if (m_overed_item == null && m_items_state.OveredNode == null && m_items_state.DragedWireDot == null)
                return true;

            return false;
        }

        private void onPointerPress(object sender, PointerRoutedEventArgs e)
        {
            var pointer = e.GetCurrentPoint(m_scroll);
            var doubleClick = m_pointer_timestamp.HasValue && pointer.Timestamp - m_pointer_timestamp.Value < 300000;

            if (pointer.PointerDevice.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Touch)
            {
                m_touch_list[pointer.PointerId] = pointer;                                          
            }
            else
            {
                var mouseScroll =
                    pointer.PointerDevice.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Mouse &&
                    e.KeyModifiers == Windows.System.VirtualKeyModifiers.Control;

                if (mouseScroll)
                {
                    SetScrollMode(e);
                    return;
                }

                if (CanControll())
                    StartSelection(e.GetCurrentPoint(m_canvas));
                return;
            }

            if (CanControll())
            { 
                if (doubleClick)
                {
                    StartSelection(e.GetCurrentPoint(m_canvas));
                    m_canvas.ManipulationMode = ManipulationModes.None;
                    return;
                }
                SetManipulation(true, e);
                m_manipulation_mode = rMindManipulationMode.None;
                return;
            }

            SetManipulation(false, e);
        }

        protected virtual void SetManipulation(bool state, PointerRoutedEventArgs e)
        {
            if (state)
            {
                m_touch_list.Clear();
                SetDragItem(null, e);
                SetDragWireDot(null, e);
                m_canvas.ManipulationMode = ManipulationModes.System;
                e.Handled = true;
            }
            else
            {
                m_canvas.ManipulationMode = ManipulationModes.None;
            }
        }

        protected virtual void SetScrollMode(PointerRoutedEventArgs e)
        {
            var pointer = e.GetCurrentPoint(m_scroll);
            m_manipulation_mode = rMindManipulationMode.Scroll;
            m_manipulation_data.BeginVector = new Vector2(pointer);
            m_manipulation_data.CurrentScroll = new Vector2(
                m_scroll.HorizontalOffset,
                m_scroll.VerticalOffset
            );

            SetManipulation(false, e);
        }

        protected virtual void onPointerMove(object sender, PointerRoutedEventArgs e)
        {
            e.Handled = true;

            if (m_manipulation_mode == rMindManipulationMode.Select)
            {
                UpdateSelectorRect(e.GetCurrentPoint(m_canvas)); 
                return;
            }
            
            if (m_manipulation_mode == rMindManipulationMode.Scroll)
            {
                var point = e.GetCurrentPoint(m_scroll);
                var offset = new Vector2(point) - m_manipulation_data.BeginVector;
                m_scroll.ChangeView(
                    m_manipulation_data.CurrentScroll.X - offset.X,
                    m_manipulation_data.CurrentScroll.Y - offset.Y,
                    m_scroll.ZoomFactor,
                    true
                );
                return;
            }            

            if (m_items_state.IsDragDot())
            {
                DragWireDot(e);
            }

            if (m_items_state.IsDrag())
            {
                DragContainer(e);
            }
        }

        protected virtual void onWheel(object sender, PointerRoutedEventArgs e)
        {
            e.Handled = true;
        }

        protected virtual void onKeyDown(CoreWindow window, KeyEventArgs e)
        {
            if (e.VirtualKey == Windows.System.VirtualKey.Delete)
            {
                DeleteSelection();
            }
        }

        protected virtual void onKeyUp(CoreWindow window, KeyEventArgs e)
        {

        }
    }
}

