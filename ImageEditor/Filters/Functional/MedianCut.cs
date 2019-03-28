using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using ImageEditor.Filters.Interfaces;

namespace ImageEditor.Filters.Functional
{

    public class MedianCut : FunctionalFilterBase, IParameterized
    {
        private readonly HashSet<Color> _colorList;
        private readonly List<MedianCutCube> _cubeList;
        public readonly Dictionary<Color, int> Cache;
        public List<Parameter> Parameters { get; set; }
        public override string Name => "Median Cut";

        public MedianCut()
        {
            Cache = new Dictionary<Color, int>();
            _cubeList = new List<MedianCutCube>();
            _colorList = new HashSet<Color>();
            Parameters = new List<Parameter>();
            var param = new Parameter("Number of cuboids") { Value = "16" };
            for (int i = 2; i < 255; i*=2)
            {
                param.PossibleValues.Add(i.ToString());
            }
            Parameters.Add(param);
        }
        protected override void SetUpBeforeFiltering()
        {
            Clear();
            SetUpColors();
            SetUpColorPalette(int.Parse(Parameters[0].Value));
        }

        public void SetUpColors()
        {
            unsafe
            {
                BitmapData bitmapData = ProcessedBitmap.LockBits(new Rectangle(0, 0, ProcessedBitmap.Width, ProcessedBitmap.Height), ImageLockMode.ReadWrite, ProcessedBitmap.PixelFormat);

                int bytesPerPixel = Image.GetPixelFormatSize(ProcessedBitmap.PixelFormat) / 8;
                int heightInPixels = bitmapData.Height;
                int widthInBytes = bitmapData.Width * bytesPerPixel;
                byte* ptrFirstPixel = (byte*)bitmapData.Scan0;

                for (int y = 0; y < heightInPixels; y++)
                {
                    byte* currentLine = ptrFirstPixel + (y * bitmapData.Stride);
                    for (int x = 0; x < widthInBytes; x = x + bytesPerPixel)
                    {
                        int b = currentLine[x];
                        int g = currentLine[x + 1];
                        int r = currentLine[x + 2];

                        var color = Color.FromArgb(r, g, b);
                        if (!_colorList.Contains(color)) { }
                            _colorList.Add(color);
                    }
                }
                ProcessedBitmap.UnlockBits(bitmapData);
            }
        }

        // Gets the palette with specified count of the colors.     
        public List<Color> SetUpColorPalette(int colorCount)
        {
            // creates the initial cube covering all the pixels in the image
            var initalMedianCutCube = new MedianCutCube(_colorList);
            _cubeList.Add(initalMedianCutCube);

            // finds the minimum iterations needed to achieve the cube count (color count) we need
            int iterationCount = 1;
            while ((1 << iterationCount) < colorCount) { iterationCount++; }

            for (var iteration = 0; iteration < iterationCount; iteration++)
            {
                SplitCubes(colorCount);
            }
            var result = new List<Color>();

            // adds all the cubes' colors to the palette
            foreach (var cube in _cubeList)
            {
                result.Add(cube.Color);
            }

            return result;
        }

        // Splits all the cubes on the list.
        private void SplitCubes(int colorCount)
        {
            var newCubes = new List<MedianCutCube>();

            foreach (var cube in _cubeList)
            {
                // if another new cubes should be over the top; don't do it and just stop here
                // if (newCubes.Count >= colorCount) break;

                MedianCutCube newMedianCutCubeA, newMedianCutCubeB;

                // splits the cube along the longest color axis
                if (cube.RedSize >= cube.GreenSize && cube.RedSize >= cube.BlueSize)
                {
                    cube.SplitAtMedian(0, out newMedianCutCubeA, out newMedianCutCubeB);
                }
                else if (cube.GreenSize >= cube.BlueSize)
                {
                    cube.SplitAtMedian(1, out newMedianCutCubeA, out newMedianCutCubeB);
                }
                else
                {
                    cube.SplitAtMedian(2, out newMedianCutCubeA, out newMedianCutCubeB);
                }

                // adds newly created cubes to our list; but one by one and if there's enough cubes stops the process
                newCubes.Add(newMedianCutCubeA);
                if (newCubes.Count >= colorCount) break;
                newCubes.Add(newMedianCutCubeB);
            }
            _cubeList.Clear();
            _cubeList.AddRange(newCubes);
        }

        public void Clear()
        {
            Cache.Clear();
            _cubeList.Clear();
            _colorList.Clear();
        }

        public override Color Transform(byte r, byte g, byte b)
        {
            var color = Color.FromArgb(r, g, b);
            foreach (var cube in _cubeList)
            {
                if (cube.IsColorInBounds(color))
                {
                    return cube.Color;
                }
            }
            throw new Exception("Color not in any cube");
        }
    }
}
