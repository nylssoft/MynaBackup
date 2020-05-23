/*
    Myna Backup
    Copyright (C) 2020 Niels Stockfleth

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/
using System.ComponentModel;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace Backup
{
    /// Provides a decorator for sorting a column in a list view.
    /// </summary>
    public class SortDecorator
    {
        private SortAdorner adorner;
        private UIElement element;

        /// <summary>
        /// Constructs the decorator with the initial sort direction.
        /// </summary>
        /// <param name="direction">initial sort direction</param>
        public SortDecorator(ListSortDirection direction)
        {
            Direction = direction;
        }

        public bool HasAdorner {  get { return adorner != null; } }

        /// <summary>
        /// Returns the sort direction.
        /// </summary>
        public ListSortDirection Direction { get; private set; }

        /// <summary>
        /// Updates the decorated element after it has been clicked.
        /// Toggles the sort direction if the same column has been clicked.
        /// </summary>
        /// <param name="elem">UI element for the clicked column</param>
        public void Click(UIElement elem)
        {
            if (element != null)
            {
                var layerBefore = AdornerLayer.GetAdornerLayer(element);
                if (layerBefore != null && adorner != null)
                {
                    layerBefore.Remove(adorner);
                    adorner = null;
                }
                if (element == elem)
                {
                    Direction = Direction == ListSortDirection.Ascending ? ListSortDirection.Descending : ListSortDirection.Ascending;
                }
            }
            element = elem;
            if (element != null)
            {
                var layer = AdornerLayer.GetAdornerLayer(element);
                if (layer != null)
                {
                    adorner = new SortAdorner(element, Direction);
                    layer.Add(adorner);
                }
            }
        }

        private class SortAdorner : Adorner
        {
            private static Geometry geometryAscending = Geometry.Parse("M 0 4 L 3.5 0 L 7 4 Z");
            private static Geometry geometryDescending = Geometry.Parse("M 0 0 L 3.5 4 L 7 0 Z");

            private Geometry geometry;

            public SortAdorner(UIElement uiElement, ListSortDirection direction)
                : base(uiElement)
            {
                geometry = direction == ListSortDirection.Ascending ? geometryAscending : geometryDescending;
                IsHitTestVisible = false;
            }

            protected override void OnRender(DrawingContext dc)
            {
                base.OnRender(dc);
                var w = AdornedElement.RenderSize.Width;
                if (w >= 20)
                {
                    var h = AdornedElement.RenderSize.Height;
                    dc.PushTransform(new TranslateTransform(w - 15, (h - 5) / 2));
                    dc.DrawGeometry(Brushes.Black, null, geometry);
                    dc.Pop();
                }
            }
        }
    }
}
