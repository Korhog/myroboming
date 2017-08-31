﻿using System;
using Windows.UI.Xaml.Controls;


namespace rMind.Elements
{
    /// <summary>
    /// Base scheme controller : Menu section
    /// </summary>
    public partial class rMindBaseController
    {
        protected virtual void InitMenu()
        {
            if (m_canvas == null)
            {
                return;
            }

            m_flyout = new MenuFlyout();
            m_flyout.Opening += OnFlyout;

            var item = new MenuFlyoutItem()
            {
                Text = "Удалить"
            };
            item.Click += (sender, e) => {
                (m_items_state.ActionItem as rMindBaseElement)?.Delete();
                 m_items_state.ActionItem = null;
            };
            m_flyout.Items.Add(item);

            item = new MenuFlyoutItem()
            {
                Text = "Статичный"
            };
            item.Click += (sender, e) => {
                if (m_items_state.ActionItem == null)
                    return;
                var it = m_items_state.ActionItem as rMind.Content.rMindRowContainer;
                if (it != null)
                {
                    it.Static = !it.Static;   
                }
                m_items_state.ActionItem = null;
            };
            m_flyout.Items.Add(item);

            m_canvas.ContextFlyout = m_flyout;
        }

        protected virtual void OnFlyout(object sender, object e)
        {
            m_items_state.ActionItem = m_overedItem;
        }

        //
    }
}

