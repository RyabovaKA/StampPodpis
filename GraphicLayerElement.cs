/*
  Copyright © 2018 ASCON-Design Systems LLC. All rights reserved.
  This sample is licensed under the MIT License.
*/
using System;
using System.Windows;

namespace Ascon.Pilot.SDK.GraphicLayerSample
{
    [Serializable]
    public class GraphicLayerElement : IGraphicLayerElement
    {
        public Guid ElementId { get; set; }
        public Guid ContentId { get; set; }
        public Point Scale { get; set; }
        public double Angle { get; set; }
        public int PositionId { get; set; }
        public int PageNumber { get; set; }
        public Point CornerPoint { get; set; }
        public string ContentType { get; set; }
        public bool IsFloating { get; set; }
        public HorizontalAlignment HorizontalAlignment { get; set; }
        public VerticalAlignment VerticalAlignment { get; set; }
        public double OffsetX { get; set; }
        public double OffsetY { get; set; }

        public GraphicLayerElement() { }

        public GraphicLayerElement(Guid elementId, Guid contentId, int positionId, Point scale, double angle, 
            string contentType, int pageNumber, bool isFloating)
        {
            if (pageNumber < 0)
                throw new ArgumentOutOfRangeException(nameof(pageNumber), pageNumber, "pageNumber must be greater than or equal to 0");

            ElementId = elementId;
            ContentId = contentId;
            Scale = scale;
            Angle = angle;
            PositionId = positionId;
            ContentType = contentType;
            PageNumber = pageNumber;
            IsFloating = isFloating;
        }

        public string GetFileName()
        {
            return GraphicLayerElementConstants.GRAPHIC_LAYER_ELEMENT + ElementId;
        }

        public string GetContentFileName()
        {
            return GraphicLayerElementConstants.GRAPHIC_LAYER_ELEMENT_CONTENT + ContentId;
        }
    }
}