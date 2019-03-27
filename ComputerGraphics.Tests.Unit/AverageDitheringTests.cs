using System;
using System.Drawing;
using ImageEditor.Filters;
using ImageEditor.Filters.Functional;
using NUnit.Framework;

namespace ComputerGraphics.Tests.Unit
{
    [TestFixture]
    public class AverageDitheringTests
    {
        [Test]
        public void GetGreyLevels_Returns_0_255_When_K_Is_2()
        {
            var filter = new AverageDithering();
            CollectionAssert.AreEquivalent(new Byte[] { 0,  255 }, filter.GetGreyLevels(2));
        }

        [Test]
        public void GetGreyLevels_Returns_0_85_170_255_When_K_Is_4()
        {
            var filter = new AverageDithering();
            CollectionAssert.AreEquivalent(new Byte[]{0,85,170,255},filter.GetGreyLevels(4));
        }

        [Test]
        public void GetGreyLevels_Returns_0_51_102_153_204_255_When_K_Is_6()
        {
            var filter = new AverageDithering();
            CollectionAssert.AreEquivalent(new Byte[] { 0, 51,102,153,204, 255 }, filter.GetGreyLevels(6));
        }

        [Test]
        public void GetClosestGreyLevel_Return_153_And_102_When_Color_Intensity_Is_150_And_K_Is_6()
        {
            var filter=new AverageDithering();
            Assert.AreEqual((102,153),filter.GetClosestGreyLevel(filter.GetGreyLevels(6),Color.FromArgb(150,150,150)));
        }

        [Test]
        public void GetClosestGreyLevel_Return_204_And_255_When_Color_Intensity_Is_254_And_K_Is_6()
        {
            var filter = new AverageDithering();
            Assert.AreEqual((204,255), filter.GetClosestGreyLevel(filter.GetGreyLevels(6), Color.FromArgb(254,254,254)));
        }

        [Test]
        public void GetClosestGreyLevel_Return_153_And_102_When_Color_Intensity_Is_102_And_K_Is_6()
        {
            var filter = new AverageDithering();
            Assert.AreEqual((102, 153), filter.GetClosestGreyLevel(filter.GetGreyLevels(6), Color.FromArgb(102,102,102)));
        }

        [Test]
        public void GetClosestGreyLevel_Return_153_And_102_When_Color_Intensity_Is_153_And_K_Is_6()
        {
            var filter = new AverageDithering();
            Assert.AreEqual((153, 204), filter.GetClosestGreyLevel(filter.GetGreyLevels(6), Color.FromArgb(153,153,153)));
        }

        [Test]
        public void GetClosestGreyLevel_Return_0_And_51_When_Color_Intensity_Is_0_And_K_Is_6()
        {
            var filter = new AverageDithering();
            Assert.AreEqual((0,51), filter.GetClosestGreyLevel(filter.GetGreyLevels(6), Color.FromArgb(0,0,0)));
        }

        [Test]
        public void GetClosestGreyLevel_Return_204_And_255_When_Color_Intensity_Is_255_And_K_Is_6()
        {
            var filter = new AverageDithering();
            Assert.AreEqual((204, 255), filter.GetClosestGreyLevel(filter.GetGreyLevels(6), Color.FromArgb(255,255,255)));
        }
    }
}
