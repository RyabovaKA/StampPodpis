/*
  Copyright © 2018 ASCON-Design Systems LLC. All rights reserved.
  This sample is licensed under the MIT License.
*/
using System;
using System.Globalization;
using System.Windows;
using System.Xml.Linq;

namespace Ascon.Pilot.SDK.GraphicLayerSample
{
    public class GraphicLayerElementCreator
    {
 
        public static GraphicLayerElement Create( Point scale, double angle, int position, string contentType, Guid elementId, int pageNumber, bool isFloating)
        {
            var element = new GraphicLayerElement(elementId, Guid.NewGuid(),
                position, scale, angle, contentType, pageNumber, isFloating);
            return element;
        }

    }
}