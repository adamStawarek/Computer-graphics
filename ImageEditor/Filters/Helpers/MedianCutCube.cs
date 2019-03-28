using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using MoreLinq.Extensions;

namespace ImageEditor.Filters.Helpers
{
    public class MedianCutCube
    {
        private byte _redLowBound;
        private byte _redHighBound;
        private byte _greenLowBound;
        private byte _greenHighBound;
        private byte _blueLowBound;
        private byte _blueHighBound;
        private readonly HashSet<Color> _colorList;
        //Gets the size of the particular color side of this cube.
        public int RedSize => _redHighBound - _redLowBound;
        public int GreenSize => _greenHighBound - _greenLowBound;
        public int BlueSize => _blueHighBound - _blueLowBound;
        //Gets an average color
        public Color Color
        {
            get
            {
                int red = 0, green = 0, blue = 0;

                _colorList.ForEach(value =>
                {
                    red += value.R;
                    green += value.G;
                    blue += value.B;
                });

                red = _colorList.Count == 0 ? 0 : red / _colorList.Count;
                green = _colorList.Count == 0 ? 0 : green / _colorList.Count;
                blue = _colorList.Count == 0 ? 0 : blue / _colorList.Count;

                var result = Color.FromArgb(255, red, green, blue);
                return result;
            }
        }

        public MedianCutCube(HashSet<Color> colors)
        {
            _colorList = colors;
            Shrink();
        }             

        // Shrinks this cube to the least dimensions that covers all the colors in the RGB space.
        private void Shrink()
        {
            _redLowBound = _greenLowBound = _blueLowBound = 255;
            _redHighBound = _greenHighBound = _blueHighBound = 0;

            foreach (var color in _colorList)
            {
                if (color.R < _redLowBound) _redLowBound = color.R;
                if (color.R > _redHighBound) _redHighBound = color.R;
                if (color.G < _greenLowBound) _greenLowBound = color.G;
                if (color.G > _greenHighBound) _greenHighBound = color.G;
                if (color.B < _blueLowBound) _blueLowBound = color.B;
                if (color.B > _blueHighBound) _blueHighBound = color.B;
            }
        }
        // Splits this cube's color list at median index, and returns two newly created cubes.
        //componentIndex-Index of the component (red = 0, green = 1, blue = 2)
        public void SplitAtMedian(byte componentIndex, out MedianCutCube firstMedianCutCube, out MedianCutCube secondMedianCutCube)
        {
            List<Color> colors;

            switch (componentIndex)
            {
                case 0:
                    colors = _colorList.OrderBy(color => color.R).ToList();
                    break;
                case 1:
                    colors = _colorList.OrderBy(color => color.G).ToList();
                    break;
                case 2:
                    colors = _colorList.OrderBy(color => color.B).ToList();
                    break;

                default:
                    throw new Exception("Only three color channels are supported (R, G and B).");

            }

            // retrieves the median index (a half point)
            var medianIndex = _colorList.Count/2;

            // creates the two half-cubes
            firstMedianCutCube = new MedianCutCube(colors.GetRange(0, medianIndex).ToHashSet());
            secondMedianCutCube = new MedianCutCube(colors.GetRange(medianIndex, colors.Count - medianIndex).ToHashSet());
        }   
        // Determines whether the color is in the space of this cube.
        public bool IsColorInBounds(Color color)
        {
            return (color.R >= _redLowBound && color.R <= _redHighBound) &&
                   (color.G >= _greenLowBound && color.G <= _greenHighBound) &&
                   (color.B >= _blueLowBound && color.B <= _blueHighBound);
        }

    }
}
