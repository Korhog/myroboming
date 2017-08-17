﻿using System;
using System.Collections.Generic;
using System.Linq;

using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using Windows.UI.Xaml.Controls;
using System.Threading.Tasks;

namespace rMind.Content
{
    using Row;

    using Elements;

    /// <summary>
    /// Контейнер с возможностью добавлять строки с данными
    /// </summary>
    public class rMindRowContainer : rMindBaseElement
    {
        protected List<rMindRow> m_rows;
        protected Button m_add_button;

        /// <summary> Шаблон для добавления новых строк </summary>
        public rMindRow AddRowTemplate { get; set; } = new rMindRow {
            OutputNodeType = Nodes.rMindNodeConnectionType.Container
        };

        protected bool m_static = true;
        /// <summary> Контейнер с фиксированным количеством строк. Если false появляется кнопка для 
        /// добавления строк типа с шаблоном AddRowTemplate </summary>        
        public bool Static {
            get { return m_static; }
            set { SetStatic(value); }
        }

        protected virtual void SetStatic(bool state)
        {
            if (state == m_static) return;

            m_static = state;
            if (m_static)
            {
                Template.Children.Remove(m_add_button);
                Template.RowDefinitions.Remove(
                    Template.RowDefinitions[Template.RowDefinitions.Count - 1]
                );
            }
            else
            {  
                if (m_add_button == null)
                {
                    //< FontIcon FontFamily = "Segoe MDL2 Assets" Glyph = "&#xE109;" />

                    m_add_button = new Button()
                    {
                        Content = new FontIcon()
                        {
                            FontFamily = new FontFamily("Segoe MDL2 Assets"),
                            Glyph = "\uE109",
                            Foreground = new SolidColorBrush(Colors.White),
                            FontSize = 10
                        },
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Background = new SolidColorBrush(Colors.Green),
                        Padding = new Thickness(2)

                    };
                    m_add_button.Click += OnAddRowClick;
                    Grid.SetColumn(m_add_button, 1);
                }

                Template.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
                Grid.SetRow(m_add_button, Template.RowDefinitions.Count - 1);
                Template.Children.Add(m_add_button);
            }
        }

        protected virtual void OnAddRowClick(object sender, RoutedEventArgs e)
        {
            AddRow(new rMindRow {
                InputNodeType = Nodes.rMindNodeConnectionType.Container,
                OutputNodeType = Nodes.rMindNodeConnectionType.Container
            });
        }

        public rMindRowContainer(rMindBaseController parent) : base(parent)
        {
            Template.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(40) });
            Template.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });

            Grid.SetColumnSpan(m_base, 3);

            m_rows = new List<rMindRow>(); 
        }

        public virtual rMindRow AddRow()
        {
            return AddRow(new rMindRow());
        }

        public virtual rMindRow AddRow(rMindRow row)
        {
            m_rows.Add(row);
            // Данная секция добавлена чисто для тестирования
            if (m_rows.Count > 0)
            {
                // Если это не первая строка, то надо добавить строку в шаблон
                Template.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            }

            //
            var idx_row = m_rows.IndexOf(row);

            
            if (row.InputNodeType != Nodes.rMindNodeConnectionType.None)
            {                
                CreateNode(new Nodes.rMindNodeDesc { ConnectionType = row.InputNodeType})
                    .SetCell(0, idx_row);
            }
               
            if (row.OutputNodeType != Nodes.rMindNodeConnectionType.None)
            {
                CreateNode(new Nodes.rMindNodeDesc { ConnectionType = row.OutputNodeType })
                    .SetCell(2, idx_row);
            }            

            var rect = new Rectangle()
            {
                Height = 20,
                Margin = new Thickness(2),
                Fill = new SolidColorBrush(Colors.CadetBlue),
                IsHitTestVisible = false
            };

            Grid.SetColumn(rect, 1);
            Grid.SetRow(rect, idx_row);
            Template.Children.Add(rect);

            Grid.SetRowSpan(m_base, m_rows.Count);

            if (!m_static)
            {
                Grid.SetRow(m_add_button, Template.RowDefinitions.Count - 1);
            }

            return row;
        }
    }
}
